# Sugar Drop (branch `sugar-drop`)

Un puzzle original de tip „aliniază 3”, cu temă de cofetărie și mascota **Bobo**, un cupcake.

## Cum se joacă
- Sus e o grămadă de **bomboane**, ținută de o tablă de **capace colorate**.
- **Glisează** un capac spre vecin (sus/jos/stânga/dreapta) ca să le schimbi. Poți muta un capac și într-o gaură.
- Când se aliniază **3 sau mai multe capace de aceeași culoare** (rând sau coloană), ele dispar și lasă **găuri**.
  Nu cad capace noi în locul lor.
- Prin găuri, bomboanele cad (fizică reală), alunecă pe pâlnie și intră în **borcan**.
- **Obiectiv:** strânge numărul cerut de bomboane în borcan înainte să rămâi fără mutări.
- Un schimb care nu formează o aliniere se anulează și nu consumă mutarea. Dacă nu mai există mutări posibile,
  capacele se amestecă automat.

## Arome (fiecare are pictograma, nota muzicală și explozia ei)
| Aromă | Pictogramă | Efect la dispariție |
|---|---|---|
| Căpșună | inimă | inimioare care țâșnesc și plutesc |
| Afină | picătură | bule care urcă legănându-se |
| Mentă | frunză | frunzulițe care se învârt și cad |
| Lămâie | stea | explozie de steluțe + inel |
| Strugure | gem | gemuri pe orbită care se strâng într-o sclipire |

Notele aromelor sunt din gama pentatonică și urcă pe parcursul unei mutări: lanțurile lungi sună ca o melodie.

## Capace speciale
- **4 în linie** → capac **dungat**: curăță tot rândul (sau toată coloana). Efect: rază de lumină.
- **L sau T** → capac **ambalat**: explozie 3×3. Efect: undă de șoc, confetti de zahăr, camera tremură.
- **5 în linie** → capac **curcubeu**: schimbat cu un capac, curăță toată culoarea acestuia. Efect: raze spre fiecare țintă.
- Două speciale schimbate între ele se activează amândouă; curcubeu + curcubeu curăță toată tabla.

## Obstacole
- **Biscuiți** (de la nivelul 4): nu se mută și țin bomboanele; se sparg de alinierile vecine (de la nivelul 15 au nevoie de 2 lovituri).
- **Capace înghețate** (de la nivelul 6): nu se mută; prima aliniere în care intră le sparge gheața.
- **Nivel Boss** la fiecare al 10-lea nivel: mai multe bomboane și obstacole, mai multe mutări, monede duble și +1 la fiecare ajutor.

## Progres și economie
- **Vieți:** 5, una se pierde la înfrângere (sau la ieșirea dintr-un nivel început); se regenerează câte una la 20 de minute.
- **Monede:** 20 + 5 pe fiecare mutare rămasă (+20 la 3 stele, ×2 la Boss).
- **Ajutoare:** bombă (explozie 3×3 unde atingi), +5 mutări, amestecare. Se cumpără cu 100 de monede sau cu o reclamă.
- La înfrângere poți continua o dată cu **+5 mutări** (reclamă). Reclamele sunt deocamdată de test.
- Salvarea (nivel, stele, vieți, monede, ajutoare, setări) e în `SugarSave` (PlayerPrefs, JSON).

## Unde se reglează
- Dificultatea pe niveluri (culori, mutări, bomboane, obstacole, Boss): `Scripts/Core/LevelConfig.cs`
- Culorile: `Scripts/Core/Palette.cs`; textele RO → EN: `Scripts/Core/SugarLoc.cs`
- Tabla, capacele speciale, combinațiile: `Scripts/Board/Board.cs`; aspectul capacelor: `Scripts/Board/Tile.cs`
- Fizica bomboanelor: `CandySpawner` + `Data/CandyPhysics`; pâlnia și borcanul: `Playfield`
- Efectele vizuale: `Scripts/Effects/SugarVfx.cs`; sunetele: `Scripts/Effects/SugarAudio.cs`
- Grafica (desenată din cod): `Scripts/Visual/SugarSprites.cs`; mascota: `Scripts/Visual/Mascot.cs`
- Fluxul jocului, monede, ajutoare: `Scripts/Core/SugarDropGame.cs`; tema UI: `Data/SugarTheme.asset`

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
