// Renders an isometric contact sheet of all models (visual QA of silhouettes).
// Usage: node tools/preview.mjs out.png [id,id,...]
import { buildAll } from './models.mjs';
import { createRequire } from 'module';
const require = createRequire(import.meta.url);
const { chromium } = require('playwright');

const out = process.argv[2] || 'models.png';
const only = process.argv[3] ? process.argv[3].split(',') : null;
const models = buildAll().filter((m) => !only || only.includes(m.id)).map((m) => ({ id: m.id, palette: m.palette, X: m.grid.X, Y: m.grid.Y, Z: m.grid.Z, v: Array.from(m.grid.v) }));
const cols = Math.min(6, models.length), cell = 260, rows = Math.ceil(models.length / cols);
const html = `<canvas id=c width=${cols * cell} height=${rows * cell}></canvas><script>
const M=${JSON.stringify(models)};const ctx=c.getContext('2d');ctx.fillStyle='#dff1ff';ctx.fillRect(0,0,c.width,c.height);
function sh(h,f){const n=parseInt(h.slice(1),16);return 'rgb('+[(n>>16)&255,(n>>8)&255,n&255].map(v=>Math.min(255,v*f|0)).join(',')+')'}
M.forEach((m,i)=>{const ox=(i%${cols})*${cell}+${cell}/2, oy=Math.floor(i/${cols})*${cell};
 const s=Math.min(9, 210/((m.X+m.Z)*0.87), 200/(m.Y+(m.X+m.Z)/2));
 // view from front-right: camera at -z,+x. screen x = (x + (Zmax - z))..., draw back to front
 const P=(x,y,z)=>[ox+(x+z-(m.X+m.Z)/2)*s*0.87, oy+${cell}-20 - m.X*s*0.5 - y*s + (x-z)*s*0.5];
 const get=(x,y,z)=>x<0||y<0||z<0||x>=m.X||y>=m.Y||z>=m.Z?-1:m.v[x+m.X*(y+m.Y*z)];
 const list=[];for(let z=0;z<m.Z;z++)for(let y=0;y<m.Y;y++)for(let x=0;x<m.X;x++){const c=get(x,y,z);if(c>=0)list.push([x,y,z,c])}
 list.sort((a,b)=>(a[0]-a[2]+a[1])-(b[0]-b[2]+b[1]));
 for(const [x,y,z,ci] of list){const col=m.palette[ci];
  const q=(pts,f)=>{ctx.beginPath();pts.forEach((p,j)=>{const r=P(...p);j?ctx.lineTo(...r):ctx.moveTo(...r)});ctx.closePath();ctx.fillStyle=sh(col,f);ctx.fill();ctx.strokeStyle='rgba(0,0,0,.12)';ctx.stroke()};
  if(get(x,y+1,z)<0)q([[x,y+1,z],[x+1,y+1,z],[x+1,y+1,z+1],[x,y+1,z+1]],1.08);
  if(get(x,y,z-1)<0)q([[x,y,z],[x+1,y,z],[x+1,y+1,z],[x,y+1,z]],0.9);
  if(get(x+1,y,z)<0)q([[x+1,y,z],[x+1,y,z+1],[x+1,y+1,z+1],[x+1,y+1,z]],0.72);
 }
 ctx.fillStyle='#333';ctx.font='14px sans-serif';ctx.fillText(m.id,ox-${cell}/2+8,oy+16);
});</script>`;
const b = await chromium.launch(); const p = await b.newPage({ viewport: { width: cols * cell, height: rows * cell } });
await p.setContent(html); await (await p.$('#c')).screenshot({ path: out }); await b.close();
console.log('wrote', out);
