# Sugar Drop — Soft Pop Edition: sistemul de design

## Culori (`Scripts/Core/SoftPop.cs`, singura sursă)
| Token | Valoare | Folosire |
|---|---|---|
| Background | #F8F7FF | fundalul aplicației, splash |
| Surface | #FFFFFF | ferestre, tava tablei, cardul obiectivelor |
| SurfaceTint | #F1EDFC | tăvi, plăcuțe inactive |
| Lavender | #A58AE0 | primară: panglici, cadrul implicit al tablei |
| LavenderDeep | #8B6FD3 | butoane și panouri cu text alb (`UiTheme.PanelColor`) |
| Coral / CoralDeep | #FF8D9E / #F06A80 | secundară; negativ (închide, pierdere) |
| Mint / MintDeep | #66D7B5 / #35BE94 | pozitiv (JOACĂ, CONTINUĂ, comutatoare pornite) |
| Yellow / YellowDeep | #FFD36B / #F5B93A | accent: stele, mutări, titluri |
| Sky | #5FB0F5 | reclame (recompense) |
| Text / TextSoft | #403653 / #7C7291 | text principal / secundar |
| Shadow | prună 16% | umbre moi (panouri, tablă, piese) |
| Scrim | prună 45% | fundalul de sub ferestre |

Tema UI (`SugarTheme.asset`) e generată din acești tokeni de `Editor/SugarDropSetup.cs` la fiecare build.

## Piesele (`Visual/PieceSprites.cs`)
Obiecte abstracte 2.5D, nu bomboane. Fiecare are siluetă proprie, fațete luminate din stânga sus, o margine fină (fără contur gros), un luciu oblic și o sclipire mică.

| Index | Formă | Culoare | Notă |
|---|---|---|---|
| 0 | inimă-gemă (8 fațete) | coral #FF6B80 | |
| 1 | picătură lichidă (netedă) | albastru #409EFF | rămâne „apa” de la nivelurile cu foc |
| 2 | cristal hexagonal | mentă #38CC94 | |
| 3 | gemă octogonală | auriu #FFC440 | |
| 4 | prismă triunghiulară | violet #9975EB | |

Numărul de culori și regulile nu se schimbă: forma e doar vizuală.

## Ținuturi (`Core/ChapterTheme.cs`)
Același nume și aceeași poveste, dar cer pastel (două tonuri), tablă albă, celule lavandă discrete și câte o culoare de accent pe cadru: coral, mentă, cer, galben, lavandă, cacao, gheață, caise, liliac. Galaxia rămâne mai închisă (noapte).

## Forme și adâncime
- Colțuri rotunjite peste tot. Umbre moi (`SoftPop.Shadow`), fără negru pur.
- Tabla e o tavă albă cu umbră, pe un cadru plin în culoarea ținutului. Bastoanele cu dungi au dispărut.
- Textul conturat folosește un contur prună, subțire și semitransparent.

## Tipografie
Baloo 2 ExtraBold (SIL OFL, liberă pentru uz comercial), plus fontul chirilic existent pentru rusă și ucraineană.
