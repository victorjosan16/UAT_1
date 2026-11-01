# Voxel Bloom 3D: conveyor sort (rules v3), audit and migration

## Phase A: audit (before the rebuild)
* Unity 6.6 (6000.6.7f1, Unity Build Automation), URP 17.6, uGUI 2.6, Test Framework 1.8. Android package
  `com.animatix.voxelbloom3d`. **Signing and build settings were not touched.**
* One bootstrap scene; all screens are built at runtime by `Router` (no prefabs or scene YAML). That stays.
* The backup of the shooter version is the git history: commit `54f0c80` on `ccr-1b2ddcf1-u2muz2` is the last shooter build.

| System | Decision |
|---|---|
| Projectile launchers, numbered ammo blocks, voxel destruction, 4-column board (`GameSession`, `GreedySolver`, `GameplayController`, `GameplayScreen`, shooter dialogs, `Levels/level_*.json`) | **Replaced.** No entry point leads to them any more (Home, level list, events and dialogs start conveyor levels). The files stay for now so nothing referenced breaks; they are deleted in the cleanup phase after a compiled Unity run confirms no references. |
| Save (`SaveData` v4 + atomic file store), coins, gems, lives, daily rewards, settings, audio (`SfxSynth`), haptics, rewarded-ad abstraction (`IRewardedAdService`, no SDK bundled), Router/UI kit, premium art (`Art/UI`), Home 3D diorama | **Reused** |
| Boosters | Inventory/purchase reused. New types `Undo` (4) and `ExtraStation` (5); `Shuffle` (1) is reused with conveyor meaning. Hammer/ColorBomb/Eliminate counts stay in the save but are unused |
| Progress | **Explicit reset policy:** the shooter levels are a different game, so conveyor progress starts at level 1 (`ConveyorUnlocked`, `ConveyorStars`, `ConveyorActive`, save v4). Coins, gems, lives, boosters, settings and the collection carry over. The shooter's unfinished-level snapshot is dropped by the v3→v4 migration |

## References and uncertain points
* The text prompt, the gameplay screenshot (`Docs/reference/conveyor-gameplay-ref.png`) and the six-panel mockup
  (`Docs/reference/conveyor-mockup-v2.png`) were used. **The video 13224.mp4 was not attached**, so it could not be
  audited frame by frame. The rules below are the prompt's baseline. Where the prompt leaves a choice open, the decision is
  written down here.
* A static screenshot cannot show the order of motion. Chosen: one direction, clockwise (right along the top),
  matching the chevrons on the top and left side of the reference.
* The screenshot shows more cubes than compartments. Chosen: **supply equals demand per colour** (validated), so every
  cube has a destination and a level never ends with leftovers.

## Rules (implemented in `Core/Conveyor/ConveyorSession.cs`, mirrored by `tools/conveyor/rules.mjs`)
1. **Loop:** one closed rounded rectangle with `slots` evenly spaced belt slots (pitch P = 100 path units). All slots move
   together by `speed` units per 60 Hz tick, in integer math. Constant spacing, no overtaking, continuous wrap-around
   through all four corners. A slot holds one cube or is empty.
2. **Feeder:** cubes that do not fit on the belt wait in the hopper (shown as `+N` on the chute at the top-left corner).
   When an empty slot passes the feeder, the next hopper cube enters it.
3. **Stations and trays:** a station is a fixed pickup point on the inner side of the loop. Its tray has up to 4
   compartments, which are filled in order (compartment 0 is next to the belt). When a slot crosses the pickup point
   and its cube's colour equals the tray's next required compartment, the cube is **reserved** for that compartment
   and leaves the belt. It lands 18 ticks later (a visible flight); only then does it count as delivered. Non-matching cubes keep circulating.
   One cube can only be reserved once, and stations are evaluated in a fixed order.
4. **Completion:** when all compartments are filled, the objective goes +1 (the HUD shows `completed/total` trays, never
   collected cubes). The tray leaves the station in 24 ticks, and then the station is free.
5. **Player:** taps one of the first `visible` queued trays. It flies to the **first free, unlocked station** (in
   station order) with enough capacity. If no station is free, nothing is consumed (a toast explains). A double tap cannot
   place a tray twice: placement is a single atomic `Place()` call.
6. **Locks (per level, optional):** a numbered grey tile is a station that opens after *n* completed trays (the
   tile shows how many are still needed). A green plus slot is a station that opens with the **Extra station** booster
   (tap the slot or the booster).
7. **Win:** every tray is completed (transfers in flight have landed). **Loss (deadlock, exact and proven):** nothing is in flight or
   leaving, no tray can be placed, no cube can enter (the hopper is empty or the belt is full), and no belt cube is the next
   colour of any station. Because supply equals demand, this happens only when the belt is full of cubes that no
   station needs while the needed ones wait in the hopper. That jam is the puzzle. The loss dialog offers Undo /
   Extra station (paid with coins if the inventory is empty), Retry (one life) or Quit. The game never softlocks.
8. **Boosters** (never consumed on a failed action):
   * **Undo:** restores the full state from just before the last tray placement (snapshot stack).
   * **Shuffle:** reorders the queued trays with a seeded random order. A new order is kept only if the greedy player
     can still win from the current state (up to 20 tries). Otherwise nothing changes and nothing is consumed.
   * **Extra station:** opens the first green plus slot. Not available on levels without one.
9. **Stars:** 3 without boosters, 2 with one or two, 1 otherwise. Rewards: level coins (+10 per extra star); replays pay 25%;
   every 5th level gives +10 gems on the first clear.
10. **Saving:** the unfinished level is saved as a snapshot (`ConveyorActive`) on every placement, on pause and on quit.
    It carries a level signature and a cube-conservation check, so it is never restored onto different level data.

## Levels (`tools/conveyor/build-levels.mjs`)
20 levels are generated from **hand-set parameters** (colours, trays, tray sizes, stations, visible queue, cube clumping,
locks) and then **validated by simulation**. They were not hand-played. For each level:
* supply equals demand per colour, no empty or over-long trays, at least one free station, stations far enough apart;
* a rollout solver finds a winning **witness** (tap tick plus queue index). The JS and C# replays must win on the same tick
  (`Tests/EditMode/ConveyorRulesTests`), which proves the two rule implementations agree;
* a casual player (random visible tray after a random delay) plays 16 games; the candidate closest to the target win
  rate is kept. Results: `Docs/ConveyorLevelReport.txt`.
Level 1 is the integrated tutorial: an arrow points at the best tray, with a two-line hint, until two trays are placed.
The generator scales to more levels, but 100+ levels are **not** shipped yet.

## Tests
* `node tools/conveyor/test.mjs`: loop continuity and constant spacing, matching/non-matching transfer, transfer
  timing, order of compartments, unique reservation, double tap, completion and leaving, queue advance, numbered lock and plus slot,
  deadlock, undo rollback, cube conservation at every tick of every witness, level validation.
* `Tests/EditMode/ConveyorRulesTests.cs`: the same rules in C#, witness replay of all levels with identical tick counts,
  snapshot round trip, rejection of saves from another level version.
* **Not run here:** Unity compile, EditMode/PlayMode in the Editor, Android build, device performance, real Unity screenshots.
