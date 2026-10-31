# VELIQORA — Design & Technical Specification

## 1. Product
- **Brand:** VELIQORA. **Subtitle:** Color Sorting Adventure. **Store working title:** Veliqora: Color Sort Puzzle.
- **Genre:** casual liquid-sorting puzzle (original implementation; the reference board is visual direction only, and none of its pixels ship).
- **Platform:** Android, portrait 9:16 with safe-area support (cutouts, gesture bars). The 60 FPS target is unverified until profiled on device.
- **Engine:** Unity 6 LTS, **Built-in render pipeline + uGUI**. This was the least risky choice. Everything is screen-space UI with custom mesh graphics, so it
  behaves the same in Built-in or URP projects, needs no render-pipeline asset, and has no shader dependencies.

## 2. Rules (pure core: `Veliqora.Core.Puzzle`)
- A bottle is an ordered list of ColorIds, bottom → top. Capacity is 4 by default and configurable per level (levels 166–180 use 5).
- A pour `from → to` is legal iff: `from ≠ to`, the source is non-empty, the destination has space, and the destination is empty or its top color equals the source's top color.
- Amount moved = the contiguous top run of the source color, limited by destination free space. Moves are atomic and return a `MoveResult` with immutable before/after states.
- **Win:** every bottle is empty, or full and single-colored. All-empty boards and one-color puzzles are handled.
- **Stuck:** the board is not won and no *useful* move exists (useful excludes relabels such as pouring a monochrome bottle into an empty one, and moves out of completed bottles).
- **Tap model** (`GameSession.Tap`):
  - tap a non-empty bottle to select it;
  - tap it again to deselect;
  - tap another bottle to pour if legal;
  - tapping a non-empty bottle with a mismatched top switches the selection (with an invalid cue);
  - tapping an empty or full target gives feedback and clears the selection;
  - all taps are ignored while busy (animating, hint search, shuffle generation) or after the win.

## 3. Architecture
```
Veliqora.Core (no UnityEngine)         Veliqora.Runtime (Unity)                     Veliqora.Editor
 ├─ Puzzle: BottleState, GameState,     ├─ Infrastructure: AppRoot (composition      ├─ VeliqoraSetup (menu)
 │  Move, MoveResult, PuzzleRules       │   root), SceneNavigator, SceneEntry,       ├─ VeliqoraValidator
 ├─ Solver: PuzzleSolver (weighted A*)  │   HapticsService                           ├─ VeliqoraBuild (APK/AAB, CLI)
 ├─ Levels: LevelDefinition, Catalog,   ├─ Gameplay: GameplayController              └─ VeliqoraScreenshots
 │  Repository, Generator, Builder,     ├─ Presentation: BoardView, BottleView,
 │  HandcraftedLevels, StarRating,      │   LiquidGraphic, LiquidGeometry, Theme*,
 │  DifficultyAnalyzer                  │   BackgroundArt, LiquidPalette
 ├─ Session: GameSession, Snapshot      ├─ UI: UIStyle (tokens), UIFactory, Overlays,
 ├─ Economy: Config, Service, State     │   Tween/MotionPrefs, SpriteLibrary, PixelPainter,
 ├─ Save: SaveData, Serializer,         │   Screens/*, Widgets/*
 │  Migrator, SaveService, storages     ├─ Audio: AudioService, ProceduralAudio
 ├─ Ads: IAdsService, RewardedAdGate    ├─ Settings: SettingsService
 ├─ Json: MiniJson; Random: PRNG        └─ Ads: AdsConfig, AdsFactory, MockAdsService
```
- **Model first:** input → `GameSession` validates and commits → `BoardView` animates the already-committed result. The view never decides legality, so
  model and view can't desync. If an animation is interrupted (rebuild, scene unload), its completion callback still runs exactly once (`BoardView._pendingDone`).
- **Composition root:** `AppRoot` is the only global. It is created via `RuntimeInitializeOnLoadMethod(BeforeSceneLoad)`, survives scene loads, and builds `AppServices`
  (save, content, economy, settings, themes, audio, haptics, ads, navigator), which is passed explicitly to screens. The project uses no `FindObjectOfType`.
- **Navigation:** one scene per screen, with a 0.2 s fade. If a scene isn't in Build Settings, the navigator creates an empty runtime scene, so a fresh checkout is playable.
  Settings and Pause are overlays on a persistent canvas.
- **Back button / lifecycle:**
  - the top overlay closes first;
  - Gameplay → Pause; Map / LevelComplete → Home / Map; Shop / Customization → previous screen; Home → quit confirmation;
  - on app pause the game force-saves, pauses music, and resumes into the Pause overlay mid-level.
- **Threading:** hint search and shuffle generation run on `Task.Run` over immutable states, with input locked and cancellation on screen destroy.

## 4. Liquid rendering
- `LiquidGraphic` (a `MaskableGraphic`) builds one mesh per bottle: no masks, no extra materials, one batchable draw call per bottle.
- Layer boundaries are lines perpendicular to **world-up**, positioned by **area** inside the convex interior polygon (binary search + Sutherland–Hodgman clipping).
  Liquid stays level and volume is preserved while the bottle tilts. Verified visually at 0°/30°/60°/90° (`Docs/ArtPreview/tilt_sequence.png`).
- Contiguous same-color units merge into one band, so there are no seams. The top band gets a lighter meniscus strip, and selection, pour and landing give a decaying slosh (surface tilt).
- **Pour:**
  1. lift and travel while tilting (0.28 s);
  2. pour (0.2 s + 0.12 s per unit): the mouth stays fixed over the target, the bottle keeps tipping, a stream draws to the rising surface, source drains and destination fills;
  3. return (0.26 s).
  
  Durations scale with *Animation speed*. **Reduced motion** replaces this with a 0.15 s in-place level crossfade.
- Bottle skins (`classic, slim, flask, crystal`) define glass geometry and the matching interior polygon, so liquid can never render outside the glass.

## 5. Levels and difficulty
- **200 levels** live in `Resources/Veliqora/Levels/levels.json` (content version 1, generator version 1, base seed `0x56454C49514F5241`).
- **1–10 handcrafted onboarding**, each with a contextual tip:
  - tap-to-pour, same-color stacking, empty bottles, planning, undo, partial pours, boosters, hints, five colors, stars.
- **11–200 generated** offline by `LevelCatalogBuilder`:
  1. deterministic deal from `Combine(baseSeed, level, candidate)`;
  2. structural filters (no pre-completed bottles, limited adjacent pairs);
  3. **solver acceptance**: must be `Solved` within budget and replay-verified, otherwise rejected;
  4. minimum solution length per segment;
  5. several candidates per level, ordered by a difficulty score (solution length + branching + search effort + **stuck rate** from naive random playouts), picked along a sawtooth ramp;
  6. the chosen level is re-solved with plain A* for a tighter benchmark.
- **New constraints arrive gradually:**
  - one spare bottle: first at L92, alternating in 91–110, 141–150 and 181–200;
  - tall capacity-5 bottles: 166–180.
- **Measured curve** (from `Docs/level_generation_report.csv`):

| Tier | Levels | Colors | Benchmark moves (avg / min–max) | Naive stuck rate (avg) |
|---|---|---|---|---|
| Tutorial | 1–5 | 1–3 | 5.6 / 1–10 | 0.00 |
| Beginner | 6–25 | 3–5 | 13.6 / 7–18 | 0.03 |
| Intermediate | 26–75 | 5–7 | 19.4 / 15–26 | 0.29 |
| Advanced | 76–150 | 4–8 (1-spare variants use fewer colors) | 22.8 / 10–29 | 0.61 |
| Expert | 151–200 | 6–8 | 26.3 / 17–32 | 0.67 |

- **Solvability proof:** every level stores an explicit solution that tests replay to a won state. An independent solver run also confirms all 200.
  Solutions are *benchmarks*, not proven optimal, and uniqueness is not claimed.
- **Stars:**
  - ★★★ if moves ≤ par + max(2, ⌈25% par⌉);
  - ★★ if moves ≤ par + max(5, ⌈60% par⌉);
  - ★ on any completion.
  
  Undone moves don't count, and the HUD shows the ★★★ target. After a Shuffle, par becomes the shuffled layout's verified solution length.
- **Optional challenge:** the ★★★ move target shown on every level.

## 6. Boosters and economy (`economy.json`)
- **Starting grant (once):** 200 coins, Undo ×3, Add bottle ×1, Shuffle ×1, Hint ×2.
- **Level reward:**
  - first clear: 20 + 10 per star;
  - every 10th level: +100 coins, Undo ×2 and Hint ×1 (milestone chest);
  - replay: 5 coins + 10 per newly earned star.
- **Prices:** Undo ×3 = 100, Add bottle = 200, Shuffle = 150, Hint ×2 = 120. Cosmetics cost 300–1200, and the defaults are free.
- **Transactions:** every mutation has an id, and a repeated id is a no-op (`Duplicate`). Balances never go negative.
  Failed transactions aren't recorded, so they can be retried. The ledger is bounded (512), and the starting grant is a permanent flag.
- **Booster rules:**
  - **Undo:** reverts the exact pre-move snapshot (multi-unit safe) and consumes one only when something is undone.
  - **Add bottle:** at most one per attempt, persisted in the save, survives undo and restart.
  - **Shuffle:** confirmation first. Generates a new layout with the same colors, bottle count and capacity, verified by the solver within 600 ms / 40k nodes.
    If none is found, it is refunded and the puzzle is left unchanged, with a message.
  - **Hint:** stored solution while the player follows it, otherwise a bounded solver search from the current state. It is charged only if a move is found.
    If not, it shows a truthful message ("No solution from here…" or "Couldn't find a hint quickly…").
- No real-money purchases are implemented.

## 7. Save system
- **Location:** `persistentDataPath/veliqora_save.json`, plus `veliqora_save.bak.json`.
- **Envelope:** `{format, schemaVersion, checksum (FNV-1a 64 of canonical payload), payload}`.
- **Atomic write:** temp file + flush → `File.Replace` with backup (copy/delete/move fallback).
- **Load:** primary → backup → fresh save with a user-facing message. Newer schemas are rejected rather than misread, and a damaged in-progress session is dropped while the rest of the save is kept.
- **Contents:** progress (highest unlocked, current, best stars, chests), wallet + inventory + owned cosmetics + ledger, settings, cosmetic selection,
  seen tutorials, the in-progress session (initial/current boards, last 30 undo snapshots, extra bottle, shuffles, par, solution), and content version.
- **Autosave:** after every committed move, booster, purchase and setting change, throttled to 0.75 s. It is forced on level finish, navigation and app pause.
- **Migration:** schema v1 (prototype flat layout) → v2, covered by tests. A content-version change drops only the in-progress session.

## 8. Ads (safe fallbacks)
- `IAdsService` has these implementations:
  - `MockAdsService`: editor and development builds only, labelled **"MOCK AD — TEST ONLY"**, with a tester-chosen outcome;
  - `NoAdsService`;
  - a `LevelPlayAdsAdapter` placeholder compiled only with `VELIQORA_LEVELPLAY`. It is intentionally unimplemented and reports unavailable, so no SDK API names are invented.
- Mock mode is refused in non-development player builds unless `allowMockInReleaseBuilds` is set.
- `RewardedAdGate`:
  - one request at a time; the reward is granted **only** on `Completed`;
  - duplicate or late callbacks are ignored;
  - timeout runs on a foreground-only clock;
  - cooldown applies; abort means no reward.
- Rewards are idempotent through economy transaction ids.
- **Placements:** double level coins, free booster (Shop / out-of-booster dialog). An optional banner is allowed only on Home, Map and Shop, and is off by default.
  There are no interstitials.
- `ads.json` holds clearly named placeholders: `REPLACE_WITH_ANDROID_APP_KEY`, `REPLACE_WITH_REWARDED_AD_UNIT_ID`, `REPLACE_WITH_BANNER_AD_UNIT_ID`.

## 9. Art direction, design system and asset replacement
- **Palette tokens (`UIStyle`):** sky blue `#7EC8F2`, ivory `#FFF8EC`, mint `#4FD1A5`, coral `#FF7A6B`, violet `#8C6CF0`, sunshine `#FFC93C`, navy `#1D2A55`.
  Six button variants each have edge, gloss and press-scale states. Type scale: 132/72/56/42/36/30 px at 1080×1920. Spacing: 12/24/36/56. Radii: 24/40/64. Minimum touch target: 132 px.
  Motion: 0.15/0.25/0.4 s.
- **Liquid colors (8 used, 12 defined):** blue, pink, green, yellow, purple, orange, cyan, red. Optional color-blind symbols (circle, triangle, square, diamond, star, plus, ring, X…) sit on every unit.
- **Screens:**
  - Home: valley background, gradient wordmark with droplet motif, Play/Continue, stars, Map/Shop/Themes;
  - Map: seamless river tile, nodes with stars, current pulse, locks, chests every 10;
  - Gameplay: navy gradient or light ivory option;
  - LevelComplete: three animated stars, coin count-up, green Next CTA, restrained confetti;
  - Shop: booster cards;
  - Customization: Themes/Bottles tabs, live preview, lock/price/applied states.
- **Themes:** Classic Valley (free), Sunset Shores, Blossom Garden, Starry Night, Winter Glow, Galaxy Bloom.
- **Placeholder art:** all of it is generated at runtime by `PixelPainter` (SDF rasterizer). Previews are in `Docs/ArtPreview/`. The logo is a gradient-outlined wordmark plus droplet.
  Icons from Setup are named `*_PLACEHOLDER.png`.
- **Replacing art (no code changes):**
  - icons → `Resources/Veliqora/Art/Icons/<IconName>.png` (Sprite), names from the `Icon` enum;
  - backgrounds → `Resources/Veliqora/Art/Backgrounds/<themeId>.png` (Texture2D);
  - font → `Resources/Veliqora/Fonts/Primary.ttf`;
  - audio → `Resources/Veliqora/Audio/{tap,select,invalid,pour,complete,win,coin,music}`;
  - mixer → `Resources/Veliqora/Audio/VeliqoraMixer` with groups `Music/UI/Pour/Reward`.
  
  Bottle glass is code-defined, to keep it consistent with the liquid polygon.
- **Authored art (from the supplied sheets; see `Tools/ArtImport/`):**

  | Asset | Used for |
  |---|---|
  | `screen_home` | Home background, Classic theme (other themes show their own illustration) |
  | `screen_shop` | Shop background |
  | `screen_themes` | Customization background |
  | `screen_complete` | Level Complete background |
  | `theme_valley`, `theme_sunset`, `theme_night`, `theme_garden`, `theme_winter`, `theme_desert` | Gameplay backgrounds (navy veil 38% + edge gradients for readability), theme thumbnails and previews |
  | `Buttons/{Back,Pause,Restart,Undo,Bulb,BottlePlus,Shuffle}` | Replace circle + glyph icon buttons |
  | `pill_green` (derived from the blue pill), `pill_purple` | Primary and Accent buttons (9-slice) |
  | `sign_wood` | Every title banner |
  | `Effects/fx_crown_burst`, `Effects/fx_star_burst` | Level Complete (3 stars / fewer stars) |
  | `Bottles/glass_{milk,cork,cone,cap}` | New skins *Milk Bottle, Cork Bottle, Cone Flask, Vintage Vial*. Glass is drawn behind the liquid plus a 38% copy in front; the convex interior comes from `bottle_shapes.json`. |

  - New theme: **Desert Oasis** (1100 coins).
  - **Imported but not used yet:**
    - `screen_map`: the scrolling map needs a seamless vertical tile, so it keeps the procedural tile;
    - `screen_settings` and `screen_events`: there is no Events feature yet;
    - heart and gem icons, the obstacle blocks (ice, wood, stone, metal, chained), special liquids (galaxy, gold), pour/splash illustrations, leaves, flowers and bubbles.
  - **Not imported:** non-convex bottles (heart, star, hourglass, amphora), because the liquid renderer needs a convex interior, and the filled bottle showcases.
- **TextMeshPro:** not required. The legacy uGUI `Text` with the built-in font is used to avoid an Essentials import step. A move to TMP would be confined to `UIFactory.Text`.

## 9b. Dark-fantasy pass
- **Backgrounds:** `DarkScenes` paints 12 original 540×960 scenes. Each takes about 120 ms in the offline renderer, runs once per theme and is cached.
  Glows use additive light and silhouettes use fBm ridges; a vignette keeps the board area calm.
  Themes `dark_*` in `economy.json` are free. `Resources/Veliqora/Art/Backgrounds/<themeId>.png` overrides any scene with authored art.
- **Screen treatment:**
  - Gameplay: the selected scene under a 22% veil plus edge gradients, a neon aura per bottle and ambient motes.
  - Home, Shop, Themes and Level Complete use the dark scene when a dark theme is active, and the illustrated screens otherwise.
- **UI tokens:** dark glass surfaces (`#1D1940` panel, `#2A2456` cards), light text (`#F1ECFF`), and an `InkOnLight` color for the optional light gameplay background.
- **Liquid palette:** neon set blue, pink, green, yellow, violet, orange, cyan, red, lime, amber, pearl, moonstone. The minimum pairwise ΔE (CIE76) is 36.9, and color-blind symbols remain available.
- **Particles (`UiParticles`):**
  - pooled (no allocations after warm-up) with one shared glow sprite;
  - splash droplets every 0.11 s where the stream lands, bubbles after each pour;
  - 14-spark burst on bottle completion, sparkles on win, ambient motes;
  - all disabled in reduced motion.
- **Master tier (levels 201–300):** 9–12 colors and up to 15 bottles, laid out in 3 rows. Benchmark solutions run up to 43 moves.
  Levels 1–200 are byte-identical to the previous catalog.
- **Committed scenes:** `Tools/write_scenes.py` writes 7 `.unity` files, each with a `SceneEntry` marker referencing a stable script GUID, plus `EditorBuildSettings.asset`.
  `ProjectFilesTests` guards them against drift.

## 10. Accessibility
- Color-blind symbols, a light gameplay background, reduced motion (decorative motion off, functional motion shortened) and an animation-speed setting.
- Large touch targets, plus text alongside color for state (ON/OFF labels, "Owned: n", price text).

## 11. Audio and haptics
- Synthesized placeholder SFX (tap, select, invalid, pour with bubbles, bottle complete chime, win arpeggio, coin) and a streaming ambient music loop generated on the audio thread.
- Four channels with separate volumes and toggles.
- Haptics use the Android Vibrator with millisecond pulses (12/24/40 ms) and can be toggled.
