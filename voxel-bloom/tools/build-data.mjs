// Builds all runtime data for Voxel Bloom 3D (rules v2: 4 columns, 4 launchers):
//   Models  -> Assets/VoxelBloom/Data/Resources/VoxelBloom/Models/<id>.json   (120: 100 mascots + 20 objects)
//   Levels  -> Assets/VoxelBloom/Data/Resources/VoxelBloom/Levels/level_###.json (120, multicolour, solver-validated)
//   Config  -> Assets/VoxelBloom/Data/Resources/VoxelBloom/Config/game_config.json
// Usage: node tools/build-data.mjs
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
import { buildAll, MASCOT_ORDER } from './models.mjs';
import { Grid, rng, encode } from './voxel.mjs';
import { solveGreedy, randomWinRate, replay } from './rules.mjs';
import { RO, CHAPTERS_RO } from './names-ro.mjs';

const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const OUT = path.join(ROOT, 'Assets/VoxelBloom/Data/Resources/VoxelBloom');
for (const d of ['Models', 'Levels', 'Config']) fs.mkdirSync(path.join(OUT, d), { recursive: true });

const all = buildAll();
const models = Object.fromEntries(all.map((m) => [m.id, m]));
// Chapter 1 = the 20 objects of the reference image, in its order; then the other mascots/objects (one level each).
const FEATURED = ['castle', 'house', 'car', 'cake', 'rocket', 'windmill', 'train', 'robot', 'excavator', 'ship', 'plane', 'balloon', 'ferriswheel', 'stadium', 'lighthouse', 'dragon', 'elephant', 'bear', 'dolphin', 'pyramid'];
const EXTRA_DROP = new Set(['icecream', 'candy', 'watermelon', 'burger']); // kept out of levels (keeps 120 unique levels)
const ORDER = FEATURED.concat(MASCOT_ORDER().filter((id) => !FEATURED.includes(id) && !EXTRA_DROP.has(id)));
if (ORDER.length !== 120 || ORDER.some((id) => !models[id])) throw new Error('level order must list 120 existing models, got ' + ORDER.length);
for (const id of EXTRA_DROP) delete models[id];
const CH_NAMES = CHAPTERS_RO; // Romanian UI
for (const m of all) if (RO[m.id]) m.name = RO[m.id];
const CHAPTERS = CH_NAMES.map((name, i) => ({ id: i + 1, name, list: ORDER.slice(i * 20, i * 20 + 20) }));
const CATEGORY = Object.fromEntries(CHAPTERS.flatMap((c) => c.list.map((id) => [id, c.name])));

// The 12-colour level palette from the reference (red, orange, yellow, green, cyan, blue, purple, pink, light pink, white, brown, black)
const VIVID = ['#FF3B3B', '#FF8A1F', '#FFD23F', '#3DCC4A', '#2EC4E8', '#2F6BFF', '#8E4DFF', '#FF5FB8', '#FFB3D9', '#F5F5F5', '#8B5A2B', '#2B2B36'];
const REGION_COLORS = VIVID.slice(0, 9).concat(['#8B5A2B']); // not white/black: those stay for eyes and highlights

/**
 * Multicolour recolouring: small "feature" colours (eyes, nose, highlights) keep their colour; large colour regions are
 * split along their longest axis until the model has `target` colours, then mapped to distinct vivid colours.
 */
function multicolor(m, target, r, inner = 0) {
  const g = m.grid, total = g.count();
  const byColor = new Map();
  for (let i = 0; i < g.v.length; i++) { const c = g.v[i]; if (c < 0) continue; if (!byColor.has(c)) byColor.set(c, []); byColor.get(c).push(i); }
  const features = [], regions = [];
  for (const [c, list] of byColor) (list.length < total * 0.03 ? features : regions).push({ c, list });
  const want = Math.max(3, target - features.length - inner);
  const pos = (i) => [i % g.X, Math.floor(i / g.X) % g.Y, Math.floor(i / (g.X * g.Y))];
  while (regions.length < want) {
    regions.sort((a, b) => b.list.length - a.list.length);
    const big = regions[0]; if (big.list.length < 8) break;
    const ps = big.list.map(pos);
    const ext = [0, 1, 2].map((a) => Math.max(...ps.map((p) => p[a])) - Math.min(...ps.map((p) => p[a])));
    const axis = ext[1] >= ext[0] * 0.8 && ext[1] >= ext[2] * 0.8 ? 1 : ext.indexOf(Math.max(...ext));
    const sorted = big.list.slice().sort((a, b) => pos(a)[axis] - pos(b)[axis] || a - b);
    const half = Math.floor(sorted.length / 2);
    regions.splice(0, 1, { c: big.c, list: sorted.slice(0, half) }, { c: big.c, list: sorted.slice(half) });
  }
  const colors = REGION_COLORS.slice();
  for (let i = colors.length - 1; i > 0; i--) { const j = Math.floor(r() * (i + 1)); [colors[i], colors[j]] = [colors[j], colors[i]]; }
  const palette = [], v = new Int16Array(g.v.length).fill(-1);
  regions.forEach((reg, k) => { const ci = palette.length; palette.push(colors[k % colors.length]); for (const i of reg.list) v[i] = ci; });
  // inner layers (onion): voxels at depth >= 2 (and >= 4) get colours that are not on the surface -> launchers must wait
  if (inner > 0) {
    const X = g.X, Y = g.Y, Z = g.Z, depth = new Int16Array(g.v.length).fill(-1), q = [];
    for (let i = 0; i < g.v.length; i++) {
      if (g.v[i] < 0) continue;
      const [x, y, z] = pos(i);
      if ([[1,0,0],[-1,0,0],[0,1,0],[0,-1,0],[0,0,1],[0,0,-1]].some(([a, b, c]) => g.get(x + a, y + b, z + c) < 0)) { depth[i] = 0; q.push(i); }
    }
    for (let h = 0; h < q.length; h++) { const i = q[h], [x, y, z] = pos(i); for (const [a, b, c] of [[1,0,0],[-1,0,0],[0,1,0],[0,-1,0],[0,0,1],[0,0,-1]]) { if (!g.in(x + a, y + b, z + c)) continue; const j = g.i(x + a, y + b, z + c); if (g.v[j] >= 0 && depth[j] < 0) { depth[j] = depth[i] + 1; q.push(j); } } }
    const free = colors.slice(regions.length);
    const isFeature = new Set(features.flatMap((f) => f.list));
    for (let layer = 0; layer < inner && layer < free.length; layer++) {
      const ci = palette.length; palette.push(free[layer]);
      const minD = 2 + layer * 2;
      for (let i = 0; i < v.length; i++) if (depth[i] >= minD && !isFeature.has(i)) v[i] = ci;
    }
  }
  for (const f of features) { let hex = m.palette[f.c]; let ci = palette.indexOf(hex); if (ci < 0) { ci = palette.length; palette.push(hex); } for (const i of f.list) v[i] = ci; }
  // merge identical colours (a feature may equal a vivid colour)
  const uniq = [], remap = palette.map((h) => { let j = uniq.indexOf(h); if (j < 0) { j = uniq.length; uniq.push(h); } return j; });
  for (let i = 0; i < v.length; i++) if (v[i] >= 0) v[i] = remap[v[i]];
  return { palette: uniq, v, size: [g.X, g.Y, g.Z] };
}

/** Recolours part of the interior (invisible at start) with one palette colour that is rare on the surface. */
function hiddenCore(mc, share, r) {
  const [X, Y, Z] = mc.size, v = mc.v, g = { get: (x, y, z) => (x < 0 || y < 0 || z < 0 || x >= X || y >= Y || z >= Z ? -1 : v[x + X * (y + Y * z)]) };
  const interior = [];
  for (let z = 0; z < Z; z++) for (let y = 0; y < Y; y++) for (let x = 0; x < X; x++) {
    const i = x + X * (y + Y * z); if (v[i] < 0) continue;
    if ([[1,0,0],[-1,0,0],[0,1,0],[0,-1,0],[0,0,1],[0,0,-1]].every(([a, b, c]) => g.get(x + a, y + b, z + c) >= 0)) interior.push(i);
  }
  if (interior.length < 20) return 0;
  const core = Math.floor(r() * mc.palette.length);
  const seed = interior[Math.floor(r() * interior.length)], P = (i) => [i % X, Math.floor(i / X) % Y, Math.floor(i / (X * Y))], q = P(seed);
  const sorted = interior.map((i) => { const p = P(i); return [i, Math.hypot(p[0] - q[0], p[1] - q[1], p[2] - q[2])]; }).sort((a, b) => a[1] - b[1]);
  const n = Math.floor(interior.length * share);
  for (let k = 0; k < n; k++) v[sorted[k][0]] = core;
  return n;
}

/** Fraction of each colour visible at the start (used to bias hidden colours upwards in the columns). */
function exposedFraction(level) {
  const [X, Y, Z] = level.size, cell = Array.from(level.voxels, (ch) => (ch === '.' ? -1 : parseInt(ch, 36)));
  const get = (x, y, z) => (x < 0 || y < 0 || z < 0 || x >= X || y >= Y || z >= Z ? -1 : cell[x + X * (y + Y * z)]);
  const exp = new Array(level.palette.length).fill(0), cnt = new Array(level.palette.length).fill(0);
  for (let z = 0; z < Z; z++) for (let y = 0; y < Y; y++) for (let x = 0; x < X; x++) {
    const c = get(x, y, z); if (c < 0) continue; cnt[c]++;
    if ([[1,0,0],[-1,0,0],[0,1,0],[0,-1,0],[0,0,1],[0,0,-1]].some(([a, b, cc]) => get(x + a, y + b, z + cc) < 0)) exp[c]++;
  }
  return exp.map((e, i) => (cnt[i] ? e / cnt[i] : 1));
}

/** Splits each colour count into blocks and deals them into 4 columns (top = index 0).
 *  `bias` > 0 moves blocks of mostly-hidden colours towards the top (they must wait in a launcher). */
function buildColumns(counts, d, r, expFrac = null, bias = 0, sizeScale = 1) {
  const maxB = Math.round((150 - d * 90) * sizeScale), minB = Math.max(10, Math.round((36 - d * 22) * sizeScale));
  const blocks = [];
  counts.forEach((n, c) => {
    if (!n) return;
    const parts = Math.max(1, Math.ceil(n / maxB));
    let left = n;
    for (let p = 0; p < parts; p++) {
      const rest = parts - p;
      let size = rest === 1 ? left : Math.round(left / rest * (0.75 + r() * 0.5));
      size = Math.max(Math.min(size, left - minB * (rest - 1)), Math.min(minB, left));
      blocks.push([c, size]); left -= size;
    }
  });
  const keyed = blocks.map((b) => [b, r() - (expFrac ? bias * (1 - expFrac[b[0]]) : 0)]);
  keyed.sort((a, b) => a[1] - b[1]);
  blocks.length = 0; keyed.forEach(([b]) => blocks.push(b));
  const cols = [[], [], [], []];
  blocks.forEach((b, i) => cols[i % 4].push(b));
  return cols;
}

// write models (original colours: used by Home, Collection and thumbnails)
const modelIndex = [];
for (const m of all.filter((x) => models[x.id])) {
  m.category = CATEGORY[m.id];
  const g = m.grid;
  fs.writeFileSync(path.join(OUT, 'Models', m.id + '.json'), JSON.stringify({ id: m.id, name: m.name, category: m.category, size: [g.X, g.Y, g.Z], palette: m.palette, voxels: encode(g), anim: m.anim }));
  modelIndex.push({ id: m.id, name: m.name, category: m.category, voxels: g.count() });
}

const FROM = +process.env.VB_FROM || 1; // resume: levels below FROM are read back from disk
const LIMIT = +process.env.VB_LIMIT || 1e9; // quick probes: VB_LIMIT=12 node tools/build-data.mjs
const levels = []; const report = []; let n = 0;
const T = 119;
for (const ch of CHAPTERS) {
  ch.list.forEach((id, k) => {
    n++;
    if (n > LIMIT) return;
    if (n < FROM) {
      const l = JSON.parse(fs.readFileSync(path.join(OUT, 'Levels', `level_${String(n).padStart(3, '0')}.json`)));
      levels.push(l);
      report.push(`${String(n).padStart(3)} ch${ch.id} ${l.name.padEnd(18)} vox=${String(l.voxelCount).padStart(4)} colors=${String(l.palette.length).padStart(2)} blocks=${String(l.columns.flat().length).padStart(3)} witness=${String(l.witnessMoves).padStart(3)} ${l.hard ? 'HARD' : '    '} casual=${l.casualWinRate} reward=${l.rewardCoins}`);
      return;
    }
    const d = (n - 1) / T;
    const hard = n % 5 === 0;
    const target = Math.max(0.12, (n <= 2 ? 0.95 : 0.9 - 0.5 * d) - (hard ? 0.3 : 0));
    const nColors = 6 + Math.min(4, Math.floor(d * 5));
    let best = null;
    for (let attempt = 0; attempt < 40 && !(best && attempt >= 20); attempt++) {
      const relax = Math.floor(attempt / 10); // after 10 failed attempts: fewer inner layers, weaker bias
      const seed = 2000 + n * 7919 + attempt * 104729;
      const r = rng(seed);
      const innerLayers = n <= 3 ? 0 : Math.max(0, Math.min(3, 1 + Math.floor(d * 2.5) + (hard ? 1 : 0)) - relax);
      const mc = multicolor(models[id], nColors, r, innerLayers);
      const core = n >= 26 && n % 3 === 0 ? hiddenCore(mc, 0.3 + 0.3 * d, r) : 0;
      const counts = new Array(mc.palette.length).fill(0); for (const c of mc.v) if (c >= 0) counts[c]++;
      const keep = counts.map((c, i) => (c > 0 ? i : -1)).filter((i) => i >= 0);
      const rem = new Map(keep.map((o, i) => [o, i]));
      const palette = keep.map((i) => mc.palette[i]);
      const cnt = keep.map((i) => counts[i]);
      const voxels = Array.from(mc.v, (c) => (c < 0 ? '.' : rem.get(c).toString(36))).join('');
      const probe = { size: mc.size, palette, voxels };
      const bias = n <= 2 ? 0 : Math.min(4, 0.6 + d * 1.5 + (hard ? 0.8 : 0) + (attempt % 10) * 0.15) * Math.pow(0.6, relax);
      const columns = buildColumns(cnt, d, r, exposedFraction(probe), bias, Math.max(0.45, 1 - d * 0.3 - attempt * 0.02));
      const level = { id: n, hiddenCore: core, chapter: ch.id, indexInChapter: k + 1, name: models[id].name, model: id, difficulty: +d.toFixed(3), hard, seed, size: mc.size, palette, voxels, voxelCount: cnt.reduce((a, b) => a + b), colorCounts: cnt, launchers: 4, columns };
      const sol = solveGreedy(level);
      if (!sol.won) continue;
      const rep = replay(level, sol.taps);
      if (!rep.ok || !rep.s.isWon() || rep.s.score !== sol.score) throw new Error('witness replay mismatch at level ' + n);
      const rate = randomWinRate(level, 24, seed);
      const err = Math.abs(rate - target);
      if (palette.length >= 6 && (!best || err < best.err)) best = { level, sol, rate, err };
      if (palette.length < 6) continue; // the reference asks for 6-10 colours per object
      if (err < 0.08) break;
    }
    if (!best) throw new Error('no solvable configuration for level ' + n + ' ' + id);
    const { level, sol, rate } = best;
    level.witness = sol.taps; level.witnessMoves = sol.moves; level.witnessScore = sol.score;
    level.casualWinRate = +rate.toFixed(2);
    level.starScore = [Math.floor(sol.score * 0.85), Math.floor(sol.score * 0.55)];
    level.rewardCoins = Math.round((40 + d * 80) * (hard ? 2.5 : 1));
    level.boosters = { hammer: n >= 3, shuffle: n >= 5, colorBomb: n >= 8, eliminate: true };
    levels.push(level);
    fs.writeFileSync(path.join(OUT, 'Levels', `level_${String(n).padStart(3, '0')}.json`), JSON.stringify(level));
    report.push(`${String(n).padStart(3)} ch${ch.id} ${level.name.padEnd(18)} vox=${String(level.voxelCount).padStart(4)} colors=${String(level.palette.length).padStart(2)} blocks=${String(level.columns.flat().length).padStart(3)} witness=${String(sol.moves).padStart(3)} ${hard ? 'HARD' : '    '} casual=${level.casualWinRate} target=${target.toFixed(2)} reward=${level.rewardCoins}`);
  });
}

if (levels.length < LIMIT && LIMIT < 1e9) process.exit(0);
const sigs = new Set(levels.map((l) => l.voxels + '|' + JSON.stringify(l.columns)));
if (sigs.size !== levels.length) throw new Error('duplicate level configuration detected');

const config = {
  version: 2,
  rules: { columns: 4, launchers: 4, note: 'v2: only the top block of each column is active; launchers fire one voxel per round' },
  chapters: CHAPTERS.map((c) => ({ id: c.id, name: c.name, firstLevel: (c.id - 1) * 20 + 1, lastLevel: c.id * 20, unlockStars: [0, 30, 70, 110, 150, 190][c.id - 1], thumbnailModel: c.list[0] })),
  collection: modelIndex.map((m) => ({ ...m, unlockLevel: levels.find((l) => l.model === m.id)?.id ?? null })),
  hardLevels: { every: 5, rewardMultiplier: 2.5, note: 'every 5th level is HARD: harder to finish, bigger reward' },
  economy: { startCoins: 500, startLives: 5, maxLives: 5, lifeRegenMinutes: 30, dailyRewards: [100, 150, 200, 250, 300, 400, 600], comboCoinBonus: 5 },
  boosters: {
    hammer: { price: 300, startCount: 3, voxels: 12 },
    shuffle: { price: 200, startCount: 3 },
    colorBomb: { price: 500, startCount: 1, voxels: 60 },
    // 'Eliminare': empties one stuck launcher (its block is discarded), freeing it.
    eliminate: { price: 150, startCount: 2 },
  },
  stars: { note: 'starScore[0] = min score for 3 stars, starScore[1] = min score for 2 stars, otherwise 1 star' },
};
fs.writeFileSync(path.join(OUT, 'Config', 'game_config.json'), JSON.stringify(config, null, 2));
fs.writeFileSync(path.join(ROOT, 'Docs', 'LevelReport.txt'), report.join('\n') + '\n');
console.log(report.join('\n'));
console.log(`\n${levels.length} levels, ${all.length} models written to ${path.relative(ROOT, OUT)}`);
