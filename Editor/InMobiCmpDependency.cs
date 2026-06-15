#if UNITY_EDITOR
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.PackageManager.UI;
using UnityEngine;

namespace Autech.LevelPlay.EditorTools
{
    /// <summary>
    /// Treats the <b>InMobi CMP (Choice)</b> plugin as a soft dependency of this
    /// package. The plugin ships bundled as the package sample "InMobi CMP"
    /// (<c>Samples~/InMobiCMP</c>); on Editor load this checks whether it has
    /// been imported and, if not, offers a one-click import.
    ///
    /// Why a sample and not a hard UPM dependency: the InMobi CMP plugin has no
    /// asmdef, so its <c>ChoiceCMP</c> type only resolves when the files live
    /// under the consumer's <c>Assets/</c> (compiled into <c>Assembly-CSharp</c>,
    /// where <see cref="Autech.LevelPlay.ConsentManager"/> looks for it by
    /// reflection). Importing the sample copies the files into <c>Assets/</c>,
    /// satisfying that requirement. The package compiles fine with or without it.
    /// </summary>
    [InitializeOnLoad]
    internal static class InMobiCmpDependency
    {
        private const string PackageName   = "com.autech.levelplay-mediation";
        private const string SampleName    = "InMobi CMP";
        private const string DocsUrl       = "https://support.inmobi.com/choice/other-resources/unity-app-implementation-sdk/";
        private const string DismissPrefKey = "Autech.LevelPlay.InMobiCmpPrompt.Dismissed";
        private const string SessionDeferKey = "Autech.LevelPlay.InMobiCmpPrompt.DeferredThisSession";

        static InMobiCmpDependency()
        {
            // Defer until the Editor is idle so we never prompt mid-compile/import.
            EditorApplication.delayCall += MaybePrompt;
        }

        /// <summary>True when the InMobi CMP plugin is present in the project.</summary>
        private static bool IsCmpPresent()
        {
            // Matches ConsentManager's lookup, but assembly-agnostic so it also
            // succeeds if the plugin was given an asmdef.
            if (Type.GetType("ChoiceCMP, Assembly-CSharp") != null) return true;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try { if (asm.GetType("ChoiceCMP") != null) return true; }
                catch { /* dynamic/reflection-only assemblies — ignore */ }
            }
            return false;
        }

        private static void MaybePrompt()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += MaybePrompt; // try again once settled
                return;
            }

            if (IsCmpPresent()) return;
            if (SessionState.GetBool(SessionDeferKey, false)) return;
            if (EditorPrefs.GetBool(DismissPrefKey, false)) return;

            var choice = EditorUtility.DisplayDialogComplex(
                "Autech LevelPlay — InMobi CMP required",
                "This package uses InMobi CMP (Choice) for GDPR / IAB TCF v2.2 consent, " +
                "but it isn't imported yet. Without it the package still builds and serves " +
                "ads, but it cannot show a consent prompt in GDPR regions.\n\n" +
                "Import the bundled InMobi CMP plugin now?",
                "Import",          // 0
                "Later",           // 1
                "Don't ask again"); // 2

            switch (choice)
            {
                case 0: ImportBundledSample(); break;
                case 1: SessionState.SetBool(SessionDeferKey, true); break;
                case 2: EditorPrefs.SetBool(DismissPrefKey, true); break;
            }
        }

        /// <summary>Import the bundled "InMobi CMP" sample into Assets/Samples/…</summary>
        [MenuItem("Tools/Autech/Import InMobi CMP (consent)")]
        public static void ImportBundledSample()
        {
            if (IsCmpPresent())
            {
                EditorUtility.DisplayDialog("InMobi CMP",
                    "InMobi CMP is already present in this project.", "OK");
                return;
            }

            try
            {
                // version "" → samples for the package's installed version.
                var sample = Sample.FindByPackage(PackageName, string.Empty)
                    .FirstOrDefault(s => s.displayName == SampleName);

                if (sample.Equals(default(Sample)) || string.IsNullOrEmpty(sample.resolvedPath))
                {
                    OfferManualFallback();
                    return;
                }

                if (sample.Import(Sample.ImportOptions.OverridePreviousImports))
                {
                    Debug.Log("[Autech.LevelPlay] Imported InMobi CMP. Set your CMP p-code on the " +
                              "VerifyandInitializeLevelPlay prefab (Inspector → Consent & Privacy).");
                    // The InMobi plugin needs the Android Material Components + Gson
                    // libraries (declared in the sample's ChoiceCMPDependencies.xml).
                    // Trigger the resolver so consumers never hit the AAPT link error
                    // or the runtime Gson NoClassDefFoundError crash on first launch.
                    EditorApplication.delayCall += TriggerAndroidDependencyResolution;
                }
                else
                    OfferManualFallback();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Autech.LevelPlay] Could not import the bundled InMobi CMP sample: {e.Message}");
                OfferManualFallback();
            }
        }

        /// <summary>
        /// Best-effort kick of the External Dependency Manager (EDM4U, shipped with
        /// the LevelPlay SDK) so the InMobi CMP Android dependencies
        /// (Material Components + Gson) are fetched into the Gradle build. Without
        /// them the Android build fails at resource linking and the app crashes on
        /// the first consent call. If EDM isn't found, falls back to a clear
        /// instruction. Safe no-op on failure.
        /// </summary>
        private static void TriggerAndroidDependencyResolution()
        {
            try
            {
                Type resolver = Type.GetType("GooglePlayServices.PlayServicesResolver, Google.JarResolver");
                if (resolver == null)
                {
                    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try { resolver = asm.GetType("GooglePlayServices.PlayServicesResolver"); }
                        catch { resolver = null; }
                        if (resolver != null) break;
                    }
                }

                var resolve = resolver?.GetMethod("MenuForceResolve", BindingFlags.Public | BindingFlags.Static);
                if (resolve != null)
                {
                    resolve.Invoke(null, null);
                    Debug.Log("[Autech.LevelPlay] Resolving InMobi CMP Android dependencies " +
                              "(Material Components + Gson) via the External Dependency Manager.");
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Autech.LevelPlay] Auto dependency-resolution couldn't run ({e.Message}).");
            }

            Debug.LogWarning("[Autech.LevelPlay] If the Android build fails to link " +
                             "(Theme.Design.BottomSheetDialog) or crashes on launch (Gson), run " +
                             "Assets → External Dependency Manager → Android Resolver → Force Resolve.");
        }

        private static void OfferManualFallback()
        {
            if (EditorUtility.DisplayDialog("InMobi CMP",
                    "Couldn't auto-import the bundled InMobi CMP sample (the package may have " +
                    "been installed as a .unitypackage rather than via Package Manager).\n\n" +
                    "Import it from Package Manager → Autech LevelPlay Mediation → Samples → " +
                    "InMobi CMP, or open InMobi's docs.",
                    "Open docs", "Close"))
            {
                Application.OpenURL(DocsUrl);
            }
        }
    }
}
#endif
