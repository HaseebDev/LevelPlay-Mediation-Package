using UnityEngine;

namespace Autech.LevelPlay
{
    /// <summary>
    /// Banner anchor positions. Mirrors the Autech AdMob package enum so game
    /// code and serialized prefab values carry over unchanged.
    /// </summary>
    public enum BannerPosition
    {
        Top = 0,
        Bottom = 1,
        TopLeft = 2,
        TopRight = 3,
        BottomLeft = 4,
        BottomRight = 5,
        Center = 6
    }

    /// <summary>
    /// Banner sizes supported by LevelPlay. Adaptive is recommended.
    /// </summary>
    public enum BannerSize
    {
        Banner = 0,           // 320x50
        Large = 1,            // 320x90
        MediumRectangle = 2,  // 300x250
        Leaderboard = 3,      // 728x90 (tablets)
        Adaptive = 4          // Recommended: device-width adaptive
    }

    /// <summary>
    /// Global test-mode policy. Controls whether the SDK runs in test mode —
    /// integration test suite enabled and the device's advertising ID logged so
    /// it can be registered as a LevelPlay test device (the only way LevelPlay
    /// serves TEST ads at your real in-game trigger points, since LevelPlay has
    /// no separate "test ad unit ids").
    /// </summary>
    public enum AdTestMode
    {
        /// <summary>Test mode ON in Development builds and the Editor, OFF in production. (Recommended.)</summary>
        AutoDetectDevelopmentBuild = 0,
        /// <summary>Force test mode ON regardless of build type. NEVER ship.</summary>
        AlwaysOn = 1,
        /// <summary>Force test mode OFF even in Development builds (serve production ads).</summary>
        AlwaysOff = 2
    }

    /// <summary>
    /// Runtime ad configuration: LevelPlay app keys, ad unit ids, banner and
    /// consent settings. Owned by <see cref="AdsManager"/>; populated via
    /// <see cref="AdsManager.ApplyConfiguration"/>.
    /// </summary>
    public class AdConfiguration
    {
        // LevelPlay app keys (from the LevelPlay dashboard, per platform app)
        public string AndroidAppKey { get; set; } = "";
        public string IosAppKey { get; set; } = "";

        // Ad unit ids (from LevelPlay dashboard > Ad units)
        public string AndroidBannerAdUnitId { get; set; } = "";
        public string AndroidInterstitialAdUnitId { get; set; } = "";
        public string AndroidRewardedAdUnitId { get; set; } = "";
        public string IosBannerAdUnitId { get; set; } = "";
        public string IosInterstitialAdUnitId { get; set; } = "";
        public string IosRewardedAdUnitId { get; set; } = "";

        // General settings

        // Master switch. When false the SDK never initializes and no consent
        // dialog or ATT prompt is shown — for shipping ad-free interim builds.
        public bool AdsEnabled { get; set; } = true;

        public bool RemoveAds { get; set; } = false;

        // Test mode. Auto = ON in Development builds (and the Editor), OFF in
        // production. When active, the integration test suite is enabled and the
        // device's advertising ID is logged for test-device registration.
        public AdTestMode TestMode { get; set; } = AdTestMode.AutoDetectDevelopmentBuild;

        // When test mode is active, also auto-launch the integration test suite
        // panel right after init. Leave OFF to keep the game's own ad trigger
        // points usable; launch the suite on demand instead.
        public bool AutoLaunchTestSuite { get; set; } = false;

        /// <summary>
        /// True when ads should run in test mode for the current build:
        /// Development builds / Editor under Auto, or whenever forced On.
        /// </summary>
        public bool IsTestModeActive
        {
            get
            {
                switch (TestMode)
                {
                    case AdTestMode.AlwaysOn: return true;
                    case AdTestMode.AlwaysOff: return false;
                    default: return Debug.isDebugBuild;
                }
            }
        }

        // Banner settings
        public bool UseAdaptiveBanners { get; set; } = true;
        public BannerSize PreferredBannerSize { get; set; } = BannerSize.Adaptive;
        public BannerPosition BannerPosition { get; set; } = BannerPosition.Bottom;

        // Consent / privacy settings

        // Runs the consent flow on init when true. (Field name kept for
        // AdMob-package parity; it gates whichever consent path is selected.)
        public bool ShowConsentDialog { get; set; } = true;

        // Collect consent with the package's own local form instead of the InMobi
        // CMP. OFF by default, so existing projects keep the InMobi path untouched.
        // The local form is not an IAB TCF CMP and is not Google-certified; it
        // suits mediation stacks that do not require a certified CMP.
        public bool UseLocalConsent { get; set; } = false;

        // Local form only: how the two choices start on a first ask. TRUE pre-ticks
        // them, so the user opts OUT rather than in.
        //
        // Know what this costs. GDPR Recital 32 says silence, pre-ticked boxes and
        // inactivity do not constitute consent, and the CJEU confirmed it in Planet49
        // (C-673/17). In the EEA/UK a pre-ticked box therefore does not establish the
        // consent this form exists to collect. It raises opt-in rates everywhere else.
        // Set deliberately, and consider pairing it with OnlyAskWhereGdprApplies.
        public bool DefaultChoicesTicked { get; set; } = true;

        // Bump this when the privacy policy or the set of ad partners changes. A
        // stored answer from an older version stops counting as answered, so the
        // user is asked again rather than silently carrying consent forward to
        // something they never agreed to.
        public int ConsentPolicyVersion { get; set; } = 1;

        // Local path only: ask only where GDPR/UK GDPR applies, resolved from the
        // device's IP so the answer reflects where the user actually is rather than
        // what their device is configured to say. Checked once, on the first launch
        // that needs an answer; never again after they have answered.
        //
        // Every failure path asks. A timeout, no network, a blocked endpoint or an
        // unrecognised reply all fall through to showing the form, because asking
        // someone who did not need it costs a tap while skipping someone who did is
        // the actual exposure.
        public bool OnlyAskWhereGdprApplies { get; set; } = false;

        // Endpoint used for that lookup. Must return the two-letter country code in
        // a "loc=XX" line; Cloudflare's trace endpoint is the default because it
        // needs no account or API key and returns plain text. Point it at your own
        // backend if you would rather not depend on a third party, or clear it to
        // fall back to the device region.
        public string GeoLookupUrl { get; set; } = "https://www.cloudflare.com/cdn-cgi/trace";

        // How long to wait for that lookup before giving up and asking anyway. Kept
        // short because it sits in front of the consent form on first launch.
        public int GeoLookupTimeoutSeconds { get; set; } = 3;

        // InMobi CMP account p-code (from the InMobi CMP portal profile menu).
        // Without it, no consent UI is shown. Drop the leading "p-".
        public string CmpPCode { get; set; } = "";

        // iOS only: let the InMobi CMP show the ATT ("Allow tracking") popup as part
        // of its consent flow. This is now the package default — the CMP is the single
        // source of truth for ATT, so we do NOT also run a standalone app-side prompt.
        public bool CmpShowIdfaPopup { get; set; } = true;

        // Legacy app-controlled ATT via AttManager. OFF by default now that the CMP
        // owns the ATT trigger; turn it on only if you deliberately want the app to
        // present ATT itself instead of the CMP. Do not enable both.
        public bool RequestAttAuthorization { get; set; } = false;
        public bool CcpaOptOut { get; set; } = false;
        public bool TagForChildDirectedTreatment { get; set; } = false;
        public string PrivacyPolicyUrl { get; set; } = "https://autechsolutions.netlify.app/privacy-policy";

        /// <summary>LevelPlay app key for the current runtime platform.</summary>
        public string AppKey
        {
            get
            {
#if UNITY_IOS
                return IosAppKey;
#else
                return AndroidAppKey;
#endif
            }
        }

        /// <summary>Banner ad unit id for the current runtime platform.</summary>
        public string BannerAdUnitId
        {
            get
            {
#if UNITY_IOS
                return IosBannerAdUnitId;
#else
                return AndroidBannerAdUnitId;
#endif
            }
        }

        /// <summary>Interstitial ad unit id for the current runtime platform.</summary>
        public string InterstitialAdUnitId
        {
            get
            {
#if UNITY_IOS
                return IosInterstitialAdUnitId;
#else
                return AndroidInterstitialAdUnitId;
#endif
            }
        }

        /// <summary>Rewarded ad unit id for the current runtime platform.</summary>
        public string RewardedAdUnitId
        {
            get
            {
#if UNITY_IOS
                return IosRewardedAdUnitId;
#else
                return AndroidRewardedAdUnitId;
#endif
            }
        }

        /// <summary>True when the platform app key is present.</summary>
        public bool HasAppKey => !string.IsNullOrEmpty(AppKey);

        /// <summary>Log current configuration (keys truncated) for debugging.</summary>
        public void LogConfiguration()
        {
            Debug.Log($"[Autech.LevelPlay] AppKey={Truncate(AppKey)} banner={Truncate(BannerAdUnitId)} " +
                      $"interstitial={Truncate(InterstitialAdUnitId)} rewarded={Truncate(RewardedAdUnitId)} " +
                      $"removeAds={RemoveAds} testMode={TestMode}(active={IsTestModeActive}) adaptive={UseAdaptiveBanners} " +
                      $"position={BannerPosition} ccpaOptOut={CcpaOptOut} coppa={TagForChildDirectedTreatment}");
        }

        private static string Truncate(string value)
        {
            if (string.IsNullOrEmpty(value)) return "<empty>";
            return value.Length <= 6 ? value : value.Substring(0, 6) + "…";
        }
    }
}
