# Phone preview of the Home screen (same layout fractions as HomeScreen.cs). NOT a Unity screenshot.
import os, re, sys
from PIL import Image, ImageDraw, ImageFont
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
U = ROOT + '/Assets/VoxelBloom/Art/UI/Resources/VBUI/'
src = open(ROOT + '/Assets/VoxelBloom/Scripts/UI/Screens/HomeScreen.cs').read()
layout = [(m[0], float(m[1]), float(m[2]), float(m[3])) for m in re.findall(r'\("([^"]+)", ([\d.]+)f, ([\d.]+)f, ([\d.]+)f\)', src)]
font = ImageFont.truetype(ROOT + '/Assets/VoxelBloom/Art/Resources/VoxelBloom/Fonts/Baloo2-ExtraBold.ttf', 46)
def render(H, out):
    W = 1080
    bg = Image.open(ROOT + '/Assets/VoxelBloom/Art/Backgrounds/Resources/VBUI/Backgrounds/home_background.png').convert('RGBA')
    sc = max(W / bg.width, H / bg.height); bg = bg.resize((round(bg.width * sc), round(bg.height * sc)))
    ox, oy = (bg.width - W) // 2, (bg.height - H) // 2; c = bg.crop((ox, oy, ox + W, oy + H))
    d = ImageDraw.Draw(c)
    k = min(1.08, max(1.0, (H / W / 1.5) ** 0.5))
    for path, x, y, w in layout:
        hud = path.startswith('HUD/') or path in ('Buttons/home_settings', 'Buttons/home_play', 'Buttons/home_levels') or path.startswith('Logo/')
        im = Image.open(U + path + '.png').convert('RGBA'); pw = w * W * (1 if hud else k); ph = pw * im.height / im.width
        im = im.resize((round(pw), round(ph))); x0, y0 = round(x * W - pw / 2), round(y * H - ph / 2); c.alpha_composite(im, (x0, y0))
        if 'coins' in path: d.text((x0 + pw * 0.52, y0 + ph / 2), '12 450', fill='white', font=font, anchor='mm', stroke_width=2, stroke_fill='#0E1430')
        if 'gems' in path: d.text((x0 + pw * 0.565, y0 + ph / 2), '320', fill='white', font=font, anchor='mm', stroke_width=2, stroke_fill='#0E1430')
    c.save(out); return c
a = render(int(sys.argv[1]) if len(sys.argv) > 1 else 2374, '/tmp/claude-0/home_phone.png')
b = render(1620, '/tmp/claude-0/home_23.png')
m = Image.open(ROOT + '/Docs/reference/home-mockup-v3.png').convert('RGBA')
h = 1000; ims = [x.resize((round(x.width * h / x.height), h)) for x in (a, b, m)]
o = Image.new('RGB', (sum(i.width for i in ims) + 40, h), 'white'); x = 0
for i in ims: o.paste(i, (x, 0), i); x += i.width + 20
o.save(ROOT + '/Docs/HomeMockupCompare.png')
