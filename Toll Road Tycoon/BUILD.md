# Build automat (GitHub Actions)

Fișierul `.github/workflows/toll-road-tycoon.yml` (în rădăcina repository-ului) pornește automat la
fiecare push pe `toll-road-tycoon` sau `main` care schimbă ceva în `Toll Road Tycoon/`. Se poate
porni și manual, din **Actions → Toll Road Tycoon → Run workflow**.

| Job | Ce face | Are nevoie de |
|---|---|---|
| **Simulation tests** | compilează logica jocului și rulează toate testele NUnit (fără Unity) | nimic, merge imediat |
| **Android apk** | build de test `TollRoadTycoon.apk` (build de dezvoltare, reclame în mod TEST) | licența Unity |
| **Android aab** | build de release `TollRoadTycoon.aab` pentru Google Play | licența Unity + cheia de semnare |

Fișierele rezultate apar la rularea respectivă, în secțiunea **Artifacts**. La un tag care începe cu
`trt-v` (de exemplu `trt-v0.1.0`), fișierele se atașează și la un GitHub Release.

Build-ul Unity folosește [GameCI](https://game.ci) (`game-ci/unity-builder`). Versiunea de Unity e
citită din `ProjectSettings/ProjectVersion.txt`.

## 1. Licența Unity (obligatoriu pentru build)

Fără aceste secrete rulează doar testele, iar job-urile Android sunt sărite cu un avertisment.

1. Instalează Unity Hub pe calculator, autentifică-te și activează licența **Personal**
   (Hub → Preferences → Licenses → Add → Get a free personal license).
2. Găsește fișierul licenței:
   * Windows: `C:\ProgramData\Unity\Unity_lic.ulf`
   * macOS: `/Library/Application Support/Unity/Unity_lic.ulf`
   * Linux: `~/.local/share/unity3d/Unity/Unity_lic.ulf`
3. În GitHub: repository → **Settings → Secrets and variables → Actions → New repository secret**:
   * `UNITY_LICENSE`: tot conținutul fișierului `Unity_lic.ulf`
   * `UNITY_EMAIL`: emailul contului Unity
   * `UNITY_PASSWORD`: parola contului Unity

## 2. Cheia de semnare (pentru Google Play)

Fără cheie, AAB-ul se semnează cu cheia de test a Unity: merge pentru teste, dar **nu** poate fi
încărcat în Google Play.

1. Creează cheia o singură dată, de exemplu cu `keytool` (vine cu Java):
   ```bash
   keytool -genkeypair -v -keystore release.keystore -alias tollroad -keyalg RSA -keysize 2048 -validity 10000
   ```
   Păstrează fișierul și parolele în siguranță: fără ele nu mai poți publica actualizări.
2. Transform-o în text base64:
   * Linux/macOS: `base64 -w0 release.keystore` (pe macOS: `base64 -i release.keystore`)
   * Windows (PowerShell): `[Convert]::ToBase64String([IO.File]::ReadAllBytes("release.keystore"))`
3. Adaugă secretele:
   * `ANDROID_KEYSTORE_BASE64`: textul base64
   * `ANDROID_KEYSTORE_PASS`: parola keystore-ului
   * `ANDROID_KEYALIAS_NAME`: aliasul (în exemplu, `tollroad`)
   * `ANDROID_KEYALIAS_PASS`: parola aliasului

Cheia nu se salvează niciodată în repository: fișierele `*.keystore` și `*.jks` sunt ignorate de git.

## Detalii

* Numărul build-ului Android (`versionCode`) este numărul rulării din GitHub Actions, deci crește singur.
* Directorul `Library/` al Unity e păstrat în cache între rulări, ca să meargă mai repede (primul build
  durează mult, circa 20–40 de minute).
* Build-ul pornește prin `TollRoadTycoon.EditorTools.BuildAndroid.Ci`, care configurează întâi proiectul
  (scena Boot, portret, ARM64, IL2CPP) și apoi construiește.
* Pe calculatorul tău poți folosi în continuare meniul **Tools → Toll Road Tycoon → Build Android APK / AAB**.

> Workflow-ul nu a putut fi rulat din mediul în care a fost scris. Prima rulare pe GitHub e și primul
> test real. Dacă un pas eșuează, deschide jurnalul rulării (Actions → rularea → job-ul roșu).
