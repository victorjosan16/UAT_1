# Modernizare vizuală — Underground Metro Tycoon

Document de lucru pentru redesignul vizual. Acoperă **Faza 1 (analiză + sistem de design)** și
**Faza 2 (scena de joc, iluminare, teren, personaj, HUD)**. Fazele 3–5 așteaptă aprobarea.

## 1. Analiza proiectului existent

| Zonă | Stare înainte de redesign |
|---|---|
| Motor / randare | Unity 6 (Cloud Build: 6000.6.6f1), URP 17.x, portret, scenă generată de `ProjectSetup` |
| Arhitectură | `Simulation/` (C# pur: excavare, contracte, economie, progresie, salvare, mișcare) + `Runtime/` (vizualizări Unity) + `Editor/` |
| UI | uGUI construit integral din cod (`GameUI`, `HudView`, `ContractsPanel`, `SettingsPanel`, `FeedbackLayer`) |
| Artă | Geometrie procedurală din primitive Unity; fără texturi, fără fonturi proprii |
| Sisteme funcționale | mișcare + joystick, săpat automat, contracte (3), monede, XP/nivel, salvare/încărcare, setări |
| Sisteme inexistente (faze viitoare) | muncitori, utilaje, unelte cumpărabile, tuneluri, stații, trenuri, hartă, orașe, meniu principal |

Redesignul **nu a modificat** nimic din `Simulation/` în afară de textele implicite (traduse în română) și
un helper nou de text românesc. Formatul salvării, regulile de joc și testele au rămas identice.

## 2. Probleme vizuale și UX identificate

1. **Teren:** cuburi plate de o singură culoare, fără muchii vizibile; câmpul arăta ca o tablă de șah.
2. **Groapa:** pereții excavați erau un singur maro; straturile de sol nu se vedeau.
3. **Mediu:** iarbă plată, puține obiecte, nimic în mișcare — șantierul părea „mort”.
4. **Iluminare:** lumină ambientală plată, fără atmosferă / adâncime, culori spălăcite.
5. **Personaj:** proporții de manechin, fără expresie, lopata intra în pământ la impact, picioarele alunecau.
6. **HUD:** text în engleză, fontul implicit Unity, butoane plate fără feedback, contorul de unealtă era un simplu chip de text.
7. **Navigare:** butonul Contracte era un pătrat galben fără ierarhie; nimic nu semnala că un contract așteaptă.
8. **Reset progres:** confirmare prin „apasă de două ori” (ușor de declanșat accidental).

## 3. Sistemul de design (`Runtime/UI/UITheme.cs`)

**Culori**

| Token | Hex | Folosire |
|---|---|---|
| `NavyDeep` / `Navy` / `NavyRaised` | `#0B1730` / `#13254A` / `#1D3466` | fundaluri HUD, antete, butoane de navigare |
| `PanelGlass` | `#122245` @ 90% | panouri HUD peste scena 3D |
| `CardWhite` / `CardPale` | `#FFFFFF` / `#EAF3FF` | carduri și ferestre (meniuri luminoase) |
| `Blue` / `BlueDark` | `#2F7BFF` / `#1C55C4` | acțiuni de navigare (secundare) |
| `Green` / `GreenDark` | `#2DBE60` / `#1C8E45` | acțiuni de succes (Acceptă) |
| `Gold` / `GoldDark` | `#FFC52E` / `#D68E00` | monede, recompense (Revendică) |
| `Orange` | `#FF9A3C` | progresul construcției |
| `Red` | `#F0544F` | închidere / acțiuni distructive |
| `Turquoise` | `#34D1C6` | XP, etichete secundare |

Mediu: nisip cald `#E8C88F`, sol teracota (straturi `Palette.TerrainTop/TerrainSide`), iarbă `#8BCB52`, cer `#8FD2FA`.

**Tipografie:** Baloo 2 (SIL Open Font License, inclus în `Resources/Fonts`, subsetat la latină + diacritice
românești). ExtraBold pentru titluri / cifre / butoane, SemiBold pentru text. Mărimi (canvas 1080 px lățime):
26 legendă · 32 text · 36 etichetă · 44 titlu · 54 antet · 72 afișaj.

**Formă și spațiere:** colțuri rotunjite 9-slice (card 0.8, buton 0.7, pastilă 1.4), spațiere 12 / 24 / 36,
margine 3D a butoanelor 8 px.

**Mișcare:** 0.15 s (apăsare), 0.25 s (deschidere fereastră, puls monede), 0.35 s (tranziții lente).

**Componente reutilizabile**

| Componentă | Fișier | Rol |
|---|---|---|
| `UIButton` | `UI/Components/UIButton.cs` | buton „tactil” cu muchie 3D; stiluri Primary / Secondary / Gold / Danger / Navy / Light; rotund sau dreptunghiular |
| `ProgressBarView` | `UI/Components/ProgressBarView.cs` | bară rotunjită cu umplere animată și etichetă |
| `CurrencyDisplay` | `UI/Components/CurrencyDisplay.cs` | pastilă de monede cu număr care se rulează și puls |
| `ModalPanel` | `UI/ModalPanel.cs` | fereastră modală: fundal estompat, antet navy, buton X rotund, intrare glisată |
| `ConfirmDialog` | `UI/Components/ConfirmDialog.cs` | confirmare Da / Anulează |
| Toast + banner | `UI/FeedbackLayer.cs` | notificări și celebrări |
| `UIIcons` | `UI/UIIcons.cs` | iconițe originale desenate procedural (roată, lopată, clipboard, avatar, stea, lacăt, bifă, X, bloc de pământ, monedă) |

## 4. Ce s-a implementat în Faza 2

**Scena de joc**
- Blocuri de teren cu **muchii teșite** (mesh procedural comun, 2 sub-meshuri): fața de sus mai deschisă, laturile mai închise,
  3 variante de nuanță, mici variații de înălțime, pietricele, **fisuri** progresive și întunecare la deteriorare,
  animație de „pop” la spargere.
- **Pereți stratificați** în groapă: o bandă de culoare pentru fiecare strat de teren, crustă de nisip, rocă la bază.
- Bandă de delimitare galben/bleumarin cu stâlpi și **stegulețe care flutură**.
- Curte de șantier din nisip cu pietriș, bordură de lemn, cărare de roți; birou-container, toaletă ecologică,
  stivă de țevi, lăzi, trusă de scule, roabă, paleți de cărămizi, saci de ciment, turn de iluminat, conuri, bariere cu lumini.
- **Mini-excavator animat** (braț, cupă, cabină care se rotește), basculantă, **doi muncitori ambientali** care sapă,
  **mașini în trafic**, macara cu brațul în rotație, felinare, trecere de pietoni, clădiri colorate cu ferestre/copertine,
  copaci stufoși, tufișuri și flori.
- Geometria statică este combinată prin **static batching** (dacă mesh-urile sunt citibile) pentru mai puține draw call-uri.

**Iluminare**
- Soare cald `(1, 0.93, 0.8)` cu umbre soft, lumină ambientală pe trei tonuri (cer / orizont / sol), ceață liniară
  discretă spre oraș, cer albastru-deschis.
- Post-procesare URP (dacă URP e activ): saturație +14, contrast +10, vinietă discretă; bloom doar pe „Ridicată”.
  Pe „Scăzută” post-procesarea și muncitorii ambientali sunt dezactivați.

**Personaj**
- Proporții prietenoase (cap mai mare), ochi cu pupile, sprâncene, nas, zâmbet, obraji; cască cu lanternă,
  vestă cu benzi reflectorizante și buzunar, mănuși, pantaloni albaștri, bocanci cu talpă.
- **Mers sincronizat cu distanța** (picioarele nu mai alunecă), **înclinare în viraje**, rotire mai lentă și naturală.
- **Săpat recalculat**: vârful lamei atinge suprafața exact la impact (≈ −0,05 m, ≈ 1 m în față) și nu mai
  intră în pământ în restul ciclului; un bulgăre de pământ apare pe lamă după impact și este **aruncat lateral**
  (particule) — sincronizat cu animația.
- Animație de **„culegere”** la spargerea unui bloc și **celebrare** (sărituri, brațe sus, lopata ridicată)
  la finalizarea / încasarea contractului.

**HUD (în română)**
- Sus-stânga: avatar, **Nivel N**, bară XP cu `64/100` și stea.
- Sus-dreapta: monede (număr rulant, puls la încasare) + buton **Setări** rotund cu roată dințată.
- Panou **Contract activ**: „Sapă 20 de blocuri de pământ”, bloc de pământ, `7/20`, bară portocalie, recompensă;
  devine „CONTRACT FINALIZAT!” cu buton auriu **REVENDICĂ 500**; fără contract: „VEZI CONTRACTE”.
- Stânga: dala **Contracte** cu insignă roșie „!” când există un contract de acceptat sau de revendicat.
- Jos: joystick restilizat (stânga), **insigna uneltei** echipate (dreapta) cu nivelul și un marcaj care se rotește în timpul săpatului.
- Ferestrele Contracte și Setări folosesc noile componente și textele în română (redesignul lor complet este în Faza 3).
- Reset progres cu **dialog de confirmare**.

**Elemente din referință omise intenționat** (nu au încă sisteme reale; regula „fără butoane decorative”):
Oameni, Utilaje, Magazin, monedă premium (diamante) și butoanele „+”. Vor apărea odată cu sistemele lor.

## 5. Fișiere

Noi: `UI/UITheme.cs`, `UI/UIIcons.cs`, `UI/Components/{UIButton, ProgressBarView, CurrencyDisplay, ConfirmDialog}.cs`,
`Terrain/TerrainBlockMesh.cs`, `Terrain/AmbientMotion.cs`, `Core/PostProcessingSetup.cs`,
`Simulation/Text/RoText.cs`, `Tests/EditMode/RoTextTests.cs`, `Resources/Fonts/Baloo2-*.ttf` + `OFL-Baloo2.txt`.

Modificate: `HudView`, `ModalPanel`, `ContractsPanel`, `SettingsPanel`, `FeedbackLayer`, `GameUI`, `UIFactory`, `UISprites`,
`ExcavationSiteView`, `SiteEnvironmentBuilder`, `CharacterRig`, `PlayerController`, `DigController`, `ExcavationVfx`,
`GameBootstrap`, `Palette`, `PrimitiveFactory`, `DefaultContent` (texte RO), `ProjectSetup` (ceață în scenă),
`UndergroundMetroTycoon.Game.asmdef` (referințe URP opționale).

## 6. Planul pentru fazele următoare

- **Faza 3:** meniu principal animat (oraș + secțiune metrou + tren în mișcare, JOACĂ), Contracte cu tab-uri
  (Disponibile / În desfășurare / Finalizate) și ilustrații, apoi Oameni / Unelte / Utilaje — fiecare **împreună cu
  sistemul de joc** corespunzător (angajare, cumpărare unelte, mini-excavator), altfel ar fi butoane fără funcție.
- **Faza 4:** tuneluri, stații, trenuri, hartă metrou, orașe — depind de sistemele de metrou (fazele 5–6 din planul inițial).
- **Faza 5:** polish, audio, profilare pe Android, build de test.

## 7. Verificare și limitări

Vezi raportul din conversație / README pentru rezultatele exacte. Pe scurt: logica de joc are teste automate
care trec; codul Unity compilează împotriva API-ului UnityEngine + stub-uri UGUI/URP; **aspectul vizual final nu
a fost văzut în Unity** (nu există Editor în mediul de dezvoltare) — scena, HUD-ul și animațiile trebuie
verificate în Play Mode sau pe telefon. Geometria rămâne artă procedurală placeholder (nu modele finale).
