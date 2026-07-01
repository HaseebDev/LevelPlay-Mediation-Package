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
    /// InMobiCMP ships as a **dynamic** framework (it's Swift). EDM4U's default iOS
    /// Podfile uses <c>use_frameworks! :linkage =&gt; :static</c>, under which CocoaPods
    /// links InMobiCMP via <c>@rpath</c> but does NOT generate an "Embed Pods Frameworks"
    /// phase — so <c>InMobiCMP.framework</c> is never copied into the app bundle. The app
    /// then crashes on launch with:
    ///   dyld: Library not loaded: @rpath/InMobiCMP.framework/InMobiCMP
    ///
    /// This post-processor adds a build phase that copies InMobiCMP.framework out of the
    /// resolved CocoaPods xcframework into the app's Frameworks folder and code-signs it at
    /// Xcode build time (so it works for device + simulator, and after `pod install` has run).
    /// IronSource is a static framework and needs no embedding.
    /// </summary>
    public static class ChoiceCMPFrameworkEmbed
    {
        private const string PhaseName = "[Autech] Embed InMobiCMP.framework";

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
                "FWX=\"${PODS_ROOT}/InMobiCMP/InMobiCMP.xcframework\"\n" +
                "if [ ! -d \"$FWX\" ]; then echo \"[Autech] InMobiCMP.xcframework not found under PODS_ROOT; skipping embed\"; exit 0; fi\n" +
                "if [ \"${PLATFORM_NAME}\" = \"iphonesimulator\" ]; then\n" +
                "  SLICE=$(ls -d \"$FWX\"/*simulator* 2>/dev/null | head -1)\n" +
                "else\n" +
                "  SLICE=\"$FWX/ios-arm64\"\n" +
                "fi\n" +
                "FW=\"$SLICE/InMobiCMP.framework\"\n" +
                "if [ ! -d \"$FW\" ]; then echo \"[Autech] InMobiCMP.framework slice not found at $FW\"; exit 0; fi\n" +
                "DEST=\"${TARGET_BUILD_DIR}/${FRAMEWORKS_FOLDER_PATH}\"\n" +
                "mkdir -p \"$DEST\"\n" +
                "/usr/bin/rsync -a --delete \"$FW\" \"$DEST/\"\n" +
                "if [ \"${CODE_SIGNING_ALLOWED}\" = \"YES\" ] && [ -n \"${EXPANDED_CODE_SIGN_IDENTITY}\" ]; then\n" +
                "  /usr/bin/codesign --force --sign \"${EXPANDED_CODE_SIGN_IDENTITY}\" --preserve-metadata=identifier,entitlements,flags \"$DEST/InMobiCMP.framework\"\n" +
                "fi\n" +
                "echo \"[Autech] Embedded InMobiCMP.framework into $DEST\"\n";

            project.AddShellScriptBuildPhase(mainTarget, PhaseName, "/bin/sh", script);
            File.WriteAllText(projPath, project.WriteToString());
        }
    }
}
#endif
