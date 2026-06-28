# Changelog

## [Unreleased]

## [1.1.1] - 2026-06-28

### Added
- **Debug panel privacy/consent snapshot.** The demo scene's on-screen debug log now
  dumps a full snapshot once the SDK is up (and again after the privacy form): ATT
  trigger + status, CCPA/COPPA flags, and the IAB-TCF values the CMP wrote to native
  storage (gdprApplies, consent type, Purpose 1/3, PurposeConsents, VendorConsents,
  TC string, US-Privacy/GPP), plus the device advertising ID (IDFA/GAID). Lets you
  verify on device that consent + ATT were actually grabbed. New public
  `AdsManager.GetPrivacyDebugSnapshot()` / `RequestAdvertisingId()`.

### Changed
- **ATT is now owned by the InMobi CMP (single source of truth).** `cmpShowIdfaPopup`
  defaults ON and the app-side `AttManager` prompt (`requestAttAuthorization`) defaults
  OFF, so the ATT "Allow tracking" popup is presented by the CMP as part of its consent
  flow instead of by a separate app-side step. `AttManager` remains as a read-only ATT
  status source (and an opt-in fallback). `AttInfoPlistPostprocessor` now injects
  `NSUserTrackingUsageDescription` whenever **either** ATT path is enabled, so the CMP's
  prompt always has its required usage string.

### Fixed
- **InMobi CMP iOS build failure — missing native SDK.** The iOS binding
  (`Plugins/iOS/ChoiceCMPManager.mm`) imports `<InMobiCMP/InMobiCMP-Swift.h>`, but
  nothing provided the native framework: no `.xcframework`, no Podfile entry, and
  `ChoiceCMPDependencies.xml` declared only `androidPackages`. An iOS build failed
  with `'InMobiCMP/InMobiCMP-Swift.h' file not found`. Added an `<iosPods>` block
  declaring the **`InMobiCMP`** CocoaPod (pinned to **2.4.2**, matching the bundled
  Android native `InMobi-CMP-2.4.2.aar` so both platforms run the same CMP SDK).
  EDM4U's iOS Resolver now writes `pod 'InMobiCMP'` into the generated Xcode
  project and runs `pod install`. Verified end-to-end on macOS: Podfile generated,
  `InMobiCMP.xcframework` resolved, and the Obj-C binding compiles/links unsigned.

### Removed
- **Stray iOS location usage string.** `ChoiceCMPPostBuildiOS` no longer injects
  `NSLocationWhenInUseUsageDescription` into Info.plist. InMobi's iOS CMP does not
  require a location string (only `NSUserTrackingUsageDescription`, for IDFA),
  and shipping it forced a location entry into App Privacy and invited App Review
  questions for an app that never requests location.

### Changed
- **SKAdNetwork documentation corrected.** `AttInfoPlistPostprocessor` previously
  claimed "LevelPlay 9.1.0+ manages SKAdNetwork ids automatically." That is wrong:
  LevelPlay 8.8.0+ writes `SKAdNetworkItems` only when the publisher enables the
  **SKAdNetwork IDs** feature in the **LevelPlay Network Manager** (opt-in).
  Corrected the code comment and documented the publisher step in README/INSTALL.

## [1.1.0] - 2026-06-15

### Added
- **Global ad test mode.** New `TestMode` field on `VerifyLevelPlay` (`Auto` /
  `AlwaysOn` / `AlwaysOff`). `Auto` (default) ties test mode to Unity's
  *Development Build* (`Debug.isDebugBuild`) — ON in dev builds and the Editor,
  OFF in production — on both iOS and Android. When active the package enables
  LevelPlay's integration test suite (`is_test_suite` metadata) and logs the
  device advertising ID (GAID/IDFA) for test-device registration, which is what
  makes real in-game trigger points serve test ads (LevelPlay has no separate
  "test ad unit ids"). Added `AdsManager.IsTestMode` and `AdsManager.LaunchTestSuite()`
  (also a `VerifyLevelPlay` **Launch Test Suite** context-menu item), plus an
  optional `Auto Launch Test Suite` toggle. The sample scene reports test-mode
  status in its on-screen log. Replaces the old manual `enableTestSuite` boolean.

### Removed
- Removed the **`EnableAdsOnIosAppOnMac`** option (and its `VerifyLevelPlay` field).
  An iOS build running as an "iPad app on Apple-silicon Mac" now **always runs ad-free**
  — LevelPlay supports iOS/Android only, so there is no longer a toggle to serve ads there.

### Fixed
- **InMobi CMP Android build failure and runtime crashes.** InMobi's plugin
  declares none of its Android dependencies (its post-build processor only sets
  `android.useAndroidX=true`), so on Unity the build fails and the app crashes on
  scene load. Added a `ChoiceCMPDependencies.xml` (EDM4U), shipped with the InMobi
  CMP sample, declaring the libraries the Choice SDK actually uses:
  `com.google.android.material:material:1.12.0` (Material `BottomSheetDialog` —
  fixes `AAPT: resource style/Theme.Design.BottomSheetDialog not found`),
  `com.google.code.gson:gson:2.10.1` (fixes `NoClassDefFoundError Lcom/google/gson/Gson;`;
  `newtonsoft-json` is C# and does not satisfy this Java need),
  `androidx.preference:preference:1.2.1` (fixes
  `NoClassDefFoundError Landroidx/preference/PreferenceManager;`), and
  `androidx.constraintlayout:constraintlayout:2.1.4` (consent-UI layouts). The
  auto-import prompt also triggers an EDM Android resolve so consumers don't hit these.

### Changed
- **Consent is now a real CMP.** Replaced the placeholder built-in GDPR dialog
  with an **InMobi CMP (Choice)** integration — Google-certified IAB TCF v2.2,
  the LevelPlay counterpart to AdMob's Google UMP. `ConsentManager` starts InMobi
  CMP, reads the IAB `IABTCF_*` consent values from native storage, and exposes
  `GetConsentType` / `GetTCFConsentString` / `HasConsentForPurpose` and privacy
  options (`ShowPrivacyOptionsForm`, `ShowCcpaForm`). Integrated via reflection,
  so the package compiles with or without the InMobi SDK present.
- Added `MediationConsentManager` (applies CCPA/COPPA to LevelPlay; GDPR flows via
  the CMP's TCF string) — mirrors the AdMob package.
- Removed the placeholder `ConsentDialog`.
- `VerifyLevelPlay` gains a **CMP p-code** field (Consent & Privacy section).
- **InMobi CMP is now bundled** (Choice plugin v2.0.1). It ships as the package
  sample `InMobi CMP`; an editor bootstrap detects it on load and offers a
  one-click import (also available via **Tools ▸ Autech ▸ Import InMobi CMP**).
  The `.unitypackage` artifact includes it directly. Added
  `com.unity.nuget.newtonsoft-json` as a dependency (required by the plugin).

### Repo
- Renamed repo to `LevelPlay-Mediation-Package`; editable dev copy moved to
  `Assets/AutechLevelPlay` (the LevelPlay SDK reserves `Assets/LevelPlay`).
- Added example scene + `AdsExampleUI` (`Samples~/ExampleScene`); `com.unity.ugui`
  declared as a dependency.

## [1.0.0] - 2026-06-10

### Added
- Initial release: LevelPlay (Unity Ads) mediation wrapper mirroring the
  `com.autech.admob-mediation` API surface.
- `AdsManager` singleton: rewarded / interstitial / banner with retry, auto-reload,
  single-show lock, RemoveAds gating + events.
- `VerifyLevelPlay` scene bootstrap component (Inspector-configured app keys & ad unit ids).
- Built-in GDPR consent dialog; consent applied via `LevelPlayPrivacySettings.SetGDPRConsents`.
- CCPA opt-out API (`SetCcpaOptOut`) and COPPA child-directed flag (`SetCOPPA`).
- iOS ATT prompt handling before SDK init + `NSUserTrackingUsageDescription` build injection.
- AES-256 encrypted RemoveAds persistence (ported from the AdMob package).
