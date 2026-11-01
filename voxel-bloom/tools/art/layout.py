import os
ROOT=os.environ.get('VB_ROOT', os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..')))
from PIL import Image, ImageDraw
U=ROOT+'/Assets/VoxelBloom/Art/UI/Resources/VBUI/'
R=ROOT+'/Docs/reference/extracted/'
W,H=1080,1920
c=Image.new('RGBA',(W,H),(120,190,255,255))
def put(path,cx,cy,w,flip=False,base=U):
    im=Image.open(base+path+'.png').convert('RGBA'); h=round(w*im.height/im.width); im=im.resize((round(w),h),Image.LANCZOS)
    if flip: im=im.transpose(Image.FLIP_LEFT_RIGHT)
    c.alpha_composite(im,(round(cx-w/2),round(cy-h/2)))
k=0.96; y=79
for p,x,w in [('HUD/avatar',0,146),('HUD/pill_hearts_blank',150,215),('HUD/pill_coins_blank',370,278),('HUD/pill_gems_blank',652,250)]:
    put(p,30+x*k+w*k/2,y,w*k)
put('Buttons/settings',1050-52,y,104)
put('Logo/logo',534,312,790)
put('Backgrounds/fox_pedestal',540,1044-258,560,base=R)  # stand-in for the 3D fox + pedestal
put('Buttons/back',0.17*W,1920*(1-0.568),118); put('Buttons/back',0.83*W,1920*(1-0.568),118,True)
cy=1920*(1-0.474); s=1.39
put('Icons/rotate_left',540-137,cy,76*s); put('Icons/label_360',542,cy,116*s); put('Icons/rotate_right',540+132,cy,82*s)
d=ImageDraw.Draw(c); d.rounded_rectangle([24,1920-876,W-24,1920-700],44,fill=(236,242,255,150))
for i in range(6):
    x=24+(W-48)*(i+0.5)/6; d.rounded_rectangle([x-75,1132-70,x+75,1132+70],34,fill=(231,228,255,255) if i else (255,214,64,255))
put('Buttons/play',540,1920-540,680)
put('HUD/caption_pill_blank',540,1920-672,360)
put('Buttons/level_badge_blank',540,1920-407,460)
for p,x in [('nav_collection',-393),('nav_rewards',-131),('nav_shop',131),('nav_events',393)]:
    im=Image.open(U+'Buttons/'+p+'.png'); w=240*im.width/180; h=w*im.height/im.width
    put('Buttons/'+p,540+x,1920-88-h/2,w)
m=Image.open(ROOT+'/Docs/reference/home-mockup-v2.png').convert('RGBA').resize((W,H))
out=Image.new('RGBA',(W*2+20,H),(255,255,255,255)); out.alpha_composite(c,(0,0)); out.alpha_composite(m,(W+20,0))
d2=ImageDraw.Draw(out); d2.rectangle([0,H-70,W,H],fill=(20,20,30,255)); d2.text((20,H-55),'LAYOUT CHECK of the art placement (flat sky, 2D fox stand-in for the 3D model) - NOT a Unity screenshot',fill='white')
out.convert('RGB').resize((W+10,H//2)).save(ROOT+'/Docs/HomeLayoutCheck.png')
