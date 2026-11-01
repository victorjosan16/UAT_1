// Conveyor rules tests (node tools/conveyor/test.mjs). Mirrored in Tests/EditMode/ConveyorRulesTests.cs.
import fs from 'fs';
import { Session, replay, P, TRANSFER, LEAVE } from './rules.mjs';
import { validate } from './build-levels.mjs';

let pass = 0, fail = 0;
function t(name, fn) { try { fn(); pass++; console.log('  ok  ' + name); } catch (e) { fail++; console.log('  FAIL ' + name + ': ' + e.message); } }
function eq(a, b, m) { if (a !== b) throw new Error(`${m || ''} expected ${b}, got ${a}`); }
function ok(c, m) { if (!c) throw new Error(m || 'assertion failed'); }

// tiny level: 10 slots, stations at fixed points, feeder at 50
function mk({ belt, hopper = [], queue, stations, visible = 7, speed = 10 }) {
  const slots = belt.length;
  return { slots, speed, feeder: 50, visible, belt, hopper, queue, colors: ['#f00', '#0f0', '#00f'],
    stations: stations.map((s) => ({ s: s.s, capacity: s.cap || 4, lock: s.lock || null })) };
}
function run(s, n) { for (let i = 0; i < n; i++) s.step(); }

t('slots keep constant spacing and wrap around the loop', () => {
  const s = new Session(mk({ belt: [0, -1, -1, -1, -1, -1, -1, -1, -1, -1], queue: [[1]], stations: [{ s: 450 }] }));
  for (let i = 0; i < 250; i++) {
    s.step();
    for (let k = 0; k < s.C; k++) eq((s.slotPos((k + 1) % s.C) - s.slotPos(k) + s.L) % s.L, P, 'spacing');
  }
  eq(s.slot[0], 0, 'cube stays in its slot');
});

t('matching cube transfers, then lands after TRANSFER ticks', () => {
  const s = new Session(mk({ belt: [0, -1, -1, -1, -1, -1, -1, -1, -1, -1], queue: [[0]], stations: [{ s: 450 }] }));
  s.place(0);
  let took = -1;
  for (let i = 0; i < 100 && took < 0; i++) { s.step(); if (s.transfers.length) took = s.tick; }
  ok(took > 0, 'transfer started');
  eq(s.slot[0], -1, 'cube left the belt');
  run(s, TRANSFER - 1); eq(s.completed, 0, 'not yet landed');
  s.step(); eq(s.completed, 1, 'landed and completed');
  ok(s.isWon(), 'won');
});

t('non-matching cube keeps circulating', () => {
  const s = new Session(mk({ belt: [1, 0, -1, -1, -1, -1, -1, -1, -1, -1], queue: [[0]], stations: [{ s: 450 }] }));
  s.place(0);
  run(s, 300);
  eq(s.slot[0], 1, 'red-needing tray ignores green');
  ok(s.isWon(), 'the red cube was taken');
});

t('tray fills compartments in order', () => {
  const s = new Session(mk({ belt: [1, 0, -1, -1, -1, -1, -1, -1, -1, -1], queue: [[0, 1]], stations: [{ s: 450 }] }));
  s.place(0);
  run(s, 400);
  ok(s.isWon(), 'green waits a lap until red is in');
});

t('one cube is reserved by one station only', () => {
  const s = new Session(mk({ belt: [0, -1, -1, -1, -1, -1, -1, -1, -1, 0], queue: [[0], [0]], stations: [{ s: 300 }, { s: 600 }] }));
  s.place(0); s.place(0);
  run(s, 60);
  eq(s.transfers.length + s.completed, 1, 'first cube reserved by the first station only');
  run(s, 200); ok(s.isWon()); eq(s.delivered, 2, 'each cube delivered once');
});

t('double tap with one free station places one tray', () => {
  const s = new Session(mk({ belt: [0, 0, -1, -1, -1, -1, -1, -1, -1, -1], queue: [[0], [0]], stations: [{ s: 450 }] }));
  ok(s.place(0) >= 0); eq(s.place(0), -1, 'no free station: nothing consumed'); eq(s.queue.length, 1);
});

t('completed tray leaves and frees the station; queue advances', () => {
  const s = new Session(mk({ belt: [0, 1, -1, -1, -1, -1, -1, -1, -1, -1], queue: [[0], [1], [0]], visible: 2, stations: [{ s: 450 }] }));
  eq(s.visibleCount(), 2);
  s.place(0); eq(s.queue[0].colors[0], 1, 'queue shifted');
  let c = 0; while (s.completed < 1 && c++ < 400) s.step();
  eq(s.place(0), -1, 'station busy while leaving');
  run(s, LEAVE); ok(s.place(0) >= 0, 'station free after leaving');
});

t('numbered lock opens after n completed trays; plus slot opens with Extra Station', () => {
  const s = new Session(mk({ belt: [0, 0, -1, -1, -1, -1, -1, -1, -1, -1], queue: [[0], [0]], stations: [{ s: 300 }, { s: 600, lock: { type: 'completed', n: 1 } }, { s: 800, lock: { type: 'booster' } }] }));
  s.place(0); eq(s.freeStation(1), -1, 'locked');
  let c = 0; while (s.completed < 1 && c++ < 400) s.step();
  eq(s.stations[1].lock, null, 'numbered lock opened');
  ok(s.extraStation(), 'plus slot opened'); eq(s.stations[2].lock, null);
  ok(!s.extraStation(), 'nothing left to open: not consumed');
});

t('deadlock: belt full of unneeded cubes, needed colour in the hopper', () => {
  const s = new Session(mk({ belt: [1, 1, 1, 1, 1, 1, 1, 1, 1, 1], hopper: [0], queue: [[0], [1, 1, 1, 1], [1, 1, 1, 1], [1, 1]], visible: 1, stations: [{ s: 450 }] }));
  s.place(0);
  run(s, 50);
  ok(s.isStuck(), 'stuck');
  ok(!s.isWon());
});

t('undo restores the state before the last placement', () => {
  const s = new Session(mk({ belt: [0, 1, -1, -1, -1, -1, -1, -1, -1, -1], queue: [[0], [1]], stations: [{ s: 450 }] }), { undo: true });
  const before = s.snapshot();
  s.place(0); run(s, 50);
  ok(s.undoLast()); const after = JSON.parse(s.snapshot()), b = JSON.parse(before);
  eq(JSON.stringify(after.slot), JSON.stringify(b.slot)); eq(after.queue.length, 2); eq(s.boostersUsed, 1);
  ok(!s.undoLast(), 'nothing to undo');
});

t('no cube is ever duplicated or lost (all levels, witness replay)', () => {
  for (const f of fs.readdirSync('Assets/VoxelBloom/Data/Resources/VoxelBloom/Conveyor').sort()) {
    const l = JSON.parse(fs.readFileSync('Assets/VoxelBloom/Data/Resources/VoxelBloom/Conveyor/' + f));
    const cubes = l.queue.reduce((a, q) => a + q.length, 0);
    const s = new Session(l); let w = 0;
    while (!s.isWon() && s.tick < 60 * 60 * 6) {
      while (w < l.witness.length && l.witness[w][0] === s.tick) s.place(l.witness[w++][1]);
      s.step();
      eq(s.beltCount() + s.hopperLeft() + s.transfers.length + s.delivered, cubes, `${f}: cube count at tick ${s.tick}`);
    }
    ok(s.isWon(), f + ' witness wins'); eq(s.tick, l.witnessTicks, f + ' ticks');
  }
});

t('all levels validate (supply == demand, stations, trays)', () => {
  const files = fs.readdirSync('Assets/VoxelBloom/Data/Resources/VoxelBloom/Conveyor');
  ok(files.length >= 50, '50 levels');
  for (const f of files) validate(JSON.parse(fs.readFileSync('Assets/VoxelBloom/Data/Resources/VoxelBloom/Conveyor/' + f)));
});

console.log(`\n${pass} passed, ${fail} failed`);
process.exit(fail ? 1 : 0);
