#if LEVELPLAY_INSTALLED
using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Autech.LevelPlay.EditorTools
{
    /// <summary>
    /// Fails the build when the local consent form is enabled but cannot actually
    /// render, rather than letting the problem reach a player's device.
    ///
    /// The form's labels are TextMeshPro. The package requires Unity 6 and
    /// com.unity.ugui 2.0.0, which ships the TMP code, so the type itself is
    /// guaranteed. The FONT is not: the asset those labels point at comes from TMP
    /// Essential Resources, a separate one-time import into the consumer's own
    /// project that Package Manager cannot express as a dependency.
    ///
    /// Without it the labels render nothing, so the player is asked to accept or
    /// decline text they cannot read. The runtime guard in LocalConsentManager
    /// refuses to show the form in that state, which is safe but means no consent is
    /// collected at all, so the useful place to catch it is at build time.
    ///
    /// Both conditions are checked: the type (belt and braces, in case the ugui
    /// floor is ever lowered again) and the resolved font.
    /// </summary>
    public class LocalConsentBuildCheck : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (!AnyPrefabUsesLocalConsent())
            {
                return;
            }

            if (!IsTextMeshProAvailable())
            {
                throw new BuildFailedException(
                    "[Autech.LevelPlay] 'Use Local Consent' is enabled but TextMeshPro is not in this project, " +
                    "so the consent form's labels cannot render and no consent would be collected. " +
                    "Install TextMeshPro, or untick Use Local Consent to use the InMobi CMP path instead.");
            }

            // The TMP package being present is not enough. The font asset the form's
            // labels point at comes from TMP Essential Resources, which is a separate
            // one-time import into the consumer's own project. Without it the labels
            // have no font and render nothing, which looks identical to TMP missing.
            if (!ConsentFormLabelsHaveFonts())
            {
                throw new BuildFailedException(
                    "[Autech.LevelPlay] 'Use Local Consent' is enabled but the consent form's labels have no font, " +
                    "so the form would render with no readable text. Import TMP Essential Resources " +
                    "(Window > TextMeshPro > Import TMP Essential Resources), or untick Use Local Consent.");
            }
        }

        /// <summary>
        /// Reflectively check the shipped consent-form prefab: at least one label must
        /// resolve a font asset. Reflection keeps this file free of a hard TextMeshPro
        /// dependency, which is the whole reason the check exists.
        /// </summary>
        private static bool ConsentFormLabelsHaveFonts()
        {
            var tmpType = FindTextMeshProType();
            if (tmpType == null)
            {
                return false;
            }

            var fontProperty = tmpType.GetProperty("font");
            if (fontProperty == null)
            {
                // Cannot introspect it, so do not block the build on a guess.
                return true;
            }

            foreach (var guid in AssetDatabase.FindAssets("AutechLocalConsentForm t:Prefab"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                var labels = prefab.GetComponentsInChildren(tmpType, true);
                if (labels == null || labels.Length == 0) continue;

                foreach (var label in labels)
                {
                    var font = fontProperty.GetValue(label) as UnityEngine.Object;
                    if (font != null)
                    {
                        return true;
                    }
                }

                return false;
            }

            // Prefab not found by name; leave it to the runtime guard rather than
            // failing a build for something that may not be this package's prefab.
            return true;
        }

        private static Type FindTextMeshProType()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var found = assembly.GetType("TMPro.TextMeshProUGUI");
                    if (found != null)
                    {
                        return found;
                    }
                }
                catch
                {
                    // Dynamic or reflection-only assemblies cannot be queried; skip.
                }
            }

            return null;
        }

        private static bool IsTextMeshProAvailable()
        {
            return FindTextMeshProType() != null;
        }

        private static bool AnyPrefabUsesLocalConsent()
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
                var useLocal = serialized.FindProperty("useLocalConsent");
                var showDialog = serialized.FindProperty("showConsentDialog");

                bool ads = adsEnabled != null && adsEnabled.boolValue;
                bool local = useLocal != null && useLocal.boolValue;
                bool dialog = showDialog != null && showDialog.boolValue;
                if (ads && local && dialog)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
#endif // LEVELPLAY_INSTALLED
