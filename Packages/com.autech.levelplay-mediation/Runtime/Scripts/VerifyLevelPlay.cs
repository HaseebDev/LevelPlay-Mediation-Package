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
        [Tooltip("Launches the LevelPlay integration test suite after init. NEVER ship enabled.")]
        [SerializeField] private bool enableTestSuite = false;

        [Header("Mac (iOS app on Apple silicon)")]
        [Tooltip("LevelPlay officially supports iOS/Android only. Leave OFF to run the game ad-free on Macs.")]
        [SerializeField] private bool enableAdsOnIosAppOnMac = false;

        [Header("Consent & Privacy")]
        [Tooltip("Show the built-in GDPR consent dialog on first launch.")]
        [SerializeField] private bool showConsentDialog = true;
        [Tooltip("Request the iOS App Tracking Transparency prompt before SDK init.")]
        [SerializeField] private bool requestAttAuthorization = true;
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
                EnableTestSuite = enableTestSuite,
                EnableAdsOnIosAppOnMac = enableAdsOnIosAppOnMac,
                UseAdaptiveBanners = useAdaptiveBanners,
                PreferredBannerSize = preferredBannerSize,
                BannerPosition = bannerPosition,
                ShowConsentDialog = showConsentDialog,
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

        [ContextMenu("Reset Stored Consent (Testing)")]
        public void TestResetConsent() => AdsManager.Instance.Consent.ResetConsentForTesting();

        [ContextMenu("Log Debug Status")]
        public void TestLogDebugStatus() => AdsManager.Instance.LogDebugStatus();

        #endregion
    }
}
#endif // LEVELPLAY_INSTALLED
