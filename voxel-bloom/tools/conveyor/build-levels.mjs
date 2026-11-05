// Builds the conveyor levels: Assets/VoxelBloom/Data/Resources/VoxelBloom/Conveyor/level_NNN.json
// Levels differ in kind, not only in size: three board sizes (small / medium / large loop), four difficulty tiers
// (Ușor, Normal, Greu, Foarte greu) in a saw-tooth curve, tray sizes 2-4, belt speed, visible queue, colour clumping,
// numbered locks and green plus slots. Every level is validated: supply == demand per colour, a rollout solver must find
// a winning witness (replayed), and a casual-player simulation measures the difficulty (kept near the tier's target).
// Usage: node tools/conveyor/build-levels.mjs   (VB_ONLY=5 builds one level, VB_FROM=21 builds from a level on)
import fs from 'fs';
import { rng } from '../voxel.mjs';
import { solve, replay, casualWinRate, P, EMPTY } from './rules.mjs';

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
function trayRect(B, side, c, cap = B.cap) {
  const len = TRAY.first - TRAY.cell / 2 - 0.12 + cap * TRAY.pitch + 0.24, half = TRAY.width / 2, near = 0.7;
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
  if (id === 1) return { id, tier: 0, board: 'M', k: 4, size: [3, 4], st: 14, locks: [], n: 10 + 5, space: 8, scramble: 0.18,
    vis: 7, speed: 6, target: 1, tutorial: true };
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
  const unlocked = st - locks.length;
  const extra = id <= 3 ? id : Math.round((3 + p * 9) * [0.6, 1, 1.25, 1.5][tier]);
  return {
    id, tier, board, k, size, st, locks,
    n: unlocked + extra,
    space: id <= 3 ? 10 : [10, 8, 7, 6][tier],
    scramble: id <= 3 ? 0.25 : [0.3, 0.45, 0.55, 0.65][tier] + 0.1 * p,
    vis: id <= 3 ? Math.min(n, 3 + id - 1) : [6, 5, 5, 4][tier],
    clump: tier === 0 ? 3 + Math.round(p * 4) : 6 + Math.round(p * 8) + tier * 3,
    speed: tier >= 2 ? 7 : 6,
    target: +[0.95 - 0.1 * p, 0.75 - 0.15 * p, 0.5 - 0.15 * p, 0.3 - 0.1 * p][tier].toFixed(2),
    tutorial: id === 1,
  };
}
export const LEVEL_COUNT = 50;

// Level 1: the approved Premium layout (7 BUSes/blocks on the left with locks "1" and "2" and two plus slots,
// 3 vertical BUSes on top, one horizontal on the right, 3 vertical at the bottom, queue of 7 below the belt).
const L1_BOARD = { w: 17, h: 19, r: 2, cap: 4 };
const L1_STATIONS = [
  ['left', 2.6, 4, ['completed', 1]], ['left', 4.8, 4], ['left', 7.0, 4, ['booster']], ['left', 9.2, 4], ['left', 11.4, 4, ['booster']],
  ['left', 13.6, 4], ['left', 15.8, 4, ['completed', 2]], ['top', 9.8, 4], ['top', 11.95, 4], ['top', 14.1, 4],
  ['right', 10.35, 3], ['bottom', 9.8, 3], ['bottom', 11.95, 3], ['bottom', 14.1, 3]];

function generate(sp, seed) {
  const custom = sp.id === 1;
  const B = custom ? L1_BOARD : BOARDS[sp.board], G = geometry(B);
  let stations;
  if (custom) {
    stations = L1_STATIONS.map(([side, at, cap, lk]) => ({ side, at, s: G.sideS(side, at), capacity: cap,
      lock: lk ? (lk[0] === 'booster' ? { type: 'booster' } : { type: 'completed', n: lk[1] }) : null }));
  } else {
    const pool = stationPool(B);
    if (pool.length < sp.st) throw new Error(`level ${sp.id}: board ${sp.board} has only ${pool.length} stations`);
    stations = pool.slice(0, sp.st).map((ps, i) => {
      const lock = sp.locks.find((l) => l[0] === i);
      return { side: ps.side, at: ps.at, s: G.sideS(ps.side, ps.at), capacity: B.cap,
        lock: lock ? (lock[1] === 'booster' ? { type: 'booster' } : { type: 'completed', n: lock[2] }) : null };
    });
  }
  const R = rng(seed), pick = (n) => Math.floor(R() * n);
  const open = stations.map((st, i) => (st.lock ? -1 : i)).filter((i) => i >= 0);
  // BUSes: required colours
  const buses = [];
  for (let t = 0; t < sp.n; t++) {
    const k = t < open.length ? open[t] : -1, cap = k >= 0 ? stations[k].capacity : Math.min(...open.map((i) => stations[i].capacity));
    const len = Math.min(cap, sp.size[0] + pick(sp.size[1] - sp.size[0] + 1));
    const mono = R() < 0.3, base = pick(sp.k), need = [];
    for (let i = 0; i < len; i++) need.push(mono ? base : pick(sp.k));
    buses.push({ need, have: need.slice(), station: k });
  }
  for (let c = 0; c < sp.k; c++) if (!buses.some((b) => b.need.includes(c))) { const b = buses[pick(buses.length)]; b.need[0] = c; b.have[0] = c; }
  // cargo: scramble a share of all compartments among themselves
  const cells = []; buses.forEach((b, bi) => b.need.forEach((_, i) => cells.push([bi, i])));
  for (let i = cells.length - 1; i > 0; i--) { const j = pick(i + 1); [cells[i], cells[j]] = [cells[j], cells[i]]; }
  const m = Math.max(2, Math.round(sp.scramble * cells.length)), sc = cells.slice(0, m);
  const cols = sc.map(([bi, i]) => buses[bi].have[i]);
  for (let i = cols.length - 1; i > 0; i--) { const j = pick(i + 1); [cols[i], cols[j]] = [cols[j], cols[i]]; }
  sc.forEach(([bi, i], j) => { buses[bi].have[i] = cols[j]; });
  // a few cubes already circulate (they leave empty compartments behind)
  const belt = new Array(G.SLOTS).fill(-1), b0 = Math.min(sp.space - 3, 1 + pick(3));
  for (let j = 0; j < b0; j++) {
    const [bi, i] = sc[(j * 7) % sc.length]; if (buses[bi].have[i] < 0) continue;
    let k; do k = pick(G.SLOTS); while (belt[k] >= 0 || belt[(k + 1) % G.SLOTS] >= 0 || belt[(k + G.SLOTS - 1) % G.SLOTS] >= 0);
    belt[k] = buses[bi].have[i]; buses[bi].have[i] = EMPTY;
  }
  // no BUS may start complete: swap a compartment with another BUS
  for (let guard = 0; guard < 200; guard++) {
    const done = buses.findIndex((b) => b.need.every((c, i) => b.have[i] === c));
    if (done < 0) break;
    const b = buses[done], i = pick(b.need.length), o = buses[pick(buses.length)], j = pick(o.need.length);
    if (o === b || o.have[j] < 0 || o.have[j] === b.have[i]) continue;
    [b.have[i], o.have[j]] = [o.have[j], b.have[i]];
  }
  const start = stations.map(() => null), queue = [];
  for (const b of buses) { if (b.station >= 0) start[b.station] = { need: b.need, have: b.have }; else queue.push({ need: b.need, have: b.have }); }
  return {
    id: sp.id, rules: 4, name: 'Nivel ' + sp.id, tutorial: sp.tutorial, seed, tier: sp.tier, tierName: TIERS[sp.tier], boardSize: custom ? 'M' : sp.board,
    board: { w: B.w, h: B.h, r: B.r }, slots: G.SLOTS, speed: sp.speed, feeder: G.feeder, visible: custom ? 7 : sp.vis, space: sp.space,
    colors: COLORS.slice(0, sp.k), stations, start, queue, belt, hopper: [],
  };
}

export function validate(l) {
  const supply = new Array(l.colors.length).fill(0), demand = supply.slice();
  for (const c of l.belt) if (c >= 0) supply[c]++;
  for (const c of l.hopper || []) supply[c]++;
  const cap = Math.max(...l.stations.map((s) => s.capacity));
  const all = [...(l.start || []).filter(Boolean), ...l.queue];
  for (const b of all) {
    if (!b.need.length) throw new Error('empty BUS');
    if (b.need.length > cap) throw new Error('BUS longer than every station');
    if (b.have.length !== b.need.length) throw new Error('BUS have/need size');
    b.need.forEach((c, i) => { demand[c]++; if (b.have[i] >= 0) supply[b.have[i]]++; });
    if (b.need.every((c, i) => b.have[i] === c)) throw new Error('BUS starts complete');
  }
  (l.start || []).forEach((b, k) => { if (b && (l.stations[k].lock || b.need.length > l.stations[k].capacity)) throw new Error('start BUS on a locked/small station'); });
  for (let c = 0; c < supply.length; c++) if (supply[c] !== demand[c]) throw new Error(`colour ${c}: supply ${supply[c]} != demand ${demand[c]}`);
  if (l.belt.length !== l.slots) throw new Error('belt size');
  if (l.belt.filter((c) => c >= 0).length >= l.space) throw new Error('belt starts full');
  if (!l.stations.some((s) => !s.lock)) throw new Error('no free station');
  const B = { w: l.board.w, h: l.board.h };
  for (let i = 0; i < l.stations.length; i++) for (let j = i + 1; j < l.stations.length; j++) {
    const a = l.stations[i], b = l.stations[j], d = Math.abs(a.s - b.s);
    if (Math.min(d, l.slots * P - d) < l.speed * 2) throw new Error('stations too close');
    if (overlap(trayRect(B, a.side, a.at, a.capacity), trayRect(B, b.side, b.at, b.capacity), 0.2)) throw new Error(`trays ${i} and ${j} overlap`);
  }
}

function build(id) {
  const sp = spec(id);
  let best = null;
  for (let attempt = 0; attempt < 48; attempt++) {
    if (!best && attempt > 0 && attempt % 16 === 0) sp.space++; // relax: one more space on the belt
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
  const all = [...(l.start || []).filter(Boolean), ...l.queue];
  const cubes = all.reduce((a, b) => a + b.need.length, 0), wrong = all.reduce((a, b) => a + b.need.filter((c, i) => b.have[i] !== c).length, 0);
  const locks = l.stations.filter((s) => s.lock).map((s) => s.lock.type === 'booster' ? '+' : '#' + s.lock.n).join(' ') || '-';
  return `L${String(l.id).padStart(2)}  ${(l.tierName || '').padEnd(11)} culori ${l.colors.length}  BUS ${String(all.length).padStart(2)} (coadă ${l.queue.length})  cuburi ${String(cubes).padStart(3)} greșite ${String(wrong).padStart(2)}  spațiu ${l.space}  stații ${l.stations.length} [${locks}]  soluție ${(l.witnessTicks / 60).toFixed(0)}s  casual ${l.casualWinRate}`;
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
  for (let id = 1; fs.existsSync(`${OUT}/level_${String(id).padStart(3, '0')}.json`); id++) { const l = JSON.parse(fs.readFileSync(`${OUT}/level_${String(id).padStart(3, '0')}.json`)); if (l.rules === 4) lines.push(reportLine(l)); }
  fs.writeFileSync('Docs/ConveyorLevelReport.txt', lines.join('\n') + '\n');
}
