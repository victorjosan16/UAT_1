# Implementation Status

Legend:
- ✅ **Verified here**: executed or measured in this environment (.NET 8, Linux).
- 🟡 **Implemented, pending Unity**: written and compiles against Unity reference assemblies, but not run in the Unity Editor or on a device.
- ⬜ **Not done / placeholder**.

Environment facts: no Unity Editor, Unity license, Android SDK or device was available. Nothing here claims a Unity build, a PlayMode run,
an APK/AAB, or on-device performance.

## Phase A: audit, docs, folders, core rules + tests
- ✅ **Repo audit:** the repo held an unrelated image-processing service (`backend/`, `frontend/`). It was left untouched, and the Unity project lives in `Veliqora/`.
- ✅ **Core domain:** immutable `BottleState`/`GameState`, `Move`/`MoveResult`, `PuzzleRules` (legal/illegal, contiguous run, capacity, win, stuck, useful moves).
- ✅ **Infrastructure:** deterministic PRNG with golden-stability test and dependency-free JSON.
- ✅ **Assemblies:** `Veliqora.Core` asmdef with `noEngineReferences`. Separate Runtime, Editor, EditMode-test and PlayMode-test asmdefs.

## Phase B: board, input, pouring, win, animations, tutorial levels
- ✅ **Session logic:** `GameSession` (tap state machine, busy lock, undo, restart, extra bottle, shuffle apply, hints, snapshots).
- ✅ **Tutorial levels:** 10 handcrafted onboarding levels, solver-verified.
- 🟡 **Rendering:** `BoardView` / `BottleView` / `LiquidGraphic` (responsive rows, selection lift, shake, slosh, pour with stream, undo flow-back, add-bottle pop, celebrate hop, reduced-motion path).
- ✅ **Liquid geometry:** area-preserving tilt, verified by the offline renderer (`Docs/ArtPreview/tilt_sequence.png`).
- 🟡 **Contextual tutorial:** coach marks with pointing hand, never blocking input.

## Phase C: screens and navigation
- 🟡 **Screens:** Bootstrap (auto composition root), Home, LevelMap (virtualized nodes, chests), Gameplay, LevelComplete, Shop, Customization.
- 🟡 **Overlays:** Settings, Pause, Confirm, Message, Toast.
- 🟡 **Platform handling:** Android back-button handling, pause/resume, safe area, adaptive canvas scaling.
- 🟡 **Scenes:** created by **Veliqora → Setup Project**. Runtime fallback scenes cover the case where Setup hasn't run.

## Phase D: generator, solver, 200 levels, hints, stars
- ✅ **Solver:** weighted A* with canonicalization, symmetric-empty and pointless-move pruning, node/time budgets and cancellation.
- ✅ **Generator:** verified generator plus curve builder with stuck-rate difficulty metric.
- ✅ **Determinism:** the first 40 levels are rebuilt from the seed in tests and match exactly.
- ✅ **Batch validation:** 200/200 PASS (`Docs/level_validation_report.csv`).
- ✅ **Hints:** always legal; following hints solves a level; a truthful fallback covers dead ends.
- ✅ **Stars:** transparent star thresholds.

## Phase E: economy, boosters, customization, save, audio
- ✅ **Economy:** data-driven config, idempotent transactions, no negative balances, replay-farming limits, milestone chests, refunds.
- ✅ **Save:** versioned, checksummed, atomic temp-file write with backup, recovery, v1→v2 migration, newer-schema rejection, damaged-session isolation.
- 🟡 **Booster UI:** counts, buy-when-empty dialog, rewarded free booster, stuck panel.
- 🟡 **Customization:** 6 themes and 4 bottle skins with preview, unlock and apply; selection persisted (logic ✅ in tests).
- 🟡 **Audio:** procedural SFX and streaming music, four channels, optional mixer routing, haptics.
- ⬜ **AudioMixer asset:** none shipped, because Unity offers no public API to author one from code. Routing activates automatically if you add one (see DESIGN_SPEC §9).

## Phase F: ads and privacy placeholders
- ✅ **Reward gating:** `IAdsService` and `RewardedAdGate`, covering completed-only rewards, duplicate callbacks, timeout, cancel, unavailable, abort and cooldown.
- 🟡 **Ad implementations:** `MockAdsService` (clearly labelled overlay; offline and simulated-failure paths), `NoAdsService`, `AdsFactory` (mock refused in release builds).
- ⬜ **Production SDK adapter:** intentionally not implemented, because no SDK API names were invented. It needs your chosen SDK, its current docs and real IDs.
- ⬜ **Privacy and consent:** Privacy Policy and Terms URLs and the consent flow are placeholders (`CommonOverlays.PrivacyPolicyUrl`; see `Docs/RELEASE_CHECKLIST.md`).

## Phase G: tests, validators, Android config, release docs
- ✅ **EditMode tests:** 95, executed via `dotnet test` (same sources as Unity's EditMode suite).
- 🟡 **PlayMode tests:** 12 written (flows, restart, pause/resume, rapid taps, boosters, shop, themes, save/reload, ads offline/mock, safe area, back button). **Not executed.**
- 🟡 **Editor tooling:** Validate Project, Generate Levels, Batch Validate, screenshot capture, debug APK / release AAB menus and CLI with env-var signing.
- ⬜ **Builds:** APK/AAB **not built** (no Unity/Android toolchain here).

## Authored art integration (supplied art sheets)
- ✅ **Importer:** `Tools/ArtImport/import_art.py` (+ `extract_sheet.py`) runs and writes 40 files: 14 backgrounds, 8 icons, 11 buttons/pills/sign, 3 effects, 4 glasses + shapes JSON.
- ✅ **Extraction details:**
  - difference matte against the local green background keeps glass translucent;
  - baked badges are removed by mirror inpainting;
  - labels are cropped off the backgrounds;
  - the green pill is derived from the blue pill, since a green-screen key destroys green art.
- ✅ **Glass interiors:** derived from alpha and checked by overlaying the polygons on the art.
- ✅ **Composition previews:** `Docs/ArtPreview/compose_*.png`, approximate and not Unity screenshots.
- 🟡 **Runtime integration:** art loader (`SpriteLibrary.Art`, runtime 9-slicing with `pixelsPerUnitMultiplier`), art-face icon buttons, art pills,
  title sign banners, screen and theme backgrounds with readability scrims, Level Complete effects, opened-chest badge, authored bottle skins,
  Desert theme, `VeliqoraArtImporter` import settings. Compiles, but has not been seen in Unity.
- ⬜ **Unused assets:** map art (it needs a seamless tile), the Events and Settings screens' art, obstacles and special liquids (no such mechanics yet).

## Known issues / risks to check first in Unity
1. **First open:** let Unity import, then run Setup. If the Input System prompt appears, either choice is fine.
2. **API drift:** the compile check used Unity 2021.3/2020.3 reference assemblies. Unity 6 may report obsolete-API warnings (handled via `#if UNITY_2021_2_OR_NEWER` for known ones).
3. **Layout:** layout values were tuned for 1080×1920 and safe-area math, but never seen on screen. Expect minor spacing tweaks on very tall or tablet aspect ratios.
4. **Startup cost:** procedural backgrounds are generated on first use per theme (estimated tens of ms). Profile on a midrange device, and consider authored PNGs.
5. **Haptics:** the legacy `Vibrator.vibrate(long)` call is deprecated on new APIs but still functional. Consider `VibrationEffect` for API 26+.
6. **Mock mode:** ads stay in mock mode in Editor/dev builds and are disabled in release builds until a real adapter exists.

## Next steps
Open in Unity 6 → Setup Project → run EditMode and PlayMode suites → play through levels 1–15 on a device → profile → replace placeholders →
integrate the production ad SDK and consent → complete the release checklist.
