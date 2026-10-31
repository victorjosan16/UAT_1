# Test Report

Date: 2026-10-31. Environment: Linux container, .NET SDK 8.0.133 (Ubuntu package). **No Unity Editor, no Android device.**

## 1. Executed

### 1.1 EditMode suite against the pure core: **95 / 95 passed**
```
$ cd Veliqora && dotnet test Tools/Veliqora.Core.Tests -c Release
Passed!  - Failed: 0, Passed: 95, Skipped: 0, Total: 95, Duration: 1 s - Veliqora.Core.Tests.dll (net8.0)
```
These are the same source files Unity's Test Runner uses (`Assets/_Veliqora/Tests/EditMode`, NUnit 3 attributes only).

| Fixture | Tests | Covers |
|---|---|---|
| PuzzleRulesTests | 19 | all legal/illegal pour cases, out-of-range, contiguous run, free-space limit, immutability, custom capacity, win (incl. empty / one-color), stuck, useful moves, canonical keys, color distribution |
| GameSessionTests | 16 | tap state machine, deselect/reselect, busy lock, rapid-tap conservation, multi-unit undo, undo after win blocked, extra bottle (survives undo/restart, max 1), restart, hint legality + hint-following solves, solver fallback, dead-end truthful message, shuffle restart + par, snapshot round trip, tampered snapshot rejection |
| SolverAndGeneratorTests | 13 | verified solutions, proven-unsolvable, already solved, node budget, cancellation, illegal-sequence rejection, PRNG determinism/uniformity/golden stability, deal determinism + color conservation, generator determinism + verification, rejection of unsolvable configs, verified shuffle, stuck-rate determinism |
| LevelCatalogTests | 10 | 200 contiguous levels, **all 200 replay-verified**, **all 200 re-solved independently within budget**, tiers + 10 handcrafted, 2–8 colors, tier-average ramp, no trivial or pre-completed starts, **first 40 levels reproducible from seed**, JSON round trip, star thresholds |
| EconomyTests | 10 | data-driven prices, starting grant once, purchase atomic + idempotent, insufficient funds (no state change, retryable), never negative, consume/refund idempotency, first-clear vs replay rewards, milestone, cosmetics, bounded ledger |
| SaveSystemTests | 11 | full round trip incl. in-progress session, checksum corruption + truncation, newer-schema rejection, v1→v2 migration, backup recovery, fresh start with message, write failure reported, damaged session isolated, real file storage atomic write + backup recovery, progress rules |
| RewardedAdGateTests | 10 | reward only on Completed, duplicate callbacks, cancel/fail/no-fill, timeout + late completion ignored, single in-flight, unavailable, abort on background, cooldown, NoAds never blocks |
| MiniJsonTests | 6 | round trip, malformed input |

### 1.2 Batch level validation: **200 / 200 PASS**
```
$ dotnet run -c Release --project Tools/LevelGen -- validate
Validated 200 levels: 200 PASS, 0 FAIL in 0.2s -> Docs/level_validation_report.csv
```
For each level the validator:
- checks the color distribution;
- confirms the start state is not already won;
- replays the stored solution to a win;
- runs an independent solver (`SolverOptions.Validation`: ≤400k nodes, ≤10 s).

All 200 were solved; the worst case was 124 expanded nodes and 5 ms. Per-level seed, colors, bottles, capacity, solution length, nodes, ms,
branching, stuck rate and pass/fail are in the CSV. Generation itself (`-- build`) takes ≈5 s for all 200 levels, see `Docs/level_generation_report.csv`.

### 1.3 Unity-side static compile check: **0 errors**
```
$ dotnet build Tools/UnityCompileCheck
Build succeeded.
```
- **Compiled:** every runtime, editor and PlayMode-test source file (45 files + a 2-attribute test-framework stub).
- **References:** UnityEngine 2021.3 module reference assemblies, uGUI 2020.3 and UnityEditor 2021.1, all third-party NuGet repackages.
- **Defines:** `UNITY_ANDROID; UNITY_EDITOR; ENABLE_LEGACY_INPUT_MANAGER`.
- **Not compiled:** the `ENABLE_INPUT_SYSTEM` branches (the Input System package wasn't available).
- **Real bug found:** the check caught a `Motion` name clash with `UnityEngine.Motion`, which was fixed by renaming to `MotionPrefs`.

### 1.4 Procedural art rendered offline
```
$ dotnet run -c Release --project Tools/ArtPreview -- Docs/ArtPreview
```
- **Method:** the actual art code (`PixelPainter`, `SpriteLibrary.PaintIcon`, `BackgroundArt`, `BottleShape.PaintGlass`, `LiquidGeometry`) ran on a managed shim of Unity's value types.
- **Output:** PNGs in `Docs/ArtPreview/`: valley in 3 themes, map tile, icon sheet, bottle skins, two board mock-ups and a **tilt sequence (0/30/60/90°)**.
- **Tilt result:** the tilt sequence confirms that liquid surfaces stay level and volume is preserved.
- **Issues found from these images and fixed:**
  - over-noisy mountains;
  - low glass contrast on the light background.
- **Caveat:** these previews approximate composition. They are **not** Unity screenshots.

## 2. Not executed (requires Unity Editor / device)

| Item | Status | How to run |
|---|---|---|
| EditMode suite inside Unity | pending | Window → General → Test Runner → EditMode → Run All |
| 12 PlayMode tests (home→map→level→win→next, restart, pause/resume, rapid tapping, boosters, shop, theme apply + persistence, save/reload, offline ad fallback, mock rewarded success/cancel, safe area, back button) | **written, never run** | Test Runner → PlayMode → Run All (uses an isolated temp save directory via `AppRoot.Reboot`) |
| Unity 6 compilation | pending | open project; check Console |
| Visual QA / automated screenshots | pending | Play Mode + **Veliqora → Capture Screenshot**; checklist below |
| Performance (60 FPS target, GC, startup) | **not measured** | Unity Profiler on a midrange Android device (development build) |
| Android debug APK / release AAB | **not built** | **Veliqora → Build** menu or CLI (see Docs/BUILD_AND_RELEASE.md) |

## 3. Manual screenshot / QA checklist (run in Unity, portrait 1080×1920, then 1080×2400 and 1536×2048)
1. Home: wordmark, droplet, Play pulse, level pill, stars, Map/Shop/Themes, coin badge, gear. First-launch coach bubble.
2. Map: scroll to the current node, locked node toast, completed stars, chest every 10 levels, back → Home.
3. Gameplay L1: coach mark "tap a bottle" → select lifts the bottle with glow → coach "now tap the other bottle" → pour animation (tilt, stream, fill) → win → confetti → LevelComplete.
4. LevelComplete: stars pop in, coin count-up, mock double-coins button (labelled), Next/Replay/Map.
5. Boosters at 0 inventory → buy dialog (disabled when broke), mock free booster. Shuffle confirmation text. Hint highlights two bottles.
6. Stuck panel appears when no useful moves remain.
7. Settings: all toggles persist across restart; reduced motion removes travel and confetti; color-blind symbols appear; light background shows dark glass outlines.
8. Shop/Themes: purchase feedback, insufficient-funds toast, apply theme → backgrounds update.
9. Android back on every screen, background/foreground mid-level → Pause overlay.
