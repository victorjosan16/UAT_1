#!/usr/bin/env python3
"""Approximate composition preview of screens using the imported art (NOT a Unity screenshot).
Mirrors the in-game recipe: cover-fit background, navy veil + edge gradients, authored glass behind the
area-filled liquid with a 38% copy in front, authored booster buttons.
Usage: python3 -I compose_preview.py <unity_project_root> <out_dir>"""
import json, os, sys
from PIL import Image, ImageDraw, ImageFont

ROOT, OUT = sys.argv[1], sys.argv[2]
ART = os.path.join(ROOT, 'Assets/_Veliqora/Resources/Veliqora/Art')
LEVELS = json.load(open(os.path.join(ROOT, 'Assets/_Veliqora/Resources/Veliqora/Levels/levels.json')))['levels']
SHAPES = json.load(open(os.path.join(ART, 'Bottles/bottle_shapes.json')))
PALETTE = ['#3D8BFF', '#FF4FA3', '#36C46A', '#FFD23F', '#8E5CF7', '#FF8A2B', '#2ED3E6', '#E8364F']
W, H = 540, 960
os.makedirs(OUT, exist_ok=True)

def cover(img, w, h):
    s = max(w / img.width, h / img.height)
    img = img.resize((int(img.width * s + 1), int(img.height * s + 1)), Image.LANCZOS)
    x, y = (img.width - w) // 2, (img.height - h) // 2
    return img.crop((x, y, x + w, y + h)).convert('RGBA')

def veil(canvas, overall, top, bottom):
    ov = Image.new('RGBA', canvas.size, (0, 0, 0, 0))
    px = ov.load()
    for y in range(canvas.height):
        v = 1 - y / (canvas.height - 1)
        a = max(bottom * max(0, 1 - v / 0.32) ** 1.6, top * max(0, (v - 0.72) / 0.28) ** 1.6)
        a = 1 - (1 - a) * (1 - overall)
        for x in range(canvas.width): px[x, y] = (10, 13, 38, int(a * 255))
    canvas.alpha_composite(ov)

def clip_below(poly, c):
    out = []
    for i in range(len(poly)):
        a, b = poly[i], poly[(i + 1) % len(poly)]
        ia, ib = a[1] <= c, b[1] <= c
        if ia: out.append(a)
        if ia != ib:
            t = (c - a[1]) / (b[1] - a[1]); out.append((a[0] + (b[0] - a[0]) * t, c))
    return out

def area(p):
    return abs(sum(p[i - 1][0] * p[i][1] - p[i][0] * p[i - 1][1] for i in range(len(p)))) / 2 if len(p) > 2 else 0

def level_for(poly, frac):
    total = area(poly); lo, hi = min(p[1] for p in poly), max(p[1] for p in poly)
    for _ in range(30):
        mid = (lo + hi) / 2
        if area(clip_below(poly, mid)) < frac * total: lo = mid
        else: hi = mid
    return (lo + hi) / 2

def bottle(canvas, art, units, cap, cx, by, bw):
    glass = Image.open(os.path.join(ART, 'Bottles', art + '.png')).convert('RGBA')
    asp = SHAPES[art]['aspect']; bh = int(bw * asp)
    g = glass.resize((int(bw), bh), Image.LANCZOS)
    x0, y0 = int(cx - bw / 2), int(by - bh)
    canvas.alpha_composite(g, (x0, y0))
    poly = [tuple(p) for p in SHAPES[art]['interior']]
    liq = Image.new('RGBA', canvas.size, (0, 0, 0, 0)); d = ImageDraw.Draw(liq)
    lower = min(p[1] for p in poly) - 1; cum = 0; i = 0
    while i < len(units):
        c = units[i]; run = 0
        while i < len(units) and units[i] == c: run += 1; i += 1
        cum += run
        upper = level_for(poly, cum / cap)
        band = clip_below(poly, upper)
        band = [(x, y) for x, y in band]
        # keep only above lower
        inv = [(x, -y) for x, y in band]
        band = [(x, -y) for x, y in clip_below(inv, -lower)]
        if len(band) > 2:
            d.polygon([(cx + x * bw, by - y * bw) for x, y in band], fill=PALETTE[(c - 1) % 8])
        lower = upper
    canvas.alpha_composite(liq)
    front = g.copy(); front.putalpha(front.getchannel('A').point(lambda a: int(a * 0.38)))
    canvas.alpha_composite(front, (x0, y0))

def paste(canvas, rel, cx, cy, w):
    im = Image.open(os.path.join(ART, rel)).convert('RGBA')
    h = int(im.height * w / im.width); im = im.resize((int(w), h), Image.LANCZOS)
    canvas.alpha_composite(im, (int(cx - w / 2), int(cy - h / 2)))

def font(sz):
    for f in ('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf',):
        if os.path.exists(f): return ImageFont.truetype(f, sz)
    return ImageFont.load_default()

def gameplay(theme, art, level_no, name):
    c = cover(Image.open(os.path.join(ART, 'Backgrounds', theme + '.png')), W, H)
    veil(c, 0.38, 0.55, 0.6)
    lvl = LEVELS[level_no - 1]
    n = len(lvl['bottles']); rows = 1 if n <= 5 else 2; per = -(-n // rows)
    asp = SHAPES[art]['aspect']
    bw = min(W / (per * 1.5 + 0.4), (H * 0.52) / (rows * asp * 1.32))
    idx = 0
    for r in range(rows):
        inrow = min(per, n - idx); roww = inrow * bw + (inrow - 1) * bw * 0.5
        by = H * 0.32 + (r + 1) * bw * asp * 1.32
        for k in range(inrow):
            bottle(c, art, lvl['bottles'][idx], lvl['capacity'], W / 2 - roww / 2 + k * bw * 1.5 + bw / 2, by, bw); idx += 1
    d = ImageDraw.Draw(c)
    paste(c, 'Buttons/Pause.png', 50, 52, 58)
    paste(c, 'Icons/Coin.png', 400, 52, 46)
    d.rounded_rectangle((420, 34, 520, 70), 18, fill=(15, 20, 60, 150)); d.text((440, 40), '1,250', font=font(20), fill='white')
    d.text((W / 2 - 60, 36), 'Level %d' % level_no, font=font(26), fill='white')
    for i, b in enumerate(('Undo', 'BottlePlus', 'Shuffle', 'Bulb')):
        x = W * (0.125 + 0.25 * i)
        paste(c, 'Buttons/%s.png' % b, x, H - 95, 84)
        d.ellipse((x + 20, H - 145, x + 50, H - 115), fill='#FF7A6B', outline='white', width=2)
        d.text((x + 30, H - 138), str([3, 1, 1, 2][i]), font=font(16), fill='white')
        d.text((x - 30, H - 45), ['Undo', 'Add bottle', 'Shuffle', 'Hint'][i], font=font(15), fill='white')
    c.convert('RGB').save(os.path.join(OUT, name))

def home():
    c = cover(Image.open(os.path.join(ART, 'Backgrounds/screen_home.png')), W, H)
    veil(c, 0.0, 0.35, 0.5)
    d = ImageDraw.Draw(c)
    d.text((W / 2 - 150, 230), 'VELIQORA', font=font(56), fill='#FFD15A', stroke_width=4, stroke_fill='#8A4B22')
    sign = Image.open(os.path.join(ART, 'Buttons/sign_wood.png')).convert('RGBA').resize((330, 66), Image.LANCZOS)
    c.alpha_composite(sign, (W // 2 - 165, 300))
    d.text((W / 2 - 128, 320), 'COLOR SORTING ADVENTURE', font=font(15), fill='white', stroke_width=2, stroke_fill='#5A2E10')
    pill = Image.open(os.path.join(ART, 'Buttons/pill_green.png')).convert('RGBA').resize((300, 84), Image.LANCZOS)
    c.alpha_composite(pill, (W // 2 - 150, 600)); d.text((W / 2 - 50, 625), 'PLAY', font=font(32), fill='white', stroke_width=2, stroke_fill='#1D6B2A')
    c.convert('RGB').save(os.path.join(OUT, 'compose_home.png'))

gameplay('theme_night', 'glass_cork', 45, 'compose_gameplay_night_cork.png')
gameplay('theme_garden', 'glass_milk', 120, 'compose_gameplay_garden_milk.png')
home()
print('composition previews ->', OUT)
