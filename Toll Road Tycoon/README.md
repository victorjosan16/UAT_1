# Toll Road Tycoon

Idle toll-road management game for Android (Unity 2D, portrait 9:16).
Joc idle de administrare a unei autostrăzi cu taxă: mașinile vin, stau la coadă, plătesc la cabină,
bariera se ridică, iar tu investești câștigurile în cabine, angajați, benzi și upgrade-uri.

**Status: Phase 1 (first playable loop).** Read the QA report below before relying on anything:
the simulation is tested headlessly, but the Unity project has **not** been opened in the
Unity Editor or built for Android yet.

## Project layout

```
Toll Road Tycoon/                      Unity project root (open this folder in Unity Hub)
  Assets/
    Scripts/
      TollRoadTycoon.Simulation.asmdef   engine-independent game logic (noEngineReferences)
      Core/            GameSimulation (root/fixed step), GameConfig, GameStats, Vec2, SimRandom
      Traffic/         LaneManager (geometry), TrafficManager (car following, plaza gates),
                       QueueManager (booth choice), VehicleSpawner, Vehicle + VehiclePool
      TollSystem/      TollBooth (data), TollBoothController (state machine), PaymentProcessor
      Economy/         EconomyManager, Money (integer cents, K/M/B format), CostCurve
      Workers/         Worker, WorkerManager
      Upgrades/        UpgradeManager (booth upgrades, vehicle unlocks)
      RoadExpansion/   RoadExpansionManager (booth slots, lanes)
      Save/            SaveData (versioned), SaveManager (atomic write, backup, migration)
      Localization/    Loc (Romanian primary, English complete)
      Presentation/    Unity layer (TollRoadTycoon.Presentation.asmdef)
        Core/          GameBootstrap, GameRoot, SaveService, GameConfigAsset (ScriptableObject)
        Views/         WorldView, BoothView, VehicleViews, CoinPopups, SpriteFactory, Palette
        UI/            UIManager, MainMenuScreen, GameplayScreen, Panels, UIFactory, SafeAreaFitter
        Input/         CameraController (drag pan, pinch/wheel zoom, tap select)
        Audio/         AudioManager (procedural SFX)
      Editor/          ProjectSetup (Boot scene, portrait, Android ids), BuildAndroid
    Tests/EditMode/    NUnit tests (Unity Test Runner + headless dotnet)
  Packages/manifest.json
  ProjectSettings/ProjectVersion.txt   2022.3 LTS
  Tools/SimulationTests/               dotnet project that compiles the simulation + tests without Unity
```

All game rules live in the `Simulation` assembly, which has no `UnityEngine` reference.
That is why the simulation and its tests can run without the Unity Editor. The Unity layer only reads the
simulation and calls its purchase APIs.

## How the core loop works

* **Road geometry** (`LaneManager`): approach lanes → S-curve plaza-entry connectors → one booth lane
  per booth → plaza-exit connectors → exit lanes, plus an untolled oncoming carriageway.
  Buying a lane or booth **adds real segments** that routing uses immediately.
* **Traffic** (`TrafficManager`): each vehicle follows its route. Its speed is limited so it can always
  stop before the rear of the car ahead (plus a gap). On top of that, a hard clamp stops it from ever
  moving past that point, so cars cannot overlap or teleport. Connectors that cross or merge are
  entered one at a time through reservations, and only the front car of a lane may reserve, so there
  are no deadlocks.
* **Booth choice** (`QueueManager`): expected wait plus lateral distance, limited by queue capacity and
  by the physical space left in the booth lane.
* **Booth state machine**: `Closed → Idle → WaitingForVehicle → ProcessingPayment → OpeningBarrier →
  VehiclePassing → ClosingBarrier → Idle`. Vehicle states: `Spawning, Driving, ApproachingQueue,
  WaitingInQueue, ApproachingBooth, Paying, WaitingForBarrier, PassingBarrier, Exiting, Completed`.
* **Payment** (`PaymentProcessor`): toll = `BaseVehicleToll × BoothMultiplier × ActiveBonus` in integer
  cents. Money is awarded **only on payment**, once per trip id (duplicate calls are ignored).
* **Workers**: manual booths need a worker (level 1 = 3.0 s … level 5 = 1.8 s). A worker staffs at most
  one booth. Withdrawing a worker from a booth with a queue first serves the whole queue, then closes the booth.
* **Expansion**: booths 1→6 (booth 3–4 need 2 lanes, booth 5–6 need 3 lanes); lanes 1→3.
* **Saves**: versioned JSON (`JsonUtility`), temp file + `File.Replace` with `.bak` backup, fallback
  to the backup when the main file is corrupt, migrations from older versions, and refusal to load
  saves written by a newer version (the file is kept). Vehicles in transit are not saved. They had
  not paid yet, so reloading can never pay twice.

## Running the tests without Unity

```bash
cd "Toll Road Tycoon/Tools/SimulationTests"
dotnet test
```

The same test files run inside Unity: **Window → General → Test Runner → EditMode**.

## Opening in Unity

1. Unity Hub → *Add project from disk* → select the `Toll Road Tycoon` folder. Use Unity **2022.3 LTS**
   with the **Android Build Support** module.
2. On first import, `ProjectSetup` creates `Assets/Scenes/Boot.unity`, adds it to Build Settings and
   sets portrait orientation, `com.animatix.tollroadtycoon`, IL2CPP and ARM64. You can re-run it from
   *Tools → Toll Road Tycoon → Configure Project*.
3. Open `Boot.unity` and press Play. `GameBootstrap` builds the camera, world, UI and audio at runtime.
4. Android test build: *Tools → Toll Road Tycoon → Build Android APK (test)*, or on the command line:
   `Unity -batchmode -quit -projectPath "Toll Road Tycoon" -executeMethod TollRoadTycoon.EditorTools.BuildAndroid.Apk`.
   AAB: `...BuildAndroid.Aab`. No signing keystore is configured; add your own before a store release.

## QA / build report (Phase 1)

What was actually executed in this environment (Linux container, no Unity Editor, no Android SDK):

| Check | Result |
|---|---|
| Simulation + tests compiled with .NET 8, C# 9 language level (Unity 2022.3 compatible) | ✅ 0 warnings |
| NUnit tests (`dotnet test`) | ✅ 57/57 passed |
| Presentation layer compiled against Unity **reference** assemblies (UnityEngine 2021.3 modules + UnityEngine.UI 2020.3 from NuGet) | ✅ 0 errors, 0 warnings |
| Editor scripts (`Assets/Scripts/Editor`) compiled | ❌ not checked: no UnityEditor reference assembly was available |
| Opened in Unity Editor / Play mode / visual check of the procedural art and UI layout | ❌ not done: Unity is not installed here |
| Android APK/AAB build | ❌ not done: no Unity/Android SDK here |

Covered by the tests:

* full state sequence of a single car through the plaza, and money paid only on payment, not at spawn
* exact oriented-box overlap check of **every pair of vehicles at every step** for 1/2/4/6 booths
  over 300 simulated seconds (no overlaps)
* exactly-once payments (duplicate calls, reset flag), earnings equal to the sum of the collected tolls, free emergency vehicles
* queue capacity is respected, throughput grows with booths, every lane is used, vehicles are recycled through the pool
* worker rules (one booth per worker, hire/upgrade costs, the queue drains before a booth closes, traffic reroutes to the open booth)
* upgrades (real before/after values, failed purchases change nothing, max levels, toll ×2 doubles income
  under the same traffic, automation prerequisites)
* expansion (new geometry and routing, lane prerequisites, charged once, new lanes carry traffic)
* save round-trip, no double award on reload, backup recovery, v0 migration, newer-version refusal, clamping of tampered values
* localization: the Romanian and English key sets are identical

Stress (headless, one simulation step at 30 Hz, measured on this container's CPU):

| Vehicles | ms per step |
|---|---|
| ~60 | 0.02 |
| 100 | 0.03 |
| ~160 | 0.04 |
| ~290 | 0.08 |

These timings cover the simulation only. Rendering cost on a phone has not been profiled.

Balance check (bot that saves for the next booth/lane when it costs less than ~6 minutes of income, otherwise
buys the cheapest upgrade): 10 cars processed at ~1 min, first speed upgrade at ~0.2 min,
booth 2 at ~12 min, booth 3 at ~45 min, booth 4 at ~72 min, trucks unlocked at ~3 h. The early
game may still feel slow and should be tuned after playtesting on a device.

### Known limitations and deviations (Phase 1)

* **Art is procedural placeholder art** (tinted rounded shapes generated at runtime), following the
  mockup's palette and layout. It is not final art.
* **Text uses legacy uGUI `Text` with Unity's built-in font**, not TextMeshPro. TMP needs *TMP Essential
  Resources* imported in the Editor, and that could not be done or verified here. Moving to TMP is a Phase 2 task.
* Scenes: a single `Boot` scene (created by `ProjectSetup`). The main menu and gameplay are runtime
  screens, not separate scenes. RoadMap/Shop scenes come in Phase 2.
* DRUMURI, MAGAZIN and MISIUNI are visibly marked **"În curând"** and open an explanation instead of
  pretending to work. Music is marked "în curând". SFX are simple procedural tones.
* Not implemented yet (Phase 2): five road sections and the road map, offline earnings popup (the
  last-seen UTC timestamp is already saved), roadside services, missions/tutorial, shop, statistics
  screen, decorations, ads (`AdService`), PlayMode tests.

## Phase 2 plan

Five road sections with their own config, analytical income for inactive sections, RoadMap scene,
offline earnings (8 h cap, estimate label, COLECTEAZĂ / DUBLEAZĂ CU RECLAMA with a test-mode ad service),
services (gas station, parking, café …) with event-driven income, missions and tutorial, shop,
statistics overlay, TMP migration, authored art and audio, PlayMode/UI navigation tests, device
profiling and a real Android build.
