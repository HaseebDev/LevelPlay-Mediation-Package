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
    /// Handles the iOS App Tracking Transparency (ATT) prompt. Apple and Unity
    /// both require the prompt to be presented by the app (not the SDK) BEFORE
    /// initializing any SDK that may access the IDFA, so <see cref="AdsManager"/>
    /// awaits <see cref="RequestAuthorizationAsync"/> before LevelPlay init.
    /// The NSUserTrackingUsageDescription Info.plist entry is injected at build
    /// time by the package's iOS post-processor.
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
            if (status != AttStatus.NotDetermined)
            {
                Debug.Log($"[Autech.LevelPlay] ATT already resolved: {status}");
                return status;
            }

            Debug.Log("[Autech.LevelPlay] Requesting ATT authorization…");
            _autechAttRequest();

            var elapsed = 0f;
            while (Status == AttStatus.NotDetermined && elapsed < RequestTimeoutSeconds)
            {
                await Task.Delay(PollIntervalMs);
                elapsed += PollIntervalMs / 1000f;
            }

            status = Status;
            Debug.Log($"[Autech.LevelPlay] ATT result: {status}");
            return status;
#else
            await Task.CompletedTask;
            return AttStatus.NotSupported;
#endif
        }
    }
}
