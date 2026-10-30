# Drop Quest – redesign premium UI (ramura `feature/premium-ui-redesign`)

Data: 2026-11-02. Baza: `ads/levelplay` (deci cu pachetul LevelPlay, gata de build cu reclame).

> **Limită importantă.** În mediul de lucru nu există Unity și nici telefon. Nu am deschis proiectul, nu am văzut niciun ecran
> randat și nu am rulat un build. Ce urmează e scris din cod, cu verificare doar prin testele regulilor jocului
> (`Tools/CoreTests`, 122 de teste). **Compilarea în Unity și aspectul pe telefon rămân de verificat de tine** (lista de la final).

## 1. Audit (etapa 1)
Jocul **nu are scene, prefaburi, animatoare sau materiale în depozit**: scena, interfața, sprite-urile procedurale și
animațiile se construiesc din cod (`Editor/SugarDropSetup.cs` rulează în build-ul din cloud). De aceea redesignul se face
în cod, prin componente reutilizabile, nu prin prefaburi.

| Zonă | Fișiere principale |
|---|---|
| Tokeni de culoare | `Core/QuestStyle.cs`, `Core/ChapterTheme.cs`, `Core/Palette.cs` |
| Componente de UI | `UI/SugarUi.cs` (Box, Card, Button, IconButton, Ribbon*, Chip*), `_Game/.../UiFactory.cs`, `UiLabel.cs` (TextMeshPro, `Fit`, `MeasureHeight`) |
| HUD | `UI/SugarHud.cs`, `UI/BoosterBar.cs` |
| Hartă / meniu | `UI/MapScreen.cs`, `UI/MainMenu.cs`, `UI/QuestNav.cs` |
| Ferestre | `UI/DialogPanel.cs`, `UI/ResultPanel.cs`, `UI/ChapterCompletePanel.cs`*, `UI/SettingsPanel.cs`, `UI/WardrobePanel.cs`, `UI/LeaderboardPanel.cs`, `UI/EventsPanel.cs`, `UI/WheelPanel.cs`, `UI/LevelStartPanel.cs` |
| Animații | `UI/Motion/*` (apăsare buton 80 ms / revenire 320 ms, `PopIn` 220 ms, stele 320 ms, tranziții) |
| Tablă | `Board/Playfield.cs`, `Board/Tile.cs`, `Visual/PieceSprites.cs`, `Visual/SugarSprites.cs` |

\* nou în această ramură.

## 2. Ce am implementat
1. **Paleta nouă** (tokeni în `QuestStyle`): turcoaz `#78C7D5`, mentă `#8CD5B0`, petrol `#34545A` (nou), crem `#FFF5DF`, caramel `#9D633B`,
   verde butoane `#50BC3A`, auriu `#F6B62D`. Tabla și celulele ei trec pe petrol; primul ținut primește cer turcoaz–mentă.
2. **Componente reutilizabile noi** în `SugarUi`: `Ribbon` (panglică de lemn cu titlu, titlul se micșorează dacă nu încape) și
   `Chip` (plăcuță de recompensă: iconiță + text).
3. **Pictograme noi desenate din cod** (`SugarSprites`): creion, tricou, cronometru.
4. **Butoane de dialog cu iconiță** (`DialogButton.Icon`): toate ferestrele generice le pot folosi.
5. **Fereastra „Capitolul terminat!”** (`ChapterCompletePanel`): trei stele mari care se aprind pe rând (animatorul ecranului de
   victorie), panglică cu titlu, recompense ca plăcuți cu iconițe (monede, rachetă, ajutor), pancartă cu ținutul următor (cerul lui,
   ornamentele lui), Bobo și un singur buton mare. Recompensele sunt aceleași ca înainte; doar prezentarea e nouă. Dacă panoul
   nu e în scenă, jocul revine la fereastra veche.
6. **Profilul jucătorului**: butoane cu iconiță (creion, tricou, trofeu, roată dințată), „Închide” roșu, „Setări” nou.
7. **Cadru pentru tablă**: margine mai deschisă în jurul panoului închis, colorată după ținut.
8. **Ferestre care se adaptează la text** (mesajul se măsoară, fereastra crește, butoanele nu mai acoperă nimic) și scenariul de la
   începutul nivelului care se micșorează să încapă.

## 3. Ce a rămas neschimbat (intenționat)
Mecanica Match-3, generarea nivelurilor, obiectivele, mutările, recompensele, dificultatea, salvarea, progresia, reclamele,
economia, personajul Bobo și toate evenimentele butoanelor. Piesele de pe tablă (apă, foc, frunză, ciupercă, cristal, speciale)
nu și-au schimbat desenul.

## 4. Neimplementat, cu motiv
| Cerință din prompt | Situație |
|---|---|
| Hartă nouă (traseu curbat, insule, efecte la nivelul curent, tranziții între capitole) | **Neatinsă.** Harta există deja cu ținuturi, noduri cu stele și paralaxă; fără Unity nu pot judeca ce arată slab, iar o rescriere oarbă riscă să strice deblocarea și navigarea. Cere iterații vizuale pe telefon. |
| Butonul „+” lângă monede (din imagine) | **Omis deliberat:** jocul nu are magazin, iar „+” ar promite unul. |
| Stări „dezactivat” pentru toate butoanele, prefaburi, Sprite Atlas, DOTween | Jocul nu folosește prefaburi; animațiile sunt native, fără DOTween. Atlasul cere profilare pe telefon. |
| Reacții noi ale lui Bobo (victorie, înfrângere, booster, deblocare nivel) | Există 14 ipostaze statice. **Lipsesc animații** (mers, săritură, frică, curiozitate): vezi `docs/visual/VISUAL_AUDIT.md`. Punctele de integrare sunt `BoboArt`, `BoboView`, `Mascot`. |
| Profilare 60 FPS | **Nu a fost făcută** (nu am dispozitiv). |

## 5. Verificat și neverificat
- ✅ Testele regulilor jocului: 122 din 122 trec (nu se ating de interfață).
- ❌ Compilarea în Unity (cod de UI nou: `ChapterCompletePanel`, `SugarUi.Ribbon/Chip`, `DialogPanel`, pictograme).
- ❌ Aspectul și poziționarea pe telefon, Safe Area, 60 FPS, animațiile.

## 6. Cum verifici pe Android
1. Unity Build Automation → target pe ramura `feature/premium-ui-redesign` (conține deja LevelPlay) → build → AAB.
2. Instalează și verifică: profilul (meniu → Profil), „Capitolul terminat!” (termină nivelul 10 sau forțează-l dintr-un salvare),
   culorile tablei, cadrul din jurul ei, ferestrele cu mult text.
3. Dacă ceva arată prost sau build-ul nu compilează, trimite captura sau logul.
