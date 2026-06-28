#if UNITY_IOS && LEVELPLAY_INSTALLED
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace Autech.LevelPlay.EditorTools
{
    /// <summary>
    /// Injects the App Tracking Transparency usage description into the built
    /// Xcode project's Info.plist — but ONLY when the project's VerifyLevelPlay
    /// configuration has ads + ATT enabled. Shipping the key in an ad-free build
    /// trips Apple's plist/App-Privacy cross-check ("app contains
    /// NSUserTrackingUsageDescription ... update your App Privacy response")
    /// and blocks review submission. SKAdNetwork ids are NOT handled here:
    /// LevelPlay 8.8.0+ writes SKAdNetworkItems to Info.plist, but only when the
    /// publisher enables the "SKAdNetwork IDs" feature in the LevelPlay Network
    /// Manager — it is opt-in, not automatic. See INSTALL.md for the publisher step.
    /// </summary>
    public static class AttInfoPlistPostprocessor
    {
        private const string UsageDescriptionKey = "NSUserTrackingUsageDescription";

        /// <summary>
        /// Override from another editor script before building if a different
        /// wording is needed; the default matches the previous AdMob setup.
        /// </summary>
        public static string UsageDescription =
            "This identifier will be used to deliver personalized ads and measure ad performance.";

        [PostProcessBuild(45)]
        public static void OnPostProcessBuild(BuildTarget buildTarget, string buildPath)
        {
            if (buildTarget != BuildTarget.iOS) return;

            var plistPath = Path.Combine(buildPath, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);

            if (ShouldInjectAttDescription())
            {
                plist.root.SetString(UsageDescriptionKey, UsageDescription);
                Debug.Log($"[Autech.LevelPlay] {UsageDescriptionKey} written to Info.plist (ads + ATT enabled).");
            }
            else
            {
                if (plist.root.values.Remove(UsageDescriptionKey))
                {
                    Debug.Log($"[Autech.LevelPlay] {UsageDescriptionKey} removed from Info.plist (ads or ATT disabled).");
                }
                else
                {
                    Debug.Log($"[Autech.LevelPlay] {UsageDescriptionKey} not injected (ads or ATT disabled).");
                }
            }

            plist.WriteToFile(plistPath);
        }

        /// <summary>
        /// True when any VerifyLevelPlay prefab has ads enabled AND an ATT prompt
        /// will be shown — either app-side (requestAttAuthorization) or by the
        /// InMobi CMP (cmpShowIdfaPopup, the default). Both need the usage string.
        /// </summary>
        private static bool ShouldInjectAttDescription()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                var bootstrap = prefab.GetComponentInChildren<VerifyLevelPlay>(true);
                if (bootstrap == null) continue;

                var serialized = new SerializedObject(bootstrap);
                var adsEnabled = serialized.FindProperty("adsEnabled");
                var requestAtt = serialized.FindProperty("requestAttAuthorization");
                var cmpIdfa = serialized.FindProperty("cmpShowIdfaPopup");

                // The ATT prompt may be presented either by the app (AttManager,
                // requestAttAuthorization) OR by the InMobi CMP (cmpShowIdfaPopup,
                // the package default). Either path REQUIRES the usage string in
                // Info.plist — without it the ATT prompt crashes at runtime.
                bool ads = adsEnabled != null && adsEnabled.boolValue;
                bool attByApp = requestAtt != null && requestAtt.boolValue;
                bool attByCmp = cmpIdfa != null && cmpIdfa.boolValue;
                if (ads && (attByApp || attByCmp))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
#endif // UNITY_IOS && LEVELPLAY_INSTALLED
