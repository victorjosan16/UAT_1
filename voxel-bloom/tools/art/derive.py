import os
ROOT=os.environ.get('VB_ROOT', os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..')))
from PIL import Image, ImageDraw, ImageFilter
import numpy as np
D=ROOT+'/Assets/VoxelBloom/Art/UI/Resources/VBUI/'
def load(p): return np.array(Image.open(D+p).convert('RGBA')).astype(float)
def save(a,p): Image.fromarray(np.clip(a,0,255).astype(np.uint8),'RGBA').save(D+p)
def rowfill(a,L,R,y0=0,y1=None):
    h=a.shape[0]; y1=h if y1 is None else y1
    for y in range(y0,y1):
        l=a[y,L].copy(); r=a[y,R].copy()
        if l[3]<250 or r[3]<250: continue
        t=np.linspace(0,1,R-L+1)[:,None]
        a[y,L:R+1]=l*(1-t)+r*t
    return a
save(rowfill(load('HUD/pill_coins.png'),90,212),'HUD/pill_coins_blank.png')
save(rowfill(load('HUD/pill_gems.png'),95,184),'HUD/pill_gems_blank.png')
save(rowfill(load('HUD/pill_hearts.png'),106,202),'HUD/pill_hearts_blank.png')
save(rowfill(load('Buttons/level_badge.png'),116,281),'Buttons/level_badge_blank.png')
save(rowfill(load('HUD/caption_pill.png'),16,260),'HUD/caption_pill_blank.png')
save(rowfill(load('HUD/percent_pill.png'),16,123),'HUD/percent_pill_blank.png')
# carousel tile frames without the baked model
def tile(src,dst,L,y0,yb):
    a=load(src); h,w=a.shape[:2]
    save(rowfill(a,L,w-1-L,y0,h-yb),dst)
a=load('HUD/progress_bar.png'); h,w=a.shape[:2]
tr=a.copy()
for x in range(w-60): tr[:,x]=a[:,w-60]
for x in range(40): tr[:,x]=a[:,w-1-x]
save(tr,'HUD/progress_track.png')
fill=tr.copy(); gcol=a[:,140]
fill[...,:3]=gcol[:,None,:3]
save(fill,'HUD/progress_fill.png')
s=Image.open(D+'Icons/stars_strip.png')
for n,(x0,x1) in {'star_gold':(0,64),'star_empty':(128,192),'lightning':(252,309)}.items():
    c=s.crop((x0,0,x1,s.height)); c=c.crop(c.getbbox()); c.save(D+'Icons/'+n+'.png')
