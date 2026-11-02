# Previews (NOT Unity screenshots) of the win screen and the level map page 1 with the same layout as the code.
import os
from PIL import Image, ImageDraw, ImageFont
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..')); U = ROOT + '/Assets/VoxelBloom/Art/UI/Resources/VBUI/'
F = lambda n: ImageFont.truetype(ROOT + '/Assets/VoxelBloom/Art/Resources/VoxelBloom/Fonts/Baloo2-ExtraBold.ttf', int(n))
W, H = 1080, 2374
def art(p): return Image.open(U + p + '.png').convert('RGBA')
def put(c, im, cx, cy, w):
    h = w * im.height / im.width; im = im.resize((round(w), round(h)), Image.LANCZOS); c.alpha_composite(im, (round(cx - w / 2), round(cy - h / 2))); return (cx - w / 2, cy - h / 2, w, h)
# win
c = Image.new('RGBA', (W, H), (60, 40, 150, 255)); d = ImageDraw.Draw(c)
top = (H - 1760) / 2
bx, by, bw, bh = put(c, art('Collection/win_banner'), 540, top + 250, 860)
for i, line in enumerate(['Nivel 7', 'Finalizat!']):
    d.text((540, by + bh * (0.76 - 0.13 + i * 0.13)), line, fill='white', font=F(78), anchor='mm', stroke_width=4, stroke_fill='#3A1582')
put(c, art('Win/cubes_podium'), 540, top + 760, 640)
cx, cy, cw, ch = put(c, art('Win/reward_card_blank'), 540, top + 1140, 780)
d.text((cx + cw * 0.25, cy + ch * 0.66), '+118', fill='#2B1D6B', font=F(64), anchor='lm'); d.text((cx + cw * 0.75, cy + ch * 0.66), '+0', fill='#2B1D6B', font=F(64), anchor='lm')
put(c, art('Win/next'), 540, top + 1390, 640); put(c, art('Win/replay'), 540, top + 1590, 540)
win = c
# map
c = Image.new('RGBA', (W, H)); bg = Image.open(ROOT + '/Assets/VoxelBloom/Art/Backgrounds/Resources/VBUI/Backgrounds/home_background.png').convert('RGBA')
sc = H / bg.height; bg = bg.resize((round(bg.width * sc), H)); c.alpha_composite(bg.crop(((bg.width - W) // 2, 0, (bg.width - W) // 2 + W, H)))
c.alpha_composite(Image.new('RGBA', (W, H), (38, 20, 90, 90)))
m = art('Map/map_full').resize((W, round(W * 1536 / 1024)), Image.LANCZOS); my = (H - m.height) // 2; c.alpha_composite(m, (0, my)); d = ImageDraw.Draw(c)
k = W / 1024; nodes = [(212, 452), (472, 352), (748, 322), (320, 685), (619, 740), (843, 908), (524, 996), (232, 1066)]
for i, (x, y) in enumerate(nodes):
    lv = i + 1; px, py = x * k, my + y * k
    if lv <= 3: put(c, art('Map/node_open_blank'), px, py, 230 * k); d.text((px, py - 30 * k), str(lv), fill='white', font=F(90 * k), anchor='mm', stroke_width=4, stroke_fill='#2A1466')
    elif lv == 4: put(c, art('Map/node_current_blank'), px, py, 262 * k); d.text((px, py - 30 * k), str(lv), fill='white', font=F(90 * k), anchor='mm', stroke_width=4, stroke_fill='#2A1466')
    else: put(c, art('Map/node_locked'), px, py, (270 if i == 4 else 212) * k)
mp = c
ref = [Image.open(ROOT + '/Docs/reference/v4/win.png').convert('RGBA'), Image.open(ROOT + '/Docs/reference/v4/map.png').convert('RGBA')]
h = 1000; ims = [i.resize((round(i.width * h / i.height), h)) for i in (win, mp, ref[1])]
o = Image.new('RGB', (sum(i.width for i in ims) + 40, h), 'white'); x = 0
for i in ims: o.paste(i, (x, 0), i); x += i.width + 20
o.save(ROOT + '/Docs/WinMapCompare.png')
