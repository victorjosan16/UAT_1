# VELIQORA — Color Sorting Adventure

Original water-sort puzzle for Android, built in **Unity 6 (LTS)** with C#.
The working title for the store is *Veliqora: Color Sort Puzzle*. This repo makes no claim about trademark status or Play Store availability.

> **Status (honest summary):** the pure C# game core, 200 verified levels and 95 automated tests are
> implemented and **were executed** in this environment (.NET 8). The Unity layer (scenes, UI, rendering,
> audio, ads, editor tooling, PlayMode tests) is implemented and **compiles** against Unity reference
> assemblies. It has **not been run inside the Unity Editor or on a device yet**, because no Unity Editor or license was
> available here. See [IMPLEMENTATION_STATUS.md](IMPLEMENTATION_STATUS.md) and [TEST_REPORT.md](TEST_REPORT.md).

## Quick start (Unity)

1. Install **Unity 6 LTS (6000.0.x)** with **Android Build Support** (SDK/NDK/OpenJDK).
2. In Unity Hub choose **Add project from disk** and select this `Veliqora/` folder. Unity resolves `Packages/manifest.json`
   (uGUI, Input System, Test Framework). If Unity asks to enable the new Input System backend, either choice works,
   because the code supports both.
3. Run the menu **Veliqora → Setup Project**. It is idempotent and does the following:
   - creates the scenes `Bootstrap, Home, LevelMap, Gameplay, LevelComplete, Shop, Customization` (each with a `SceneEntry` marker)
     and adds them to Build Settings in that order;
   - sets the Android player settings: package placeholder `com.animatix.veliqora`, portrait, IL2CPP, ARM64, min SDK 24, target SDK *auto*;
   - generates the PLACEHOLDER app icons and the feature graphic in `Assets/_Veliqora/Art/Icons/`.
4. Open `Assets/_Veliqora/Scenes/Bootstrap.unity`, set the Game view to a portrait resolution (e.g. 1080×1920) and press **Play**.
5. Commit the `.meta` files Unity generates on first import, so asset GUIDs stay stable for everyone.

The game is playable even before step 3. The composition root boots automatically, and the navigator falls back to runtime-created scenes when the
real scenes aren't in Build Settings.

## What's inside

| Area | Where | Notes |
|---|---|---|
| Pure game core (no `UnityEngine`) | `Assets/_Veliqora/Scripts/Core` (`Veliqora.Core.asmdef`, `noEngineReferences`) | rules, solver, generator, session/undo/hints, economy, save, ads contract |
| Unity runtime | `Assets/_Veliqora/Scripts/{Infrastructure,UI,Presentation,Gameplay,Audio,Settings,Ads}` (`Veliqora.Runtime.asmdef`) | composition root, navigation, screens, liquid rendering, audio, haptics, mock ads |
| Editor tooling | `Assets/_Veliqora/Editor` | Setup Project, Validate Project, Generate/Batch-Validate Levels, Android builds, screenshots |
| Content | `Assets/_Veliqora/Resources/Veliqora/{Levels,Config}` | `levels.json` (200 levels), `economy.json` (prices/rewards/cosmetics), `ads.json` (placeholder IDs) |
| Tests | `Assets/_Veliqora/Tests/{EditMode,PlayMode}` | EditMode tests run in Unity **and** via `dotnet test` |
| Offline tools | `Tools/` | .NET projects: core build, LevelGen, test runner, Unity compile check, art preview |
| Docs | `DESIGN_SPEC.md`, `IMPLEMENTATION_STATUS.md`, `TEST_REPORT.md`, `Docs/` | build/release guide, release checklist, level reports, art previews |

## Offline commands (no Unity required)

```bash
# from Veliqora/
dotnet test Tools/Veliqora.Core.Tests                         # 95 EditMode tests against the pure core
dotnet run -c Release --project Tools/LevelGen -- build       # regenerate the 200 levels deterministically (~5 s)
dotnet run -c Release --project Tools/LevelGen -- validate    # replay + independent solver check -> Docs/level_validation_report.csv
dotnet build Tools/UnityCompileCheck                          # compile Unity-side code against reference assemblies (optional)
dotnet run -c Release --project Tools/ArtPreview -- Docs/ArtPreview   # render procedural art previews to PNG
```

`UnityCompileCheck` pulls third-party repackaged Unity reference DLLs from NuGet (UnityEngine 2021.3 modules, uGUI 2020.3,
UnityEditor 2021.1). It is a fast sanity check only; opening the project in Unity 6 is the source of truth.

## Gameplay summary

- Tap a bottle to select it, then tap a destination. The contiguous top run of one color pours, limited by free space.
  A pour is legal only onto the same color or into an empty bottle.
- The level is won when every bottle is empty, or full and a single color.
- There are four boosters: **Undo** (exact snapshot revert), **Add bottle** (one temporary empty bottle), **Shuffle**
  (a new *verified-solvable* layout, with a disclosure before it restarts) and **Hint** (the next move from the solver; it is charged only when a real hint is found).
- Stars depend on the move count relative to a solver benchmark, with forgiving, published thresholds (see DESIGN_SPEC §5).
- Coins come from levels and milestones. Everything is offline-first, and rewarded ads are optional, guarded and never block progress.

## Replacing placeholder art and audio

All visuals and sounds are **procedurally generated placeholders** and are labelled as such. To ship authored assets, drop files into
`Resources/Veliqora/...` with the documented names. They override the generated versions automatically, with no code changes
(see DESIGN_SPEC §9).

## License / assets

All code and procedural art in this folder were written for this project. No paid assets, no external art, no cloud services.
