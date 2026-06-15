# Releasing

This repo mirrors the **Autech AdMob** package layout.

```
repo root/
├── package.json, README.md, CHANGELOG.md, LICENSE.md, INSTALL.md
├── Runtime/  Editor/  Samples~/      ← the DISTRIBUTED UPM package (git URL / OpenUPM)
│     Samples~/InMobiCMP/             ← bundled InMobi CMP (Choice) v2.0.1, the consent dep
├── Assets/AutechLevelPlay/                 ← the EDITABLE / TESTABLE working copy (source of truth)
├── Assets/InMobi/                          ← editable InMobi CMP copy (mirrored → Samples~/InMobiCMP)
├── Assets/Editor/AutechPackageExporter.cs   ← dev-only release tooling
├── Packages/ ProjectSettings/        ← the Unity dev project (so the repo opens)
└── .npmignore .gitattributes .gitignore
```

You **edit and test** in `Assets/AutechLevelPlay` (open the repo in Unity). The repo-root
`Runtime/`/`Editor/`/`Samples~/` is the clean copy consumers install; it is kept in
sync from `Assets/AutechLevelPlay` at release time.

Releases are **manual** (no CI): a git tag **and** a `.unitypackage` attached to a
GitHub Release.

## One-time

- Remote points at `github.com/HaseebDev/LevelPlay-Mediation-Package` and the
  `package.json` URLs match. (Done.)

## Cutting a release

1. **Edit & test** in `Assets/AutechLevelPlay` (open the repo root in Unity 6000.x).
2. **Sync** the dev copy into the root package mirror:
   Unity menu **Tools ▸ Autech ▸ Sync dev copy → root package**. This also
   mirrors `Assets/InMobi/Choice` → `Samples~/InMobiCMP/Choice`.
   (If you changed the example scene, copy it into `Samples~/ExampleScene/` too.)
3. **Bump the version** in `package.json` and update `CHANGELOG.md`.
4. **Export** the importable artifact:
   **Tools ▸ Autech ▸ Export .unitypackage** → writes
   `releases/com.autech.levelplay-mediation-<version>.unitypackage`
   (the `releases/` folder is git-ignored).
5. **Commit & tag:**
   ```sh
   git add -A
   git commit -m "release: v1.0.1"
   git tag v1.0.1
   git push origin main --tags
   ```
6. **Create the GitHub Release** for `v1.0.1` and attach the `.unitypackage`.

## How consumers install

- **git URL:** `…/LevelPlay-Mediation-Package.git#v1.0.1`
- **.unitypackage:** download from the Release and import.

See `INSTALL.md` for the full instructions.
