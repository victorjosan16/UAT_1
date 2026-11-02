# Win screen and level map pieces from Docs/reference/v4 (green sheets). Text and digits baked into the art are
# removed with OpenCV inpainting so live values can be drawn on top.
import sys; sys.path.insert(0, __import__('os').path.dirname(__file__))
from keylib import *
NONE = lambda c: np.zeros(c.shape[:2], bool)

# ---------------- win
win, wc = key(ROOT + '/Docs/reference/v4/win.png')
save(win, (118, 20, 802, 309), 'Win/stars3')
save(win, (110, 296, 806, 604), 'Win/banner')
save(win, (150, 525, 752, 955), 'Win/cubes_podium')
save(win, (804, 268, 1446, 526), 'Win/reward_card')
save(win, (809, 554, 1427, 737), 'Win/next')
save(win, (850, 755, 1373, 903), 'Win/replay')
save(win, (1285, 63, 1452, 234), 'Buttons/close_round')
pass  # the empty ribbon comes from the home sheet (Collection/win_banner)
rowfill('Win/reward_card', 0.25, 0.505, 0.44, 0.88, 'Win/reward_card_blank', 1.5); rowfill('Win/reward_card_blank', 0.752, 0.945, 0.44, 0.88, None, 1.5)

# ---------------- map
mp, mc = key(ROOT + '/Docs/reference/v4/map.png')
mp.save(UI + 'Map/map_full.png') if os.path.isdir(UI + 'Map') else (os.makedirs(UI + 'Map'), mp.save(UI + 'Map/map_full.png'))
def node(box, path, digit_rect=None):
    c = mp.crop(box); w, h = c.size
    # soft elliptical mask so the piece blends over the painted node of any position
    yy, xx = np.mgrid[0:h, 0:w]; d = ((xx - w / 2) / (w * 0.5)) ** 2 + ((yy - h / 2) / (h * 0.5)) ** 2
    m = np.clip((1.0 - d) / 0.12, 0, 1)
    a = np.array(c).astype(float); a[..., 3] = a[..., 3] * m
    Image.fromarray(a.astype(np.uint8), 'RGBA').save(UI + path + '.png')
    if digit_rect: domefill(path, digit_rect[0], digit_rect[2], digit_rect[1], digit_rect[3], path + '_blank')
# remove the pink glow of the painted "current" node 5 around its dome (the dome is always covered by an overlay)
mk = np.array(mp); x0, y0, x1, y1 = 455, 610, 785, 880
sub = mk[y0:y1, x0:x1, :3].astype(int); r_, g_, b_ = sub[..., 0], sub[..., 1], sub[..., 2]
yy, xx = np.mgrid[y0:y1, x0:x1]; inside = ((xx - 619) / 118.0) ** 2 + ((yy - 740) / 96.0) ** 2 < 1
mask = np.zeros(mk.shape[:2], np.uint8); mask[y0:y1, x0:x1][((r_ > 170) & (b_ > 170) & (g_ < 175) | ((r_ > 230) & (b_ > 230) & (g_ > 200))) & ~inside] = 255
mask = cv2.dilate(mask, np.ones((5, 5), np.uint8), 2)
res = cv2.inpaint(cv2.cvtColor(mk[..., :3], cv2.COLOR_RGB2BGR), mask, 9, cv2.INPAINT_TELEA)
mk[..., :3] = cv2.cvtColor(res, cv2.COLOR_BGR2RGB); mp = Image.fromarray(mk, 'RGBA'); mp.save(UI + 'Map/map_full.png')
node((205, 585, 435, 785), 'Map/node_open', (0.08, 0.06, 0.92, 0.63))
mp = Image.open(ROOT + '/Docs/reference/v4/map.png') and key(ROOT + '/Docs/reference/v4/map.png')[0]
node((488, 640, 750, 840), 'Map/node_current', (0.12, 0.1, 0.88, 0.6))
mp = Image.open(UI + 'Map/map_full.png')
node((738, 812, 950, 1005), 'Map/node_locked')
print('ok')
