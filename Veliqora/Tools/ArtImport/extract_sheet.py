#!/usr/bin/env python3
"""Extracts game sprites from the user-supplied green-screen asset sheet.

Difference matte against the locally estimated green background (handles translucent glass), green
de-spill, and inpainting of the red count badges that are baked into three booster buttons.
Usage: python3 -I extract_sheet.py <sheet.png> <out_dir>
"""
import json, sys, os
import numpy as np
from PIL import Image
from scipy import ndimage

SRC, OUT = sys.argv[1], sys.argv[2]
im = np.array(Image.open(SRC).convert('RGB')).astype(np.float32)
H, W, _ = im.shape
r, g, b = im[..., 0], im[..., 1], im[..., 2]
strict_bg = (g > 120) & (r < 60) & (b < 60) & (g > r + 90) & (g > b + 90)
# Nearest background color for every pixel (the sheet's green has a gradient).
_, (iy, ix) = ndimage.distance_transform_edt(~strict_bg, return_indices=True)
G = im[iy, ix]
G = ndimage.uniform_filter(G, size=(9, 9, 1))

def key(box, t0=18.0, t1=150.0):
    y0, x0, y1, x1 = box
    obs = im[y0:y1, x0:x1]
    bg = G[y0:y1, x0:x1]
    d = np.sqrt(((obs - bg) ** 2).sum(-1))
    a = np.clip((d - t0) / (t1 - t0), 0, 1)
    a = np.where(strict_bg[y0:y1, x0:x1], 0, a)
    a = np.maximum(a, 0.0)
    safe = np.maximum(a, 1e-3)[..., None]
    fg = bg + (obs - bg) / safe           # un-mix: obs = a*F + (1-a)*G
    fg = np.clip(fg, 0, 255)
    # de-spill residual green
    m = np.maximum(fg[..., 0], fg[..., 2])
    fg[..., 1] = np.where(fg[..., 1] > m + 25, m + 25, fg[..., 1])
    rgba = np.dstack([fg, a * 255]).astype(np.uint8)
    return rgba

def trim(rgba, pad=2):
    a = rgba[..., 3]
    ys, xs = np.where(a > 8)
    if len(ys) == 0: return rgba
    y0, y1 = max(0, ys.min() - pad), min(rgba.shape[0], ys.max() + pad + 1)
    x0, x1 = max(0, xs.min() - pad), min(rgba.shape[1], xs.max() + pad + 1)
    return rgba[y0:y1, x0:x1]

def disc_of(rgba, diameter):
    """Disc (cx, cy, r) of a round button from its left and bottom edges (badges sit top-right)."""
    ys, xs = np.where(rgba[..., 3] > 128)
    r = diameter / 2.0
    return xs.min() + r, ys.max() - r, r

def remove_badge(rgba, diameter):
    """Inpaint the red count badge by mirroring the button across its vertical axis, then cut to the disc."""
    px = rgba.astype(np.int32)
    cx, cy, rad = disc_of(rgba, diameter)
    red = (px[..., 0] > 190) & (px[..., 1] < 50) & (px[..., 2] < 70) & (px[..., 3] > 60)
    lab, n = ndimage.label(red)
    if n == 0: return rgba
    largest = 1 + int(np.argmax(ndimage.sum(red, lab, range(1, n + 1))))
    ys, xs = np.where(lab == largest)
    bcy, bcx = ys.mean(), xs.mean()
    brad = max(ys.max() - ys.min(), xs.max() - xs.min()) / 2.0 + 4
    Hh, Ww = rgba.shape[:2]
    yy, xx = np.mgrid[0:Hh, 0:Ww]
    out = rgba.copy()
    badge = (yy - bcy) ** 2 + (xx - bcx) ** 2 <= brad ** 2
    mx = np.clip(np.round(2 * cx - xx).astype(int), 0, Ww - 1)
    out[badge] = rgba[yy[badge], mx[badge]]
    outside = (yy - cy) ** 2 + (xx - cx) ** 2 > (rad + 1) ** 2
    out[outside, 3] = 0
    return out

def save(name, rgba):
    Image.fromarray(rgba, 'RGBA').save(os.path.join(OUT, name + '.png'))

def grayscale(rgba):
    out = rgba.copy().astype(np.float32)
    l = out[..., 0] * 0.3 + out[..., 1] * 0.59 + out[..., 2] * 0.11
    for c in range(3): out[..., c] = l * 0.75 + 70
    return out.clip(0, 255).astype(np.uint8)

# (y0, x0, y1, x1) boxes found by component analysis of the sheet, padded slightly.
ICONS = {
    'Back': (578, 5, 687, 112), 'Pause': (578, 108, 687, 215), 'Restart': (578, 212, 687, 321),
    'Undo': (578, 318, 687, 427), 'Bulb': (565, 423, 690, 540), 'BottlePlus': (565, 534, 690, 650),
    'Shuffle': (565, 644, 690, 760), 'Coin': (592, 773, 687, 866), 'Star': (593, 867, 686, 959),
    'Heart': (601, 959, 686, 1050), 'Gem': (600, 1053, 691, 1147), 'Chest': (583, 1158, 692, 1297),
    'Crown': (596, 1366, 711, 1511),
}
FX = {
    'fx_crown_burst': (711, 23, 848, 198), 'fx_star_burst': (702, 222, 843, 423), 'fx_confetti_star': (702, 431, 848, 632),
    'sign_wood': (711, 1246, 826, 1526),
    'pill_blue': (864, 233, 970, 433), 'pill_purple': (863, 436, 962, 636),
}
BOTTLES = {  # convex glass shapes usable as liquid containers
    'glass_milk': (19, 15, 213, 91), 'glass_cork': (15, 281, 213, 356), 'glass_cone': (18, 358, 213, 442),
    'glass_cap': (19, 693, 213, 773),
}
os.makedirs(OUT, exist_ok=True)
manifest = {}
ref = trim(key(ICONS['Undo']), pad=0)
DIAMETER = float(max(ref.shape[0], ref.shape[1]))  # an unbadged button of the same set
for name, box in ICONS.items():
    rgba = key(box)
    if name in ('Bulb', 'BottlePlus', 'Shuffle'): rgba = remove_badge(trim(rgba, pad=0), DIAMETER)
    rgba = trim(rgba)
    save('icon_' + name, rgba)
    manifest['icon_' + name] = rgba.shape[:2]
save('icon_StarEmpty', grayscale(trim(key(ICONS['Star']))))
for name, box in FX.items():
    rgba = trim(key(box))
    save(name, rgba)
    manifest[name] = rgba.shape[:2]
# The sheet's green pill can't survive a green-screen key; derive it from the blue pill by hue rotation.
blue = Image.open(os.path.join(OUT, 'pill_blue.png'))
hsv = np.array(blue.convert('RGB').convert('HSV')).astype(np.int32)
hsv[..., 0] = (hsv[..., 0] - 50) % 256   # blue (~145/255) -> emerald green (~95/255)
hsv[..., 1] = (hsv[..., 1] * 0.8).astype(np.int32)   # tone down the neon
hsv[..., 2] = (hsv[..., 2] * 0.93).astype(np.int32)
green = Image.fromarray(hsv.astype(np.uint8), 'HSV').convert('RGB')
green.putalpha(blue.getchannel('A'))
green.save(os.path.join(OUT, 'pill_green.png'))
manifest['pill_green'] = (blue.height, blue.width)
for name, box in BOTTLES.items():
    rgba = trim(key(box, t0=10, t1=120), pad=1)
    save(name, rgba)
    manifest[name] = rgba.shape[:2]
json.dump({k: [int(v[1]), int(v[0])] for k, v in manifest.items()}, open(os.path.join(OUT, 'manifest.json'), 'w'), indent=1)
print('extracted', len(manifest) + 1, 'sprites ->', OUT)
