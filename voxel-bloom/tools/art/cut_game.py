# Gameplay pieces from Docs/reference/game-sheet-green.png and the belt frame Docs/reference/belt-frame-green.png.
# Run tools/art/key_sheet.py game-sheet-green.png first (writes the keyed sheet to /tmp/claude-0).
import os, json
from collections import deque
import numpy as np
from PIL import Image, ImageFilter
from scipy import ndimage
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
UI = ROOT + '/Assets/VoxelBloom/Art/UI/Resources/VBUI/'
sheet = Image.open('/tmp/claude-0/game-sheet-green.png.rgba.png')
C = json.load(open('/tmp/claude-0/game-sheet-green.png.json'))
def save(img, box, path):
    c = img.crop(box); c = c.crop(c.getbbox()); os.makedirs(os.path.dirname(UI + path), exist_ok=True); c.save(UI + path + '.png'); return c
save(sheet, C[0], 'Buttons/game_pause'); save(sheet, C[3], 'Buttons/game_settings')
save(sheet, C[1], 'HUD/game_level_pill'); save(sheet, C[2], 'HUD/game_progress_pill')
save(sheet, C[6], 'Board/lock_tile'); save(sheet, C[11], 'Board/plus_tile')
save(sheet, C[26], 'Board/queue_card_empty'); save(sheet, C[23], 'Board/queue_check')
# one belt cube -> white template (luminance normalised) so it can be tinted with any palette colour
x0, y0, x1, y1 = C[36]; cw = (x1 - x0) / 5
cube = save(sheet, (int(x0 + 2 * cw), y0, int(x0 + 3 * cw) + 2, y1), 'Board/cube_yellow')
a = np.array(cube).astype(float); L = a[..., :3].max(2)
ref = np.percentile(L[a[..., 3] > 200], 92)
t = np.clip(L / ref, 0, 1) * 255
Image.fromarray(np.dstack([t, t, t, a[..., 3]]).astype(np.uint8), 'RGBA').save(UI + 'Board/cube_white.png')
# blank the "Nivel 5" text of the level pill (copy a clean body column across)
p = np.array(Image.open(UI + 'HUD/game_level_pill.png')).astype(float); h, w = p.shape[:2]
L0, R0 = int(w * 0.16), int(w * 0.86); col = p[:, int(w * 0.12)].copy()
for y in range(h):
    if col[y, 3] >= 250 and p[y, R0, 3] >= 250:
        tt = np.linspace(0, 1, R0 - L0 + 1)[:, None]; p[y, L0:R0 + 1] = col[y] * (1 - tt) + p[y, R0] * tt
Image.fromarray(p.astype(np.uint8), 'RGBA').save(UI + 'HUD/game_level_pill_blank.png')

# ---- belt frame: key out the green (outside and inside), measure the lane centreline
im = np.array(Image.open(ROOT + '/Docs/reference/belt-frame-green.png').convert('RGB')).astype(float); H, W = im.shape[:2]
r, g, b = im[..., 0], im[..., 1], im[..., 2]
key = (g > 140) & (g - np.maximum(r, b) > 80)
lab, n = ndimage.label(key)
sizes = ndimage.sum(np.ones_like(lab), lab, range(1, n + 1))
bgm = np.isin(lab, [i + 1 for i in range(n) if sizes[i] > 2000])  # outer + inner green areas
alpha = np.array(Image.fromarray(((~bgm) * 255).astype(np.uint8)).filter(ImageFilter.MinFilter(3)).filter(ImageFilter.GaussianBlur(0.9))).astype(float) / 255
green = g - np.maximum(r, b); out = im.copy(); rim = (alpha < 0.999) & (alpha > 0)
out[..., 1] = np.where(rim & (green > 10), np.maximum(r, b), g)
frame = Image.fromarray(np.dstack([np.clip(out, 0, 255), alpha * 255]).astype(np.uint8), 'RGBA')
frame = frame.crop(frame.getbbox()); frame.save(UI + 'Board/belt_frame.png')
fa = np.array(frame).astype(float); h, w = fa.shape[:2]
# the lane runs between the two white neon lines: its centre is their midpoint
white = (fa[..., 3] > 200) & (fa[..., :3].min(2) > 200)
def mid(line):
    idx = np.where(line)[0]
    return (idx.min() + idx.max()) / 2, idx.max() - idx.min()
c_l, lw = mid(white[h // 2 + 40, : w // 3])
c_r = w - 1 - (mid(white[h // 2 + 40, 2 * w // 3:])[0] + 2 * w // 3)
c_t = mid(white[: h // 3, w // 2 + 40])[0]
dg = np.array([white[i, i] for i in range(min(h, w) // 2)])
dd = mid(dg)[0]
c = (c_l + c_r + c_t) / 3
rc = (dd - c) / (1 - 0.70710678)
meta = {'size': [w, h], 'centreInset': round(float(c), 1), 'cornerRadius': round(float(rc), 1), 'laneWidth': int(lw)}
json.dump(meta, open(UI + 'Board/belt_frame.json', 'w'))
print(meta, 'insets', c_l, c_r, c_t, 'diag', dd)
