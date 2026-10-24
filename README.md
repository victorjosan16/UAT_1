# Quarry Empire

Joc mobil 3D idle / hyper-casual de minerit (Unity 6 LTS, URP, Android, portret).
Sapi într-o carieră, aduni minereu pe spate, îl vinzi sau îl rafinezi, cumperi upgrade-uri,
utilaje și muncitori și cobori strat cu strat spre minereuri tot mai rare.

## Cum se joacă
- **Joystick** (jos): mișcare. Minerul lovește singur cel mai apropiat bloc.
- Resursele sar din bloc și **zboară pe spatele tău** (stiva are capacitate limitată).
- **VINDE** (placa verde, stânga): vinde automat tot ce cari.
- **Upgrade-uri** (plăcile mov, rândul de jos): stai pe ele ca să plătești — Forță, Viteză lovire,
  Viteză mers, Capacitate, Târnăcop (sparge blocuri mai dure).
- **Plăcile portocalii** = cumpărături plătite stând pe ele: Rafinărie, Muncitori, Buldozer, Camion, Burghiu.
- **Rafinăria** (dreapta): lasă minereul pe placa „MINEREU”, ia lingourile de pe „LINGOURI” (valorează mai mult).
- **Comenzi** (camionul albastru, stânga sus): livrează X resurse în Y secunde pentru bonus.
- **Utilaje**: stai pe placa „URCĂ” de lângă utilaj ca să urci; butonul „COBOARĂ” te dă jos.
  Buldozerul sparge pe arie, burghiul sparge orice (inclusiv diamant).
- **Camionul**: îl încarci pe placa „ÎNCARCĂ CAMIONUL”; pleacă singur la vânzare.
- **Muncitorii** minează și vând singuri; câștigă și cât jocul e închis (max. 2 ore).
- Când cariera e curățată, **cobori la stratul următor** (minereuri mai rare, blocuri mai dure).
- Butoanele albastre din dreapta: **reclame opționale** (bani x2 / bonus). Acum sunt reclame de test.

## Structură
```
Assets/_Game/
  Editor/        GameSetup.*.cs — construiește automat proiectul, datele, prefab-urile și scena
  Scripts/Core   GameManager, SaveSystem, ObjectPool, GameEvents, ZoneArea, PlayerRoot, ...
  Scripts/Player Mișcare, minat, stivă, colectare, upgrade-uri aplicate, urcare în utilaje
  Scripts/Mining Blocuri, resurse, straturi (LayerGenerator)
  Scripts/Economy Bani, Sell, zone plătite, upgrade-uri, rafinărie, câștiguri offline
  Scripts/Machines Vehicul, buldozer, burghiu, camion
  Scripts/NPC    Muncitori
  Scripts/Orders Comenzi de la camioane
  Scripts/Effects Sunete procedurale, particule, tremur cameră, vibrații
  Scripts/Monetization Reclame rewarded (test) + butoane
  Scripts/UI     HUD, meniu, setări, etichete 3D, sprite-uri procedurale, zonă sigură
  Scripts/Visual Mesh rotunjit, animații personaj, decor animat, profiluri de calitate
```

## Valori de echilibru
Toate prețurile, viețile blocurilor, straturile, upgrade-urile, utilajele și comenzile sunt ScriptableObjects
în `Assets/_Game/Data/`. Ele sunt **generate și suprascrise** de `Editor/GameSetup.Data.cs` la fiecare build,
deci valorile se schimbă în acel fișier.

## Build în cloud (fără calculator) — Unity Build Automation
Configurație: Platform **Android**, Branch `main`, Project subfolder gol, Unity **6000.0.x (Unity 6 LTS)**,
**Pre-Export Method = `QuarryCloudBuild.PreExport`**, output **APK**, Bundle ID `com.victorjosan.quarryempire`.

## Documentație
- `docs/VIZUAL.md` — personaj, mediu, HUD, efecte, performanță: ce face fiecare componentă și unde se configurează
- `docs/MONETIZARE.md` — cum se activează reclamele reale (Unity LevelPlay)
- `docs/GOOGLE_PLAY.md` — pașii pentru publicarea pe Google Play
