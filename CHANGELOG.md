# Changelog

## [Unreleased]

## [1.2.3] - 2026-09-26

### Added
- **Ad lifecycle telemetry for analytics integrations.** A new `AdTelemetry.Event`
  (`Autech.LevelPlay`) reports `Request`, `Loaded`, `LoadFailed`, `Displayed`,
  `DisplayFailed`, `Clicked`, `Closed` and `Rewarded` for rewarded, interstitial and
  banner ads, with the placement and error code. The package has no dependency on any
  analytics SDK: an analytics package subscribes to the event and maps it itself (see
  `com.autech.gameanalytics`). Observability only: each listener runs inside its own
  try/catch, so a failing listener can never block another listener, an SDK call or reward
  settlement, and the reward event fires once per accepted show even if the native reward
  callback repeats.

## [1.2.2] - 2026-09-26

### Fixed
- **The app-side ATT prompt was lost if the app was not active when it was requested.**
  iOS only presents the prompt while the app is active and silently drops a request made
  otherwise (another system alert on screen, an incoming call, Control Center), leaving
  the status NotDetermined for the whole session. The request is now armed instead of
  fired once: it asks only while `applicationState` is active, re-asks on every
  `UIApplicationDidBecomeActiveNotification` while the status is still NotDetermined,
  and removes its observer once there is an answer. If the 90 second wait times out,
  init continues without IDFA as before, but the request stays armed and the prompt
  appears the next time the app becomes active. Applies to the local consent path and
  to `requestAttAuthorization`. The InMobi CMP path is unchanged: the CMP raises its
  own prompt and the package only waits for the answer.

## [1.2.1] - 2026-09-12

### Changed
- **Diagnostic logging is now compiled out of release builds.** All package logging goes
  through a new `AdLog` helper whose `Info` and `Warn` carry `[Conditional]` attributes
  for `UNITY_EDITOR` and `DEVELOPMENT_BUILD`, so a release build removes the call
  entirely, arguments included. That matters because most messages interpolate a string:
  a runtime flag would still pay to build every message before discarding it.
  `Error` and `Exception` are deliberately NOT conditional, because a shipping game that
  fails silently is far harder to diagnose than one that leaves a line in the device log.
- **Much deeper tracing on the consent path**, all of it dev-build only: which consent
  path was chosen, the ATT plan and which component owns the prompt, the settled ATT
  status before init, the geo lookup endpoint and how long it took, prefab load and
  instantiation, what the form was seeded with and whether it is answerable and readable,
  EventSystem creation and which input backend, EventSystem recovery after a scene load,
  which button was tapped and the resulting decision, and the guard being released.


## [1.2.0] - 2026-09-12

### Added
- **Local consent form** as an alternative to the InMobi CMP, selected with the new
  `useLocalConsent` toggle on the LevelPlayBootstrap prefab. Collects personalised-ads
  and analytics choices, stores them on the device, and hands the ads decision to
  LevelPlay, which forwards it to every mediated adapter. Off by default, so existing
  projects are unaffected. Not an IAB TCF CMP and not Google-certified; see INSTALL.md.
- Meta Audience Network `setAdvertiserTrackingEnabled` is now applied by the package
  from the device's ATT status, before LevelPlay init, as Meta requires. Resolved
  dynamically, so it is a safe no-op in builds without the Meta adapter.

### Changed
- Require **Unity 6** (was 2021.3) and **com.unity.ugui 2.0.0** (was 1.0.0), which is
  what guarantees TextMeshPro is present for the local consent form.
- Require LevelPlay **9.5.1** (was 9.4.1). GDPR consent now uses the current
  `LevelPlayPrivacySettings.SetGDPRConsent(bool)`, gated behind a `LEVELPLAY_9_5_OR_NEWER`
  version define with a fallback to the older global call on 9.4.x. The per-network
  `SetGDPRConsents(Dictionary)` is deliberately NOT used: 9.5.0 deprecated it, and it
  replaces every stored entry on each call, silently revoking consent for any network
  missing from the map.
- The iOS ATT prompt is now presented by the app whenever `useLocalConsent` is on. On
  that path the InMobi CMP never starts, so its `cmpShowIdfaPopup` trigger is dead and
  the IDFA would otherwise stay all-zeros for the life of the app.
- `NSUserTrackingUsageDescription` injection accounts for the local-consent path, and
  the on-device privacy snapshot reports the correct ATT trigger for it.
- `UnityEngine.UI` is now an explicit assembly reference instead of relying on
  auto-referencing.

### Fixed
- The iOS ATT prompt is now awaited on **every** path that expects one, including the
  InMobi CMP path. The consent flow previously returned as soon as the TC string
  landed, which is before the user answers ATT, so anything reading the status
  straight after (such as the Meta flag) saw a pending value.
- The consent form can no longer be opened twice. A second request while one is open
  used to stack a second full-screen canvas; answering either left the other alive and
  raycasting, blocking all input and leaving initialization stuck forever.
- The privacy-options entry point now reports itself unavailable while the form is
  open, which is what allowed the double-open above.
- The EventSystem the form creates is now `DontDestroyOnLoad`, like the form itself.
  A scene load while the form was up destroyed the EventSystem but not the form,
  leaving it visible and permanently unclickable.
- An unanswered consent no longer pushes "denied". Skipping the flow, or failing to
  show the form, now leaves the GDPR flag untouched instead of telling every network
  the user refused.
- The first answer no longer applies the consent flags twice.
- The on-device privacy snapshot reports availability from the active consent path
  instead of always querying the InMobi component.
- Re-opening the privacy options no longer swallows exceptions silently.
- The InMobi path no longer raises an unexpected ATT prompt. Widening the ATT gate had
  made projects where the CMP never starts (no p-code, plugin missing, dialog off) show
  a tracking prompt they never showed before. The CMP path now waits for the CMP's own
  prompt instead of raising one, and only when the CMP actually took ownership.
- Switching to the local path now clears any leftover `IABTCF_*` values. A TC string from
  a previous InMobi build could otherwise keep telling the adapters "consented" and
  override a user who had just declined in the local form.
- `ResetConsentForTesting` clears both consent paths instead of only the active one.
- The consent form refuses to show, with a clear error, when its buttons are unwired or
  when TextMeshPro is missing. Both previously produced a form that could never be
  answered, hanging initialization forever with no ads and nothing in the log.
- The form now recovers if the EventSystem it was relying on is destroyed by a scene
  load, and cleans up its own EventSystem on destroy rather than only on submit.

### Added (continued)
- The consent form now shows a **privacy policy link**, driven by the existing
  `privacyPolicyUrl` setting, which until now was configured but read by nothing.
  Hidden automatically when no URL is set.
- **Consent records now carry a timestamp and a policy version.** New
  `consentPolicyVersion` setting: bump it when the privacy policy or the partner list
  changes and existing answers stop counting, so those users are asked again rather
  than carrying consent forward to terms they never saw. Exposed as
  `LocalConsent.AnsweredAtUtc` and `LocalConsent.AnsweredPolicyVersion`.
- **Optional region-limited asking.** New `onlyAskWhereGdprApplies` setting, off by
  default. When on, the form is shown only in the EEA/UK, resolved from the user's IP
  on the first launch that needs an answer, and no GDPR signal is sent elsewhere rather
  than a refusal. Every failure path (timeout, no network, blocked endpoint, reply with
  no country) shows the form anyway, so a lookup problem can never silently skip
  somebody who should have been asked. `geoLookupUrl` defaults to Cloudflare's trace
  endpoint and can be pointed at your own backend, or cleared to fall back to the
  device Region setting.
- **The consent form's choices are pre-ticked by default**, via the new
  `defaultChoicesTicked` setting, so the user opts out rather than in. Note that GDPR
  Recital 32 and the CJEU's Planet49 ruling (C-673/17) say a pre-ticked box does not
  establish valid consent, so in the EEA/UK this weakens the consent the form collects;
  set it to false there, or pair it with `onlyAskWhereGdprApplies`. Re-opening the form
  always shows the user's real stored answer, never this default.
- **A build now fails** if the local consent form is enabled without TextMeshPro in the
  project, instead of shipping a form whose labels cannot render. Checked at build time
  rather than declared as a package dependency, because no single dependency entry
  project, or if TMP Essential Resources have not been imported so its labels resolve
  no font. The second case is the common one and Package Manager cannot express it as
  a dependency.

## [1.1.10] - 2026-08-27

### Fixed
- Resolve the active package from Unity's registered-package list without producing virtual-folder warnings.

## [1.1.9] - 2026-08-27

### Fixed
- Resolve the package source from its mounted asset path so the stable prefab auto-import works during the first editor domain load.

## [1.1.8] - 2026-08-27

### Changed
- Auto-import the bootstrap prefab once to the stable `Assets/Autech/LevelPlay/Prefabs` path instead of creating a new versioned Samples folder after every package upgrade.

## [1.1.7] - 2026-08-27

### Fixed
- Let Unity generate fresh metadata for auto-imported prefab samples, avoiding GUID collisions with older imported sample versions.

## [1.1.6] - 2026-08-27

### Fixed
- Import the prefab sample from the active package path instead of Unity's stale Package Manager sample cache after a Git-tag upgrade.

## [1.1.5] - 2026-08-25

### Changed
- Auto-import the package's prefab sample once on editor load and expose the bootstrap as `Prefabs/LevelPlayBootstrap.prefab`.
- Use `https://autechsolutions.netlify.app/privacy-policy` as the default privacy-policy URL.

## [1.1.4] - 2026-07-08

### Fixed
- **iOS launch crash - missing dynamic mediation frameworks.** Extended the iOS
  Xcode build phase added in v1.1.3 so it embeds and code-signs all known dynamic
  LevelPlay/CMP frameworks required at runtime: `InMobiCMP.framework`,
  `InMobiSDK.framework`, and `FBAudienceNetwork.framework`. This fixes dyld startup
  failures such as `Library not loaded: @rpath/FBAudienceNetwork.framework/FBAudienceNetwork`
  when CocoaPods resolves the pods but no "Embed Pods Frameworks" phase copies them
  into the final app bundle.

## [1.1.3] - 2026-07-02

### Fixed
- **iOS launch crash — `InMobiCMP.framework` was not embedded.** InMobiCMP is a
  **dynamic** (Swift) framework, but EDM4U's `use_frameworks! :linkage => :static`
  Podfile links it via `@rpath` without generating an "Embed Pods Frameworks" phase —
  so the framework was never copied into the app bundle. Every iOS build from
  v1.1.1 / v1.1.2 therefore crashed on launch with
  `dyld: Library not loaded: @rpath/InMobiCMP.framework/InMobiCMP`. Added a post-build
  step (`ChoiceCMPFrameworkEmbed`, ships with the InMobi CMP sample) that copies and
  code-signs `InMobiCMP.framework` into the app's Frameworks folder at Xcode build
  time, for both device and simulator. Verified on a physical iOS device: the app now
  launches and the ATT prompt fires. (IronSource is a static framework — no embedding
  needed.) v1.1.1's `<iosPods>` fix made iOS *compile*; this makes it *run*.

## [1.1.2] - 2026-06-28

### Fixed
- **UPM import warnings** ("…has no meta file, but it's in an immutable folder.
  The asset will be ignored") when installing via git URL. Added the missing
  `.meta` files for `INSTALL.md` and `RELEASING.md`, and removed the temporary
  `IOS-HANDOFF.md` dev doc from the package. No code changes.

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
