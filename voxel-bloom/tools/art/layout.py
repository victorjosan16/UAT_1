import os
ROOT=os.environ.get('VB_ROOT', os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..')))
from PIL import Image, ImageDraw
U=ROOT+'/Assets/VoxelBloom/Art/UI/Resources/VBUI/'
R=ROOT+'/Docs/reference/extracted/'
W,H=1080,int(os.environ.get('VB_H','1920'))  # canvas height = 1080 * screen aspect (1920 for 16:9, 2374 for 20:9)
import sys
BG=sys.argv[1] if len(sys.argv)>1 else None
OUTP=sys.argv[2] if len(sys.argv)>2 else ROOT+'/Docs/HomeLayoutCheck.png'
c=Image.new('RGBA',(W,H),(120,190,255,255))
T=int(os.environ.get('VB_TOP','0'))  # top safe-area inset (status bar / notch) in canvas units
SH=H-T                                # safe-area height: Root covers [T, H]
if BG:  # 3D render (900x1950 texture) shown with EnvelopeParent: width fits, centred vertically
    r=Image.open(BG).convert('RGBA'); sc=max(W/r.width,SH/r.height); r=r.resize((round(r.width*sc),round(r.height*sc)),Image.LANCZOS)
    ox,oy=(r.width-W)//2,(r.height-SH)//2; c.alpha_composite(r.crop((ox,oy,ox+W,oy+SH)),(0,T))
def put(path,cx,cy,w,flip=False,base=U):
    im=Image.open(base+path+'.png').convert('RGBA'); h=round(w*im.height/im.width); im=im.resize((round(w),h),Image.LANCZOS)
    if flip: im=im.transpose(Image.FLIP_LEFT_RIGHT)
    c.alpha_composite(im,(round(cx-w/2),round(cy-h/2)))
k=0.96; y=79+T
for p,x,w in [('HUD/avatar',0,146),('HUD/pill_hearts_blank',150,215),('HUD/pill_coins_blank',370,278),('HUD/pill_gems_blank',652,250)]:
    put(p,30+x*k+w*k/2,y,w*k)
put('Buttons/settings',1050-52,y,104)
put('Logo/logo',534,312+T,790)
if not BG: put('Backgrounds/fox_pedestal',540,1044-258,560,base=R)  # stand-in for the 3D fox + pedestal
put('Buttons/back',0.17*W,(T+SH*(1-0.568)),118); put('Buttons/back',0.83*W,(T+SH*(1-0.568)),118,True)
cy=T+SH*(1-0.474); s=1.39
put('Icons/rotate_left',540-137,cy,76*s); put('Icons/label_360',542,cy,116*s); put('Icons/rotate_right',540+132,cy,82*s)
d=ImageDraw.Draw(c); d.rounded_rectangle([24,H-876,W-24,H-700],44,fill=(236,242,255,150))
for i in range(6):
    x=24+(W-48)*(i+0.5)/6; d.rounded_rectangle([x-75,H-788-70,x+75,H-788+70],34,fill=(231,228,255,255) if i else (255,214,64,255))
put('Buttons/play',540,H-540,680)
put('HUD/caption_pill_blank',540,H-672,360)
put('Buttons/level_badge_blank',540,H-407,460)
for p,x in [('nav_collection',-393),('nav_rewards',-131),('nav_shop',131),('nav_events',393)]:
    im=Image.open(U+'Buttons/'+p+'.png'); w=240*im.width/180; h=w*im.height/im.width
    put('Buttons/'+p,540+x,H-88-h/2,w)
m=Image.open(os.environ.get('VB_RIGHT',ROOT+'/Docs/reference/home-mockup-v2.png')).convert('RGBA').resize((W,H))
out=Image.new('RGBA',(W*2+20,H),(255,255,255,255)); out.alpha_composite(c,(0,0)); out.alpha_composite(m,(W+20,0))
d2=ImageDraw.Draw(out); d2.rectangle([0,H-70,W,H],fill=(20,20,30,255)); d2.text((20,H-55),('PREVIEW: diorama.json rendered with the Unity camera (flat-shaded approximation) + art placement - NOT a Unity screenshot' if BG else 'LAYOUT CHECK of the art placement (flat sky, 2D fox stand-in for the 3D model) - NOT a Unity screenshot'),fill='white')
out.convert('RGB').resize((W+10,H//2)).save(OUTP)
