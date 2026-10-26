# Underground Metro Tycoon: Phase 1 (Core Playable Prototype)

A portrait 3D isometric idle tycoon for Android (iOS later), built with Unity 6 + URP.
Phase 1 covers the first gameplay loop: walk with the joystick, auto-dig soil with a basic shovel,
finish an excavation contract, get paid, and keep playing.

> Status: the Unity scripts and project files are written. The simulation layer is unit-tested with .NET.
> The Unity-side code compiles against the UnityEngine API, but **it has not been opened in the Unity Editor
> or run on a device yet**. See "Verification status" below.

## What's in Phase 1

| Feature | Where |
|---|---|
| Isometric orthographic camera: smooth follow, pinch / mouse-wheel zoom, bounds | `Scripts/Runtime/Camera` |
| Stylised worker (helmet, vest, trousers, boots, shovel) with procedural idle / walk / dig animation | `Scripts/Runtime/Player/CharacterRig.cs` |
| Floating virtual joystick, plus WASD / arrow keys in the Editor | `Scripts/Runtime/UI/VirtualJoystick.cs` |
| Deterministic movement: obstacle collision, sliding, 0.55 m step-up, falling into dug holes | `Scripts/Simulation/Movement` |
| Grid excavation: 6 terrain types, HP, tool levels, accessibility, contract bounds, reservations | `Scripts/Simulation/Excavation` |
| Automatic digging: target pick, turn, swing, damage at the blade-impact frame | `Scripts/Runtime/Excavation/DigController.cs` |
| Hit punch, damage tint, break animation, pooled debris / dust / confetti, synthesised SFX | `Terrain/ExcavationSiteView.cs`, `VFX`, `Audio` |
| Contracts: Available → Accepted → In progress → Ready → Completed → Reward claimed | `Scripts/Simulation/Contracts` |
| Wallet that can't go negative; each reward is paid exactly once | `Scripts/Simulation/Economy`, `Core/GameSession.cs` |
| XP and levels | `Scripts/Simulation/Progression` |
| HUD (level/XP, coins, tool, contract card), contract board, settings, toasts, flying coins | `Scripts/Runtime/UI` |
| Versioned, checksummed save with atomic replace, backup recovery and migration hook | `Scripts/Simulation/Save` |
| Procedural construction-site world (city, roads, crane, props) | `Scripts/Runtime/Terrain/SiteEnvironmentBuilder.cs` |

**Phase 1 contracts** (default config; editable in the `GameConfig` asset):

1. **Garden Trench**: 20 soft-soil blocks, pays 500 coins. Available immediately with 0 coins and the basic shovel.
2. **Small Trench**: 12 blocks, pays 120. Repeatable, so the player can always earn money and never gets stuck.
3. **Residential Foundation**: 2 layers (soft soil, then compact soil), pays 1,200. Unlocks at level 2. The pit is dug
   layer by layer and keeps one row as a step, so the player can always climb out.

Each dug block also pays coins and XP directly (5 coins for soft soil, 8 for compact).

## Architecture

```
Assets/_UndergroundMetroTycoon/
  Scripts/
    Simulation/   UndergroundMetroTycoon.Simulation (pure C#, noEngineReferences). All game rules and state.
      Core/ Economy/ Excavation/ Contracts/ Progression/ Save/ Movement/ Tools/
    Runtime/      UndergroundMetroTycoon.Game. Unity views, input, camera, UI, VFX, audio, bootstrap.
      Core/ Player/ Camera/ Terrain/ Excavation/ UI/ Audio/ VFX/ Save/ Visuals/
    Editor/       Project setup + Android build menu items
  Tests/EditMode  NUnit tests for the simulation (also runnable without Unity, see below)
  Tests/PlayMode  Smoke test that boots the game and auto-digs a block
Tools~/           .NET projects for running the tests outside Unity (ignored by Unity because of the "~")
```

- **Simulation and rendering are separate.** `GameSession` is the only place that changes state or pays rewards.
  Views subscribe to its events and render whatever the authoritative data says.
- **Config is data-driven.** `GameConfig` (a ScriptableObject) holds terrain, tool, contract and progression
  balancing. `ContentValidator` rejects configs that would soft-lock the player: no starter contract, no repeatable
  contract, or a pit with no walkable way out. If the config is invalid, the game falls back to the defaults.
- **No global singletons.** `GameBootstrap` is the composition root and creates everything under one runtime root.
- The suggested folder layout was adjusted slightly: game rules live in `Simulation/` (no UnityEngine) and Unity code
  in `Runtime/`, so the rules can be unit-tested without the Editor.

## Opening the project

1. Install **Unity 6 (6000.0 LTS)** with **Android Build Support** (OpenJDK + Android SDK & NDK) via Unity Hub.
2. Unity Hub → **Add → Add project from disk** → choose the repository root folder (`UAT_1/`).
   There is no `ProjectSettings/` folder, so Hub asks which editor to use; pick your Unity 6 version.
   On first import Unity generates `ProjectSettings/`, `Library/` and `.meta` files. **Commit the generated
   `ProjectSettings/` and `.meta` files** afterwards.
3. When prompted *"Create the Phase 1 scene…?"*, click **Set up now**. You can also run it any time from
   **Underground Metro Tycoon → Setup Project (Phase 1)**. The setup:
   - creates and assigns a URP pipeline asset (`Settings/UMT_URP_Mobile.asset`) if the project has none,
   - creates the base lit material (`Materials/UMT_BaseLit.mat`) and `ScriptableObjects/GameConfig.asset`,
   - creates `Scenes/Phase1_ConstructionSite.unity` and puts it first in Build Settings,
   - sets portrait orientation, product name, an Android bundle id (`com.undergroundmetro.tycoon`, only if still the
     default), IL2CPP and ARM64,
   - turns on the legacy Input Manager ("Both") if the project was set to the new Input System only. The UGUI
     joystick uses `StandaloneInputModule`. Restart the Editor if it asks you to.

   If automatic URP setup fails for your URP version, create one manually with *Assets → Create → Rendering → URP
   Asset (with Universal Renderer)*, assign it in *Project Settings → Graphics* and *Quality*, then run the setup again
   so the base material picks up the URP shader.

## Running the first scene

Open `Assets/_UndergroundMetroTycoon/Scenes/Phase1_ConstructionSite.unity` and press **Play**.
Set the Game view to a portrait resolution (e.g. 1080×1920).

- Move: drag anywhere on the lower half of the screen (or use WASD / arrow keys). Zoom: mouse wheel or pinch.
- Tap **CONTRACTS** → **ACCEPT** on *Garden Trench*. Walk to the soil and stop next to it; digging starts automatically.
- When all 20 blocks are gone, tap **CLAIM** on the contract card. Then accept *Small Trench* (repeatable) to keep earning.
- **Settings** (top-right ≡): sound, graphics quality (Low = no shadows, 30 FPS, fewer particles), reset progress.
- Progress saves every 10 s when something changed, on contract accept/claim, and when the app is paused or quit.
  The file is `Application.persistentDataPath/umt_save.json`, with a `.bak` copy of the previous save.

## Tests

**Without Unity** (requires the .NET 8 SDK):

```bash
dotnet test  "Tools~/SimulationTests"     # 63 NUnit tests for the simulation
dotnet build "Tools~/SimulationApiCheck"  # compiles Simulation as .NET Standard 2.1 / C# 9 (Unity's profile)
```

**In Unity:** *Window → General → Test Runner* → **EditMode** (the same 63 tests, using Unity's JsonUtility for the
save tests) and **PlayMode** (`Phase1SmokeTests`: boots the game, accepts the first contract, checks that the player
auto-digs a block and earns coins).

Coverage: wallet edge cases, HP clamping, accessibility and layer-by-layer digging, contract bounds, tool levels,
reservations, deterministic site generation, the full contract lifecycle with no double payouts, repeatable
contracts, level gating, the progression curve, save round-trip, backup recovery, checksum tampering, newer-version
rejection, migration, repair of inconsistent saves, movement collision, step and fall behaviour, config soft-lock
detection, and a bot that finishes all three contracts using only the real movement and auto-dig rules.

## Android test build

- Menu: **Underground Metro Tycoon → Build Android Test APK**. This makes a development APK at
  `Builds/Android/UndergroundMetroTycoon-phase1.apk`.
- Command line:
  ```bash
  "<Unity 6 path>/Unity" -batchmode -quit -projectPath . \
    -executeMethod UndergroundMetroTycoon.EditorTools.AndroidBuild.BuildFromCommandLine -logFile build.log
  ```
- Install: `adb install -r Builds/Android/UndergroundMetroTycoon-phase1.apk`.
- Before testing performance, connect the Unity Profiler to the development build on a real mid-range device.
  The 30/60 FPS targets have **not** been measured yet.

## Verification status

| Check | Result |
|---|---|
| Simulation layer: 63 NUnit tests with .NET 8 | Passed |
| Simulation compiled as .NET Standard 2.1 / C# 9, warnings as errors | Passed |
| Runtime + test code compiled against UnityEngine 2021.3 reference assemblies, with a hand-written UGUI API stub | Compiles, 0 warnings |
| Editor scripts (setup, Android build) | **Not compiled.** No UnityEditor reference assembly was available |
| Opening in Unity 6, URP setup, scene generation, Play mode, PlayMode test | **Not run.** No Unity Editor in the dev environment |
| Android build, on-device input, performance | **Not run** |

## Known limitations (Phase 1)

- All art is procedural placeholder geometry built from Unity primitives. It is not production artwork.
- Animation is procedural (no Animator / skinned meshes). Only the basic shovel exists; there are no tool upgrades
  yet, so coins have no spending sink until Phase 3.
- Excavated material counts as sold on removal. Transport and storage come in Phase 3.
- The UI is in Romanian and uses the Baloo 2 font (SIL Open Font License, `Resources/Fonts/OFL-Baloo2.txt`). Visual redesign notes: `docs/VISUAL_MODERNIZATION.md`.
- Input uses the legacy Input Manager by default. If you install the Input System package (`com.unity.inputsystem`, a version that supports your Unity version), the game uses it automatically.
- No offline earnings, workers, machinery, metro, ads or purchases. Those are later phases by design.

## Next milestone: Phase 2 (Employee Automation)

Worker hiring (3 slots), worker AI state machine using the existing `ExcavationService` reservations, central task
allocation, worker upgrades and operating costs. Starts only after Phase 1 is reviewed and approved.
