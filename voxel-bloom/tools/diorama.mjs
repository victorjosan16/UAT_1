// Home screen diorama (the 3D landscape behind the mascot), authored as data.
// Writes Assets/VoxelBloom/Data/Resources/VoxelBloom/Home/diorama.json, which HomeScreen loads.
// The same file is rendered by tools/preview-diorama.mjs with the Unity camera, so the look can be checked without Unity.
// Coordinates are stage-local (Unity units): x right, y up, z away from the camera. A part's voxel (x,y,z)
// occupies [x,x+1]*size, centred on X/Z, base at y = 0 (same as VoxelMeshBuilder), placed at `pos`.
import fs from 'fs';

const OUT = 'Assets/VoxelBloom/Data/Resources/VoxelBloom/Home/diorama.json';
export const CAMERA = { pos: [0, 1.85, -8.6], look: [0, -0.1, 0], fov: 40, texture: [900, 1950] };
// Home / title backdrop of the reference mockup: no pedestal or mascot, looking at the waterfall cliffs
// parts left out of the home backdrop (they would hide the waterfall)
export const HOME_SKIP = ['pedestal', 'treeB', 'terraceR', 'bush2', 'fK', 'fK2', 'fP', 'bush1'];
export const HOME_CAMERA = { pos: [2.6,2.4,-3.2], look: [3.1,0.9,7.0], fov: 56, texture: [900, 1950] };

function hash(x, y, z) { let h = (Math.imul(x, 73856093) ^ Math.imul(y, 19349663) ^ Math.imul(z, 83492791)) >>> 0; h ^= h >>> 13; h = Math.imul(h, 0x5bd1e995) >>> 0; return (h & 1023) / 1023; }

class Part {
  constructor(name, X, Y, Z) { this.name = name; this.X = X; this.Y = Y; this.Z = Z; this.v = new Int8Array(X * Y * Z).fill(-1); this.pal = []; }
  idx(col) { let i = this.pal.indexOf(col); if (i < 0) { this.pal.push(col); i = this.pal.length - 1; if (i > 35) throw new Error(this.name + ': >36 colours'); } return i; }
  set(x, y, z, col) { if (x >= 0 && y >= 0 && z >= 0 && x < this.X && y < this.Y && z < this.Z) this.v[x + this.X * (y + this.Y * z)] = col == null ? -1 : this.idx(col); }
  has(x, y, z) { return x >= 0 && y >= 0 && z >= 0 && x < this.X && y < this.Y && z < this.Z && this.v[x + this.X * (y + this.Y * z)] >= 0; }
  encode() { let s = ''; for (const c of this.v) s += c < 0 ? '.' : c < 10 ? String.fromCharCode(48 + c) : String.fromCharCode(87 + c); return s; }
}

// ---------------------------------------------------------------- palette (bright, saturated, like the reference)
const C = {
  grass: '#6FD84E', grassL: '#93E85C', grassD: '#4FBF45', grassDD: '#3FA63F',
  dirt: '#B9784A', dirtD: '#94593A',
  stone: '#9AA6C2', stoneL: '#B9C3DA', stoneD: '#7884A6',
  water: '#5EC8FFCC', waterD: '#3AA6F2CC', foam: '#E8F7FFCC',
  trunk: '#8E5A34', trunkD: '#6E4428',
  leaf: '#4CC446', leafL: '#7FDE52', leafD: '#36A040', leafY: '#B6E84E',
  cloud: '#FFFFFF', cloudS: '#E4F1FF',
  purple: '#7C4DEB', purpleD: '#5A33C4', purpleL: '#9C72FF', gold: '#FFC23C', goldL: '#FFE07A', goldD: '#E89A1A', gem: '#6BF0FF',
};

// ---------------------------------------------------------------- props
/** Blocky voxel tree like the reference: brown trunk, 2-3 stacked cubic canopy tiers with chipped corners, light tops. */
function cubeTree(seed, scale = 1) {
  const W = Math.round(17 * scale) | 1, X = W, Z = W, T = Math.round(8 * scale), Y = T + Math.round(16 * scale), c = (W - 1) / 2;
  const p = new Part('Tree', X, Y, Z);
  const tw = Math.max(1, Math.round(1.2 * scale));
  for (let y = 0; y < T + 3; y++) for (let x = c - tw; x <= c + tw - 1 + (tw === 1 ? 1 : 0); x++) for (let z = c - tw; z <= c + tw - 1 + (tw === 1 ? 1 : 0); z++) p.set(Math.round(x), y, Math.round(z), hash(x, y, z) < 0.3 ? C.trunkD : C.trunk);
  const tiers = [[T, T + Math.round(6 * scale), c], [T + Math.round(5 * scale), T + Math.round(11 * scale), c * 0.72], [T + Math.round(10 * scale), Y, c * 0.42]];
  for (const [y0, y1, r] of tiers) for (let y = y0; y < y1; y++) for (let x = 0; x < X; x++) for (let z = 0; z < Z; z++) {
    const dx = Math.abs(x - c), dz = Math.abs(z - c);
    if (dx > r + 0.01 || dz > r + 0.01) continue;
    if (dx > r - 1 && dz > r - 1 && hash(x + seed, y, z) < 0.75) continue; // chipped corners
    if ((y === y0 || y === y1 - 1) && (dx > r - 1 || dz > r - 1) && hash(x, y + seed, z) < 0.35) continue;
    const top = y === y1 - 1, hr = hash(x, y + seed * 3, z);
    p.set(x, y, z, top ? (hr < 0.35 ? C.leafY : C.leafL) : y === y0 ? C.leafD : hr < 0.18 ? C.leafD : hr < 0.36 ? C.leafL : C.leaf);
  }
  return p;
}

function bush(seed) {
  const X = 11, Y = 7, Z = 9, p = new Part('Bush', X, Y, Z);
  for (let x = 0; x < X; x++) for (let y = 0; y < Y; y++) for (let z = 0; z < Z; z++) {
    const d = (x - 5) ** 2 / 30 + y * y / 42 + (z - 4) ** 2 / 18;
    if (d < 1 - hash(x + seed, y, z) * 0.15) p.set(x, y, z, y >= Y - 2 ? C.leafL : hash(x, y, z) < 0.3 ? C.leafD : C.leaf);
  }
  for (let i = 0; i < 6; i++) { const x = 1 + Math.floor(hash(i, seed, 1) * 9), z = 1 + Math.floor(hash(seed, i, 2) * 7); for (let y = Y - 1; y >= 0; y--) if (p.has(x, y, z)) { p.set(x, Math.min(Y - 1, y + 1), z, i % 2 ? '#FF7FC8' : '#FFE15A'); break; } }
  return p;
}

/** Daisy facing the camera (petal disc in the XY plane) on a stem with two leaves. */
function flower(petal, centre, petalD) {
  const R = 4, X = 2 * R + 1, Y = 15, Z = 3, c = R, p = new Part('Flower', X, Y, Z), cy = Y - R - 1;
  for (let y = 0; y < cy; y++) p.set(c, y, 1, C.grassD);
  for (let k = 1; k <= 3; k++) { p.set(c - k, 3 + (k >> 1), 1, k === 3 ? C.leafL : C.leaf); p.set(c + k, 5 + (k >> 1), 1, k === 3 ? C.leafL : C.leaf); }
  for (let x = 0; x < X; x++) for (let y = cy - R; y <= cy + R; y++) {
    const dx = x - c, dy = y - cy, d = Math.hypot(dx, dy), a = Math.atan2(dy, dx);
    const lobe = R * (0.62 + 0.38 * Math.abs(Math.cos(2.5 * a)));
    if (d > lobe + 0.35) continue;
    const col = d < 1.6 ? centre : d > lobe - 0.9 ? petalD : petal;
    p.set(x, y, 0, col); if (d < 1.6) p.set(x, y, 1, centre);
  }
  return p;
}

function cloud(seed, w) {
  const X = w, Y = Math.round(w * 0.5), Z = Math.round(w * 0.35), p = new Part('Cloud', X, Y, Z);
  const puffs = [[0.5, 0.42, 0.34], [0.27, 0.3, 0.24], [0.74, 0.32, 0.25], [0.4, 0.62, 0.22], [0.62, 0.6, 0.2], [0.12, 0.2, 0.13], [0.88, 0.22, 0.14]];
  for (let x = 0; x < X; x++) for (let y = 0; y < Y; y++) for (let z = 0; z < Z; z++) {
    for (const [px, py, pr] of puffs) {
      const r = pr * X, d = ((x + .5 - px * X) ** 2 + ((y + .5 - py * Y) * 1.25) ** 2 + ((z + .5 - Z / 2) * 1.6) ** 2) / (r * r);
      if (d < 1 && y >= 1) { p.set(x, y, z, y < Y * 0.32 ? C.cloudS : C.cloud); break; }
    }
  }
  return p;
}

/** Display pedestal: thick purple base with glowing cyan lights, gold rim, golden top (top surface at local y = 0). */
function pedestal(R) {
  const X = 2 * R + 1, Y = 7, Z = X, p = new Part('Pedestal', X, Y, Z);
  for (let x = 0; x < X; x++) for (let z = 0; z < Z; z++) {
    const d = Math.hypot(x - R, z - R), a = Math.atan2(z - R, x - R);
    for (let y = 0; y < Y; y++) {
      const r = y === 0 ? R - 1 : R;
      if (d > r + 0.3) continue;
      let c;
      if (y === Y - 1) c = d > R - 1.2 ? C.goldL : d > R - 2.4 ? C.gold : d > R - 3.4 ? C.goldD : d > R - 4.4 ? C.gold : (Math.floor(d * 0.7) % 2 ? C.gold : C.goldL);
      else if (y === Y - 2) c = d > R - 0.8 ? C.goldD : C.purpleL;
      else if (y === Y - 3 && d > r - 0.8) c = C.purpleL;
      else if (d > r - 0.8 && y === 2 && Math.abs((((a / (Math.PI * 2)) * 20) % 1 + 1) % 1 - 0.5) < 0.16) c = C.gem;
      else c = y === 0 ? C.purpleD : (hash(x, y, z) < 0.15 ? C.purpleD : C.purple);
      p.set(x, y, z, c);
    }
  }
  return p;
}

// river centre line (world x, z), from the waterfall pond (right) to the front, then across to the left
const RIVER = [[2.6, 6.2], [2.2, 4.2], [2.5, 2.0], [2.3, -0.6], [1.2, -2.6], [-1.0, -3.4], [-3.5, -3.0], [-7, -3.6]];
function riverDist(x, z) {
  let best = 1e9;
  for (let k = 0; k + 1 < RIVER.length; k++) {
    const [ax, az] = RIVER[k], [bx, bz] = RIVER[k + 1], vx = bx - ax, vz = bz - az;
    const t = Math.max(0, Math.min(1, ((x - ax) * vx + (z - az) * vz) / (vx * vx + vz * vz)));
    best = Math.min(best, Math.hypot(x - ax - t * vx, z - az - t * vz));
  }
  return best;
}

/** Lawn with soft colour variation, the river with foam and stone banks, a cobblestone plaza at the front. */
const G = { size: 0.11, x0: -6.6, z0: -6.4, X: 120, Z: 190, Y: 3 };
function ground() {
  const { X, Y, Z, size, x0, z0 } = G, p = new Part('Ground', X, Y, Z);
  for (let ix = 0; ix < X; ix++) for (let iz = 0; iz < Z; iz++) {
    const wx = x0 + (ix + 0.5) * size, wz = z0 + (iz + 0.5) * size, dr = riverDist(wx, wz), w = 0.42;
    const river = dr < w, bank = !river && dr < w + 0.16, plaza = wz < -4.6;
    for (let y = 0; y < Y; y++) {
      let c;
      if (river) { if (y === Y - 1) continue; c = y === Y - 2 ? (dr > w - 0.1 ? C.foam : hash(ix >> 1, 2, iz >> 1) < 0.25 ? C.waterD : C.water) : C.waterD; }
      else if (y === Y - 1) {
        if (bank) c = hash(ix, 3, iz) < 0.5 ? C.stoneL : C.stone;
        else if (plaza) c = ((ix >> 1) + (iz >> 1)) % 3 === 0 ? C.stoneD : hash(ix, 5, iz) < 0.5 ? C.stoneL : '#C9B79C';
        else { const n = hash(ix >> 1, 0, iz >> 1), r = hash(ix, 1, iz); c = n < 0.22 ? C.grassL : n > 0.8 ? C.grassD : C.grass; if (r > 0.988) c = r > 0.994 ? '#FFE15A' : '#FF8FD0'; }
      } else c = y === 0 ? C.dirtD : C.dirt;
      p.set(ix, y, iz, c);
    }
  }
  return p;
}

/** Raised grass terrace with dirt sides (the ledges framing the scene in the reference). */
function terrace(w, d, h, seed) {
  const p = new Part('Terrace', w, h, d);
  for (let x = 0; x < w; x++) for (let z = 0; z < d; z++) {
    const edge = Math.min(x, z, w - 1 - x, d - 1 - z);
    const hh = h - (edge === 0 && hash(x + seed, 0, z) < 0.5 ? 1 : 0);
    for (let y = 0; y < hh; y++) p.set(x, y, z, y === hh - 1 ? (hash(x, y, z + seed) < 0.3 ? C.grassL : C.grass) : y === hh - 2 ? C.grassD : hash(x, y, z) < 0.35 ? C.dirtD : C.dirt);
  }
  return p;
}

/** Stone cliffs with a cascading waterfall falling into a pond (placed right of the mascot). */
function cliffs() {
  const X = 44, Y = 34, Z = 22, p = new Part('Cliffs', X, Y, Z), wx = 13;
  for (let x = 0; x < X; x++) for (let z = 0; z < Z; z++) {
    let h = Math.round(18 + 9 * Math.sin(x * 0.16 + 0.4) + 4 * Math.sin(x * 0.41) - (Z - 1 - z) * 0.9 + hash(x >> 2, 0, z >> 2) * 2);
    if (Math.abs(x - wx) <= 8) h = Math.max(h, Math.round(31 - (Z - 1 - z) * 0.55));
    h = Math.max(0, Math.min(h, Y - 1));
    for (let y = 0; y < h; y++) {
      const step = (y + Math.floor(x / 5)) % 6 === 0;
      p.set(x, y, z, y >= h - 1 ? (hash(x, y, z) < 0.35 ? C.grassL : C.grass) : step ? C.stoneD : hash(x >> 1, y >> 1, z) < 0.35 ? C.stoneL : C.stone);
    }
  }
  // cascade: three steps of falling water down the front of the high section
  for (let z = 0; z < Z; z++) for (let x = wx - 5; x <= wx + 5; x++) {
    let top = -1; for (let y = Y - 1; y >= 0; y--) if (p.has(x, y, z)) { top = y; break; }
    if (top < 1) continue;
    const front = !p.has(x, top, z - 1) || z === 0;
    p.set(x, top, z, hash(x, top, z) < 0.3 ? C.foam : C.water);
    if (front) for (let y = 0; y < top; y++) p.set(x, y, z, (y + x * 2) % 5 === 0 ? C.foam : (x === wx - 5 || x === wx + 5) ? C.waterD : C.water);
  }
  return p;
}

/** Low rolling hills on the horizon (fills the gap between the lawn and the sky). */
function farHills() {
  const X = 150, Y = 22, Z = 12, p = new Part('FarHills', X, Y, Z);
  for (let x = 0; x < X; x++) for (let z = 0; z < Z; z++) {
    const h = Math.round(13 + 4 * Math.sin(x * 0.09 + 1) + 3 * Math.sin(x * 0.23) - (Z - 1 - z) * 0.5 + hash(x >> 2, 1, z) * 1.5);
    for (let y = 0; y < Math.min(h, Y); y++) p.set(x, y, z, y >= h - 1 ? (hash(x, y, z) < 0.3 ? C.leafL : C.leaf) : y >= h - 3 ? C.leafD : hash(x, y, z) < 0.4 ? C.stoneD : C.stone);
  }
  return p;
}

/** Small voxel windmill (white tower, red roof, four wooden blades facing the camera). */
function windmill() {
  const X = 21, Y = 34, Z = 9, p = new Part('Windmill', X, Y, Z), cx = 10;
  for (let y = 0; y < 20; y++) { const r = 4 - Math.floor(y / 7); for (let x = cx - r; x <= cx + r; x++) for (let z = 4 - r; z <= 4 + r; z++) p.set(x, y, z, (y % 6 === 0) ? '#E3DED3' : '#F6F3EC'); }
  for (let y = 20; y < 25; y++) { const r = 4 - (y - 20); for (let x = cx - r; x <= cx + r; x++) for (let z = 4 - r; z <= 4 + r; z++) p.set(x, y, z, '#D9483B'); }
  for (let y = 2; y < 6; y++) p.set(cx, y, 0, '#7A4A2A');
  const hub = 18;
  for (let i = -9; i <= 9; i++) { p.set(cx + i, hub, 0, '#A0703E'); p.set(cx, hub + i, 0, '#A0703E');
    if (Math.abs(i) > 2) { p.set(cx + i, hub + (i > 0 ? 1 : -1), 0, '#F1E6C8'); p.set(cx + (i > 0 ? -1 : 1), hub + i, 0, '#F1E6C8'); } }
  return p;
}

// ---------------------------------------------------------------- composition
export function buildDiorama() {
  const models = {}, parts = [];
  const model = (key, part) => { if (!models[key]) models[key] = { X: part.X, Y: part.Y, Z: part.Z, palette: part.pal, cells: part.encode() }; return key; };
  const add = (key, part, pos, size) => parts.push({ model: model(key, part), pos: pos.map((v) => +v.toFixed(3)), size });
  const gy = -0.33; // lawn level (top of the ground)
  add('ground', ground(), [G.x0 + G.X * G.size / 2, gy - G.Y * G.size, G.z0 + G.Z * G.size / 2], G.size);
  add('pedestal', pedestal(15), [0, -7 * 0.07, 0], 0.07);
  // side terraces (raised lawn with dirt edges) and the waterfall cliffs on the right
  add('terraceL', terrace(30, 40, 6, 1), [-3.0, gy, 3.8], 0.09);
  add('terraceR', terrace(22, 30, 4, 2), [3.2, gy, 1.4], 0.09);
  add('cliffs', cliffs(), [3.3, gy - 0.05, 7.0], 0.1);
  add('farHills', farHills(), [0, gy - 0.1, 16.5], 0.12);
  for (let i = 0; i < 9; i++) { const x = -5 + i * 1.25 + (hash(i, 9, 9) - 0.5) * 0.5; if (x > 2 && x < 4.5) continue; add('far' + (i % 3), cubeTree(40 + (i % 3), 0.7), [x, gy + 0.2, 14.2 + hash(i, 1, 1) * 0.6], 0.09); }
  // trees: big ones framing the sides (partly cropped like the reference), smaller ones behind
  const T = [['treeA', cubeTree(3, 1.25), [-2.05, gy + 0.54, 3.4], 0.075], ['treeB', cubeTree(7, 1.0), [2.35, gy + 0.36, 2.6], 0.07],
    ['treeC', cubeTree(11, 0.9), [-1.25, gy, 7.2], 0.07], ['treeD', cubeTree(13, 0.85), [1.0, gy, 9.6], 0.07],
    ['treeE', cubeTree(17, 0.8), [-2.9, gy + 0.54, 6.6], 0.075], ['treeF', cubeTree(21, 0.75), [4.4, gy + 2.6, 10.6], 0.08],
    ['treeG', cubeTree(25, 0.8), [-0.2, gy, 12.5], 0.08], ['treeH', cubeTree(29, 0.7), [2.3, gy + 2.9, 9.8], 0.07]];
  for (const [k, prt, pos, s] of T) add(k, prt, pos, s);
  add('bush1', bush(1), [-1.5, gy, 1.0], 0.07); add('bush2', bush(4), [1.55, gy, 0.6], 0.07); add('bush3', bush(6), [-0.7, gy, 4.6], 0.07);
  // daisies facing the camera, at the sides of the pedestal and in the front corners
  const F = [['fP', flower('#B07CFF', '#FFD84A', '#8A55E8'), [-1.3, gy, -0.5], 0.042], ['fK', flower('#FF7FC8', '#FFD84A', '#E8549E'), [1.34, gy, -0.4], 0.042],
    ['fY', flower('#FFD23F', '#FF8A1F', '#F0A800'), [-1.6, gy + 0.54, 2.0], 0.045], ['fK2', flower('#FF9AD5', '#FFE15A', '#F06AB0'), [1.85, gy + 0.36, 1.2], 0.045],
    ['fP2', flower('#C9A2FF', '#FFE15A', '#9C6CF0'), [-1.12, gy, -1.9], 0.05], ['fK3', flower('#FF7FC8', '#FFE15A', '#E8549E'), [1.2, gy, -1.6], 0.05]];
  for (const [k, prt, pos, s] of F) add(k, prt, pos, s);
  // fluffy clouds beside the logo
  add('cloud1', cloud(1, 22), [-3.3, 3.3, 18], 0.09); add('cloud2', cloud(2, 20), [3.5, 3.7, 19], 0.09);
  // home backdrop only: windmill on the hills and fluffy clouds in the upper sky
  const home = (key, part, pos, size) => { add(key, part, pos, size); parts[parts.length - 1].only = 'home'; };
  home('windmill', windmill(), [4.4, gy + 3.2, 14.5], 0.11);
  home('hf1', flower('#FF7FC8', '#FFE15A', '#E8549E'), [1.15, gy, 0.4], 0.075); home('hf2', flower('#C9A2FF', '#FFE15A', '#9C6CF0'), [1.7, gy, 1.4], 0.06);
  home('hf3', flower('#FF9AD5', '#FFE15A', '#F06AB0'), [4.9, gy, 0.6], 0.075); home('hf4', flower('#FFD23F', '#FF8A1F', '#F0A800'), [4.4, gy, 1.6], 0.06);
  home('hcloud1', cloud(7, 26), [-1.0, 6.4, 18], 0.13); home('hcloud2', cloud(8, 22), [6.5, 7.2, 20], 0.13); home('hcloud3', cloud(9, 18), [2.0, 8.6, 24], 0.14);
  return { camera: CAMERA, homeCamera: HOME_CAMERA, homeSkip: HOME_SKIP, models, parts };
}

if (process.argv[1] && process.argv[1].endsWith('diorama.mjs')) {
  const d = buildDiorama();
  fs.mkdirSync(OUT.replace(/\/[^/]+$/, ''), { recursive: true });
  const json = JSON.stringify(d);
  fs.writeFileSync(OUT, json);
  console.log('wrote', OUT, (json.length / 1024).toFixed(0) + ' KB', Object.keys(d.models).length, 'models', d.parts.length, 'parts');
}
