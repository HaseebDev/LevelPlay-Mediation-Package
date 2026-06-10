# Releasing

This repo is a **Unity development project**. The actual package lives in
[`Packages/com.autech.levelplay-mediation`](Packages/com.autech.levelplay-mediation).
You open this repo in Unity to edit/test the package, then cut a release.

Releases are **manual**: a git tag + a `.unitypackage` attached to the GitHub Release.
There is no CI.

## One-time setup

- Make sure the git remote points at **your** repo (not the original author's):
  ```sh
  git remote set-url origin https://github.com/<you>/com.autech.levelplay-mediation.git
  ```
- Update the URLs in `Packages/com.autech.levelplay-mediation/package.json`
  (`repository`, `documentationUrl`, `changelogUrl`, `licensesUrl`) to match.

## Cutting a release

1. **Develop & test** — open this repo in Unity (6000.x). The package is an
   embedded package under `Packages/`, so edits compile live.

2. **Bump the version** in `Packages/com.autech.levelplay-mediation/package.json`
   (e.g. `1.0.0` → `1.0.1`) following [SemVer](https://semver.org/).

3. **Update** `Packages/com.autech.levelplay-mediation/CHANGELOG.md` with a new
   section for the version.

4. **Commit**:
   ```sh
   git add -A
   git commit -m "release: v1.0.1"
   ```

5. **Tag and push** (the tag *is* the git-URL release):
   ```sh
   git tag v1.0.1
   git push origin main --tags
   ```

6. **Export the `.unitypackage`** — in Unity:
   `Tools ▸ Autech ▸ Export LevelPlay .unitypackage`
   (menu added by `Assets/Editor/AutechPackageExporter.cs`). It writes to
   `dist/com.autech.levelplay-mediation-<version>.unitypackage`.

   *Manual fallback:* right-click the `com.autech.levelplay-mediation` folder in
   the Project window ▸ **Export Package…** ▸ uncheck dependencies ▸ Export.

7. **Create the GitHub Release** for tag `v1.0.1` and attach the `.unitypackage`
   from `dist/`.

## How others install a released version

**A) Package Manager → Add package from git URL** (recommended):
```
https://github.com/<you>/com.autech.levelplay-mediation.git?path=/Packages/com.autech.levelplay-mediation#v1.0.1
```
The `?path=` points at the embedded package folder; `#v1.0.1` pins the tag.

**B) Offline / from the `.unitypackage`:** download the asset from the GitHub
Release and import it via `Assets ▸ Import Package ▸ Custom Package…`.

> Either way, the consuming project also needs the LevelPlay SDK
> (`com.unity.services.levelplay`), which is declared as a dependency in
> `package.json` and resolves automatically via Package Manager for method (A).
