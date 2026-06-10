# Autech LevelPlay Mediation

Unity LevelPlay (Unity Ads) wrapper for AU-TECH Solutions games. Drop-in successor to
`com.autech.admob-mediation` — same `AdsManager` API surface, so migrating a game is a
namespace swap (`Autech.Admob` → `Autech.LevelPlay`) plus new dashboard ids.

## What it does

- **Ads**: rewarded / interstitial / banner via LevelPlay mediation (`com.unity.services.levelplay` 8+,
  built against 9.4.1). Load retry with backoff, auto-reload after close, single-show lock.
- **Consent (GDPR)**: built-in first-launch consent dialog (dependency-free uGUI). Choice is
  persisted and applied each session via `LevelPlayPrivacySettings.SetGDPRConsents` —
  full network map every call (the API replaces, never merges).
- **CCPA / US states**: `AdsManager.Instance.SetCcpaOptOut(bool)` — wire to a
  "Do Not Sell or Share My Personal Information" settings toggle.
- **COPPA**: `tagForChildDirectedTreatment` flag (leave OFF for general-audience games),
  applied via `LevelPlayPrivacySettings.SetCOPPA` before init.
- **iOS ATT**: shows the App Tracking Transparency prompt BEFORE LevelPlay init (Apple/Unity
  requirement) and injects `NSUserTrackingUsageDescription` into Info.plist at build time.
- **SKAdNetwork**: nothing to do — LevelPlay 9.1.0+ manages SKAN ids automatically.
- **Remove Ads**: AES-256 encrypted persistence (ported unchanged from the AdMob package).
  Banner + interstitial are suppressed; rewarded stays available.

## Install

**Via Package Manager (git URL):** `Window → Package Manager → + → Add package from git URL…`

```
https://github.com/BadranRaza/com.autech.levelplay-mediation.git?path=/Packages/com.autech.levelplay-mediation
```

> The repo is a Unity development project; the package lives in the
> `Packages/com.autech.levelplay-mediation` subfolder, so the install URL needs
> the `?path=` query. Pin a release with `#v1.0.0` at the end.

Or pin a version in `Packages/manifest.json`:

```json
"com.autech.levelplay-mediation": "https://github.com/BadranRaza/com.autech.levelplay-mediation.git?path=/Packages/com.autech.levelplay-mediation#v1.0.0"
```

Requires Unity 2021.3+. The `com.unity.services.levelplay` dependency (Ads Mediation,
Unity Registry) resolves automatically; the package compiles to a no-op until it is present
(`LEVELPLAY_INSTALLED` version define).

## Quick start

1. Install `com.unity.services.levelplay` (Ads Mediation) from the Unity Registry.
2. Install this package.
3. Add a GameObject with `VerifyLevelPlay` to your first scene (or use the sample prefab).
4. Fill in the LevelPlay **app keys** and **ad unit ids** from the
   [LevelPlay dashboard](https://platform.ironsrc.com) (Apps + Ad units pages).
5. Press play. Init order: consent dialog → ATT (iOS) → `LevelPlay.Init` → ad loading.

```csharp
using Autech.LevelPlay;

// Rewarded with reward validation
AdsManager.Instance.ShowRewarded(
    onRewarded: reward => GrantGems(),
    onSuccess: () => Resume(),
    onFailure: () => ShowNoAdToast());

// Banner
AdsManager.Instance.ShowBanner(true);

// Privacy options (GDPR withdrawal — keep reachable from Settings)
AdsManager.Instance.ShowPrivacyOptionsForm();
```

## Testing

- Enable **Test Suite** on `VerifyLevelPlay` to launch LevelPlay's integration test suite
  after init (SDK 7.3+, portrait). Never ship with it enabled.
- Register test devices in the LevelPlay dashboard (Settings → Test devices, GAID/IDFA).
- **Never click real ads in production builds** — Unity's Invalid Activity Policy treats
  self-clicks and accidental-click placements as violations, with payment clawback.

## Compliance notes for store submission

- **Apple App Privacy**: declare Device ID (used for tracking, third-party advertising),
  ad interaction + advertising data, performance data, per Unity's LevelPlay questionnaire.
- **Google Play Data safety**: approximate location, ad interactions, diagnostics
  (collected, not shared), device or other IDs — per Unity's LevelPlay questionnaire.
- Your privacy policy must disclose Unity Ads data collection, link Unity's privacy policy
  (https://unity.com/legal/privacy-policy), and explain the opt-out (Unity Advertising ToS §3.3).
