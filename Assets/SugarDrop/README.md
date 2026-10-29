# Drop Quest – Aventurile lui Bobo (ramura `sugar-drop`)

Un puzzle original „potrivește 3” (schimbi piese vecine), cu mascota **Bobo**, un mic aventurier blănos cu urechi-frunze
albastre, și o poveste în zece ținuturi: Bobo adună bucățile Cristalului Izvorului, sparte de Vrăjitorul Secetei.
Termenii „bomboane”/„capace” din cod și din textele de mai jos înseamnă **piesele** jocului (frunză, picătură, gemă,
ciupercă, ametist).

## Cum se joacă
- Tabla are 9×9 celule (unele niveluri au forme: colțuri tăiate, arc, pâlnie, romb, trepte).
- **Trage** o bomboană spre o vecină (sus/jos/stânga/dreapta) ca să le schimbi, sau atinge o bomboană și apoi o vecină.
  Dacă schimbul face o linie de cel puțin **3 bomboane la fel** (orizontal sau vertical), linia dispare.
  Un schimb fără linie se întoarce la loc și nu consumă mutarea.
- Piesele de deasupra **cad** în golurile rămase (cu accelerație, întindere și aterizare „de jeleu”), iar piese noi
  **intră de sus**. Liniile formate de piesele căzute dispar și ele, **în cascadă**. Fără schimburi posibile,
  bomboanele se amestecă singure. Tabla pornește mereu fără linii gata făcute.
- Sub tablă e pâlnia cu **borcanul** și **Bobo**: bomboanele-ingredient care ies jos cad (cu fizică) în borcan, iar o parte
  din bomboanele strânse cad și ele acolo, ca decor (obiectivele nu depind de ele).

## Piese speciale (`SpecialFor` în `Scripts/Core/BlastEngine.cs`)
| Formă | Piesă | Efect |
|---|---|---|
| 3 în linie | — | eliminare normală |
| 4 în linie | **Rachetă** (perpendiculară pe linie) | curăță tot rândul / toată coloana |
| L sau T | **Bombă** | explozie 3×3 (tăiată la marginea tablei) |
| 5 în linie | **Curcubeu** | elimină toate bomboanele unei culori |

Piesa specială apare în celula unde ai mutat bomboana (sau la mijlocul liniei, în cascade). **O piesă specială se
activează când o muți** (schimbi cu orice vecină); curcubeul schimbat cu o bomboană elimină toată culoarea ei.
Exploziile activează în lanț piesele speciale lovite, fiecare o singură dată. Bomboanele cu gheață sau lanț pot face
parte dintr-o linie: linia le sparge un strat.

**Combinații** — schimbă între ele două piese speciale vecine:
| Combinație | Efect |
|---|---|
| Rachetă + rachetă | un rând și o coloană |
| Rachetă + bombă | trei rânduri și trei coloane |
| Bombă + bombă | zonă 5×5 |
| Curcubeu + rachetă | toate bomboanele unei culori devin rachete și pornesc |
| Curcubeu + bombă | toate bomboanele unei culori devin bombe și explodează |
| Curcubeu + curcubeu | toate bomboanele de pe tablă |

## Efecte după câte bomboane dispar
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
- **Ladă de lemn** (de la nivelul 4): nu se mișcă, oprește căderea; se sparge din liniile vecine și din explozii (1–2 lovituri).
- **Gheață** (de la nivelul 7): bomboana dinăuntru nu se poate muta și nu cade; 1–3 straturi, câte unul pe lovitură.
- **Lanț** (de la nivelul 12): ține bomboana pe loc până e lovit o dată.
- **Ciocolată** (de la nivelul 25): ca lada, dar dacă într-o mutare nu e lovită nicio ciocolată, crește peste o bomboană vecină.
- Golurile de sub obstacole se umplu pe diagonală. Fiecare obstacol primește cel mult o lovitură pe acțiune.
- Pe nivelurile cu bomboane-ingredient apar doar lăzi, și nu pe coloanele bomboanelor (altfel le-ar bloca).

## Piese (indicele culorii decide regulile; forma e doar vizuală)
| Indice | Piesă | Efect la dispariție |
|---|---|---|
| 0 | ciupercă roșie | puncte roșii care țâșnesc |
| 1 | **picătură de apă** (albastră) | bule care urcă; **stinge focul** la nivelurile cu foc |
| 2 | frunză verde | frunzulițe care se învârt și cad |
| 3 | gemă aurie | steluțe și un inel |
| 4 | ametist | sclipiri mov care se strâng |

## Scor și stele
- Punctele (`Scripts/Core/ScoreRules.cs`): bomboană 20, bonus de grup mare (5 → 40, 7 → 160, 10 → 490), piesă specială
  nouă 60, activată 100, combinație 300, obstacol 40, glazură 50, bomboană în borcan 500.
- Bara de scor din HUD arată pragurile de 2 și 3 stele; victoria dă mereu o stea.
- **Finalul dulce:** la victorie, fiecare mutare rămasă devine o rachetă care explodează (+200 puncte).

## Limbi
Jocul are 11 limbi: engleză (implicită), română, spaniolă, portugheză, franceză, germană, italiană, turcă, indoneziană, rusă și ucraineană.
- Rusa și ucraineana folosesc fontul Nunito Black (OFL, `Resources/Fonts`), pentru că Baloo 2 nu are litere chirilice (`UI/SugarFonts.cs`).
- La prima pornire, limba se ia după telefon. Dacă telefonul e în altă limbă, jocul pornește în engleză.
- Limba se schimbă din **Setări → LIMBA**.
- Textele din cod sunt în română și servesc drept chei. Fiecare rând din tabelul `SugarLoc` are traducerea în toate
  limbile, iar testele verifică să nu lipsească nimic.
- Pentru un text nou: adaugi un rând `R(ro, en, es, pt, fr, de, it, tr, id, ru, uk)`.
- Japoneza, chineza, coreeana, araba sau hindi ar avea nevoie de încă un font.

## Meniu, roată, garderobă, evenimente
- **Meniul** (`UI/MainMenu.cs`): vieți cu cronometru, monede, stele; logo-ul; Bobo cu costumul ales (salută, iar când
  îl atingi arată degetul mare); progresul capitolului; butonul mare „Joacă” și **bara de lemn** de jos (`UI/QuestNav.cs`):
  Profil, Bobo (garderobă), Evenimente, Clasament, Setări (punct roșu când e ceva nou).
- **Evenimente** (`UI/EventsPanel.cs`): roata norocului, cadoul zilnic, misiunile și weekendul norocos într-un singur loc.
- **Roata norocului** (`UI/WheelPanel.cs`, reguli în `Core/SugarExtras.cs`): 1 rotire gratuită pe zi (2 în weekend)
  și 3 cu reclamă; vieți, vieți pline, „+5 mutări”, monede, bombă, rachetă de start, amestecare.
- **Boostere de start** (de la nivelul 2): rachetă și/sau bombă puse pe tablă la început; 150 de monede bucata.
  Se câștigă și din roată și din cufărul de capitol.
- **Garderoba lui Bobo** (`UI/WardrobePanel.cs`): coif de petrecere, fundiță, coroană, bonetă, ochelari, pălărie de
  vrăjitor — deblocate cu stele; Bobo le poartă în meniu și lângă tablă.
- **Weekend norocos:** sâmbăta și duminica monedele din niveluri sunt duble și roata are o rotire gratuită în plus.
- **Harta pe ținuturi:** fiecare capitol are cerul lui, nori la graniță, decor propriu animat (flori, frunze, picături,
  stele, cristale, gheață, nori) și drum în culorile ținutului; jos, bara de lemn (Acasă, Harta, Evenimente, Bobo)
  și, când e activ, bannerul de reclame (interfața urcă deasupra lui, tabla nu se mută).

## Poveste și antrenament
- **Intro** (`UI/IntroPanel.cs`): imaginea Drop Quest la pornire, apoi povestea (prima dată) sau „Bine ai revenit”.
- **Cartea poveștii** (`UI/StoryPanel.cs`): ilustrație mare animată și replici scurte în balon (Bobo, Vrăjitorul, boșii);
  zece episoade, câte unul pe ținut, cu final după Boss.
- **Scena nivelului** (`Core/LevelStory.cs`, `UI/LevelStoryPanel.cs`): panou de bandă desenată înaintea unui nivel nou
  (imaginile `Resources/Art/story{nivel}.jpg`, deocamdată pentru nivelurile 1–11, 15, 20, 25, 29, 30). Tăblița și balonul
  sunt desenate de joc, deci textul e tradus; scena apare o singură dată (`levelStorySeen`).
- **Antrenamentul de la nivelul 1** (`UI/TutorialCoach.cs`, tabla din `Core/TutorialBoard.cs`): 3 la fel în linie →
  4 la rând fac o rachetă → mută racheta → obiectivul (săgeată spre el) → mutările → ajutoarele → „Acum e rândul
  tău!”. Se poate revedea din **Setări → Revezi tutorialul**.

## Capitole, obiective, zilnic
- **Capitole:** la fiecare 10 niveluri se schimbă tema (cer, tablă, bastoane, harta): Câmpia cu Flori, Pădurea de
  Smarald, Lacul Albastru ... în total 10, apoi se reiau (`Core/ChapterTheme.cs`). Primul Boss învins dintr-un capitol
  dă **cufărul capitolului** (monede după stele, ajutoare la 24+ stele).
- **Obiective:** pe lângă bomboanele în borcan, culori, glazură și scor, nivelurile cer și: **sparge lăzile** (de la 4),
  **folosește piese speciale** (de la 5), **sparge gheața** (de la 7), **rupe lanțurile** (de la 12), **oprește
  ciocolata** (de la 25). Ordinea: `Cycle` din `Core/LevelConfig.cs`.
- **Cadou zilnic** (serie de 7 zile, monede x2 cu reclamă) și **3 misiuni pe zi** (`Core/SugarDaily.cs`), din
  butoanele din stânga hărții.
- **Setări:** grafică (Înaltă / Medie / Scăzută: particule, lumini, 30 sau 60 FPS; implicit după memoria telefonului)
  și notificări.
- **Salvare în cloud** (Unity Cloud Save, `Online/SugarCloudSave.cs`): la pornire, dacă progresul din cloud e mai
  avansat (ex. după reinstalare), îl aduce pe telefon; după fiecare victorie trimite progresul.
- **Notificări locale** (`Online/SugarNotifications.cs`): vieți pline, cadoul zilnic la 10:00, „Bobo îți duce dorul”
  după 3 zile. Permisiunea Android se cere după prima victorie.
- **Recenzie:** după nivelul 8, Bobo întreabă dacă îți place jocul; „Da” duce la pagina din Google Play.

## Nickname și clasament
- La prima pornire, Bobo te salută și îți alegi un **nickname** (`UI/WelcomePanel.cs`). Abia după aceea apare întrebarea
  despre reclame. Nickname-ul se schimbă din Setări, iar meniul arată „Salut, <nickname>!”.
- **Clasamentul** se deschide de pe hartă, din butonul cu trofeu de sub setări (`UI/LeaderboardPanel.cs`).
  Punctele sunt **suma celor mai bune scoruri** de pe fiecare nivel (`SugarSave.TotalScore`).
  E online prin Unity Gaming Services (`Online/SugarLeaderboard.cs`). Configurarea e în `CLASAMENT.md`.

## Progres și economie
- **Vieți:** 5, una se pierde la înfrângere (sau la ieșirea dintr-un nivel început); se regenerează câte una la 20 de minute.
- **Monede:** 20 + 5 pe fiecare mutare rămasă (+20 la 3 stele, ×2 la Boss).
- **Ajutoare:** bombă (explozie 3×3 unde atingi), +5 mutări, amestecare. Se cumpără cu 100 de monede sau cu o reclamă.
- La înfrângere poți continua o dată cu **+5 mutări** (reclamă). Fără pachetul LevelPlay, reclamele sunt de test (vezi `RECLAME.md`).
- Salvarea (nivel, stele, recorduri, vieți, monede, ajutoare, setări, nickname) e în `SugarSave` (PlayerPrefs, JSON).

## Unde se reglează
- Nivelurile (tip, val de dificultate, formă, obiective, mutări, obstacole): `Scripts/Core/LevelConfig.cs`
- Regulile tablei: `Scripts/Core/BlastEngine.cs` — cod pur, fără Unity; `Board` din scenă doar animă pașii motorului
  și ține o singură stare la un moment dat (liberă / piesă selectată / animează), ca acțiunile să nu se suprapună.
- Culorile și duratele comune: `Scripts/Core/QuestStyle.cs` (jocul) și `Scripts/Core/Palette.cs` (piese); traducerile: `Scripts/Core/SugarLoc.cs`
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
- **EditMode** (`Tests/EditMode`): schimburi invalide (fără linie, nevecine, obstacole, bomboane blocate), linii de 3,
  4 (rachetă), L (bombă), 5 (curcubeu), piese speciale mutate, curcubeul cu o bomboană, reacții în lanț fără dubluri,
  cele șase combinații, gheață în linie și obstacole vecine (o lovitură pe acțiune), cascade, tabla fără linii la start,
  mii de schimburi aleatoare pe 60 de niveluri (fără linii rămase, fără piese pierdute, tabla nu rămâne blocată),
  valuri de dificultate, Boss la fiecare 10 și Mega-Boss la 50/100/150/200, obiective, scor, vieți, hartă.
- **Echilibru** (`BalanceTests`, `BalanceSimulator`): un jucător automat (care urmează indiciul, fără ajutoare) joacă
  nivelurile; primele niveluri trebuie să fie ușoare și niciunul imposibil.
- **PlayMode** (`Tests/PlayMode`): pe scena reală, meniu → hartă → nivel → pauză → hartă, cu apăsări rapide repetate.
- În Unity: `Sugar Drop → Construiește jocul`, apoi `Window → General → Test Runner`.

## Build în cloud (Unity Build Automation)
- **Ramura:** `sugar-drop` · **Pre-Export Method:** `SugarDropCloudBuild.PreExport` · **Bundle ID:** `com.victorjosan.sugardrop`
- Depozite: `victorjosan16/Games01` și `victorjosan16/UAT_1`; Build Automation e legat de UAT_1, iar ramura e identică în amândouă.
- Versiunea: `versionCode` = numărul build-ului din cloud; numele versiunii = `1.0.<număr>` (`SugarDropSetup.VersionPrefix`).
- Reclame reale: ramura `ads/levelplay` (pachetul LevelPlay în manifest); vezi `RECLAME.md`.
- Publicarea pe Google Play și textele de magazin în 11 limbi: `GOOGLE_PLAY.md`.
