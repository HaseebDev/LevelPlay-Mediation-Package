using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Autech.LevelPlay
{
    /// <summary>
    /// Local consent collection: the alternative to <see cref="ConsentManager"/>
    /// (InMobi CMP), selected with VerifyLevelPlay → <c>useLocalConsent</c>.
    ///
    /// Shows the package's authored consent form on first launch, stores the
    /// answer on the device, and exposes it so <see cref="MediationConsentManager"/>
    /// can push the ads decision into LevelPlay, which forwards it to every
    /// mediated adapter. Re-openable from the existing privacy-options entry
    /// point; changing the choice there raises <see cref="OnConsentChanged"/> so
    /// the flags are pushed again.
    ///
    /// This is deliberately NOT an IAB TCF CMP: it writes no <c>IABTCF_*</c>
    /// values and is not Google-certified. It is for mediation stacks that do not
    /// require a certified CMP. Keep using the InMobi path where one is required.
    ///
    /// The analytics decision is exposed through <see cref="AnalyticsAllowed"/>
    /// for an analytics package to read; this package does not collect analytics.
    /// </summary>
    public class LocalConsentManager
    {
        private const string StoredKey = "Autech.LevelPlay.LocalConsent.Stored";
        private const string AdsKey = "Autech.LevelPlay.LocalConsent.Ads";
        private const string AnalyticsKey = "Autech.LevelPlay.LocalConsent.Analytics";
        private const string AnsweredAtKey = "Autech.LevelPlay.LocalConsent.AnsweredAtUtc";
        private const string PolicyVersionKey = "Autech.LevelPlay.LocalConsent.PolicyVersion";

        /// <summary>
        /// Countries where GDPR or UK GDPR applies: the EEA (EU 27 plus Iceland,
        /// Liechtenstein, Norway) and the UK. Used only when the publisher opts into
        /// region-limited asking.
        /// </summary>
        private static readonly HashSet<string> GdprRegions = new HashSet<string>
        {
            "AT", "BE", "BG", "HR", "CY", "CZ", "DK", "EE", "FI", "FR",
            "DE", "GR", "HU", "IE", "IT", "LV", "LT", "LU", "MT", "NL",
            "PL", "PT", "RO", "SK", "SI", "ES", "SE",
            "IS", "LI", "NO",
            "GB"
        };

        /// <summary>Resources path of the authored form prefab shipped in the package.</summary>
        private const string FormResourcePath = "AutechLocalConsentForm";

        private readonly AdConfiguration config;

        /// <summary>
        /// True while a form instance is on screen. Guards against a second form
        /// being stacked on top of the first: both would be full-screen canvases at
        /// the same sorting order, and answering one would leave the other alive,
        /// raycasting, and waiting on a task that can never complete.
        /// </summary>
        private bool isFormShowing;

        /// <summary>
        /// Set this session when the consent flow decided the user does not need to
        /// be asked: GDPR does not apply to them, or the flow is disabled. Never
        /// persisted, so it is re-resolved on each launch until the user answers.
        /// </summary>
        private bool consentNotRequired;

        /// <summary>True while the consent form is on screen.</summary>
        public bool IsFormShowing => isFormShowing;

        /// <summary>Fired when the consent flow completes; bool = ads may be requested.</summary>
        public event Action<bool> OnConsentReady;

        /// <summary>Fired when the stored consent changes; bool = personalized ads granted.</summary>
        public event Action<bool> OnConsentChanged;

        public LocalConsentManager(AdConfiguration config)
        {
            this.config = config;
        }

        #region Queries

        /// <summary>
        /// True once the user has answered, AND that answer still applies to the
        /// current policy version. Bumping <see cref="AdConfiguration.ConsentPolicyVersion"/>
        /// makes an older answer stop counting, so the user is asked again instead of
        /// carrying consent forward to partners or terms they never saw.
        /// </summary>
        public bool HasStoredConsent
        {
            get
            {
                if (PlayerPrefs.GetInt(StoredKey, 0) != 1)
                {
                    return false;
                }

                return AnsweredPolicyVersion == config.ConsentPolicyVersion;
            }
        }

        /// <summary>The policy version the stored answer was given against; 0 when unanswered.</summary>
        public int AnsweredPolicyVersion => PlayerPrefs.GetInt(PolicyVersionKey, 0);

        /// <summary>
        /// When the stored answer was given, UTC. Null when unanswered. Recorded so
        /// there is something to point at if the consent is ever questioned.
        /// </summary>
        public DateTime? AnsweredAtUtc
        {
            get
            {
                var raw = PlayerPrefs.GetString(AnsweredAtKey, "");
                if (string.IsNullOrEmpty(raw))
                {
                    return null;
                }

                DateTime parsed;
                if (DateTime.TryParse(raw, System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.AdjustToUniversal |
                        System.Globalization.DateTimeStyles.AssumeUniversal, out parsed))
                {
                    return parsed;
                }

                return null;
            }
        }

        /// <summary>
        /// Fallback only: whether the DEVICE is configured to a GDPR country. This is
        /// a settings value the user picks, not a location, so it is only used when
        /// no lookup URL is configured. Prefer the IP-resolved answer.
        /// </summary>
        public static bool IsGdprRegion()
        {
            try
            {
                var region = System.Globalization.RegionInfo.CurrentRegion;
                if (region == null)
                {
                    return true;
                }

                return GdprRegions.Contains(region.TwoLetterISORegionName.ToUpperInvariant());
            }
            catch (Exception e)
            {
                // Unknown region means ask, because asking is the safe direction.
                AdLog.Warn($"Could not read the device region ({e.Message}), asking for consent anyway.");
                return true;
            }
        }

        /// <summary>
        /// Resolve whether this user must be asked, from their IP, so the answer
        /// reflects where they actually are rather than what their device is set to.
        /// A device region setting is trivially changed and says nothing about
        /// location; an IP is what a certified CMP uses for the same decision.
        ///
        /// EVERY failure path returns true. Timeout, no network, blocked endpoint,
        /// unparseable reply: all of them show the form. Asking somebody who did not
        /// need it costs a tap; skipping somebody who did is the real exposure, so
        /// the uncertain case must never be the silent one.
        /// </summary>
        private async Task<bool> ShouldAskAsync()
        {
            if (!config.OnlyAskWhereGdprApplies)
            {
                return true;
            }

            if (string.IsNullOrEmpty(config.GeoLookupUrl))
            {
                bool deviceSaysGdpr = IsGdprRegion();
                AdLog.Info($"No geo lookup URL configured, falling back to the device region " +
                          $"(isGdprRegion={deviceSaysGdpr}).");
                return deviceSaysGdpr;
            }

            AdLog.Info($"Resolving GDPR applicability from {config.GeoLookupUrl} " +
                       $"(timeout {config.GeoLookupTimeoutSeconds}s).");
            var startedAt = Time.realtimeSinceStartup;
            var country = await LookupCountryAsync();
            AdLog.Info($"Geo lookup took {(Time.realtimeSinceStartup - startedAt):F2}s.");
            if (string.IsNullOrEmpty(country))
            {
                AdLog.Info("Could not resolve the country, showing the consent form anyway.");
                return true;
            }

            bool required = GdprRegions.Contains(country);
            AdLog.Info($"Resolved country {country}, GDPR applies={required}.");
            return required;
        }

        /// <summary>
        /// Fetch the two-letter country code, or null on any problem. Never throws.
        /// </summary>
        private async Task<string> LookupCountryAsync()
        {
            UnityWebRequest request = null;
            try
            {
                request = UnityWebRequest.Get(config.GeoLookupUrl);
                request.timeout = Mathf.Max(1, config.GeoLookupTimeoutSeconds);

                var completion = new TaskCompletionSource<bool>();
                var operation = request.SendWebRequest();
                operation.completed += _ => completion.TrySetResult(true);
                await completion.Task;

                if (request.result != UnityWebRequest.Result.Success)
                {
                    AdLog.Warn($"Geo lookup failed ({request.error}).");
                    return null;
                }

                return ParseCountry(request.downloadHandler.text);
            }
            catch (Exception e)
            {
                AdLog.Warn($"Geo lookup threw ({e.Message}).");
                return null;
            }
            finally
            {
                if (request != null)
                {
                    request.Dispose();
                }
            }
        }

        /// <summary>Pull "loc=XX" out of a Cloudflare-style trace response.</summary>
        internal static string ParseCountry(string body)
        {
            if (string.IsNullOrEmpty(body))
            {
                return null;
            }

            foreach (var line in body.Split('\n'))
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith("loc=", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var value = trimmed.Substring(4).Trim().ToUpperInvariant();
                if (value.Length == 2)
                {
                    return value;
                }

                return null;
            }

            return null;
        }

        /// <summary>The user's personalized-ads decision. False until answered.</summary>
        public bool AdsGranted => PlayerPrefs.GetInt(AdsKey, 0) == 1;

        /// <summary>The user's stored analytics decision. False until answered; read <see cref="AnalyticsAllowed"/> to gate collection.</summary>
        public bool AnalyticsGranted => PlayerPrefs.GetInt(AnalyticsKey, 0) == 1;

        /// <summary>True when this session's consent flow found that the user does not need to be asked.</summary>
        public bool ConsentNotRequired => consentNotRequired;

        /// <summary>
        /// Whether analytics may collect. A stored answer always wins, including one
        /// given later through the privacy options where GDPR does not apply. Without
        /// one, analytics is on only where consent is not required; an unresolved
        /// region or an unanswered form keeps it off. A refusal given against an older
        /// policy version still counts there, so a policy bump never turns it back on.
        /// </summary>
        public bool AnalyticsAllowed
        {
            get
            {
                if (HasStoredConsent) return AnalyticsGranted;
                if (!consentNotRequired) return false;
                return PlayerPrefs.GetInt(StoredKey, 0) != 1 || AnalyticsGranted;
            }
        }

        /// <summary>Ads can always be requested; consent gates personalization, not serving.</summary>
        public bool CanUserRequestAds() => true;

        /// <summary>
        /// Available so users can change consent at any time, except while the form
        /// is already open. Leaving it live during the form is what lets a double
        /// tap stack a second copy.
        /// </summary>
        public bool ShouldShowPrivacyOptionsButton() => !isFormShowing;

        /// <summary>"Personalized" | "NonPersonalized" | "Unknown". Mirrors <see cref="ConsentManager"/>.</summary>
        public string GetConsentType()
        {
            if (!HasStoredConsent)
            {
                return "Unknown";
            }

            if (AdsGranted)
            {
                return "Personalized";
            }

            return "NonPersonalized";
        }

        /// <summary>Human-readable dump for the on-device debug panel.</summary>
        public string GetConsentDebugSnapshot()
        {
            var sb = new StringBuilder();
            sb.AppendLine("- Consent (local) -");
            sb.AppendLine($"  Answered: {HasStoredConsent}");
            sb.AppendLine($"  Consent type: {GetConsentType()}");
            sb.AppendLine($"  Personalized ads: {AdsGranted}");
            sb.AppendLine($"  Analytics: {AnalyticsGranted} (allowed now: {AnalyticsAllowed}, consent not required: {consentNotRequired})");
            sb.AppendLine($"  Answered at (UTC): {AnsweredAtUtc}");
            sb.AppendLine($"  Policy version: answered={AnsweredPolicyVersion} current={config.ConsentPolicyVersion}");
            sb.AppendLine($"  Region-limited asking: {config.OnlyAskWhereGdprApplies} (device region says GDPR: {IsGdprRegion()}, resolved by IP at first ask)");
            return sb.ToString();
        }

        #endregion

        #region Flow

        /// <summary>
        /// Show the form when the user has not answered yet, then return. Call
        /// BEFORE LevelPlay init so the decision is in place when the flags are
        /// applied. No-op (consent assumed) when the consent flow is disabled.
        /// </summary>
        public async Task<bool> InitializeConsentAsync()
        {
            AdLog.Info($"Local consent starting: showDialog={config.ShowConsentDialog} " +
                       $"policyVersion={config.ConsentPolicyVersion} regionLimited={config.OnlyAskWhereGdprApplies} " +
                       $"defaultTicked={config.DefaultChoicesTicked} stored={HasStoredConsent}");

            if (!config.ShowConsentDialog)
            {
                AdLog.Info("Consent flow disabled in configuration, skipping local consent form.");
                consentNotRequired = true;
                OnConsentReady?.Invoke(true);
                return true;
            }

            if (HasStoredConsent)
            {
                AdLog.Info($"Local consent already stored: ads={AdsGranted} analytics={AnalyticsGranted} " +
                          $"answeredAtUtc={AnsweredAtUtc} policyVersion={AnsweredPolicyVersion}");
                OnConsentReady?.Invoke(CanUserRequestAds());
                return true;
            }

            if (PlayerPrefs.GetInt(StoredKey, 0) == 1)
            {
                AdLog.Info($"Stored consent was given against policy version {AnsweredPolicyVersion} " +
                          $"but the current version is {config.ConsentPolicyVersion}, so the user will be asked again.");
            }

            if (!await ShouldAskAsync())
            {
                // Nothing is stored, so nothing is pushed to the networks either: the
                // GDPR flag is left untouched rather than set either way, which lets
                // each network apply its own regional default.
                AdLog.Info("GDPR does not apply to this user, so no consent prompt is shown " +
                          "and no GDPR signal is sent. Analytics defaults to on until the user chooses otherwise.");
                consentNotRequired = true;
                OnConsentReady?.Invoke(CanUserRequestAds());
                return true;
            }

            await ShowFormAsync();

            // Deliberately no OnConsentChanged here. This is the FIRST answer, not a
            // change to an existing one, and the caller applies the flags straight
            // after this returns. Raising it would make the flags be applied twice.
            // OnConsentReady is the signal for "the initial answer is in".
            OnConsentReady?.Invoke(CanUserRequestAds());
            return true;
        }

        /// <summary>
        /// Re-open the form so the user can change their choice. Raises
        /// <see cref="OnConsentChanged"/> once they have answered, so the new
        /// decision is pushed to the networks. No-op while a form is already open.
        /// </summary>
        public void ShowPrivacyOptionsForm()
        {
            if (isFormShowing)
            {
                AdLog.Info("Consent form already open, ignoring the repeat request.");
                return;
            }

            _ = ShowPrivacyOptionsAsync();
        }

        private async Task ShowPrivacyOptionsAsync()
        {
            // Fire-and-forget: swallow nothing silently, or a throw here leaves the
            // button looking dead with no trace in the log.
            try
            {
                await ShowFormAsync();
                AdLog.Info($"Privacy options closed, re-pushing consent: ads={AdsGranted} analytics={AnalyticsGranted}");
                OnConsentChanged?.Invoke(AdsGranted);
            }
            catch (Exception e)
            {
                AdLog.Error($"Failed to show the privacy options form: {e}");
            }
        }

        /// <summary>TESTING ONLY: clear the stored answer so the form shows again next launch.</summary>
        public void ResetConsentForTesting()
        {
            PlayerPrefs.DeleteKey(StoredKey);
            PlayerPrefs.DeleteKey(AdsKey);
            PlayerPrefs.DeleteKey(AnalyticsKey);
            PlayerPrefs.DeleteKey(AnsweredAtKey);
            PlayerPrefs.DeleteKey(PolicyVersionKey);
            PlayerPrefs.Save();
            AdLog.Info("Cleared stored local consent.");
        }

        private async Task ShowFormAsync()
        {
            if (isFormShowing)
            {
                AdLog.Warn("A consent form is already open, refusing to stack another.");
                return;
            }

            AdLog.Info($"Loading the consent form prefab from Resources/{FormResourcePath}.");
            var prefab = Resources.Load<LocalConsentForm>(FormResourcePath);
            if (prefab == null)
            {
                AdLog.Error($"Local consent form prefab '{FormResourcePath}' not found in Resources, " +
                               "continuing without a prompt.");
                return;
            }

            isFormShowing = true;
            LocalConsentForm instance = null;
            try
            {
                instance = UnityEngine.Object.Instantiate(prefab);
                UnityEngine.Object.DontDestroyOnLoad(instance.gameObject);
                AdLog.Info("Consent form instantiated and marked DontDestroyOnLoad.");

                // A form with no buttons wired can never be answered, so waiting on it
                // would block initialization forever with nothing in the log and no
                // ads for the whole session. Refuse to wait, and say why.
                if (!instance.IsAnswerable)
                {
                    AdLog.Error("Consent form prefab has neither Accept nor Decline wired, " +
                                   "so it can never be answered. Continuing without a prompt.");
                    return;
                }

                // Asking someone to consent to text they cannot read is worse than not
                // asking at all, and it would not be valid consent either.
                if (!instance.HasReadableText)
                {
                    AdLog.Error("Consent form labels cannot render because TextMeshPro is not " +
                                   "in this project. Import it (Window > TextMeshPro > Import TMP Essential " +
                                   "Resources) to use the local consent form. Continuing without a prompt.");
                    return;
                }

                // Re-opening shows what they actually chose. Only a genuine first ask
                // uses the configured default, so changing that default never rewrites
                // a decision the user already made.
                bool adsSeed = AdsGranted;
                bool analyticsSeed = AnalyticsGranted;
                if (PlayerPrefs.GetInt(StoredKey, 0) != 1)
                {
                    adsSeed = config.DefaultChoicesTicked;
                    analyticsSeed = config.DefaultChoicesTicked;
                }

                var completion = new TaskCompletionSource<bool>();
                instance.Present(adsSeed, analyticsSeed, config.PrivacyPolicyUrl, (ads, analytics) =>
                {
                    Store(ads, analytics);
                    completion.TrySetResult(true);
                });

                AdLog.Info("Waiting for the user to answer the consent form.");
                await completion.Task;
                AdLog.Info("Consent form answered.");
            }
            finally
            {
                // Clear the guard even if something threw, so the form can never be
                // permanently locked out.
                isFormShowing = false;
                AdLog.Info("Consent form closed, guard released.");
                if (instance != null)
                {
                    UnityEngine.Object.Destroy(instance.gameObject);
                }
            }
        }

        private void Store(bool ads, bool analytics)
        {
            int adsValue = 0;
            if (ads)
            {
                adsValue = 1;
            }

            int analyticsValue = 0;
            if (analytics)
            {
                analyticsValue = 1;
            }

            var answeredAt = DateTime.UtcNow.ToString("o", System.Globalization.CultureInfo.InvariantCulture);

            PlayerPrefs.SetInt(StoredKey, 1);
            PlayerPrefs.SetInt(AdsKey, adsValue);
            PlayerPrefs.SetInt(AnalyticsKey, analyticsValue);
            PlayerPrefs.SetString(AnsweredAtKey, answeredAt);
            PlayerPrefs.SetInt(PolicyVersionKey, config.ConsentPolicyVersion);
            PlayerPrefs.Save();

            AdLog.Info($"Local consent stored: ads={ads} analytics={analytics} " +
                      $"answeredAtUtc={answeredAt} policyVersion={config.ConsentPolicyVersion}");
        }

        #endregion
    }
}
