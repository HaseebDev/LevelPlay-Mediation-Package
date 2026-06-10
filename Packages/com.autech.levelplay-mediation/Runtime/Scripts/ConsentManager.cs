using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
#if LEVELPLAY_INSTALLED
using Unity.Services.LevelPlay;
#endif

namespace Autech.LevelPlay
{
    /// <summary>GDPR consent choice persisted across sessions.</summary>
    public enum ConsentState
    {
        Unknown = -1,
        Denied = 0,
        Granted = 1
    }

    /// <summary>
    /// Owns the user-consent lifecycle for LevelPlay / Unity Ads:
    /// - Shows a first-launch GDPR consent dialog (built-in, dependency-free UI).
    /// - Persists the choice and re-applies it every session BEFORE SDK init
    ///   (LevelPlay requires CCPA/COPPA flags before initialization, and
    ///   SetGDPRConsents replaces the whole map on every call).
    /// - Exposes a privacy-options entry point so users can change their choice
    ///   at any time (GDPR withdrawal right).
    /// Replaces the Google UMP flow used by the Autech AdMob package.
    /// </summary>
    public class ConsentManager
    {
        private const string GdprPrefKey = "Autech.LevelPlay.Consent.Gdpr";
        private const string CcpaPrefKey = "Autech.LevelPlay.Consent.CcpaOptOut";

        /// <summary>Network key LevelPlay expects for the Unity Ads bidder.</summary>
        private const string UnityAdsNetworkKey = "UnityAds";

        private readonly AdConfiguration config;

        /// <summary>Fired when the consent flow completes; bool = ads may be requested (always true: declining only disables personalization).</summary>
        public event Action<bool> OnConsentReady;

        /// <summary>Fired whenever the stored GDPR consent changes; bool = granted.</summary>
        public event Action<bool> OnConsentChanged;

        public ConsentManager(AdConfiguration config)
        {
            this.config = config;
        }

        #region State

        /// <summary>Stored GDPR consent choice.</summary>
        public ConsentState CurrentConsentState
        {
            get
            {
                var stored = PlayerPrefs.GetInt(GdprPrefKey, (int)ConsentState.Unknown);
                return (ConsentState)stored;
            }
        }

        /// <summary>Stored CCPA/US-state "do not sell or share" opt-out.</summary>
        public bool CcpaOptOut => PlayerPrefs.GetInt(CcpaPrefKey, config.CcpaOptOut ? 1 : 0) == 1;

        /// <summary>"Personalized" | "NonPersonalized" | "Unknown" — mirrors the AdMob package API.</summary>
        public string GetConsentType()
        {
            switch (CurrentConsentState)
            {
                case ConsentState.Granted: return "Personalized";
                case ConsentState.Denied: return "NonPersonalized";
                default: return "Unknown";
            }
        }

        /// <summary>
        /// Ads can always be requested: consent gates personalization, not serving.
        /// Unity serves contextual ads when consent is absent or denied.
        /// </summary>
        public bool CanUserRequestAds() => true;

        /// <summary>
        /// The privacy options entry point is always available so users can
        /// withdraw or change consent at any time (GDPR Art. 7(3)).
        /// </summary>
        public bool ShouldShowPrivacyOptionsButton() => true;

        #endregion

        #region Flow

        /// <summary>
        /// Run the consent flow: show the first-launch dialog when no choice is
        /// stored (and the dialog is enabled), then apply all privacy flags to
        /// the LevelPlay SDK. Call BEFORE LevelPlay init.
        /// </summary>
        public async Task<bool> InitializeConsentAsync()
        {
            if (config.ShowConsentDialog && CurrentConsentState == ConsentState.Unknown)
            {
                var granted = await ConsentDialog.ShowAsync(config.PrivacyPolicyUrl);
                StoreGdprConsent(granted);
            }

            ApplyConsentToSdk();
            var canRequestAds = CanUserRequestAds();
            OnConsentReady?.Invoke(canRequestAds);
            return canRequestAds;
        }

        /// <summary>
        /// Re-open the consent dialog so the user can change their choice.
        /// The new choice is persisted and re-applied to the SDK immediately.
        /// </summary>
        public async Task ShowPrivacyOptionsFormAsync()
        {
            var granted = await ConsentDialog.ShowAsync(config.PrivacyPolicyUrl, isPrivacyOptions: true);
            StoreGdprConsent(granted);
            ApplyConsentToSdk();
        }

        /// <summary>Fire-and-forget wrapper mirroring the AdMob package signature.</summary>
        public void ShowPrivacyOptionsForm()
        {
            _ = ShowPrivacyOptionsFormAsync();
        }

        /// <summary>
        /// Set the CCPA/US-state "do not sell or share" opt-out (true = opted out)
        /// and apply it to the SDK immediately. Wire this to a settings toggle.
        /// </summary>
        public void SetCcpaOptOut(bool optedOut)
        {
            PlayerPrefs.SetInt(CcpaPrefKey, optedOut ? 1 : 0);
            PlayerPrefs.Save();
#if LEVELPLAY_INSTALLED
            LevelPlayPrivacySettings.SetCCPA(optedOut);
#endif
            Debug.Log($"[Autech.LevelPlay] CCPA opt-out set: {optedOut}");
        }

        private void StoreGdprConsent(bool granted)
        {
            PlayerPrefs.SetInt(GdprPrefKey, granted ? 1 : 0);
            PlayerPrefs.Save();
            Debug.Log($"[Autech.LevelPlay] GDPR consent stored: {(granted ? "granted" : "denied")}");
            OnConsentChanged?.Invoke(granted);
        }

        /// <summary>
        /// Push all stored privacy flags into LevelPlay. SetGDPRConsents has
        /// replace-not-merge semantics, so the full network map is sent each time.
        /// </summary>
        public void ApplyConsentToSdk()
        {
#if LEVELPLAY_INSTALLED
            var state = CurrentConsentState;
            if (state != ConsentState.Unknown)
            {
                var consents = new Dictionary<string, bool>
                {
                    { UnityAdsNetworkKey, state == ConsentState.Granted }
                };
                LevelPlayPrivacySettings.SetGDPRConsents(consents);
            }

            LevelPlayPrivacySettings.SetCCPA(CcpaOptOut);
            LevelPlayPrivacySettings.SetCOPPA(config.TagForChildDirectedTreatment);

            Debug.Log($"[Autech.LevelPlay] Privacy applied: gdpr={state} ccpaOptOut={CcpaOptOut} coppa={config.TagForChildDirectedTreatment}");
#endif
        }

        /// <summary>TESTING ONLY: clear the stored consent so the dialog shows again.</summary>
        public void ResetConsentForTesting()
        {
            PlayerPrefs.DeleteKey(GdprPrefKey);
            PlayerPrefs.DeleteKey(CcpaPrefKey);
            PlayerPrefs.Save();
            Debug.Log("[Autech.LevelPlay] Stored consent cleared (testing).");
        }

        #endregion
    }
}
