# Android Build & Release

> Nothing here has been executed yet: no Unity/Android toolchain was available when this was written.

## Prerequisites
- Unity 6 LTS (6000.0.x) with **Android Build Support** (Android SDK & NDK Tools, OpenJDK) installed through Unity Hub.
- Run **Veliqora → Setup Project** once. It sets the following:

| Setting | Value |
|---|---|
| Product / company | Veliqora / AnimatiX |
| Package | `com.animatix.veliqora` (**placeholder**: verify it is available and owned before release) |
| Orientation | Portrait only |
| Scripting backend | IL2CPP, ARM64 |
| Min SDK | 26 (Android 8.0) |
| Target SDK | *Automatic (highest installed)*. Make sure the installed SDK meets the Google Play target-API requirement in force on your release date. |
| Version | `bundleVersion` 0.1.0, `bundleVersionCode` 1 (bump `bundleVersionCode` for every upload) |
| Icon | `Art/Icons/AppIcon_PLACEHOLDER.png` (replace with final adaptive icon layers in Player Settings → Icon) |

## Debug APK
- Menu **Veliqora → Build → Android Debug APK** → `Builds/Android/Veliqora-debug.apk` (Development build, mock ads active).
- CLI:
  ```bash
  Unity -batchmode -quit -projectPath Veliqora -buildTarget Android \
        -executeMethod Veliqora.EditorTools.VeliqoraBuild.BuildDebugApkCli -logFile build.log
  ```
- Install: `adb install -r Builds/Android/Veliqora-debug.apk`

## Release AAB (signed)
1. Create an upload keystore **outside the repo**:
   `keytool -genkeypair -v -keystore ~/keys/veliqora-upload.jks -alias veliqora -keyalg RSA -keysize 2048 -validity 10000`
2. Export the secrets as environment variables (CI secret store or local shell; never commit them):
   ```bash
   export VELIQORA_KEYSTORE_PATH=~/keys/veliqora-upload.jks
   export VELIQORA_KEYSTORE_PASS=...   VELIQORA_KEY_ALIAS=veliqora   VELIQORA_KEY_PASS=...
   ```
3. Run **Veliqora → Build → Android Release AAB**, or the CLI with `-executeMethod Veliqora.EditorTools.VeliqoraBuild.BuildReleaseAabCli`.
   The build first runs **Validate Project** and refuses to continue if there are problems or signing is missing.
   It clears the passwords from PlayerSettings afterwards.
4. Output: `Builds/Android/Veliqora-<version>-<code>.aab`. Upload it to Play Console with Play App Signing enabled.

`.gitignore` excludes `*.keystore`, `*.jks`, `keystore.properties`, `*.apk`, `*.aab`, `Builds/` and Unity's generated folders.

## Before a release build
- Ads: in non-development builds, mock ads are **automatically disabled** (`AdsFactory`). Either ship with `"mode": "none"`, or implement a real
  adapter (compile with the `VELIQORA_LEVELPLAY` define after implementing `LevelPlayAdsAdapter`) and replace every `REPLACE_WITH_*` value in `ads.json`.
- Set `CommonOverlays.PrivacyPolicyUrl` / `TermsUrl`.
- Complete `Docs/RELEASE_CHECKLIST.md`.

## CI options
- **Ready to use:** `.github/workflows/veliqora-android.yml` (core checks always; Unity tests + debug APK once the Unity license secrets are set). Setup steps: `Docs/CI.md`.
- **GameCI** (`game-ci/unity-builder`, `unity-test-runner`) with a Unity license secret can run the EditMode/PlayMode suites and both build methods above.
- Without Unity, CI can still run `dotnet test Tools/Veliqora.Core.Tests`, `dotnet run --project Tools/LevelGen -- validate`, and
  `dotnet build Tools/UnityCompileCheck`.
