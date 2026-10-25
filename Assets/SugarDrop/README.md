# Sugar Drop (branch `sugar-drop`)

Un puzzle original de tip „atinge și sparge” (Tap-to-Blast), cu temă de cofetărie și mascota **Bobo**, un cupcake.

## Cum se joacă
- Tabla are 9×9 celule (unele niveluri au forme: colțuri tăiate, arc, pâlnie, romb, trepte).
- **Atinge** un grup de cel puțin **3 bomboane la fel**, lipite pe orizontală sau verticală: tot grupul dispare.
  Grupurile de 1–2 nu se pot sparge (atingerea nu consumă mutarea; bomboana doar tremură).
- Când pui degetul pe o bomboană, ea se ridică și grupul ei strălucește; ridici degetul pe aceeași celulă ca să spargi
  (dacă tragi degetul în altă parte, nu se întâmplă nimic).
- Piesele de deasupra **cad** în golurile rămase (cu accelerație, întindere și aterizare „de jeleu”), iar piese noi
  **intră de sus**. Fără mutări posibile, bomboanele se amestecă singure.
- Sub tablă e pâlnia cu **borcanul** și **Bobo**: bomboanele-ingredient care ies jos cad (cu fizică) în borcan, iar o parte
  din bomboanele strânse cad și ele acolo, ca decor (obiectivele nu depind de ele).

## Piese speciale (pragurile sunt în `BlastRules`, în `Scripts/Core/TileKind.cs`)
| Grup | Piesă | Efect |
|---|---|---|
| 3–4 | — | eliminare normală |
| 5–6 | **Rachetă** (orizontală dacă grupul e mai lat, verticală dacă e mai înalt) | curăță tot rândul / toată coloana |
| 7–9 | **Bombă** | explozie 5×5 (tăiată la marginea tablei) |
| 10+ | **Curcubeu** | elimină toate bomboanele culorii din care s-a născut |

Piesa specială apare exact în celula atinsă. Exploziile activează în lanț piesele speciale lovite, fiecare o singură dată.

**Combinații.** O piesă specială fără vecine speciale se activează la atingere. Dacă are vecine speciale, prima atingere o
selectează (vecinele strălucesc); atinge o vecină ca să le combini, aceeași piesă ca s-o activezi singură, sau altundeva
ca să renunți.
| Combinație | Efect |
|---|---|
| Rachetă + rachetă | un rând și o coloană |
| Rachetă + bombă | trei rânduri și trei coloane |
| Bombă + bombă | zonă 7×7 |
| Curcubeu + rachetă | toate bomboanele unei culori devin rachete și pornesc |
| Curcubeu + bombă | toate bomboanele unei culori devin bombe și explodează |
| Curcubeu + curcubeu | toate bomboanele de pe tablă |

## Efecte după mărimea grupului
3–4: pocnet mic și sclipiri · 5–6: explozie medie și puls de ecran · 7–9: undă de șoc și laudă · 10+: explozie curcubeu
și impuls de cameră. Rachetele zboară în ambele direcții cu dâră, bombele au undă de șoc, combinațiile au blitz și inele.
Setarea **Mișcare redusă** oprește tremuratul și zoom-ul camerei.

## Tipuri de niveluri și valuri de dificultate
| Tip | Obiectiv |
|---|---|
| Bomboane în borcan | Adu jos 1–3 bomboane-ingredient: coboară odată cu piesele și ies pe la săgețile de jos |
| Strânge culori | Elimină un număr de bomboane de 1–3 arome |
| Glazură | Topește toată glazura (o zonă de pe tablă, 1 sau 2 straturi) |
| Scor | Atinge scorul țintă |

Fiecare capitol de 10 niveluri e un **val**: începe ușor, are câteva niveluri normale, unul greu urmat de unul ușor
(recuperare), un nivel foarte greu spre final și **Boss** la nivelul 10 (Mega-Boss la fiecare 50). Poziția nivelului
greu și a celor normale variază de la un capitol la altul. Mutările se calculează din obiectiv: cât îi trebuie de obicei
jucătorului automat din teste, înmulțit cu ×1,5 (ușor), ×1,2 (normal), ×1,05 (greu), ×0,95 (foarte greu).
Același nivel arată la fel la fiecare încercare.

## Obstacole
- **Ladă de lemn** (de la nivelul 4): nu se mișcă, oprește căderea; se sparge din grupurile vecine și din explozii (1–2 lovituri).
- **Gheață** (de la nivelul 7): bomboana dinăuntru nu se poate atinge și nu cade; 1–3 straturi, câte unul pe lovitură.
- **Lanț** (de la nivelul 12): ține bomboana pe loc până e lovit o dată.
- **Ciocolată** (de la nivelul 25): ca lada, dar dacă într-o mutare nu e lovită nicio ciocolată, crește peste o bomboană vecină.
- Golurile de sub obstacole se umplu pe diagonală. Fiecare obstacol primește cel mult o lovitură pe acțiune.
- Pe nivelurile cu bomboane-ingredient apar doar lăzi, și nu pe coloanele bomboanelor (altfel le-ar bloca).

## Arome (fiecare are forma de jeleu, nota muzicală și explozia ei)
| Aromă | Formă | Efect la dispariție |
|---|---|---|
| Căpșună | inimă | inimioare care țâșnesc și plutesc |
| Afină | picătură | bule care urcă legănându-se |
| Mentă | hexagon | frunzulițe care se învârt și cad |
| Lămâie | stea | explozie de steluțe + inel |
| Strugure | drajeu oval | gemuri pe orbită care se strâng într-o sclipire |

## Scor și stele
- Punctele (`Scripts/Core/ScoreRules.cs`): bomboană 20, bonus de grup mare (5 → 40, 7 → 160, 10 → 490), piesă specială
  nouă 60, activată 100, combinație 300, obstacol 40, glazură 50, bomboană în borcan 500.
- Bara de scor din HUD arată pragurile de 2 și 3 stele; victoria dă mereu o stea.
- **Finalul dulce:** la victorie, fiecare mutare rămasă devine o rachetă care explodează (+200 puncte).

## Progres și economie
- **Vieți:** 5, una se pierde la înfrângere (sau la ieșirea dintr-un nivel început); se regenerează câte una la 20 de minute.
- **Monede:** 20 + 5 pe fiecare mutare rămasă (+20 la 3 stele, ×2 la Boss).
- **Ajutoare:** bombă (explozie 3×3 unde atingi), +5 mutări, amestecare. Se cumpără cu 100 de monede sau cu o reclamă.
- La înfrângere poți continua o dată cu **+5 mutări** (reclamă). Reclamele sunt deocamdată de test.
- Salvarea (nivel, stele, vieți, monede, ajutoare, setări) e în `SugarSave` (PlayerPrefs, JSON).

## Unde se reglează
- Nivelurile (tip, val de dificultate, formă, obiective, mutări, obstacole): `Scripts/Core/LevelConfig.cs`
- Regulile tablei: `Scripts/Core/BlastEngine.cs` — cod pur, fără Unity; `Board` din scenă doar animă pașii motorului
  și ține o singură stare la un moment dat (liberă / piesă selectată / animează), ca acțiunile să nu se suprapună.
- Culorile: `Scripts/Core/Palette.cs`; textele RO → EN: `Scripts/Core/SugarLoc.cs`
- Aspectul pieselor și animațiile de jeleu: `Scripts/Board/Tile.cs`; bomboanele din borcan: `CandySpawner`; pâlnia și borcanul: `Playfield`
- Efectele vizuale: `Scripts/Effects/SugarVfx.cs`; sunetele: `Scripts/Effects/SugarAudio.cs`
- Grafica (desenată din cod): `Scripts/Visual/SugarSprites.cs`, `PieceSprites.cs`; mascota: `Scripts/Visual/Mascot.cs`
- Fluxul jocului, monede, ajutoare: `Scripts/Game/SugarDropGame.cs`; tema UI: `Data/SugarTheme.asset`

## Harta și animațiile interfeței
- Harta (`UI/MapScreen.cs`) se derulează, se centrează pe nivelul curent și are 3 straturi de adâncime (paralaxă).
  După o victorie, drumul se aprinde până la nivelul nou, Bobo „merge” acolo și nodul se deblochează animat.
- Componente refolosibile (`UI/Motion/`): `UIButtonPressAnimator`, `LevelNodeAnimator`, `UIParticleController`,
  `UITransitionController`, `LevelCompleteAnimator`, `LevelMapScrollController`.
- Butonul „Înapoi” de pe Android: închide fereastra deschisă → pauză în nivel → din hartă în meniu → iese din meniu.

## Teste
- Codul pur (reguli, niveluri, obiective, scor, vieți, hartă, traduceri) e în asamblarea `SugarDrop.Core` (`Scripts/Core`).
- **EditMode** (`Tests/EditMode`): grupuri de 1, 2, 3, 5, 7, 10+ și neregulate, atingeri invalide, rachete orizontale și
  verticale, bomba la margine, curcubeul, cele șase combinații, reacții în lanț fără dubluri, obstacole (o lovitură pe
  acțiune), mii de atingeri aleatoare pe 60 de niveluri (fără goluri, fără piese pierdute, tabla nu rămâne blocată),
  valuri de dificultate, Boss la fiecare 10 și Mega-Boss la 50/100/150/200, obiective, scor, vieți, hartă.
- **Echilibru** (`BalanceTests`, `BalanceSimulator`): un jucător automat (care urmează indiciul, fără ajutoare) joacă
  nivelurile; primele niveluri trebuie să fie ușoare și niciunul imposibil.
- **PlayMode** (`Tests/PlayMode`): pe scena reală, meniu → hartă → nivel → pauză → hartă, cu apăsări rapide repetate.
- În Unity: `Sugar Drop → Construiește jocul`, apoi `Window → General → Test Runner`.

## Build în cloud (Unity Build Automation)
- **Branch:** `sugar-drop` · **Pre-Export Method:** `SugarDropCloudBuild.PreExport` · **Bundle ID:** `com.victorjosan.sugardrop`
- restul ca la Quarry Empire (Android, APK, debug keystore)

## Etape (promptul „Premium Candy Blast”)
1. ✅ Audit; nucleul Tap-to-Blast (grupuri, cădere, umplere, o singură stare a tablei)
2. ✅ Animații de jeleu, strălucirea grupului, efecte pe mărimea grupului
3. ✅ Rachete, bombe, curcubeu și cele șase combinații
4. ✅ Borcanul de colectare (bomboane cu fizică, număr limitat)
5. ✅ Valuri de dificultate; obstacole: gheață 1–3, lanțuri, lăzi, ciocolată
6. Bătăliile cu Boss (viață, atacuri anunțate) și Mega-Boss pe faze
7. Primele 25 de niveluri finisate de mână, recompense (cufere, capitole, recompensa zilnică)
8. Ecrane de Boss, sunete noi, setări de calitate, extindere la 200+ niveluri
