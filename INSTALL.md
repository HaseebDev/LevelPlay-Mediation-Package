# Installing Autech LevelPlay Mediation

There are two supported ways to add this package to a Unity project.

## 1. Package Manager — git URL (recommended)

`Window → Package Manager → +  → Add package from git URL…` and paste:

```
https://github.com/HaseebDev/LevelPlay-Mediation-Package.git
```

To pin a specific release, append a tag:

```
https://github.com/HaseebDev/LevelPlay-Mediation-Package.git#v1.0.0
```

…or add it directly to `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.autech.levelplay-mediation": "https://github.com/HaseebDev/LevelPlay-Mediation-Package.git#v1.0.0"
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
It is **optional** but required to serve personalized ads in GDPR regions.

1. Create a (free) account + a CMP **property** at **https://choice.inmobi.com**;
   note your **p-code** (profile menu — looks like `p-XXXXXXXX`).
2. Download and import the **InMobi CMP Unity plugin**
   (`Assets → Import Package → Custom Package…`) —
   [InMobi CMP Unity docs](https://support.inmobi.com/choice/other-resources/unity-app-implementation-sdk/).
3. Add its dependency via Package Manager → *Add package from git URL*:
   `com.unity.nuget.newtonsoft-json`.
4. On the **VerifyandInitializeLevelPlay** prefab (Inspector → *Consent & Privacy*),
   paste your **CMP p-code** (the leading `p-` is optional).
5. Per-platform build setup per InMobi's docs (Android: `com.iabgpp:iabgpp-encoder`
   gradle dependency; iOS: the CMP framework).

Without the plugin + p-code the package still builds and serves ads — it just
won't show a consent prompt (a warning is logged). The integration is
reflection-based, so the package compiles with or without the InMobi SDK present.

## Quick start

After importing, add the `VerifyandInitializeLevelPlay` prefab (Samples ▸ Prefabs)
to your first scene and fill in your LevelPlay app keys / ad unit ids. See the
[README](README.md) for the full quick-start.
