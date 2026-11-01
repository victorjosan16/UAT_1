// Runnable rules tests for rules v2 (node tools/test-rules.mjs). Mirrors the C# EditMode tests.
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
import { Session, replay } from './rules.mjs';

const ROOT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const LV = path.join(ROOT, 'Assets/VoxelBloom/Data/Resources/VoxelBloom/Levels');
let pass = 0, fail = 0;
function test(name, fn) { try { fn(); pass++; console.log('  ok  ', name); } catch (e) { fail++; console.log('  FAIL', name, '-', e.message); } }
function eq(a, b, m = '') { if (a !== b) throw new Error(`${m} expected ${b} got ${a}`); }
function ok(c, m) { if (!c) throw new Error(m || 'assertion failed'); }

// 3x3x3 cube: colour 0 shell (26), colour 1 centre (1)
function cube(columns) {
  let v = ''; for (let z = 0; z < 3; z++) for (let y = 0; y < 3; y++) for (let x = 0; x < 3; x++) v += x === 1 && y === 1 && z === 1 ? '1' : '0';
  return { size: [3, 3, 3], palette: ['#ff0000', '#00ff00'], voxels: v, columns };
}
const idx = (x, y, z) => x + 3 * (y + 3 * z);

test('only the top block of a column is active; tap moves it into the first free launcher', () => {
  const s = new Session(cube([[[0, 5], [0, 21]], [[1, 1]], [], []]));
  ok(s.tap(0)); eq(s.launchers[0].n, 5); eq(s.columns[0].length, 1); eq(s.columns[0][0].n, 21);
  ok(!s.canTap(2), 'empty column'); eq(s.moves, 1);
});
test('launcher fires one voxel of its colour per round and frees when empty', () => {
  const s = new Session(cube([[[0, 3]], [[0, 23], [1, 1]], [], []]));
  s.tap(0);
  for (let k = 0; k < 3; k++) { const r = s.step(); eq(r.shots.length, 1); ok(s.cell[r.shots[0][1]] === -1); }
  eq(s.launchers[0], null); eq(s.remaining[0], 23); eq(s.remaining[1], 1, 'never other colours');
});
test('a launcher waits while its colour is hidden, then fires when it becomes visible', () => {
  const s = new Session(cube([[[1, 1]], [[0, 26]], [], []]));
  s.tap(0); eq(s.step(), null, 'centre not visible: waiting'); ok(!s.canFire(0));
  s.tap(1); s.resolveAll(); ok(s.isWon());
});
test('parallel launchers fire in the same round (combo score)', () => {
  const s = new Session(cube([[[0, 13]], [[0, 13]], [[1, 1]], []]));
  s.tap(0); s.tap(1); const r = s.step(); eq(r.combo, 2); eq(s.score, 2 * 10 * 2);
});
test('jammed: all 4 launchers occupied and waiting', () => {
  const v = '0'.repeat(13) + '1' + '0'.repeat(13); // centre colour 1
  const lvl = { size: [3, 3, 3], palette: ['#ff0000', '#00ff00'], voxels: v, columns: [[[1, 1]], [[1, 1]], [[1, 1]], [[1, 1], [0, 26]]] };
  const s = new Session(lvl);
  for (let c = 0; c < 4; c++) s.tap(c);
  s.settle(); eq(s.failReason(), 'jammed'); ok(!s.canTap(3));
});
test('finished colour: launcher freed (rest wasted) and its blocks leave the columns', () => {
  const s = new Session(cube([[[0, 30]], [[0, 5]], [[1, 1]], []]));
  s.tap(0); s.resolveAll(); eq(s.remaining[0], 0); eq(s.columns[1].length, 0, 'colour-0 block purged');
  s.tap(2); s.resolveAll(); ok(s.isWon());
});
test('collapse: floating piece falls and counts as destroyed', () => {
  const v = '00000' + '..1..' + '..00.';
  const s = new Session({ size: [5, 3, 1], palette: ['#ff0000', '#00ff00'], voxels: v, columns: [[[1, 1]], [[0, 7]], [], []] });
  s.tap(0); const r = s.step(); eq(r.shots.length, 1); eq(r.dropped.length, 2); eq(s.totalRemaining, 5);
});
test('models: every model is a single connected piece (no floating cubes)', () => {
  const MD = path.join(ROOT, 'Assets/VoxelBloom/Data/Resources/VoxelBloom/Models');
  for (const f of fs.readdirSync(MD).filter((x) => x.endsWith('.json'))) {
    const m = JSON.parse(fs.readFileSync(path.join(MD, f)));
    const s = new Session({ size: m.size, palette: m.palette, voxels: m.voxels, columns: [[], [], [], []] });
    eq(s.collapse().length, 0, f + ' has detached pieces');
  }
});
test('all levels: witness replays to a win with the recorded score; 6-10+ colours; HARD every 5th', () => {
  const files = fs.readdirSync(LV).filter((f) => f.endsWith('.json')).sort();
  ok(files.length >= 100, 'at least 100 levels, got ' + files.length);
  for (const f of files) {
    const l = JSON.parse(fs.readFileSync(path.join(LV, f)));
    const { ok: valid, s } = replay(l, l.witness);
    ok(valid && s.isWon(), f + ' witness does not win'); eq(s.score, l.witnessScore, f + ' score'); eq(s.moves, l.witnessMoves, f + ' moves');
    ok(l.palette.length >= 6, f + ' must have at least 6 colours (has ' + l.palette.length + ')');
    eq(l.hard, l.id % 5 === 0, f + ' hard flag');
    eq(l.columns.length, 4);
  }
});
test('levels are distinct; difficulty trend; HARD levels harder and pay more', () => {
  const ls = fs.readdirSync(LV).filter((f) => f.endsWith('.json')).sort().map((f) => JSON.parse(fs.readFileSync(path.join(LV, f))));
  eq(new Set(ls.map((l) => l.model)).size, ls.length, 'one unique object per level');
  const avg = (a) => a.reduce((x, y) => x + y, 0) / a.length;
  const first = avg(ls.slice(0, 20).filter((l) => !l.hard).map((l) => l.casualWinRate)), last = avg(ls.slice(-20).filter((l) => !l.hard).map((l) => l.casualWinRate));
  ok(first >= last, `first chapter (${first.toFixed(2)}) should not be harder than the last (${last.toFixed(2)})`);
  const hard = avg(ls.filter((l) => l.hard).map((l) => l.casualWinRate)), normal = avg(ls.filter((l) => !l.hard).map((l) => l.casualWinRate));
  ok(hard < normal, `hard (${hard.toFixed(2)}) vs normal (${normal.toFixed(2)})`);
  ok(ls.filter((l) => l.hard).every((l) => l.rewardCoins >= 2 * ls.find((m) => m.id === l.id - 1).rewardCoins), 'hard reward >= 2x neighbour');
});
console.log(`\n${pass} passed, ${fail} failed`);
process.exit(fail ? 1 : 0);
