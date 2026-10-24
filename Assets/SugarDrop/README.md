# Sugar Drop (branch `sugar-drop`)

Puzzle original de tip „aliniază 3”, cu temă de cofetărie.

## Cum se joacă
- Sus e o grămadă de **bomboane**, ținută de o tablă de **capace colorate**.
- **Glisează** un capac spre vecin (sus/jos/stânga/dreapta) ca să le schimbi. Poți muta un capac și într-o gaură.
- Când se aliniază **3 sau mai multe capace de aceeași culoare** (rând sau coloană), ele dispar și lasă **găuri**.
  Nu cad capace noi în locul lor.
- Prin găuri, bomboanele cad (fizică reală), alunecă pe pâlnie și intră în **borcan**.
- **Obiectiv:** strânge numărul cerut de bomboane în borcan înainte să rămâi fără mutări.
- Un schimb care nu formează o aliniere se anulează și nu consumă mutarea. Dacă nu mai există mutări posibile,
  capacele se amestecă automat.

## Unde se reglează
- Dificultatea pe niveluri (culori, mutări, bomboane, obiectiv): `Scripts/LevelConfig.cs`
- Culorile: `Scripts/Palette.cs`
- Mărimea tablei / animațiile: componenta `Board`; fizica bomboanelor: `CandySpawner` + `Data/CandyPhysics`
- Pâlnia și borcanul: `Playfield`; tema UI: `Data/SugarTheme.asset`

## Build în cloud (Unity Build Automation)
Configurație nouă (separată de Quarry Empire):
- **Branch:** `sugar-drop`
- **Pre-Export Method:** `SugarDropCloudBuild.PreExport`
- **Bundle ID:** `com.victorjosan.sugardrop`
- restul ca la Quarry Empire (Android, APK, debug keystore)

## Etape
1. ✅ Nucleul: tabla, schimb + aliniere, găuri, bomboane cu fizică, pâlnie + borcan, niveluri, victorie/înfrângere
2. Ajutoare (bombă, +5 mutări, amestecare), vieți, hartă de niveluri cu nivele „boss”
3. Obstacole pe tablă (capace înghețate, pietre), bomboane speciale
4. Efecte, sunete, muzică, meniu, iconiță; tutorial
5. Reclame și publicare
