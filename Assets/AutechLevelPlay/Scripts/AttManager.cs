using System.Threading.Tasks;
using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Autech.LevelPlay
{
    /// <summary>
    /// iOS App Tracking Transparency status. Mirrors ATTrackingManagerAuthorizationStatus.
    /// </summary>
    public enum AttStatus
    {
        NotDetermined = 0,
        Restricted = 1,
        Denied = 2,
        Authorized = 3,
        /// <summary>Non-iOS platform or editor: ATT does not apply.</summary>
        NotSupported = -1
    }

    /// <summary>
    /// iOS App Tracking Transparency (ATT) helper. As of the InMobi-CMP-owns-ATT
    /// change, the ATT prompt is triggered by the InMobi CMP (shouldDisplayIDFA)
    /// during the consent flow, so this class is primarily a read-only status
    /// source (<see cref="Status"/>/<see cref="IsAuthorized"/>) used by the debug
    /// panel. <see cref="RequestAuthorizationAsync"/> remains available as an
    /// opt-in app-controlled prompt (VerifyLevelPlay → requestAttAuthorization),
    /// which <see cref="AdsManager"/> awaits before init only when that toggle is on.
    /// Either way the NSUserTrackingUsageDescription Info.plist entry is injected at
    /// build time by the package's iOS post-processor.
    /// </summary>
    public static class AttManager
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern int _autechAttGetStatus();

        [DllImport("__Internal")]
        private static extern void _autechAttRequest();
#endif

        private const float RequestTimeoutSeconds = 90f;
        private const int PollIntervalMs = 200;

        /// <summary>Current ATT authorization status.</summary>
        public static AttStatus Status
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR
                return (AttStatus)_autechAttGetStatus();
#else
                return AttStatus.NotSupported;
#endif
            }
        }

        /// <summary>True when tracking is authorized (IDFA available).</summary>
        public static bool IsAuthorized => Status == AttStatus.Authorized;

        /// <summary>
        /// Wait for an ATT prompt raised by something ELSE (the InMobi CMP) to be
        /// answered, without ever raising one ourselves. Returns as soon as the
        /// status resolves, or on timeout.
        ///
        /// Use this when another component owns the prompt, and
        /// <see cref="RequestAuthorizationAsync"/> when the app owns it. Calling the
        /// requesting version on top of a prompt the CMP already raised would issue a
        /// second requestTrackingAuthorization while the first is in flight, which is
        /// not documented as safe, and would also raise a prompt in projects where
        /// the CMP never started.
        /// </summary>
        public static async Task<AttStatus> WaitForResolutionAsync()
        {
#if UNITY_IOS && !UNITY_EDITOR
            var status = Status;
            AdLog.Info($"ATT wait requested, current status={status}.");
            if (status != AttStatus.NotDetermined)
            {
                AdLog.Info($"ATT already resolved, not waiting: {status}");
                return status;
            }

            AdLog.Info("Waiting for the ATT prompt raised by the CMP…");

            var elapsed = 0f;
            while (Status == AttStatus.NotDetermined && elapsed < RequestTimeoutSeconds)
            {
                await Task.Delay(PollIntervalMs);
                elapsed += PollIntervalMs / 1000f;
            }

            status = Status;
            AdLog.Info($"ATT resolved to: {status}");
            return status;
#else
            await Task.CompletedTask;
            return AttStatus.NotSupported;
#endif
        }

        /// <summary>
        /// Show the ATT prompt if the status is still NotDetermined and await the
        /// user's choice. Returns the final status. No-ops outside iOS devices.
        /// The long timeout covers the app being backgrounded while the system
        /// dialog is up; on timeout the current (NotDetermined) status returns
        /// and ads simply run without IDFA.
        /// </summary>
        public static async Task<AttStatus> RequestAuthorizationAsync()
        {
#if UNITY_IOS && !UNITY_EDITOR
            var status = Status;
            AdLog.Info($"ATT requested by the app, current status={status}.");
            if (status != AttStatus.NotDetermined)
            {
                AdLog.Info($"ATT already resolved: {status}");
                return status;
            }

            AdLog.Info("Requesting ATT authorization…");
            _autechAttRequest();

            var elapsed = 0f;
            while (Status == AttStatus.NotDetermined && elapsed < RequestTimeoutSeconds)
            {
                await Task.Delay(PollIntervalMs);
                elapsed += PollIntervalMs / 1000f;
            }

            status = Status;
            AdLog.Info($"ATT result: {status}");
            return status;
#else
            await Task.CompletedTask;
            return AttStatus.NotSupported;
#endif
        }
    }
}
