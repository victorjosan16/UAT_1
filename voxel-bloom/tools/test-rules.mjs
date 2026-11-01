// Runnable rules tests (node tools/test-rules.mjs). Mirrors the C# EditMode tests.
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
import { Session, solveGreedy } from './rules.mjs';

const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const LV = path.join(ROOT, 'Assets/VoxelBloom/Data/Resources/VoxelBloom/Levels');
let pass = 0, fail = 0;
function test(name, fn) { try { fn(); pass++; console.log('  ok  ', name); } catch (e) { fail++; console.log('  FAIL', name, '-', e.message); } }
function eq(a, b, m = '') { if (a !== b) throw new Error(`${m} expected ${b} got ${a}`); }
function ok(c, m) { if (!c) throw new Error(m || 'assertion failed'); }

// 3x3x3 cube: colour 0 outside, colour 1 in the centre
function cube(deck, opts = {}) {
  let v = ''; for (let z = 0; z < 3; z++) for (let y = 0; y < 3; y++) for (let x = 0; x < 3; x++) v += x === 1 && y === 1 && z === 1 ? '1' : '0';
  return { size: [3, 3, 3], palette: ['#ff0000', '#00ff00'], voxels: v, deck, queueSize: opts.queueSize ?? 5, moveLimit: opts.moveLimit };
}
const idx = (x, y, z) => x + 3 * (y + 3 * z);

test('exposure: centre hidden, surface exposed, recomputed after destruction', () => {
  const s = new Session(cube([[0, 2]]));
  eq(s.exposed[idx(1, 1, 1)], 0); eq(s.exposed[idx(0, 0, 0)], 1); eq(s.exposed[idx(1, 1, 0)], 1);
  eq(s.exposedCount(1), 0); eq(s.exposedCount(0), 26);
  s.destroy(idx(1, 1, 0));
  eq(s.exposed[idx(1, 1, 1)], 1); eq(s.exposedCount(1), 1);
});
test('attack destroys up to power, matching colour only', () => {
  const s = new Session(cube([[0, 4], [1, 2]]));
  s.tap(0); const r = s.resolveNext();
  eq(r.destroyed.length, 4); ok(r.destroyed.every((i) => i !== idx(1, 1, 1)), 'never other colours'); eq(s.remaining[0], 22); eq(s.remaining[1], 1);
});
test('fewer eligible voxels: remove only those and consume the attack', () => {
  const s = new Session(cube([[1, 8], [0, 64]]));
  s.tap(1); s.resolveAll(); eq(s.remaining[0], 0, 'one 64 attack clears all 26 exposed');
  s.tap(0); const r = s.resolveAll(); eq(r.length, 1); eq(r[0].destroyed.length, 1); eq(s.queue.length, 0); eq(s.combo, 0); ok(s.isWon());
});
test('attack waits in queue while its colour is hidden', () => {
  const s = new Session(cube([[1, 2], [0, 32]]));
  s.tap(0); eq(s.resolveAll().length, 0); eq(s.queue.length, 1);
  s.tap(1); const rs = s.resolveAll(); eq(rs.length, 2); ok(s.isWon());
});
test('combo increases on full-power attacks and multiplies score (cap x5)', () => {
  const deck = Array.from({ length: 7 }, () => [0, 2]);
  const s = new Session(cube(deck));
  let last = 0;
  for (let k = 0; k < 7; k++) { s.tap(k); s.resolveAll(); const gained = s.score - last; last = s.score; eq(gained, 2 * 10 * Math.min(Math.max(s.combo, 1), 5), 'score step ' + k); }
  eq(s.combo, 7); eq(s.maxCombo, 7);
});
test('queue full + stuck = queue_full failure', () => {
  const deck = [[1, 2], [1, 2], [1, 2], [1, 2], [1, 2], [0, 32]];
  const s = new Session(cube(deck, { queueSize: 5 }));
  for (let k = 0; k < 5; k++) { s.tap(k); s.resolveAll(); }
  eq(s.failReason(), 'queue_full'); ok(!s.canTap(5));
});
test('move limit = out_of_moves failure', () => {
  const s = new Session(cube([[0, 2], [0, 2], [0, 32], [1, 2]], { moveLimit: 2 }));
  s.tap(0); s.resolveAll(); s.tap(1); s.resolveAll();
  eq(s.failReason(), 'out_of_moves'); ok(!s.canTap(2));
});
test('board refills from deck; cleared colour blocks are replaced', () => {
  const deck = [[0, 32], [0, 2], [1, 2], ...Array.from({ length: 17 }, () => [1, 2]), [1, 4], [0, 2], [1, 8]];
  const s = new Session(cube(deck));
  eq(s.deckIndex, 20);
  s.tap(0); eq(s.board[0].c, 1); eq(s.board[0].p, 4, 'refilled from deck');
  s.resolveAll();
  eq(s.remaining[0], 0); ok(s.board.every((b) => !b || b.c === 1), 'colour 0 purged from board');
});
test('attacks for a finished colour are discarded as wasted (combo reset)', () => {
  const s = new Session(cube([[0, 32], [0, 2], [1, 2]]));
  s.tap(0); s.tap(1);
  const rs = s.resolveAll();
  eq(rs.length, 2); eq(rs[0].destroyed.length, 26); eq(rs[0].colorCleared, true); eq(rs[1].wasted, true); eq(s.combo, 0); eq(s.queue.length, 0);
});
test('all 100 levels: witness replays to a win with identical moves and score', () => {
  const files = fs.readdirSync(LV).filter((f) => f.endsWith('.json')).sort();
  eq(files.length, 100, 'level count');
  for (const f of files) {
    const l = JSON.parse(fs.readFileSync(path.join(LV, f)));
    const s = new Session(l);
    for (const slot of l.witness) { ok(s.tap(slot), `${f}: tap ${slot} invalid`); s.resolveAll(); }
    ok(s.isWon(), f + ' witness does not win'); eq(s.moves, l.witnessMoves, f + ' moves'); eq(s.score, l.witnessScore, f + ' score');
    ok(l.witnessMoves <= l.moveLimit, f + ' witness exceeds move limit');
    eq(l.hard, l.id % 5 === 0, f + ' hard flag');
  }
});
test('levels are distinct and difficulty increases (casual win rate trend)', () => {
  const ls = fs.readdirSync(LV).filter((f) => f.endsWith('.json')).sort().map((f) => JSON.parse(fs.readFileSync(path.join(LV, f))));
  eq(new Set(ls.map((l) => l.voxels + JSON.stringify(l.deck))).size, 100);
  const avg = (a) => a.reduce((x, y) => x + y, 0) / a.length;
  const first = avg(ls.slice(0, 20).filter((l) => !l.hard).map((l) => l.casualWinRate)), last = avg(ls.slice(80).filter((l) => !l.hard).map((l) => l.casualWinRate));
  ok(first > last, `chapter 1 (${first.toFixed(2)}) should be easier than chapter 5 (${last.toFixed(2)})`);
  const hard = avg(ls.filter((l) => l.hard).map((l) => l.casualWinRate)), normal = avg(ls.filter((l) => !l.hard).map((l) => l.casualWinRate));
  ok(hard < normal, `hard levels (${hard.toFixed(2)}) must be harder than normal (${normal.toFixed(2)})`);
  ok(ls.filter((l) => l.hard).every((l) => l.rewardCoins > 2 * ls.find((m) => m.id === l.id - 1).rewardCoins), 'hard reward > 2x neighbour');
});
console.log(`\n${pass} passed, ${fail} failed`);
process.exit(fail ? 1 : 0);
