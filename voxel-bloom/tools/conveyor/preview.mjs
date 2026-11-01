// Renders a conveyor level at a given moment of its witness with the same layout numbers as ConveyorScreen.cs
// (1080-wide canvas): HUD, loop, cubes, stations, queue, boosters. A layout/look check, NOT a Unity screenshot.
// Usage: node tools/conveyor/preview.mjs <level> <seconds> out.png [height=2374]
import fs from 'fs';
import { createRequire } from 'module';
import { Session } from './rules.mjs';
const require = createRequire(import.meta.url);
const { chromium } = require('playwright');

const id = +(process.argv[2] || 5), sec = +(process.argv[3] || 20), out = process.argv[4] || '/tmp/conveyor.png', H = +(process.argv[5] || 2374);
const l = JSON.parse(fs.readFileSync(`Assets/VoxelBloom/Data/Resources/VoxelBloom/Conveyor/level_${String(id).padStart(3, '0')}.json`));
const s = new Session(l, { events: false });
let w = 0; const target = sec * 60;
while (s.tick < target && !s.isWon()) { while (w < l.witness.length && l.witness[w][0] === s.tick) s.place(l.witness[w++][1]); s.step(); }
const { w: BW, h: BH, r: BR } = l.board;
const a = BW - 2 * BR, b = BH - 2 * BR, q = Math.PI * BR / 2, LG = 2 * a + 2 * b + 4 * q, PU = l.slots * 100;
function at(sp) {
  let g = (((sp % PU) + PU) % PU) * LG / PU;
  if (g < a) return [BR + g, 0]; g -= a;
  if (g < q) { const f = g / BR; return [BW - BR + BR * Math.sin(f), BR - BR * Math.cos(f)]; } g -= q;
  if (g < b) return [BW, BR + g]; g -= b;
  if (g < q) { const f = g / BR; return [BW - BR + BR * Math.cos(f), BH - BR + BR * Math.sin(f)]; } g -= q;
  if (g < a) return [BW - BR - g, BH]; g -= a;
  if (g < q) { const f = g / BR; return [BR - BR * Math.sin(f), BH - BR + BR * Math.cos(f)]; } g -= q;
  if (g < b) return [0, BH - BR - g]; g -= b;
  const f = Math.min(g, q) / BR; return [BR - BR * Math.cos(f), BR - BR * Math.sin(f)];
}
function comp(st, i) { const f = 1.6 + i * 1.7; return st.side === 'top' ? [st.at, f] : st.side === 'right' ? [BW - f, st.at] : st.side === 'bottom' ? [st.at, BH - f] : [f, st.at]; }
const state = {
  cubes: Array.from(s.slot).map((c, k) => c < 0 ? null : [...at(s.slotPos(k)), l.colors[c]]),
  stations: l.stations.map((d, i) => { const st = s.stations[i]; return { d, lock: st.lock, comps: [0, 1, 2, 3].map((k) => comp(d, k)), tray: st.tray ? st.tray.colors.map((c, k) => [l.colors[c], k < st.tray.filled]) : null, leave: st.leave }; }),
  queue: s.queue.slice(0, l.visible).map((t) => t.colors.map((c) => l.colors[c])), hidden: Math.max(0, s.queue.length - l.visible),
  completed: s.completed, total: s.total, hopper: s.hopperLeft(), feeder: at(l.feeder), id,
};
const html = `<canvas id=c width=1080 height=${H}></canvas><script>
const S=${JSON.stringify(state)}, BW=${BW}, BH=${BH}, BR=${BR}, H=${H};
const g=c.getContext('2d'); const K=1020/(BW+2), BH_UI=(BH+2)*K, TOP=150+40;
const X=(x)=>30+(x+1)*K, Y=(y)=>TOP+(y+1)*K;
function rr(x,y,w,h,r,fill){g.beginPath();g.roundRect(x,y,w,h,r);g.fillStyle=fill;g.fill();}
function rrG(cx,cy,w,h,r,fill){rr(X(cx)-w*K/2,Y(cy)-h*K/2,w*K,h*K,r*K,fill);}
function cube(px,py,size,col,alpha=1){g.globalAlpha=alpha;rr(px-size/2,py-size/2,size,size,size*0.22,col);
 const gr=g.createLinearGradient(0,py-size/2,0,py+size/2);gr.addColorStop(0,'rgba(255,255,255,0.55)');gr.addColorStop(0.45,'rgba(255,255,255,0.08)');gr.addColorStop(1,'rgba(0,0,0,0.22)');
 rr(px-size/2,py-size/2,size,size,size*0.22,gr);g.globalAlpha=1;}
const bg=g.createLinearGradient(0,0,0,H);bg.addColorStop(0,'#2A2366');bg.addColorStop(1,'#3C2F8F');g.fillStyle=bg;g.fillRect(0,0,1080,H);
g.fillStyle='#45389A';rr(-35,H*0.08,70,H*0.82,30,'#45389A');rr(1045,H*0.08,70,H*0.82,30,'#45389A');
// HUD
const hy=40+20;
rr(30,hy,100,100,26,'#8E5BEA');g.fillStyle='#fff';g.fillRect(62,hy+28,12,44);g.fillRect(86,hy+28,12,44);
rr(950,hy,100,100,26,'#8E5BEA');g.fillStyle='#fff';g.beginPath();g.arc(1000,hy+50,22,0,7);g.fill();g.fillStyle='#8E5BEA';g.beginPath();g.arc(1000,hy+50,10,0,7);g.fill();
rr(540-170-145-4,hy+6-4,298,96,48,'#8E7BFF');rr(540-170-145,hy+6,290,88,44,'#4B3BB0');
g.font='bold 50px sans-serif';g.textAlign='center';g.textBaseline='middle';g.fillStyle='#fff';g.fillText('Nivel ',540-170-25,hy+52);g.fillStyle='#FFD84A';g.fillText(S.id,540-170+70,hy+52);
rr(540+165-165,hy+6,330,88,44,'#1E1848');rr(540+12,hy+20,306*(S.completed/S.total)+10,60,30,'#45D85A');g.fillStyle='#fff';g.fillText(S.completed+'/'+S.total,540+165,hy+52);
// board
rrG(BW/2,BH/2,BW+2,BH+2,BR+1,'#221C52');rrG(BW/2,BH/2,BW+1.32,BH+1.32,BR+0.66,'#5BE7FF');rrG(BW/2,BH/2,BW+1.16,BH+1.16,BR+0.58,'#5A5396');
rrG(BW/2,BH/2,BW-0.84,BH-0.84,BR-0.42,'#5BE7FF');rrG(BW/2,BH/2,BW-1,BH-1,Math.max(0.3,BR-0.5),'#2B2466');
// stations
for(const st of S.stations){const v=st.d.side==='top'||st.d.side==='bottom';const c0=st.comps[0],c3=st.comps[3];const cx=(c0[0]+c3[0])/2,cy=(c0[1]+c3[1])/2;
 const w=v?1.84:4*1.7+0.24,h=v?4*1.7+0.24:1.84;
 if(st.lock&&st.lock.type==='completed'){rrG(cx,cy,w,h,0.3,'#9396B8');rrG(cx,cy-0.04,w-0.18,h-0.22,0.24,'#B0B2CC');rrG(cx,cy,0.95,0.95,0.25,'#2C2A4A');g.fillStyle='#fff';g.font='bold '+(0.66*K)+'px sans-serif';g.fillText(Math.max(1,st.lock.n-S.completed),X(cx),Y(cy)+2);continue;}
 if(st.lock&&st.lock.type==='booster'){rrG(cx,cy,w,h,0.3,'#B5A57E');rrG(cx,cy,w-0.16,h-0.16,0.24,'#8C7A52');g.fillStyle='#4CD964';g.fillRect(X(cx)-0.35*K,Y(cy)-0.1*K,0.7*K,0.2*K);g.fillRect(X(cx)-0.1*K,Y(cy)-0.35*K,0.2*K,0.7*K);continue;}
 rrG(cx,cy,w,h,0.3,'#DAD2F7');rrG(cx,cy,w-0.2,h-0.2,0.24,'#383070');
 if(st.tray) st.tray.forEach(([col,filled],k)=>{const p=st.comps[k];cube(X(p[0]),Y(p[1]),1.5*K,col,(filled?1:0.5)*(st.leave>0?st.leave/24:1));});}
// chevrons
g.fillStyle='rgba(91,231,255,0.75)';
// cubes
for(const cb of S.cubes) if(cb) cube(X(cb[0]),Y(cb[1]),0.8*K,cb[2]);
// feeder
g.save();g.translate(X(S.feeder[0]-0.55),Y(S.feeder[1]-0.55));g.rotate(Math.PI/4);rr(-0.75*K,-0.75*K,1.5*K,1.5*K,0.4*K,'#6B5FC2');g.restore();
g.fillStyle='#fff';g.font='bold '+(0.6*K)+'px sans-serif';if(S.hopper>0)g.fillText('+'+S.hopper,X(S.feeder[0]-0.55),Y(S.feeder[1]-0.55));
// queue
const qy=TOP+BH_UI+22+115, slotW=1020/7;const n=${l.visible};
for(let i=0;i<n;i++){const x=540-(n-1)*slotW/2+i*slotW;const t=S.queue[i];g.globalAlpha=t?1:0.35;rr(x-66,qy-107,132,214,30,'#DAD2F7');rr(x-57,qy-98,114,196,24,'#383070');
 if(t) t.forEach((col,r)=>cube(x,qy-107+12+((214-40)/4)*(r+0.5)+20, 78,col));g.globalAlpha=1;}
if(S.hidden>0){g.fillStyle='rgba(255,255,255,0.75)';g.font='34px sans-serif';g.textAlign='right';g.fillText('+'+S.hidden+' tăvi în așteptare',1050,qy-140);g.textAlign='center';}
// boosters
const by=H-24-85;['↶','⇄','+'].forEach((s2,i)=>{const x=150+(i+0.5)*(780/3);rr(x-75,by-75,150,150,40,'#8E5BEA');g.fillStyle='#fff';g.font='bold 80px sans-serif';g.fillText(s2,x,by);
 g.fillStyle='#fff';g.beginPath();g.arc(x+63,by-63,31,0,7);g.fill();g.fillStyle='#E8202E';g.beginPath();g.arc(x+63,by-63,26,0,7);g.fill();g.fillStyle='#fff';g.font='bold 34px sans-serif';g.fillText('3',x+63,by-61);});
</script>`;
const br = await chromium.launch(); const pg = await br.newPage({ viewport: { width: 1080, height: H } });
await pg.setContent(html); await (await pg.$('#c')).screenshot({ path: out }); await br.close();
console.log('wrote', out, 'tick', s.tick, 'completed', s.completed + '/' + s.total);
