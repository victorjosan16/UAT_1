// 3D preview of the conveyor gameplay screen: the board geometry of ConveyorBoard3D.cs (bands, fills, boxes,
// instanced cubes) rendered with the same camera framing (fov 30, pitch 62°) and a Lambert approximation of VoxelLit,
// composited with the HUD / queue / boosters layout of ConveyorScreen.cs. A look check, NOT a Unity screenshot.
// Usage: node tools/conveyor/preview3d.mjs <level> <seconds> out.png [height=2374]
import fs from 'fs';
import { createRequire } from 'module';
import { Session } from './rules.mjs';
const require = createRequire(import.meta.url);
const { chromium } = require('playwright');

const id = +(process.argv[2] || 5), sec = +(process.argv[3] || 20), out = process.argv[4] || '/tmp/conveyor3d.png', SH = +(process.argv[5] || 2374);
const l = JSON.parse(fs.readFileSync(`Assets/VoxelBloom/Data/Resources/VoxelBloom/Conveyor/level_${String(id).padStart(3, '0')}.json`));
const s = new Session(l);
let w = 0; while (s.tick < sec * 60 && !s.isWon()) { while (w < l.witness.length && l.witness[w][0] === s.tick) s.place(l.witness[w++][1]); s.step(); }

const { w: W, h: H, r: R } = l.board, PU = l.slots * 100;
const a = W - 2 * R, b = H - 2 * R, q = Math.PI * R / 2, LG = 2 * a + 2 * b + 4 * q;
function at(sp) {
  let g = (((sp % PU) + PU) % PU) * LG / PU;
  if (g < a) return [R + g, 0]; g -= a;
  if (g < q) { const f = g / R; return [W - R + R * Math.sin(f), R - R * Math.cos(f)]; } g -= q;
  if (g < b) return [W, R + g]; g -= b;
  if (g < q) { const f = g / R; return [W - R + R * Math.cos(f), H - R + R * Math.sin(f)]; } g -= q;
  if (g < a) return [W - R - g, H]; g -= a;
  if (g < q) { const f = g / R; return [R - R * Math.sin(f), H - R + R * Math.cos(f)]; } g -= q;
  if (g < b) return [0, H - R - g]; g -= b;
  const f = Math.min(g, q) / R; return [R - R * Math.cos(f), R - R * Math.sin(f)];
}
const tan = (sp) => { const p = at(sp + 8), m = at(sp - 8); const d = [p[0] - m[0], p[1] - m[1]], n = Math.hypot(...d); return [d[0] / n, d[1] / n]; };
const world = (g, y) => [g[0] - W / 2, y, H / 2 - g[1]];
const hex = (h) => { const n = parseInt(h.slice(1, 7), 16); return [(n >> 16) & 255, (n >> 8) & 255, n & 255]; };
const lighten = (c, k) => c.map((v) => v + (255 - v) * k);
const C = { frame: hex('#221C52'), floor: hex('#2B2466'), track: hex('#5A5396'), trackDark: hex('#4A4384'), edge: hex('#5BE7FF'), rim: hex('#DAD2F7'), inner: hex('#383070'), stone: hex('#9396B8'), plus: hex('#8C7A52'), green: hex('#4CD964'), feeder: hex('#6B5FC2'), badge: hex('#2C2A4A') };
const polys = []; // {pts:[[x,y,z]...], n:[x,y,z], col:[r,g,b], bevel}
let LAYER = 0;
const quad = (pts, n, col, bevel = false) => polys.push({ pts, n, col, bevel, layer: LAYER });
function box(c, sz, col, bevel = true) {
  const [hx, hy, hz] = sz.map((v) => v / 2), p = (x, y, z) => [c[0] + x * hx, c[1] + y * hy, c[2] + z * hz];
  quad([p(-1, 1, -1), p(-1, 1, 1), p(1, 1, 1), p(1, 1, -1)], [0, 1, 0], col, bevel);
  quad([p(-1, -1, -1), p(1, -1, -1), p(1, 1, -1), p(-1, 1, -1)], [0, 0, -1], col.map((v) => v * 0.9), bevel);
  quad([p(1, -1, 1), p(-1, -1, 1), p(-1, 1, 1), p(1, 1, 1)], [0, 0, 1], col.map((v) => v * 0.9), bevel);
  quad([p(-1, -1, 1), p(-1, -1, -1), p(-1, 1, -1), p(-1, 1, 1)], [-1, 0, 0], col.map((v) => v * 0.85), bevel);
  quad([p(1, -1, -1), p(1, -1, 1), p(1, 1, 1), p(1, 1, -1)], [1, 0, 0], col.map((v) => v * 0.85), bevel);
}
const N = 160;
function band(i0, o0, y, bottom, top, wall) {
  for (let i = 0; i < N; i++) {
    const s0 = i / N * PU, s1 = (i + 1) / N * PU, c0 = at(s0), c1 = at(s1), t0 = tan(s0), t1 = tan(s1), n0 = [t0[1], -t0[0]], n1 = [t1[1], -t1[0]];
    const P = (c, n, o) => world([c[0] + n[0] * o, c[1] + n[1] * o], y);
    const A = P(c0, n0, i0), B = P(c0, n0, o0), Cc = P(c1, n1, o0), D = P(c1, n1, i0);
    quad([A, B, Cc, D], [0, 1, 0], top);
    if (bottom < y) { const dn = (p) => [p[0], bottom, p[2]]; const wo = [n0[0], 0, -n0[1]]; quad([B, dn(B), dn(Cc), Cc], wo, wall); quad([dn(A), A, D, dn(D)], wo.map((v) => -v), wall); }
  }
}
function fill(off, y, bottom, top, wall) {
  const ctr = world([W / 2, H / 2], y);
  for (let i = 0; i < N; i++) {
    const s0 = i / N * PU, s1 = (i + 1) / N * PU, t0 = tan(s0), t1 = tan(s1), c0 = at(s0), c1 = at(s1);
    const A = world([c0[0] + t0[1] * off, c0[1] - t0[0] * off], y), B = world([c1[0] + t1[1] * off, c1[1] - t1[0] * off], y);
    quad([ctr, A, B, ctr], [0, 1, 0], top);
    if (bottom < y) { const dn = (p) => [p[0], bottom, p[2]]; quad([A, dn(A), dn(B), B], [A[0] - ctr[0], 0, A[2] - ctr[2]], wall); }
  }
}
const TT = 0.30, FT = 0.12, CS = 0.74, BH = 0.62, PITCH = 1.7, CELL = 1.5, TW = 1.84;
fill(1.05, FT - 0.01, -0.5, C.frame, C.frame.map((v) => v * 0.7));
fill(-0.7, FT, FT, C.floor, C.floor);
LAYER = 1;
band(-0.58, 0.58, TT, FT, C.track, C.trackDark);
for (let i = 0; i < l.slots; i++) { const sp = (i + 0.5) * 100, c = at(sp), t = tan(sp), n = [t[1], -t[0]];
  const P = (o, d) => world([c[0] + n[0] * o + t[0] * d, c[1] + n[1] * o + t[1] * d], TT + 0.004);
  quad([P(-0.56, 0.03), P(0.56, 0.03), P(0.56, -0.03), P(-0.56, -0.03)], [0, 1, 0], C.trackDark); }
band(0.58, 0.72, TT + 0.03, FT, C.edge, C.edge.map((v) => v * 0.8));
band(-0.72, -0.58, TT + 0.03, FT, C.edge, C.edge.map((v) => v * 0.8));
LAYER = 2;
const fp = at(l.feeder);
box(world([fp[0] - 0.75, fp[1] - 0.75], 0.45), [1.3, 0.9, 1.3], C.feeder);
const comp = (st, i) => { const f = 1.6 + Math.min(i, st.capacity - 1) * PITCH; return st.side === 'top' ? [st.at, f] : st.side === 'right' ? [W - f, st.at] : st.side === 'bottom' ? [st.at, H - f] : [f, st.at]; };
const overlays = [];
l.stations.forEach((st, i) => {
  const c0 = comp(st, 0), cN = comp(st, st.capacity - 1), cg = [(c0[0] + cN[0]) / 2, (c0[1] + cN[1]) / 2], ctr = world(cg, 0);
  const len = st.capacity * PITCH + 0.24, vert = st.side === 'top' || st.side === 'bottom', sx = vert ? TW : len, sz = vert ? len : TW;
  const lock = s.stations[i].lock, wall = 0.12, h = 0.4;
  const walls = (col) => { box([ctr[0] - sx / 2 + wall / 2, FT + h / 2, ctr[2]], [wall, h, sz], col); box([ctr[0] + sx / 2 - wall / 2, FT + h / 2, ctr[2]], [wall, h, sz], col);
    box([ctr[0], FT + h / 2, ctr[2] - sz / 2 + wall / 2], [sx - 2 * wall, h, wall], col); box([ctr[0], FT + h / 2, ctr[2] + sz / 2 - wall / 2], [sx - 2 * wall, h, wall], col); };
  if (lock && lock.type === 'completed') { box([ctr[0], FT + 0.22, ctr[2]], [sx, 0.44, sz], C.stone); box([ctr[0], FT + 0.46, ctr[2]], [sx - 0.24, 0.06, sz - 0.24], lighten(C.stone, 0.22)); box([ctr[0], FT + 0.5, ctr[2]], [1.15, 0.06, 1.15], C.badge); overlays.push({ text: String(Math.max(1, lock.n - s.completed)), p: [ctr[0], FT + 0.53, ctr[2]] }); return; }
  if (lock && lock.type === 'booster') { box([ctr[0], FT + 0.07, ctr[2]], [sx, 0.14, sz], C.plus, false); walls(lighten(C.plus, 0.35)); box([ctr[0], FT + 0.36, ctr[2]], [1, 0.3, 0.3], C.green); box([ctr[0], FT + 0.36, ctr[2]], [0.3, 0.3, 1], C.green); return; }
  box([ctr[0], FT + 0.07, ctr[2]], [sx, 0.14, sz], C.inner, false); walls(C.rim);
  const tr = s.stations[i].tray; if (!tr) return;
  const lv = s.stations[i].leave > 0 ? s.stations[i].leave / 24 : 1;
  tr.colors.forEach((ci, k) => { const g = comp(st, k), col = hex(l.colors[ci]);
    if (k < tr.filled) box(world(g, FT + 0.14 + BH / 2 + (1 - lv) * 0.8), [CELL * 0.94 * lv, BH * lv, CELL * 0.94 * lv], col);
    else box(world(g, FT + 0.2), [CELL * 0.9, 0.12, CELL * 0.9], lighten(col, 0.45).map((v) => v * 0.9)); });
});
for (let k = 0; k < l.slots; k++) { const ci = s.slot[k]; if (ci < 0) continue; box(world(at(s.slotPos(k)), TT + CS / 2), [CS, CS, CS], hex(l.colors[ci])); }
// camera framing (same search as ConveyorBoard3D.FrameCamera)
const K = 1020 / (W + 2), BHUI = (H + 2) * K, RTW = 1024, RTH = Math.round(1024 * BHUI / 1020), ASP = RTW / RTH, FOV = 26 * Math.PI / 180, PIT = 78 * Math.PI / 180;
const sub = (u, v) => [u[0] - v[0], u[1] - v[1], u[2] - v[2]], dot = (u, v) => u[0] * v[0] + u[1] * v[1] + u[2] * v[2];
const nrm = (u) => { const n = Math.hypot(...u); return u.map((x) => x / n); }, cross = (u, v) => [u[1] * v[2] - u[2] * v[1], u[2] * v[0] - u[0] * v[2], u[0] * v[1] - u[1] * v[0]];
let cam, f, rgt, up;
const look = [0, 0, 0.2];
function vp(p) { const d = sub(p, cam), z = dot(d, f), th = Math.tan(FOV / 2); return [0.5 + dot(d, rgt) / z / (th * ASP) / 2, 0.5 + dot(d, up) / z / th / 2, z]; }
const hx = W / 2 + 1.15, hz = H / 2 + 1.15, corners = [[-hx, 0, -hz], [hx, 0, -hz], [-hx, 0, hz], [hx, 0, hz], [-hx, 1, hz], [hx, 1, hz]];
for (let dist = 12; dist < 120; dist += 0.25) {
  cam = [look[0], look[1] + Math.sin(PIT) * dist, look[2] - Math.cos(PIT) * dist]; f = nrm(sub(look, cam)); rgt = nrm(cross([0, 1, 0], f)); up = cross(f, rgt);
  if (corners.every((c) => { const v = vp(c); return v[0] >= 0.01 && v[0] <= 0.99 && v[1] >= 0.01 && v[1] <= 0.99; })) break;
}
const L = nrm([0.45, 0.8, -0.4]);
const draw = polys.map((pg) => {
  const ctr = pg.pts.reduce((acc, p) => [acc[0] + p[0] / pg.pts.length, acc[1] + p[1] / pg.pts.length, acc[2] + p[2] / pg.pts.length], [0, 0, 0]);
  let n = nrm(pg.n); if (dot(n, sub(cam, ctr)) < 0) n = n.map((x) => -x);
  if (pg.n[1] === 0 && dot(nrm(pg.n), sub(cam, ctr)) < 0) return null; // back side of a wall
  const amb = 0.64 + (0.84 - 0.64) * (n[1] * 0.5 + 0.5), diff = Math.max(0, dot(n, L)), k = amb * 0.48 + diff * 0.72;
  const P = pg.pts.map((p) => { const v = vp(p); return [v[0] * 1020 - 510, (1 - v[1]) * BHUI - BHUI / 2]; });
  return { layer: pg.layer, d: Math.hypot(...sub(ctr, cam)), P, col: pg.col.map((v) => Math.min(255, v * k * 1.12) | 0), bevel: pg.bevel };
}).filter(Boolean).sort((x, y) => x.layer - y.layer || y.d - x.d);
const ov = overlays.map((o) => { const v = vp(o.p); return { t: o.text, x: v[0] * 1020 - 510, y: (1 - v[1]) * BHUI - BHUI / 2 }; });
const fpv = vp(world([fp[0] - 0.75, fp[1] - 0.75], 0.95));
const S = { draw, ov, queue: s.queue.slice(0, l.visible).map((t) => t.colors.map((c) => l.colors[c])), hidden: Math.max(0, s.queue.length - l.visible), completed: s.completed, total: s.total, hopper: s.hopperLeft(), id, vis: l.visible, feeder: [fpv[0] * 1020 - 510, (1 - fpv[1]) * BHUI - BHUI / 2] };
const html = `<canvas id=c width=1080 height=${SH}></canvas><script>
const S=${JSON.stringify(S)}, H=${SH}, BHUI=${BHUI};
const g=c.getContext('2d');
function rr(x,y,w,h,r,fill){g.beginPath();g.roundRect(x,y,w,h,r);g.fillStyle=fill;g.fill();}
function cube2d(px,py,w,h,col){rr(px-w/2,py-h/2,w,h,Math.min(w,h)*0.22,col);const gr=g.createLinearGradient(0,py-h/2,0,py+h/2);gr.addColorStop(0,'rgba(255,255,255,0.5)');gr.addColorStop(0.45,'rgba(255,255,255,0.06)');gr.addColorStop(1,'rgba(0,0,0,0.25)');rr(px-w/2,py-h/2,w,h,Math.min(w,h)*0.22,gr);}
const bg=g.createLinearGradient(0,0,0,H);bg.addColorStop(0,'#2A2366');bg.addColorStop(1,'#3C2F8F');g.fillStyle=bg;g.fillRect(0,0,1080,H);
rr(-35,H*0.1,70,H*0.82,30,'#45389A');rr(1045,H*0.1,70,H*0.82,30,'#45389A');
const avail=H-150-220, TOPY=150+Math.max(0,(avail-(BHUI+22+230))/2);
const hy=60;
rr(30,hy,100,100,26,'#8E5BEA');g.fillStyle='#fff';g.fillRect(62,hy+28,12,44);g.fillRect(86,hy+28,12,44);
rr(950,hy,100,100,26,'#8E5BEA');g.fillStyle='#fff';g.beginPath();g.arc(1000,hy+50,22,0,7);g.fill();g.fillStyle='#8E5BEA';g.beginPath();g.arc(1000,hy+50,10,0,7);g.fill();
rr(540-170-149,hy+2,298,96,48,'#8E7BFF');rr(540-170-145,hy+6,290,88,44,'#4B3BB0');
g.font='bold 50px sans-serif';g.textAlign='center';g.textBaseline='middle';g.fillStyle='#fff';g.fillText('Nivel ',540-195,hy+52);g.fillStyle='#FFD84A';g.fillText(S.id,540-100,hy+52);
rr(540,hy+6,330,88,44,'#1E1848');rr(552,hy+20,306*(S.completed/S.total)+10,60,30,'#45D85A');g.fillStyle='#fff';g.fillText(S.completed+'/'+S.total,705,hy+52);
// board: 3D render placed in the board rect
const cx=540, cy=TOPY+BHUI/2;
const glow=g.createRadialGradient(cx,cy,100,cx,cy,800);glow.addColorStop(0,'rgba(91,231,255,0.10)');glow.addColorStop(1,'rgba(91,231,255,0)');g.fillStyle=glow;g.fillRect(0,TOPY-100,1080,BHUI+200);
for(const p of S.draw){g.beginPath();g.moveTo(cx+p.P[0][0],cy+p.P[0][1]);for(let i=1;i<p.P.length;i++)g.lineTo(cx+p.P[i][0],cy+p.P[i][1]);g.closePath();
 g.fillStyle='rgb('+p.col+')';g.fill();g.strokeStyle=p.bevel?'rgba(255,255,255,0.14)':'rgba(0,0,0,0.06)';g.lineWidth=p.bevel?1.4:0.6;g.stroke();}
g.fillStyle='#fff';g.font='bold 46px sans-serif';for(const o of S.ov)g.fillText(o.t,cx+o.x,cy+o.y);
if(S.hopper>0){g.font='bold 34px sans-serif';g.fillText('+'+S.hopper,cx+S.feeder[0],cy+S.feeder[1]);}
// queue
const qy=TOPY+BHUI+22+115, slotW=1020/7;
for(let i=0;i<S.vis;i++){const x=540-(S.vis-1)*slotW/2+i*slotW;const t=S.queue[i];g.globalAlpha=t?1:0.35;rr(x-66,qy-107,132,214,30,'#DAD2F7');rr(x-57,qy-98,114,196,24,'#383070');
 if(t) t.forEach((col,r)=>cube2d(x,qy-107+12+((214-40)/4)*(r+0.5)+20,98,38,col));g.globalAlpha=1;}
if(S.hidden>0){g.fillStyle='rgba(255,255,255,0.75)';g.font='34px sans-serif';g.textAlign='right';g.fillText('+'+S.hidden+' tăvi în așteptare',1050,qy-140);g.textAlign='center';}
const by=H-24-85;['↶','⇄','+'].forEach((s2,i)=>{const x=150+(i+0.5)*(780/3);rr(x-75,by-75,150,150,40,'#8E5BEA');g.fillStyle='#fff';g.font='bold 80px sans-serif';g.fillText(s2,x,by);
 g.fillStyle='#fff';g.beginPath();g.arc(x+63,by-63,31,0,7);g.fill();g.fillStyle='#E8202E';g.beginPath();g.arc(x+63,by-63,26,0,7);g.fill();g.fillStyle='#fff';g.font='bold 34px sans-serif';g.fillText('3',x+63,by-61);});
</script>`;
const br = await chromium.launch(); const pg = await br.newPage({ viewport: { width: 1080, height: SH } });
pg.on('pageerror', (e) => console.log('PAGE ERROR', e.message)); await pg.setContent(html); await (await pg.$('#c')).screenshot({ path: out }); await br.close();
console.log('wrote', out, polys.length, 'polys');
