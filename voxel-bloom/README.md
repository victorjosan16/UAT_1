# Voxel Bloom 3D (Unity, Android)

Original 3D voxel puzzle game built from the visual spec in `Docs/reference/mockup.png` and
`Docs/reference/MasterPrompt.txt`. Destroy a rotating voxel model with numbered colour blocks:
each block attacks the **exposed** voxels of its own colour, up to its power (2, 4, 8, 16, 32, 64).

> **Status: honest summary.** The game logic, the 34 models and the 100 levels are generated and
> **tested** here (Node). All C# passes a syntax check (tree-sitter). It has **not been compiled or run
> in Unity yet**, because no Unity Editor was available in this environment. Expect a first round of
> compile fixes in the Editor. See "Verification" below.

## Quick start
1. Open this folder (`voxel-bloom/`) with **Unity 6.6** (6000.6.7f1, the version used by Unity Build Automation). Packages: URP 17.6, uGUI 2.6, Test Framework 1.8. Text uses UnityEngine.UI.Text + bundled Baloo 2 (no TMP Essentials needed).
2. Menu **Voxel Bloom → Setup Project (URP + Android + Scene)**. This creates and
   assigns a URP asset, configures Android (`com.animatix.voxelbloom3d`, IL2CPP, ARM64, portrait, AAB,
   min API 26; **signing is never touched**) and creates `Scenes/Bootstrap/Bootstrap.unity`.
3. Press **Play** with any scene. The game boots itself (`App.Boot`, `RuntimeInitializeOnLoadMethod`).
4. Tests: **Window → General → Test Runner**, EditMode + PlayMode.
5. Automated iOS/Android builds (Editor menu, `tools/build.sh`, GitHub Actions): see `Docs/BuildAutomation.md`.

Regenerate data after changing models or rules: `node tools/build-data.mjs` (≈1 min), then
`node tools/test-rules.mjs`. Mascot contact sheets: `COLS=5 node tools/preview.mjs out.png id,id,...` (see `Docs/Mascots_*.png`).

## Gameplay rules (v2 — 4 columns, 4 launchers)
* The board has **4 columns** of numbered colour blocks; **only the top block of each column is active**.
* Tap a top block → it moves into one of the **4 launchers** above the board.
* Every launcher **fires glowing projectiles** (visible trajectory) at the model: one voxel of its colour per shot,
  counting down its number (e.g. 180 → 0). Launchers fire in parallel (2-4 at once = combo score).
* If its colour is **not visible yet** (inside the object), the launcher **waits** (pulsing red) until digging exposes it.
* A voxel is visible if one of its 6 neighbours is empty or outside; exposure is recomputed after every hit.
  Detached pieces fall (pooled Rigidbody debris) and count as destroyed. Finished colours leave the columns.
* **Fail:** all 4 launchers are waiting (jammed) — continue with **Eliminare** (empties a waiting launcher) — or no blocks left.
* Stars from score (combos); every 5th level is HARD (×2.5 coins). Hold a top block to preview the voxels it will hit.
* Levels are **multicolour objects (6-10 colours from the 12-colour palette)**; later levels have inner colour layers
  that only appear while digging, so loading them too early jams the launchers.

## Content
* **120 levels, one unique multicolour object each.** Chapter 1 "Dream Builds" = the 20 objects of the reference image
  (castle, house, sports car, cake, rocket, windmill, train, robot, excavator, ship, plane, balloon, ferris wheel, stadium,
  lighthouse, dragon, elephant, bear, dolphin, pyramid); chapters 2-6 use the other mascots and objects.
  Level colours are generated from the 12-colour reference palette (`multicolor` in `tools/build-data.mjs`); the
  Home carousel and Collection show the original-colour models.
* 124 voxel models: 100 chibi mascots (`tools/animals.mjs`, `tools/mascots.mjs`) + objects (`tools/objects.mjs`).
  Each level is solver-validated (witness replay), see `Docs/Solver.md` and `Docs/LevelReport.txt`.

## Home screen
**Premium artwork:** the logo, HUD, buttons, nav, boosters, progress bar and block style come from the reference asset sheet
(`Docs/ArtAssets.md`), with procedural fallbacks. Home is static.

Follows the premium mockup: HUD (avatar with player level, lives, coins +, settings), dimensional logo with rays, leaves and
floating cubes, a 3D diorama (floating grass island, layered voxel trees, swaying flowers, cliffs with an animated waterfall)
with the selected character (idle breathing, blink, look-around, head tilt, tail wag, happy squint; tap = heart eyes + hop + spin +
coloured sparkles; drag to rotate 360°; the same animated mascot is used in the Collection 3D preview),
a 100-mascot carousel (6 per page, arrows wrap, page indicator, locked mascots shown as silhouettes with their unlock level),
level badge, PLAY (always the latest unlocked level) and Collection / Rewards / Shop / Events. The Home selection is saved
(save v3) but never changes which model a level uses.

In gameplay the model turns slowly on a 360° turntable (pauses while you drag it or aim a booster; toggle
"Rotate model 360°" in Settings). Mascots stand on a golden-rimmed pedestal with a soft contact shadow; Home and
Collection add twinkles around the mascot. Every face has blush cheeks and a small "w" smile.

Voxels render with bevelled edges and a 2×2 "small cubes" sub-grid (`VoxelLit.shader`), so models read as built from
small cubes without increasing the number of gameplay voxels.

**Text:** UI text uses `UnityEngine.UI.Text` + the bundled Baloo 2 font (`VText`), not TextMeshPro: Unity Build
Automation does not contain the imported TMP Essential Resources, and TMP text crashed the Home screen on device.

## Architecture
```
Assets/VoxelBloom/
  Scripts/Core/            VoxelBloom.Core (noEngineReferences, unit-testable)
    Gameplay/  GameSession (rules), GreedySolver, GameStateMachine
    Voxels/    VoxelGrid, VoxelTopology        Levels/  LevelDefinition, GameConfig
    Economy/   Wallet, Lives, DailyRewards      Boosters/ BoosterType, BoosterInventory
    Save/      SaveData, SaveSerializer (versioned + migrations), SaveManager, FileSaveStorage (atomic + .bak)
    Progression/ stars, unlocks, rewards, collection     Monetization/ IRewardedAdService, RewardGate
    Utilities/ MiniJson, DeterministicRandom (bit-identical to the JS generator)
  Scripts/                 VoxelBloom.Runtime (Unity layer)
    Bootstrap/ App (entry), Services (composition root)   Levels/ DataRepository (Resources JSON)
    Voxels/    VoxelMeshBuilder (culled faces + AO, 1 draw call/model), VoxelModelView (DDA picking),
               VoxelStage (camera → RenderTexture), FragmentSystem (pooled, GPU-instanced), ThumbnailRenderer, Props
    Gameplay/  GameplayController (input → rules → animation, boosters, win/fail, autosave)
    UI/        Theme, IconPainter + SpriteFactory (all sprites procedural), UIFactory, Router, Tween, SafeArea
    UI/Screens Home, LevelSelect, LevelGrid, Gameplay, Collection, Shop, Events
    UI/Dialogs Pause, LevelComplete, LevelFailed, Boosters, Settings, DailyRewards, NoLives, Profile, Confirm, ModelPreview
    Audio/     SfxSynth (procedurally synthesised SFX + music, original), AudioService, Haptics
  Art/Resources/VoxelBloom/  Shaders/VoxelLit.shader (URP + Built-in), Fonts/Baloo2-ExtraBold.ttf (SIL OFL)
  Data/Resources/VoxelBloom/ Models/*.json, Levels/level_001..100.json, Config/game_config.json
  Editor/                    VoxelBloomSetup (menu), level validation
  Tests/EditMode, Tests/PlayMode
tools/                       Node: models, rules (reference implementation), level builder, tests, previews
```
The game uses one bootstrap scene, and screens are built at runtime by the `Router`. That replaces the
per-screen scenes suggested in the prompt and avoids hand-authored scene/prefab YAML. State machine
states follow §17 (BOOT … LEVEL_FAILED) with validated transitions.

## Monetization
No ad SDK is bundled and no credentials are invented. `AdServiceFactory` returns `NoAdsService`, so Double Reward
shows "unavailable". Define `VB_MOCK_ADS` for the explicitly labelled `MockTestAdService`. `RewardGate` never grants
rewards from test providers. To ship ads, implement `IRewardedAdService` for your SDK and return it from the factory.

## Verification
| Check | Where | Result |
|---|---|---|
| Rules (exposure, power, colour matching, queue, combos, fails, refill, wasted attacks) | `node tools/test-rules.mjs` | **11/11 pass** |
| 100 levels solvable (witness replays to a win within the move limit), distinct, difficulty trend, HARD levels harder and pay more | same | **pass** |
| C# syntax (57 files) | tree-sitter C# parser | **0 syntax errors** |
| C# compile + EditMode/PlayMode tests (≈40 tests) | Unity Test Runner | **not run (no Editor here)** |
| Android build | Unity | **not run** |
| Visual comparison with the mockup | `Docs/VisualQA.md` | **not measured** |

## Licences
Code, models, levels, procedural art and audio are original to this project. Baloo 2 font: SIL Open Font License 1.1
(`Art/Resources/VoxelBloom/Fonts/FONT_LICENSE.md`; add the full OFL.txt before shipping).
