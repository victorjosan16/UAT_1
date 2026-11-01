import os
ROOT=os.environ.get('VB_ROOT', os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..')))
from PIL import Image, ImageDraw
import numpy as np, json
from collections import deque
src=Image.open(ROOT+'/Docs/reference/asset-sheet-green.png').convert('RGB')
im=np.array(src).astype(float); H,W=im.shape[:2]
r,g,b=im[...,0],im[...,1],im[...,2]
keyish=(g>140)&(g-np.maximum(r,b)>80)
# flood from border over keyish pixels (8-connected to cross thin anti-aliased gaps)
bg=np.zeros((H,W),bool); q=deque()
for x in range(W):
    for y in (0,1,H-2,H-1):
        if keyish[y,x] and not bg[y,x]: bg[y,x]=1; q.append((y,x))
for y in range(H):
    for x in (0,1,W-2,W-1):
        if keyish[y,x] and not bg[y,x]: bg[y,x]=1; q.append((y,x))
while q:
    cy,cx=q.popleft()
    for dy in (-1,0,1):
        for dx in (-1,0,1):
            ny,nx=cy+dy,cx+dx
            if 0<=ny<H and 0<=nx<W and keyish[ny,nx] and not bg[ny,nx]:
                bg[ny,nx]=1; q.append((ny,nx))
print('bg frac',bg.mean(),'keyish frac',keyish.mean(),'enclosed keyish',(keyish&~bg).sum())
np.save('/tmp/vb_bg.npy',bg); np.save('/tmp/vb_keyish.npy',keyish)
fg=~bg; seen=np.zeros_like(fg); comps=[]
for y in range(0,H,2):
    for x in range(0,W,2):
        if fg[y,x] and not seen[y,x]:
            qq=deque([(y,x)]); seen[y,x]=1; minx=maxx=x; miny=maxy=y; n=0
            while qq:
                cy,cx=qq.popleft(); n+=1
                if cx<minx:minx=cx
                if cx>maxx:maxx=cx
                if cy<miny:miny=cy
                if cy>maxy:maxy=cy
                for dy,dx in ((1,0),(-1,0),(0,1),(0,-1)):
                    ny,nx=cy+dy,cx+dx
                    if 0<=ny<H and 0<=nx<W and fg[ny,nx] and not seen[ny,nx]:
                        seen[ny,nx]=1; qq.append((ny,nx))
            if n>150: comps.append((n,minx,miny,maxx,maxy))
comps.sort(key=lambda c:(c[2]//40,c[1]))
for i,c in enumerate(comps): print(i,c)
prev=src.copy(); d=ImageDraw.Draw(prev)
for i,c in enumerate(comps): d.rectangle(c[1:],outline=(255,0,0),width=2); d.text((c[1]+3,c[2]+3),str(i),fill=(255,0,0))
prev.save('/tmp/vb_boxes.png')
json.dump(comps,open('/tmp/vb_comps.json','w'))
