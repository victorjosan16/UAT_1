# CI: automatic checks and Android debug APK (GitHub Actions + GameCI)

Workflow: `.github/workflows/veliqora-android.yml`. It runs on every push to `ccr-454f53a2-sy5k23` / `main` that touches `Veliqora/`,
and manually from **Actions → Veliqora Android → Run workflow**.

| Job | Needs a Unity license | What it does |
|---|---|---|
| `core` | no | 133 EditMode tests on the pure core, validation of all 300 levels, Unity compile check against reference assemblies |
| `unity-tests` | yes | Unity EditMode **and** PlayMode suites inside the real Unity 6 editor (results uploaded as an artifact) |
| `android-apk` | yes | Debug APK through `VeliqoraBuild.BuildDebugApkCli`, uploaded as artifact `Veliqora-debug-apk-<run>` (kept 14 days) |

Without the secrets the Unity jobs are **skipped**, not failed.

## One-time setup: Unity license secrets (Unity Personal is fine)
1. Install Unity Hub on your computer, sign in, and activate a **Personal** license (Hub → Preferences → Licenses → Add).
2. Find the license file:
   - Windows: `C:\ProgramData\Unity\Unity_lic.ulf`
   - macOS: `/Library/Application Support/Unity/Unity_lic.ulf`
   - Linux: `~/.local/share/unity3d/Unity/Unity_lic.ulf`
3. In GitHub: repository **Settings → Secrets and variables → Actions → New repository secret**, add:
   - `UNITY_LICENSE`: the whole content of `Unity_lic.ulf`
   - `UNITY_EMAIL`: your Unity account e-mail
   - `UNITY_PASSWORD`: your Unity account password
4. Re-run the workflow. The APK appears at the bottom of the run page under **Artifacts** (zip with `Veliqora-debug.apk`).

Never put the license, passwords or a keystore in the repository.

## Notes
- The editor image is chosen from `ProjectSettings/ProjectVersion.txt` (`6000.0.40f1`). The first run downloads it and imports the
  project (expect 30–60 min); later runs reuse the cached `Library/`.
- The debug APK uses labelled **mock ads** (development build). A signed release AAB is still built locally with
  **Veliqora → Build → Android Release AAB** and keystore environment variables (see `BUILD_AND_RELEASE.md`), so signing secrets
  never leave your machine.
- Install on a phone: unzip the artifact, then `adb install -r Veliqora-debug.apk` or copy the file to the phone and open it
  (allow "install unknown apps").
