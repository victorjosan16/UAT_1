# Cuts the title logo and the "Joacă" button from Docs/reference/title-sheet-green.png (green key).
# Background = key-green pixels connected to the sheet border (the white outlines stop the flood, so the green of the
# button, the leaves and the cubes is never removed); edges feathered, green spill reduced on the outer rim only.
import os
from collections import deque
import numpy as np
from PIL import Image, ImageFilter
ROOT = os.environ.get('VB_ROOT', os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..')))
im = np.array(Image.open(ROOT + '/Docs/reference/title-sheet-green.png').convert('RGB')).astype(float)
H, W = im.shape[:2]
r, g, b = im[..., 0], im[..., 1], im[..., 2]
keyish = (g > 140) & (g - np.maximum(r, b) > 80)
bg = np.zeros((H, W), bool); q = deque()
for x in range(W):
    for y in (0, H - 1):
        if keyish[y, x] and not bg[y, x]: bg[y, x] = True; q.append((y, x))
for y in range(H):
    for x in (0, W - 1):
        if keyish[y, x] and not bg[y, x]: bg[y, x] = True; q.append((y, x))
while q:
    cy, cx = q.popleft()
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            ny, nx = cy + dy, cx + dx
            if 0 <= ny < H and 0 <= nx < W and not bg[ny, nx] and keyish[ny, nx]: bg[ny, nx] = True; q.append((ny, nx))
# feathered alpha from the border-connected background
fg = Image.fromarray(((~bg) * 255).astype(np.uint8)).filter(ImageFilter.MinFilter(3)).filter(ImageFilter.GaussianBlur(0.9))
alpha = np.array(fg).astype(float) / 255
rim = (alpha < 0.999) & (alpha > 0)
green = g - np.maximum(r, b)
out = im.copy()
out[..., 1] = np.where(rim & (green > 10), np.maximum(r, b) + np.clip(green, 0, 10) * 0.5, g)
rgba = Image.fromarray(np.dstack([np.clip(out, 0, 255), alpha * 255]).astype(np.uint8), 'RGBA')
a = np.array(rgba)[..., 3]
rows = np.where(a.max(1) > 20)[0]
# split at the widest transparent gap between logo (top) and button (bottom)
gaps = [y for y in range(rows.min(), rows.max()) if a[y].max() <= 20]
split = int(np.median(gaps)) if gaps else H // 2
def crop(y0, y1):
    part = rgba.crop((0, y0, W, y1)); return part.crop(part.getbbox())
UI = ROOT + '/Assets/VoxelBloom/Art/UI/Resources/VBUI/'
logo = crop(0, split); play = crop(split, H)
logo.save(UI + 'Logo/logo_title.png'); play.save(UI + 'Buttons/play_title.png')
print('split', split, 'logo', logo.size, 'play', play.size)
