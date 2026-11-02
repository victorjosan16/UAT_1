# Shared helpers: green-key a sheet (border-connected flood + large enclosed key pockets), list components, crop,
# and erase baked text/digits with OpenCV inpainting.
import os, json
from collections import deque
import numpy as np
from PIL import Image, ImageFilter
from scipy import ndimage
import cv2
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
UI = ROOT + '/Assets/VoxelBloom/Art/UI/Resources/VBUI/'

def key(path, pocket=True):
    im = np.array(Image.open(path).convert('RGB')).astype(float); H, W = im.shape[:2]
    r, g, b = im[..., 0], im[..., 1], im[..., 2]
    keyish = (g > 140) & (g - np.maximum(r, b) > 80)
    lab, n = ndimage.label(keyish)
    edge = set(np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))) - {0}
    sizes = ndimage.sum(np.ones_like(lab), lab, range(1, n + 1))
    keep = [i for i in range(1, n + 1) if i in edge or (pocket and sizes[i - 1] > 1500 and _pure(im, lab == i))]
    bg = np.isin(lab, keep)
    alpha = np.array(Image.fromarray(((~bg) * 255).astype(np.uint8)).filter(ImageFilter.MinFilter(3)).filter(ImageFilter.GaussianBlur(0.9))).astype(float) / 255
    green = g - np.maximum(r, b); rim = (alpha < 0.999) & (alpha > 0); out = im.copy()
    out[..., 1] = np.where(rim & (green > 10), np.maximum(r, b) + np.clip(green, 0, 10) * 0.5, g)
    rgba = Image.fromarray(np.dstack([np.clip(out, 0, 255), alpha * 255]).astype(np.uint8), 'RGBA')
    lab2, n2 = ndimage.label(~bg, structure=np.ones((3, 3)))
    comps = [(s[1].start, s[0].start, s[1].stop, s[0].stop) for s in ndimage.find_objects(lab2) if (s[0].stop - s[0].start) * (s[1].stop - s[1].start) > 600]
    comps.sort(key=lambda c: (round(c[1] / 60), c[0]))
    return rgba, comps

def _pure(im, m):  # pocket of nearly exact key colour (not green artwork like leaves or buttons)
    px = im[m]; return px[:, 1].mean() > 220 and px[:, 0].mean() < 40 and px[:, 2].mean() < 40

def save(img, box, path):
    c = img.crop(box); c = c.crop(c.getbbox()); os.makedirs(os.path.dirname(UI + path), exist_ok=True); c.save(UI + path + '.png'); return c

def erase(path, rects, mode='light', radius=6, out=None):
    """Inpaints text inside rects (fractions x0,y0,x1,y1 of the image). mode: 'light' removes white/light glyphs and
    their dark outline, 'dark' removes dark glyphs, 'all' the whole rect."""
    im = Image.open(UI + path + '.png').convert('RGBA'); a = np.array(im); h, w = a.shape[:2]
    bgr = cv2.cvtColor(a[..., :3], cv2.COLOR_RGB2BGR); mask = np.zeros((h, w), np.uint8)
    lum = a[..., :3].astype(int).mean(2); sat = a[..., :3].max(2).astype(int) - a[..., :3].min(2)
    for (fx0, fy0, fx1, fy1) in rects:
        x0, y0, x1, y1 = int(fx0 * w), int(fy0 * h), int(fx1 * w), int(fy1 * h)
        sub = np.zeros((h, w), bool); sub[y0:y1, x0:x1] = True
        if mode == 'all': m = sub
        elif mode == 'dark': m = sub & (lum < 90)
        else: m = sub & (((lum > 185) & (sat < 90)) | (lum < 60))
        mask[m] = 255
    mask = cv2.dilate(mask, np.ones((5, 5), np.uint8), 2)
    res = cv2.inpaint(bgr, mask, radius, cv2.INPAINT_TELEA)
    a[..., :3] = cv2.cvtColor(res, cv2.COLOR_BGR2RGB)
    Image.fromarray(a, 'RGBA').save(UI + (out or path) + '.png')

def rowfill(path, x0f, x1f, y0f, y1f, out=None, smooth=0):
    """Replaces a rectangle by interpolating every row between its left and right edge colours (good for domes,
    ribbons and pills whose shading is mostly vertical)."""
    a = np.array(Image.open(UI + path + '.png').convert('RGBA')).astype(float); h, w = a.shape[:2]
    x0, x1, y0, y1 = int(x0f * w), int(x1f * w), int(y0f * h), int(y1f * h)
    for y in range(y0, y1):
        L, R = a[y, x0].copy(), a[y, x1].copy()
        t = np.linspace(0, 1, x1 - x0 + 1)[:, None]; a[y, x0:x1 + 1] = L * (1 - t) + R * t
    if smooth:
        reg = a[y0:y1, x0:x1 + 1]; a[y0:y1, x0:x1 + 1] = cv2.GaussianBlur(reg, (0, 0), smooth)
    Image.fromarray(np.clip(a, 0, 255).astype(np.uint8), 'RGBA').save(UI + (out or path) + '.png')

def erase2(path, rects, keep, out=None, radius=9, blur=4, grow=3):
    """Inpaints every pixel inside rects that is NOT background colour (keep(rgb)->bool for the plain surface), then
    blends the filled area with a light blur so no strokes remain."""
    a = np.array(Image.open(UI + path + '.png').convert('RGBA')); h, w = a.shape[:2]
    rgb = a[..., :3].astype(int); mask = np.zeros((h, w), np.uint8)
    for (fx0, fy0, fx1, fy1) in rects:
        x0, y0, x1, y1 = int(fx0 * w), int(fy0 * h), int(fx1 * w), int(fy1 * h)
        sub = rgb[y0:y1, x0:x1]; m = ~keep(sub) & (a[y0:y1, x0:x1, 3] > 200)
        mask[y0:y1, x0:x1][m] = 255
    mask = cv2.dilate(mask, np.ones((3, 3), np.uint8), grow)
    bgr = cv2.cvtColor(a[..., :3], cv2.COLOR_RGB2BGR)
    res = cv2.cvtColor(cv2.inpaint(bgr, mask, radius, cv2.INPAINT_NS), cv2.COLOR_BGR2RGB).astype(float)
    soft = cv2.GaussianBlur(res, (0, 0), blur); f = cv2.GaussianBlur((mask / 255.0).astype(float), (0, 0), 3)[..., None]
    a[..., :3] = np.clip(res * (1 - f) + soft * f, 0, 255).astype(np.uint8)
    Image.fromarray(a, 'RGBA').save(UI + (out or path) + '.png')

def is_purple(c):  # ribbon / dome surface
    r, g, b = c[..., 0], c[..., 1], c[..., 2]
    return (b > g + 50) & (r > g + 20) & (b > 90) & (b < 250)

def is_lavender(c):
    l = c.mean(2); return (l > 195) & (c.max(2) - c.min(2) < 70)

def domefill(path, x0f, x1f, y0f, y1f, out=None, margin=6, blur=2.5):
    """Like rowfill, but each row interpolates between the dome's own purple edges (found per row), for round shapes."""
    a = np.array(Image.open(UI + path + '.png').convert('RGBA')).astype(float); h, w = a.shape[:2]
    x0, x1, y0, y1 = int(x0f * w), int(x1f * w), int(y0f * h), int(y1f * h)
    pur = is_purple(a[..., :3].astype(int))
    done = np.zeros((h, w), bool)
    for y in range(y0, y1):
        xs = np.where(pur[y, x0:x1])[0]
        if len(xs) < 10: continue
        L, R = x0 + xs.min() + margin, x0 + xs.max() - margin
        if R - L < 6: continue
        cl, cr = a[y, L].copy(), a[y, R].copy(); t = np.linspace(0, 1, R - L + 1)[:, None]
        a[y, L:R + 1] = cl * (1 - t) + cr * t; done[y, L:R + 1] = True
    soft = cv2.GaussianBlur(a, (0, 0), blur); a[done] = soft[done]
    Image.fromarray(np.clip(a, 0, 255).astype(np.uint8), 'RGBA').save(UI + (out or path) + '.png')
