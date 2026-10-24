# LAST BLOCK v0.1

A 3D mobile physics puzzle prototype for Unity 6 (URP, portrait 9:16).
A tower of stacked wooden blocks stands on a platform. Tap a block, swipe to slide it out, and remove
the required number of blocks. When you've removed enough, the tower has to stay standing for 5 seconds.

> **Status:** I wrote all the scripts and the editor setup tool without a Unity Editor. They have
> **not been compiled, run or tested in Unity yet**. Read "Verification status" below before you rely
> on them.

---

## 1. Open and run

### Option A: new Unity project (recommended)

1. Install **Unity 6** (6000.0 LTS or newer) through Unity Hub, with the **Android Build Support** module
   (and iOS if you need it).
2. In Unity Hub, click **New project**, choose **Universal 3D** (URP) and create the project.
3. Copy everything inside `LastBlock/Assets/` into the new project's `Assets/` folder.
4. Wait for scripts to compile. Then click **Tools → Last Block → Setup Game**.
   - If TextMesh Pro resources are missing, the tool imports them and asks you to run **Setup Game** again.
5. Open `Assets/Scenes/MainGame.unity` (the tool opens it for you) and press **Play**.
6. In the Game view, choose a portrait resolution such as **1080x1920** or a phone preset.

### Option B: open this folder directly

`LastBlock/` includes `Packages/manifest.json` (URP, uGUI/TextMeshPro, Test Framework).
1. In Unity Hub, click **Add → Add project from disk** and pick `LastBlock/`.
2. Choose a Unity 6 editor. Unity creates `ProjectSettings/` on first open.
3. Run **Tools → Last Block → Setup Game**. If no URP asset is assigned, the tool creates one in
   `Assets/Settings/` and assigns it.

### Running Setup Game again

Each run rebuilds `MainGame.unity` from scratch and updates the generated assets in place. Prefab and
material GUIDs stay the same, so you can run it as often as you like without duplicate objects or broken
references. **Manual edits to the MainGame scene are lost on the next run.** Put custom content in a
separate scene, or change the tool.

**Tools → Last Block → Reset Saved Progress** clears the saved unlocked level and best score.

## 2. How to play

| Action | Mobile | Editor |
|---|---|---|
| Select a block | Tap it | Left click |
| Pull it out | Drag along the block's length | Drag the mouse |
| Let go | Lift your finger | Release the button |
| Rotate the view | Drag on empty space | Drag on empty space |

- You can't move top-layer blocks. They flash red when tapped.
- A block counts as removed once it has fully cleared the tower's footprint.
- Score: **+100** per block, **+50** bonus for a *perfect* pull (no other block moved more than 5 cm),
  **+200** for finishing a level. If you retry, your score goes back to what it was when the level started.
- Your highest unlocked level and best score are saved locally with `PlayerPrefs`. The game starts at
  your highest unlocked level.

### Levels

| Level | Layers | Remove | Fallen blocks allowed | Friction (static/dynamic) | Middle blocks pre-removed (layer index) |
|---|---|---|---|---|---|
| 1 | 3 | 2 | 1 | 0.70 / 0.55 | – |
| 2 | 4 | 2 | 1 | 0.70 / 0.55 | – |
| 3 | 5 | 3 | 1 | 0.65 / 0.50 | – |
| 4 | 6 | 3 | 1 | 0.65 / 0.50 | – |
| 5 | 7 | 4 | 0 | 0.60 / 0.47 | – |
| 6 | 8 | 4 | 0 | 0.60 / 0.47 | 3 |
| 7 | 9 | 5 | 0 | 0.55 / 0.45 | 2 |
| 8 | 10 | 5 | 0 | 0.55 / 0.43 | 2, 6 |
| 9 | 11 | 6 | 0 | 0.50 / 0.42 | 3, 7 |
| 10 | 12 | 7 | 0 | 0.50 / 0.40 | 2, 5, 8 |

The levels are fixed, not random. Each one can be solved by taking the middle block from intact layers
below the top. An edit-mode test checks this.

## 3. How it works

### Project layout

```
Assets/
  Scenes/            MainGame.unity (generated)
  Scripts/
    Core/            GameManager, LevelManager (+LevelConfig), ScoreManager
    Gameplay/        TowerGenerator, BlockController, BlockDragController,
                     TowerStabilityChecker, CameraController
    UI/              UIManager, SafeAreaFitter
    Effects/         SimpleEffects, ProceduralAudio
    Editor/          LastBlockSetup  (Tools → Last Block → Setup Game)
    LastBlock.asmdef
  Prefabs/           Block.prefab (generated)
  Materials/         block/platform/background/particle materials + Textures/ (generated)
  Audio/             empty: sounds are generated in code (see below)
  Resources/         optional: a prefab named "Block" here is used if none is assigned
  Tests/
    EditMode/        level table, progress saving, scoring, stability thresholds
    PlayMode/        tower stays still, extraction works, locked top layer, collapse detection
```

### Physics

- Each block is a unit cube scaled to **1.54 × 0.30 × 0.50** with a `BoxCollider`, a `Rigidbody`
  (mass 1, interpolation on, discrete collision detection) and a per-level runtime `PhysicsMaterial`.
  Friction is averaged and bounce is set to minimum.
- Each layer holds 3 blocks with 2 cm gaps between them. Layers alternate by 90°. Blocks spawn exactly
  touching and **asleep**, so the tower starts perfectly still.
- `GameManager.ApplyPhysicsSettings()` sets a 100 Hz fixed step, 12 solver iterations (4 for velocity),
  and a max depenetration velocity of 1 m/s. These make tall stacks stable.
- **Dragging**: once a block is selected, its rotation and its two non-pull axes are frozen. Each
  `FixedUpdate` sets its velocity toward the finger's target, capped at 2 m/s. The block slides along its
  own long axis and is never teleported. The blocks next to it in the same layer are parallel and 2 cm
  away, so it can't pass through them. The blocks above and below are dragged along only by real
  friction.
- When the block clears the footprint, its collider is turned off and it plays a short fly-out
  animation before it is destroyed.

### Stability and the win/lose rules (`TowerStabilityChecker`)

- For the first **1 s** after the tower spawns, nothing is checked. After that, each block's settled
  position is saved as its baseline.
- A block counts as **fallen** if, for **0.25 s** in a row, one of these is true:
  - it has dropped more than **half a block height** (15 cm) below its baseline,
  - it is tilted more than **30°**,
  - it is below `y = -1.5` (fell off the platform).
- Horizontal sliding doesn't count. Removed blocks and the block being dragged are always ignored.
- **Lose**: more blocks have fallen than the level allows.
- **Win**: the target number of blocks is removed and the tower doesn't collapse for **5 s**. After
  those 5 s, the game waits up to 3 more seconds for the blocks to stop moving.

### Visuals and audio

URP Lit materials with a procedurally generated wood-grain texture tinted in 6 colours. A gradient
quad sits behind everything, attached to the camera. The directional light casts soft shadows and
the ambient light is trilight. Particles (wood chips, confetti) are built in code. A camera shake
plays on hard impacts. The victory card pops in, and the defeat card pops in and shakes. All sounds
(click, pop, thud, deny, win, lose) are generated in code by `ProceduralAudio`, so the project needs
no audio files. To use your own clips, assign them in the `SimpleEffects` override slots.

### Input

Input works with both the **Input System package** (the Universal 3D template default) and the
**legacy Input Manager**, selected at compile time (`ENABLE_INPUT_SYSTEM` + a version define, or
`ENABLE_LEGACY_INPUT_MANAGER`). The setup tool adds the matching EventSystem module. Taps over UI are
ignored.

## 4. Required Unity settings

The setup tool configures these for you:

- **Player → Resolution and Presentation**: Default Orientation set to *Portrait*, autorotation
  limited to portrait.
- **Product name** `Last Block`. The application identifier is set to `com.lastblock.game`, but only
  if it's still the `DefaultCompany` placeholder.
- **Graphics**: a URP asset is assigned if none is active.
- **Build Settings**: `MainGame.unity` is added as scene 0.

You need to check these yourself:

- **Active Input Handling** (Player → Other Settings) can be *Input System Package*, *Input Manager*
  or *Both*. All three work.
- If you switch the render pipeline after setup, run **Setup Game** again so the materials pick up
  the right shaders.

## 5. Android build

1. In **File → Build Profiles** (Build Settings in older versions), select **Android** and click
   **Switch Platform**.
2. Make sure `Assets/Scenes/MainGame.unity` is in the scene list. Setup adds it.
3. In **Player Settings → Other Settings**:
   - **Scripting Backend**: IL2CPP.
   - **Target Architectures**: ARM64. Google Play requires it.
   - **Minimum API Level**: Android 7.0 (API 24) or higher.
   - Check that **Package Name** is `com.lastblock.game`, or set your own.
4. To ship on Google Play, sign the app under **Publishing Settings** with a keystore. Development
   builds use the debug key automatically.
5. Connect a device with USB debugging on and click **Build And Run**. Or click **Build** to get an
   `.apk`, or an `.aab` if **Build App Bundle** is checked.

For iOS, switch the platform to iOS, click **Build**, then open the generated Xcode project and
sign it there.

## 6. Tests

Open **Window → General → Test Runner**:

- **EditMode**: 10 levels with the right layer counts, every level solvable, difficulty never
  decreasing, progress unlock and persistence, score rules, stability thresholds (vibration vs.
  drop/tilt).
- **PlayMode**: the 12-layer tower stays still for 3 s, pulling a middle block extracts it without a
  collapse, top-layer blocks are locked, and removing the bottom support is detected as a collapse.

### Verification status

| Check | Status |
|---|---|
| Project compiles | **Not verified.** Unity was not available when I wrote the code |
| Scene setup tool runs | **Not verified** |
| Tower starts stable, select/drag/physics, victory, collapse, restart, next level, save | **Not verified in the editor.** The PlayMode/EditMode tests cover most of these and need to be run |

Physics constants (drag speed, friction, thresholds) are first guesses and will probably need tuning
on a real device.

## 7. Known limitations and future improvements

**Limitations**
- Untested in the Unity Editor. Expect a round of compile fixes and physics tuning.
- Running Setup Game again overwrites manual changes to the MainGame scene.
- Blocks can only be pulled straight along their long axis, not pushed sideways or lifted.
- A pulled block is "perfect" based on how far other blocks moved, not how fast you pulled.
- There is no level-select screen. The game resumes at your highest unlocked level.
- The UI uses the default TMP font and built-in UI sprites. Sounds are simple synth tones.
- Camera orbit is horizontal only, and there's no zoom.

**Possible next steps**
- Level select and star ratings. Pause menu with settings: sound, vibration, reset progress.
- Haptic feedback when picking a block and on impacts.
- Push from the opposite side, and a "place on top" rule like the real game.
- Wood-chip decals, a better block material, and an animated collapse slow-motion.
- Per-device quality settings and further physics tuning (mass, damping, solver) from playtests.
- An endless/procedural mode and online features (left out of v0.1 on purpose).
