# Sugar Drop (branch `sugar-drop`)

Un puzzle original de tip „aliniază 3”, cu temă de cofetărie și mascota **Bobo**, un cupcake.

## Cum se joacă
- Tabla are 9×9 celule (unele niveluri au forme: colțuri tăiate, arc, pâlnie, romb, trepte).
- **Glisează** o piesă spre vecin ca să le schimbi. 3+ piese de aceeași aromă în linie dispar.
- Piesele de deasupra **cad** în golurile rămase, iar piese noi **intră de sus**. Noile alinieri dispar singure
  (**cascade**), iar fiecare cascadă înmulțește punctele (×2, ×3…).
- Un schimb fără aliniere se anulează și nu consumă mutarea. Fără mutări posibile, piesele se amestecă singure.
- Sub tablă e pâlnia cu **borcanul** și **Bobo**: bomboanele care ies jos cad (cu fizică) în borcan.

## Tipuri de niveluri (se alternează pe hartă)
| Tip | Obiectiv |
|---|---|
| Bomboane în borcan | Adu jos 2–4 bomboane-ingredient: ele coboară odată cu piesele și ies pe la săgețile de jos |
| Strânge culori | Elimină un număr de piese de 1–3 arome (și cele speciale contează) |
| Glazură | Topește toată glazura de pe celule (1 sau 2 straturi); se topește când dispare piesa de pe ea |
| Scor | Atinge scorul țintă |

Nivelul 1 e de scor (cu tutorial). Fiecare al 10-lea nivel e **Boss**: bomboane + culori, mai multe mutări, recompensă dublă.
Același nivel arată la fel la fiecare încercare (forma, obstacolele și glazura sunt generate din numărul nivelului).

## Obstacole
- **Gheață** (de la nivelul 7): piesa înghețată nu se poate muta, dar cade; prima aliniere o eliberează.
- **Biscuiți** (de la nivelul 5): nu se mută și opresc căderea; se sparg din alinierile vecine (de la nivelul 15, din 2 lovituri).
  Golurile de sub ei se umplu pe diagonală.
- **Ciocolată** (de la nivelul 25): ca biscuitul, dar dacă într-o mutare nu spargi nicio ciocolată, ea crește peste o piesă vecină.

## Arome (fiecare are forma de jeleu, nota muzicală și explozia ei)
| Aromă | Formă | Efect la dispariție |
|---|---|---|
| Căpșună | inimă | inimioare care țâșnesc și plutesc |
| Afină | picătură | bule care urcă legănându-se |
| Mentă | hexagon | frunzulițe care se învârt și cad |
| Lămâie | stea | explozie de steluțe + inel |
| Strugure | drajeu oval | gemuri pe orbită care se strâng într-o sclipire |

Piesele sunt desenate din cod în `Scripts/Visual/PieceSprites.cs` (margine de jeleu, lumină reflectată, luciu).
Toate aromele au și stropi de jeleu și bucățele care sar.

## Piese speciale
- **4 în linie** → piesă **dungată**: curăță tot rândul (sau toată coloana).
- **L sau T** → piesă **ambalată**: explozie 3×3.
- **5 în linie** → **curcubeu**: schimbat cu o piesă, curăță toată culoarea ei; curcubeu + curcubeu curăță toată tabla.
- Două piese speciale schimbate între ele se activează amândouă.

## Scor și stele
- Punctele (`Scripts/Core/ScoreRules.cs`): piesă 20 × cascada, capac special nou 60, activat 100, obstacol 40,
  glazură 50, bomboană în borcan 500. Bara de scor din HUD arată pragurile de 2 și 3 stele; victoria dă mereu o stea.
- **Finalul dulce:** la victorie, fiecare mutare rămasă devine un capac dungat care explodează (+200 puncte).

## Progres și economie
- **Vieți:** 5, una se pierde la înfrângere (sau la ieșirea dintr-un nivel început); se regenerează câte una la 20 de minute.
- **Monede:** 20 + 5 pe fiecare mutare rămasă (+20 la 3 stele, ×2 la Boss).
- **Ajutoare:** bombă (explozie 3×3 unde atingi), +5 mutări, amestecare. Se cumpără cu 100 de monede sau cu o reclamă.
- La înfrângere poți continua o dată cu **+5 mutări** (reclamă). Reclamele sunt deocamdată de test.
- Salvarea (nivel, stele, vieți, monede, ajutoare, setări) e în `SugarSave` (PlayerPrefs, JSON).

## Unde se reglează
- Nivelurile (tip, formă, obiective, mutări, obstacole, Boss): `Scripts/Core/LevelConfig.cs`
- Regulile tablei (cădere, piese noi, cascade, speciale, obstacole): `Scripts/Core/Match3Engine.cs` — cod pur, fără Unity;
  `Board` din scenă doar animă pașii pe care îi calculează motorul
- Culorile: `Scripts/Core/Palette.cs`; textele RO → EN: `Scripts/Core/SugarLoc.cs`
- Aspectul pieselor și animațiile: `Scripts/Board/Tile.cs`; bomboanele din borcan: `CandySpawner`; pâlnia și borcanul: `Playfield`
- Efectele vizuale: `Scripts/Effects/SugarVfx.cs`; sunetele: `Scripts/Effects/SugarAudio.cs`
- Grafica (desenată din cod): `Scripts/Visual/SugarSprites.cs`; mascota: `Scripts/Visual/Mascot.cs`
- Fluxul jocului, monede, ajutoare: `Scripts/Game/SugarDropGame.cs`; tema UI: `Data/SugarTheme.asset`

## Harta și animațiile interfeței
- Harta (`UI/MapScreen.cs`) se derulează, se centrează pe nivelul curent și are 3 straturi de adâncime (paralaxă).
  După o victorie, drumul se aprinde până la nivelul nou, Bobo „merge” acolo și nodul se deblochează animat.
  Nivelurile trecute se pot rejuca (pentru mai multe stele); cele încuiate doar tremură.
- Componente refolosibile (`UI/Motion/`): `UIButtonPressAnimator` (1 → 0,94 → 1 cu săritură), `LevelNodeAnimator`,
  `UIParticleController` (particule UI din pool), `UITransitionController` (tranziții care blochează apăsările repetate),
  `LevelCompleteAnimator` (stele pe rând, monede numărate), `LevelMapScrollController` (centrare lină + paralaxă).
- Butonul „Înapoi” de pe Android: închide fereastra deschisă → pauză în nivel → din hartă în meniu → iese din meniu.

## Teste
- Codul pur (reguli de aliniere, niveluri, vieți, hartă, traduceri) e în asamblarea `SugarDrop.Core` (`Scripts/Core`).
- **EditMode** (`Tests/EditMode`): motorul tablei (inclusiv mii de mutări aleatoare pe 60 de niveluri: fără piese pierdute,
  fără alinieri rămase, fără goluri), obiectivele, scorul, nivelurile, vieți, hartă, traduceri.
- **Echilibru** (`BalanceTests`, `BalanceSimulator`): un jucător automat (care urmează indiciul, fără ajutoare) joacă
  nivelurile 1–40; primele niveluri trebuie să fie ușoare și niciunul imposibil.
- **PlayMode** (`Tests/PlayMode`): pe scena reală, meniu → hartă → nivel → pauză → hartă, cu apăsări rapide repetate,
  și un nivel încuiat care nu pornește. Salvarea jucătorului e pusă la loc după test.
- În Unity: `Sugar Drop → Construiește jocul`, apoi `Window → General → Test Runner`.

## Build în cloud (Unity Build Automation)
Configurație nouă (separată de Quarry Empire):
- **Branch:** `sugar-drop`
- **Pre-Export Method:** `SugarDropCloudBuild.PreExport`
- **Bundle ID:** `com.victorjosan.sugardrop`
- restul ca la Quarry Empire (Android, APK, debug keystore)

## Etape
1. ✅ Nucleul: tabla, schimb + aliniere, găuri, bomboane cu fizică, pâlnie + borcan, niveluri, victorie/înfrângere
2. ✅ Ajutoare (bombă, +5 mutări, amestecare), vieți, monede, hartă de niveluri cu nivele Boss
3. ✅ Obstacole (biscuiți, gheață) și capace speciale (dungat, ambalat, curcubeu) cu combinații
4. ✅ Efecte diferite pe aromă, sunete și muzică, mascota Bobo, meniu, pauză și setări, tutorial, indicii, iconiță
5. Reclame reale și publicare
