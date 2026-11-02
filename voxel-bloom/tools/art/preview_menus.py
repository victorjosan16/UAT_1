# Previews (NOT Unity screenshots) of the art menu pages with live overlays at the coordinates used in ArtPages.cs.
import os
from PIL import Image, ImageDraw, ImageFont
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..')); U = ROOT + '/Assets/VoxelBloom/Art/UI/Resources/VBUI/'
F = lambda n: ImageFont.truetype(ROOT + '/Assets/VoxelBloom/Art/Resources/VoxelBloom/Fonts/Baloo2-ExtraBold.ttf', int(n))
def base(name):
    im = Image.open(U + 'Menus/' + name + '.png').convert('RGBA'); c = Image.new('RGBA', im.size, (70, 45, 170, 255)); c.alpha_composite(im); return c
def pic(c, p, x0, y0, x1, y1):
    im = Image.open(U + p + '.png').convert('RGBA'); s = min((x1 - x0) / im.width, (y1 - y0) / im.height); im = im.resize((round(im.width * s), round(im.height * s)))
    c.alpha_composite(im, (round((x0 + x1 - im.width) / 2), round((y0 + y1 - im.height) / 2)))
def bar(c, x0, y0, x1, y1, v, t):
    d = ImageDraw.Draw(c); d.rounded_rectangle([x0, y0 + 4, x0 + (x1 - x0) * v, y1 - 4], (y1 - y0) // 2, fill=(69, 216, 90)); d.text(((x0 + x1) / 2, (y0 + y1) / 2), t, fill='white', font=F(36), anchor='mm', stroke_width=3, stroke_fill='#1A1450')
b = base('boosters'); d = ImageDraw.Draw(b)
pic(b, 'HUD/home_pill_coins_blank', 668, 40, 988, 135); pic(b, 'HUD/home_pill_gems_blank', 690, 140, 988, 225)
d.text((828, 88), '12 450', fill='white', font=F(44), anchor='mm', stroke_width=2, stroke_fill='#0E1430'); d.text((858, 183), '320', fill='white', font=F(44), anchor='mm', stroke_width=2, stroke_fill='#0E1430')
names = ['Reîmprospătează', 'Amestecă culorile', 'Adaugă o tavă']; desc = ['Schimbă ordinea tăvilor din coadă.', 'Rearanjează culorile cuburilor de pe bandă.', 'Deschide o stație în plus.']
for i, c0 in enumerate([366, 588, 804]):
    if i: pic(b, ['', 'Boosters/color_swap', 'Boosters/add_tray'][i], 78, c0 - 96, 272, c0 + 96)
    d.text((300, c0 - 56), names[i], fill='#2B1D6B', font=F(46), anchor='lm'); d.text((300, c0 + 10), desc[i][:24] + ('…' if len(desc[i]) > 24 else ''), fill='#3F2E86', font=F(28), anchor='lm')
    d.text((856, c0 + 8), ['200', '300', '500'][i], fill='white', font=F(52), anchor='mm', stroke_width=3, stroke_fill='#1F6B1F')
e = base('events')
bar(e, 322, 350, 758, 402, 0.6, '3/5'); bar(e, 322, 577, 758, 629, 0.6, '120/200'); bar(e, 322, 1034, 758, 1086, 0.6, '18/30'); d = ImageDraw.Draw(e)
d.text((615, 835), '6z 12h', fill='white', font=F(44), anchor='mm'); d.text((615, 1286), '23h 59m', fill='white', font=F(44), anchor='mm')
s = base('settings'); d = ImageDraw.Draw(s)
for y in (300, 412, 527): d.rounded_rectangle([742 + 8, y - 36, 912 - 8, y + 36], 36, fill=(69, 216, 90)); d.ellipse([912 - 8 - 70, y - 33, 912 - 14, y + 33], fill='white')
d.text((870, 762), 'Oprit', fill='#6A5E9A', font=F(40), anchor='rm')
ims = [b, e, s]; h = 900; ims = [i.resize((round(i.width * h / i.height), h)) for i in ims]
o = Image.new('RGB', (sum(i.width for i in ims) + 40, h), 'white'); x = 0
for i in ims: o.paste(i, (x, 0), i); x += i.width + 20
o.save(ROOT + '/Docs/MenusCompare.png')
