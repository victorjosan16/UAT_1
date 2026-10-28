# Drop Quest – Aventurile lui Bobo · Audit de calitate (faza 1)

Data: 2026-10-28 · Ramura auditului: `audit/quality` (pornită din `sugar-drop`, commit `2d12d75`).
Lista de lucru care continuă între sesiuni: [`AUDIT_CHECKLIST.md`](AUDIT_CHECKLIST.md). Inventarul: [`INVENTORY.md`](INVENTORY.md).

> **Limite ale acestui mediu.** Aici nu există Unity Editor și nici dispozitiv Android. Am putut rula: compilarea C# a
> scripturilor (cu stub-uri Unity și uGUI din sursă), cele 102 teste EditMode prin `Tools/CoreTests` (.NET 8) și
> simulatorul de echilibru pe nivelurile 1–100. **Nu** am putut rula: testele PlayMode, jocul în sine, profilerul,
> build-ul Android. Nicio afirmație de mai jos despre performanță sau aspect nu e măsurată pe dispozitiv.

## A. Prezentarea proiectului
| Element | Valoare |
|---|---|
| Unity | 6000.0.58f2 |
| Randare | URP 17.0.4 în manifest; jocul e 2D (SpriteRenderer + uGUI), fără shadere proprii |
| Pachete relevante | Input System 1.11.2, Mobile Notifications 2.4.3, Cloud Save 3.2.2, Leaderboards 2.3.3, Test Framework 1.4.5 |
| Reclame | Cod LevelPlay prin simbolul `SUGARDROP_LEVELPLAY`; **pachetul nu e instalat, cheile sunt goale** |
| Scene | Una, construită din cod (`Editor/SugarDropSetup.cs`) la fiecare build |
| Grafică | Procedurală + PNG/JPG înlocuibile în `Resources/Art` (49 fișiere) |
| Cod propriu | 101 fișiere C#, ~20.700 linii; 3 asmdef (Core, teste EditMode, teste PlayMode) |

## B. Inventarul fișierelor
Vezi [`INVENTORY.md`](INVENTORY.md). Toate cele 101 fișiere au trecut prin scanarea automată; 24 au fost citite integral,
4 parțial. **Nu se poate afirma că fiecare fișier a fost revizuit manual** – restul rămân în listă pentru fazele următoare.

## C. Arhitectura
```
SugarDropGame (orchestrator, stări Menu/Map/Playing/Finale/Result)
 ├─ Core (pur, testabil): BlastEngine, BoardStep, LevelConfig, LevelGoals, ScoreRules, BossBattle,
 │                         SugarSave(+Data), SugarLoc, SugarDaily/Extras, LevelStory, MapLayout, LifeClock
 ├─ Board: Board (animare), Tile, BoardInput, Playfield, Jar, Candy
 ├─ Visual: SugarSprites/PieceSprites (procedural + ArtLibrary), Backdrop, DangerScene, Mascot, BoboArt
 ├─ UI (uGUI din cod): MainMenu, MapScreen, SugarHud, BoosterBar, ResultPanel, SettingsPanel, DialogPanel,
 │                     StoryPanel, LevelStoryPanel, IntroPanel, EventsPanel, Wheel/Wardrobe/Leaderboard, QuestNav
 ├─ Effects: SugarVfx (pool de particule), SugarAudio (sunet procedural)
 └─ Online/Ads: SugarCloudSave, SugarLeaderboard, SugarNotifications, SugarAds (singleton DontDestroyOnLoad)
```
Constatări (din cod, nu presupuneri):
- **Bun:** logica de joc e separată în `Core` (asmdef propriu, fără MonoBehaviour), ceea ce permite cele 102 teste fără Unity.
- **Bun:** fără `FindObjectOfType` în bucle (o singură căutare, în `SugarFonts`, la schimbarea fontului); fără LINQ.
- **Bun:** abonamentele la evenimente statice sunt perechi `+=`/`-=` (`SugarQuality.Changed`, `SugarCloudSave.Restored`, `SugarAds.Unavailable`).
- **De urmărit:** `SugarDropGame.cs` are ~2.100 de linii și coordonează aproape tot – merge, dar e locul cu cel mai mare risc
  de regresie. Recomandare: nicio rescriere; doar extrageri mici, cu teste, când e nevoie.
- 31 de clase au `Update()`; majoritatea ies imediat când ecranul lor e închis (verificat la cele citite).

## D. Jocul (motorul de potriviri)
Verificat prin testele existente (102, toate trec): potriviri, speciale și combo-uri, gravitație, cascade, amestecare,
obiective, scor și stele, boși, foc/apă, Bobo pe tablă, vieți, misiuni, salvare/JSON, chei de traducere.
Verificat prin citirea codului:
- Victoria salvează recompensele **o singură dată, imediat**, înainte de finalul animat (`Win()`): închiderea aplicației în timpul
  finalului nu pierde și nu dublează monedele.
- `Board.Build` oprește toate corutinele tablei → „Reîncearcă” în timpul unei cascade nu lasă animații vechi active.
- Pauza (`timeScale = 0`) e permisă doar în starea `Playing`; în timpul finalului butonul Înapoi nu face nimic.
Neverificat aici (cere joc rulat): atingeri rapide repetate pe dispozitiv, sincronizarea vizuală tablă–logică în timp real.

## E. Validarea nivelurilor (simulare)
Simulator: `Tests/EditMode/BalanceSimulator.cs` (bot care alege cea mai bună mutare). 1–100 × 20 de seed-uri = **2.000 de jocuri**.
Rezultatul complet: [`sim-levels-1-100.txt`](sim-levels-1-100.txt).

| Dificultate | Niveluri | Rată de câștig (bot) |
|---|---|---|
| Easy | 25 | 96% |
| Normal | 46 | 90% |
| Hard | 13 | 87% |
| Boss | 8 | 86% |
| VeryHard | 6 | 63% |
| MegaBoss | 2 | 72% |

- Niciun nivel imposibil și niciunul sub 40%.
- Pe capitole: între 82% (capitolul 4) și 94% (capitolul 10).
- Botul joacă mai bine decât un om obișnuit, deci ratele reale vor fi mai mici; diferența nu se poate măsura fără date de la jucători.
- **Nu am schimbat nicio dificultate.**

## F. Povestea lui Bobo
Clasele reale (nu cele din prompt): `StoryPanel` (carte: început, episoade, „bine ai revenit”), `LevelStory` + `LevelStoryPanel`
(scena-bandă desenată dinaintea nivelului), `BoboView`/`Mascot`/`BoboArt` (ipostaze), `DangerScene` (pericolul sub tablă).
- Scena nivelului apare **o singură dată**, doar pentru nivelul nou (`LevelStory.ShouldShow`, testat), se poate sări, se salvează (`levelStorySeen`).
- La pierdere povestea nu avansează (episoadele se marchează doar la victorie).
- Lipsesc: reacții de poveste legate de evenimentele din timpul nivelului (doar Bobo de sub tablă reacționează), scene pentru 14 din nivelurile 1–30.

## G. UI/UX
Toate ecranele sunt construite din cod pe o rezoluție de referință 1080×1920 (potrivire pe lățime) + `SafeAreaFitter`.
Neverificat vizual. Riscuri cunoscute: texte lungi în rusă/germană; Bobo din colțul ferestrelor pe ecrane înguste;
panourile de poveste mărite din ~360 px (neclare).

## H. Performanță (estimări din cod, nemăsurate)
- **Tablă:** fiecare piesă nouă e un `GameObject` nou (cu straturi de SpriteRenderer), iar cele sparte se distrug (`Board.cs` 201/478/630).
  Pe cascade mari înseamnă zeci de creări/distrugeri → posibile sărituri de cadre pe telefoane slabe. Recomandare: pool de piese,
  **doar după profilare** pe dispozitiv (risc de regresie vizuală).
- Particulele sunt deja în pool (`SugarVfx`); calitatea grafică (3 trepte) limitează particulele și FPS-ul (30 pe Scăzut).
- Imaginile din `Resources/Art` sunt comprimate la import (`ArtImport`: CompressedHQ, max 2048) și ținute în cache după prima folosire.

## I. Android
| Setare | Stare |
|---|---|
| Application id | `com.victorjosan.sugardrop` |
| Versiune / versionCode | din numărul build-ului din cloud |
| Backend / arhitectură | IL2CPP, ARM64 |
| Orientare | Portret |
| Stripping | Minimal |
| Target SDK | **nesetat explicit** (Unity alege cel mai mare SDK instalat pe mașina de build) |
| AAB / keystore | din setările Unity Build Automation (nu din cod) |

Cerința Google Play (verificată pe web, octombrie 2026): **aplicațiile noi și actualizările trebuie să țintească API 36 (Android 16)
din 31 august 2026**. Trebuie confirmat în Play Console că AAB-ul construit are `targetSdkVersion 36`.

## J. Securitate și integritatea datelor
- Salvarea: JSON în PlayerPrefs, scrisă după fiecare schimbare importantă. Câmpurile noi au valori implicite → salvările vechi se citesc.
- Cloud: anonim pe dispozitiv; se descarcă înainte de orice upload și se păstrează progresul mai avansat (`IsAhead`).
  **Limită:** contul anonim nu supraviețuiește reinstalării, deci cloud-ul nu recuperează progresul după dezinstalare.
- Fără secrete în cod (cheile de reclame sunt goale; e-mailul nu apare în depozit).

## K. Teste automate
| Suită | Rezultat | Cum |
|---|---|---|
| EditMode (102 teste) | **102/102 trec** | `cd Tools/CoreTests && dotnet test` (fără Unity) sau Unity → Window → General → Test Runner → EditMode |
| PlayMode (1 fișier) | **nerulat aici** | Unity → Test Runner → PlayMode → Run All |
| Simulare 1–100 × 20 | rulată (vezi E) | `dotnet run` în proiectul de simulare / `BalanceSimulator` |

## L. Probleme găsite și reparate (pe `audit/quality`)
| # | Sev. | Fișier | Problema | Corecția | Dovadă |
|---|---|---|---|---|---|
| 1 | P2 | `Ads/SugarAds.cs` `AfterClose` | Recompensa reclamei venea ignorată dacă sosea la >1 s după închidere (SDK-ul nu garantează ordinea) → jucătorul vedea reclama și nu primea nimic | Așteptare până la 3 s | Compilare OK; neverificat cu SDK real (neinstalat) |
| 2 | P2 | `Core/SugarSave.cs` `Load` | O salvare ilizibilă era înlocuită cu una goală și apoi suprascrisă → progres pierdut fără cale de recuperare | Textul vechi e păstrat în `sugardrop_save_corrupt` înainte de a porni de la zero | Compilare + 102 teste OK |

## M. Probleme rămase (neschimbate, cer decizia ta)
| # | Sev. | Problema | Ce propun |
|---|---|---|---|
| R1 | **P1 (lansare)** | Reclamele reale nu sunt configurate: în orice build apare **„RECLAMĂ DE TEST”**, iar recompensa vine gratis | Instalezi LevelPlay și completezi cheile în `SugarAdsConfig`, sau decizi lansarea fără reclame (atunci ascundem butoanele „reclamă”) |
| R2 | **P1 (lansare)** | Target SDK nesetat explicit; Google cere API 36 | Verifici în Play Console; dacă e sub 36, îl fixăm din cod la 36 |
| R3 | P2 | Piese create/distruse la fiecare cascadă | Pool de piese, după profilare pe telefon |
| R4 | P2 | Cloud anonim → progresul nu revine după reinstalare | Legarea contului (Google Play Games) – funcție nouă |
| R5 | P2 | Panourile de poveste sunt imagini mărite (neclare) | Imagini noi 1536×1024 fără text |
| R6 | P3 | `SugarDropGame.cs` foarte mare | Extrageri mici, cu teste, doar când se lucrează acolo |
| R7 | P3 | 73 de fișiere încă necitite integral | Fazele 2–5 din listă |

## N. Recomandări viitoare
Pool de piese (după profilare) · legare cont pentru cloud · teste PlayMode pentru pornire/pauză/victorie/pierdere ·
scene de poveste pentru toate nivelurile 1–30 · analytics minime (rata de câștig reală pe nivel).

## O. Listă pentru lansarea pe Google Play
- [ ] AAB construit din `sugar-drop` (sau `audit/quality` după unire), semnat cu cheia de upload
- [ ] `targetSdkVersion` = 36 confirmat în Play Console
- [ ] Reclame: LevelPlay instalat + chei, **sau** butoanele de reclamă ascunse
- [ ] Formularul „Data safety” (Cloud Save, Leaderboards, reclame/ID publicitar dacă există)
- [ ] Politica de confidențialitate publicată (placeholder-ul `[email]` completat de tine)
- [ ] Clasificarea conținutului (IARC), public țintă
- [ ] Testare internă pe 2–3 telefoane (slab / mediu / bun): pornire, nivel complet, pierdere, pauză, reclamă, fundal–revenire
- [ ] Testele PlayMode rulate în Unity

## Tabel de evaluare (estimări inginerești, nu certificare)
| Domeniu | Scor | De ce |
|---|---|---|
| Calitatea codului | 74 | Cod comentat, consecvent, fără tipare periculoase găsite la scanare; fișierul central e foarte mare |
| Arhitectură | 70 | Logica pură separată și testată; orchestratorul unic concentrează riscul |
| Fiabilitatea jocului | 78 | 102 teste + 2.000 de jocuri simulate fără blocaje; nimic rulat pe dispozitiv |
| Consecvența UI/UX | 60 | Stil unitar în cod, dar **neverificat vizual** |
| Performanță | 55 | Pool la particule, trepte de calitate; creare/distrugere de piese; nimic măsurat |
| Integrarea poveștii | 68 | Episoade, scene o dată, salvate, testate; lipsesc reacții în timpul nivelului și scene pentru 14 niveluri |
| Pregătire Android | 50 | Setări de bază corecte; reclame de test și target SDK neconfirmat blochează lansarea |
| Mentenabilitate | 72 | Teste fără Unity, documentație în `docs/`; fișiere mari |
