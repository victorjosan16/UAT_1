// Front (orthographic, looking along +z) + 3/4 views for character QA. Usage: node tools/preview-front.mjs out.png id,id
import { buildAll } from './models.mjs';
import { createRequire } from 'module';
const require = createRequire(import.meta.url);
const { chromium } = require('playwright');
const out = process.argv[2] || 'front.png';
const only = process.argv[3] ? process.argv[3].split(',') : null;
const models = buildAll().filter((m) => !only || only.includes(m.id)).map((m) => ({ id: m.id, palette: m.palette, X: m.grid.X, Y: m.grid.Y, Z: m.grid.Z, v: Array.from(m.grid.v) }));
const cell = 300, cols = Math.min(5, models.length), rows = Math.ceil(models.length / cols);
const html = `<canvas id=c width=${cols * cell} height=${rows * cell}></canvas><script>
const M=${JSON.stringify(models)};const ctx=c.getContext('2d');ctx.fillStyle='#cfeaff';ctx.fillRect(0,0,c.width,c.height);
function sh(h,f){const n=parseInt(h.slice(1),16);return 'rgb('+[(n>>16)&255,(n>>8)&255,n&255].map(v=>Math.min(255,v*f|0)).join(',')+')'}
M.forEach((m,i)=>{const ox=(i%${cols})*${cell}, oy=Math.floor(i/${cols})*${cell};
 const s=Math.floor(Math.min(250/m.X, 250/m.Y));
 const get=(x,y,z)=>x<0||y<0||z<0||x>=m.X||y>=m.Y||z>=m.Z?-1:m.v[x+m.X*(y+m.Y*z)];
 for(let y=0;y<m.Y;y++)for(let x=0;x<m.X;x++){for(let z=0;z<m.Z;z++){const c=get(x,y,z);if(c>=0){
   ctx.fillStyle=sh(m.palette[c],1.05-z*0.035);ctx.fillRect(ox+(${cell}-m.X*s)/2+x*s, oy+${cell}-20-(y+1)*s, s-1, s-1);break;}}}
 ctx.fillStyle='#333';ctx.font='14px sans-serif';ctx.fillText(m.id,ox+6,oy+16);
});</script>`;
const b = await chromium.launch(); const p = await b.newPage({ viewport: { width: cols * cell, height: rows * cell } });
await p.setContent(html); await (await p.$('#c')).screenshot({ path: out }); await b.close(); console.log('wrote', out);
