# iOS v1.1.1 Handoff (read me first)

> Temporary dev handoff for finishing iOS verification on a Mac. Delete this file
> once v1.1.1 ships. This is NOT shipped to consumers (root dev doc).

You are continuing work on the **Autech LevelPlay Mediation** Unity package. A
prior session (on Windows) shipped **v1.1.0** and then did an iOS review. iOS
changes need a real Mac/Xcode build to verify, which is why this was handed off.

## Repo / project facts
- Repo: `https://github.com/HaseebDev/LevelPlay-Mediation-Package` (branch `main`).
- This repo is **both** the Unity dev project (`Assets/`, `ProjectSettings/`,
  `Packages/`) **and** the distributed UPM package mirror (root `Runtime/`,
  `Editor/`, `Samples~/`, `package.json`).
- Unity version: **6000.4.6f1** (install the same on the Mac).
- **Two source trees kept in sync** — always edit BOTH, or edit `Assets/` then run
  the sync tool:
  - Dev/testable copy: `Assets/AutechLevelPlay/...` and `Assets/InMobi/...`
  - Shipped mirror: `Runtime/`, `Editor/`, `Samples~/InMobiCMP/`, `Samples~/ExampleScene/`, `Samples~/Prefabs/`
- Release tooling (menu): **Tools ▸ Autech ▸ Sync dev copy → root package** and
  **Tools ▸ Autech ▸ Export .unitypackage** (writes `releases/*.unitypackage`).
- `package.json` is currently `1.1.0`. Bump to `1.1.1` when shipping these fixes.

## First steps on the Mac
1. `git pull origin main` (gets this file + all v1.1.0 work).
2. Open the project in Unity 6000.4.6f1; let it reimport.
3. Switch Build Target to **iOS** (File ▸ Build Settings ▸ iOS ▸ Switch Platform).
4. If the `unity-mcp` MCP server is configured, use it to drive Unity; otherwise
   do the menu/editor steps manually. CocoaPods must be installed (`pod --version`).

## The iOS findings to implement (all internet-verified — see sources at bottom)

### P1 — BUILD-BREAKER: InMobi CMP iOS native SDK is not integrated
The iOS binding imports the native framework but nothing provides it:
```objc
// Assets/InMobi/Choice/Plugins/iOS/ChoiceCMPManager.mm  (and .h, ChoiceCMPUtils)
#import <InMobiCMP/InMobiCMP-Swift.h>
```
There is no `.xcframework`, no `.a`, no Podfile entry, and `ChoiceCMPDependencies.xml`
only declares `androidPackages`. An iOS build will fail: `'InMobiCMP/InMobiCMP-Swift.h'
file not found`. InMobi's documented Unity path is a **manual framework download**, but
the **CocoaPod `InMobiCMP` exists** and EDM4U (shipped with LevelPlay) resolves iOS pods —
which is the clean fit here since everything else uses EDM.

**Fix:** add an `<iosPods>` block to **both** copies of `ChoiceCMPDependencies.xml`:
- `Assets/InMobi/Choice/Editor/ChoiceCMPDependencies.xml`
- `Samples~/InMobiCMP/Choice/Editor/ChoiceCMPDependencies.xml`

```xml
<dependencies>
  <androidPackages>
    <androidPackage spec="com.google.android.material:material:1.12.0" />
    <androidPackage spec="com.google.code.gson:gson:2.10.1" />
    <androidPackage spec="androidx.preference:preference:1.2.1" />
    <androidPackage spec="androidx.constraintlayout:constraintlayout:2.1.4" />
  </androidPackages>
  <iosPods>
    <iosPod name="InMobiCMP" version="2.2.0" />
  </iosPods>
</dependencies>
```
**VERIFY THE VERSION.** `2.2.0` is what InMobi's current iOS docs reference, but the
bundled Unity binding came from plugin v2.0.3. If the build hits Swift API errors,
try the InMobiCMP pod version matching the binding (check the InMobi iOS changelog:
https://support.inmobi.com/choice/changelog/ios-changelog-cmp) and adjust. Run
`Assets ▸ External Dependency Manager ▸ iOS Resolver ▸ Install Cocoapods` if prompted.

### P2 — Regulatory: remove the location usage string injected by default
`Assets/InMobi/Choice/Editor/ChoiceCMPPostBuildiOS.cs` (and the `Samples~` copy)
unconditionally injects `NSLocationWhenInUseUsageDescription`. InMobi's iOS docs do
**NOT** require it (only `NSUserTrackingUsageDescription`, for IDFA). Shipping it forces
a location entry into App Privacy and invites App Review questions.

**Fix:** delete the location-string block in `ChoiceCMPPostBuildiOS.OnPostprocessBuild`
(the `const string locationKey = "NSLocationWhenInUseUsageDescription"` ... `plist.WriteToFile`
section). Apply to BOTH the `Assets/InMobi/...` and `Samples~/InMobiCMP/...` copies.

### SKAdNetwork — fix the inaccurate comment + document the opt-in
`Assets/AutechLevelPlay/Editor/AttInfoPlistPostprocessor.cs` (+ `Editor/` mirror) has a
comment saying "LevelPlay 9.1.0+ manages them automatically." Verified WRONG: it's
**8.8.0+** and **opt-in** — the publisher must enable **SKAdNetwork IDs** in the
**LevelPlay Network Manager** for `SKAdNetworkItems` to be written to Info.plist.

**Fix:** correct the comment to "8.8.0+, enabled via the LevelPlay Network Manager
(SKAdNetwork IDs feature)"; add an INSTALL.md/README note telling publishers to enable it.

### Export compliance + IAB GPP — DOCUMENT ONLY, do not inject
- Do **NOT** auto-set `ITSAppUsesNonExemptEncryption`. It's an app-wide legal
  declaration the publisher owns. Just document that the AES RemoveAds storage is
  typically export-exempt and they may set the key themselves.
- `com.iabgpp:iabgpp-encoder:3.2.3` is listed by InMobi's docs but a bytecode scan of
  `InMobi-CMP-2.4.2.aar` shows ZERO references and Android works without it. Only add it
  (to `androidPackages`) if you enable US-state / GPP regulations in your CMP property.

### Already correct (no change)
- ATT requested before LevelPlay init (`AdsManager.InitializeAsync`). ✅
- `NSUserTrackingUsageDescription` injected conditionally (ads+ATT on) by
  `AttInfoPlistPostprocessor`. ✅
- Privacy manifests: LevelPlay + Unity Core ship their own; the InMobiCMP pod ships its
  own; the package's tiny ATT plugin uses no required-reason APIs. ✅
- Android deps meet/exceed InMobi's documented versions. ✅

## Build + verify on device (iOS)
1. Apply P1 + P2 + SKAN-comment edits (both trees). Edit `Assets/` copies, then run
   **Tools ▸ Autech ▸ Sync dev copy → root package** to mirror to `Runtime/Editor/Samples~`.
   (The example scene/prefabs aren't synced by that tool — they're already correct from v1.1.0.)
2. Force-resolve: **Assets ▸ External Dependency Manager ▸ iOS Resolver ▸ Force Resolve**
   (and Android Resolver ▸ Force Resolve). Confirm no compile errors.
3. Build the Xcode project (File ▸ Build Settings ▸ iOS ▸ Build). Open the generated
   `.xcworkspace` (CocoaPods creates a workspace). Confirm the Podfile contains
   `pod 'InMobiCMP'` and `pod install` succeeded.
4. Run on a connected iOS device. Watch the device log (Xcode ▸ Window ▸ Devices and
   Simulators ▸ View Device Logs, or `xcrun simctl`/Console.app, or `idevicesyslog`).
5. Verify, when the first scene loads:
   - No crash on the consent step (the Android analog was `NoClassDefFoundError`; iOS
     analog would be a missing-symbol/dyld crash for `InMobiCMP`).
   - `[Autech.LevelPlay]` logs show: ATT prompt → consent (InMobi CMP) → LevelPlay init
     succeeded → ad units load.
   - The **TEST MODE ON** banner + advertising ID log appear (dev build), and the
     on-screen **Launch Test Suite** button works.
   - In the built `Info.plist`: `NSUserTrackingUsageDescription` present (ATT on),
     `NSLocationWhenInUseUsageDescription` ABSENT (P2), and `SKAdNetworkItems` present
     IF you enabled SKAdNetwork IDs in the LevelPlay Network Manager.

## Ship v1.1.1 (only after device verification passes)
1. Bump `package.json` `version` → `1.1.1`; update `CHANGELOG.md` (new `[1.1.1]` section
   describing the iOS native-SDK integration + location-string removal + SKAN docs),
   and `INSTALL.md`/`README.md` per the doc notes above. Update the two `#v1.1.0` refs in
   `INSTALL.md` to `#v1.1.1`.
2. **Tools ▸ Autech ▸ Export .unitypackage** → `releases/com.autech.levelplay-mediation-1.1.1.unitypackage`.
3. Commit (write the message as the user's own — NO "Co-Authored-By"/AI trailer; this is a
   hard repo rule), push `main`.
4. `git tag v1.1.1 && git push origin v1.1.1`.
5. `gh release create v1.1.1 --title "v1.1.1" --notes-file <notes> --latest releases/com.autech.levelplay-mediation-1.1.1.unitypackage`.
6. Verify the release asset attached (`gh release view v1.1.1`).

## Heads-up / open product decisions (carried over from v1.1.0)
- The shipped sample prefab (`Samples~/Prefabs/VerifyandInitializeLevelPlay.prefab`)
  contains the owner's REAL test App Key (`1cc894335`) + test ad-unit IDs. Intentional
  per the owner; flag if going broadly public.
- `package.json` min Unity is `2021.3`, but code uses `FindAnyObjectByType` (2021.3.18f1+).
  Consider bumping min to 2022.3 LTS.

## Sources (verified)
- EDM4U iosPods syntax: https://github.com/googlesamples/unity-jar-resolver
- InMobi CMP Unity: https://support.inmobi.com/choice/other-resources/unity-app-implementation-sdk/
- InMobi CMP iOS: https://support.inmobi.com/choice/other-resources/ios-app-implementation-sdk/
- InMobi CMP iOS changelog: https://support.inmobi.com/choice/changelog/ios-changelog-cmp
- Apple ITSAppUsesNonExemptEncryption: https://developer.apple.com/documentation/bundleresources/information-property-list/itsappusesnonexemptencryption
- LevelPlay iOS privacy/SKAdNetwork: https://docs.unity.com/en-us/grow/levelplay/sdk/ios/privacy-settings-configurations
