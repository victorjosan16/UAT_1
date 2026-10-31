# Toll Road Tycoon

Joc idle de administrare a unei autostrăzi cu taxă, pentru Android (Unity 2D, portret 9:16).
Mașinile vin, stau la coadă, plătesc la cabină, bariera se ridică, iar tu investești câștigurile
în cabine, angajați, benzi și upgrade-uri.

**Stadiu: Faza 1 și Faza 2 implementate** (bucla de bază, 5 porțiuni de drum, hartă, servicii,
decor, misiuni, magazin, statistici, venit offline, reclame în mod test, muzică și sunete).
Citește raportul de verificare de mai jos: logica e testată automat fără Unity, dar proiectul
**nu** a fost încă deschis în Unity Editor și nici construit pentru Android.

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
      Core/TycoonGame  rădăcina jocului complet: 5 porțiuni, economie comună, estimări, offline, reclame
      Services/        ServiceManager (clădiri de servicii cu clienți și venit pe eveniment)
      Shop/            DecorationManager (decor cosmetic)
      Missions/        MissionManager (misiuni zilnice + lanțul de obiective / tutorial)
      Monetization/    IAdService (dezactivat / test), AdRewards, BoostManager
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

## Funcționalități (Faza 2)

* **5 porțiuni de drum** (Km 0–5, 5–10, 10–20, 20–30, 30–50), fiecare cu prețuri, taxe, trafic,
  limite de cabine/benzi, vehicule și servicii permise proprii, plus condiții de deblocare (cabine pe
  porțiunea anterioară, venit/min, preț). Se poate trece oricând între porțiunile deblocate (HARTĂ).
* **Doar porțiunea curentă e simulată mașină cu mașină**; celelalte porțiuni deblocate câștigă dintr-o
  estimare analitică (debit × taxă medie × 0,85). Testele arată că estimarea brută e la ±5% din simulare.
  Nimic nu se numără de două ori: porțiunea activă primește doar plăți reale.
* **Servicii** (parcare, cafenea, benzinărie, zonă de odihnă, magazin, spălătorie, stație de încărcare):
  8 locuri lângă drum, construire cu confirmare, mutare, demolare (50% înapoi), upgrade pe nivele.
  Venitul vine doar de la clienți: o mașină care a plătit taxa poate opri la un serviciu cu loc liber și
  plătește când termină. Clienții sunt evenimente simulate, nu mașini care se văd conducând spre clădire.
* **Decor** cosmetic (bancă, felinar, flori, coș, con, gard) în 8 locuri, cu mutare și eliminare.
* **Misiuni zilnice** (4 pe zi, generate pe data UTC) și **lanțul de obiective** care servește și ca
  tutorial (banner în HUD). Recompensele se acordă o singură dată.
* **Magazin**: vehicule, decor, clădiri, bonusuri (x2 pe 2 min prin reclamă, bani instant prin reclamă,
  x2 pe 5 min pe bani din joc).
* **Venit offline**: estimare pentru toate porțiunile deblocate, limită 8 ore, fereastră COLECTEAZĂ /
  DUBLEAZĂ CU RECLAMA, acordat o singură dată; marcaje de timp invalide sau din viitor nu dau nimic.
* **Reclame**: interfața `IAdService`. În Editor și în build-urile de dezvoltare rulează **modul TEST**
  (un ecran clar marcat „RECLAMĂ DE TEST”, recompensă doar dacă aștepți până la final). În build-urile
  de release reclamele sunt **dezactivate** și butoanele lor nu apar. Nu există niciun SDK sau ID de producție.
* **Statistici**, setări (muzică, sunete, vibrații, calitate, limbă, confidențialitate, resetare),
  muzică de fundal generată din cod.
* **Salvare v3** cu migrare automată a salvărilor vechi (drumul unic devine porțiunea 1).

## Raport de verificare

Ce s-a rulat efectiv în acest mediu (container Linux, fără Unity Editor și fără Android SDK):

| Verificare | Rezultat |
|---|---|
| Logica jocului + teste compilate cu .NET 8, la nivelul de limbaj C# 9 (compatibil cu Unity 2022.3) | ✅ 0 avertismente |
| Teste NUnit (`dotnet test`) | ✅ 85/85 trecute |
| Stratul Unity compilat cu bibliotecile de **referință** Unity (UnityEngine 2021.3 + UnityEngine.UI 2020.3, de pe NuGet) | ✅ 0 erori, 0 avertismente |
| Scripturile de editor (`Assets/Scripts/Editor`) compilate | ❌ neverificat: nu a existat o bibliotecă de referință UnityEditor |
| Deschis în Unity Editor / Play mode / verificare vizuală a interfeței | ❌ nefăcut: Unity nu e instalat aici |
| Teste PlayMode / navigare în interfață | ❌ nescrise: nu pot fi rulate fără Unity |
| Build Android APK/AAB | ❌ nefăcut: lipsesc Unity și Android SDK |

Testele noi din Faza 2 acoperă: cele 5 porțiuni cu progresie distinctă, condițiile de deblocare,
schimbarea porțiunii fără pierderea progresului, venitul porțiunilor inactive fără dublă numărare,
precizia estimării față de simulare, venitul offline (limită, marcaje invalide, colectare o singură
dată, păstrare dacă nu e colectat), dublarea doar cu reclamă terminată, reclame dezactivate fără
recompensă, bonusul x2, regulile și locurile serviciilor, venitul din servicii doar de la clienți,
upgrade-urile serviciilor, decorul și salvarea lui, misiunile (ordinea, o singură revendicare,
reîmprospătare zilnică), salvarea completă a jocului și migrarea din versiunea 2.

Trafic: la verificarea suplimentară cu autobuze și TIR-uri pe porțiunea cea mai aglomerată au apărut
suprapuneri în plază. Le-am corectat (curbe mai lungi, limită de deviere laterală, rezervări ținute până
iese tot vehiculul, distanță mai mare pe curbe pentru vehiculele lungi) și am adăugat teste permanente;
o rulare de verificare cu 36 de combinații × 400 s nu a mai găsit nicio suprapunere.

Echilibrul primei porțiuni (jucător simulat): cabina 2 la ~11 min, cabina 3 la ~45 min, cabina 4 la ~70 min.
Echilibrul porțiunilor 2–5 nu a fost simulat pe durate lungi; prețurile lor trebuie ajustate după testare.

### Limitări cunoscute

* Grafica vine din foile de imagini primite, decupate automat; unele obiecte sunt mici în sursă și pot
  părea neclare pe ecran (vezi `ASSETS.md`).
* Textele folosesc `Text` clasic din uGUI, nu TextMeshPro (TMP cere importul resurselor în editor).
* O singură scenă (`Boot`); meniul, harta, magazinul și jocul sunt ecrane/ferestre construite la rulare.
* Nu există achiziții reale (fără „Restabilește achizițiile”) și niciun SDK de reclame real.
* Clienții serviciilor nu sunt animați ca mașini care merg la clădire.
