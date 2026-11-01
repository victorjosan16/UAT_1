# Build automat Voxel Bloom 3D (iOS + Android)

Trei moduri, toate folosesc `Assets/VoxelBloom/Editor/BuildScript.cs`:

| Mod | Comandă |
|---|---|
| Din Editor | meniul **Voxel Bloom → Build → iOS (Xcode project)** / **Android (AAB)** |
| Linie de comandă | `UNITY=<cale Unity> tools/build.sh ios` sau `... android` |
| CI (GitHub Actions) | `.github/workflows/voxel-bloom.yml`: la push pe `main` sau manual (*Run workflow*, opțiunea „upload”) |

## Setări aplicate automat de BuildScript

**Comune:** nume produs „Voxel Bloom 3D”, companie Animatix, versiunea din `-buildVersion` / `VB_VERSION`
(implicit 1.0.0), orientare doar Portrait, Linear color space, URP + TMP + scena Bootstrap create dacă lipsesc.

**iOS:**
- Bundle ID `com.animatix.voxelbloom3d` (suprascris cu `IOS_BUNDLE_ID`)
- IL2CPP, ARM64, iOS minim **13.0**, iPhone + iPad, full screen
- Build number din `-iosBuildNumber` / `VB_BUILD_NUMBER` (în CI = numărul rulării, mereu crescător)
- **Automatic signing** cu `APPLE_TEAM_ID`. Unity produce proiectul Xcode, iar semnarea și arhivarea se fac în Xcode (job-ul macOS).
- Jocul e offline: nu are nevoie de permisiuni, ATT sau `NSUserTrackingUsageDescription`.

**Android:**
- Package `com.animatix.voxelbloom3d`, IL2CPP, ARM64, min API 24, target API = cel mai nou instalat
- **AAB** (pentru Google Play); APK cu `-apk` sau `VB_ANDROID_FORMAT=apk`
- `versionCode` din `-androidVersionCode` / `VB_BUILD_NUMBER`
- Fără permisiunea INTERNET
- Semnare **doar** din secrete/variabile de mediu (keystore-ul nu se salvează în proiect). Fără keystore iese un build debug, care nu poate fi urcat pe Play.

## Secrete GitHub (Settings → Secrets and variables → Actions)

| Secret | Pentru | De unde |
|---|---|---|
| `UNITY_LICENSE` | toate job-urile Unity | Personal: conținutul fișierului `.ulf` (vezi game.ci/docs → Activation). Pro: folosește `UNITY_SERIAL` în loc. |
| `UNITY_EMAIL`, `UNITY_PASSWORD` | toate | contul Unity |
| `APPLE_TEAM_ID` | iOS | developer.apple.com → Membership (10 caractere) |
| `APPSTORE_KEY_ID`, `APPSTORE_ISSUER_ID` | iOS | App Store Connect → Users and Access → Integrations → **App Store Connect API**, cheie cu rol *App Manager* |
| `APPSTORE_P8` | iOS | conținutul text al fișierului `AuthKey_XXXX.p8` descărcat o singură dată |
| `ANDROID_KEYSTORE_BASE64` | Android | `base64 -i upload.keystore` (pe Linux: `base64 -w0 upload.keystore`) |
| `ANDROID_KEYSTORE_PASS`, `ANDROID_KEYALIAS_NAME`, `ANDROID_KEYALIAS_PASS` | Android | parolele/alias-ul keystore-ului |
| `GOOGLE_PLAY_SERVICE_ACCOUNT_JSON` | upload Play (opțional) | Google Cloud service account cu acces în Play Console → Users and permissions |

Creare keystore (o singură dată, păstrează-l în siguranță, nu în git):
`keytool -genkeypair -v -keystore upload.keystore -alias voxelbloom -keyalg RSA -keysize 2048 -validity 10000`

## Pregătiri unice în conturi
- **Apple:** creează App ID-ul `com.animatix.voxelbloom3d` și aplicația în App Store Connect (cu același bundle ID). Cu API key și *automatic signing*, certificatele și profilele sunt create automat de Xcode în CI.
- **Google Play:** creează aplicația cu package-ul `com.animatix.voxelbloom3d`, activează Play App Signing și **urcă manual primul AAB** (API-ul Play nu permite prima încărcare). După aceea, workflow-ul urcă automat pe track-ul *internal* (ca draft).

## Ce face workflow-ul
1. `tests`: testele Node ale regulilor și nivelurilor, apoi testele Unity EditMode + PlayMode.
2. `build-ios` (Ubuntu): exportă proiectul Xcode (artifact `ios-xcode-project`).
3. `archive-ios` (macOS 15): `xcodebuild archive` cu semnare automată, apoi export `.ipa` (artifact `ios-ipa`). Cu „upload” bifat, urcă direct în **TestFlight**.
4. `build-android` (Ubuntu): AAB semnat (artifact `android-aab`). Cu „upload” bifat, urcă pe Google Play internal.

## Versiunea de Unity
`ProjectSettings/ProjectVersion.txt` fixează **6000.6.7f1** (versiunea folosită de Unity Build Automation), iar game-ci alege imaginea Docker după ea. Dacă
folosești altă versiune de Unity 6, deschide proiectul o dată cu ea; Unity rescrie fișierul, iar tu îi faci commit.

## Status
Scriptul de build și workflow-ul sunt scrise și au trecut verificarea de sintaxă (C# și YAML), dar **nu au fost rulate**:
nu există Unity în mediul în care au fost create, iar secretele trebuie adăugate de tine. Prima rulare va
confirma dacă mai e nevoie de ajustări.

## Unity Build Automation (Cloud Build, cloud.unity.com)
Alternativă la GitHub Actions, configurată din browser:
- **Repository / branch:** `victorjosan16/UAT_1`, ramura `ccr-1b2ddcf1-u2muz2` (sau `main`, după merge)
- **Project subfolder / Project path:** `voxel-bloom` (proiectul Unity nu e în rădăcina repo-ului)
- **Unity version:** 6000.6.x (aceeași cu `ProjectSettings/ProjectVersion.txt`)
- **Bundle ID:** `com.animatix.voxelbloom3d`
- **Credentials:** „Auto-generated debug keystore” pentru test, sau keystore-ul tău de upload pentru Google Play
- **Advanced → Pre-Export Method:** `VoxelBloom.EditorTools.BuildScript.CloudBuildPreExport` (**obligatoriu**:
  creează scena Bootstrap, URP și TMP, care nu sunt în repo)
- **Android → Build App Bundle:** bifat pentru Google Play, debifat pentru APK de test
