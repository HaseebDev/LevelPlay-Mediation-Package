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
        public bool EnableTestSuite;
        public bool EnableAdsOnIosAppOnMac;
        public bool UseAdaptiveBanners;
        public BannerSize PreferredBannerSize;
        public BannerPosition BannerPosition;
        public bool ShowConsentDialog;
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

        private BannerAdController bannerController;
        private InterstitialAdController interstitialController;
        private RewardedAdController rewardedController;

        private bool isInitialized;
        private bool isInitializing;
        private bool isShowingAd;

        public bool IsInitialized => isInitialized;
        public bool IsShowingAd => isShowingAd;

        /// <summary>Consent component for advanced queries (consent type, CCPA toggle).</summary>
        public ConsentManager Consent => consentManager;

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
        }

        #region Initialization

        /// <summary>Apply settings; call before <see cref="InitializeAsync"/>.</summary>
        public void ApplyConfiguration(AdsManagerSettings settings)
        {
            config.AdsEnabled = settings.AdsEnabled;
            config.RemoveAds = settings.RemoveAds;
            config.EnableTestSuite = settings.EnableTestSuite;
            config.EnableAdsOnIosAppOnMac = settings.EnableAdsOnIosAppOnMac;
            config.UseAdaptiveBanners = settings.UseAdaptiveBanners;
            config.PreferredBannerSize = settings.PreferredBannerSize;
            config.BannerPosition = settings.BannerPosition;
            config.ShowConsentDialog = settings.ShowConsentDialog;
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
                // Macs; LevelPlay is likewise unsupported there. Run the game
                // ad-free on Mac unless explicitly enabled.
                if (UnityEngine.iOS.Device.iosAppOnMac && !config.EnableAdsOnIosAppOnMac)
                {
                    Debug.Log("[Autech.LevelPlay] iOS app running on Mac — ads disabled (EnableAdsOnIosAppOnMac is off).");
                    return;
                }
#endif

                if (persistenceManager.HasRemoveAdsDataInStorage())
                {
                    config.RemoveAds = persistenceManager.LoadRemoveAdsStatus();
                }

                // 1. Consent BEFORE init: LevelPlay wants CCPA/COPPA flags pre-init,
                //    and GDPR consent must exist before any personalized request.
                await consentManager.InitializeConsentAsync();

                // 2. ATT BEFORE init: Unity requires the ATT prompt before
                //    initializing any SDK that may access the IDFA.
                if (config.RequestAttAuthorization)
                {
                    await AttManager.RequestAuthorizationAsync();
                }

                if (!config.HasAppKey)
                {
                    Debug.LogError("[Autech.LevelPlay] No LevelPlay app key configured for this platform — init aborted.");
                    return;
                }

                // 3. Test suite metadata must be set before Init.
                if (config.EnableTestSuite)
                {
                    LevelPlaySdk.SetMetaData("is_test_suite", "enable");
                }

                // 4. Initialize the SDK and await the result event.
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

                if (config.EnableTestSuite)
                {
                    LevelPlaySdk.LaunchTestSuite();
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
            consentManager.ShowPrivacyOptionsForm();
        }

        /// <summary>False while ads are disabled — no consent to manage, so settings hide the button.</summary>
        public bool ShouldShowPrivacyOptionsButton() => config.AdsEnabled && consentManager.ShouldShowPrivacyOptionsButton();

        public bool CanUserRequestAds() => consentManager.CanUserRequestAds();

        /// <summary>"Personalized" | "NonPersonalized" | "Unknown".</summary>
        public string GetConsentType() => consentManager.GetConsentType();

        /// <summary>CCPA/US-state "do not sell or share" opt-out toggle.</summary>
        public void SetCcpaOptOut(bool optedOut) => consentManager.SetCcpaOptOut(optedOut);

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
