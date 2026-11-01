# Voxel Bloom 3D (Unity, Android)

Original 3D voxel puzzle game built from the visual spec in `Docs/reference/mockup.png` and
`Docs/reference/MasterPrompt.txt`. Destroy a rotating voxel model with numbered colour blocks:
each block attacks the **exposed** voxels of its own colour, up to its power (2, 4, 8, 16, 32, 64).

> **Status: honest summary.** The game logic, the 34 models and the 100 levels are generated and
> **tested** here (Node). All C# passes a syntax check (tree-sitter). It has **not been compiled or run
> in Unity yet**, because no Unity Editor was available in this environment. Expect a first round of
> compile fixes in the Editor. See "Verification" below.

## Quick start
1. Open this folder (`voxel-bloom/`) with **Unity 6.6** (6000.6.7f1, the version used by Unity Build Automation). Packages: URP 17.6, uGUI 2.6, Test Framework 1.8. Text uses UnityEngine.UI.Text + bundled Fredoka (no TMP Essentials needed).
2. Menu **Voxel Bloom → Setup Project (URP + Android + Scene)**. This creates and
   assigns a URP asset, configures Android (`com.animatix.voxelbloom3d`, IL2CPP, ARM64, portrait, AAB,
   min API 26; **signing is never touched**) and creates `Scenes/Bootstrap/Bootstrap.unity`.
3. Press **Play** with any scene. The game boots itself (`App.Boot`, `RuntimeInitializeOnLoadMethod`).
4. Tests: **Window → General → Test Runner**, EditMode + PlayMode.
5. Automated iOS/Android builds (Editor menu, `tools/build.sh`, GitHub Actions): see `Docs/BuildAutomation.md`.

Regenerate data after changing models or rules: `node tools/build-data.mjs` (≈1 min), then
`node tools/test-rules.mjs`. Mascot contact sheets: `COLS=5 node tools/preview.mjs out.png id,id,...` (see `Docs/Mascots_*.png`).

## Gameplay rules
* Tap a block on the 5×4 board → it flies into the 5-slot FIFO attack queue and the slot refills from the level deck.
* The first queued attack with a reachable target fires: it removes up to *power* exposed voxels of its colour.
  A voxel is exposed if one of its 6 neighbours is empty or outside the model. Exposure is recomputed after every destruction.
  Fewer targets → only those are removed and the attack is consumed. Attacks never hit other colours.
* Attacks whose colour is not reachable yet **wait** in the queue. Queue full with nothing able to fire = fail.
* Consecutive full-power attacks → **COMBO x2…x5** (score multiplier, effects, coins).
* **No floating parts:** after every destruction the largest connected piece stays (it rests on the pedestal) together
  with any piece touching the floor; every other detached piece falls (pooled Rigidbody debris) and counts as destroyed.
  Same rule in `tools/rules.mjs` and `GameSession.Collapse`, so solver witnesses still replay exactly.
* **See where the colour goes:** hold a block to highlight on the model exactly which voxels it would remove; when an
  attack fires, its targets pulse and coloured cubes fly from the queue slot to them. Blocks with no reachable voxel are dimmed.
* Fail: queue jammed, move limit reached, or no blocks left. Stars: moves ≤ 3★ / 2★ thresholds.
* **HARD levels: every 5th level** (5, 10, …, 100). Tighter move limit, **x2.5 coins**, red HARD badge, own
  Events track. Coins buy boosters, including **Eliminare** (150): it removes a stuck attack from the queue,
  and the fail dialog offers "continue with Eliminare" when the queue is jammed.

Boosters: Hammer (300, smash 12 voxels of any colour where you tap the model), Shuffle (200, mixes board + deck and
keeps a playable move), Color Bomb (500, 48 voxels of one colour), Eliminare (150). Coins are deducted only together
with the grant and can never go negative.

## Content
* **100 original voxel mascots, one per level** (level N unlocks mascot N), following `Docs/reference` (100-mascot sheet):
  1-10 detailed chibi heroes (`tools/animals.mjs`), 11-100 built from five parametric chibi templates — quadruped,
  bird, fish, reptile/amphibian, bug — plus specials (jellyfish, octopus, crab, seahorse, snake, bat, snail) in
  `tools/mascots.mjs`. Every mascot is one connected piece, has big 2×3 eyes and an idle-animation profile.
  Contact sheets: `Docs/Mascots_001-025.png` … `Docs/Mascots_076-100.png`, front views of 1-10 in `Docs/AnimalsFront.png`.
* **5 chapters = mascot groups:** Happy Friends (1-20), Wild Safari (21-40), Sky & Sea (41-60), Tiny Wonders (61-80),
  Wild North (81-100). Collection tabs are the same groups. From level 26 every 3rd level adds a hidden core, every 5th
  level is HARD. Each level is solver-validated, see `Docs/Solver.md` and `Docs/LevelReport.txt`.

## Home screen
Follows the premium mockup: HUD (avatar with player level, lives, coins +, settings), dimensional logo with rays, leaves and
floating cubes, a 3D diorama (floating grass island, layered voxel trees, swaying flowers, cliffs with an animated waterfall)
with the selected character (idle breathing, blink, look-around, head tilt, tail wag, happy squint; tap = heart eyes + hop + spin +
coloured sparkles; drag to rotate 360°; the same animated mascot is used in the Collection 3D preview),
a 100-mascot carousel (6 per page, arrows wrap, page indicator, locked mascots shown as silhouettes with their unlock level),
level badge, PLAY (always the latest unlocked level) and Collection / Rewards / Shop / Events. The Home selection is saved
(save v3) but never changes which model a level uses.

Voxels render with bevelled edges and a 2×2 "small cubes" sub-grid (`VoxelLit.shader`), so models read as built from
small cubes without increasing the number of gameplay voxels.

**Text:** UI text uses `UnityEngine.UI.Text` + the bundled Fredoka font (`VText`), not TextMeshPro: Unity Build
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
  Art/Resources/VoxelBloom/  Shaders/VoxelLit.shader (URP + Built-in), Fonts/Fredoka-SemiBold.ttf (SIL OFL)
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
| C# syntax (48 files) | tree-sitter C# parser | **0 syntax errors** |
| C# compile + EditMode/PlayMode tests (≈40 tests) | Unity Test Runner | **not run (no Editor here)** |
| Android build | Unity | **not run** |
| Visual comparison with the mockup | `Docs/VisualQA.md` | **not measured** |

## Licences
Code, models, levels, procedural art and audio are original to this project. Fredoka font: SIL Open Font License 1.1
(`Art/Resources/VoxelBloom/Fonts/FONT_LICENSE.md`; add the full OFL.txt before shipping).
