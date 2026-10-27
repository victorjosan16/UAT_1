# Drop Quest: sistemul de design

**Direcția:** aventură în natură, ca un joc puzzle premium. Panouri crem, lemn, butoane 3D groase, tablă ardezie închisă pe care piesele colorate ies în evidență, cer de natură pe fundal.

## Culori (`Scripts/Core/QuestStyle.cs`, singura sursă)
| Token | Valoare | Folosire |
|---|---|---|
| Background | #EAF6FF | fundalul implicit |
| Surface / SurfaceTint | #FFF6E3 / #F6E7C8 | ferestre, cardul obiectivelor, tăvi |
| Wood / WoodDeep | #A0612F / #7A4520 | panglici, butoane secundare (`UiTheme.PanelColor`) |
| Green / GreenDeep | #4CB82E / #3C9E24 | acțiunea principală (JOACĂ, CONTINUĂ) |
| Blue | #2E8BEA | reclame, bara de progres |
| Gold / GoldDeep | #FFC233 / #F0A51C | stele, mutări, accent (`AccentColor`) |
| Red | #E8483C | închide, negativ |
| Board / CellA / CellB | #3A4658 / #4C5A6E / #556479 | tabla și celulele |
| Text / TextSoft | #4A2E17 / #8A6A4E | text principal / secundar (maro) |
| Shadow / Scrim | maro 22% / ardezie 50% | umbre moi / fundalul de sub ferestre |

Tema UI (`SugarTheme.asset`) e generată din acești tokeni de `Editor/SugarDropSetup.cs`.

## Piesele
Tot 5 culori, cu aceleași reguli:

| Index | Piesă |
|---|---|
| 0 | ciupercă roșie |
| 1 | picătură de apă (stinge focul) |
| 2 | frunză |
| 3 | gemă aurie cu fațete |
| 4 | ametist |

Fiecare are lumină din stânga sus, o margine fină și un luciu discret. Toate se pot înlocui cu PNG-uri (`docs/redesign/ART_LIST.md`).

## Ținuturile
Același nume și aceeași poveste. Au cer de natură (în degrade sau pictat, `sky0`–`sky9`), tablă ardezie nuanțată pe ținut și cadru de lemn sau în culoarea ținutului.

## Grafica înlocuibilă
`Scripts/Visual/ArtLibrary.cs` caută `Resources/Art/<nume>.png` pentru sprite-urile colorate: piese, speciale, obstacole, Bobo, boși, fundaluri, logo. Dacă imaginea lipsește, se folosește desenul din cod. Fundalurile pictate acoperă ecranul păstrându-și proporțiile. Setările de import le face `Editor/ArtImport.cs`.

## Tipografie
Baloo 2 ExtraBold (SIL OFL, se poate folosi comercial), plus fontul chirilic existent. Logo-ul scris are „DROP” albastru și „QUEST” auriu, pe două rânduri, sau `logo.png` dacă există.
