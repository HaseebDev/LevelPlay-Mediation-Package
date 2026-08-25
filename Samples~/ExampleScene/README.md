# Example Scene with UI

`ExampleLevelPlayScene.unity` demonstrates every Autech.LevelPlay ad format with a
simple on-screen UI:

- **Show Rewarded** / **Show Interstitial** — display those ad formats
- **Toggle Banner** — show/hide the banner
- **Toggle Remove Ads** — flip the persisted Remove-Ads state (button turns red)
- **Privacy Options** — re-open the GDPR consent dialog
- A live debug log panel mirrors every call.

The scene also contains the **LevelPlayBootstrap** prefab (from the
*Prefabs* sample) — set your LevelPlay app keys / ad unit ids on it to run.
`AdsExampleUI.cs` is the wiring; read it to see the AdsManager API in use.
