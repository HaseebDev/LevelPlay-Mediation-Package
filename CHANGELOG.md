# Changelog

## [Unreleased]

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
