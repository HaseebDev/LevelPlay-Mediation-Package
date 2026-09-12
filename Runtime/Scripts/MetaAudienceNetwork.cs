using UnityEngine;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif

namespace Autech.LevelPlay
{
    /// <summary>
    /// Meta Audience Network (FAN) pre-init flags, applied by this package rather
    /// than left to the game.
    ///
    /// Meta requires <c>setAdvertiserTrackingEnabled</c> to be set BEFORE
    /// LevelPlay initializes, and no mediation adapter wires it to the device's
    /// ATT status for you. If it stays false, FAN will not deliver personalized
    /// ads. <see cref="AdsManager"/> owns the pre-init window, so the call belongs
    /// here.
    ///
    /// iOS only: the flag exists to tell FAN whether the IDFA is usable, which is
    /// an ATT concern and has no Android equivalent.
    ///
    /// The Meta SDK is resolved dynamically on the native side, so this whole
    /// class is a safe no-op in builds without the Meta adapter and the package
    /// never takes a hard dependency on it.
    /// </summary>
    public static class MetaAudienceNetwork
    {
#if UNITY_IOS && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern int _autechMetaIsAvailable();

        [DllImport("__Internal")]
        private static extern int _autechMetaSetAdvertiserTrackingEnabled(int enabled);
#endif

        /// <summary>True when the Meta Audience Network SDK is present in this build.</summary>
        public static bool IsPresent
        {
            get
            {
#if UNITY_IOS && !UNITY_EDITOR
                try
                {
                    return _autechMetaIsAvailable() == 1;
                }
                catch (System.Exception e)
                {
                    AdLog.Warn($"Meta availability check failed: {e.Message}");
                    return false;
                }
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// Tell Meta whether advertiser tracking is permitted, taken from the
        /// device's ATT status. MUST be called before LevelPlay init. Returns true
        /// when the flag was actually applied, false when Meta is not in the build.
        /// </summary>
        public static bool ApplyAdvertiserTracking(bool authorized)
        {
#if UNITY_IOS && !UNITY_EDITOR
            try
            {
                AdLog.Info($"Meta bridge probe: adapter present={IsPresent}, applying advertiserTrackingEnabled={authorized}.");
                int applied = 0;
                if (authorized)
                {
                    applied = _autechMetaSetAdvertiserTrackingEnabled(1);
                }
                else
                {
                    applied = _autechMetaSetAdvertiserTrackingEnabled(0);
                }

                if (applied == 1)
                {
                    AdLog.Info($"Meta advertiserTrackingEnabled={authorized} (applied pre-init).");
                    return true;
                }

                AdLog.Info("Meta Audience Network not present, advertiser-tracking flag skipped.");
                return false;
            }
            catch (System.Exception e)
            {
                AdLog.Warn($"Failed to set Meta advertiser tracking: {e.Message}");
                return false;
            }
#else
            return false;
#endif
        }
    }
}
