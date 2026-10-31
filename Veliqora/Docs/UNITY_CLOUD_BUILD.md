# Build în Unity Cloud (Build Automation)

Unity construiește APK-ul pe serverele lui, direct din GitHub. Nu ai nevoie de Unity instalat local și nici de fișierul de licență.

## 1. Proiect în Unity Cloud
1. Intră pe https://cloud.unity.com și autentifică-te cu contul Unity.
2. **Projects → Create project** (de exemplu „Veliqora”); dacă ai deja un proiect, îl poți folosi.
3. În proiect: **DevOps → Build Automation → Get started / Configure**.

## 2. Legătura cu GitHub
- Source control: **GitHub** → autorizează aplicația Unity → alege repo-ul `victorjosan16/UAT_1`.

## 3. Configurația de build (Add target / New configuration)
| Câmp | Valoare |
|---|---|
| Platform | **Android** |
| Branch | `ccr-454f53a2-sy5k23` |
| Project subfolder (path) | `Veliqora` |
| Unity version | **6000.0.40f1** (aceeași ca în `ProjectSettings/ProjectVersion.txt`; dacă nu apare, cea mai apropiată 6000.0.x) |
| Build type / Output | **APK** (pentru test pe telefon) sau **AAB** (pentru Google Play) |
| Development build | bifat pentru test (reclamele simulate rămân active și au o etichetă vizibilă) |
| Scripting backend | IL2CPP (îl setează oricum metoda de mai jos) |

### Advanced settings (important)
- **Pre-export method:** `Veliqora.EditorTools.VeliqoraBuild.CloudPreExport`

  Repo-ul nu conține `ProjectSettings.asset`, așa că metoda aceasta aplică setările Android înainte de build:
  pachet `com.animatix.veliqora`, portret, IL2CPP, ARM64, minimum Android 8.0 (API 26). Tot ea verifică dacă
  cele 8 scene sunt în Build Settings.

## 4. Semnarea (Android keystore)
- Pentru test: lasă **Default / Debug keystore**. APK-ul se instalează pe telefon, dar nu poate fi urcat în Google Play.
- Pentru Google Play: încarcă un keystore de upload în configurație (Unity îl păstrează la ei, nu în repo).

## 5. Pornește build-ul
- **Build → Start build** pe configurația creată. Primul build durează mai mult (30–60 min, importul proiectului).
- La final: **Download** → APK. Îl copiezi pe telefon și îl deschizi (permite „instalare din surse necunoscute”).
- Poți activa **Auto-build**, ca să construiască la fiecare push pe branch.

## Important: target separat
Creează un **build target nou** pentru Veliqora (de exemplu `veliqora-android`). Nu refolosi target-ul altui joc
(ex. `room-road-tycoon`): Unity Cloud păstrează cache-ul `Library/` per target, iar cache-ul altui proiect poate strica
build-ul Android („failed to write ... unity-classes.jar”). Dacă totuși refolosești un target, pornește build-ul cu
**Clean build** bifat. Metoda de pre-export șterge oricum proiectul Gradle vechi din cache.

## Dacă pică
Deschide build-ul → **Logs → Full log** și trimite-mi primele erori (liniile cu `error CS` sau `Exception`).
