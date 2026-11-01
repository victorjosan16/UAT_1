// Builds all runtime data for Voxel Bloom 3D:
//   Models  -> Assets/VoxelBloom/Data/Resources/VoxelBloom/Models/<id>.json
//   Levels  -> Assets/VoxelBloom/Data/Resources/VoxelBloom/Levels/level_###.json (100, solver-validated)
//   Config  -> Assets/VoxelBloom/Data/Resources/VoxelBloom/Config/game_config.json
// Usage: node tools/build-data.mjs
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
import { buildAll, COLORS } from './models.mjs';
import { Grid, rng, encode } from './voxel.mjs';
import { Session, solveGreedy, randomWinRate } from './rules.mjs';

const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const OUT = path.join(ROOT, 'Assets/VoxelBloom/Data/Resources/VoxelBloom');
for (const d of ['Models', 'Levels', 'Config']) fs.mkdirSync(path.join(OUT, d), { recursive: true });

const models = Object.fromEntries(buildAll().map((m) => [m.id, m]));
if (Object.keys(models).length !== 100) throw new Error('expected 100 mascots');

// ---------------------------------------------------------------- variants (colour-name substitutions)
const VARIANTS = {
  'chicken:golden': ['Golden Chick', { yellow: 'gold', white: 'cream' }],
  'bunny:choco': ['Chocolate Bunny', { white: 'choco', pink: 'tan', black: 'cream' }],
  'penguin:royal': ['Royal Penguin', { navy: 'yellow', orange: 'red' }],
  'bear:polar': ['Polar Bear', { brown: 'white', tan: 'lightGrey', darkBrown: 'grey' }],
  'cat:ginger': ['Ginger Cat', { grey: 'orange', darkGrey: 'brown' }],
  'fox:arctic': ['Arctic Fox', { orange: 'white', white: 'lightGrey', darkBrown: 'grey' }],
  'panda:red': ['Red Panda', { white: 'orange', black: 'darkBrown', pink: 'cream' }],
  'wolf:night': ['Night Wolf', { grey: 'navy', darkGrey: 'black', white: 'sky' }],
  'deer:snow': ['Snow Deer', { brown: 'white', white: 'sky', tan: 'gold' }],
  'cake:choco': ['Chocolate Cake', { pink: 'choco', cream: 'tan' }],
  'cake:lemon': ['Lemon Cake', { pink: 'yellow', red: 'orange' }],
  'icecream:berry': ['Berry Cone', { mint: 'purple', choco: 'hotPink' }],
  'candy:mint': ['Mint Candy', { hotPink: 'teal', purple: 'mint' }],
  'donut:blue': ['Blueberry Donut', { pink: 'sky' }],
  'donut:choco': ['Choco Donut', { pink: 'choco', tan: 'waffle' }],
  'cupcake:lemon': ['Lemon Cupcake', { pink: 'yellow', purple: 'teal', lavender: 'mint' }],
  'burger:veggie': ['Veggie Burger', { choco: 'darkGreen', red: 'orange' }],
  'car:blue': ['Blue Racer', { red: 'blue', yellow: 'white' }],
  'car:mint': ['Mint Cruiser', { red: 'mint' }],
  'bus:city': ['City Bus', { yellow: 'green', orange: 'darkGreen' }],
  'truck:ice': ['Ice Cream Truck', { red: 'pink', blue: 'yellow' }],
  'house:blue': ['Blue Cottage', { red: 'blue', cream: 'white' }],
  'rocket:moon': ['Moon Rocket', { red: 'purple', blue: 'teal' }],
  'robot:gold': ['Gold Robot', { lightGrey: 'gold', blue: 'red' }],
  'lighthouse:blue': ['Harbour Light', { red: 'blue' }],
  'fish:tang': ['Blue Tang', { orange: 'blue', white: 'yellow' }],
  'crab:golden': ['Golden Crab', { red: 'gold', orange: 'yellow' }],
  'octopus:pink': ['Pink Octopus', { purple: 'hotPink', lavender: 'pink' }],
  'turtle:sea': ['Sea Turtle', { green: 'teal', darkGreen: 'navy', lime: 'mint' }],
  'whale:orca': ['Orca', { blue: 'black', sky: 'white', navy: 'darkGrey' }],
  'castle:sunset': ['Sunset Castle', { lavender: 'cream', pink: 'orange', purple: 'red' }],
  'dragon:fire': ['Fire Dragon', { green: 'red', lime: 'orange', purple: 'darkBrown', red: 'yellow' }],
  'unicorn:night': ['Night Unicorn', { white: 'navy', pink: 'purple', lavender: 'sky' }],
  'windmill:spring': ['Spring Mill', { tan: 'white', red: 'blue' }],
};

// ---------------------------------------------------------------- the 100 levels
import { MASCOT_ORDER } from './models.mjs';
// 100 mascots, one per level (level N = mascot N). Later levels add hidden cores for difficulty.
const ORDER = MASCOT_ORDER();
const CH_NAMES = ['Happy Friends', 'Wild Safari', 'Sky & Sea', 'Tiny Wonders', 'Wild North'];
const CHAPTERS = CH_NAMES.map((name, i) => ({
  id: i + 1, name,
  list: ORDER.slice(i * 20, i * 20 + 20).map((id, j) => { const n = i * 20 + j + 1; return n >= 26 && n % 3 === 0 ? id + '#core' : id; }),
}));

function hexDist(a, b) { const p = (h) => [1, 3, 5].map((i) => parseInt(h.slice(i, i + 2), 16)); const [x, y] = [p(a), p(b)]; return Math.hypot(x[0] - y[0], x[1] - y[1], x[2] - y[2]); }

function makeVariant(spec) {
  const [base, core] = spec.split('#');
  const [modelId] = base.split(':');
  const m = models[modelId];
  if (!m) throw new Error('unknown model ' + modelId);
  const variant = VARIANTS[base];
  const subs = variant ? variant[1] : {};
  const name = variant ? variant[0] : m.name;
  // substitute names, merge duplicate colours
  const names = m.paletteNames.map((n) => subs[n] || n);
  const hexes = names.map((n) => { if (!COLORS[n]) throw new Error('colour ' + n); return COLORS[n]; });
  const uniq = [], remap = [];
  hexes.forEach((h) => { let j = uniq.indexOf(h); if (j < 0) { j = uniq.length; uniq.push(h); } remap.push(j); });
  const g = m.grid; const v = new Int16Array(g.v.length);
  for (let i = 0; i < v.length; i++) v[i] = g.v[i] < 0 ? -1 : remap[g.v[i]];
  return { modelId, name: core ? name + ' ✦' : name, variant: base.includes(':') ? base.split(':')[1] : '', hasCore: core === 'core', size: [g.X, g.Y, g.Z], palette: uniq, v };
}

// Recolour a share of the interior (never visible at start) with a contrasting palette colour.
function applyHiddenCore(lv, share, r) {
  const [X, Y, Z] = lv.size; const g = new Grid(X, Y, Z); g.v = lv.v;
  const interior = [];
  for (let z = 0; z < Z; z++) for (let y = 0; y < Y; y++) for (let x = 0; x < X; x++) {
    if (g.get(x, y, z) < 0) continue;
    const n = [[1, 0, 0], [-1, 0, 0], [0, 1, 0], [0, -1, 0], [0, 0, 1], [0, 0, -1]];
    if (n.every(([a, b, c]) => g.get(x + a, y + b, z + c) >= 0)) interior.push(g.i(x, y, z));
  }
  if (!interior.length) return 0;
  // choose the palette colour most different from the dominant interior colour
  const cnt = new Array(lv.palette.length).fill(0); interior.forEach((i) => cnt[g.v[i]]++);
  const dom = cnt.indexOf(Math.max(...cnt));
  let core = 0, best = -1; lv.palette.forEach((h, j) => { const d = hexDist(h, lv.palette[dom]); if (j !== dom && d > best) { best = d; core = j; } });
  // blobby selection: seeds + radius
  const seeds = Array.from({ length: 4 }, () => interior[Math.floor(r() * interior.length)]);
  const pos = (i) => [i % X, Math.floor(i / X) % Y, Math.floor(i / (X * Y))];
  let changed = 0; const target = Math.floor(interior.length * share);
  const scored = interior.map((i) => { const p = pos(i); return [i, Math.min(...seeds.map((s) => { const q = pos(s); return Math.hypot(p[0] - q[0], p[1] - q[1], p[2] - q[2]); }))]; }).sort((a, b) => a[1] - b[1]);
  for (const [i] of scored) { if (changed >= target) break; g.v[i] = core; changed++; }
  return changed;
}

const POWERS = [2, 4, 8, 16, 32, 64];
function buildDeck(counts, d, slack, r) {
  // larger powers early (shorter levels), richer mix later; very large models keep big powers so levels stay < ~70 taps
  const total = counts.reduce((a, b) => a + b, 0);
  let allowed = d < 0.03 ? [32, 64] : d < 0.15 ? [16, 32, 64] : d < 0.4 ? [8, 16, 32, 64] : d < 0.7 ? [4, 8, 16, 32, 64] : [2, 4, 8, 16, 32, 64];
  if (total > 1400) allowed = allowed.filter((p) => p >= 16);
  if (total > 2200) allowed = [32, 64, 64];
  const deck = [];
  counts.forEach((n, c) => {
    if (n === 0) return;
    let t = Math.ceil(n * (1 + slack));
    while (t > 0) {
      const fit = allowed.filter((p) => p <= t);
      let p;
      if (fit.length) { const w = fit.map((q, k) => 1 + k * 1.5); const sum = w.reduce((a, b) => a + b); let x = r() * sum; p = fit[fit.length - 1]; for (let k = 0; k < fit.length; k++) { x -= w[k]; if (x <= 0) { p = fit[k]; break; } } }
      else p = POWERS.find((q) => q >= t) || 64;
      deck.push([c, p]); t -= p;
    }
  });
  for (let i = deck.length - 1; i > 0; i--) { const j = Math.floor(r() * (i + 1)); [deck[i], deck[j]] = [deck[j], deck[i]]; }
  return deck;
}

// write models
const modelIndex = [];
for (const m of Object.values(models)) {
  const g = m.grid;
  fs.writeFileSync(path.join(OUT, 'Models', m.id + '.json'), JSON.stringify({ id: m.id, name: m.name, category: m.category, size: [g.X, g.Y, g.Z], palette: m.palette, voxels: encode(g), anim: m.anim }));
  modelIndex.push({ id: m.id, name: m.name, category: m.category, voxels: g.count() });
}

const levels = []; let n = 0; const report = [];
for (const ch of CHAPTERS) {
  ch.list.forEach((spec, k) => {
    n++;
    const d = (n - 1) / 99; // 0..1 difficulty curve
    let accepted = null;
    for (let attempt = 0; attempt < 400 && !accepted; attempt++) {
      const seed = 1000 + n * 7919 + attempt * 104729;
      const r = rng(seed);
      const lv = makeVariant(spec);
      let core = 0;
      if (lv.hasCore) core = applyHiddenCore(lv, 0.35 + 0.35 * d, r);
      const counts = new Array(lv.palette.length).fill(0); for (const c of lv.v) if (c >= 0) counts[c]++;
      // drop unused colours
      const keep = counts.map((c, i) => (c > 0 ? i : -1)).filter((i) => i >= 0);
      const rem = new Map(keep.map((o, i) => [o, i]));
      const palette = keep.map((i) => lv.palette[i]);
      const v = lv.v.map((c) => (c < 0 ? -1 : rem.get(c)));
      const cnt = keep.map((i) => counts[i]);
      const slackBase = 0.6 - 0.3 * d;
      const slack = Math.max(0.15, slackBase + ((attempt % 3) - 1) * 0.06);
      const deck = buildDeck(cnt, d, slack, r);
      const voxels = Array.from(v, (c) => (c < 0 ? '.' : c.toString(36))).join('');
      const level = { id: n, chapter: ch.id, indexInChapter: k + 1, name: lv.name, model: lv.modelId, variant: lv.variant, hiddenCore: core, difficulty: +d.toFixed(3), seed, size: lv.size, palette, voxels, voxelCount: cnt.reduce((a, b) => a + b), colorCounts: cnt, queueSize: 5, deck };
      const sol = solveGreedy(level);
      if (!sol.won) continue;
      level.witness = sol.taps; level.witnessMoves = sol.moves; level.witnessScore = sol.score; level.witnessMaxCombo = sol.maxCombo;
      // Move limit calibration: binary-search the margin over the witness so that a casual
      // player model wins about `target` of the time. Every 5th level is a HARD level.
      const hard = n % 5 === 0;
      const target = Math.max(0.15, (n <= 2 ? 0.95 : 0.9 - 0.45 * d) - (hard ? 0.3 : 0));
      let lo = 0, hi = 1.5, rate = 1;
      for (let it = 0; it < 9; it++) {
        const mid = (lo + hi) / 2; level.moveLimit = Math.ceil(sol.moves * (1 + mid));
        const r2 = randomWinRate(level, 32, seed);
        if (r2 > target) hi = mid; else lo = mid;
      }
      level.moveLimit = Math.min(deck.length, Math.max(sol.moves, Math.ceil(sol.moves * (1 + hi))));
      rate = randomWinRate(level, 64, seed + 1);
      if (Math.abs(rate - target) > 0.2 && attempt < 30) continue;
      level.hard = hard;
      level.casualWinRate = +rate.toFixed(2);
      const t3 = sol.moves, t2 = sol.moves + Math.ceil((level.moveLimit - sol.moves) / 2);
      level.stars = [t3, t2];
      level.moveTarget = level.moveLimit;
      level.rewardCoins = Math.round((40 + d * 80 + (core ? 20 : 0)) * (hard ? 2.5 : 1));
      level.boosters = { hammer: n >= 3, shuffle: n >= 5, colorBomb: n >= 8 };
      level.solverAttempts = attempt + 1;
      accepted = level;
    }
    if (!accepted) throw new Error('could not build solvable level ' + n + ' ' + spec);
    levels.push(accepted);
    fs.writeFileSync(path.join(OUT, 'Levels', `level_${String(n).padStart(3, '0')}.json`), JSON.stringify(accepted));
    report.push(`${String(n).padStart(3)} ch${ch.id} ${accepted.name.padEnd(20)} vox=${String(accepted.voxelCount).padStart(4)} colors=${accepted.palette.length} deck=${String(accepted.deck.length).padStart(3)} witness=${String(accepted.witnessMoves).padStart(3)} limit=${accepted.moveLimit} ${accepted.hard?'HARD':'    '} reward=${accepted.rewardCoins} stars=${accepted.stars} casual=${accepted.casualWinRate} core=${accepted.hiddenCore} tries=${accepted.solverAttempts}`);
  });
}

// uniqueness check: no two levels may share an identical (voxels, deck) configuration
const sigs = new Set(levels.map((l) => l.voxels + '|' + JSON.stringify(l.deck)));
if (sigs.size !== levels.length) throw new Error('duplicate level configuration detected');

const config = {
  version: 1,
  chapters: CHAPTERS.map((c) => ({ id: c.id, name: c.name, firstLevel: (c.id - 1) * 20 + 1, lastLevel: c.id * 20, unlockStars: [0, 30, 70, 110, 150][c.id - 1], thumbnailModel: c.list[0].split(/[:#]/)[0] })),
  collection: modelIndex.map((m) => ({ ...m, unlockLevel: levels.find((l) => l.model === m.id)?.id ?? null })),
  hardLevels: { every: 5, rewardMultiplier: 2.5, note: 'every 5th level is HARD: tighter move limit, bigger reward' },
  economy: { startCoins: 500, startLives: 5, maxLives: 5, lifeRegenMinutes: 30, dailyRewards: [100, 150, 200, 250, 300, 400, 600], comboCoinBonus: 5 },
  boosters: {
    hammer: { price: 300, startCount: 3, voxels: 12 },
    shuffle: { price: 200, startCount: 3 },
    colorBomb: { price: 500, startCount: 1, voxels: 48 },
    // 'Eliminare': removes one queued (stuck) attack, freeing a slot. Bought with coins earned (HARD levels pay x2.5).
    eliminate: { price: 150, startCount: 2 },
  },
  stars: { note: 'stars[0] = max moves for 3 stars, stars[1] = max moves for 2 stars, otherwise 1 star' },
};
fs.writeFileSync(path.join(OUT, 'Config', 'game_config.json'), JSON.stringify(config, null, 2));
fs.writeFileSync(path.join(ROOT, 'Docs', 'LevelReport.txt'), report.join('\n') + '\n');
console.log(report.join('\n'));
console.log(`\n${levels.length} levels, ${Object.keys(models).length} models written to ${path.relative(ROOT, OUT)}`);
