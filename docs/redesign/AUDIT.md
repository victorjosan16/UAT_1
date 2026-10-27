# Sugar Drop — Audit tehnic (înainte de „Soft Pop Edition”)

Starea de pornire: ramura `sugar-drop`, commit `e2486eb`. Redesign-ul se face pe ramura `redesign/soft-pop`.

## 1. Proiectul
| Element | Stare |
|---|---|
| Unity | 6000.0.58f2 (Unity 6) |
| Randare | Pachetul URP 17.0.4 e în manifest, dar Sugar Drop nu setează un pipeline propriu: totul e 2D, cu `SpriteRenderer` și uGUI. Nu există shadere sau materiale proprii. |
| Scene | **Niciuna în git.** Scena `Assets/SugarDrop/Scenes/SugarDrop.unity` e construită din cod de `Editor/SugarDropSetup.cs`: meniul „Sugar Drop → Construiește jocul” sau Pre-Export-ul din Unity Build Automation (`SugarDropCloudBuild.PreExport`). |
| Grafica | **Procedurală.** Toate sprite-urile sunt desenate din cod (`Visual/SugarSprites.cs`, `Visual/PieceSprites.cs`) și ținute în cache. Singurele imagini din proiect sunt iconița (`Art/Icon`) și fontul. |
| Font | Baloo 2 ExtraBold (`_Game/Art/Fonts`), licență SIL OFL, deci se poate folosi comercial. Pentru rusă și ucraineană se încarcă un font chirilic (`UI/SugarFonts.cs`). |
| Interfață | uGUI construit din cod, cu framework-ul comun `QuarryEmpire` din `_Game/Scripts/UI`: `UiFactory`, `UiTheme`, `UiLabel`, `UiRoot`, `SafeAreaFitter`, `ProceduralSprites`, `PopIn`, `PunchScale`. Tema `SugarTheme.asset` e creată de setup. |
| Alt cod | `_Game/` conține și alt joc (Quarry Empire), cu propriul setup. Framework-ul UI e comun, deci **nu modificăm `_Game`**: redesign-ul se face doar în `Assets/SugarDrop`. |
| Android | Portret, IL2CPP, ARM64, stripping Minimal (pentru SDK-urile de reclame), `com.victorjosan.sugardrop`. versionCode = numărul build-ului din cloud. |
| Teste | `Tools/CoreTests` (.NET 8 + stub-uri Unity): 99 de teste pe logica pură (`Scripts/Core`), rulate fără Unity. |

## 2. Sistemele jocului și fișierele lor
| Sistem | Fișiere |
|---|---|
| Orchestrare (stări, meniu, hartă, nivel, final, pauză) | `Game/SugarDropGame.cs` |
| Reguli tablă: potriviri 3+, speciale, combo-uri, gravitație, cascade, amestecare, ieșiri, foc, Bobo | `Core/BlastEngine.cs`, `Core/BoardStep.cs`, `Core/TileKind.cs` |
| Animarea tablei, input | `Board/Board.cs`, `Board/Tile.cs`, `Board/BoardInput.cs` |
| Cadrul tablei, pâlnie, borcan | `Board/Playfield.cs`, `Board/Jar.cs`, `Board/Candy.cs`, `Board/CandySpawner.cs` |
| Configurația nivelurilor (deterministă, nelimitată), dificultate, tipuri | `Core/LevelConfig.cs` |
| Obiective | `Core/LevelGoals.cs` |
| Scor și stele | `Core/ScoreRules.cs` |
| Boși (atacuri, faze, Mega-Boss) | `Core/BossBattle.cs` |
| Teme pe ținut | `Core/ChapterTheme.cs`, `Visual/LandStyle.cs`, `Visual/Backdrop.cs` |
| Pericolul de sub tablă, globul boșilor | `Visual/DangerScene.cs` |
| Mascota Bobo (în lume și în UI) | `Visual/Mascot.cs`, `UI/BoboView.cs` |
| Efecte vizuale (pool de particule) | `Effects/SugarVfx.cs` |
| Sunet procedural și vibrații | `Effects/SugarAudio.cs` |
| Salvare: PlayerPrefs JSON (`sugardrop_save`) și Unity Cloud Save | `Core/SugarSave.cs`, `Online/SugarCloudSave.cs` |
| Vieți | `Core/LifeClock.cs`, `SugarSave` (5 vieți, regenerare în timp) |
| Recompense zilnice, misiuni, roata, costume, evenimente, ajutoare la start | `Core/SugarDaily.cs`, `Core/SugarExtras.cs` |
| Clasament, notificări, servicii UGS | `Online/*` |
| Reclame LevelPlay (prin reflecție, opționale) | `Ads/SugarAds.cs`, `Ads/SugarAdsConfig.cs`, `Editor/LevelPlaySetup.cs` |
| Cumpărături în aplicație | **Nu există** (magazinul a fost exclus la cerere). |
| Localizare (11 limbi) | `Core/SugarLoc.cs` |
| Calitate grafică (3 niveluri) | `Core/SugarQuality.cs` |
| Ecrane | `UI/MainMenu.cs` (dashboard), `UI/MapScreen.cs`, `UI/SugarHud.cs`, `UI/BoosterBar.cs`, `UI/LevelStartPanel.cs`, `UI/ResultPanel.cs`, `UI/SettingsPanel.cs` (pauză și setări), `UI/DialogPanel.cs`, `UI/StoryPanel.cs`, `UI/WelcomePanel.cs`, `UI/LeaderboardPanel.cs`, `UI/WheelPanel.cs`, `UI/WardrobePanel.cs`, `UI/TutorialCoach.cs`, `UI/Motion/*` |

## 3. Tipurile de nivel existente (lista de compatibilitate)
| Tip | Obiectiv | Pierdere | Vizual specific |
|---|---|---|---|
| Score | scor | mutări 0 | — |
| Collect | N bomboane dintr-o culoare | mutări 0 | bomboane decorative în borcan |
| Ingredients | bomboanele ambalate ies jos | mutări 0 | borcanul, săgețile de ieșire |
| Frosting | glazura de pe celule | mutări 0 | glazura |
| Crates / Ice / Chains / Chocolate | obstacolele | mutări 0 | lăzi, gheață, lanțuri, ciocolată |
| Specials | N speciale activate | mutări 0 | — |
| Garden | ghivece înflorite | mutări 0 | ghiveci și răsad |
| Defuse | bombe cu ceas dezamorsate | o bombă ajunge la 0 | contorul de pe bombă |
| Bonus (Sugar Rush) | — (timp, 40 s) | — | cronometrul în discul de mutări |
| Boss / Mega-Boss | viața Boss-ului | mutări 0 | intrarea Boss-ului, globul cu Bobo |
| Descent / Frozen (Bobo pe tablă) | Bobo ajunge jos | mutări 0 | piesa Bobo, gheața, săgețile de ieșire |
| Blaze (foc și apă) | focul stins | focul ajunge lângă Bobo | flacăra, Bobo speriat |
| Portaluri (variantă pe nivel) | — | — | portalurile animate |
| Nivelul 1 (antrenament) | tutorial ghidat | — | `TutorialCoach`, `HintGuide` |

## 4. Dependențe importante pentru redesign
- **Culorile** vin din 4 surse: `UiTheme` (din setup), `Palette`, `ChapterTheme` și culori scrise direct în UI. Schimbarea temei afectează toate ecranele.
- **Forma bomboanelor** e doar vizuală (`PieceSprites.Distance`): indexul culorii decide regulile, nu desenul. Silueta se poate schimba fără niciun risc pentru gameplay.
- **Albastrul (index 1)** e „apa” la nivelurile cu foc. Noua piesă albastră trebuie să rămână o picătură, ca regula să se înțeleagă.
- **Textul alb** e folosit pe `PanelColor`. Un panou deschis la culoare cere text închis: trebuie verificat ecran cu ecran.
- **`SugarDropSetup`** reconstruiește scena și tema la fiecare build. Orice culoare a temei trebuie schimbată acolo, nu doar în asset.

## 5. Ce nu poate fi verificat aici
Aici nu există Unity Editor și nici dispozitiv Android. Se pot rula doar compilarea C# (cu stub-uri și uGUI din sursă), cele 99 de teste de logică și randarea de test a sprite-urilor procedurale (portate în Python). Rularea jocului, aspectul pe ecran, performanța și build-ul Android rămân **neverificate** până la un build real.
