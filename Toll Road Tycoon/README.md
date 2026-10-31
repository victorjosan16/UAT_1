# Toll Road Tycoon

Joc idle de administrare a unei autostrăzi cu taxă, pentru Android (Unity 2D, portret 9:16).
Mașinile vin, stau la coadă, plătesc la cabină, bariera se ridică, iar tu investești câștigurile
în cabine, angajați, benzi și upgrade-uri.

**Stadiu: Faza 1 (prima buclă de joc jucabilă).** Citește raportul de verificare de mai jos:
simularea e testată automat fără Unity, dar proiectul **nu** a fost încă deschis în Unity Editor
și nici construit pentru Android.

## Structura proiectului

```
Toll Road Tycoon/                      rădăcina proiectului Unity (acest director se deschide în Unity Hub)
  Assets/
    Scripts/
      TollRoadTycoon.Simulation.asmdef   logica jocului, independentă de Unity (noEngineReferences)
      Core/            GameSimulation (rădăcina, pas fix), GameConfig, GameStats, Vec2, SimRandom
      Traffic/         LaneManager (geometria drumului), TrafficManager (urmărirea mașinilor, porțile din plază),
                       QueueManager (alegerea cabinei), VehicleSpawner, Vehicle + VehiclePool
      TollSystem/      TollBooth (date), TollBoothController (mașina de stări), PaymentProcessor
      Economy/         EconomyManager, Money (cenți întregi, format K/M/B), CostCurve
      Workers/         Worker, WorkerManager
      Upgrades/        UpgradeManager (upgrade-uri de cabină, deblocare vehicule)
      RoadExpansion/   RoadExpansionManager (cabine noi, benzi noi)
      Save/            SaveData (cu versiune), SaveManager (scriere atomică, copie de rezervă, migrare)
      Localization/    Loc (româna principală, engleza completă)
      Presentation/    stratul Unity (TollRoadTycoon.Presentation.asmdef)
        Core/          GameBootstrap, GameRoot, SaveService, GameConfigAsset (ScriptableObject)
        Views/         WorldView, BoothView, VehicleViews, CoinPopups, SpriteFactory, Palette
        UI/            UIManager, MainMenuScreen, GameplayScreen, Panels, UIFactory, SafeAreaFitter
        Input/         CameraController (glisare, zoom cu două degete sau rotița mouse-ului, atingere)
        Audio/         AudioManager (efecte sonore generate din cod)
      Editor/          ProjectSetup (scena Boot, portret, identificator Android), BuildAndroid
    Tests/EditMode/    teste NUnit (Unity Test Runner + dotnet fără Unity)
  Packages/manifest.json
  ProjectSettings/ProjectVersion.txt   2022.3 LTS
  Tools/SimulationTests/               proiect dotnet care compilează simularea și testele fără Unity
```

Toate regulile jocului sunt în assembly-ul `Simulation`, care nu folosește `UnityEngine`.
De aceea simularea și testele ei pot rula fără Unity Editor. Stratul Unity doar citește starea
simulării și apelează funcțiile ei de cumpărare.

## Cum funcționează bucla de joc

* **Geometria drumului** (`LaneManager`): benzi de intrare → racorduri în S spre plază → câte o bandă
  pentru fiecare cabină → racorduri de ieșire → benzi de ieșire, plus un sens opus fără taxă.
  Când cumperi o bandă sau o cabină se **adaugă segmente reale** de drum, pe care mașinile le folosesc imediat.
* **Traficul** (`TrafficManager`): fiecare vehicul își urmează traseul. Viteza e limitată astfel încât
  să poată opri oricând înainte de spatele mașinii din față (plus o distanță de siguranță). În plus,
  poziția e blocată fix în acel punct, deci mașinile nu se pot suprapune și nu pot „sări”. Racordurile
  care se intersectează sau se unesc sunt folosite pe rând, prin rezervări. Doar prima mașină de pe o
  bandă poate rezerva, deci traficul nu se poate bloca reciproc.
* **Alegerea cabinei** (`QueueManager`): după timpul de așteptare estimat și distanța laterală, în limita
  capacității cozii și a spațiului fizic rămas pe banda cabinei.
* **Stările cabinei**: `Closed → Idle → WaitingForVehicle → ProcessingPayment → OpeningBarrier →
  VehiclePassing → ClosingBarrier → Idle`. Stările vehiculului: `Spawning, Driving, ApproachingQueue,
  WaitingInQueue, ApproachingBooth, Paying, WaitingForBarrier, PassingBarrier, Exiting, Completed`.
* **Plata** (`PaymentProcessor`): taxa = `Taxa de bază × Multiplicatorul cabinei × Bonusul activ`, în
  cenți întregi. Banii se primesc **doar la plată**, o singură dată pe călătorie (apelurile duplicate
  sunt ignorate).
* **Angajații**: cabinele manuale au nevoie de un angajat (nivel 1 = 3,0 s … nivel 5 = 1,8 s). Un angajat
  poate lucra la o singură cabină. Dacă retragi un angajat de la o cabină cu coadă, cabina servește
  întâi toată coada și abia apoi se închide.
* **Extinderea**: cabine de la 1 la 6 (cabinele 3–4 cer 2 benzi, cabinele 5–6 cer 3 benzi); benzi de la 1 la 3.
* **Salvarea**: JSON cu versiune (`JsonUtility`). Se scrie întâi un fișier temporar, apoi se înlocuiește
  fișierul principal (`File.Replace`) păstrând o copie `.bak`. Dacă fișierul principal e corupt, se
  încarcă copia de rezervă. Salvările vechi sunt migrate, iar cele scrise de o versiune mai nouă nu se
  încarcă (fișierul e păstrat). Mașinile aflate pe drum nu se salvează: nu plătiseră încă, așa că o
  reîncărcare nu poate plăti de două ori.

## Rularea testelor fără Unity

```bash
cd "Toll Road Tycoon/Tools/SimulationTests"
dotnet test
```

Aceleași teste rulează și în Unity: **Window → General → Test Runner → EditMode**.

## Deschiderea în Unity

1. Unity Hub → *Add project from disk* → alege directorul `Toll Road Tycoon`. Folosește Unity
   **2022.3 LTS** cu modulul **Android Build Support**.
2. La primul import, `ProjectSetup` creează `Assets/Scenes/Boot.unity`, o adaugă în Build Settings și
   setează orientarea portret, `com.animatix.tollroadtycoon`, IL2CPP și ARM64. Pasul poate fi repetat
   din *Tools → Toll Road Tycoon → Configure Project*.
3. Deschide `Boot.unity` și apasă Play. `GameBootstrap` construiește la rulare camera, lumea, interfața și sunetul.
4. APK de test pentru Android: *Tools → Toll Road Tycoon → Build Android APK (test)*, sau din linia de comandă:
   `Unity -batchmode -quit -projectPath "Toll Road Tycoon" -executeMethod TollRoadTycoon.EditorTools.BuildAndroid.Apk`.
   Pentru AAB: `...BuildAndroid.Aab`. Nu e configurată nicio cheie de semnare; adaug-o pe a ta înainte
   de publicarea în magazin.

## Raport de verificare (Faza 1)

Ce s-a rulat efectiv în acest mediu (container Linux, fără Unity Editor și fără Android SDK):

| Verificare | Rezultat |
|---|---|
| Simularea și testele compilate cu .NET 8, la nivelul de limbaj C# 9 (compatibil cu Unity 2022.3) | ✅ 0 avertismente |
| Teste NUnit (`dotnet test`) | ✅ 57/57 trecute |
| Stratul Unity compilat cu bibliotecile de **referință** Unity (modulele UnityEngine 2021.3 + UnityEngine.UI 2020.3, de pe NuGet) | ✅ 0 erori, 0 avertismente |
| Scripturile de editor (`Assets/Scripts/Editor`) compilate | ❌ neverificat: nu a existat o bibliotecă de referință UnityEditor |
| Deschis în Unity Editor / Play mode / verificarea vizuală a graficii și a interfeței | ❌ nefăcut: Unity nu e instalat aici |
| Build Android APK/AAB | ❌ nefăcut: lipsesc Unity și Android SDK |

Ce acoperă testele:

* toate stările unei mașini prin plază, iar banii vin doar la plată, nu la apariția mașinii
* verificarea exactă a suprapunerii pentru **fiecare pereche de vehicule la fiecare pas**, cu 1/2/4/6
  cabine, pe 300 de secunde simulate (nicio suprapunere)
* plăți exact o dată (apeluri duplicate, indicator resetat), câștigurile egale cu suma taxelor încasate,
  vehiculele de urgență trec gratuit
* capacitatea cozii e respectată, debitul crește cu numărul de cabine, toate benzile sunt folosite,
  vehiculele sunt refolosite din pool
* regulile angajaților (o singură cabină pe angajat, costuri de angajare și upgrade, coada se golește
  înainte de închidere, traficul trece la cabina deschisă)
* upgrade-urile (valori reale înainte/după, o cumpărare eșuată nu schimbă nimic, niveluri maxime,
  taxa ×2 dublează venitul la același trafic, condițiile pentru automatizare)
* extinderea (geometrie și rutare noi, condiția de benzi, plata o singură dată, benzile noi chiar au trafic)
* salvarea și încărcarea, fără plată dublă la reîncărcare, recuperarea din copia de rezervă, migrarea
  versiunii 0, refuzul versiunilor mai noi, limitarea valorilor modificate manual
* traducerile: cheile din română și engleză sunt identice

Test de încărcare (fără afișare, un pas de simulare la 30 Hz, măsurat pe procesorul acestui container):

| Vehicule | ms pe pas |
|---|---|
| ~60 | 0,02 |
| 100 | 0,03 |
| ~160 | 0,04 |
| ~290 | 0,08 |

Timpii sunt doar pentru simulare. Costul de afișare pe telefon nu a fost măsurat.

Echilibrul economiei, verificat cu un jucător simulat (strânge bani pentru următoarea cabină sau bandă
când costă mai puțin de ~6 minute de venit, altfel cumpără cel mai ieftin upgrade): 10 mașini servite
la ~1 min, primul upgrade de viteză la ~0,2 min, cabina 2 la ~12 min, cabina 3 la ~45 min, cabina 4
la ~72 min, camioanele deblocate la ~3 ore. Începutul jocului poate părea încă lent și trebuie ajustat
după testarea pe telefon.

### Limitări cunoscute și abateri (Faza 1)

* **Grafica e provizorie**, generată din cod (forme rotunjite colorate), după paleta și aranjarea din
  schiță. Nu e grafica finală. Codul încarcă însă automat grafica desenată din `Assets/Resources/Art/`;
  lista completă de imagini și prompturile de generare sunt în [`ASSETS.md`](ASSETS.md).
* **Textele folosesc `Text` clasic din uGUI cu fontul inclus în Unity**, nu TextMeshPro. TextMeshPro
  cere importul *TMP Essential Resources* în editor, ceea ce nu s-a putut face sau verifica aici.
  Trecerea la TextMeshPro rămâne pentru Faza 2.
* Scene: o singură scenă, `Boot` (creată de `ProjectSetup`). Meniul principal și jocul sunt ecrane
  construite la rulare, nu scene separate. Scenele RoadMap și Shop vin în Faza 2.
* DRUMURI, MAGAZIN și MISIUNI sunt marcate vizibil **„În curând”** și deschid o explicație în loc să
  pretindă că funcționează. Muzica e marcată „în curând”. Efectele sonore sunt tonuri simple generate din cod.
* Neimplementat încă (Faza 2): cele cinci porțiuni de drum și harta lor, fereastra de câștiguri offline
  (momentul ultimei vizite, în UTC, se salvează deja), serviciile de pe marginea drumului, misiunile și
  tutorialul, magazinul, ecranul de statistici, decorațiunile, reclamele (`AdService`), testele PlayMode.

## Planul pentru Faza 2

Cinci porțiuni de drum cu configurație proprie, venit calculat pentru porțiunile inactive, scena RoadMap,
câștiguri offline (limită de 8 ore, etichetă de estimare, COLECTEAZĂ / DUBLEAZĂ CU RECLAMA cu un serviciu
de reclame în mod test), servicii (benzinărie, parcare, cafenea …) cu venit generat de clienți reali,
misiuni și tutorial, magazin, statistici, trecerea la TextMeshPro, grafică și sunet finale, teste
PlayMode și de navigare în interfață, măsurători de performanță pe telefon și un build Android real.
