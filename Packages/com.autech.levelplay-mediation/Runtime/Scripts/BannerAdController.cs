#if LEVELPLAY_INSTALLED
using UnityEngine;
using Unity.Services.LevelPlay;

namespace Autech.LevelPlay
{
    /// <summary>
    /// Wraps one LevelPlay banner ad unit. Banners auto-refresh; this controller
    /// owns load/show/hide state and exposes size info for safe-area layout
    /// (NotchSafeArea queries IsBannerVisible / GetBannerSize).
    /// </summary>
    public class BannerAdController
    {
        private readonly AdConfiguration config;

        private LevelPlayBannerAd bannerAd;
        private bool isLoaded;
        private bool isVisible;
        private bool showWhenLoaded;

        public bool IsBannerLoaded => isLoaded;
        public bool IsBannerVisible => isVisible;
        public BannerPosition CurrentPosition { get; private set; }

        public BannerAdController(AdConfiguration config)
        {
            this.config = config;
            CurrentPosition = config.BannerPosition;
        }

        /// <summary>Create (if needed) and load the banner. Hidden until ShowBanner(true).</summary>
        public void LoadBanner()
        {
            if (bannerAd == null)
            {
                CreateBanner();
            }

            bannerAd.LoadAd();
        }

        /// <summary>Show or hide the banner; loads first when necessary.</summary>
        public void ShowBanner(bool show)
        {
            if (show)
            {
                if (bannerAd == null || !isLoaded)
                {
                    showWhenLoaded = true;
                    LoadBanner();
                    return;
                }

                bannerAd.ShowAd();
                isVisible = true;
            }
            else
            {
                showWhenLoaded = false;
                if (bannerAd != null && isVisible)
                {
                    bannerAd.HideAd();
                }
                isVisible = false;
            }
        }

        /// <summary>Move the banner; recreates the ad object since position is fixed at construction.</summary>
        public void SetBannerPosition(BannerPosition position)
        {
            if (CurrentPosition == position) return;

            CurrentPosition = position;
            config.BannerPosition = position;

            if (bannerAd == null) return;

            var wasVisible = isVisible;
            DestroyBanner();
            if (wasVisible)
            {
                showWhenLoaded = true;
            }
            LoadBanner();
        }

        /// <summary>Current banner size in device pixels (Vector2.zero before load).</summary>
        public Vector2 GetBannerSize()
        {
            var size = bannerAd?.GetAdSize();
            if (size == null) return Vector2.zero;
            return new Vector2(size.Width, size.Height);
        }

        public void DestroyBanner()
        {
            if (bannerAd == null) return;

            bannerAd.DestroyAd();
            bannerAd = null;
            isLoaded = false;
            isVisible = false;
        }

        private void CreateBanner()
        {
            var bannerConfig = new LevelPlayBannerAd.Config.Builder()
                .SetSize(ResolveSize())
                .SetPosition(ResolvePosition())
                .SetDisplayOnLoad(false)
                .SetRespectSafeArea(true)
                .Build();

            bannerAd = new LevelPlayBannerAd(config.BannerAdUnitId, bannerConfig);
            bannerAd.OnAdLoaded += HandleLoaded;
            bannerAd.OnAdLoadFailed += HandleLoadFailed;
        }

        private void HandleLoaded(LevelPlayAdInfo info)
        {
            isLoaded = true;
            if (showWhenLoaded)
            {
                showWhenLoaded = false;
                bannerAd.ShowAd();
                isVisible = true;
            }
        }

        private void HandleLoadFailed(LevelPlayAdError error)
        {
            Debug.LogWarning($"[Autech.LevelPlay] Banner load failed: {error}");
            isLoaded = false;
        }

        private LevelPlayAdSize ResolveSize()
        {
            if (config.UseAdaptiveBanners || config.PreferredBannerSize == BannerSize.Adaptive)
            {
                return LevelPlayAdSize.CreateAdaptiveAdSize();
            }

            switch (config.PreferredBannerSize)
            {
                case BannerSize.Large: return LevelPlayAdSize.LARGE;
                case BannerSize.MediumRectangle: return LevelPlayAdSize.MEDIUM_RECTANGLE;
                case BannerSize.Leaderboard: return LevelPlayAdSize.LEADERBOARD;
                default: return LevelPlayAdSize.BANNER;
            }
        }

        private LevelPlayBannerPosition ResolvePosition()
        {
            switch (CurrentPosition)
            {
                case BannerPosition.Top: return LevelPlayBannerPosition.TopCenter;
                case BannerPosition.TopLeft: return LevelPlayBannerPosition.TopLeft;
                case BannerPosition.TopRight: return LevelPlayBannerPosition.TopRight;
                case BannerPosition.BottomLeft: return LevelPlayBannerPosition.BottomLeft;
                case BannerPosition.BottomRight: return LevelPlayBannerPosition.BottomRight;
                case BannerPosition.Center: return LevelPlayBannerPosition.Center;
                default: return LevelPlayBannerPosition.BottomCenter;
            }
        }
    }
}
#endif // LEVELPLAY_INSTALLED
