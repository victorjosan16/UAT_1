# Cuts the Home / HUD pieces from Docs/reference/home-sheet-green.png (green key, border-connected flood fill so greens
# inside the artwork are kept). Writes every component as comp_NN.png (for inspection) and the named pieces.
import os, json, sys
from collections import deque
import numpy as np
from PIL import Image, ImageFilter
ROOT = os.environ.get('VB_ROOT', os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..')))
SRC = ROOT + '/Docs/reference/' + sys.argv[1]
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
json.dump(comps, open('/tmp/claude-0/' + sys.argv[1] + '.json', 'w'))
for i, c in enumerate(comps): print(i, c)


rgba.save('/tmp/claude-0/' + sys.argv[1] + '.rgba.png')
