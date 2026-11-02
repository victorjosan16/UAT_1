# Splits Docs/reference/level-backgrounds.png (5x2 grid of rounded cards on white) into 10 level themes.
# The rounded corners are cut away by insetting each card; themes are numbered row by row.
import os
from PIL import Image
ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
im = Image.open(ROOT + '/Docs/reference/level-backgrounds.png').convert('RGB')
cols = [(2, 306), (311, 609), (614, 921), (926, 1227), (1232, 1533)]; rows = [(4, 509), (514, 1020)]
OUT = ROOT + '/Assets/VoxelBloom/Art/Backgrounds/Resources/VBUI/Backgrounds/'
NAMES = ['cascada', 'plaja', 'toamna', 'iarna', 'dulciuri', 'pestera', 'ruine', 'ferma', 'munte', 'oras']
i = 0
for (y0, y1) in rows:
    for (x0, x1) in cols:
        tile = im.crop((x0 + 14, y0 + 14, x1 - 14, y1 - 14))
        tile.save(OUT + 'theme_%02d.png' % i); print(i, NAMES[i], tile.size); i += 1
