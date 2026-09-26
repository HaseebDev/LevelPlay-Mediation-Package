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
        private readonly string telemetryUnit;
        private string telemetryPlacement;
        private bool telemetryRewardPending;

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
            telemetryUnit = adUnitId;
            rewardedAd = new LevelPlayRewardedAd(adUnitId);
            rewardedAd.OnAdDisplayed += info => AdTelemetry.Report(AdTelemetryFormat.Rewarded, AdTelemetryAction.Displayed, info?.PlacementName ?? telemetryPlacement);
            rewardedAd.OnAdClicked += info => AdTelemetry.Report(AdTelemetryFormat.Rewarded, AdTelemetryAction.Clicked, info?.PlacementName ?? telemetryPlacement);
            rewardedAd.OnAdLoaded += HandleLoaded;
            rewardedAd.OnAdLoadFailed += HandleLoadFailed;
            rewardedAd.OnAdDisplayFailed += HandleDisplayFailed;
            rewardedAd.OnAdRewarded += HandleRewarded;
            rewardedAd.OnAdClosed += HandleClosed;
        }

        public void LoadAd()
        {
            CancelRetry();
            AdTelemetry.Report(AdTelemetryFormat.Rewarded, AdTelemetryAction.Request, telemetryUnit);
            rewardedAd.LoadAd();
        }

        /// <summary>
        /// Show the ad. onRewarded fires when the user earns the reward (may be
        /// after close), onSuccess when the ad closes, onFailure when the ad is
        /// not ready or fails to display.
        /// </summary>
        public void Show(Action<LevelPlayReward> onRewarded, Action onSuccess, Action onFailure, string placementName = null)
        {
            telemetryPlacement = placementName ?? telemetryUnit;
            if (!IsReady)
            {
                AdTelemetry.Report(AdTelemetryFormat.Rewarded, AdTelemetryAction.DisplayFailed, telemetryPlacement);
                AdLog.Warn("Rewarded ad not ready.");
                onFailure?.Invoke();
                LoadAd();
                return;
            }

            pendingOnRewarded = onRewarded;
            telemetryRewardPending = true;
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
            AdTelemetry.Report(AdTelemetryFormat.Rewarded, AdTelemetryAction.Loaded, telemetryUnit);
            retryAttempt = 0;
        }

        private void HandleLoadFailed(LevelPlayAdError error)
        {
            AdTelemetry.Report(AdTelemetryFormat.Rewarded, AdTelemetryAction.LoadFailed, telemetryUnit, error.ErrorCode);
            AdLog.Warn($"Rewarded load failed: {error}");
            _ = RetryLoadAsync();
        }

        private void HandleDisplayFailed(LevelPlayAdInfo info, LevelPlayAdError error)
        {
            telemetryRewardPending = false;
            AdTelemetry.Report(AdTelemetryFormat.Rewarded, AdTelemetryAction.DisplayFailed, telemetryPlacement, error.ErrorCode);
            AdLog.Warn($"Rewarded display failed: {error}");
            var onFailure = pendingOnFailure;
            ClearPendingCallbacks();
            onFailure?.Invoke();
            LoadAd();
        }

        private void HandleRewarded(LevelPlayAdInfo info, LevelPlayReward reward)
        {
            if (telemetryRewardPending)
            {
                telemetryRewardPending = false;
                AdTelemetry.Report(AdTelemetryFormat.Rewarded, AdTelemetryAction.Rewarded, info?.PlacementName ?? telemetryPlacement);
            }
            var onRewarded = pendingOnRewarded;
            pendingOnRewarded = null;
            onRewarded?.Invoke(reward);
            OnRewardEarned?.Invoke(reward);
        }

        private void HandleClosed(LevelPlayAdInfo info)
        {
            AdTelemetry.Report(AdTelemetryFormat.Rewarded, AdTelemetryAction.Closed, info?.PlacementName ?? telemetryPlacement);
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
                AdLog.Warn("Rewarded retry budget exhausted.");
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
                AdTelemetry.Report(AdTelemetryFormat.Rewarded, AdTelemetryAction.Request, telemetryUnit);
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
