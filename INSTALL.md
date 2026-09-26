# Installing Autech LevelPlay Mediation

There are two supported ways to add this package to a Unity project.

## 1. Package Manager — git URL (recommended)

`Window → Package Manager → +  → Add package from git URL…` and paste:

```
https://github.com/HaseebDev/LevelPlay-Mediation-Package.git
```

To pin a specific release, append a tag:

```
https://github.com/HaseebDev/LevelPlay-Mediation-Package.git#v1.2.2
```

…or add it directly to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.autech.levelplay-mediation": "https://github.com/HaseebDev/LevelPlay-Mediation-Package.git#v1.2.2"
  }
}
```

## 2. `.unitypackage` from a GitHub Release

1. Open the [Releases page](https://github.com/HaseebDev/LevelPlay-Mediation-Package/releases).
2. Download `com.autech.levelplay-mediation-<version>.unitypackage`.
3. In Unity: `Assets → Import Package → Custom Package…` and select the file.
   (Imports the package under `Assets/AutechLevelPlay`.)

## Requirements

- **Unity 6** or newer.
- The **LevelPlay (Ads Mediation)** SDK — `com.unity.services.levelplay`
  **9.5.1 or newer** (declared as a dependency; Package Manager resolves it
  automatically for method 1). Install it from the Unity Registry if you use method 2.
- **TMP Essential Resources**, but only if you use the local consent form. The TMP code
  ships inside `com.unity.ugui` on Unity 6, but the font asset its labels point at does
  not: import it once via *Window > TextMeshPro > Import TMP Essential Resources*.
  Without it the labels resolve no font and render blank, so the package refuses to
  show the form rather than ask someone to consent to text they cannot read, and the
  build fails with that message. The InMobi path does not need TMP.

## Consent — InMobi CMP (GDPR / IAB TCF)

GDPR consent is handled by **InMobi CMP** (Choice) — a Google-certified IAB TCF
v2.2 Consent Management Platform, the LevelPlay equivalent of AdMob's Google UMP.
**The InMobi CMP plugin (v2.0.1) is bundled with this package**, so you don't
download it separately.

1. **Import the plugin.** When the package loads and InMobi CMP isn't present
   yet, it prompts you — click **Import**. (You can also import it any time from
   *Package Manager → Autech LevelPlay Mediation → Samples → InMobi CMP*, or via
   the menu **Tools ▸ Autech ▸ Import InMobi CMP**.) If you installed the
   `.unitypackage`, the plugin is already included — no import step needed.
   - The plugin needs **`com.unity.nuget.newtonsoft-json`**, declared as a
     package dependency so Package Manager resolves it automatically.
2. Create a (free) account + a CMP **property** at **https://choice.inmobi.com**;
   note your **p-code** (profile menu — looks like `p-XXXXXXXX`).
3. On the **LevelPlayBootstrap** prefab (Inspector → *Consent & Privacy*),
   paste your **CMP p-code** (the leading `p-` is optional).
4. Per-platform build setup is handled for you:
   - **Android**: InMobi's plugin declares **no** Android dependencies itself (its
     post-build processor only sets `android.useAndroidX=true`). This package fills
     that gap with `ChoiceCMPDependencies.xml` (shipped with the sample), resolved
     by the **External Dependency Manager (EDM4U)** that comes with the LevelPlay
     SDK — **Material Components**, **Gson**, **androidx.preference**, and
     **androidx.constraintlayout** (the libraries the Choice SDK actually uses).
     A resolve runs automatically right after the InMobi CMP import.
   - **iOS**: the same `ChoiceCMPDependencies.xml` declares the **`InMobiCMP`
     CocoaPod** (pinned to the version of the bundled Android native, so both
     platforms run the same CMP SDK), resolved by EDM4U's **iOS Resolver** — it
     writes `pod 'InMobiCMP'` into the generated Xcode project's Podfile and runs
     `pod install`. The bundled post-build processor then sets the Swift/`-ObjC`
     linker flags. CocoaPods must be installed on the build Mac (`pod --version`).
     If the iOS build can't find `InMobiCMP/InMobiCMP-Swift.h`, run
     **Assets → External Dependency Manager → iOS Resolver → Force Resolve**.

   See the [InMobi CMP Unity docs](https://support.inmobi.com/choice/other-resources/unity-app-implementation-sdk/).

Without the plugin imported + a p-code set, the package still builds and serves
ads — it just won't show a consent prompt (a warning is logged). The integration
is reflection-based, so the package compiles with or without the InMobi SDK present.

### Troubleshooting the Android build

If you import InMobi CMP and the Android build fails or the app crashes on launch,
the cause is almost always that the EDM4U resolver hasn't run yet. Fix:
**Assets → External Dependency Manager → Android Resolver → Force Resolve**, then rebuild.

| Symptom | Missing dependency |
|---|---|
| Build error: `AAPT: error: resource style/Theme.Design.BottomSheetDialog not found` | `com.google.android.material:material` |
| Crash on scene load: `NoClassDefFoundError: Lcom/google/gson/Gson;` | `com.google.code.gson:gson` |
| Crash on scene load: `NoClassDefFoundError: Landroidx/preference/PreferenceManager;` | `androidx.preference:preference` |
| Crash / inflate error when the consent UI shows (ConstraintLayout) | `androidx.constraintlayout:constraintlayout` |

All four are declared in `ChoiceCMPDependencies.xml`; a Force Resolve injects them
into `mainTemplate.gradle`. (`newtonsoft-json` is the C# JSON library and does **not**
satisfy the plugin's native Gson requirement.)

## Consent: local form (no CMP)

If you do not need a Google-certified CMP, the package can collect consent with
its own form instead of InMobi. Tick **Use Local Consent** on the
**LevelPlayBootstrap** prefab (Inspector -> *Consent & Privacy*). No InMobi
account, p-code or plugin import is needed on this path.

What it does:

1. Shows the package's consent form on first launch, with two choices:
   personalised ads and analytics. Both start unticked.
2. Stores the answer on the device (`PlayerPrefs`).
3. Hands the ads decision to **LevelPlay**, which forwards it to every mediated
   adapter. There are no per-network SDK calls to make.
4. Stays reachable: the existing privacy-options entry point
   (`AdsManager.Instance.ShowPrivacyOptionsForm()`) reopens the same form, and
   changing the answer pushes the new decision to the networks again.

The analytics answer is stored and readable via
`AdsManager.Instance.LocalConsent.AnalyticsGranted`. Nothing in this package
consumes it yet.

**This form is not an IAB TCF CMP and is not Google-certified.** It writes no
`IABTCF_*` values. Use it only where your mediation stack does not require a
certified CMP, and keep the InMobi path for the cases that do. AdMob in
particular requires a certified CMP, so do not use this path with it.

**Requires TMP Essentials** in your project (*Window -> TextMeshPro -> Import TMP
Essential Resources*), because the form's labels are TextMeshPro. Most projects
already have them.

### iOS ATT on this path

The InMobi CMP normally presents the iOS ATT prompt for you (`cmpShowIdfaPopup`).
On the local path the CMP never starts, so the package presents ATT itself, right
after the consent form and before LevelPlay init. You do not need to tick the
legacy `requestAttAuthorization` box; `useLocalConsent` implies it.
`NSUserTrackingUsageDescription` is injected at build time accordingly.

iOS only shows the ATT prompt while the app is active and silently drops a request
made while it is not (a system alert on screen, an incoming call, Control Center).
The package therefore keeps the request armed: it asks as soon as the app is active,
and again every time the app returns to active, until the user answers. If nothing is
answered within 90 seconds, init goes ahead without IDFA, and the prompt still appears
the next time the app becomes active.

### How the choices start

`Default Choices Ticked` is **on**, so both choices are pre-ticked on a first ask and
the user unticks what they do not want. This raises opt-in rates.

Be aware of what it costs where GDPR applies: Recital 32 states that silence,
pre-ticked boxes and inactivity do not constitute consent, and the CJEU confirmed it
in Planet49 (C-673/17). A pre-ticked box therefore does not establish valid consent in
the EEA/UK. If you ship there, either turn this off or pair it with
**Only Ask Where GDPR Applies** so the pre-ticked form is never the one EEA users see.

Re-opening the form always shows the user's real stored answer, never this default, so
changing the setting never rewrites a decision somebody already made.

### Re-asking when your policy or partners change

`Consent Policy Version` on the prefab (default `1`) is stamped onto every stored
answer, along with the UTC timestamp of when it was given. Bump the number whenever
the privacy policy or the set of ad partners changes and every existing answer stops
counting, so those users are asked again instead of silently carrying consent forward
to something they never saw. Read them back via
`AdsManager.Instance.LocalConsent.AnsweredAtUtc` and `.AnsweredPolicyVersion`.

### Asking only where GDPR applies

`Only Ask Where GDPR Applies` is **off** by default, meaning everyone is asked. Turn
it on and the prompt is limited to the EEA and UK, resolved from the user's **IP** on
the first launch that needs an answer. Outside those regions no prompt is shown and
**no GDPR signal is sent at all**, which lets each network apply its own regional
default rather than being told the user refused.

The IP is what a certified CMP uses for this decision, and it is the right signal: a
device's Region setting is something the user picks and says nothing about where they
are. On the machine this was tested on, the device region reported `US` while the IP
resolved to `PK`.

**Every failure path asks.** Timeout, no network, a blocked endpoint, a reply with no
country in it: all of them show the form. Asking someone who did not need it costs a
tap, whereas skipping someone who did is the actual exposure, so uncertainty is never
resolved silently.

`Geo Lookup Url` defaults to Cloudflare's trace endpoint because it needs no account
or API key and returns plain text. Point it at your own backend if you would rather
not depend on a third party. Clear it entirely and the check falls back to the device
Region setting, which is weaker and can fail to ask somebody who should have been
asked. `Geo Lookup Timeout Seconds` defaults to 3, since this sits in front of the
consent form on first launch.

### Turning the flow off

Turning **Show Consent Dialog** off skips the form and sends **no GDPR signal at
all**, matching the InMobi path. It is not treated as a refusal: an answer has to
be collected before anything is pushed to the networks.

Leaving **Use Local Consent** unticked keeps the InMobi CMP behaviour described
above, unchanged.

## Meta Audience Network (both consent paths)

This applies whichever consent path you use. If the Meta adapter is in your build,
the package sets Meta's `setAdvertiserTrackingEnabled` from the device's ATT status
before LevelPlay initializes. Meta requires the flag to be set pre-init and no
mediation adapter derives it from ATT for you, so the package owns it. It is
resolved dynamically at runtime, so it is a safe no-op on Android, in the Editor,
and in builds without the Meta adapter.

Scope worth knowing: Meta ignores this flag on **iOS 17+** with Audience Network SDK
6.15.0+, where it reads `ATTrackingManager` directly instead. The flag still matters
on iOS 14.5 to 16.x, which is why it is set. Do not remove it after observing that it
changes nothing on a modern device.

If the log says `Meta Audience Network not present` on a build that does include the
adapter, the class was dead-stripped: check that the Xcode project links with `-ObjC`.

**Not handled: Meta's Limited Data Use (LDU) flag** for US state privacy laws. There
is no LevelPlay API for it and it needs a direct Meta SDK call, so it currently has to
come from your own native bridge. This is a compliance gap in US states, not a fill or
revenue problem.

## Quick start

After importing, add `Assets/Autech/LevelPlay/Prefabs/LevelPlayBootstrap.prefab`
to your first scene and fill in your LevelPlay app keys / ad unit ids. See the
[README](README.md) for the full quick-start.
