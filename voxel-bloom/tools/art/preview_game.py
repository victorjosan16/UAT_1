# Phone preview of the gameplay screen with the supplied art, using the same layout numbers as ConveyorScreen.cs.
# State comes from a level's witness (node /tmp/claude-0/state.mjs <level> <seconds>). NOT a Unity screenshot.
import json, sys, os
import numpy as np
from PIL import Image, ImageDraw, ImageFont
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
U = ROOT + '/Assets/VoxelBloom/Art/UI/Resources/VBUI/'; BGD = ROOT + '/Assets/VoxelBloom/Art/Backgrounds/Resources/VBUI/Backgrounds/'
S = json.load(open(sys.argv[1])); out = sys.argv[2]; SW, SH = 1080, 2374
F = lambda n: ImageFont.truetype(ROOT + '/Assets/VoxelBloom/Art/Resources/VoxelBloom/Fonts/Baloo2-ExtraBold.ttf', int(n))
hexc = lambda h: tuple(int(h[i:i + 2], 16) for i in (1, 3, 5))
c = Image.new('RGBA', (SW, SH), (42, 35, 102, 255))
bg = Image.open(BGD + 'theme_%02d.png' % (((S['id'] - 1) // 5) % 10)).convert('RGBA')
sc = max(SW / bg.width, SH / bg.height); bg = bg.resize((round(bg.width * sc), round(bg.height * sc)), Image.LANCZOS)
c.alpha_composite(bg.crop(((bg.width - SW) // 2, (bg.height - SH) // 2, (bg.width - SW) // 2 + SW, (bg.height - SH) // 2 + SH)))
shade = Image.new('RGBA', (SW, SH)); sd = np.zeros((SH, SW, 4), np.uint8); t = np.linspace(0, 1, SH)[:, None]
sd[..., 0] = 26; sd[..., 1] = 16; sd[..., 2] = 80; sd[..., 3] = (255 * (0.35 + 0.25 * t)).astype(np.uint8); c.alpha_composite(Image.fromarray(sd, 'RGBA'))
d = ImageDraw.Draw(c)
def art(p): return Image.open(U + p + '.png').convert('RGBA')
def put(img, cx, cy, w, h=None, rot=0):
    h = h or w * img.height / img.width; im = img.resize((max(1, round(w)), max(1, round(h))), Image.LANCZOS)
    if rot: im = im.rotate(rot, expand=True)
    c.alpha_composite(im, (round(cx - im.width / 2), round(cy - im.height / 2)))
def nine(img, w, h, b, s):
    W0, H0 = img.size; bs = max(1, round(b * s)); w, h = round(w), round(h); o = Image.new('RGBA', (w, h))
    xs = [(0, b, 0, bs), (b, W0 - b, bs, w - bs), (W0 - b, W0, w - bs, w)]; ys = [(0, b, 0, bs), (b, H0 - b, bs, h - bs), (H0 - b, H0, h - bs, h)]
    for (sx0, sx1, dx0, dx1) in xs:
        for (sy0, sy1, dy0, dy1) in ys:
            if dx1 > dx0 and dy1 > dy0: o.paste(img.crop((round(sx0), round(sy0), round(sx1), round(sy1))).resize((dx1 - dx0, dy1 - dy0), Image.LANCZOS), (dx0, dy0))
    return o
def rr(cx, cy, w, h, r, col): d.rounded_rectangle([cx - w / 2, cy - h / 2, cx + w / 2, cy + h / 2], max(1, r), fill=col)
def glossy(cx, cy, w, h, col, a=255):
    cell = Image.new('RGBA', (round(w), round(h))); cd = ImageDraw.Draw(cell); r = min(w, h) * 0.22
    cd.rounded_rectangle([0, 0, w - 1, h - 1], r, fill=col + (a,))
    g = np.array(cell).astype(float); yy = np.linspace(0, 1, cell.height)[:, None]
    g[..., :3] = np.clip(g[..., :3] * (1.15 - 0.35 * yy)[..., None] + (yy < 0.35)[..., None] * 40, 0, 255); c.alpha_composite(Image.fromarray(g.astype(np.uint8)), (round(cx - w / 2), round(cy - h / 2)))
# HUD
put(art('Buttons/game_pause'), 80, 75, 100, 100); put(art('Buttons/game_settings'), 1000, 75, 100, 100)
put(art('HUD/game_level_pill_blank'), 540 - 170, 75, 318)
d.text((540 - 170, 72), 'Nivel ', fill='white', font=F(50), anchor='rm'); d.text((540 - 170, 72), str(S['id']), fill='#FFD84A', font=F(50), anchor='lm')
rr(705, 75, 330, 88, 44, (30, 24, 72)); f = S['completed'] / S['total']; rr(552 + (306 * f + 10) / 2, 75, 306 * f + 10, 60, 30, (69, 216, 90))
d.text((705, 72), '%d/%d' % (S['completed'], S['total']), fill='white', font=F(46), anchor='mm', stroke_width=2, stroke_fill='#0E1430')
# board
W, H, R = S['W'], S['H'], S['R']; maxH = SH - 150 - 220 - 252
BW = min(1020, maxH * (W + 2) / (H + 2)); K = BW / (W + 2); bh = (H + 2) * K; avail = SH - 370
topY = 150 + max(0, (avail - (bh + 22 + 230)) / 2); bcx, bcy = 540, topY + bh / 2
P = lambda g: (bcx + (g[0] + 1) * K - BW / 2, bcy - (bh / 2 - (g[1] + 1) * K))
mx, my = P((W / 2, H / 2))
rr(mx, my, (W + 2) * K, (H + 2) * K, (R + 1) * K, (34, 28, 82)); rr(mx, my, (W - 1) * K, (H - 1) * K, (R - 0.5) * K, (43, 36, 102))
meta = json.load(open(U + 'Board/belt_frame.json')); s = R * K / meta['cornerRadius']; ci = meta['centreInset']
fr = nine(art('Board/belt_frame'), W * K + 2 * ci * s, H * K + 2 * ci * s, ci + meta['cornerRadius'] + 12, s); c.alpha_composite(fr, (round(mx - fr.width / 2), round(my - fr.height / 2)))
for st in S['stations']:
    vert = st['side'] in ('top', 'bottom'); c0, cN = P(st['comps'][0]), P(st['comps'][-1]); cx, cy = (c0[0] + cN[0]) / 2, (c0[1] + cN[1]) / 2
    L = (st['cap'] * 1.7 + 0.24) * K; Wd = 1.84 * K; w, h = (Wd, L) if vert else (L, Wd)
    lk = st['lock']
    if lk and lk['type'] == 'completed':
        put(art('Board/lock_tile'), cx, cy, L, Wd, 90 if vert else 0); rr(cx, cy, 1.05 * K, 1.05 * K, 10, (44, 42, 74))
        d.text((cx, cy - 2), str(max(1, lk['n'] - S['completed'])), fill='white', font=F(0.85 * K), anchor='mm'); continue
    if lk: put(art('Board/plus_tile'), cx, cy, L, Wd, 90 if vert else 0); continue
    rr(cx, cy, w, h, 0.3 * K, (218, 210, 247)); rr(cx, cy, w - 0.2 * K, h - 0.2 * K, 0.24 * K, (56, 48, 112))
    if st['tray']:
        for k, ci2 in enumerate(st['tray']['colors']):
            px, py = P(st['comps'][k]); col = hexc(S['colors'][ci2]); filled = k < st['tray']['filled']
            if not filled: col = tuple(int(v + (255 - v) * 0.25) for v in col)
            glossy(px, py, 1.5 * K, 1.5 * K, col, 255 if filled else 128)
cw = art('Board/cube_white')
for x, y, ci2 in S['cubes']:
    px, py = P((x, y)); a = np.array(cw.resize((round(0.98 * K), round(0.98 * K * cw.height / cw.width)), Image.LANCZOS)).astype(float)
    a[..., :3] *= np.array(hexc(S['colors'][ci2])) / 255; c.alpha_composite(Image.fromarray(a.astype(np.uint8)), (round(px - a.shape[1] / 2), round(py - a.shape[0] / 2)))
fx, fy = P((S['feeder'][0] - 0.55, S['feeder'][1] - 0.55))
if S['hopper']: d.text((fx, fy), '+%d' % S['hopper'], fill='white', font=F(0.6 * K), anchor='mm', stroke_width=2, stroke_fill='#22185E')
if S['tier'] != 1:
    tc = [(63, 203, 76), (108, 92, 231), (255, 154, 46), (255, 77, 94)][S['tier']]; tw = 300 if S['tier'] == 3 else 200
    rr(540, topY + 6, tw, 62, 30, tc); d.text((540, topY + 4), S['tierName'].upper(), fill='white', font=F(36), anchor='mm')
# queue
qy = topY + bh + 22 + 115; slotW = 1020 / 7; cwid = min(132, slotW - 14); chh = 214; n = S['visible']; card = art('Board/queue_card_empty')
for i in range(n):
    x = 540 - (n - 1) * slotW / 2 + i * slotW; cimg = nine(card, cwid, chh, 30, 1.0)
    if i >= len(S['queue']): cimg.putalpha(cimg.getchannel('A').point(lambda v: int(v * 0.35)))
    c.alpha_composite(cimg, (round(x - cwid / 2), round(qy - chh / 2)))
    if i < len(S['queue']):
        for r, ci2 in enumerate(S['queue'][i]): glossy(x, qy - chh / 2 + 9 + 12 + 43.5 * (r + 0.5), cwid - 34, 37.5, hexc(S['colors'][ci2]))
if S['hidden']: d.text((1050, qy - 141), '+%d tăvi în așteptare' % S['hidden'], fill=(255, 255, 255, 190), font=F(34), anchor='rm')
# boosters
for i, p in enumerate(['Boosters/refresh', 'Boosters/color_swap', 'Boosters/add_tray']):
    x, y = 150 + (i + 0.5) * 260, SH - 24 - 85; put(art(p), x, y, 170, 165)
    bx, by = x + 85 - 33, y - 82.5 + 30; d.ellipse([bx - 33, by - 33, bx + 33, by + 33], fill='white'); d.ellipse([bx - 28, by - 28, bx + 28, by + 28], fill=(232, 32, 46))
    d.text((bx, by - 2), '3', fill='white', font=F(34), anchor='mm')
c.convert('RGB').save(out)
