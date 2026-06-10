#if LEVELPLAY_INSTALLED
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.LevelPlay;

namespace Autech.LevelPlay
{
    /// <summary>
    /// Wraps one LevelPlay rewarded ad unit: load with retry, show with the
    /// 3-callback pattern (onRewarded / onSuccess / onFailure) used across the
    /// game, automatic reload after close.
    /// LevelPlay's OnAdRewarded may fire AFTER OnAdClosed, so the reward
    /// callback stays armed until either the reward arrives or the next show.
    /// </summary>
    public class RewardedAdController
    {
        private const int MaxRetryAttempts = 3;
        private const float BaseRetryDelaySeconds = 2f;

        private readonly LevelPlayRewardedAd rewardedAd;

        private Action<LevelPlayReward> pendingOnRewarded;
        private Action pendingOnSuccess;
        private Action pendingOnFailure;
        private int retryAttempt;
        private CancellationTokenSource retryCts;

        /// <summary>Fired when the user earns a reward (after callbacks dispatch).</summary>
        public event Action<LevelPlayReward> OnRewardEarned;

        public bool IsReady => rewardedAd != null && rewardedAd.IsAdReady();

        public RewardedAdController(string adUnitId)
        {
            rewardedAd = new LevelPlayRewardedAd(adUnitId);
            rewardedAd.OnAdLoaded += HandleLoaded;
            rewardedAd.OnAdLoadFailed += HandleLoadFailed;
            rewardedAd.OnAdDisplayFailed += HandleDisplayFailed;
            rewardedAd.OnAdRewarded += HandleRewarded;
            rewardedAd.OnAdClosed += HandleClosed;
        }

        public void LoadAd()
        {
            CancelRetry();
            rewardedAd.LoadAd();
        }

        /// <summary>
        /// Show the ad. onRewarded fires when the user earns the reward (may be
        /// after close), onSuccess when the ad closes, onFailure when the ad is
        /// not ready or fails to display.
        /// </summary>
        public void Show(Action<LevelPlayReward> onRewarded, Action onSuccess, Action onFailure, string placementName = null)
        {
            if (!IsReady)
            {
                Debug.LogWarning("[Autech.LevelPlay] Rewarded ad not ready.");
                onFailure?.Invoke();
                LoadAd();
                return;
            }

            pendingOnRewarded = onRewarded;
            pendingOnSuccess = onSuccess;
            pendingOnFailure = onFailure;

            rewardedAd.ShowAd(placementName);
        }

        public void Destroy()
        {
            CancelRetry();
            rewardedAd?.DestroyAd();
        }

        private void HandleLoaded(LevelPlayAdInfo info)
        {
            retryAttempt = 0;
        }

        private void HandleLoadFailed(LevelPlayAdError error)
        {
            Debug.LogWarning($"[Autech.LevelPlay] Rewarded load failed: {error}");
            _ = RetryLoadAsync();
        }

        private void HandleDisplayFailed(LevelPlayAdInfo info, LevelPlayAdError error)
        {
            Debug.LogWarning($"[Autech.LevelPlay] Rewarded display failed: {error}");
            var onFailure = pendingOnFailure;
            ClearPendingCallbacks();
            onFailure?.Invoke();
            LoadAd();
        }

        private void HandleRewarded(LevelPlayAdInfo info, LevelPlayReward reward)
        {
            var onRewarded = pendingOnRewarded;
            pendingOnRewarded = null;
            onRewarded?.Invoke(reward);
            OnRewardEarned?.Invoke(reward);
        }

        private void HandleClosed(LevelPlayAdInfo info)
        {
            var onSuccess = pendingOnSuccess;
            pendingOnSuccess = null;
            pendingOnFailure = null;
            // pendingOnRewarded intentionally stays armed: LevelPlay documents
            // OnAdRewarded as asynchronous and possibly later than OnAdClosed.
            onSuccess?.Invoke();
            LoadAd();
        }

        private void ClearPendingCallbacks()
        {
            pendingOnRewarded = null;
            pendingOnSuccess = null;
            pendingOnFailure = null;
        }

        private async Task RetryLoadAsync()
        {
            if (retryAttempt >= MaxRetryAttempts)
            {
                Debug.LogWarning("[Autech.LevelPlay] Rewarded retry budget exhausted.");
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
                rewardedAd.LoadAd();
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
