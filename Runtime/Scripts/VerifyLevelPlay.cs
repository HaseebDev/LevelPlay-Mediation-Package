#if LEVELPLAY_INSTALLED
using System.Threading.Tasks;
using UnityEngine;

namespace Autech.LevelPlay
{
    /// <summary>
    /// Scene bootstrap component: configure everything in the Inspector, drop
    /// the prefab into the first scene, ads initialize on Start. Successor to
    /// VerifyAdmob with the same role and a near-identical field layout.
    /// </summary>
    public class VerifyLevelPlay : MonoBehaviour
    {
        [Header("Master Switch")]
        [Tooltip("OFF = ship ad-free: no SDK init, no consent dialog, no ATT prompt.")]
        [SerializeField] private bool adsEnabled = true;

        [Header("LevelPlay App Keys")]
        [SerializeField] private string androidAppKey = "";
        [SerializeField] private string iosAppKey = "";

        [Header("Ad Unit IDs - Android")]
        [SerializeField] private string androidBannerAdUnitId = "";
        [SerializeField] private string androidInterstitialAdUnitId = "";
        [SerializeField] private string androidRewardedAdUnitId = "";

        [Header("Ad Unit IDs - iOS")]
        [SerializeField] private string iosBannerAdUnitId = "";
        [SerializeField] private string iosInterstitialAdUnitId = "";
        [SerializeField] private string iosRewardedAdUnitId = "";

        [Header("Ad Display Settings")]
        [SerializeField] private BannerPosition bannerPosition = BannerPosition.Bottom;
        [SerializeField] private bool showBannerOnStart = true;
        [SerializeField] private bool useAdaptiveBanners = true;
        [SerializeField] private BannerSize preferredBannerSize = BannerSize.Adaptive;

        [Header("Remove Ads")]
        [SerializeField] private bool removeAds = false;

        [Header("Testing")]
        [Tooltip("Global ad test mode. Auto = test mode ON in Development builds and the Editor, OFF in production builds (iOS & Android). " +
                 "While active the integration test suite is enabled and the device's advertising ID is logged so you can register it as a " +
                 "LevelPlay test device — that's what makes your real in-game ad trigger points serve safe TEST ads.")]
        [SerializeField] private AdTestMode testMode = AdTestMode.AutoDetectDevelopmentBuild;
        [Tooltip("When test mode is active, auto-launch the test suite panel right after init. Leave OFF to keep the game's own ad flows " +
                 "testable; launch the suite on demand via LaunchTestSuite() or the 'Launch Test Suite' context menu.")]
        [SerializeField] private bool autoLaunchTestSuite = false;

        [Header("Consent & Privacy (InMobi CMP)")]
        [Tooltip("Run the InMobi CMP consent flow on first launch (GDPR / IAB TCF).")]
        [SerializeField] private bool showConsentDialog = true;
        [Tooltip("InMobi CMP account p-code (CMP portal > profile menu; the leading 'p-' is optional). Required to show the consent UI.")]
        [SerializeField] private string cmpPCode = "";
        [Tooltip("iOS only (recommended ON): the InMobi CMP shows the ATT \"Allow tracking\" popup as part of its consent flow. " +
                 "This is the single source of truth for ATT — leave the app-side prompt below OFF.")]
        [SerializeField] private bool cmpShowIdfaPopup = true;
        [Tooltip("Legacy: have the app present the iOS ATT prompt itself (AttManager) before init, instead of the CMP. " +
                 "Leave OFF — the InMobi CMP owns ATT now. Do not enable together with the CMP popup above.")]
        [SerializeField] private bool requestAttAuthorization = false;
        [Tooltip("COPPA: flag all users as child-directed. Leave OFF for general-audience games.")]
        [SerializeField] private bool tagForChildDirectedTreatment = false;
        [SerializeField] private string privacyPolicyUrl = "https://autechsolutions.netlify.app/privacy";

        /// <summary>True once AdsManager finished initializing.</summary>
        public bool IsAdsManagerInitialized => AdsManager.Instance.IsInitialized;

        private void Start()
        {
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            var manager = AdsManager.Instance;
            manager.ApplyConfiguration(BuildSettings());

            await manager.InitializeAsync();

            if (manager.IsInitialized && showBannerOnStart && !manager.RemoveAds)
            {
                manager.ShowBanner(true);
            }

            Debug.Log("[Autech.LevelPlay] VerifyLevelPlay bootstrap complete.");
        }

        private AdsManagerSettings BuildSettings()
        {
            return new AdsManagerSettings
            {
                AdsEnabled = adsEnabled,
                RemoveAds = removeAds,
                TestMode = testMode,
                AutoLaunchTestSuite = autoLaunchTestSuite,
                UseAdaptiveBanners = useAdaptiveBanners,
                PreferredBannerSize = preferredBannerSize,
                BannerPosition = bannerPosition,
                ShowConsentDialog = showConsentDialog,
                CmpPCode = NormalizePCode(cmpPCode),
                CmpShowIdfaPopup = cmpShowIdfaPopup,
                RequestAttAuthorization = requestAttAuthorization,
                CcpaOptOut = false,
                TagForChildDirectedTreatment = tagForChildDirectedTreatment,
                PrivacyPolicyUrl = privacyPolicyUrl,
                AndroidAppKey = androidAppKey,
                IosAppKey = iosAppKey,
                AndroidBannerAdUnitId = androidBannerAdUnitId,
                AndroidInterstitialAdUnitId = androidInterstitialAdUnitId,
                AndroidRewardedAdUnitId = androidRewardedAdUnitId,
                IosBannerAdUnitId = iosBannerAdUnitId,
                IosInterstitialAdUnitId = iosInterstitialAdUnitId,
                IosRewardedAdUnitId = iosRewardedAdUnitId
            };
        }

        // Accepts the p-code with or without the leading "p-" the CMP portal shows.
        private static string NormalizePCode(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            raw = raw.Trim();
            return raw.StartsWith("p-") ? raw.Substring(2) : raw;
        }

        #region Public API (AdMob-package parity)

        public void SetRemoveAds(bool remove)
        {
            removeAds = remove;
            AdsManager.Instance.RemoveAds = remove;
        }

        public bool IsRemoveAdsEnabled() => AdsManager.Instance.RemoveAds;

        public void PurchaseRemoveAds() => SetRemoveAds(true);

        public void RestorePurchases() => AdsManager.Instance.ForceLoadFromStorage();

        public void ShowPrivacyOptionsForm() => AdsManager.Instance.ShowPrivacyOptionsForm();

        public bool ShouldShowPrivacyOptionsButton() => AdsManager.Instance.ShouldShowPrivacyOptionsButton();

        public bool CanUserRequestAds() => AdsManager.Instance.CanUserRequestAds();

        public bool IsAnyAdShowing() => AdsManager.Instance.IsShowingAd;

        public bool IsBannerVisible() => AdsManager.Instance.IsBannerVisible();

        public bool IsInterstitialReady() => AdsManager.Instance.IsInterstitialReady();

        public bool IsRewardedReady() => AdsManager.Instance.IsRewardedReady();

        #endregion

        #region Context Menu (editor testing)

        [ContextMenu("Show Interstitial")]
        public void TestShowInterstitial() => AdsManager.Instance.ShowInterstitial();

        [ContextMenu("Show Rewarded")]
        public void TestShowRewarded() => AdsManager.Instance.ShowRewarded();

        [ContextMenu("Toggle Banner")]
        public void TestToggleBanner() => AdsManager.Instance.ShowBanner(!AdsManager.Instance.IsBannerVisible());

        [ContextMenu("Toggle Remove Ads")]
        public void TestToggleRemoveAds() => SetRemoveAds(!AdsManager.Instance.RemoveAds);

        [ContextMenu("Show Privacy Options")]
        public void TestShowPrivacyOptions() => ShowPrivacyOptionsForm();

        [ContextMenu("Launch Test Suite")]
        public void TestLaunchTestSuite() => AdsManager.Instance.LaunchTestSuite();

        [ContextMenu("Reset Stored Consent (Testing)")]
        public void TestResetConsent() => AdsManager.Instance.Consent.ResetConsentForTesting();

        [ContextMenu("Log Debug Status")]
        public void TestLogDebugStatus() => AdsManager.Instance.LogDebugStatus();

        #endregion
    }
}
#endif // LEVELPLAY_INSTALLED
