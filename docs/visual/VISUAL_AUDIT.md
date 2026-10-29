# Drop Quest – Audit vizual și design system (faza 1–2)

Data: 2026-10-28 · Ramura: `audit/quality`.

> **Limită importantă.** Aici nu există Unity și nici telefon, deci **nu am putut face capturi „înainte/după”** și nu am văzut
> niciun ecran randat. Auditul e făcut din cod: poziții, mărimi, ancore, culori și sprite-uri, pe rezoluția de referință
> 1080×1920 (CanvasScaler, potrivire pe lățime). **Verificarea vizuală rămâne de făcut în build**, cu lista de la final.

## 1. Tabla (aprobată, neatinsă)
Fișierele tablei (`Board/*`, `Visual/PieceSprites.cs`, `Visual/Playfield`, culorile `QuestStyle.Board/CellA/CellB`, `Palette`)
**nu au fost modificate** în această fază (verificat cu `git diff --stat`: niciun fișier din `Board/`).

## 2. Ecran cu ecran

| Ecran | Ce e bine | Probleme găsite (cauza din cod) | Scor |
|---|---|---|---|
| Ecran Unity + Intro | Imaginea-cheie, apariție cu săritură, sclipiri, se poate sări | Ecranul Unity (albastru) apare înaintea intro-ului | 70 |
| Meniu principal | Logo, Bobo pictat, JOACĂ mare, bara de lemn | **Ploaie de bomboane** (rest Sugar Drop) → *reparat*; monedele/viețile și bara de jos **nu respectau zona sigură** (notch, bara de gesturi) → *reparat*; pe ecrane 20:9 conținutul rămâne pe centru, cu benzi goale sus/jos | 66 |
| Harta | Drum, noduri cu stele, Boss marcat, paralaxă, bara de jos | **Acadele/spirale de bomboane și inimi** ca decor în ținutul 1 și 8 → *reparat* (flori, frunze, nori albi); nivelurile încuiate erau **lila** (rest) → *gri-albastru*; fața nodurilor e încă „capacul” de bomboană | 62 |
| HUD (în afara tablei) | Casete crem Nivel/Obiectiv/Mutări, scor cu stele | Contur **mov** la textele albe → *maro-închis, ca lemnul* | 70 |
| Pauză / Setări | Listă clară, butoane late, comutatoare | — (neverificat vizual) | 72 |
| Victorie / Pierdere | Panglică, stele, scor, recompensă, Bobo | Bobo din stânga poate atinge marginea pe ecrane înguste (de verificat) | 70 |
| Carte poveste | Ilustrație mare, balon cu portret | Hârtie **roz**, cerneală **vișinie**, noapte **mov**, inimi în colțuri (rest Sugar Drop) → *crem, maro, noapte albastră, frunze*; „SARI” putea sta sub notch → *zona sigură*; scenele sunt forme simple desenate din cod | 58 |
| Scena nivelului (bandă desenată) | Panou mare, tăbliță + balon traduse, o singură dată | Textul apărea dintr-odată → *acum literă cu literă*; „SARI” sub notch → *zona sigură*; imagini mărite din ~360 px (neclare); balonul dreptunghiular peste cel oval pictat | 60 |
| Ferestre (dialog) | Bobo în colț, după situație | Bobo poate acoperi începutul titlului pe ecrane înguste (de verificat) | 66 |
| Evenimente | Carduri crem, „Deschide”, puncte roșii | — | 70 |
| Tranziția | Acoperă ecranul la schimbare | Se rotea o **spirală de bomboană** → *steluță aurie* | 68 |
| Roata norocului | — | Centrul roții era o **bomboană** → *gema aurie din joc* | 64 |

Scorurile sunt estimări din cod (compoziție, consecvență, lizibilitate), **nu** din capturi. Niciunul nu e peste 72, pentru că nimic nu e verificat vizual.

## 3. Design system (tokenuri în `Scripts/Core/QuestStyle.cs`)
| Grup | Tokenuri |
|---|---|
| Culori de bază | `Background #EAF6FF`, `Surface #FFF6E3`, `SurfaceTint #F6E7C8`, `Wood #A0612F`, `WoodDeep #7A4520` |
| Acțiuni | `Green #4CB82E` (principal), `Blue #2E8BEA` (secundar, reclamă), `Wood` (navigare), `Red #E8483C` (închide) |
| Accente | `Gold #FFC233`, `GoldDeep #F0A51C` |
| Text | `Text #4A2E17`, `TextSoft #8A6A4E`, **nou:** `Outline` (contur maro pentru textele albe) |
| Straturi | `Shadow` (umbra maro a cardurilor), `Scrim` (fundalul ferestrelor), **nou:** `Night` (fundalul poveștilor) |
| Tablă (aprobată) | `Board`, `CellA`, `CellB` – neschimbate |
| Animație (**nou**) | `LettersPerSecond 38` (textul din baloane) |
| Componente | `SugarUi.Card` (card crem cu muchie), `SugarUi.Button` (buton 3D), `QuestNav` (bara de lemn), `BoboArt` (ipostaze) |
| Tipografie | Baloo 2 ExtraBold (SIL OFL) + font chirilic pentru rusă/ucraineană |
| Mărimi de atins | butoane principale ≥ 110 px înălțime, iconițe rotunde ≥ 100 px (la 1080 px lățime) |

## 4. Bobo
- Folosim imaginile aprobate: 14 ipostaze (liniștit, bucuros, speriat, plânge, salută, degetul mare, gânditor, îngrijorat, doarme,
  inimă, cristal, gemă, stea, fuge) + 2 capete pentru tablă.
- Trecerea între ipostaze e instantanee (schimbare de imagine + mișcare de săritură/legănare).
- **Lipsesc:** *mers*, *săritură*, *frică* distinctă de surprindere, *curiozitate*. Fără ele, folosim mișcări pe imaginea statică.

## 5. Inconsecvențe rămase (nereparate în această fază)
1. Fața nodurilor de pe hartă e „capacul” de bomboană (cere un sprite nou de nod: piatră/cristal).
2. Scenele din cartea poveștii sunt forme simple desenate din cod (cer ilustrații).
3. 14 din nivelurile 1–30 nu au scenă de bandă desenată.
4. Fundalurile sunt degradeuri (cer `sky0`–`sky9`).
5. Portretele boșilor sunt desenele vechi (cer `boss0`–`boss6` în stilul nou).
6. Logo-ul e mărit de ~4× (cere un logo la rezoluție mare).
7. Meniul pe ecrane 20:9: conținutul nu se întinde pe înălțime.

## 6. Grafică necesară (cu nume și mărimi)
| Fișier (`Resources/Art/`) | Mărime | Ce |
|---|---|---|
| `story1.jpg` … `story30.jpg` | 1536×1024 | Scena fiecărui nivel, **fără tăbliță și fără balon cu text**, cu loc liber pentru balon lângă capul lui Bobo |
| `sky0.png` … `sky9.png` | 1242×2688 | Fundalul fiecărui ținut (vertical, fără UI, fără text) |
| `boss0.png` … `boss6.png` | 512×512 | Gardienii (nu „răi”): noroi, gheață, foc, mlaștină, furtună, spini, piatră |
| `logo.png` | 2048 lățime | Logo-ul „Drop Quest” transparent, la rezoluție mare |
| `bobowalk.png`, `bobojump.png`, `boboafraid.png`, `bobocurious.png` | 1024×1024 | Ipostazele lipsă, pe verde (#00FF00) sau transparent |

## 7. Ecrane diferite (din cod, neverificat vizual)
- Canvas: 1080×1920, potrivire pe lățime → pe 19.5:9 / 20:9 ecranul are 2340–2400 px înălțime; ferestrele centrate încap.
- Zonă sigură: harta, HUD-ul și ajutoarele erau deja în `SafeArea`; **acum și** meniul (sus/jos) și butoanele „SARI”.
- Notă Android: aplicațiile care țintesc API 35+ se desenează „edge-to-edge” (sub barele sistemului). Trebuie verificat pe telefon că
  `Screen.safeArea` din Unity 6 include barele sistemului.

## 8. Performanță vizuală
- Imaginile din `Resources/Art` se comprimă la import (`ArtImport`: CompressedHQ, max 2048) și se țin în cache.
- Scenele de poveste sunt JPG (16 fișiere, 2,8 MB în proiect).
- Text: eticheta își schimbă textul doar dacă s-a schimbat (fără reconstruiri inutile); textul din baloane se reconstruiește doar
  când apare o literă nouă.
- Nemăsurat: draw call-uri, overdraw (cer profilerul pe telefon).

## 9. Lista de verificare vizuală (de bifat în build)
- [ ] Tabla arată **identic** cu build-ul anterior (captură înainte/după, același nivel)
- [ ] Meniu: monedele/viețile nu intră sub notch; bara de jos nu intră sub bara de gesturi; frunze și piese în locul bomboanelor
- [ ] Hartă: flori/frunze în primul ținut, nori albi în Insulele din Nori, niveluri încuiate gri-albastre
- [ ] Textele albe au contur maro (HUD, panglici, titluri)
- [ ] Cartea poveștii: hârtie crem, text maro, noapte albastră, frunze în colțuri; „SARI” se poate apăsa
- [ ] Scena nivelului: textul apare literă cu literă; prima atingere arată tot textul, a doua închide
- [ ] Tranziția: steluță aurie care se rotește
- [ ] Roata: gema aurie în centru
- [ ] Ecrane 16:9, 19.5:9, 20:9 și un telefon cu notch: nimic tăiat sau suprapus
