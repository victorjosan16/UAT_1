# Visual QA status (honest)

The reference image (`Docs/reference/mockup.png`) defines eight screens. The Unity Editor is **not available**
in the environment where this code was written, so **no running screen has been captured or measured yet**.
Nothing here claims pixel-perfect reproduction.

| Screen | Implemented in | Matches the reference by design | Needs checking in the Editor |
|---|---|---|---|
| 1 Home | `HomeScreen.cs` | top bar (avatar, lives, coins +, gear), Voxel Bloom 3D logo, fox on grassy voxel platform with trees and flowers, big green PLAY, 4-tab bottom nav, idle bob, cloud parallax | logo font weight/gradient, diorama framing |
| 2 Level Select | `LevelSelectScreen.cs` | purple ribbon, 5 chapter cards (thumbnail, name, stars x/60, play/lock) | card spacing, thumbnail framing |
| 3 Gameplay | `GameplayScreen.cs` | pause, Lv pill, 3 stars, coins; sky panel with rotating 3D model, arrows, pedestal; progress bar with %; 5 queue slots; 5x4 numbered board; booster row | vertical rhythm on 16:9 vs 20:9 |
| 4 Combo | `GameplayScreen.ShowCombo` | slanted gradient COMBO xN, fragments, shake, haptics | text size/position vs reference |
| 5 Pause | `PauseDialog` | dark overlay, white panel, purple ribbon, green Resume, blue Restart, purple Settings, close | — |
| 6 Level Complete | `LevelCompleteDialog` | ribbon, 3 animated stars, model, reward with coins, Continue, Double Reward | star rays/burst background is simplified |
| 7 Boosters | `BoostersDialog` | Hammer 300, Shuffle 200, Color Bomb 500 (+ Eliminare 150), icons, descriptions, prices, confirmation | — |
| 8 Collection | `CollectionScreen` | ribbon, Animals/Food/Vehicles/Buildings tabs, 3-col grid, checkmarks/locks, progress bar | — |

## Known differences from the reference
* All icons are drawn procedurally at runtime (`SpriteFactory` / `IconPainter`). They follow the
  reference's style (bold outline, glossy fill) but are simpler than hand-painted art.
* Logo uses Fredoka SemiBold (OFL) with vertex gradients instead of a custom lettering asset.
* Thumbnails and the model view are rendered from the real voxel data, so they differ in pose from the mockup.

## How to do the measured comparison
1. Open the project in Unity 6, run `Voxel Bloom > Setup Project`, press Play in the Game view at 1080x1920.
2. Capture each screen (`ScreenCapture.CaptureScreenshot`) and compare it side by side with the mockup crop.
3. Record deltas in this file.
