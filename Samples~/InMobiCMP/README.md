# InMobi CMP (Choice) — bundled consent dependency

This sample contains the **InMobi CMP (Choice) Unity plugin v2.0.1** — the
Google-certified IAB TCF v2.2 Consent Management Platform that powers GDPR
consent for this package (the LevelPlay equivalent of AdMob's Google UMP).

You normally don't import this by hand: when the package loads and the InMobi
CMP plugin is **not** present, it prompts you to import it. Choosing *Import*
copies these files into `Assets/Samples/Autech LevelPlay Mediation/<version>/InMobi CMP`,
where `ChoiceCMP` compiles into `Assembly-CSharp` and the package's
`ConsentManager` picks it up by reflection automatically.

To import manually: **Package Manager → Autech LevelPlay Mediation → Samples →
InMobi CMP → Import**.

## After importing

1. Set your **CMP p-code** on the `VerifyandInitializeLevelPlay` prefab
   (Inspector → *Consent & Privacy*). Get it from https://choice.inmobi.com.
2. The plugin requires **`com.unity.nuget.newtonsoft-json`** (declared as a
   package dependency — Package Manager resolves it automatically).
3. Per-platform build setup follows InMobi's docs (Android gradle
   `com.iabgpp:iabgpp-encoder`; iOS CMP framework) — handled by the bundled
   post-build processors under `Choice/Editor`.

See the package `INSTALL.md` for the full consent setup.
