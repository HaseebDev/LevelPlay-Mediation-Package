#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace ChoiceCMPInternal.Editor.Postbuild
{
    /// <summary>
    /// Some LevelPlay/CMP CocoaPods resolve to dynamic frameworks. EDM4U's default iOS
    /// Podfile uses <c>use_frameworks! :linkage =&gt; :static</c>, under which CocoaPods
    /// can link those frameworks via <c>@rpath</c> without generating an "Embed Pods
    /// Frameworks" phase - so the required frameworks are not copied into the app bundle.
    /// The app
    /// then crashes on launch with:
    ///   dyld: Library not loaded: @rpath/FBAudienceNetwork.framework/FBAudienceNetwork
    ///
    /// This post-processor adds a build phase that copies the known dynamic mediation
    /// frameworks out of CocoaPods into the app's Frameworks folder and code-signs them at
    /// Xcode build time, after `pod install` has run.
    /// </summary>
    public static class ChoiceCMPFrameworkEmbed
    {
        private const string PhaseName = "[Autech] Embed LevelPlay dynamic frameworks";

        [PostProcessBuild(10100)]
        public static void OnPostprocessBuild(BuildTarget buildTarget, string buildPath)
        {
            if (buildTarget != BuildTarget.iOS)
                return;

            var projPath = Path.Combine(buildPath, "Unity-iPhone.xcodeproj/project.pbxproj");
            if (!File.Exists(projPath))
                return;

            var project = new PBXProject();
            project.ReadFromFile(projPath);

            string mainTarget =
#if UNITY_2019_3_OR_NEWER
                project.GetUnityMainTargetGuid();
#else
                project.TargetGuidByName("Unity-iPhone");
#endif

            // Idempotent: don't add the phase twice (e.g. on Append builds).
            var pbxText = File.ReadAllText(projPath);
            if (pbxText.Contains(PhaseName))
                return;

            // Runs at Xcode build time — PODS_ROOT / TARGET_BUILD_DIR are resolved then,
            // regardless of when this phase was added relative to `pod install`.
            const string script =
                "set -e\n" +
                "DEST=\"${TARGET_BUILD_DIR}/${FRAMEWORKS_FOLDER_PATH}\"\n" +
                "mkdir -p \"$DEST\"\n" +
                "embed_framework() {\n" +
                "  pod_name=\"$1\"\n" +
                "  framework_name=\"$2\"\n" +
                "  FW=\"${PODS_XCFRAMEWORKS_BUILD_DIR}/${pod_name}/${framework_name}.framework\"\n" +
                "  if [ ! -d \"$FW\" ]; then\n" +
                "    FWX=\"\"\n" +
                "    for candidate in \\\n" +
                "      \"${PODS_ROOT}/${pod_name}/${framework_name}.xcframework\" \\\n" +
                "      \"${PODS_ROOT}/${pod_name}/Dynamic/${framework_name}.xcframework\" \\\n" +
                "      \"${PODS_ROOT}/${pod_name}/XCFrameworks/${framework_name}.xcframework\"; do\n" +
                "      if [ -d \"$candidate\" ]; then FWX=\"$candidate\"; break; fi\n" +
                "    done\n" +
                "    if [ -z \"$FWX\" ]; then echo \"[Autech] ${framework_name}.xcframework not found; skipping embed\"; return 0; fi\n" +
                "    if [ \"${PLATFORM_NAME}\" = \"iphonesimulator\" ]; then\n" +
                "      SLICE=$(ls -d \"$FWX\"/*simulator* 2>/dev/null | head -1)\n" +
                "    else\n" +
                "      SLICE=\"$FWX/ios-arm64\"\n" +
                "    fi\n" +
                "    FW=\"$SLICE/${framework_name}.framework\"\n" +
                "  fi\n" +
                "  if [ ! -d \"$FW\" ]; then echo \"[Autech] ${framework_name}.framework slice not found at $FW\"; return 0; fi\n" +
                "  /usr/bin/rsync -a --delete \"$FW\" \"$DEST/\"\n" +
                "  if [ \"${CODE_SIGNING_ALLOWED}\" = \"YES\" ] && [ -n \"${EXPANDED_CODE_SIGN_IDENTITY}\" ]; then\n" +
                "    /usr/bin/codesign --force --sign \"${EXPANDED_CODE_SIGN_IDENTITY}\" --preserve-metadata=identifier,entitlements,flags \"$DEST/${framework_name}.framework\"\n" +
                "  fi\n" +
                "  echo \"[Autech] Embedded ${framework_name}.framework into $DEST\"\n" +
                "}\n" +
                "embed_framework InMobiCMP InMobiCMP\n" +
                "embed_framework InMobiSDK InMobiSDK\n" +
                "embed_framework FBAudienceNetwork FBAudienceNetwork\n";

            project.AddShellScriptBuildPhase(mainTarget, PhaseName, "/bin/sh", script);
            File.WriteAllText(projPath, project.WriteToString());
        }
    }
}
#endif
