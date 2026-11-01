// Renders levels in their multicolour level palettes: node tools/preview-levels.mjs out.png 1 20
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
import { createRequire } from 'module';
const require = createRequire(import.meta.url);
const { chromium } = require('playwright');
const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const [out, a = 1, b = 20] = process.argv.slice(2);
const models = [];
for (let n = +a; n <= +b; n++) {
  const l = JSON.parse(fs.readFileSync(path.join(ROOT, 'Assets/VoxelBloom/Data/Resources/VoxelBloom/Levels', `level_${String(n).padStart(3, '0')}.json`)));
  models.push({ id: n + '. ' + l.name + ' (' + l.palette.length + ' colours)', palette: l.palette, X: l.size[0], Y: l.size[1], Z: l.size[2], v: Array.from(l.voxels, (c) => (c === '.' ? -1 : parseInt(c, 36))) });
}
const cols = 5, cell = 260, rows = Math.ceil(models.length / cols);
const html = `<canvas id=c width=${cols * cell} height=${rows * cell}></canvas><script>
const M=${JSON.stringify(models)};const ctx=c.getContext('2d');ctx.fillStyle='#dff1ff';ctx.fillRect(0,0,c.width,c.height);
function sh(h,f){const n=parseInt(h.slice(1),16);return 'rgb('+[(n>>16)&255,(n>>8)&255,n&255].map(v=>Math.min(255,v*f|0)).join(',')+')'}
M.forEach((m,i)=>{const ox=(i%${cols})*${cell}+${cell}/2, oy=Math.floor(i/${cols})*${cell};
 const s=Math.min(9, 210/((m.X+m.Z)*0.87), 200/(m.Y+(m.X+m.Z)/2));
 const P=(x,y,z)=>[ox+(x+z-(m.X+m.Z)/2)*s*0.87, oy+${cell}-20 - m.X*s*0.5 - y*s + (x-z)*s*0.5];
 const get=(x,y,z)=>x<0||y<0||z<0||x>=m.X||y>=m.Y||z>=m.Z?-1:m.v[x+m.X*(y+m.Y*z)];
 const list=[];for(let z=0;z<m.Z;z++)for(let y=0;y<m.Y;y++)for(let x=0;x<m.X;x++){const c=get(x,y,z);if(c>=0)list.push([x,y,z,c])}
 list.sort((a,b)=>(a[0]-a[2]+a[1])-(b[0]-b[2]+b[1]));
 for(const [x,y,z,ci] of list){const col=m.palette[ci];
  const q=(pts,f)=>{ctx.beginPath();pts.forEach((p,j)=>{const r=P(...p);j?ctx.lineTo(...r):ctx.moveTo(...r)});ctx.closePath();ctx.fillStyle=sh(col,f);ctx.fill();ctx.strokeStyle='rgba(0,0,0,.15)';ctx.stroke()};
  if(get(x,y+1,z)<0)q([[x,y+1,z],[x+1,y+1,z],[x+1,y+1,z+1],[x,y+1,z+1]],1.08);
  if(get(x,y,z-1)<0)q([[x,y,z],[x+1,y,z],[x+1,y+1,z],[x,y+1,z]],0.9);
  if(get(x+1,y,z)<0)q([[x+1,y,z],[x+1,y,z+1],[x+1,y+1,z+1],[x+1,y+1,z]],0.72);
 }
 ctx.fillStyle='#333';ctx.font='13px sans-serif';ctx.fillText(m.id,ox-${cell}/2+6,oy+15);
});</script>`;
const br = await chromium.launch(); const p = await br.newPage({ viewport: { width: cols * cell, height: rows * cell } });
await p.setContent(html); await (await p.$('#c')).screenshot({ path: out }); await br.close(); console.log('wrote', out);
