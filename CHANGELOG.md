# Changelog

## [Unreleased]

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
- **InMobi CMP Android build failure and runtime crash.** The bundled InMobi
  `.aar` doesn't declare its transitive Android dependencies, causing two
  failures: a build-time resource-linking error
  (`AAPT: error: resource style/Theme.Design.BottomSheetDialog not found` — the
  Choice consent UI is a Material `BottomSheetDialog`), and a runtime crash on
  scene load when consent starts
  (`java.lang.NoClassDefFoundError: Lcom/google/gson/Gson;` — the plugin parses
  JSON via Gson; note `newtonsoft-json` is C# and does not satisfy this Java
  need). Added a `ChoiceCMPDependencies.xml` (EDM4U) declaring
  `com.google.android.material:material:1.12.0` and `com.google.code.gson:gson:2.10.1`,
  shipped with the InMobi CMP sample.

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
