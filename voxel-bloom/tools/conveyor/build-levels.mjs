// Builds the conveyor levels: Assets/VoxelBloom/Data/Resources/VoxelBloom/Conveyor/level_NNN.json
// Each level is generated from hand-set parameters (SPECS), then validated: supply == demand per colour, a rollout
// solver must find a winning witness, and a casual-player simulation measures difficulty (kept near the target).
// Usage: node tools/conveyor/build-levels.mjs   (VB_ONLY=5 builds one level)
import fs from 'fs';
import { rng } from '../voxel.mjs';
import { Session, solve, replay, casualWinRate, P } from './rules.mjs';

const OUT = 'Assets/VoxelBloom/Data/Resources/VoxelBloom/Conveyor';
export const BOARD = { w: 16, h: 18, r: 2 }; // loop centreline in cube pitches
export const COLORS = ['#3ED6F0', '#FFD93B', '#FF9C7A', '#D9259C', '#4CD964', '#A47CF7', '#B9A21E', '#A0603E'];
export const COLOR_NAMES = ['cyan', 'galben', 'piersică', 'magenta', 'verde', 'mov', 'oliv', 'maro'];

const { w, h, r } = BOARD;
const LG = 2 * (w - 2 * r) + 2 * (h - 2 * r) + 2 * Math.PI * r;
export const SLOTS = Math.round(LG);
const toS = (g) => Math.round(g * SLOTS * P / LG) % (SLOTS * P);
function sideS(side, c) {
  const a = w - 2 * r, b = h - 2 * r, q = Math.PI * r / 2;
  if (side === 'top') return toS(c - r);
  if (side === 'right') return toS(a + q + (c - r));
  if (side === 'bottom') return toS(a + q + b + q + (w - r - c));
  return toS(2 * a + b + 3 * q + (h - r - c)); // left, going up
}
// station pool in fill-priority order, laid out like the reference: left column (horizontal trays), top row and
// bottom row (vertical trays), one horizontal tray in the middle of the right side. Compartments are 1.7 cube pitches,
// trays are 1.84 wide, so neighbouring trays never overlap (see ConveyorBoard.Compartment).
export const TRAY = { first: 1.6, pitch: 1.7, cell: 1.5, width: 1.84 };
const POOL = [['left', 4.6], ['left', 6.8], ['left', 9.0], ['left', 11.2], ['top', 9.0], ['top', 11.2], ['top', 13.4],
  ['bottom', 9.0], ['bottom', 11.2], ['bottom', 13.4], ['right', 9.0], ['left', 2.4], ['left', 13.4], ['left', 15.6]];
const FEEDER = toS(LG - Math.PI * r / 4); // middle of the top-left corner

// hand-set parameters: colours, trays, tray sizes, stations, visible queue, initial belt fill, clump size, locks, target win rate
const SPECS = [
  { k: 2, n: 3, size: [3, 3], st: 3, vis: 3, fill: 1, clump: 3, target: 1, tutorial: true },
  { k: 3, n: 5, size: [3, 3], st: 3, vis: 4, fill: 1, clump: 3, target: 0.98 },
  { k: 3, n: 7, size: [3, 4], st: 3, vis: 5, fill: 1, clump: 4, target: 0.95 },
  { k: 4, n: 10, size: [4, 4], st: 4, vis: 5, fill: 1, clump: 5, target: 0.92 },
  { k: 4, n: 14, size: [3, 4], st: 5, vis: 6, fill: 1, clump: 6, target: 0.88, locks: [[4, 'completed', 2]] },
  { k: 4, n: 18, size: [4, 4], st: 4, vis: 6, fill: 1, clump: 8, target: 0.85 },
  { k: 5, n: 18, size: [4, 4], st: 5, vis: 6, fill: 1, clump: 8, target: 0.8, locks: [[4, 'booster']] },
  { k: 5, n: 20, size: [4, 4], st: 4, vis: 6, fill: 1, clump: 9, target: 0.76 },
  { k: 5, n: 22, size: [3, 4], st: 5, vis: 6, fill: 1, clump: 10, target: 0.72, locks: [[4, 'completed', 3]] },
  { k: 6, n: 24, size: [4, 4], st: 5, vis: 6, fill: 1, clump: 10, target: 0.68, locks: [[4, 'booster']] },
  { k: 6, n: 25, size: [4, 4], st: 4, vis: 6, fill: 1, clump: 10, target: 0.65 },
  { k: 6, n: 26, size: [3, 4], st: 5, vis: 6, fill: 1, clump: 11, target: 0.62, locks: [[4, 'completed', 4]] },
  { k: 6, n: 27, size: [4, 4], st: 5, vis: 5, fill: 1, clump: 11, target: 0.58, locks: [[4, 'booster']] },
  { k: 7, n: 28, size: [4, 4], st: 5, vis: 6, fill: 1, clump: 12, target: 0.55 },
  { k: 7, n: 29, size: [3, 4], st: 6, vis: 6, fill: 1, clump: 12, target: 0.52, locks: [[4, 'completed', 5], [5, 'booster']] },
  { k: 7, n: 30, size: [4, 4], st: 4, vis: 5, fill: 1, clump: 12, target: 0.5 },
  { k: 7, n: 30, size: [4, 4], st: 6, vis: 6, fill: 1, clump: 13, target: 0.48, locks: [[5, 'booster']] },
  { k: 8, n: 31, size: [4, 4], st: 5, vis: 6, fill: 1, clump: 13, target: 0.46 },
  { k: 8, n: 32, size: [4, 4], st: 6, vis: 6, fill: 1, clump: 14, target: 0.44, locks: [[4, 'completed', 6], [5, 'booster']] },
  { k: 8, n: 32, size: [4, 4], st: 5, vis: 5, fill: 1, clump: 14, target: 0.42, locks: [[4, 'booster']] },
];

export function generateFor(id, spec, seed) { return generate(id, spec, seed); }
function generate(id, spec, seed) {
  const R = rng(seed);
  const pick = (n) => Math.floor(R() * n);
  // trays
  const queue = [];
  for (let t = 0; t < spec.n; t++) {
    const len = spec.size[0] + pick(spec.size[1] - spec.size[0] + 1);
    const mono = R() < 0.25;
    const base = pick(spec.k);
    const tray = [];
    for (let i = 0; i < len; i++) tray.push(mono ? base : pick(spec.k));
    queue.push(tray);
  }
  // every colour must appear
  for (let c = 0; c < spec.k; c++) if (!queue.some((t) => t.includes(c))) queue[pick(queue.length)][0] = c;
  // cube order: clumps of one colour
  const left = new Array(spec.k).fill(0); for (const t of queue) for (const c of t) left[c]++;
  const order = [];
  let remaining = left.reduce((a, b) => a + b, 0);
  while (remaining > 0) {
    let c; do c = pick(spec.k); while (left[c] === 0);
    const len = Math.min(left[c], 1 + pick(spec.clump));
    for (let i = 0; i < len; i++) order.push(c);
    left[c] -= len; remaining -= len;
  }
  // initial train on the belt just ahead of the feeder, rest in the hopper
  const fill = Math.min(order.length, Math.round(SLOTS * Math.min(1, spec.fill)), SLOTS - 4);
  const belt = new Array(SLOTS).fill(-1);
  const k0 = Math.ceil(FEEDER / P) % SLOTS;
  for (let i = 0; i < fill; i++) belt[(k0 + fill - 1 - i) % SLOTS] = order[i];
  const stations = POOL.slice(0, spec.st).map(([side, c], i) => {
    const lock = (spec.locks || []).find((l) => l[0] === i);
    return { side, at: c, s: sideS(side, c), capacity: 4, lock: lock ? (lock[1] === 'booster' ? { type: 'booster' } : { type: 'completed', n: lock[2] }) : null };
  });
  return {
    id, rules: 3, name: 'Nivel ' + id, tutorial: !!spec.tutorial, seed,
    board: BOARD, slots: SLOTS, speed: 6, feeder: FEEDER, visible: spec.vis,
    colors: COLORS.slice(0, spec.k), stations, queue, belt, hopper: order.slice(fill),
  };
}

export function validate(l) {
  const supply = new Array(l.colors.length).fill(0), demand = supply.slice();
  for (const c of l.belt) if (c >= 0) supply[c]++;
  for (const c of l.hopper) supply[c]++;
  for (const t of l.queue) { if (!t.length) throw new Error('empty tray'); if (t.length > 4) throw new Error('tray too long'); for (const c of t) demand[c]++; }
  for (let c = 0; c < supply.length; c++) if (supply[c] !== demand[c]) throw new Error(`colour ${c}: supply ${supply[c]} != demand ${demand[c]}`);
  if (l.belt.length !== l.slots) throw new Error('belt size');
  const free = l.stations.filter((s) => !s.lock).length;
  if (free < 1) throw new Error('no free station');
  for (let i = 0; i < l.stations.length; i++) for (let j = i + 1; j < l.stations.length; j++) if (Math.abs(l.stations[i].s - l.stations[j].s) < l.speed * 2) throw new Error('stations too close');
}

function build(id) {
  const spec = SPECS[id - 1];
  let best = null;
  for (let attempt = 0; attempt < 24; attempt++) {
    const lvl = generate(id, spec, 1000 + id * 7919 + attempt * 104729);
    validate(lvl);
    const sol = solve(lvl);
    if (!sol) continue;
    const check = replay(lvl, sol.witness);
    if (!check.isWon()) throw new Error('witness replay failed for level ' + id);
    const rate = casualWinRate(lvl, 16, rng(lvl.seed ^ 0x5bd1));
    const cand = { lvl, sol, rate, err: Math.abs(rate - spec.target) };
    if (!best || cand.err < best.err) best = cand;
    if (best.err <= 0.08) break;
  }
  if (!best) throw new Error('no solvable candidate for level ' + id);
  const { lvl, sol, rate } = best;
  lvl.witness = sol.witness; lvl.witnessTicks = sol.ticks; lvl.casualWinRate = +rate.toFixed(2);
  lvl.rewardCoins = 30 + id * 5;
  return lvl;
}

if (process.argv[1] && process.argv[1].endsWith('build-levels.mjs')) {
  fs.mkdirSync(OUT, { recursive: true });
  const only = process.env.VB_ONLY ? +process.env.VB_ONLY : 0;
  const report = [];
  for (let id = 1; id <= SPECS.length; id++) {
    if (only && id !== only) continue;
    const t0 = Date.now();
    const l = build(id);
    fs.writeFileSync(`${OUT}/level_${String(id).padStart(3, '0')}.json`, JSON.stringify(l));
    const cubes = l.queue.reduce((a, t) => a + t.length, 0);
    const line = `L${String(id).padStart(2)}  colours ${l.colors.length}  trays ${l.queue.length}  cubes ${cubes}  belt ${l.belt.filter((c) => c >= 0).length}/${l.slots}  hopper ${l.hopper.length}  stations ${l.stations.length}  witness ${(l.witnessTicks / 60).toFixed(0)}s  casual ${l.casualWinRate}  (${Date.now() - t0} ms)`;
    console.log(line); report.push(line);
  }
  if (!only) fs.writeFileSync('Docs/ConveyorLevelReport.txt', report.join('\n') + '\n');
}
