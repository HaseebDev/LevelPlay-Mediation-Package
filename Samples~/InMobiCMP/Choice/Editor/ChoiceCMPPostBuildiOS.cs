#if UNITY_IOS
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace ChoiceCMPInternal.Editor.Postbuild
{
    public static class ChoiceCMPPostBuildiOS
    {
        [PostProcessBuild(10001)]
        public static void OnPostprocessBuild(BuildTarget buildTarget, string buildPath)
        {
            if (buildTarget != BuildTarget.iOS)
                return;

            PrepareProject(buildPath);

            // NOTE: We intentionally do NOT inject NSLocationWhenInUseUsageDescription.
            // InMobi's iOS CMP does not require a location usage string (only
            // NSUserTrackingUsageDescription, handled by AttInfoPlistPostprocessor when
            // ATT is enabled). Shipping a location key forces a location entry into App
            // Privacy and invites App Review questions for an app that never requests
            // location. If a publisher genuinely needs location, they add the key in Xcode.
        }

        private static void PrepareProject(string buildPath)
        {
            var projPath = Path.Combine(buildPath, "Unity-iPhone.xcodeproj/project.pbxproj");
            var project = new PBXProject();
            project.ReadFromString(File.ReadAllText(projPath));

            var targets = GetTargets(project);
            // The ChoiceCMP iOS SDK now includes Swift, so these properties ensure Xcode handles that properly.
            project.UpdateBuildProperty(targets, "SWIFT_VERSION", new[] { "5.0" }, null);
            new[] {
                "GCC_ENABLE_OBJC_EXCEPTIONS"
            }.ToList().ForEach(name =>
                project.UpdateBuildProperty(targets, name, Yes, No));
            var mainTarget = GetMainTarget(project);
            string guid = project.TargetGuidByName(mainTarget);

            project.UpdateBuildProperty(mainTarget, "OTHER_LDFLAGS", objcFlag, Yes);

            File.WriteAllText(projPath, project.WriteToString());
        }

        #region Helpers

        private static readonly string[] Yes = { "YES" };
        private static readonly string[] No = { "NO" };
        private static readonly string[] objcFlag = { "-ObjC" };

        private static IEnumerable<string> GetTargets(PBXProject project)
        {
            var targets = new[] {
                project.ProjectGuid(),
                GetMainTarget(project),
                GetUnityFrameworkTarget(project)
            };
            return targets.Where(target => target != null);
        }

        private static string GetMainTarget(PBXProject project)
        {
            return
#if UNITY_2019_3_OR_NEWER
                project.GetUnityMainTargetGuid()
#else
                project.TargetGuidByName("Unity-iPhone")
#endif
                ;
        }

        private static string GetUnityFrameworkTarget(PBXProject project)
        {
            return
#if UNITY_2019_3_OR_NEWER
                project.GetUnityFrameworkTargetGuid()
#else
                project.TargetGuidByName("UnityFramework")
#endif
                ;
        }

        #endregion
    }
}
#endif
