// Conveyor BUS rules tests (node tools/conveyor/test.mjs). Mirrored in Tests/EditMode/ConveyorRulesTests.cs.
import fs from 'fs';
import { Session, replay, P, TRANSFER, LEAVE, EMPTY, INCOMING } from './rules.mjs';
import { validate } from './build-levels.mjs';

let pass = 0, fail = 0;
function t(name, fn) { try { fn(); pass++; console.log('  ok  ' + name); } catch (e) { fail++; console.log('  FAIL ' + name + ': ' + e.message); } }
function eq(a, b, m) { if (a !== b) throw new Error(`${m || ''} expected ${b}, got ${a}`); }
function ok(c, m) { if (!c) throw new Error(m || 'assertion failed'); }
const DIR = 'Assets/VoxelBloom/Data/Resources/VoxelBloom/Conveyor/';

// tiny level: 10 slots (L = 1000), speed 10. stations: [s, start bus | null, lock]
function mk({ belt = new Array(10).fill(-1), stations, queue = [], space = 8, visible = 7, hopper = [] }) {
  return { slots: belt.length, speed: 10, feeder: 50, visible, space, belt, hopper, colors: ['#f00', '#0f0', '#00f'],
    stations: stations.map(([s, , lock]) => ({ s, capacity: 4, lock: lock || null })), start: stations.map(([, b]) => b || null), queue };
}
const run = (s, n) => { for (let i = 0; i < n; i++) s.step(); };
const total = (s) => s.beltCount() + s.waiting() + s.transfers.length + s.hopperLeft()
  + s.stations.reduce((a, st) => a + (st.bus ? st.bus.have.filter((c) => c >= 0).length : 0), 0) + s.gone
  + s.queue.reduce((a, b) => a + b.have.filter((c) => c >= 0).length, 0);

t('belt moves clockwise with constant spacing and wraps (all corners)', () => {
  const s = new Session(mk({ belt: [0, -1, -1, -1, -1, -1, -1, -1, -1, -1], stations: [[450, { need: [1], have: [2] }]] }));
  let last = s.slotPos(0), travelled = 0;
  for (let i = 0; i < 250; i++) {
    s.step();
    for (let k = 0; k < s.C; k++) eq((s.slotPos((k + 1) % s.C) - s.slotPos(k) + s.L) % s.L, P, 'spacing');
    travelled += (s.slotPos(0) - last + s.L) % s.L; last = s.slotPos(0);
  }
  eq(travelled, 2500, 'constant speed, 2.5 laps'); eq(s.slot[0], 0, 'cube keeps its slot (no overtaking)');
});

t('release: BUS -> belt only into an empty slot, cube visible on the belt', () => {
  const s = new Session(mk({ stations: [[450, { need: [0, 1], have: [0, 2] }], [800, { need: [2, 1], have: [EMPTY, 1] }]] }));
  ok(s.release(0)); eq(s.stations[0].bus.have[1], EMPTY); eq(s.waiting(), 1); eq(s.occupied(), 1);
  let dropped = false; for (let i = 0; i < 20 && !dropped; i++) { s.step(); dropped = s.beltCount() === 1; }
  ok(dropped, 'entered the belt'); eq(s.waiting(), 0);
});

t('collect: belt -> BUS only into an empty compartment of the same colour', () => {
  const s = new Session(mk({ stations: [[450, { need: [0, 1], have: [0, 2] }], [800, { need: [2, 1], have: [EMPTY, 1] }]] }));
  s.release(0);
  let taken = -1; for (let i = 0; i < 200 && taken < 0; i++) { s.step(); if (s.transfers.length) taken = s.tick; }
  ok(taken > 0, 'collected by station 1'); eq(s.stations[1].bus.have[0], INCOMING);
  run(s, TRANSFER); eq(s.stations[1].bus.have[0], 2); eq(s.completed, 1, 'BUS 1 complete');
});

t('incompatible colour keeps circulating, full BUS does not collect', () => {
  const s = new Session(mk({ belt: [1, -1, -1, -1, -1, -1, -1, -1, -1, -1], stations: [[450, { need: [0, 0], have: [0, EMPTY] }], [800, { need: [1], have: [2] }]] }));
  run(s, 300);
  eq(s.slot[0], 1, 'green not taken by a red-only BUS'); eq(s.transfers.length, 0);
});

t('no direct BUS-to-BUS move: a released cube always travels on the belt', () => {
  const s = new Session(mk({ stations: [[450, { need: [0], have: [1] }], [460 + 200, { need: [1], have: [0] }]] }, ), { events: true });
  s.release(0); s.release(1);
  let order = []; for (let i = 0; i < 400; i++) { s.step(); for (const e of s.events) order.push(e.type); s.events.length = 0; }
  ok(order.indexOf('drop') >= 0 && order.indexOf('drop') < order.indexOf('take'), 'drop before take');
  ok(s.isWon(), 'both BUSes complete after swapping their cubes through the belt');
});

t('one cube is collected once (two BUSes want it)', () => {
  const s = new Session(mk({ belt: [0, -1, -1, -1, -1, -1, -1, -1, -1, -1], stations: [[300, { need: [0, 1], have: [EMPTY, 1] }], [600, { need: [0, 2], have: [EMPTY, 2] }]] }));
  run(s, 60);
  eq(s.transfers.length + s.stations.filter((st) => st.bus && st.bus.have[0] === 0).length, 1, 'only the first BUS');
  eq(s.stations[1].bus.have[0], EMPTY);
});

t('space: 7/8 still releases, 8/8 blocks releases but cubes keep circulating', () => {
  const belt = [0, 0, 0, 0, 0, 0, 0, -1, -1, -1];
  const s = new Session(mk({ belt, space: 8, stations: [[450, { need: [1, 1], have: [2, 2] }], [800, { need: [2, 2, 0], have: [EMPTY, 1, 1] }]] }));
  eq(s.occupied(), 7); ok(s.release(0), '7/8 -> release ok'); eq(s.occupied(), 8);
  ok(!s.release(0), '8/8 -> blocked'); ok(!s.isWon());
  run(s, 30); ok(s.occupied() >= 7, 'belt cubes still circulate');
});

t('space frees when a cube is collected', () => {
  const s = new Session(mk({ belt: [2, -1, -1, -1, -1, -1, -1, -1, -1, -1], space: 1, stations: [[450, { need: [2, 1], have: [EMPTY, 0] }], [800, { need: [0], have: [1] }]] }));
  ok(!s.release(0) && !s.release(1), 'full'); run(s, 60); eq(s.occupied(), 0); ok(s.release(0), 'release after collection');
});

t('complete BUS leaves and frees the station for a queued BUS; double tap places once', () => {
  const s = new Session(mk({ belt: [1, -1, -1, -1, -1, -1, -1, -1, -1, -1], stations: [[450, { need: [1], have: [EMPTY] }]], queue: [{ need: [0], have: [2] }, { need: [2], have: [0] }] }));
  eq(s.place(0), -1, 'station busy: nothing consumed'); eq(s.queue.length, 2);
  let c = 0; while (s.completed < 1 && c++ < 400) s.step();
  eq(s.place(0), -1, 'still leaving'); run(s, LEAVE); ok(s.place(0) >= 0); eq(s.place(0), -1, 'second tap: no free station'); eq(s.queue.length, 1);
});

t('locks: numbered tile opens after n completions; plus slot with Extra Station', () => {
  const s = new Session(mk({ belt: [1, -1, -1, -1, -1, -1, -1, -1, -1, -1], stations: [[300, { need: [1], have: [EMPTY] }], [600, null, { type: 'completed', n: 1 }], [800, null, { type: 'booster' }]] }));
  eq(s.freeStation(1), -1); let c = 0; while (s.completed < 1 && c++ < 400) s.step(); eq(s.stations[1].lock, null);
  ok(s.extraStation()); ok(!s.extraStation(), 'nothing left: not consumed');
});

t('8/8 is not a win; win only when every BUS is complete', () => {
  const s = new Session(mk({ belt: [1, 1, -1, -1, -1, -1, -1, -1, -1, -1], space: 2, stations: [[450, { need: [0], have: [2] }]] }));
  eq(s.occupied(), 2); ok(!s.isWon()); run(s, 200); ok(s.isStuck(), 'deadlock: full belt, nobody needs green');
});

t('undo restores the state before the last action', () => {
  const s = new Session(mk({ stations: [[450, { need: [0, 1], have: [0, 2] }], [800, { need: [2, 1], have: [EMPTY, 1] }]] }), { undo: true });
  const before = s.snapshot(); s.release(0); run(s, 30); ok(s.undoLast()); eq(JSON.parse(s.snapshot()).slot.join(), JSON.parse(before).slot.join()); eq(s.stations[0].bus.have[1], 2);
});

t('all levels validate and their witness wins with no cube lost or duplicated at any tick', () => {
  const files = fs.readdirSync(DIR).filter((f) => f.endsWith('.json')).sort();
  ok(files.length >= 50, '50 levels');
  for (const f of files) {
    const l = JSON.parse(fs.readFileSync(DIR + f)); validate(l);
    const s = new Session(l), n0 = total(s); let w = 0, done = 0;
    while (!s.isWon() && s.tick < 60 * 60 * 8) {
      while (w < l.witness.length && l.witness[w][0] === s.tick) ok(s.act(l.witness[w++]), f + ' action');
      s.step();
      eq(total(s), n0, `${f}: cube count at tick ${s.tick}`);
    }
    ok(s.isWon(), f + ' witness wins'); eq(s.tick, l.witnessTicks, f + ' ticks');
    const r = replay(l, l.witness); ok(r.isWon(), f + ' replay');
  }
});

console.log(`\n${pass} passed, ${fail} failed`);
process.exit(fail ? 1 : 0);
