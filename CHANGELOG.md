# Changelog

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
