# Sistemul vizual — unde se configurează

Tot aspectul jocului e construit din cod, de `Assets/_Game/Editor/GameSetup.*.cs`, la fiecare build.
Componentele de mai jos pot fi reglate și din Inspector (când ai Editorul), dar **valorile scrise de GameSetup
se reaplică la build-ul din cloud**, deci schimbările permanente se fac în fișierele GameSetup.

## Direcție artistică
- Forme cu muchii rotunjite: `Scripts/Visual/RoundedBoxMesh.cs` generează mesh-urile; GameSetup le salvează în `Art/Meshes/`.
- Paletă caldă, materiale mate: `GameSetup.Data.cs → CreateMaterials()` (culoare hex + netezime pentru fiecare material).
- Lumină caldă, ambient în 3 tonuri, ceață ușoară: `GameSetup.Scene.cs → BuildLighting()`.
- Font: Baloo 2 ExtraBold (licență OFL, `Art/Fonts/Baloo2-OFL.txt`).

## Personaj (jucător și muncitori)
| Ce | Unde |
|---|---|
| Model (cască, lanternă, cămașă, salopetă, mănuși, bocanci, rucsac, târnăcop) | `GameSetup.Scene.cs → BuildMiner()` |
| Animații Idle / Walk / Mine / Carry / șezut în utilaj | `CharacterAnimator` pe obiectul **Player/Model** (sau **WorkerN/Model**) |
| Inspector `CharacterAnimator` | Oase (Hips, Torso, Head, LegL/R, ArmL/R); `Full Speed`, `Steps Per Second`, `Leg/Arm Swing`, `Swing Duration`, `Arm Raised/Strike`, `Carry Lean` |
| Saltul stivei la colectare | `StackBounce` pe **Player** |

Modelul e copilul „Model”, deci colliderul (CharacterController) și mișcarea nu sunt afectate.

## Mediu și zone
- Decor (pete de nisip, drumuri, marginea carierei din roci, copaci, lăzi, butoaie, felinare): `BuildWorld()`.
  Tot decorul e marcat **static** → Unity îl unește la build (static batching = mai puține draw calls).
- Plăcile de zonă au bordură colorată, interior deschis și iconiță 3D plutitoare:
  - `ZoneHighlight` (pe fiecare placă): `Pulse Amount`, `Pulse Speed` — pulsează când stai pe zonă;
  - `FloatingBob` (pe iconiță): `Amplitude`, `Frequency`, `Spin Speed`.
- Culorile zonelor: VINDE verde, UPGRADE mov, CUMPĂRĂ portocaliu, RAFINĂRIE cyan, COMENZI albastru.

## HUD
| Element | Script | Poziție |
|---|---|---|
| Zona sigură (notch, bara de gesturi) | `SafeAreaFitter` pe **Canvas/SafeArea** | tot HUD-ul e copilul ei |
| Nivel (strat) + progresul stratului | `LayerHud` | stânga sus + bară sub bani |
| Bani + indicator „x2” | `MoneyHud` | sus centru |
| Pauză / setări (sunet, vibrații, grafică, resetare) | `SettingsPanel` | dreapta sus |
| Misiune compactă (comanda sau „curăță stratul”) | `OrderHud` | sub bara de sus |
| Butoane bonus (reclame) | `RewardedOffersHud` | dreapta jos |
| Joystick (aspect) | `JoystickSkin` pe **JoystickBG** | jos centru |
| Texte plutitoare („+$12”, „FORȚĂ NV.3!”) | `FloatingTextUI` | în lume |

- Stil (culori, fonturi, sprite-uri proprii): `Data/UiTheme.asset`. Câmpurile **Panel Sprite** și **Circle Sprite**
  goale = sprite-uri rotunjite generate din cod (`ProceduralSprites`); poți pune acolo grafică din Illustrator.
- Text: `UiLabel` folosește **TextMeshPro** dacă fontul TMP a putut fi generat la build, altfel textul clasic
  cu același font Baloo 2 (jocul arată la fel de bine în ambele cazuri; în log apare un avertisment dacă se folosește rezerva).
- Animații UI: `PunchScale` (puls), `PressScale` (butoane apăsate), `PopIn` (ferestre).

## Efecte (toate prin ObjectPool)
| Efect | Script (pe `_Systems/Vfx`) |
|---|---|
| Scântei la lovitură | `ImpactVfx` |
| Bucăți de rocă 3D + praf la spargere | `BlockBreakParticles` (`Chunk Mesh`, `Chunk Material`, `Particle Count`) |
| Sclipire la colectare | `CollectVfx` |
| Ploaie de monede la vânzare | `SellVfx` (`Min Interval`) |
| Inel + scântei + „salt” la upgrade | `UpgradeVfx` (`Player Model`) |

Materiale: `M_Particle` (alpha clip — fără amestec transparent, deci fără overdraw costisitor) și `M_ParticleSolid`.

## Performanță
- `QualityProfile` (`_Systems`): ÎNALTĂ (60 FPS, umbre 30 m, MSAA 2x), ECHILIBRATĂ (60 FPS, umbre 18 m, rezoluție 90%),
  ECONOMIC (30 FPS, fără umbre, rezoluție 80%). Se alege automat după telefon; jucătorul îl schimbă din Pauză → GRAFICĂ.
- `PerformanceGovernor`: dacă FPS-ul mediu e prea mic pe ÎNALTĂ, coboară automat la ECHILIBRATĂ.
- URP: fără HDR, o cascadă de umbre (`GameSetup.Project.cs → ConfigurePipeline`).
- Mesh-uri comune (un singur mesh pentru toate blocurile), decor static, particule puține și scurte, efecte doar lângă jucător.

## iOS
Același Bundle ID (`com.victorjosan.quarryempire`), portret, zona sigură tratată de `SafeAreaFitter`.
Build-ul iOS cere un cont Apple Developer; în Unity Build Automation se adaugă o configurație iOS separată.
