using System;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Autech.LevelPlay
{
    /// <summary>
    /// Real Consent Management Platform integration backed by <b>InMobi CMP</b>
    /// (Choice) — the LevelPlay counterpart to the AdMob package's Google-UMP
    /// <c>ConsentManager</c>. InMobi CMP is a Google-certified IAB TCF v2.2 CMP:
    /// it renders the GDPR/Google consent UI, generates the IAB TC string and
    /// writes the standard <c>IABTCF_*</c> values to native storage, which the
    /// LevelPlay/Unity Ads adapters consume automatically.
    ///
    /// The InMobi CMP Unity plugin ships without an asmdef (its <c>ChoiceCMP</c>
    /// type lives in Assembly-CSharp), which our package asmdef cannot reference
    /// directly — so the few SDK calls are made by reflection (the same approach
    /// the AdMob package uses for Unity Ads MetaData) and consent state is read
    /// straight from the IAB <c>IABTCF_*</c> keys. The package therefore compiles
    /// with or without the InMobi CMP plugin present.
    ///
    /// Setup: import the InMobi CMP Unity plugin, set your CMP <c>p-code</c>
    /// (Inspector → VerifyLevelPlay → Consent), configure vendors/Google in the
    /// InMobi CMP portal. See INSTALL.md.
    /// </summary>
    public class ConsentManager
    {
        // IAB TCF v2 storage keys (written by any TCF CMP, read by ad SDKs).
        private const string TcfStringKey = "IABTCF_TCString";
        private const string TcfPurposeConsentsKey = "IABTCF_PurposeConsents";
        private const string TcfGdprAppliesKey = "IABTCF_gdprApplies";

        // Purpose 3 = "Create a personalised ads profile" (1-indexed in the string).
        private const int PersonalizationPurposeId = 3;

        private const int TcfPollIntervalMs = 150;
        private const int TcfDataTimeoutMs = 8000;

        private readonly AdConfiguration config;

        /// <summary>Fired when the consent flow completes; bool = ads may be requested.</summary>
        public event Action<bool> OnConsentReady;

        /// <summary>Fired when the stored consent changes (e.g. after the privacy form).</summary>
        public event Action<bool> OnConsentChanged;

        public ConsentManager(AdConfiguration config)
        {
            this.config = config;
        }

        #region Queries

        /// <summary>Ads can always be requested — consent gates personalization, not serving (contextual ads are allowed without consent).</summary>
        public bool CanUserRequestAds() => true;

        /// <summary>The privacy-options entry point is always available so users can change consent at any time (GDPR Art. 7(3)).</summary>
        public bool ShouldShowPrivacyOptionsButton() => true;

        /// <summary>"Personalized" | "NonPersonalized" | "Unknown" — mirrors the AdMob package API. Derived from IAB TCF Purpose 3.</summary>
        public string GetConsentType()
        {
            var tcString = GetTCFConsentString();
            if (string.IsNullOrEmpty(tcString)) return "Unknown";
            return HasConsentForPurpose(PersonalizationPurposeId) ? "Personalized" : "NonPersonalized";
        }

        /// <summary>The IAB TC string the CMP wrote to native storage (empty in the Editor / before consent).</summary>
        public string GetTCFConsentString() => ReadNative(TcfStringKey);

        /// <summary>True if the user granted the given IAB TCF purpose (1-indexed).</summary>
        public bool HasConsentForPurpose(int purposeId)
        {
            var purposes = ReadNative(TcfPurposeConsentsKey);
            if (purposeId <= 0 || purposeId > purposes.Length) return false;
            return purposes[purposeId - 1] == '1';
        }

        /// <summary>True when the CMP determined GDPR applies to this user (region-based).</summary>
        public bool GdprApplies() => ReadNative(TcfGdprAppliesKey) == "1";

        /// <summary>
        /// Human-readable dump of the consent / IAB-TCF values the CMP wrote to
        /// native storage. For the on-device debug panel, so you can confirm the SDK
        /// actually grabbed them. All values are empty in the Editor / before the
        /// CMP has run (no native storage there).
        /// </summary>
        public string GetConsentDebugSnapshot()
        {
            var sb = new StringBuilder();
            sb.AppendLine("- Consent (IAB TCF) -");
            sb.AppendLine($"  GDPR applies: {GdprApplies()}");
            sb.AppendLine($"  Consent type: {GetConsentType()}");
            sb.AppendLine($"  Purpose 1 (store/access info): {HasConsentForPurpose(1)}");
            sb.AppendLine($"  Purpose 3 (personalised ads): {HasConsentForPurpose(PersonalizationPurposeId)}");
            sb.AppendLine($"  PurposeConsents: {Ellipsize(ReadNative(TcfPurposeConsentsKey), 24)}");
            sb.AppendLine($"  VendorConsents: {Ellipsize(ReadNative("IABTCF_VendorConsents"), 24)}");
            sb.AppendLine($"  TC string: {Ellipsize(GetTCFConsentString(), 32)}");
            var usPrivacy = ReadNative("IABUSPrivacy_String");
            if (!string.IsNullOrEmpty(usPrivacy)) sb.AppendLine($"  US Privacy: {usPrivacy}");
            var gpp = ReadNative("IABGPP_HDR_GppString");
            if (!string.IsNullOrEmpty(gpp)) sb.AppendLine($"  GPP: {Ellipsize(gpp, 24)}");
            return sb.ToString();
        }

        private static string Ellipsize(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "(empty)";
            return s.Length <= max ? s : $"{s.Substring(0, max)}…(len {s.Length})";
        }

        #endregion

        #region Flow

        /// <summary>
        /// Start the InMobi CMP, then wait until the IAB TCF data has been written
        /// (or a timeout). Call BEFORE LevelPlay init so the TC string is in place
        /// when the adapters read it. No-op (consent assumed) when the consent flow
        /// is disabled or the CMP is not configured/present.
        /// </summary>
        public async Task<bool> InitializeConsentAsync()
        {
            if (!config.ShowConsentDialog)
            {
                Debug.Log("[Autech.LevelPlay] Consent flow disabled in configuration — skipping CMP.");
                OnConsentReady?.Invoke(true);
                return true;
            }

            if (string.IsNullOrEmpty(config.CmpPCode))
            {
                Debug.LogWarning("[Autech.LevelPlay] No InMobi CMP p-code configured — consent UI will NOT be shown. " +
                                 "Set it on VerifyLevelPlay (Consent) before shipping to GDPR regions.");
                OnConsentReady?.Invoke(true);
                return true;
            }

            if (!IsCmpAvailable)
            {
                Debug.LogWarning("[Autech.LevelPlay] InMobi CMP plugin not found (ChoiceCMP missing). Import the " +
                                 "InMobi CMP Unity plugin — see INSTALL.md. Continuing without a consent prompt.");
                OnConsentReady?.Invoke(true);
                return true;
            }

            try
            {
                SetCmpLogLevel(config.IsTestModeActive);
                StartChoice(config.CmpPCode, config.CmpShowIdfaPopup);
                await WaitForTcfDataAsync();
                OnConsentChanged?.Invoke(HasConsentForPurpose(PersonalizationPurposeId));
            }
            catch (Exception e)
            {
                Debug.LogError($"[Autech.LevelPlay] InMobi CMP start failed: {e.Message}");
            }

            OnConsentReady?.Invoke(CanUserRequestAds());
            return true;
        }

        /// <summary>Re-open the CMP UI so the user can change their choice (privacy options / GDPR withdrawal).</summary>
        public void ShowPrivacyOptionsForm()
        {
            if (!IsCmpAvailable)
            {
                Debug.LogWarning("[Autech.LevelPlay] Cannot show privacy options — InMobi CMP plugin not present.");
                return;
            }
            try
            {
                InvokeStatic("ForceDisplayUI");
                OnConsentChanged?.Invoke(HasConsentForPurpose(PersonalizationPurposeId));
            }
            catch (Exception e)
            {
                Debug.LogError($"[Autech.LevelPlay] ForceDisplayUI failed: {e.Message}");
            }
        }

        /// <summary>Show the CCPA / US-privacy form (InMobi CMP). Wire to a "Do not sell my data" button.</summary>
        public void ShowCcpaForm()
        {
            if (!IsCmpAvailable) return;
            try { InvokeStatic("ShowCCPA"); }
            catch (Exception e) { Debug.LogError($"[Autech.LevelPlay] ShowCCPA failed: {e.Message}"); }
        }

        /// <summary>TESTING ONLY: clear stored IAB consent so the CMP shows again next launch.</summary>
        public void ResetConsentForTesting()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            ClearAndroidTcf();
#elif UNITY_IOS && !UNITY_EDITOR
            ClearIosTcf();
#else
            Debug.Log("[Autech.LevelPlay] ResetConsentForTesting is a no-op in the Editor.");
#endif
        }

        private async Task WaitForTcfDataAsync()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            int elapsed = 0;
            while (elapsed < TcfDataTimeoutMs)
            {
                var tc = ReadNative(TcfStringKey);
                if (!string.IsNullOrEmpty(tc)) return;
                await Task.Delay(TcfPollIntervalMs);
                elapsed += TcfPollIntervalMs;
            }
            Debug.LogWarning($"[Autech.LevelPlay] TCF data not available after {TcfDataTimeoutMs}ms — continuing (user may not be in a GDPR region).");
#else
            await Task.CompletedTask;
#endif
        }

        #endregion

        #region InMobi CMP reflection (plugin lives in Assembly-CSharp, no asmdef)

        private static Type cachedChoiceType;
        private static Type ChoiceType => cachedChoiceType != null
            ? cachedChoiceType
            : (cachedChoiceType = Type.GetType("ChoiceCMP, Assembly-CSharp"));

        private bool IsCmpAvailable => ChoiceType != null;

        private static void StartChoice(string pCode, bool showIdfaPopup)
        {
            // public static void StartChoice(string pCode, ChoiceStyle choiceStyle = null, bool shouldDisplayIDFA = false)
            var m = ChoiceType.GetMethod("StartChoice", BindingFlags.Public | BindingFlags.Static);
            if (m == null) throw new MissingMethodException("ChoiceCMP.StartChoice");
            var pars = m.GetParameters();
            var args = new object[pars.Length];
            args[0] = pCode;
            for (int i = 1; i < pars.Length; i++)
                args[i] = pars[i].ParameterType == typeof(bool) ? (object)showIdfaPopup : null;
            m.Invoke(null, args);
        }

        private static void InvokeStatic(string method)
        {
            var m = ChoiceType.GetMethod(method, BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);
            m?.Invoke(null, null);
        }

        private static void SetCmpLogLevel(bool verbose)
        {
            try
            {
                var field = ChoiceType.GetField("ChoiceLogLevel", BindingFlags.Public | BindingFlags.Static);
                var logEnum = ChoiceType.GetNestedType("LogLevel", BindingFlags.Public);
                if (field != null && logEnum != null)
                    field.SetValue(null, Enum.Parse(logEnum, verbose ? "Debug" : "Error"));
            }
            catch { /* logging config is best-effort */ }
        }

        #endregion

        #region Native IABTCF_* storage readers (ported from the AdMob package)

        private static string ReadNative(string key)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return GetAndroidPref(key, "");
#elif UNITY_IOS && !UNITY_EDITOR
            return GetIosDefault(key, "");
#else
            return "";
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        private static string GetAndroidPref(string key, string defaultValue)
        {
            try
            {
                using (var up = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = up.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var context = activity.Call<AndroidJavaObject>("getApplicationContext"))
                using (var prefs = context.Call<AndroidJavaObject>("getSharedPreferences",
                           context.Call<string>("getPackageName") + "_preferences", 0))
                {
                    // gdprApplies is stored as an int; everything else as a string.
                    if (key == TcfGdprAppliesKey)
                        return prefs.Call<int>("getInt", key, 0).ToString();
                    return prefs.Call<string>("getString", key, defaultValue);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Autech.LevelPlay] Android pref read '{key}' failed: {e.Message}");
                return defaultValue;
            }
        }

        private static void ClearAndroidTcf()
        {
            try
            {
                using (var up = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = up.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var context = activity.Call<AndroidJavaObject>("getApplicationContext"))
                using (var prefs = context.Call<AndroidJavaObject>("getSharedPreferences",
                           context.Call<string>("getPackageName") + "_preferences", 0))
                using (var editor = prefs.Call<AndroidJavaObject>("edit"))
                {
                    foreach (var k in new[] { TcfStringKey, TcfPurposeConsentsKey, TcfGdprAppliesKey })
                        editor.Call<AndroidJavaObject>("remove", k);
                    editor.Call("apply");
                }
                Debug.Log("[Autech.LevelPlay] Cleared Android IABTCF data.");
            }
            catch (Exception e) { Debug.LogError($"[Autech.LevelPlay] Clear Android TCF failed: {e.Message}"); }
        }
#endif

#if UNITY_IOS && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern string _GetUserDefault(string key, string defaultValue);

        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void _RemoveUserDefault(string key);

        private static string GetIosDefault(string key, string defaultValue)
        {
            try { return _GetUserDefault(key, defaultValue); }
            catch (Exception e)
            {
                Debug.LogWarning($"[Autech.LevelPlay] iOS default read '{key}' failed: {e.Message}");
                return defaultValue;
            }
        }

        private static void ClearIosTcf()
        {
            foreach (var k in new[] { TcfStringKey, TcfPurposeConsentsKey, TcfGdprAppliesKey })
                _RemoveUserDefault(k);
            Debug.Log("[Autech.LevelPlay] Cleared iOS IABTCF data.");
        }
#endif

        #endregion
    }
}
