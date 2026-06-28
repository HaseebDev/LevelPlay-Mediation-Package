# Installing Autech LevelPlay Mediation

There are two supported ways to add this package to a Unity project.

## 1. Package Manager — git URL (recommended)

`Window → Package Manager → +  → Add package from git URL…` and paste:

```
https://github.com/HaseebDev/LevelPlay-Mediation-Package.git
```

To pin a specific release, append a tag:

```
https://github.com/HaseebDev/LevelPlay-Mediation-Package.git#v1.1.2
```

…or add it directly to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.autech.levelplay-mediation": "https://github.com/HaseebDev/LevelPlay-Mediation-Package.git#v1.1.2"
  }
}
```

## 2. `.unitypackage` from a GitHub Release

1. Open the [Releases page](https://github.com/HaseebDev/LevelPlay-Mediation-Package/releases).
2. Download `com.autech.levelplay-mediation-<version>.unitypackage`.
3. In Unity: `Assets → Import Package → Custom Package…` and select the file.
   (Imports the package under `Assets/AutechLevelPlay`.)

## Requirements

- Unity **2021.3** or newer.
- The **LevelPlay (Ads Mediation)** SDK — `com.unity.services.levelplay`
  (declared as a dependency; Package Manager resolves it automatically for
  method 1). Install it from the Unity Registry if you use method 2.

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
3. On the **VerifyandInitializeLevelPlay** prefab (Inspector → *Consent & Privacy*),
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

## Quick start

After importing, add the `VerifyandInitializeLevelPlay` prefab (Samples ▸ Prefabs)
to your first scene and fill in your LevelPlay app keys / ad unit ids. See the
[README](README.md) for the full quick-start.
