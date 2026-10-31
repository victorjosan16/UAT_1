#!/usr/bin/env python3
"""One-shot importer for the user-supplied VELIQORA art sheets.

  python3 -I import_art.py <backgrounds_sheet.png> <asset_sheet.png> <unity_project_root>

Writes into Assets/_Veliqora/Resources/Veliqora/Art/:
  Backgrounds/  screen_*.png (8 screens) and theme_*.png (6 themes), labels cropped away
  Icons/        Coin, Star, StarEmpty, Chest, ChestOpen, Heart, Gem, Crown (glyph-style icons)
  Buttons/      Back, Pause, Restart, Undo, Bulb, BottlePlus, Shuffle (full round buttons, badges removed),
                pill_green / pill_blue / pill_purple (9-slice pills), sign_wood (title sign)
  Effects/      fx_crown_burst, fx_star_burst, fx_confetti_star
  Bottles/      glass_*.png + bottle_shapes.json (normalized convex interior polygon + aspect per glass)
and copies both source sheets to Assets/_Veliqora/Art/Source/ (not in Resources, so not shipped).
"""
import json, os, shutil, subprocess, sys
import numpy as np
from PIL import Image

BG_SHEET, ASSET_SHEET, ROOT = sys.argv[1], sys.argv[2], sys.argv[3]
ART = os.path.join(ROOT, 'Assets/_Veliqora/Resources/Veliqora/Art')
for d in ('Backgrounds', 'Icons', 'Buttons', 'Effects', 'Bottles'):
    os.makedirs(os.path.join(ART, d), exist_ok=True)
src_dir = os.path.join(ROOT, 'Assets/_Veliqora/Art/Source')
os.makedirs(src_dir, exist_ok=True)
shutil.copy(BG_SHEET, os.path.join(src_dir, 'veliqora_backgrounds_sheet.png'))
shutil.copy(ASSET_SHEET, os.path.join(src_dir, 'veliqora_assets_sheet.png'))

# ---------------- backgrounds ----------------
# Panel bounds measured from the white separators; bottom edge stops above the baked-in label pill.
INSET = 7
PANELS = {
    'screen_home': (0, 0, 260, 448), 'screen_map': (268, 0, 505, 448),
    'screen_gameplay': (512, 0, 767, 448), 'screen_complete': (774, 0, 1024, 448),
    'screen_shop': (0, 513, 261, 925), 'screen_themes': (270, 513, 507, 925),
    'screen_settings': (516, 513, 767, 925), 'screen_events': (774, 513, 1024, 925),
    'theme_valley': (0, 992, 163, 1424), 'theme_sunset': (173, 992, 337, 1424),
    'theme_night': (346, 992, 507, 1424), 'theme_garden': (516, 992, 678, 1424),
    'theme_winter': (687, 992, 853, 1424), 'theme_desert': (864, 992, 1024, 1424),
}
bg = Image.open(BG_SHEET).convert('RGB')
for name, (x0, y0, x1, y1) in PANELS.items():
    crop = bg.crop((x0 + INSET, y0 + INSET, x1 - INSET, y1))
    # 2x Lanczos upscale: smoother than letting the GPU bilinear-stretch a ~250px image to 1080px.
    crop = crop.resize((crop.width * 2, crop.height * 2), Image.LANCZOS)
    crop.save(os.path.join(ART, 'Backgrounds', name + '.png'))

# ---------------- asset sheet ----------------
tmp = os.path.join(ROOT, 'Temp', 'art_extract')
os.makedirs(tmp, exist_ok=True)
here = os.path.dirname(os.path.abspath(__file__))
subprocess.check_call([sys.executable, '-I', os.path.join(here, 'extract_sheet.py'), ASSET_SHEET, tmp])

def move(src, dst):
    shutil.copy(os.path.join(tmp, src + '.png'), os.path.join(ART, dst + '.png'))

for n in ('Coin', 'Star', 'StarEmpty', 'Chest', 'Heart', 'Gem', 'Crown'):
    move('icon_' + n, 'Icons/' + n)
move('icon_Chest', 'Icons/ChestOpen')
for n in ('Back', 'Pause', 'Restart', 'Undo', 'Bulb', 'BottlePlus', 'Shuffle'):
    move('icon_' + n, 'Buttons/' + n)
for n in ('pill_green', 'pill_blue', 'pill_purple', 'sign_wood'):
    move(n, 'Buttons/' + n)
for n in ('fx_crown_burst', 'fx_star_burst', 'fx_confetti_star'):
    move(n, 'Effects/' + n)

# ---------------- authored bottles: derive the liquid interior ----------------
def cross(o, a, b): return (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0])

def hull(pts):
    pts = sorted(set(pts))
    lo, up = [], []
    for p in pts:
        while len(lo) >= 2 and cross(lo[-2], lo[-1], p) <= 0: lo.pop()
        lo.append(p)
    for p in reversed(pts):
        while len(up) >= 2 and cross(up[-2], up[-1], p) <= 0: up.pop()
        up.append(p)
    return lo[:-1] + up[:-1]  # counter-clockwise

shapes = {}
for name in ('glass_milk', 'glass_cork', 'glass_cone', 'glass_cap'):
    move(name, 'Bottles/' + name)
    a = np.array(Image.open(os.path.join(tmp, name + '.png')))[..., 3]
    h, w = a.shape
    solid = a > 70
    rows = []
    for y in range(h):                       # y = 0 at top in image space
        xs = np.where(solid[y])[0]
        rows.append((xs.min(), xs.max()) if len(xs) else None)
    widths = np.array([(r[1] - r[0]) if r else 0 for r in rows], dtype=float)
    maxw = widths.max()
    bottom = max(y for y in range(h) if rows[y])
    # Capacity line: walk up from the lowest "body" row (>= 60% of max width) until the glass narrows into the neck.
    body_start = max(y for y in range(h) if widths[y] >= 0.6 * maxw)
    top = body_start
    for y in range(body_start, -1, -1):
        if widths[y] < 0.6 * maxw: break
        top = y
    top = max(top, int(0.17 * h))            # keep headroom below the lip / cork
    wall = max(2, int(round(0.075 * maxw)))
    pts = []
    for y in range(top + wall, bottom - wall + 1, 2):
        l, r = rows[y]
        if r - l <= 2 * wall: continue
        # flip to y-up, origin bottom-center, normalized by sprite width
        ny = (h - 1 - y) / w
        pts.append(((l + wall - w / 2) / w, ny))
        pts.append(((r - wall - w / 2) / w, ny))
    poly = hull([(round(x, 4), round(y, 4)) for x, y in pts])
    shapes[name] = {'aspect': round(h / w, 4), 'interior': [[x, y] for x, y in poly]}
json.dump(shapes, open(os.path.join(ART, 'Bottles', 'bottle_shapes.json'), 'w'), indent=1)
shutil.rmtree(tmp, ignore_errors=True)
print('Imported art into', ART)
