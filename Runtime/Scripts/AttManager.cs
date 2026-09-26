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
    /// iOS App Tracking Transparency (ATT) helper. Who raises the prompt depends on
    /// the consent path:
    ///   - InMobi CMP path: the CMP raises it (shouldDisplayIDFA) and
    ///     <see cref="WaitForResolutionAsync"/> waits for the answer without asking.
    ///   - Local consent path, or requestAttAuthorization ticked: the app raises it
    ///     with <see cref="RequestAuthorizationAsync"/>.
    /// <see cref="AdsManager"/> awaits the relevant one before init. Either way the
    /// NSUserTrackingUsageDescription Info.plist entry is injected at build time by
    /// the package's iOS post-processor.
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
        ///
        /// iOS only presents the prompt while the app is active and silently drops
        /// a request made while it is not (another system alert on screen, a call,
        /// Control Center). The native side therefore arms the request instead of
        /// firing it once: it asks now if the app is active, and again every time
        /// the app becomes active, until there is an answer. Dismissing whatever
        /// interrupted brings the prompt up straight away.
        ///
        /// On timeout the NotDetermined status returns and ads run without IDFA for
        /// now, but the request stays armed, so the prompt still appears the next
        /// time the app becomes active.
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

            AdLog.Info("Requesting ATT authorization (armed: re-asked on every return to active until answered)…");
            _autechAttRequest();

            var elapsed = 0f;
            var wasFocused = Application.isFocused;
            if (!wasFocused)
            {
                AdLog.Info("App is not active yet, so iOS will hold the ATT prompt until it is.");
            }

            while (Status == AttStatus.NotDetermined && elapsed < RequestTimeoutSeconds)
            {
                await Task.Delay(PollIntervalMs);
                elapsed += PollIntervalMs / 1000f;

                var focused = Application.isFocused;
                if (focused != wasFocused)
                {
                    if (focused)
                    {
                        AdLog.Info("App became active while ATT is still undetermined, so the prompt is being asked again.");
                    }
                    else
                    {
                        AdLog.Info("App lost focus while waiting for ATT; iOS will not show the prompt until it returns.");
                    }
                    wasFocused = focused;
                }
            }

            status = Status;
            if (status == AttStatus.NotDetermined)
            {
                AdLog.Warn($"ATT still undetermined after {RequestTimeoutSeconds:F0}s; continuing without IDFA for now. " +
                           "The request stays armed and the prompt will appear the next time the app becomes active.");
            }
            else
            {
                AdLog.Info($"ATT result: {status}");
            }
            return status;
#else
            await Task.CompletedTask;
            return AttStatus.NotSupported;
#endif
        }
    }
}
