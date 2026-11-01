// Renders the Home diorama (diorama.json + the selected mascot) with the Unity Home camera, to check the look
// without Unity. Painter's algorithm over culled voxel faces; flat shading per face direction (approximation of VoxelLit).
// Usage: node tools/preview-diorama.mjs out.png [mascotId=fox] [yaw=-22]
import fs from 'fs';
import { buildAll } from './models.mjs';
import { createRequire } from 'module';
const require = createRequire(import.meta.url);
const { chromium } = require('playwright');

const out = process.argv[2] || '/tmp/diorama.png';
const heroId = process.argv[3] || 'fox';
const yaw = +(process.argv[4] ?? -22) * Math.PI / 180;
const D = JSON.parse(fs.readFileSync('Assets/VoxelBloom/Data/Resources/VoxelBloom/Home/diorama.json', 'utf8'));
if (process.env.VB_HOME) { D.camera = D.homeCamera; D.parts = D.parts.filter((p) => p.model !== 'pedestal'); }
const [W, H] = D.camera.texture;

const sub = (a, b) => [a[0] - b[0], a[1] - b[1], a[2] - b[2]], dot = (a, b) => a[0] * b[0] + a[1] * b[1] + a[2] * b[2];
const norm = (a) => { const l = Math.hypot(...a); return [a[0] / l, a[1] / l, a[2] / l]; };
const cross = (a, b) => [a[1] * b[2] - a[2] * b[1], a[2] * b[0] - a[0] * b[2], a[0] * b[1] - a[1] * b[0]];
const cam = D.camera.pos, f = norm(sub(D.camera.look, cam)), r = norm(cross([0, 1, 0], f)), u = cross(f, r);
const th = Math.tan(D.camera.fov * Math.PI / 360), tw = th * W / H;
const proj = (p) => { const d = sub(p, cam), z = dot(d, f); return [W / 2 + dot(d, r) / z / tw * W / 2, H / 2 - dot(d, u) / z / th * H / 2, z]; };

const NORMALS = [[1, 0, 0], [-1, 0, 0], [0, 1, 0], [0, -1, 0], [0, 0, 1], [0, 0, -1]];
const SHADE = [0.76, 0.86, 1.04, 0.55, 0.7, 0.88];
const faces = [];
function hexRgba(h) { const n = parseInt(h.slice(1, 7), 16); return [(n >> 16) & 255, (n >> 8) & 255, n & 255, h.length > 7 ? parseInt(h.slice(7, 9), 16) / 255 : 1]; }
function addModel(m, cells, pos, size, rot = 0) {
  const { X, Y, Z } = m, pal = m.palette.map(hexRgba), cs = Math.cos(rot), sn = Math.sin(rot);
  const get = (x, y, z) => x < 0 || y < 0 || z < 0 || x >= X || y >= Y || z >= Z ? -1 : cells[x + X * (y + Y * z)];
  const tw3 = (p) => { const lx = (p[0] - X / 2) * size, ly = p[1] * size, lz = (p[2] - Z / 2) * size; return [pos[0] + lx * cs + lz * sn, pos[1] + ly, pos[2] - lx * sn + lz * cs]; };
  for (let z = 0; z < Z; z++) for (let y = 0; y < Y; y++) for (let x = 0; x < X; x++) {
    const c = get(x, y, z); if (c < 0) continue;
    for (let k = 0; k < 6; k++) {
      const n = NORMALS[k]; if (get(x + n[0], y + n[1], z + n[2]) >= 0) continue;
      const wn = [n[0] * cs + n[2] * sn, n[1], -n[0] * sn + n[2] * cs];
      // face corners
      const a = k >> 1, o = n[a] > 0 ? 1 : 0, q = [];
      const ax = [[1, 2], [0, 2], [0, 1]][a];
      for (const [s, t] of [[0, 0], [1, 0], [1, 1], [0, 1]]) { const p = [x, y, z]; p[a] += o; p[ax[0]] += s; p[ax[1]] += t; q.push(tw3(p)); }
      const ctr = q.reduce((acc, p) => [acc[0] + p[0] / 4, acc[1] + p[1] / 4, acc[2] + p[2] / 4], [0, 0, 0]);
      if (dot(wn, sub(ctr, cam)) >= 0) continue;
      const P = q.map(proj); if (P.some((p) => p[2] < 0.1)) continue;
      const col = pal[c], sh = SHADE[k] * (1 + ((x * 7 + y * 13 + z * 5) % 5 - 2) * 0.012);
      faces.push([Math.hypot(...sub(ctr, cam)), P.map((p) => [+p[0].toFixed(1), +p[1].toFixed(1)]), [col[0] * sh | 0, col[1] * sh | 0, col[2] * sh | 0, col[3]]]);
    }
  }
}
const decode = (s) => Int8Array.from(s, (ch) => ch === '.' ? -1 : ch <= '9' ? ch.charCodeAt(0) - 48 : ch.charCodeAt(0) - 87);
for (const p of D.parts) { const m = D.models[p.model]; addModel(m, decode(m.cells), p.pos, p.size); }
const hero = process.env.VB_HOME ? null : buildAll().find((m) => m.id === heroId);
if (hero) { const g = hero.grid, vs = 1.6 / Math.max(g.X, g.Y, g.Z); addModel({ X: g.X, Y: g.Y, Z: g.Z, palette: hero.palette }, Array.from(g.v), [0, 0, 0], vs, yaw); }
faces.sort((a, b) => b[0] - a[0]);
console.log(faces.length, 'faces');

const html = `<canvas id=c width=${W} height=${H}></canvas><script>
const F=${JSON.stringify(faces.map((x) => [x[1], x[2]]))};const g=c.getContext('2d');
const gr=g.createLinearGradient(0,0,0,${H});gr.addColorStop(0,'#2F93F0');gr.addColorStop(1,'#A6DCFF');g.fillStyle=gr;g.fillRect(0,0,${W},${H});
for(const [p,col] of F){g.beginPath();g.moveTo(p[0][0],p[0][1]);for(let i=1;i<4;i++)g.lineTo(p[i][0],p[i][1]);g.closePath();
g.fillStyle='rgba('+col[0]+','+col[1]+','+col[2]+','+col[3]+')';g.fill();g.strokeStyle='rgba(0,0,0,0.07)';g.lineWidth=0.6;g.stroke();}
</script>`;
const b = await chromium.launch(); const pg = await b.newPage({ viewport: { width: W, height: H } });
await pg.setContent(html); await (await pg.$('#c')).screenshot({ path: out }); await b.close();
console.log('wrote', out);
