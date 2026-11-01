// Builds the conveyor levels: Assets/VoxelBloom/Data/Resources/VoxelBloom/Conveyor/level_NNN.json
// Levels differ in kind, not only in size: three board sizes (small / medium / large loop), four difficulty tiers
// (Ușor, Normal, Greu, Foarte greu) in a saw-tooth curve, tray sizes 2-4, belt speed, visible queue, colour clumping,
// numbered locks and green plus slots. Every level is validated: supply == demand per colour, a rollout solver must find
// a winning witness (replayed), and a casual-player simulation measures the difficulty (kept near the tier's target).
// Usage: node tools/conveyor/build-levels.mjs   (VB_ONLY=5 builds one level, VB_FROM=21 builds from a level on)
import fs from 'fs';
import { rng } from '../voxel.mjs';
import { solve, replay, casualWinRate, P } from './rules.mjs';

const OUT = 'Assets/VoxelBloom/Data/Resources/VoxelBloom/Conveyor';
export const COLORS = ['#3ED6F0', '#FFD93B', '#FF9C7A', '#D9259C', '#4CD964', '#A47CF7', '#B9A21E', '#A0603E'];
export const TIERS = ['Ușor', 'Normal', 'Greu', 'Foarte greu'];
// loop centreline in cube pitches; cap = compartments per station tray
export const BOARDS = {
  S: { w: 12, h: 14, r: 2, cap: 3, name: 'mică' },
  M: { w: 16, h: 18, r: 2, cap: 4, name: 'medie' },
  L: { w: 18, h: 22, r: 2, cap: 4, name: 'mare' },
};
export const TRAY = { first: 1.6, pitch: 1.7, cell: 1.5, width: 1.84 }; // mirrors ConveyorBoard.cs

function geometry(B) {
  const { w, h, r } = B, a = w - 2 * r, b = h - 2 * r, q = Math.PI * r / 2;
  const LG = 2 * a + 2 * b + 4 * q, SLOTS = Math.round(LG);
  const toS = (g) => Math.round(g * SLOTS * P / LG) % (SLOTS * P);
  const sideS = (side, c) => side === 'top' ? toS(c - r) : side === 'right' ? toS(a + q + (c - r))
    : side === 'bottom' ? toS(a + q + b + q + (w - r - c)) : toS(2 * a + b + 3 * q + (h - r - c));
  return { SLOTS, sideS, feeder: toS(LG - q / 2) };
}

/** Tray rectangle [x0, y0, x1, y1] in board units for a station on a side at coordinate c. */
function trayRect(B, side, c) {
  const len = TRAY.first - TRAY.cell / 2 - 0.12 + B.cap * TRAY.pitch + 0.24, half = TRAY.width / 2, near = 0.7;
  if (side === 'left') return [near, c - half, near + len, c + half];
  if (side === 'right') return [B.w - near - len, c - half, B.w - near, c + half];
  if (side === 'top') return [c - half, near, c + half, near + len];
  return [c - half, B.h - near - len, c + half, B.h - near];
}
const overlap = (p, q, m = 0.25) => p[0] < q[2] + m && q[0] < p[2] + m && p[1] < q[3] + m && q[1] < p[3] + m;

/** Station pool for a board in fill-priority order (sides alternate), no overlapping trays. */
export function stationPool(B) {
  const picked = [];
  const tryAdd = (side, c) => {
    const rect = trayRect(B, side, c);
    if (picked.some((p) => overlap(p.rect, rect))) return;
    picked.push({ side, at: +c.toFixed(2), rect });
  };
  for (let y = 4.4; y <= B.h - 2.3; y += 2.2) tryAdd('left', y);
  for (let x = B.w - 2.6; x >= 2.6; x -= 2.2) tryAdd('top', x);
  for (let x = B.w - 2.6; x >= 2.6; x -= 2.2) tryAdd('bottom', x);
  for (let y = B.h / 2; y <= B.h - 2.3; y += 2.2) tryAdd('right', y);
  for (let y = B.h / 2 - 2.2; y >= 2.3; y -= 2.2) tryAdd('right', y);
  // fill priority alternates the sides (left, top, bottom, right, ...) so a few stations spread around the board
  const bySide = { left: [], top: [], bottom: [], right: [] };
  for (const p of picked) bySide[p.side].push(p);
  bySide.left.sort((u, v) => Math.abs(u.at - B.h / 2) - Math.abs(v.at - B.h / 2)); // middle of the left column first
  const order = [], sides = ['left', 'top', 'bottom', 'left', 'right'];
  while (order.length < picked.length) for (const sd of sides) if (bySide[sd].length) order.push(bySide[sd].shift());
  return order;
}

/** Hand-set progression: tier saw-tooth and per-tier parameters. */
export function spec(id) {
  const p = (id - 1) / 49;
  const tier = id <= 3 ? 0 : id % 10 === 0 ? 3 : id % 5 === 0 ? 2 : id % 5 === 2 ? 0 : 1;
  const board = id <= 3 ? 'S' : tier === 0 ? (p < 0.45 ? 'S' : id % 2 ? 'S' : 'M') : tier === 1 ? (id % 3 === 0 && id > 12 ? 'L' : id % 4 === 1 && id < 30 ? 'S' : 'M') : tier === 2 ? (id > 20 ? 'L' : 'M') : 'L';
  const B = BOARDS[board];
  const kMax = board === 'S' ? 5 : 8;
  const k = Math.min(kMax, 2 + Math.round(p * 5) + (tier >= 2 ? 1 : 0) + (id <= 1 ? 0 : 1));
  const size = B.cap === 3 ? (tier === 0 ? [2, 3] : [3, 3]) : tier === 0 ? [2, 3] : tier === 1 ? [3, 4] : [4, 4];
  // complexity comes from the jam risk, which needs more cubes than the belt holds:
  // Ușor below belt capacity, Normal ~1.4x, Greu ~1.65x, Foarte greu ~1.95x (+ a ramp over the 50 levels)
  const slots = { S: 49, M: 65, L: 77 }[board];
  const avg = (size[0] + size[1]) / 2;
  const ease = id < 12 ? 0.75 + (id - 4) * 0.03 : 1; // gentle start before the full jam pressure
  const cubes = slots * ([0.55, 1.4, 1.65, 1.95][tier] + 0.25 * p) * (tier === 0 ? 1 : ease);
  const n = id <= 3 ? 2 + id : Math.max(3, Math.round(cubes / avg));
  let st = board === 'S' ? 3 + (tier >= 1 ? 1 : 0) : board === 'M' ? 4 + (tier === 0 ? 1 : 0) : 5 + (tier === 0 ? 2 : tier === 1 ? 1 : 0);
  const locks = [];
  if (id > 6 && tier >= 1 && id % 3 === 0) locks.push([st - 1, 'completed', 2 + tier]);
  if (id > 8 && id % 4 === 0) { locks.push([st, 'booster']); st++; }
  return {
    id, tier, board, k, n, size, st, locks,
    vis: id <= 3 ? Math.min(n, 3 + id - 1) : [6, 5, 5, 4][tier],
    clump: tier === 0 ? 3 + Math.round(p * 4) : 6 + Math.round(p * 8) + tier * 3,
    speed: tier >= 2 ? 7 : 6,
    target: +[0.95 - 0.1 * p, 0.75 - 0.15 * p, 0.5 - 0.15 * p, 0.3 - 0.1 * p][tier].toFixed(2),
    tutorial: id === 1,
  };
}
export const LEVEL_COUNT = 50;

function generate(sp, seed) {
  const B = BOARDS[sp.board], G = geometry(B), pool = stationPool(B);
  if (pool.length < sp.st) throw new Error(`level ${sp.id}: board ${sp.board} has only ${pool.length} stations`);
  const R = rng(seed), pick = (n) => Math.floor(R() * n);
  const queue = [];
  for (let t = 0; t < sp.n; t++) {
    const len = sp.size[0] + pick(sp.size[1] - sp.size[0] + 1);
    const mono = R() < 0.25, base = pick(sp.k), tray = [];
    for (let i = 0; i < len; i++) tray.push(mono ? base : pick(sp.k));
    queue.push(tray);
  }
  for (let c = 0; c < sp.k; c++) if (!queue.some((t) => t.includes(c))) queue[pick(queue.length)][0] = c;
  const left = new Array(sp.k).fill(0); for (const t of queue) for (const c of t) left[c]++;
  const order = []; let remaining = left.reduce((a, b) => a + b, 0);
  while (remaining > 0) {
    let c; do c = pick(sp.k); while (left[c] === 0);
    const len = Math.min(left[c], 1 + pick(sp.clump));
    for (let i = 0; i < len; i++) order.push(c);
    left[c] -= len; remaining -= len;
  }
  const fill = Math.min(order.length, G.SLOTS - 4);
  const belt = new Array(G.SLOTS).fill(-1), k0 = Math.ceil(G.feeder / P) % G.SLOTS;
  for (let i = 0; i < fill; i++) belt[(k0 + fill - 1 - i) % G.SLOTS] = order[i];
  const stations = pool.slice(0, sp.st).map((ps, i) => {
    const lock = sp.locks.find((l) => l[0] === i);
    return { side: ps.side, at: ps.at, s: G.sideS(ps.side, ps.at), capacity: B.cap,
      lock: lock ? (lock[1] === 'booster' ? { type: 'booster' } : { type: 'completed', n: lock[2] }) : null };
  });
  return {
    id: sp.id, rules: 3, name: 'Nivel ' + sp.id, tutorial: sp.tutorial, seed, tier: sp.tier, tierName: TIERS[sp.tier], boardSize: sp.board,
    board: { w: B.w, h: B.h, r: B.r }, slots: G.SLOTS, speed: sp.speed, feeder: G.feeder, visible: sp.vis,
    colors: COLORS.slice(0, sp.k), stations, queue, belt, hopper: order.slice(fill),
  };
}

export function validate(l) {
  const supply = new Array(l.colors.length).fill(0), demand = supply.slice();
  for (const c of l.belt) if (c >= 0) supply[c]++;
  for (const c of l.hopper) supply[c]++;
  const cap = Math.max(...l.stations.map((s) => s.capacity));
  for (const t of l.queue) { if (!t.length) throw new Error('empty tray'); if (t.length > cap) throw new Error('tray longer than the stations'); for (const c of t) demand[c]++; }
  for (let c = 0; c < supply.length; c++) if (supply[c] !== demand[c]) throw new Error(`colour ${c}: supply ${supply[c]} != demand ${demand[c]}`);
  if (l.belt.length !== l.slots) throw new Error('belt size');
  if (!l.stations.some((s) => !s.lock)) throw new Error('no free station');
  for (let i = 0; i < l.stations.length; i++) for (let j = i + 1; j < l.stations.length; j++) {
    const d = Math.abs(l.stations[i].s - l.stations[j].s);
    if (Math.min(d, l.slots * P - d) < l.speed * 2) throw new Error('stations too close');
  }
}

function build(id) {
  const sp = spec(id);
  let best = null;
  for (let attempt = 0; attempt < 24; attempt++) {
    const lvl = generate(sp, 1000 + id * 7919 + attempt * 104729);
    validate(lvl);
    const sol = solve(lvl);
    if (!sol) continue;
    if (!replay(lvl, sol.witness).isWon()) throw new Error('witness replay failed for level ' + id);
    const rate = casualWinRate(lvl, 16, rng(lvl.seed ^ 0x5bd1));
    const cand = { lvl, sol, rate, err: Math.abs(rate - sp.target) };
    if (!best || cand.err < best.err) best = cand;
    if (best.err <= 0.08) break;
  }
  if (!best) throw new Error('no solvable candidate for level ' + id);
  const { lvl, sol, rate } = best;
  lvl.witness = sol.witness; lvl.witnessTicks = sol.ticks; lvl.casualWinRate = +rate.toFixed(2);
  lvl.rewardCoins = Math.round((30 + id * 4) * [1, 1.2, 1.6, 2.2][sp.tier]);
  return lvl;
}

function reportLine(l) {
  const cubes = l.queue.reduce((a, t) => a + t.length, 0), sizes = [...new Set(l.queue.map((t) => t.length))].sort().join('/');
  const locks = l.stations.filter((s) => s.lock).map((s) => s.lock.type === 'booster' ? '+' : '#' + s.lock.n).join(' ') || '-';
  return `L${String(l.id).padStart(2)}  ${(l.tierName || '').padEnd(11)} tablă ${(l.boardSize || 'M')}  culori ${l.colors.length}  tăvi ${String(l.queue.length).padStart(2)} (${sizes})  cuburi ${String(cubes).padStart(3)}  stații ${l.stations.length} [${locks}]  coadă ${l.visible}  viteză ${l.speed}  soluție ${(l.witnessTicks / 60).toFixed(0)}s  casual ${l.casualWinRate}`;
}

if (process.argv[1] && process.argv[1].endsWith('build-levels.mjs')) {
  fs.mkdirSync(OUT, { recursive: true });
  const only = process.env.VB_ONLY ? +process.env.VB_ONLY : 0, from = process.env.VB_FROM ? +process.env.VB_FROM : 1;
  for (let id = 1; id <= LEVEL_COUNT; id++) {
    if ((only && id !== only) || id < from) continue;
    const t0 = Date.now();
    const l = build(id);
    fs.writeFileSync(`${OUT}/level_${String(id).padStart(3, '0')}.json`, JSON.stringify(l));
    console.log(reportLine(l) + `  (${Date.now() - t0} ms)`);
  }
  const lines = [];
  for (let id = 1; fs.existsSync(`${OUT}/level_${String(id).padStart(3, '0')}.json`); id++) lines.push(reportLine(JSON.parse(fs.readFileSync(`${OUT}/level_${String(id).padStart(3, '0')}.json`))));
  fs.writeFileSync('Docs/ConveyorLevelReport.txt', lines.join('\n') + '\n');
}
