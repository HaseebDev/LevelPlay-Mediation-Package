# Autech LevelPlay Mediation

Unity LevelPlay (Unity Ads) wrapper for AU-TECH Solutions games. Drop-in successor to
`com.autech.admob-mediation` — same `AdsManager` API surface, so migrating a game is a
namespace swap (`Autech.Admob` → `Autech.LevelPlay`) plus new dashboard ids.

## What it does

- **Ads**: rewarded / interstitial / banner via LevelPlay mediation (`com.unity.services.levelplay` 8+,
  built against 9.4.1). Load retry with backoff, auto-reload after close, single-show lock.
- **Consent (GDPR)**: real **InMobi CMP (Choice)** — Google-certified IAB TCF v2.2, the
  LevelPlay counterpart to AdMob's Google UMP. The plugin (v2.0.1) is **bundled** and the
  package prompts to import it on first load; consent (`IABTCF_*`) is written to native
  storage and consumed by the LevelPlay adapters automatically. See [INSTALL.md](INSTALL.md).
- **CCPA / US states**: `AdsManager.Instance.SetCcpaOptOut(bool)` — wire to a
  "Do Not Sell or Share My Personal Information" settings toggle.
- **COPPA**: `tagForChildDirectedTreatment` flag (leave OFF for general-audience games),
  applied via `LevelPlayPrivacySettings.SetCOPPA` before init.
- **iOS ATT**: shows the App Tracking Transparency prompt BEFORE LevelPlay init (Apple/Unity
  requirement) and injects `NSUserTrackingUsageDescription` into Info.plist at build time.
- **SKAdNetwork**: LevelPlay 8.8.0+ writes `SKAdNetworkItems` into Info.plist at build
  time, but only when you enable the **SKAdNetwork IDs** feature in the **LevelPlay
  Network Manager** (it is opt-in, not automatic). Enable it there so iOS attribution
  works; nothing else is needed in this package.
- **iOS dynamic frameworks**: the package adds a post-build Xcode phase that embeds and
  code-signs the known dynamic mediation/CMP frameworks required at runtime:
  `InMobiCMP.framework`, `InMobiSDK.framework`, and `FBAudienceNetwork.framework`. This
  prevents dyld launch crashes when CocoaPods resolves the pods but does not generate an
  "Embed Pods Frameworks" phase for Unity's static-linkage Podfile.
- **Remove Ads**: AES-256 encrypted persistence (ported unchanged from the AdMob package).
  Banner + interstitial are suppressed; rewarded stays available.

## Install

**Via Package Manager (git URL):** `Window → Package Manager → + → Add package from git URL…`

```
https://github.com/HaseebDev/LevelPlay-Mediation-Package.git
```

Or pin a version in `Packages/manifest.json`:

```json
"com.autech.levelplay-mediation": "https://github.com/HaseebDev/LevelPlay-Mediation-Package.git#v1.1.4"
```

**Via `.unitypackage`:** download the asset from the latest
[GitHub Release](https://github.com/HaseebDev/LevelPlay-Mediation-Package/releases)
and import it (`Assets → Import Package → Custom Package…`). See `INSTALL.md`.

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

The package has one **global test switch** — the **Test Mode** field on `VerifyLevelPlay`
(iOS & Android):

- **Auto** (default) — test mode is **ON in Development builds and the Editor**, and
  **OFF in production builds**. Tie test ads to Unity's *Development Build* checkbox and you
  never have to remember to flip anything before shipping.
- **Always On** / **Always Off** — force it either way (never ship *Always On*).

When test mode is active, on init the package:

1. Enables LevelPlay's **integration test suite** (`is_test_suite` metadata).
2. Logs the device's **advertising ID** (GAID / IDFA) to the console.
3. Does **not** auto-pop the suite (so the sample scene / your own ad buttons stay usable).
   Open it on demand with `AdsManager.Instance.LaunchTestSuite()`, or the `VerifyLevelPlay`
   **Launch Test Suite** context-menu item. Set **Auto Launch Test Suite** to pop it on init.

### Getting test ads at your real in-game trigger points

LevelPlay has **no separate "test ad unit ids"** — you always use your real units. The
integration test suite is a *separate diagnostic panel*; it does **not** make your own
`ShowInterstitial` / `ShowRewarded` / `ShowBanner` calls serve test ads. To get test ads at
your real trigger points (and in the sample scene), register the device once:

1. Run a Development build (or Editor) — test mode is on; copy the **advertising ID** from
   the console (or read it from the test suite header).
2. LevelPlay dashboard → **Settings → Test devices** → add that GAID/IDFA.
3. Re-run. Every banner/interstitial/rewarded `Show` call now serves **test ads** on that
   device, on your production units — globally, no code changes per trigger point.

- **Never click real ads in production builds** — Unity's Invalid Activity Policy treats
  self-clicks and accidental-click placements as violations, with payment clawback.

## Compliance notes for store submission

- **Apple App Privacy**: declare Device ID (used for tracking, third-party advertising),
  ad interaction + advertising data, performance data, per Unity's LevelPlay questionnaire.
- **Google Play Data safety**: approximate location, ad interactions, diagnostics
  (collected, not shared), device or other IDs — per Unity's LevelPlay questionnaire.
- Your privacy policy must disclose Unity Ads data collection, link Unity's privacy policy
  (https://unity.com/legal/privacy-policy), and explain the opt-out (Unity Advertising ToS §3.3).
