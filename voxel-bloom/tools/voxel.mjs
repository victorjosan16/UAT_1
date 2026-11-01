// Minimal voxel construction kit used to author the original Voxel Bloom models.
// Coordinates: x = right, y = up, z = depth. The model FRONT faces -z (toward the camera).
export class Grid {
  constructor(X, Y, Z) { this.X = X; this.Y = Y; this.Z = Z; this.v = new Int16Array(X * Y * Z).fill(-1); }
  in(x, y, z) { return x >= 0 && y >= 0 && z >= 0 && x < this.X && y < this.Y && z < this.Z; }
  i(x, y, z) { return x + this.X * (y + this.Y * z); }
  get(x, y, z) { return this.in(x, y, z) ? this.v[this.i(x, y, z)] : -1; }
  set(x, y, z, c) { if (this.in(x, y, z)) this.v[this.i(x, y, z)] = c; return this; }
  // paint only existing voxels
  paint(x, y, z, c) { if (this.get(x, y, z) >= 0) this.v[this.i(x, y, z)] = c; return this; }
  box(x0, y0, z0, x1, y1, z1, c, mode = 'set') {
    for (let x = Math.min(x0, x1); x <= Math.max(x0, x1); x++)
      for (let y = Math.min(y0, y1); y <= Math.max(y0, y1); y++)
        for (let z = Math.min(z0, z1); z <= Math.max(z0, z1); z++) this.op(mode, x, y, z, c);
    return this;
  }
  ell(cx, cy, cz, rx, ry, rz, c, mode = 'set') {
    for (let x = Math.floor(cx - rx); x <= Math.ceil(cx + rx); x++)
      for (let y = Math.floor(cy - ry); y <= Math.ceil(cy + ry); y++)
        for (let z = Math.floor(cz - rz); z <= Math.ceil(cz + rz); z++) {
          const d = ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2 + ((z - cz) / rz) ** 2;
          if (d <= 1.0) this.op(mode, x, y, z, c);
        }
    return this;
  }
  // cylinder along an axis ('x' | 'y' | 'z')
  cyl(axis, cx, cy, cz, r, len, c, mode = 'set', r2 = r) {
    for (let t = 0; t < len; t++) {
      const rr = r + (r2 - r) * (len > 1 ? t / (len - 1) : 0);
      for (let a = -Math.ceil(rr); a <= Math.ceil(rr); a++)
        for (let b = -Math.ceil(rr); b <= Math.ceil(rr); b++) {
          if (a * a + b * b > rr * rr + 0.25) continue;
          if (axis === 'y') this.op(mode, Math.round(cx + a), cy + t, Math.round(cz + b), c);
          else if (axis === 'x') this.op(mode, cx + t, Math.round(cy + a), Math.round(cz + b), c);
          else this.op(mode, Math.round(cx + a), Math.round(cy + b), cz + t, c);
        }
    }
    return this;
  }
  op(mode, x, y, z, c) {
    if (mode === 'set') this.set(x, y, z, c);
    else if (mode === 'paint') this.paint(x, y, z, c);
    else if (mode === 'clear') this.set(x, y, z, -1);
  }
  // recolor the front-most voxel at (x, y)
  front(x, y, c) { for (let z = 0; z < this.Z; z++) if (this.get(x, y, z) >= 0) { this.v[this.i(x, y, z)] = c; return z; } return -1; }
  // add a voxel in front of the front-most voxel at (x, y)
  addFront(x, y, c, depth = 1) {
    for (let z = 0; z < this.Z; z++) if (this.get(x, y, z) >= 0) { for (let d = 1; d <= depth; d++) this.set(x, y, z - d, c); return; }
  }
  mirrorX() { // copy left half (x < X/2) onto right half
    for (let x = 0; x < Math.floor(this.X / 2); x++)
      for (let y = 0; y < this.Y; y++) for (let z = 0; z < this.Z; z++)
        this.v[this.i(this.X - 1 - x, y, z)] = this.v[this.i(x, y, z)];
    return this;
  }
  count() { let n = 0; for (const c of this.v) if (c >= 0) n++; return n; }
  // Crop to bounding box and return a plain object.
  trim() {
    let mn = [1e9, 1e9, 1e9], mx = [-1, -1, -1];
    for (let z = 0; z < this.Z; z++) for (let y = 0; y < this.Y; y++) for (let x = 0; x < this.X; x++)
      if (this.get(x, y, z) >= 0) { mn = [Math.min(mn[0], x), Math.min(mn[1], y), Math.min(mn[2], z)]; mx = [Math.max(mx[0], x), Math.max(mx[1], y), Math.max(mx[2], z)]; }
    const g = new Grid(mx[0] - mn[0] + 1, mx[1] - mn[1] + 1, mx[2] - mn[2] + 1);
    g.offset = mn;
    for (let z = mn[2]; z <= mx[2]; z++) for (let y = mn[1]; y <= mx[1]; y++) for (let x = mn[0]; x <= mx[0]; x++)
      g.set(x - mn[0], y - mn[1], z - mn[2], this.get(x, y, z));
    return g;
  }
}

// Mulberry32 deterministic RNG (mirrored in C# DeterministicRandom).
export function rng(seed) {
  let a = seed >>> 0;
  return function () {
    a = (a + 0x6D2B79F5) >>> 0;
    let t = a;
    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

export const ALPHABET = '0123456789abcdefghijklmnopqrstuvwxyz';
export function encode(g) {
  let s = '';
  for (let i = 0; i < g.v.length; i++) s += g.v[i] < 0 ? '.' : ALPHABET[g.v[i]];
  return s;
}

// Connects every detached component to the largest one with a straight voxel bridge (component colour),
// so no model contains floating cubes. Returns the number of bridges built.
export function connectComponents(g) {
  let bridges = 0;
  for (let guard = 0; guard < 64; guard++) {
    const comp = new Int32Array(g.v.length).fill(-1); const sizes = [];
    for (let i = 0; i < g.v.length; i++) if (g.v[i] >= 0 && comp[i] < 0) {
      const id = sizes.length; let n = 0; const st = [i]; comp[i] = id;
      while (st.length) { const j = st.pop(); n++; const x = j % g.X, y = Math.floor(j / g.X) % g.Y, z = Math.floor(j / (g.X * g.Y));
        for (const [a, b, c] of [[1,0,0],[-1,0,0],[0,1,0],[0,-1,0],[0,0,1],[0,0,-1]]) { if (!g.in(x+a,y+b,z+c)) continue; const k = g.i(x+a,y+b,z+c); if (g.v[k] >= 0 && comp[k] < 0) { comp[k] = id; st.push(k); } } }
      sizes.push(n);
    }
    if (sizes.length <= 1) return bridges;
    const main = sizes.indexOf(Math.max(...sizes));
    const other = sizes.findIndex((s, id) => id !== main);
    // nearest pair (Manhattan) between `other` and `main`
    const A = [], B = [];
    for (let i = 0; i < g.v.length; i++) { if (comp[i] === other) A.push(i); else if (comp[i] === main) B.push(i); }
    let best = null, bd = 1e9;
    const P = (i) => [i % g.X, Math.floor(i / g.X) % g.Y, Math.floor(i / (g.X * g.Y))];
    for (const a of A) { const pa = P(a); for (const b of B) { const pb = P(b); const d = Math.abs(pa[0]-pb[0]) + Math.abs(pa[1]-pb[1]) + Math.abs(pa[2]-pb[2]); if (d < bd) { bd = d; best = [pa, pb, g.v[a]]; } } }
    let [p, q, c] = best; p = [...p];
    while (p[0] !== q[0] || p[1] !== q[1] || p[2] !== q[2]) {
      const ax = [0, 1, 2].find((t) => p[t] !== q[t]); p[ax] += Math.sign(q[ax] - p[ax]);
      const i = g.i(p[0], p[1], p[2]); if (g.v[i] < 0) g.v[i] = c;
    }
    bridges++;
  }
  return bridges;
}
