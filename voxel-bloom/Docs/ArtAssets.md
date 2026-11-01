# Premium artwork: inventory, layout spec, extraction report

Source: `Docs/reference/asset-sheet-green.png` (1536×1024, green key ≈ #04F903). Home reference: `Docs/reference/home-mockup-v2.png`.
Extraction is reproducible: `python3 tools/art/ext.py && python3 tools/art/cut.py && python3 tools/art/derive.py && python3 tools/art/block.py`
(writes the same bytes as the committed files; `tools/art/layout.py` regenerates `Docs/HomeLayoutCheck.png`).

## Folder layout
Unity only loads runtime art from folders named `Resources`, and this UI is built in code (no prefabs), so the requested folders
contain a `Resources/VBUI` level:

| Requested | Actual |
|---|---|
| `Art/UI/Logo`, `Buttons`, `HUD`, `Icons`, `Blocks`, `Boosters` | `Assets/VoxelBloom/Art/UI/Resources/VBUI/<same names>` |
| `Art/UI/Collection` | not shipped: the 6 tiles have models + locks painted in (see below), kept in `Docs/reference/extracted/Collection` |
| `Art/Backgrounds` | `Assets/VoxelBloom/Art/Backgrounds/` (README; drop-in slot `Resources/VBUI/Backgrounds/home_background.png`). The sheet landscape card is kept in `Docs/reference/extracted/Backgrounds` |

`Editor/UIArtImporter.cs` imports everything under `Resources/VBUI` as sprites (no NPOT rescale, trilinear, mipmaps, HQ compression).
`Scripts/UI/UIArt.cs` builds sprites with nine-slice borders at runtime. If a file is missing, every caller falls back to the
procedural look. `Tests/EditMode/UIArtTests.cs` checks that all referenced files exist.

## How the extraction was done
1. Background = green pixels (`g > 140 && g − max(r,b) > 80`) connected to the sheet border (8-connected flood fill), plus
   enclosed pockets that are within 22 of the exact key colour.
2. The edges within 2 px of the background get alpha from their greenness, and the green cast is removed (despill).
3. **Protected green artwork:** `Joacă`, the coin/gem pills (green "+") and the hearts pill only use the border-connected
   background (feathered by 0.8 px). No greenness test runs inside them, so their greens are untouched.
4. Each asset is cropped manually, and stray neighbour fragments touching the crop edge are dropped.
5. Contact sheets on dark and white backgrounds were inspected: no visible green halos on the shipped files.

## Inventory (shipped)
| File | Size px | Use | Notes |
|---|---|---|---|
| Logo/logo | 608×306 | Home logo (exact artwork, not text) | |
| HUD/avatar | 152×122 | TopBar avatar | painted "12" badge covered by a live level badge |
| HUD/pill_hearts_blank / pill_coins_blank / pill_gems_blank | 224×95 / 289×89 / 260×91 | TopBar, gameplay coins | baked numbers removed (row interpolation), live text |
| HUD/caption_pill_blank | 276×47 | Home "N. Name · page/pages" | text removed |
| HUD/percent_pill_blank | 139×64 | Gameplay progress % | text removed |
| HUD/progress_track, progress_fill | 639×49 | Gameplay glossy green progress bar | derived from the bar, nine-slice, live width |
| Buttons/play | 430×154 | Home Joacă | label is part of the art (Romanian) |
| Buttons/level_badge_blank | 335×118 | Home "Nivel N" with laurels + star | text removed, live label |
| Buttons/nav_collection / rewards / shop / events | ≈180×160 | Home bottom nav | labels part of the art |
| Buttons/settings, Buttons/back | ≈105 | gear, purple round arrows (right = mirrored) | |
| Icons/label_360, rotate_left, rotate_right | | "← 360° →" on the pedestal | |
| Icons/cube, star_gold, star_empty, lightning | | gameplay progress row, star rating | |
| Boosters/hammer, shuffle, color_bomb, remove | ≈137×134 | gameplay booster row | painted counts covered by live count badges |
| Blocks/block_face + block_gloss | 400×220 | every board/launcher block | **generated template** in the sheet style (see below) |

### Blocks: exact voxel colours
The 14 sheet blocks are packed edge-to-edge and carry painted numbers, so the clean crops lose rims, and inpainting leaves
artefacts on the green block. Recolouring painted art would also never give *exactly* the voxel colour. Instead,
`block_face` is a glossy rounded block whose body is pure white. Tinted with the palette colour, the body shows **exactly
the voxel colour**, and the darker lip and rim are shades of it. `block_gloss` adds the untinted highlights (top band, inner rim,
specular dot). The sheet's 14 sampled colours are kept in `Docs/reference/extracted/block_colors.json`. They are more neon
than the level palette, and the palette was kept because you asked to reduce neon.

## Layout spec (canvas 1080×1920, = mockup px × 1.148)
| Element | Anchor / position | Size |
|---|---|---|
| TopBar | top band y 24–134, x 30–1050; avatar 0, hearts 144, coins 355, gems 626, gear right | art × 0.96 |
| Logo | top-centre, centre (−6, −312) | width 790 |
| Purple arrows | screen (0.17, 0.568) / (0.83, 0.568) | 118 |
| 360° label | screen (0.5, 0.474) | art × 1.39 |
| 3D model + pedestal | full-screen render, camera (0, 1.85, −8.6) → (0, −0.1, 0), fov 40 | |
| Carousel strip | bottom band 700–876 | 6 tiles 150×140 |
| Caption pill | bottom-centre y 672 | width 360 |
| Joacă | bottom-centre y 540 | 680×244 |
| Nivel badge | bottom-centre y 407 | 460×162 |
| Nav | bottom 88, x centres ±131, ±393 | width 240 |

`Docs/HomeLayoutCheck.png` composites the shipped art at these coordinates next to the mockup. It is a **layout check, not a
Unity screenshot** (flat sky, 2D fox stand-in).

## Not extracted cleanly: separate transparent sources needed
| Asset | Problem | What is used instead | Needed |
|---|---|---|---|
| Home background landscape | the sheet has only a 578×325 landscape card; a portrait 1080×1920+ screen would need ≥3× upscaling | the 3D voxel landscape (as before) | full-resolution portrait background → drop it in as `Art/Backgrounds/Resources/VBUI/Backgrounds/home_background.png` and Home uses it automatically (3D scenery is then skipped) |
| Carousel tiles | models and locks painted in; an empty frame cannot be recovered cleanly | procedural tiles with live 3D thumbnails | empty tile frame (normal + selected gold) |
| Blocks | packed edge-to-edge, numbers painted in | generated glossy template | optional: one empty block as a separate PNG |
| Recompense button | the red "!" overlaps the gift and frame, so it cannot be removed | art as-is: the "!" is always visible, not only when a reward can be claimed | button without the badge + separate badge |
| Booster buttons | counts painted in | live badge drawn exactly over the painted one | optional: buttons without badges |
| Resolution | the sheet is 1536 px wide; logo, Joacă and nav are shown at ≈1.3–1.6× their pixel size on a 1080-wide canvas, so they look slightly soft on high-dpi phones | — | 2× sources of the same art |
