import os
ROOT=os.environ.get('VB_ROOT', os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..')))
from PIL import Image, ImageDraw, ImageFilter
import numpy as np
D=ROOT+'/Assets/VoxelBloom/Art/UI/Resources/VBUI/Blocks/'
S=4; W,H=400,220; R=62; LIP=22
def rr(box,r,fill=255):
    m=Image.new('L',(W*S,H*S),0); ImageDraw.Draw(m).rounded_rectangle([v*S for v in box],r*S,fill=fill); return m
def down(m): return np.array(m.resize((W,H),Image.LANCZOS)).astype(float)/255
outer=down(rr((0,0,W-1,H-1),R))
face=down(rr((0,0,W-1,H-1-LIP),R))
# face template (tinted by the voxel colour): body = 1.0 exactly, lip = 0.66, thin dark edge
y=np.linspace(0,1,H)[:,None]*np.ones((1,W))
val=np.where(face>0.5,1.0,0.66)
edge=np.clip(outer-down(rr((3,3,W-4,H-4),R-3)),0,1)
val=val*(1-0.35*edge)
# soft shading on the lower part of the face (keeps the centre exact)
fy=y*H/(H-LIP)
val=np.where(face>0.5,val*np.where(fy>0.72,1-0.18*(fy-0.72)/0.28,1),val)
img=np.dstack([val*255]*3+[outer*255])
Image.fromarray(np.clip(img,0,255).astype(np.uint8),'RGBA').save(D+'block_face.png')
# gloss overlay (untinted white): top highlight band, inner rim, specular dot
g=np.zeros((H,W))
band=down(rr((14,10,W-15,(H-LIP)*0.5),R-16))
g+=band*np.clip(0.55-0.5*(y*H/((H-LIP)*0.5)),0,1)
rim=np.clip(down(rr((7,6,W-8,H-LIP-7),R-8))-down(rr((11,10,W-12,H-LIP-11),R-12)),0,1)
g+=rim*0.45*np.clip(1.2-y*1.6,0.25,1)
dot=Image.new('L',(W*S,H*S),0); ImageDraw.Draw(dot).ellipse([40*S,22*S,96*S,46*S],fill=255)
dot=dot.filter(ImageFilter.GaussianBlur(5*S)); g+=down(dot)*0.7
g=np.clip(g,0,1)*face
img=np.dstack([np.full((H,W),255)]*3+[g*255])
Image.fromarray(np.clip(img,0,255).astype(np.uint8),'RGBA').save(D+'block_gloss.png')
# preview with the sheet colours
import json
cols=json.load(open(ROOT+'/Docs/reference/extracted/block_colors.json'))
fa=Image.open(D+'block_face.png'); gl=Image.open(D+'block_gloss.png')
out=Image.new('RGB',(4*210,4*120),(234,246,255))
for i,(n,h) in enumerate(cols.items()):
    c=tuple(int(h[k:k+2],16) for k in (1,3,5))
    a=np.array(fa).astype(float); a[...,:3]=a[...,:3]*np.array(c)/255
    t=Image.fromarray(a.astype(np.uint8),'RGBA'); t.alpha_composite(gl)
    t=t.resize((200,110),Image.LANCZOS); out.paste(t,((i%4)*210,(i//4)*120),t)
out.save('/tmp/vb_blocks_tpl.png')
