#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Autech.LevelPlay.DevTools
{
    /// <summary>
    /// Dev-only CI helper (lives in Assets/Editor, NOT shipped with the package).
    /// Drives a headless iOS Xcode-project generation so the InMobiCMP iOS pod
    /// integration can be verified with a real xcodebuild compile/link on a Mac.
    ///
    /// Invoke from the command line, e.g.:
    ///   Unity -batchmode -quit -nographics -buildTarget iOS \
    ///         -projectPath &lt;proj&gt; -logFile &lt;log&gt; \
    ///         -executeMethod Autech.LevelPlay.DevTools.AutechCIBuild.BuildiOS \
    ///         -autechBuildPath &lt;outDir&gt;
    /// Exits 0 on success, non-zero on any failure (so CI/the shell can branch).
    /// </summary>
    public static class AutechCIBuild
    {
        public static void BuildiOS()
        {
            try
            {
                string outPath = GetArg("-autechBuildPath") ?? "build/iOS";

                var scenes = EditorBuildSettings.scenes
                    .Where(s => s.enabled)
                    .Select(s => s.path)
                    .ToArray();
                if (scenes.Length == 0)
                {
                    scenes = new[] { "Assets/AutechLevelPlay/Scene/ExampleLevelPlayScene.unity" };
                }

                Debug.Log($"[AutechCI] activeBuildTarget={EditorUserBuildSettings.activeBuildTarget} " +
                          $"scenes={scenes.Length} out={outPath}");

                var opts = new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = outPath,
                    target = BuildTarget.iOS,
                    targetGroup = BuildTargetGroup.iOS,
                    // Development build so this matches what a publisher would test;
                    // also makes the on-device TEST MODE banner / logs available.
                    options = BuildOptions.Development,
                };

                BuildReport report = BuildPipeline.BuildPlayer(opts);
                BuildSummary summary = report.summary;

                Debug.Log($"[AutechCI] result={summary.result} " +
                          $"errors={summary.totalErrors} warnings={summary.totalWarnings} " +
                          $"time={summary.totalTime} output={summary.outputPath}");

                if (summary.result != BuildResult.Succeeded)
                {
                    foreach (var step in report.steps)
                    {
                        foreach (var msg in step.messages)
                        {
                            if (msg.type == LogType.Error || msg.type == LogType.Exception)
                            {
                                Debug.LogError($"[AutechCI] {step.name}: {msg.content}");
                            }
                        }
                    }
                    EditorApplication.Exit(2);
                    return;
                }

                Debug.Log("[AutechCI] iOS Xcode project generation SUCCEEDED.");
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError($"[AutechCI] Exception during build: {e}");
                EditorApplication.Exit(3);
            }
        }

        private static string GetArg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name) return args[i + 1];
            }
            return null;
        }
    }
}
#endif
