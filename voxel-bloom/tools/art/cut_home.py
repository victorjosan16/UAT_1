# Cuts the Home / HUD pieces from Docs/reference/home-sheet-green.png (green key, border-connected flood fill so greens
# inside the artwork are kept). Writes every component as comp_NN.png (for inspection) and the named pieces.
import os, json, sys
from collections import deque
import numpy as np
from PIL import Image, ImageFilter
ROOT = os.environ.get('VB_ROOT', os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..')))
SRC = ROOT + '/Docs/reference/home-sheet-green.png'
im = np.array(Image.open(SRC).convert('RGB')).astype(float); H, W = im.shape[:2]
r, g, b = im[..., 0], im[..., 1], im[..., 2]
keyish = (g > 140) & (g - np.maximum(r, b) > 80)
bg = np.zeros((H, W), bool); q = deque()
for x in range(W):
    for y in (0, H - 1):
        if keyish[y, x]: bg[y, x] = True; q.append((y, x))
for y in range(H):
    for x in (0, W - 1):
        if keyish[y, x] and not bg[y, x]: bg[y, x] = True; q.append((y, x))
while q:
    cy, cx = q.popleft()
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            ny, nx = cy + dy, cx + dx
            if 0 <= ny < H and 0 <= nx < W and not bg[ny, nx] and keyish[ny, nx]: bg[ny, nx] = True; q.append((ny, nx))
fg = Image.fromarray(((~bg) * 255).astype(np.uint8)).filter(ImageFilter.MinFilter(3)).filter(ImageFilter.GaussianBlur(0.9))
alpha = np.array(fg).astype(float) / 255
green = g - np.maximum(r, b); rim = (alpha < 0.999) & (alpha > 0)
out = im.copy(); out[..., 1] = np.where(rim & (green > 10), np.maximum(r, b) + np.clip(green, 0, 10) * 0.5, g)
rgba = Image.fromarray(np.dstack([np.clip(out, 0, 255), alpha * 255]).astype(np.uint8), 'RGBA')
from scipy import ndimage
lab, n = ndimage.label(~bg, structure=np.ones((3, 3)))
boxes = [s for s in ndimage.find_objects(lab)]
comps = []
for i, s in enumerate(boxes):
    y0, y1, x0, x1 = s[0].start, s[0].stop, s[1].start, s[1].stop
    if (y1 - y0) * (x1 - x0) < 600: continue
    comps.append((x0, y0, x1, y1))
comps.sort(key=lambda c: (round(c[1] / 120), c[0]))
json.dump(comps, open('/tmp/claude-0/home_comps.json', 'w'))
for i, c in enumerate(comps): print(i, c)

UI = ROOT + '/Assets/VoxelBloom/Art/UI/Resources/VBUI/'
A = np.array(rgba)[..., 3]
def save(path, box):
    c = rgba.crop(box); bb = c.getbbox(); c = c.crop(bb)
    a = np.array(c); lab2, n2 = ndimage.label(a[..., 3] > 40, structure=np.ones((3, 3)))
    if n2 > 1:  # drop fragments of neighbours touching the crop edge
        sizes = ndimage.sum(np.ones_like(lab2), lab2, range(1, n2 + 1)); keep = 1 + int(np.argmax(sizes))
        for i in range(1, n2 + 1):
            if i != keep and sizes[i - 1] < 0.25 * sizes.max(): a[lab2 == i, 3] = 0
        c = Image.fromarray(a, 'RGBA'); c = c.crop(c.getbbox())
    os.makedirs(os.path.dirname(UI + path), exist_ok=True); c.save(UI + path + '.png'); return c
def split_x(box):
    x0, y0, x1, y1 = box; cols = A[y0:y1, x0:x1].max(0)
    mid = (x1 - x0) // 2; gaps = [i for i in range(len(cols)) if cols[i] <= 20]
    s = min(gaps, key=lambda i: abs(i - mid)) if gaps else mid
    return (x0, y0, x0 + s, y1), (x0 + s, y0, x1, y1)
C = comps
save('Logo/logo_home', C[0])
coin = save('HUD/home_pill_coins', C[1]); gem = save('HUD/home_pill_gems', C[2])
save('HUD/home_avatar', C[3]); save('Buttons/home_settings', C[4])
save('Buttons/home_play', C[5]); save('Buttons/home_levels', C[6])
x0, y0, x1, y1 = C[8]
save('Buttons/home_rewards', (x0, y0, x0 + 228, y1)); save('Buttons/home_collection', (x0 + 230, y0, x1, y1))
save('Buttons/home_events', C[9]); save('Buttons/home_shop', C[10]); save('Buttons/home_leaderboard', C[11])
save('Boosters/refresh', C[24]); save('Boosters/color_swap', C[25]); save('Boosters/add_tray', C[26])
save('Buttons/round_pause', C[27]); save('Buttons/round_sound', C[28]); save('Buttons/round_music', C[29]); save('Buttons/round_restart', C[30])
ban = np.array(save('Collection/win_banner', C[12])).astype(float)
pocket = (ban[..., 1] - np.maximum(ban[..., 0], ban[..., 2]) > 90) & (ban[..., 1] > 150)  # enclosed key-green between stars and ribbon
ban[pocket, 3] = 0
Image.fromarray(ban.astype(np.uint8), 'RGBA').save(UI + 'Collection/win_banner.png')
# blank the baked numbers in the coin / gem pills (row interpolation across the text area)
def blank(path, fx0, fx1):
    c = np.array(Image.open(UI + path + '.png')).astype(float); h, w = c.shape[:2]
    L, R = int(w * fx0), int(w * fx1)
    for y in range(h):
        rr = c[y, R].copy()
        if rr[3] < 250: continue
        c[y, L:R + 1] = rr  # copy the clean body column (keeps the vertical gloss) over the text
    Image.fromarray(c.astype(np.uint8), 'RGBA').save(UI + path + '_blank.png')
blank('HUD/home_pill_coins', 100 / 340, 252 / 340); blank('HUD/home_pill_gems', 120 / 302, 216 / 302)
print('saved')
