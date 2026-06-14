#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Autech.LevelPlay.DevTools
{
    /// <summary>
    /// Dev-only release tooling (lives in Assets/Editor, NOT shipped with the package).
    ///
    /// Repo layout (mirrors the Autech AdMob package):
    ///   • Assets/AutechLevelPlay/  — the editable / testable working copy (source of truth)
    ///   • repo root Runtime/, Editor/, Samples~/ — the distributed UPM package mirror
    ///
    /// Release flow: edit in Assets/AutechLevelPlay → "Sync dev copy → root package" →
    /// "Export .unitypackage" → commit, tag, attach the .unitypackage to a GitHub Release.
    /// See RELEASING.md.
    /// </summary>
    public static class AutechPackageExporter
    {
        const string DevRoot   = "Assets/AutechLevelPlay";
        const string RepoRoot  = ""; // resolved from Application.dataPath/..

        [MenuItem("Tools/Autech/Export .unitypackage")]
        public static void Export()
        {
            if (!Directory.Exists(DevRoot))
            {
                Debug.LogError($"[Autech] {DevRoot} not found.");
                return;
            }
            var version = ReadVersion();
            var outDir = AbsRepoPath("releases");
            Directory.CreateDirectory(outDir);
            var outPath = Path.Combine(outDir, $"com.autech.levelplay-mediation-{version}.unitypackage");

            AssetDatabase.ExportPackage(DevRoot, outPath, ExportPackageOptions.Recurse);
            Debug.Log($"[Autech] Exported v{version} -> {outPath}");
            EditorUtility.RevealInFinder(outPath);
        }

        /// <summary>
        /// Copies the editable Assets/AutechLevelPlay copy into the repo-root package mirror
        /// (Runtime/, Editor/, Samples~/) that consumers install via git URL / OpenUPM.
        /// </summary>
        [MenuItem("Tools/Autech/Sync dev copy → root package")]
        public static void SyncToRoot()
        {
            // Assets/AutechLevelPlay (flattened) -> root Runtime/{asmdef,Scripts,Plugins}
            CopyInto($"{DevRoot}/Autech.LevelPlay.Runtime.asmdef",      "Runtime/Autech.LevelPlay.Runtime.asmdef");
            CopyInto($"{DevRoot}/Autech.LevelPlay.Runtime.asmdef.meta", "Runtime/Autech.LevelPlay.Runtime.asmdef.meta");
            CopyTree($"{DevRoot}/Scripts", "Runtime/Scripts");
            CopyTree($"{DevRoot}/Plugins", "Runtime/Plugins");
            CopyTree($"{DevRoot}/Editor",  "Editor");

            // The example UI is a sample, not core — it ships under
            // Samples~/ExampleScene, never in the distributed Runtime assembly.
            DeleteRepoFile("Runtime/Scripts/AdsExampleUI.cs");
            DeleteRepoFile("Runtime/Scripts/AdsExampleUI.cs.meta");

            Debug.Log("[Autech] Synced Assets/AutechLevelPlay -> root package (Runtime/, Editor/). " +
                      "Review the example scene under Samples~ manually if it changed.");
        }

        // ---- helpers -------------------------------------------------------

        static string AbsRepoPath(string rel) =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "..", rel));

        static void DeleteRepoFile(string rel)
        {
            var p = AbsRepoPath(rel);
            if (File.Exists(p)) File.Delete(p);
        }

        static void CopyInto(string assetRelPath, string repoRelPath)
        {
            var src = AbsRepoPath(assetRelPath);
            var dst = AbsRepoPath(repoRelPath);
            Directory.CreateDirectory(Path.GetDirectoryName(dst));
            File.Copy(src, dst, overwrite: true);
        }

        static void CopyTree(string assetRelDir, string repoRelDir)
        {
            var src = AbsRepoPath(assetRelDir);
            var dst = AbsRepoPath(repoRelDir);
            if (Directory.Exists(dst)) Directory.Delete(dst, recursive: true);
            DirCopy(src, dst);
        }

        static void DirCopy(string src, string dst)
        {
            Directory.CreateDirectory(dst);
            foreach (var f in Directory.GetFiles(src))
                File.Copy(f, Path.Combine(dst, Path.GetFileName(f)), overwrite: true);
            foreach (var d in Directory.GetDirectories(src))
                DirCopy(d, Path.Combine(dst, Path.GetFileName(d)));
        }

        static string ReadVersion()
        {
            var json = File.ReadAllText(AbsRepoPath("package.json"));
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
