import os
ROOT=os.environ.get('VB_ROOT', os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..')))
from PIL import Image, ImageFilter
import numpy as np, os, json
SRC=ROOT+'/Docs/reference/asset-sheet-green.png'
OUT=ROOT+'/Assets/VoxelBloom/Art/UI/Resources/VBUI'  # then move the reference-only crops to Docs/reference/extracted
im=np.array(Image.open(SRC).convert('RGB')).astype(float); H,W=im.shape[:2]
bg=np.load('/tmp/vb_bg.npy'); keyish=np.load('/tmp/vb_keyish.npy')
r,g,b=im[...,0],im[...,1],im[...,2]
KEY=np.array([4,249,3.])
dist=np.sqrt(((im-KEY)**2).sum(-1))
# enclosed pockets of exact background colour (between artwork) -> transparent too
pocket=(~bg)&(dist<22)
alpha=np.where(bg|pocket,0.0,1.0)
# soft edges: pixels within 2px of transparency get alpha from their greenness, colour despilled
trans=Image.fromarray(((alpha==0)*255).astype(np.uint8)).filter(ImageFilter.MaxFilter(5))
edge=(np.array(trans)>0)&(alpha>0)
green=g-np.maximum(r,b)
a_edge=np.clip(1.0-(green-25)/150.0,0,1)
alpha=np.where(edge,np.minimum(alpha,a_edge),alpha)
rgb=im.copy()
spill=edge&(green>10)
rgb[...,1]=np.where(spill,np.maximum(r,b)+np.clip(green,0,10),g)   # remove the green cast on edges only
rgba=np.dstack([np.clip(rgb,0,255),alpha*255]).astype(np.uint8)
full=Image.fromarray(rgba,'RGBA')
# protected green artwork: alpha only from border-connected background, feathered, no greenness-based edge alpha
soft=np.array(Image.fromarray(((~bg)*255).astype(np.uint8)).filter(ImageFilter.MinFilter(3)).filter(ImageFilter.GaussianBlur(0.8))).astype(float)/255
prgb=im.copy(); ed=(soft<0.99)&(green>10); prgb[...,1]=np.where(ed,np.minimum(g,np.maximum(r,b)*0.6+g*0.4+20),g)
pfull=Image.fromarray(np.dstack([np.clip(prgb,0,255),soft*255]).astype(np.uint8),'RGBA')
from scipy import ndimage
def clean(c):
    a=np.array(c); m=a[...,3]>40
    lab,n=ndimage.label(m,structure=np.ones((3,3)))
    if n<2: return c
    areas=ndimage.sum(m,lab,range(1,n+1)); big=areas.max()
    h,w=m.shape
    for i in range(1,n+1):
        ys,xs=np.where(lab==i)
        touches=ys.min()==0 or xs.min()==0 or ys.max()==h-1 or xs.max()==w-1
        if (touches and areas[i-1]<0.4*big) or areas[i-1]<0.002*big: a[lab==i,3]=0
    return Image.fromarray(a,'RGBA')

PROT={'play','pill_coins','pill_gems','block_green','pill_hearts'}
def save(cat,name,box,blank=False):
    os.makedirs(f'{OUT}/{cat}',exist_ok=True)
    c=(pfull if name in PROT else full).crop(box)
    c=clean(c)
    bb=c.getbbox()
    if bb: c=c.crop(bb)
    if blank: c=blank_block(c)
    c.save(f'{OUT}/{cat}/{name}.png'); return c

def blank_block(c):
    a=np.array(c).astype(float); h,w=a.shape[:2]
    xl,xr=int(w*0.17),int(w*0.85)
    for y in range(h):
        L=a[y,xl].copy(); R=a[y,xr].copy()
        if L[3]<200 or R[3]<200: continue
        for x in range(xl+1,xr):
            t=(x-xl)/(xr-xl); a[y,x,:3]=L[:3]*(1-t)+R[:3]*t; a[y,x,3]=255
    return Image.fromarray(a.astype(np.uint8),'RGBA')

A=[('Logo','logo',(14,4,625,312)),('Backgrounds','fox_pedestal',(586,6,940,334)),('Backgrounds','landscape',(950,4,1530,330)),
 ('HUD','avatar',(8,318,160,440)),('HUD','pill_hearts',(160,342,384,437)),('HUD','pill_coins',(390,340,680,430)),('HUD','pill_gems',(682,342,942,433)),
 ('Buttons','settings',(956,338,1063,442)),('Buttons','back',(1096,335,1207,446)),('Icons','rotate_left',(1247,344,1326,431)),('Icons','label_360',(1326,358,1446,416)),('Icons','rotate_right',(1438,344,1523,431)),
 ('Buttons','play',(6,452,436,606)),('Buttons','level_badge',(442,470,780,590)),
 ('Buttons','nav_collection',(786,448,965,607)),('Buttons','nav_rewards',(967,437,1151,607)),('Buttons','nav_shop',(1154,446,1340,607)),('Buttons','nav_events',(1342,447,1527,607)),
 ('Collection','tile_selected_fox',(0,616,162,772)),('Collection','tile_castle',(166,624,312,766)),('Collection','tile_house',(312,624,462,766)),('Collection','tile_car',(462,624,612,766)),('Collection','tile_castle2',(614,624,766,766)),('Collection','tile_rocket',(766,624,918,766)),
 ('Icons','cube',(16,776,84,848)),('HUD','progress_bar',(86,786,726,836)),('HUD','percent_pill',(738,780,878,848)),('HUD','caption_pill',(290,834,566,882)),
 ('Boosters','hammer',(10,882,150,1019)),('Boosters','shuffle',(150,882,288,1019)),('Boosters','color_bomb',(288,882,425,1019)),('Boosters','remove',(425,882,566,1019)),
 ('Icons','stars_strip',(578,878,890,958))]
for cat,name,box in A: save(cat,name,box)
blocks=[('pink',(926,628,1074,712)),('cyan',(1074,628,1222,712)),('orange',(1222,628,1370,712)),('white',(1370,628,1528,712)),
 ('cyan2',(926,714,1074,798)),('purple',(1074,714,1222,798)),('magenta',(1222,714,1370,798)),('red',(1370,714,1528,798)),
 ('green',(986,800,1150,880)),('yellow',(1150,800,1310,880)),('blue',(1310,800,1474,880)),
 ('violet',(986,884,1150,964)),('brown',(1150,884,1310,964)),('orange2',(1310,884,1474,964))]
info={}
for name,box in blocks:
    box=(box[0]+5,box[1]+5,box[2]-5,box[3]-4)
    c=save('Blocks','block_'+name,box,blank=True)
    a=np.array(c); h,w=a.shape[:2]
    px=a[int(h*0.55),int(w*0.25)][:3]
    info[name]='#%02X%02X%02X'%tuple(int(v) for v in px)
json.dump(info,open(f'{OUT}/Blocks/block_colors.json','w'),indent=1)
print(info)
