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

## Rules v4: BUS -> belt -> BUS (implemented in `Core/Conveyor/ConveyorSession.cs`, mirrored by `tools/conveyor/rules.mjs`)
These rules follow the approved functional mockup ("Machetă – Funcționare") and `voxel_bloom_modificari_functional.txt`.
The belt, its art and every other system are unchanged.

1. **Belt:** one closed loop of evenly spaced slots, moving clockwise (top to the right, right side down, bottom to the left,
   left side up) by a fixed integer step per 60 Hz tick. Spacing is constant, there is no overtaking, and the motion through
   the corners is continuous. Positions are mathematical (path units), with no Rigidbody.
2. **BUS:** a tray at a station (any side: horizontal or vertical) with compartments. Compartment *i* requires a colour and
   holds a cube (right or wrong), is empty, or is reserved for an incoming cube. On screen, each compartment shows the colour
   it requires as a plate, with the cube on top. A wrong cube is drawn smaller, so the required colour shows around it.
3. **Release (BUS -> belt):** tap a BUS and its first wrong cube lifts to the station's transfer point. It enters the belt in the
   first empty slot that passes. This is only allowed while there is space on the belt. Several waiting cubes keep their order.
4. **Collect (belt -> BUS):** a cube crossing a transfer point is reserved, atomically, by the first empty compartment of that
   BUS that requires its colour. It lands 18 ticks later. One cube can be reserved only once. If no BUS can take it, it keeps
   circulating: it never moves directly from BUS to BUS and never disappears.
5. **Space counter (HUD X/8):** cubes on the belt plus cubes waiting to enter it. At 7/8 the HUD shows "Aproape plin!".
   At 8/8 it shows "Nu mai este spațiu" and further releases are blocked. Cubes keep circulating, and collections free space.
   The objective ("BUS-uri completate N/T") is shown separately. 8/8 is never a win.
6. **Completion:** when every compartment holds its colour, the BUS shows a check mark and leaves. Its station frees for the
   next queued BUS (tap a card in the queue of 7). Numbered tiles open after *n* completed BUSes. Green plus slots open with
   the "Adaugă o tavă" booster.
7. **Win:** every BUS is complete and nothing is in flight.
   **Loss (exact deadlock):** nothing is in flight, waiting or leaving; no placement or release is possible; and no belt cube
   can be collected.
8. **Supply equals demand** per colour (validated): the cubes on the belt and in all BUSes match the required compartments.
9. **Determinism and checks:** the simulation is separate from the animations (the view only interpolates). Saves carry a
   level signature and a check that no cube was lost or duplicated.

**Level 1** uses the approved Premium layout:
* a board of 17×19 and 14 stations: on the left, 7 rows with the locks "1" and "2" and two plus slots; 3 vertical BUSes on
  top; one horizontal on the right; 3 vertical at the bottom;
* 10 BUSes placed, 5 in the queue of 7, 4 colours, space 8.

Every level stores its own stations, capacities, transfer points, BUS order and contents.

## Levels (`tools/conveyor/build-levels.mjs`)
50 levels (rules v4, regenerated with BUS cargo: a share of every BUS's cubes is scrambled, a few cubes start on the belt, space 10/8/7/6 by tier) that differ **in kind**, not only in size:
* **Tiers** in a saw-tooth curve: Ușor / Normal / Greu (every 5th) / Foarte greu (every 10th). The tier is shown as a
  badge on the board and a coloured tile in the level list. Rewards: coins ×1 / ×1.2 / ×1.6 / ×2.2, plus +5 / +10 gems on the
  first clear of Greu / Foarte greu.
* **Board sizes:**
  * small loop (12×14, 49 slots, 3-compartment stations): relaxed levels;
  * medium (16×18, 65 slots);
  * large (18×22, 77 slots, up to 8 stations).
  Stations are placed automatically for each board without overlapping trays.
* **Jam pressure:** a level can only jam when it has more cubes than the belt holds, so the cube count follows the tier:
  Ușor below capacity, Normal ≈1.4×, Greu ≈1.65×, Foarte greu ≈1.95× the belt (levels 4-11 start gentler). Colour clumping
  also grows with the tier.
* **Other levers:**
  * tray sizes: 2-3 (Ușor), 3-4 (Normal), 4 (Greu and above);
  * belt speed 6 / 7;
  * visible queue 6 / 5 / 5 / 4;
  * numbered locks (2-5 trays) and green plus slots.

Each level is generated from these hand-set parameters and **validated by simulation**. It was not hand-played. Checks:
* supply equals demand per colour; no empty tray and no tray longer than the stations; at least one free station; stations
  far enough apart;
* a rollout solver finds a winning **witness** (tap tick plus queue index). The JS and C# replays must win on the same tick
  (`Tests/EditMode/ConveyorRulesTests`);
* a casual player (random visible tray after a random delay) plays 16 games; the candidate closest to the tier's target win
  rate is kept.

Per-level parameters and results: `Docs/ConveyorLevelReport.txt`. Level 1 is the integrated tutorial.

## Tests
* `node tools/conveyor/test.mjs`: loop continuity and constant spacing, matching/non-matching transfer, transfer
  timing, order of compartments, unique reservation, double tap, completion and leaving, queue advance, numbered lock and plus slot,
  deadlock, undo rollback, cube conservation at every tick of every witness, level validation.
* `Tests/EditMode/ConveyorRulesTests.cs`: the same rules in C#, witness replay of all 50 levels with identical tick counts,
  snapshot round trip, rejection of saves from another level version.
* **Not run here:** Unity compile, EditMode/PlayMode in the Editor, Android build, device performance, real Unity screenshots.

## Screens (10-panel mockup)
| Mockup | Implementation | Notes |
|---|---|---|
| Title (logo, Joacă) | `TitleScreen` | 3D landscape backdrop (diorama without pedestal, camera on the waterfall) |
| Home | `HomeScreen` | top bar (avatar, coins, gems, settings); Recompense / Evenimente / Clasament on the left; Colecție / Magazin on the right; green **Niveluri** |
| Gameplay | `ConveyorScreen` | boosters: Reîmprospătează / Amestecă culorile / Adaugă o tavă |
| Win | `ConveyorWinDialog` | violet panel, stars, cubes, reward (coins, gems on Greu / Foarte greu), Următorul / Reia |
| Niveluri | `MapScreen` | winding golden path over a meadow with a river; 8 levels per page, stars, locks, page arrows and dots |
| Cum se joacă? | `HowToPlayDialog` | shown before level 1 the first time; also in the pause menu |
| Boostere | `BoostersInfoDialog` | buy one with coins; opens when a booster is at 0 |
| Magazin | `StoreScreen` | tabs: Boostere (×3 for 500 / 750 / 1000 coins), Monede (for gems), Gems, Pachete (for gems) |
| Evenimente | `EventsHubScreen` | Provocarea Zilnică (5 levels/day), Misiune Specială (200 cubes, repeats), Sezonul Curcubeu (15 stars in a 7-day season, countdown) |
| Setări | `SettingsScreen` / `SettingsVioletDialog` | Sunet, Muzică, Vibrații, Limbă (Română), Salvare în cloud, Despre joc |
| Clasament (button) | `LeaderboardScreen` | the player's own records |

**Not configured, so shown honestly instead of faked:**
* real-money gem packs: the button explains that payments are not configured;
* cloud sync: the preference is saved, and a message says Google Play Games is needed;
* an online leaderboard: it needs a server.

The painted backgrounds of the mockup are replaced by the 3D diorama (Title and Home) and a procedural map (Niveluri).
