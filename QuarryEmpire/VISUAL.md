# Quarry Empire — Etapa Vizuală

Ghid pentru modernizarea vizuală: ce s-a schimbat, unde se reglează în Inspector și ce trebuie testat.

> **Stare:** codul a fost compilat cu Roslyn contra DLL-urilor de referință Unity 2021.3 (motor) și 2021.1
> (Editor), plus stub-uri scrise de mână pentru pachete (uGUI/TMP, Input System, Cinemachine, URP).
> Compilarea a trecut fără erori, dar această verificare **nu înlocuiește** deschiderea în Unity 6:
> scena **nu a fost încă rulată** în Editor sau pe telefon. Vezi „Ce trebuie verificat în Unity”.

## 1. Audit

### Ce există în proiect (Etapa 1)

| Sistem | Fișiere | Ce face |
|---|---|---|
| Mișcare | `PlayerInputReader`, `PlayerMovement` | Joystick on-screen (`OnScreenStick`) + WASD, `CharacterController` |
| Minat | `PlayerMiner`, `PickaxeSwing`, `MineBlock`, `BlockHitFeedback` | Minat automat al celui mai apropiat bloc; evenimentele `Swung`, `Damaged`, `Broken` |
| Drop / colectare | `BlockDropSpawner`, `ResourcePickup`, `ResourceCollector`, `ObjectPool` | Bucățile sar din bloc și zboară spre jucător |
| UI | `ResourceCounterDebug` | Text simplu de debug (uGUI `Text`) |
| Setup | `Editor/Stage1Setup.cs` | Generează layerele, URP, materialele, prefab-urile și scena `Game.unity` (și în build-ul din cloud) |

### Ce NU există încă

Cererea pornește de la sisteme care nu sunt încă în proiect:

- **Bani, vânzare, upgrade-uri, salvări, reclame, misiuni** nu există (folderele `Economy/`, `Machines/`,
  `Orders/` sunt goale; README-ul le planifică pentru Etapele 2–9).
- De aceea nu am inventat logică nouă. Am construit **componente vizuale reutilizabile, cu metode publice**
  pe care sistemele reale le vor apela când apar (secțiunea 4). Până atunci:
  - pastila de **bani** arată *valoarea resurselor colectate* (`ResourceData.SellPrice`), doar ca afișaj;
  - **misiunea** e un obiectiv de afișare din `HudStage1Binder`, fără recompense;
  - **zonele de vânzare și de upgrade** sunt doar semnalizare 3D, fără trigger și fără tranzacții;
  - **butoanele bonus** apar blocate, cu eticheta „ÎN CURÂND”, până primesc o acțiune.

### Scena e generată din cod

`Game.unity` și prefab-urile sunt recreate de `Stage1Setup` la fiecare rulare. Orice modificare făcută de mână
în scenă se pierde. Modernizarea e deci tot un pas de setup (`VisualSetup`), care rulează **după** Etapa 1.

## 2. Fișiere

### Modificat (un singur fișier existent)

- `Assets/_Game/Editor/Stage1Setup.cs`: doar `QuarryCloudBuild.PreExport()` apelează în plus
  `VisualSetup.Run()`. Restul fișierului e neatins.

**Niciun script de gameplay nu a fost modificat.**

### Adăugate — runtime (`Assets/_Game/Scripts/Visual/`)

| Fișier | Rol |
|---|---|
| `Character/MinerAnimator.cs` | Animații procedurale Idle / Walk / Mine / Carry pentru minerul stilizat |
| `Effects/VfxManager.cs` | Particule refolosite (Emit) + text plutitor din `ObjectPool`; API pentru vânzare și upgrade |
| `Effects/FloatingText.cs` | Text 3D „+3$” (poolable) |
| `Effects/BlockVfxRelay.cs` | Pe prefab-ul `Block`: lovitură → așchii, spargere → bucăți + praf |
| `Effects/CollectFeedbackRelay.cs` | Colectare → scântei + text cu valoarea |
| `Environment/ZoneVisual.cs` | Inel pulsatoriu, icon plutitor, evidențiere la apropiere |
| `UI/HudController.cs` | Nivel, bani (numărătoare animată), progres, misiune, pauză |
| `UI/HudStage1Binder.cs` | TEMPORAR: alimentează HUD-ul din `GameEvents` |
| `UI/SafeAreaFitter.cs` | Încadrare în `Screen.safeArea` (notch, bara de gesturi) |
| `UI/UiPunchScale.cs`, `UI/UiPressFeedback.cs`, `UI/UiProgressBar.cs` | Animații UI reutilizabile |
| `UI/ResourceChipsBar.cs` | Cipuri cu resursele colectate (înlocuiesc vizual contorul de debug) |
| `UI/BonusButton.cs` | Buton rotund de bonus cu cooldown radial |
| `UI/JoystickVisual.cs` | Doar aspectul joystick-ului (strălucire + indicator de direcție) |
| `UI/PausePanel.cs` | Pauză (`Time.timeScale = 0`) + comutare profil grafic |
| `Performance/PerformanceProfile.cs`, `Performance/PerformanceManager.cs` | Profiluri 60 / 30 FPS |

### Adăugate — Editor (`Assets/_Game/Editor/Visual/`)

| Fișier | Rol |
|---|---|
| `VisualSetup.cs` | Meniu, import TMP, verificarea referințelor, rularea pe etape |
| `VisualSetup.Palette.cs` | Paleta de culori, materiale, texturi, iconuri |
| `VisualSetup.Character.cs` | Minerul + prefab-urile `Block` și `Pickup_Resource` |
| `VisualSetup.Environment.cs` | Carieră, drumuri, zone, decor, lumină, ceață |
| `VisualSetup.Effects.cs` | Sistemele de particule și prefab-ul de text |
| `VisualSetup.Hud.cs` | HUD, joystick restilizat, panoul de pauză |
| `VisualSetup.Performance.cs` | Asset-urile URP și profilurile |
| `VisualAssetFactory.cs` | Mesh-uri procedurale (cutie rotunjită, piatră, inel), SDF → sprite-uri, materiale |

### Generate la rulare (nu sunt în git)

- `Assets/_Game/Art/Visual/{Meshes,Textures,UI,Materials}`
- `Assets/_Game/Prefabs/FX_FloatingText.prefab`
- `Assets/_Game/Settings/Performance/` (2 asset-uri URP + 2 profiluri)
- `Assets/TextMesh Pro/` (TMP Essential Resources, importate automat)

Nu se folosesc asset-uri externe sau licențe: totul e generat din cod (primitive Unity, mesh-uri procedurale,
iconuri desenate prin SDF).

## 3. Cum rulezi

- **Editor:** meniul **Quarry Empire → Setup Complet (Etapa 1 + Vizual)**. Dacă scena există deja:
  **Quarry Empire → Setup Vizual (după Etapa 1)**.
  - La prima rulare se importă automat *TMP Essential Resources*. Dacă Unity deschide fereastra TMP,
    apasă **Import TMP Essentials** și rulează din nou.
  - În Console apare un raport ✓/✗ cu referințele găsite și cu fiecare etapă (Personaj, Mediu, Efecte, HUD,
    Performanță). O etapă eșuată e sărită, iar restul continuă.
- **Unity Build Automation:** Pre-Export Method rămâne `QuarryCloudBuild.PreExport`.
  Branch-ul cu modernizarea este **`ccr-47b84d6c-lm0e0i`**; actualizează configurația de build dacă încă
  folosește `ccr-1d10ca5c-f6qz54`. Dacă TMP nu se poate importa, build-ul iese cu scena Etapei 1.
- Poți rula setup-ul de câte ori vrei. Șterge doar obiectele lui (prefixul `V_`). Asset-urile URP și
  profilurile se creează o singură dată, ca reglajele tale din Inspector să nu fie suprascrise.

## 4. Configurare în Inspector

### Personaj — `Player/V_MinerVisual` → **MinerAnimator**

- **Walk:** `Steps Per Meter`, `Leg Swing`, `Arm Swing`, `Walk Bob`.
- **Idle:** `Breath Speed`, `Breath Amount`.
- **Mine:**
  - `Mine Lean` = cât se apleacă trunchiul.
  - `Swing Duration` și `Swing Curve` trebuie să fie **egale** cu `Duration` / `Curve` din `PickaxeSwing`.
  - `Right Arm Rest` / `Right Arm Strike` = unghiurile brațului.
- **Carry:**
  - `Carry Lean`, `Carry Arm Forward`.
  - `Load Pieces` = pepitele din rucsac.
  - `Auto Load From Collected` + `Auto Load Decay Seconds` sunt un placeholder până la Etapa 2.
- **Ce s-a schimbat la jucător:**
  - Capsula `Body`, `Nose` și târnăcopul vechi au doar `MeshRenderer` dezactivat.
  - `PickaxePivot` a fost mutat în mâna dreaptă.
  - În `PickaxeSwing`, doar `Rest Euler` = (45,0,0) și `Strike Euler` = (180,0,0) s-au schimbat (sunt
    unghiuri de poză). `Duration`, `Curve`, `CharacterController` și toate scripturile au rămas la fel.
- **Pentru Etapa 2:** stiva reală apelează `MinerAnimator.SetCarryLoad(n)` și debifează
  `Auto Load From Collected`.

### Efecte — `_Systems/V_VFX` → **VfxManager**

- **Cantitățile** de particule per efect se înmulțesc cu densitatea profilului de performanță.
- **Sistemele copil** (`FX_ImpactChips`, `FX_BreakChunks`, `FX_SellCoins` …) se reglează direct pe
  ParticleSystem.
- `Max Floating Texts` = limita de texte vizibile simultan.
- **API pentru etapele următoare:**
  - `VfxManager.Instance.PlaySell(poziție, sumă)` — la vânzare.
  - `PlayUpgrade(poziție)` — la upgrade.
  - `ShowFloatingText(...)` — text plutitor.
  - Testare fără gameplay: click dreapta pe componentă → **Test: Sell / Test: Upgrade** (în Play Mode).
- **CollectFeedbackRelay:** `Show Value Text`, `Text Offset`, `Text Scale`.

### Zone — `V_Environment/SellZone`, `UpgradeZone` → **ZoneVisual**

- `Highlight Radius` = raza de evidențiere automată.
- Parametrii de puls / plutire / rotație.
- **API:** `SetHighlighted(bool)` și `PlayActivated()` (pornește automat efectul de vânzare / upgrade).

### HUD — `Canvas/V_HUD`

- **HudController** — referințele la widget-uri.
  - **API:** `SetLevel`, `SetMoney(sumă, animat)`, `SetProgress(0..1, text)`, `SetMission(text, curent, țintă)`.
  - Banii mari se scurtează automat: 12.5K, 3.4M.
- **HudStage1Binder** (temporar) — `Display Level`, `Show Collected Value As Money` și lista `Missions`
  (descriere, resursă opțională, țintă).
  - Când apar sistemele reale, dezactivează componenta și apelează direct `HudController`.
- **Butoane bonus** (`BonusTurbo`, `BonusMagnet`) → **BonusButton**:
  - adaugă acțiunea în `On Activated` (ex. reclama cu recompensă din Etapa 9) și butonul devine activ;
  - din cod: `SetAvailable(true)` + `OnActivated.AddListener(...)`;
  - `Cooldown Seconds` = durata cooldown-ului.
- **Joystick** (`JoystickBG` → **JoystickVisual**) — doar alpha / scară.
  - `OnScreenStick`, poziția și mărimea nu s-au schimbat.
  - `Movement Range` e copiat din stick.
- **Pauză** (`V_PausePanel`) — butonul de calitate comută profilul grafic.
- **Layout:**
  - referința e 1080×1920, cu `CanvasScaler`-ul Etapei 1 neschimbat;
  - tot HUD-ul nou stă în `SafeAreaFitter`;
  - colțurile panourilor se reglează din `Image → Pixels Per Unit Multiplier` (mai mare = colțuri mai mici).

### Performanță — `_Systems/V_PerformanceManager`

- **Profiles:** `[0] Calitate (60 FPS)`, `[1] Economic (30 FPS)`. Fiecare `PerformanceProfile` are:
  - FPS țintă;
  - asset URP propriu;
  - densitatea particulelor;
  - umbre pe cameră.
- **Alegerea la prima pornire:** ≥ 3.5 GB RAM și ≥ 6 nuclee → 60 FPS; altfel 30 FPS. Pragurile se reglează.
  Alegerea jucătorului (din pauză) se salvează în `PlayerPrefs`.
- **Auto Downgrade:** dacă media FPS pe 5 s scade sub 45 (după 4 s de încălzire), trece singur pe profilul
  economic.
- **Asset-urile URP** (`Settings/Performance/URP_Mobile_*.asset`) sunt copii ale asset-ului existent.
  Proiectul era deja pe URP, deci nu s-a migrat nimic.

| | High (60 FPS) | Low (30 FPS) |
|---|---|---|
| Render scale | 0.9 | 0.75 |
| MSAA | 2× | oprit |
| Umbre | soft, 2048, 22 m | hard, 1024, 14 m |
| Cascade | 1 | 1 |
| HDR / depth / opaque texture | oprite | oprite |
| Lumini adiționale | oprite | oprite |
| Particule | 100% | 60% |

- **În Editor:** schimbarea de pipeline din Play Mode e anulată la ieșire. Proiectul rămâne curat.
- `GameManager` setează în continuare 60 FPS. `PerformanceManager` rulează după el și aplică FPS-ul profilului.

## 5. Direcția artistică și costul

- **Personaj:** cască galbenă cu lanternă, cămașă portocalie, salopetă albastră, mănuși, bocanci, rucsac
  cu încărcătură vizibilă, târnăcop cu cap curbat.
  - Construit din primitive și cutii cu muchii rotunjite.
  - Materiale URP Lit mate.
  - ~40 de randere mici (SRP Batcher), fără collider.
- **Mediu:**
  - iarbă caldă cu zgomot discret;
  - podeaua carierei din pământ prăfuit;
  - margine din pietre neregulate, cu intrare spre jucător;
  - drumuri cu pietricele;
  - felinare (doar emisie, fără lumini reale);
  - vagonet pe șine, gard, tufișuri, bolovani, lăzi.
- **Pentru gameplay:**
  - **niciun obiect nou nu are collider**, deci zonele de mers și scanările de minat / colectare sunt neschimbate;
  - decorul e static (static batching);
  - doar obiectele mari aruncă umbre.
- **Lumină:**
  - soare cald cu umbre soft;
  - ambient trilight;
  - ceață liniară în culoarea cerului (adâncime + ascunde marginea hărții).
- **Blocuri:**
  - mesh cu muchii rotunjite (~380 de vârfuri);
  - minereurile au luciu ușor (aurul și diamantul ceva mai mult);
  - resursele sunt pepite low-poly.
- **Draw calls și memorie:**
  - SRP Batcher activ;
  - materiale partajate (fără copii la rulare — evidențierile folosesc `MaterialPropertyBlock` doar cât durează);
  - câte un singur ParticleSystem per efect, refolosit cu `Emit`;
  - textul plutitor vine din `ObjectPool`, cu un material TMP partajat;
  - imaginile UI decorative nu primesc raycast;
  - panoul de pauză e dezactivat când nu e folosit (fără overdraw).

## 6. Ce trebuie verificat în Unity

1. **Scena se generează:** rulează **Setup Complet**. Console nu trebuie să conțină ✗.
2. **Joystick:** mișcarea e identică cu cea de dinainte. Mânerul se mișcă, iar indicatorul de direcție se rotește.
3. **Minat:**
   - târnăcopul lovește înainte-jos, sincron cu brațul;
   - apar așchii la lovituri și bucăți + praf la spargere;
   - dacă poza arată ciudat, reglează `Rest/Strike Euler` (PickaxeSwing) și `Right Arm Rest/Strike` (MinerAnimator).
4. **Colectare:**
   - resursele zboară spre rucsac;
   - apar scântei și „+N$”;
   - banii se numără animat;
   - apar cipurile cu resurse;
   - minerul trece în Carry.
5. **Pauza:**
   - jocul se oprește și pornește la loc;
   - comutarea profilului grafic merge.
6. **Safe Area:** testează în Device Simulator pe un telefon cu notch (de ex. iPhone 15, Pixel 8) și pe
   un ecran 20:9.
7. **Performanță pe un Android mid-range:**
   - Profiler + Frame Debugger: țintă < 100 draw calls / SetPass;
   - frame time < 16.6 ms pe profilul High.
8. **Build în cloud:** importul TMP în batch mode. Verifică log-ul de build pentru
   `[QuarryEmpire/Vizual] ✓ HUD`.

## 7. Limitări cunoscute

- Nu a fost rulat în Unity. Posibile ajustări:
  - unghiurile pozei de minat;
  - mărimea textelor TMP 3D;
  - tonurile culorilor sub lumina finală.
- Animațiile sunt procedurale, nu un Animator Controller. Asta e potrivit pentru un personaj din
  primitive. Pentru un model riguit (de ex. din Asset Store) se poate înlocui `MinerAnimator` cu un
  `Animator`, cu aceleași stări.
- Fontul e LiberationSans (implicit TMP). Pentru un aspect mai „casual” se poate importa un font cu licență
  OFL (de ex. Fredoka sau Baloo) și seta în `TMP Settings`.
- Banii, misiunile, zonele și bonusurile sunt doar vizuale până la etapele de economie și monetizare.
- Decorul nu are collider: jucătorul poate trece prin pietrele de pe margine și prin tufișuri.
