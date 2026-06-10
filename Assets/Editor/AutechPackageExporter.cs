#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Autech.LevelPlay.DevTools
{
    /// <summary>
    /// Dev-only tool (lives in the project's Assets/, NOT in the shipped package)
    /// that exports the embedded LevelPlay package to a versioned .unitypackage
    /// under /dist. See RELEASING.md.
    /// </summary>
    public static class AutechPackageExporter
    {
        const string PackageRoot = "Packages/com.autech.levelplay-mediation";

        [MenuItem("Tools/Autech/Export LevelPlay .unitypackage")]
        public static void Export()
        {
            var packageJsonPath = PackageRoot + "/package.json";
            if (!File.Exists(packageJsonPath))
            {
                Debug.LogError($"[Autech] package.json not found at {packageJsonPath}");
                return;
            }

            var version = ParseVersion(File.ReadAllText(packageJsonPath));
            var outDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "dist"));
            Directory.CreateDirectory(outDir);
            var outPath = Path.Combine(outDir, $"com.autech.levelplay-mediation-{version}.unitypackage");

            AssetDatabase.ExportPackage(PackageRoot, outPath, ExportPackageOptions.Recurse);
            Debug.Log($"[Autech] Exported v{version} -> {outPath}");
            EditorUtility.RevealInFinder(outPath);
        }

        // Minimal extractor for the JSON "version" field — avoids pulling a JSON lib.
        static string ParseVersion(string json)
        {
            const string key = "\"version\"";
            var i = json.IndexOf(key, System.StringComparison.Ordinal);
            if (i < 0) return "unknown";
            i = json.IndexOf(':', i) + 1;
            var start = json.IndexOf('"', i) + 1;
            var end = json.IndexOf('"', start);
            return (start <= 0 || end <= start) ? "unknown" : json.Substring(start, end - start);
        }
    }
}
#endif
