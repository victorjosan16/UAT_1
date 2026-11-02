# Menu screens from Docs/reference/v4 (boosters, events, settings, how to play): the whole keyed picture is the
# screen; variable texts (prices, wallet values, progress, timers, toggles) are removed here and drawn live in Unity.
import sys; sys.path.insert(0, __import__('os').path.dirname(__file__))
from keylib import *

def page(name):
    rgba, _ = key(ROOT + '/Docs/reference/v4/' + name + '.png'); os.makedirs(UI + 'Menus', exist_ok=True)
    rgba.save(UI + 'Menus/' + name + '.png'); return 'Menus/' + name

def px_rowfill(path, x0, y0, x1, y1, blur=2.0):
    im = Image.open(UI + path + '.png'); w, h = im.size
    rowfill(path, x0 / w, x1 / w, y0 / h, y1 / h, None, blur)

# ---- boosters: 3 rows used (Reîmprospătează, Amestecă culorile, Adaugă o tavă); rows 4-5 removed
b = page('boosters')
wa = np.array(Image.open(UI + b + '.png')); wa[28:236, 655:1000, 3] = 0; Image.fromarray(wa, 'RGBA').save(UI + b + '.png')  # painted wallet: replaced by live pills
rows = [366, 588, 804, 1013, 1222]
for i, c in enumerate(rows[:3]):
    px_rowfill(b, 290, c - 88, 664, c + 78)                                    # name + description
    px_rowfill(b, 792, c - 40, 926, c + 56, 2.5)                               # price digits (coin stays)
# only 3 boosters exist in the game: cut rows 4-5 out and close the panel (its sides are vertical)
im = Image.open(UI + b + '.png'); w, h = im.size
cut0, cut1 = 912, 1324
out = Image.new('RGBA', (w, h - (cut1 - cut0)))
out.paste(im.crop((0, 0, w, cut0)), (0, 0)); out.paste(im.crop((0, cut1, w, h)), (0, cut0))
out.save(UI + b + '.png')
# ---- events: progress bars and timers become live
e = page('events')
for (y0, y1) in [(350, 402), (577, 629), (1034, 1086)]: px_rowfill(e, 322, y0, 758, y1, 1.5)
for (y0, y1) in [(806, 864), (1258, 1314)]: px_rowfill(e, 478, y0, 700, y1, 1.5)
# ---- settings: toggles and the cloud status become live
s = page('settings')
for y in [300, 412, 527]: px_rowfill(s, 742, y - 46, 912, y + 46, 2)
px_rowfill(s, 612, 728, 870, 795, 2)
# ---- how to play: static
page('howto')
print('ok')
