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
        public bool EnableTestSuite { get; set; } = false;

        // LevelPlay officially supports iOS/Android only. When the iOS build
        // runs as an "iPad app on Apple silicon Mac", ads are skipped unless
        // this is explicitly enabled (the game then runs ad-free on Mac).
        public bool EnableAdsOnIosAppOnMac { get; set; } = false;

        // Banner settings
        public bool UseAdaptiveBanners { get; set; } = true;
        public BannerSize PreferredBannerSize { get; set; } = BannerSize.Adaptive;
        public BannerPosition BannerPosition { get; set; } = BannerPosition.Bottom;

        // Consent / privacy settings
        public bool ShowConsentDialog { get; set; } = true;
        public bool RequestAttAuthorization { get; set; } = true;
        public bool CcpaOptOut { get; set; } = false;
        public bool TagForChildDirectedTreatment { get; set; } = false;
        public string PrivacyPolicyUrl { get; set; } = "https://autechsolutions.netlify.app/privacy";

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
                      $"removeAds={RemoveAds} testSuite={EnableTestSuite} adaptive={UseAdaptiveBanners} " +
                      $"position={BannerPosition} ccpaOptOut={CcpaOptOut} coppa={TagForChildDirectedTreatment}");
        }

        private static string Truncate(string value)
        {
            if (string.IsNullOrEmpty(value)) return "<empty>";
            return value.Length <= 6 ? value : value.Substring(0, 6) + "…";
        }
    }
}
