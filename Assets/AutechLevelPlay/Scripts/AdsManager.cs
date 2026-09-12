#if LEVELPLAY_INSTALLED
using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.LevelPlay;
using LevelPlaySdk = Unity.Services.LevelPlay.LevelPlay;

namespace Autech.LevelPlay
{
    /// <summary>
    /// Settings snapshot applied via <see cref="AdsManager.ApplyConfiguration"/>.
    /// Mirrors the Autech AdMob package pattern (AdsManagerSettings struct).
    /// </summary>
    public struct AdsManagerSettings
    {
        public bool AdsEnabled;
        public bool RemoveAds;
        public AdTestMode TestMode;
        public bool AutoLaunchTestSuite;
        public bool UseAdaptiveBanners;
        public BannerSize PreferredBannerSize;
        public BannerPosition BannerPosition;
        public bool ShowConsentDialog;
        public bool UseLocalConsent;
        public bool DefaultChoicesTicked;
        public int ConsentPolicyVersion;
        public bool OnlyAskWhereGdprApplies;
        public string GeoLookupUrl;
        public int GeoLookupTimeoutSeconds;
        public string CmpPCode;
        public bool CmpShowIdfaPopup;
        public bool RequestAttAuthorization;
        public bool CcpaOptOut;
        public bool TagForChildDirectedTreatment;
        public string PrivacyPolicyUrl;
        public string AndroidAppKey;
        public string IosAppKey;
        public string AndroidBannerAdUnitId;
        public string AndroidInterstitialAdUnitId;
        public string AndroidRewardedAdUnitId;
        public string IosBannerAdUnitId;
        public string IosInterstitialAdUnitId;
        public string IosRewardedAdUnitId;
    }

    /// <summary>
    /// Central LevelPlay (Unity Ads) ads orchestrator. Drop-in successor to
    /// Autech.Admob.AdsManager: same singleton access, same Show*/Is*Ready
    /// call shapes, same RemoveAds events and persistence.
    /// Initialization order (compliance-critical): consent flags → iOS ATT
    /// prompt → LevelPlay.Init → ad loading.
    /// </summary>
    public class AdsManager : MonoBehaviour
    {
        private static AdsManager instance;
        private static readonly object instanceLock = new object();

        /// <summary>Singleton instance; auto-creates a persistent GameObject when missing.</summary>
        public static AdsManager Instance
        {
            get
            {
                if (instance != null) return instance;

                lock (instanceLock)
                {
                    if (instance == null)
                    {
                        var existing = FindAnyObjectByType<AdsManager>();
                        if (existing != null)
                        {
                            instance = existing;
                        }
                        else
                        {
                            var go = new GameObject("AdsManager (Autech.LevelPlay)");
                            instance = go.AddComponent<AdsManager>();
                        }
                    }
                }

                return instance;
            }
        }

        /// <summary>Fired when the RemoveAds state changes; bool = new state.</summary>
        public static Action<bool> OnRemoveAdsChanged;

        /// <summary>Fired when the RemoveAds state finishes loading from storage.</summary>
        public static Action<bool> OnRemoveAdsLoadedFromStorage;

        private readonly AdConfiguration config = new AdConfiguration();
        private AdPersistenceManager persistenceManager;
        private ConsentManager consentManager;
        private LocalConsentManager localConsentManager;
        private MediationConsentManager mediationConsentManager;

        private BannerAdController bannerController;
        private InterstitialAdController interstitialController;
        private RewardedAdController rewardedController;

        private bool isInitialized;
        private bool isInitializing;
        private bool isShowingAd;

        public bool IsInitialized => isInitialized;
        public bool IsShowingAd => isShowingAd;

        /// <summary>True when ads run in test mode for this build (Development build / Editor under Auto, or forced On).</summary>
        public bool IsTestMode => config.IsTestModeActive;

        /// <summary>
        /// InMobi CMP component for advanced queries (TC string, CCPA form).
        /// NOTE: this is a direct escape hatch to the CMP and does NOT respect
        /// <c>useLocalConsent</c>. Calling <c>Consent.ShowPrivacyOptionsForm()</c> or
        /// <c>Consent.ShowCcpaForm()</c> reaches InMobi even on the local path. Use
        /// <see cref="ShowPrivacyOptionsForm"/> on this class for the routed call.
        /// </summary>
        public ConsentManager Consent => consentManager;

        /// <summary>Local consent component; only drives the flow while UseLocalConsent is on.</summary>
        public LocalConsentManager LocalConsent => localConsentManager;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            persistenceManager = new AdPersistenceManager();
            persistenceManager.OnRemoveAdsLoadedFromStorage += loaded =>
            {
                config.RemoveAds = loaded;
                OnRemoveAdsLoadedFromStorage?.Invoke(loaded);
            };

            consentManager = new ConsentManager(config);
            localConsentManager = new LocalConsentManager(config);
            mediationConsentManager = new MediationConsentManager(config, consentManager, localConsentManager);

            // Changing the choice in the local form re-pushes the flags to the networks.
            localConsentManager.OnConsentChanged += _ => mediationConsentManager.Apply();
        }

        #region Initialization

        /// <summary>Apply settings; call before <see cref="InitializeAsync"/>.</summary>
        public void ApplyConfiguration(AdsManagerSettings settings)
        {
            config.AdsEnabled = settings.AdsEnabled;
            config.RemoveAds = settings.RemoveAds;
            config.TestMode = settings.TestMode;
            config.AutoLaunchTestSuite = settings.AutoLaunchTestSuite;
            config.UseAdaptiveBanners = settings.UseAdaptiveBanners;
            config.PreferredBannerSize = settings.PreferredBannerSize;
            config.BannerPosition = settings.BannerPosition;
            config.ShowConsentDialog = settings.ShowConsentDialog;
            config.UseLocalConsent = settings.UseLocalConsent;
            config.OnlyAskWhereGdprApplies = settings.OnlyAskWhereGdprApplies;
            config.GeoLookupUrl = settings.GeoLookupUrl;
            if (settings.GeoLookupTimeoutSeconds > 0)
            {
                config.GeoLookupTimeoutSeconds = settings.GeoLookupTimeoutSeconds;
            }
            config.DefaultChoicesTicked = settings.DefaultChoicesTicked;
            if (settings.ConsentPolicyVersion > 0)
            {
                config.ConsentPolicyVersion = settings.ConsentPolicyVersion;
            }
            config.CmpPCode = settings.CmpPCode;
            config.CmpShowIdfaPopup = settings.CmpShowIdfaPopup;
            config.RequestAttAuthorization = settings.RequestAttAuthorization;
            config.CcpaOptOut = settings.CcpaOptOut;
            config.TagForChildDirectedTreatment = settings.TagForChildDirectedTreatment;

            if (!string.IsNullOrEmpty(settings.PrivacyPolicyUrl))
            {
                config.PrivacyPolicyUrl = settings.PrivacyPolicyUrl;
            }

            config.AndroidAppKey = settings.AndroidAppKey;
            config.IosAppKey = settings.IosAppKey;
            config.AndroidBannerAdUnitId = settings.AndroidBannerAdUnitId;
            config.AndroidInterstitialAdUnitId = settings.AndroidInterstitialAdUnitId;
            config.AndroidRewardedAdUnitId = settings.AndroidRewardedAdUnitId;
            config.IosBannerAdUnitId = settings.IosBannerAdUnitId;
            config.IosInterstitialAdUnitId = settings.IosInterstitialAdUnitId;
            config.IosRewardedAdUnitId = settings.IosRewardedAdUnitId;
        }

        /// <summary>
        /// Full startup flow: stored RemoveAds → consent dialog + privacy flags →
        /// iOS ATT prompt → LevelPlay init → ad unit loading → optional test suite.
        /// Safe to await from a fire-and-forget context.
        /// </summary>
        public async Task InitializeAsync()
        {
            if (isInitialized || isInitializing)
            {
                Debug.Log("[Autech.LevelPlay] InitializeAsync skipped (already initialized/initializing).");
                return;
            }

            isInitializing = true;

            try
            {
                if (!config.AdsEnabled)
                {
                    Debug.Log("[Autech.LevelPlay] Ads disabled by configuration — skipping consent, ATT, and SDK init.");
                    return;
                }

#if UNITY_IOS && !UNITY_EDITOR
                // The old AdMob/UMP build crashed at launch on Apple silicon
                // Macs; LevelPlay is likewise unsupported there. An iOS build
                // running as an "iPad app on Mac" always runs ad-free.
                if (UnityEngine.iOS.Device.iosAppOnMac)
                {
                    Debug.Log("[Autech.LevelPlay] iOS app running on Mac — ads disabled (LevelPlay supports iOS/Android only).");
                    return;
                }
#endif

                if (persistenceManager.HasRemoveAdsDataInStorage())
                {
                    config.RemoveAds = persistenceManager.LoadRemoveAdsStatus();
                }

                // 1. Consent BEFORE init: LevelPlay wants CCPA/COPPA flags pre-init,
                //    and GDPR consent must exist before any personalized request.
                if (config.UseLocalConsent)
                {
                    // The adapters read IABTCF_* straight out of native storage. A TC
                    // string left behind by a previous InMobi build would keep saying
                    // "consented" and could override a user who declines in the local
                    // form, silently. Clear it before collecting our own decision.
                    consentManager.ClearTcfStorage();
                    await localConsentManager.InitializeConsentAsync();
                }
                else
                {
                    await consentManager.InitializeConsentAsync();
                }

                // InMobi path: GDPR flows via the IAB TCF string and this applies only
                // the explicit CCPA/COPPA flags. Local path: this also hands the GDPR
                // decision to LevelPlay, which forwards it to every mediated adapter.
                mediationConsentManager.Apply();

                // 2. ATT BEFORE init: Unity requires the ATT prompt before
                //    initializing any SDK that may access the IDFA, and step 3 below
                //    reads the resolved status, so this must not be skipped.
                //
                //    Await whenever ANY trigger is configured:
                //      - useLocalConsent: the CMP never starts, so the app owns the
                //        prompt. Without this the status stays NotDetermined and the
                //        IDFA is all-zeros for the life of the app.
                //      - cmpShowIdfaPopup: the CMP raises the prompt, but the consent
                //        flow returns as soon as the TC string lands, which happens
                //        BEFORE the user answers ATT. Awaiting here waits out that
                //        sheet so step 3 reads a settled value instead of a pending one.
                //      - requestAttAuthorization: the explicit app-side prompt.
                //    Who raises the prompt decides which call is used. Asking for one
                //    on top of the CMP's would double-request, and worse, would raise
                //    a prompt in projects where the CMP silently never started (no
                //    p-code, plugin missing, dialog disabled) and therefore never
                //    showed one before. DidStartWithIdfaPrompt reports whether the CMP
                //    genuinely took ownership.
                bool attByApp = config.RequestAttAuthorization || config.UseLocalConsent;
                bool attByCmp = !config.UseLocalConsent
                                && config.CmpShowIdfaPopup
                                && consentManager.DidStartWithIdfaPrompt;

                if (attByApp)
                {
                    await AttManager.RequestAuthorizationAsync();
                }
                else if (attByCmp)
                {
                    await AttManager.WaitForResolutionAsync();
                }

                // 3. Meta Audience Network requires its advertiser-tracking flag
                //    BEFORE LevelPlay init, and no adapter derives it from the ATT
                //    status for you. Safe no-op when Meta is not in the build.
                MetaAudienceNetwork.ApplyAdvertiserTracking(AttManager.IsAuthorized);

                if (!config.HasAppKey)
                {
                    Debug.LogError("[Autech.LevelPlay] No LevelPlay app key configured for this platform — init aborted.");
                    return;
                }

                // 4. Test mode (auto-on in Development builds) must be flagged
                //    before Init: this enables the integration test suite. Test
                //    ads at your real trigger points additionally require this
                //    device to be registered as a test device — the advertising
                //    ID for that is logged below after a successful init.
                if (config.IsTestModeActive)
                {
                    LevelPlaySdk.SetMetaData("is_test_suite", "enable");
                    LogTestModeBanner();
                }

                // 5. Initialize the SDK and await the result event.
                var initCompletion = new TaskCompletionSource<bool>();

                Action<LevelPlayConfiguration> onSuccess = null;
                Action<LevelPlayInitError> onFailure = null;

                onSuccess = configuration =>
                {
                    LevelPlaySdk.OnInitSuccess -= onSuccess;
                    LevelPlaySdk.OnInitFailed -= onFailure;
                    initCompletion.TrySetResult(true);
                };
                onFailure = error =>
                {
                    LevelPlaySdk.OnInitSuccess -= onSuccess;
                    LevelPlaySdk.OnInitFailed -= onFailure;
                    Debug.LogError($"[Autech.LevelPlay] LevelPlay init failed: {error}");
                    initCompletion.TrySetResult(false);
                };

                LevelPlaySdk.OnInitSuccess += onSuccess;
                LevelPlaySdk.OnInitFailed += onFailure;
                LevelPlaySdk.Init(config.AppKey);

                var initOk = await initCompletion.Task;
                if (!initOk) return;

                // 5. Ad objects may only be created after successful init.
                CreateControllersAndLoad();

                isInitialized = true;
                Debug.Log("[Autech.LevelPlay] Initialized.");

                if (config.IsTestModeActive)
                {
                    LogTestDeviceAdvertisingId();

                    if (config.AutoLaunchTestSuite)
                    {
                        LaunchTestSuite();
                    }
                    else
                    {
                        Debug.Log("[Autech.LevelPlay] Test mode ACTIVE — integration test suite is enabled but not auto-launched. " +
                                  "Call AdsManager.Instance.LaunchTestSuite() (or the VerifyLevelPlay 'Launch Test Suite' context menu) to open it.");
                    }
                }
            }
            finally
            {
                isInitializing = false;
            }
        }

        private void CreateControllersAndLoad()
        {
            if (!string.IsNullOrEmpty(config.RewardedAdUnitId))
            {
                rewardedController = new RewardedAdController(config.RewardedAdUnitId);
                rewardedController.LoadAd();
            }

            if (!string.IsNullOrEmpty(config.InterstitialAdUnitId))
            {
                interstitialController = new InterstitialAdController(config.InterstitialAdUnitId);
                interstitialController.LoadAd();
            }

            if (!string.IsNullOrEmpty(config.BannerAdUnitId))
            {
                bannerController = new BannerAdController(config);
                bannerController.LoadBanner();
            }
        }

        #endregion

        #region Banner Ads

        public void LoadBanner()
        {
            bannerController?.LoadBanner();
        }

        public void ShowBanner(bool show)
        {
            if (show && config.RemoveAds)
            {
                Debug.Log("[Autech.LevelPlay] Banner suppressed (RemoveAds active).");
                return;
            }

            bannerController?.ShowBanner(show);
        }

        /// <summary>Alias kept for AdMob-package parity (used by init wrappers).</summary>
        public void SetInitialBannerVisibility(bool show) => ShowBanner(show);

        public void SetBannerPosition(BannerPosition position)
        {
            bannerController?.SetBannerPosition(position);
        }

        public bool IsBannerLoaded() => bannerController?.IsBannerLoaded ?? false;

        public bool IsBannerVisible() => bannerController?.IsBannerVisible ?? false;

        public Vector2 GetBannerSize() => bannerController?.GetBannerSize() ?? Vector2.zero;

        #endregion

        #region Interstitial Ads

        public void ShowInterstitial(Action onSuccess, Action onFailure)
        {
            if (config.RemoveAds)
            {
                Debug.Log("[Autech.LevelPlay] Interstitial suppressed (RemoveAds active).");
                onSuccess?.Invoke();
                return;
            }

            if (interstitialController == null || !TrySetAdShowing())
            {
                onFailure?.Invoke();
                return;
            }

            interstitialController.Show(
                onSuccess: () => { ClearAdShowing(); onSuccess?.Invoke(); },
                onFailure: () => { ClearAdShowing(); onFailure?.Invoke(); });
        }

        public void ShowInterstitial(Action onSuccess) => ShowInterstitial(onSuccess, null);

        public void ShowInterstitial() => ShowInterstitial(null, null);

        public bool IsInterstitialReady() => interstitialController?.IsReady ?? false;

        #endregion

        #region Rewarded Ads

        /// <summary>
        /// Show a rewarded ad. onRewarded fires when the user earns the reward
        /// (LevelPlay may deliver it after close), onSuccess when the ad closes,
        /// onFailure when unavailable or display fails. Rewarded ads are
        /// user-initiated and therefore NOT gated by RemoveAds.
        /// </summary>
        public void ShowRewarded(Action<LevelPlayReward> onRewarded, Action onSuccess, Action onFailure)
        {
            if (rewardedController == null || !TrySetAdShowing())
            {
                onFailure?.Invoke();
                return;
            }

            rewardedController.Show(
                onRewarded: onRewarded,
                onSuccess: () => { ClearAdShowing(); onSuccess?.Invoke(); },
                onFailure: () => { ClearAdShowing(); onFailure?.Invoke(); });
        }

        public void ShowRewarded(Action onSuccess, Action onFailure) => ShowRewarded(null, onSuccess, onFailure);

        public void ShowRewarded(Action onSuccess) => ShowRewarded(null, onSuccess, null);

        public void ShowRewarded() => ShowRewarded(null, null, null);

        public bool IsRewardedReady() => rewardedController?.IsReady ?? false;

        #endregion

        #region Remove Ads

        /// <summary>RemoveAds state; setting persists and fires <see cref="OnRemoveAdsChanged"/>.</summary>
        public bool RemoveAds
        {
            get => config.RemoveAds;
            set
            {
                if (config.RemoveAds == value) return;

                config.RemoveAds = value;
                try
                {
                    persistenceManager.SaveRemoveAdsStatus(value);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Autech.LevelPlay] Failed to persist RemoveAds: {e.Message}");
                }

                if (value)
                {
                    ShowBanner(false);
                }

                OnRemoveAdsChanged?.Invoke(value);
            }
        }

        public void ForceLoadFromStorage()
        {
            config.RemoveAds = persistenceManager.LoadRemoveAdsStatus();
        }

        public void ForceSaveToStorage()
        {
            persistenceManager.SaveRemoveAdsStatus(config.RemoveAds);
        }

        public void ClearRemoveAdsData()
        {
            persistenceManager.ClearRemoveAdsData();
            config.RemoveAds = false;
        }

        public bool HasRemoveAdsDataInStorage() => persistenceManager.HasRemoveAdsDataInStorage();

        #endregion

        #region Consent

        /// <summary>Re-open the consent dialog so the user can change their choice. No-op while ads are disabled.</summary>
        public void ShowPrivacyOptionsForm()
        {
            if (!config.AdsEnabled) return;
            if (config.UseLocalConsent)
            {
                localConsentManager.ShowPrivacyOptionsForm();
                return;
            }
            consentManager.ShowPrivacyOptionsForm();
        }

        /// <summary>False while ads are disabled — no consent to manage, so settings hide the button.</summary>
        public bool ShouldShowPrivacyOptionsButton()
        {
            if (!config.AdsEnabled) return false;
            if (config.UseLocalConsent) return localConsentManager.ShouldShowPrivacyOptionsButton();
            return consentManager.ShouldShowPrivacyOptionsButton();
        }

        public bool CanUserRequestAds()
        {
            if (config.UseLocalConsent) return localConsentManager.CanUserRequestAds();
            return consentManager.CanUserRequestAds();
        }

        /// <summary>"Personalized" | "NonPersonalized" | "Unknown".</summary>
        public string GetConsentType()
        {
            if (config.UseLocalConsent) return localConsentManager.GetConsentType();
            return consentManager.GetConsentType();
        }

        /// <summary>
        /// TESTING ONLY: clear stored consent for BOTH paths. Clearing only the
        /// active one leaves a tester who flips <c>useLocalConsent</c> looking at a
        /// stale answer from the other path.
        /// </summary>
        public void ResetConsentForTesting()
        {
            localConsentManager.ResetConsentForTesting();
            consentManager.ResetConsentForTesting();
        }

        /// <summary>CCPA/US-state "do not sell or share" opt-out toggle. (For the full CMP US-privacy UI use <c>Consent.ShowCcpaForm()</c>.)</summary>
        public void SetCcpaOptOut(bool optedOut)
        {
            config.CcpaOptOut = optedOut;
            LevelPlayPrivacySettings.SetCCPA(optedOut);
        }

        /// <summary>
        /// One-shot snapshot of every privacy/consent/ATT value currently in effect —
        /// for the demo debug panel, so you can verify on device that consent, ATT,
        /// and the IAB TCF data were actually grabbed by the SDK. The advertising ID
        /// is fetched separately via <see cref="RequestAdvertisingId"/> (it's async).
        /// </summary>
        public string GetPrivacyDebugSnapshot()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("===== PRIVACY / CONSENT SNAPSHOT =====");
            sb.AppendLine($"Ads enabled: {config.AdsEnabled} | RemoveAds: {config.RemoveAds} | Test mode: {config.IsTestModeActive}");
            // Order matters: on the local-consent path the CMP never starts, so
            // cmpShowIdfaPopup triggers nothing and the app owns the prompt.
            string attTrigger = "none";
            if (config.UseLocalConsent || config.RequestAttAuthorization)
            {
                attTrigger = "app AttManager";
            }
            else if (config.CmpShowIdfaPopup)
            {
                attTrigger = "InMobi CMP (shouldDisplayIDFA)";
            }
            sb.AppendLine($"ATT trigger: {attTrigger}");
            sb.AppendLine($"ATT status: {AttManager.Status} (authorized={AttManager.IsAuthorized})");
            sb.AppendLine($"CCPA opt-out: {config.CcpaOptOut} | COPPA: {config.TagForChildDirectedTreatment}");
            if (config.UseLocalConsent)
            {
                sb.AppendLine("Consent source: local form");
                sb.Append(localConsentManager.GetConsentDebugSnapshot());
            }
            else
            {
                sb.AppendLine("Consent source: InMobi CMP (IAB TCF)");
                sb.Append(consentManager.GetConsentDebugSnapshot());
            }
            sb.AppendLine($"Can request ads: {CanUserRequestAds()} | Privacy options available: {ShouldShowPrivacyOptionsButton()}");
            sb.Append("=====================================");
            return sb.ToString();
        }

        /// <summary>
        /// Async-fetch the device advertising ID (IDFA on iOS / GAID on Android) and
        /// deliver it to <paramref name="onResult"/>. For the debug panel. On iOS the
        /// IDFA is all-zeros until ATT is authorized.
        /// </summary>
        public void RequestAdvertisingId(Action<string> onResult)
        {
            try
            {
                bool requested = Application.RequestAdvertisingIdentifierAsync(
                    (string id, bool trackingEnabled, string error) =>
                    {
                        if (!string.IsNullOrEmpty(id))
                            onResult?.Invoke($"{id} (tracking enabled: {trackingEnabled})");
                        else
                            onResult?.Invoke($"unavailable ({error})");
                    });
                if (!requested)
                    onResult?.Invoke("not supported on this platform / Editor");
            }
            catch (Exception e)
            {
                onResult?.Invoke($"error: {e.Message}");
            }
        }

        #endregion

        #region Test Mode

        /// <summary>
        /// Launch the LevelPlay integration test suite (the in-app panel that
        /// verifies each network and loads/shows test ads on demand). No-op when
        /// test mode is OFF — the required <c>is_test_suite</c> metadata is only
        /// set before init while test mode is active.
        /// </summary>
        public void LaunchTestSuite()
        {
            if (!config.IsTestModeActive)
            {
                Debug.LogWarning("[Autech.LevelPlay] LaunchTestSuite ignored — test mode is OFF. " +
                                 "Set TestMode to AlwaysOn (or make a Development Build) so the test-suite metadata is enabled before init.");
                return;
            }

            Debug.Log("[Autech.LevelPlay] Launching LevelPlay integration test suite.");
            LevelPlaySdk.LaunchTestSuite();
        }

        // Loud, hard-to-miss console banner explaining why ads are in test mode
        // and how to get test ads at the game's own trigger points.
        private void LogTestModeBanner()
        {
            string reason = Debug.isDebugBuild ? "Development build / Editor" : "forced (TestMode = AlwaysOn)";
            Debug.Log(
                "\n========================= AUTECH LEVELPLAY: TEST MODE ACTIVE =========================\n" +
                $"Reason: {reason}. Integration test suite ENABLED. DO NOT ship a build in this state.\n" +
                "To get TEST ads at your real in-game trigger points (banner/interstitial/rewarded Show calls),\n" +
                "register THIS device as a test device in the LevelPlay dashboard:\n" +
                "  Dashboard > SDK Networks / Settings > Test Devices  →  add the advertising ID logged below.\n" +
                "LevelPlay has no separate 'test ad unit ids'; test-device registration is what makes your\n" +
                "production ad units serve safe test ads everywhere (including the sample scene).\n" +
                "=====================================================================================");
        }

        // Best-effort log of the device advertising ID (GAID/IDFA) so it can be
        // pasted into the dashboard test-device list. Falls back to pointing at
        // the test suite, whose header also shows the advertising ID.
        private void LogTestDeviceAdvertisingId()
        {
            try
            {
                bool requested = Application.RequestAdvertisingIdentifierAsync(
                    (string advertisingId, bool trackingEnabled, string error) =>
                    {
                        if (!string.IsNullOrEmpty(advertisingId))
                        {
                            Debug.Log($"[Autech.LevelPlay] *** TEST DEVICE ADVERTISING ID: {advertisingId} *** " +
                                      $"(tracking enabled: {trackingEnabled}). Add it to the LevelPlay dashboard test-device list.");
                        }
                        else
                        {
                            Debug.Log($"[Autech.LevelPlay] Advertising ID unavailable ({error}). " +
                                      "Launch the test suite — its header shows the advertising ID to register as a test device.");
                        }
                    });

                if (!requested)
                {
                    Debug.Log("[Autech.LevelPlay] Advertising ID lookup not supported on this platform/Editor. " +
                              "Launch the test suite (its header shows the advertising ID) to register this device.");
                }
            }
            catch (Exception e)
            {
                Debug.Log($"[Autech.LevelPlay] Advertising ID lookup failed ({e.Message}). " +
                          "Launch the test suite to read the advertising ID and register this device.");
            }
        }

        #endregion

        #region Debug

        public void LogDebugStatus()
        {
            config.LogConfiguration();
            Debug.Log($"[Autech.LevelPlay] init={isInitialized} showing={isShowingAd} " +
                      $"rewardedReady={IsRewardedReady()} interstitialReady={IsInterstitialReady()} " +
                      $"bannerVisible={IsBannerVisible()} consent={GetConsentType()} att={AttManager.Status}");
        }

        #endregion

        private bool TrySetAdShowing()
        {
            lock (instanceLock)
            {
                if (isShowingAd) return false;
                isShowingAd = true;
                return true;
            }
        }

        private void ClearAdShowing()
        {
            lock (instanceLock)
            {
                isShowingAd = false;
            }
        }
    }
}
#endif // LEVELPLAY_INSTALLED
