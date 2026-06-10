#if LEVELPLAY_INSTALLED
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.LevelPlay;

namespace Autech.LevelPlay
{
    /// <summary>
    /// Wraps one LevelPlay interstitial ad unit: load with retry, show with
    /// success/failure callbacks, automatic reload after close.
    /// </summary>
    public class InterstitialAdController
    {
        private const int MaxRetryAttempts = 3;
        private const float BaseRetryDelaySeconds = 2f;

        private readonly LevelPlayInterstitialAd interstitialAd;

        private Action pendingOnSuccess;
        private Action pendingOnFailure;
        private int retryAttempt;
        private CancellationTokenSource retryCts;

        public bool IsReady => interstitialAd != null && interstitialAd.IsAdReady();

        public InterstitialAdController(string adUnitId)
        {
            interstitialAd = new LevelPlayInterstitialAd(adUnitId);
            interstitialAd.OnAdLoaded += HandleLoaded;
            interstitialAd.OnAdLoadFailed += HandleLoadFailed;
            interstitialAd.OnAdDisplayFailed += HandleDisplayFailed;
            interstitialAd.OnAdClosed += HandleClosed;
        }

        public void LoadAd()
        {
            CancelRetry();
            interstitialAd.LoadAd();
        }

        public void Show(Action onSuccess, Action onFailure, string placementName = null)
        {
            if (!IsReady)
            {
                Debug.LogWarning("[Autech.LevelPlay] Interstitial not ready.");
                onFailure?.Invoke();
                LoadAd();
                return;
            }

            pendingOnSuccess = onSuccess;
            pendingOnFailure = onFailure;
            interstitialAd.ShowAd(placementName);
        }

        public void Destroy()
        {
            CancelRetry();
            interstitialAd?.DestroyAd();
        }

        private void HandleLoaded(LevelPlayAdInfo info)
        {
            retryAttempt = 0;
        }

        private void HandleLoadFailed(LevelPlayAdError error)
        {
            Debug.LogWarning($"[Autech.LevelPlay] Interstitial load failed: {error}");
            _ = RetryLoadAsync();
        }

        private void HandleDisplayFailed(LevelPlayAdInfo info, LevelPlayAdError error)
        {
            Debug.LogWarning($"[Autech.LevelPlay] Interstitial display failed: {error}");
            var onFailure = pendingOnFailure;
            pendingOnSuccess = null;
            pendingOnFailure = null;
            onFailure?.Invoke();
            LoadAd();
        }

        private void HandleClosed(LevelPlayAdInfo info)
        {
            var onSuccess = pendingOnSuccess;
            pendingOnSuccess = null;
            pendingOnFailure = null;
            onSuccess?.Invoke();
            LoadAd();
        }

        private async Task RetryLoadAsync()
        {
            if (retryAttempt >= MaxRetryAttempts)
            {
                Debug.LogWarning("[Autech.LevelPlay] Interstitial retry budget exhausted.");
                return;
            }

            retryAttempt++;
            CancelRetry();
            retryCts = new CancellationTokenSource();
            var token = retryCts.Token;
            var delaySeconds = BaseRetryDelaySeconds * Mathf.Pow(2f, retryAttempt - 1);

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), token);
            }
            catch (TaskCanceledException)
            {
                return;
            }

            if (!token.IsCancellationRequested)
            {
                interstitialAd.LoadAd();
            }
        }

        private void CancelRetry()
        {
            retryCts?.Cancel();
            retryCts?.Dispose();
            retryCts = null;
        }
    }
}
#endif // LEVELPLAY_INSTALLED
