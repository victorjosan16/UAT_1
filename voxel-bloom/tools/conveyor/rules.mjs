// VOXEL BLOOM 3D — conveyor BUS rules (rules v4). Reference implementation, mirrored 1:1 by
// Assets/VoxelBloom/Scripts/Core/Conveyor/ConveyorSession.cs. Integer, tick based (60 ticks/s), deterministic.
//
// Belt: one closed loop of `slots` evenly spaced slots (pitch P path units), moving clockwise by `speed` units per tick
//   (constant spacing, no overtaking). A slot holds one cube or is empty.
// BUS: a tray of compartments; compartment i requires colour need[i] and holds have[i] (a colour, EMPTY or INCOMING).
//   A BUS sits at a station (fixed transfer point on the loop, any side). It is complete when have[i] == need[i] for all i.
// Release (player taps a BUS): its first compartment holding a wrong cube is emptied and that cube waits at the station's
//   transfer point; it enters the belt in the first EMPTY slot that crosses the point. Only allowed while space is free.
// Collect: a belt cube crossing a station's transfer point is reserved by the first EMPTY compartment of that BUS that
//   requires its colour; it lands TRANSFER ticks later. Otherwise it keeps circulating (never moved between BUSes directly).
// Space: occupied = cubes on the belt + cubes waiting to enter it. Releases (and the feeder) need occupied < space.
//   The HUD shows occupied/space; 8/8 is NOT a win.
// Complete BUS: objective +1, leaves the station in LEAVE ticks, station becomes free for a queued BUS.
// Player also taps one of the first `visible` queued BUSes: it goes to the first free unlocked station with capacity.
// Locks: {type:'completed', n} opens after n completed BUSes; {type:'booster'} (green plus) opens with Extra Station.
// Win: every BUS complete and nothing in flight. Loss (deadlock): nothing in flight, waiting or leaving, no placement,
//   no release, no feed, and no belt cube can be collected by any open BUS.
// Supply equals demand per colour (validated): belt + feeder + BUS cubes == required compartments.

export const P = 100, TRANSFER = 18, LEAVE = 24, TICKS_PER_SECOND = 60, MAX_TICKS = 60 * 60 * 8;
export const EMPTY = -1, INCOMING = -2;
export const PLACE = 0, RELEASE = 1;

const mkBus = (id, b) => ({ id, need: b.need.slice(), have: (b.have || b.need.map(() => EMPTY)).slice(), leave: 0 });

export class Session {
  constructor(level, opts = {}) {
    this.level = level;
    this.C = level.slots; this.L = this.C * P; this.v = level.speed; this.space = level.space || level.slots;
    this.offset = 0; this.tick = 0;
    this.slot = Int32Array.from(level.belt);
    this.hopper = (level.hopper || []).slice(); this.hopperPos = 0;
    let id = 0;
    this.stations = level.stations.map((st) => ({ s: st.s, cap: st.capacity, lock: st.lock ? { ...st.lock } : null, bus: null, out: [] }));
    (level.start || []).forEach((b, k) => { if (b) this.stations[k].bus = mkBus(id++, b); });
    this.queue = level.queue.map((b) => mkBus(id++, b));
    this.total = id;
    this.transfers = []; // {station, comp, color, t}
    this.completed = 0; this.delivered = 0; this.released = 0; this.gone = 0; this.placed = 0; this.boostersUsed = 0;
    this.undo = opts.undo ? [] : null;
    this.events = opts.events ? [] : null;
    for (let k = 0; k < this.stations.length; k++) this.checkComplete(k);
    this.updateLocks();
  }

  // ---------------------------------------------------------------- queries
  slotPos(k) { return (k * P + this.offset) % this.L; }
  isWon() { return this.completed === this.total && this.transfers.length === 0; }
  beltCount() { let n = 0; for (const c of this.slot) if (c >= 0) n++; return n; }
  waiting() { let n = 0; for (const st of this.stations) n += st.out.length; return n; }
  occupied() { return this.beltCount() + this.waiting(); }
  hopperLeft() { return this.hopper.length - this.hopperPos; }
  visibleCount() { return Math.min(this.level.visible, this.queue.length); }
  open(st) { return st.bus && st.bus.leave === 0; }
  freeStation(len) {
    for (let k = 0; k < this.stations.length; k++) { const st = this.stations[k]; if (!st.lock && !st.bus && st.cap >= len) return k; }
    return -1;
  }
  canPlace(qi) { return qi >= 0 && qi < this.visibleCount() && !this.isWon() && this.freeStation(this.queue[qi].need.length) >= 0; }
  anyPlaceable() { for (let i = 0; i < this.visibleCount(); i++) if (this.canPlace(i)) return true; return false; }
  wrongIndex(b) { for (let i = 0; i < b.need.length; i++) if (b.have[i] >= 0 && b.have[i] !== b.need[i]) return i; return -1; }
  canRelease(k) { const st = this.stations[k]; return !!st && this.open(st) && this.occupied() < this.space && this.wrongIndex(st.bus) >= 0; }
  anyReleasable() { for (let k = 0; k < this.stations.length; k++) if (this.canRelease(k)) return true; return false; }
  wants(st, c) { if (!this.open(st)) return -1; const b = st.bus; for (let i = 0; i < b.need.length; i++) if (b.have[i] === EMPTY && b.need[i] === c) return i; return -1; }

  isStuck() {
    if (this.isWon() || this.transfers.length || this.waiting()) return false;
    for (const st of this.stations) if (st.bus && st.bus.leave > 0) return false;
    if (this.anyPlaceable() || this.anyReleasable()) return false;
    if (this.hopperLeft() > 0 && this.occupied() < this.space && this.beltCount() < this.C) return false;
    for (const c of this.slot) if (c >= 0) for (const st of this.stations) if (this.wants(st, c) >= 0) return false;
    return true;
  }

  // ---------------------------------------------------------------- actions
  /** Sends visible queued BUS `qi` to the first free station; returns the station or -1 (nothing consumed). */
  place(qi) {
    if (!this.canPlace(qi)) return -1;
    if (this.undo) this.undo.push(this.snapshot());
    const bus = this.queue.splice(qi, 1)[0];
    const k = this.freeStation(bus.need.length);
    this.stations[k].bus = bus;
    this.placed++;
    if (this.events) this.events.push({ type: 'place', bus: bus.id, station: k, tick: this.tick });
    this.checkComplete(k);
    return k;
  }

  /** The BUS at station k lets its first wrong cube out (it waits at the transfer point for an empty slot). */
  release(k) {
    if (!this.canRelease(k)) return false;
    if (this.undo) this.undo.push(this.snapshot());
    const st = this.stations[k], i = this.wrongIndex(st.bus), c = st.bus.have[i];
    st.bus.have[i] = EMPTY; st.out.push(c); this.released++;
    if (this.events) this.events.push({ type: 'lift', station: k, comp: i, color: c, tick: this.tick });
    return true;
  }

  act(a) { return a[1] === PLACE ? this.place(a[2]) >= 0 : this.release(a[2]); }

  checkComplete(k) {
    const st = this.stations[k], b = st.bus;
    if (!b || b.leave > 0) return;
    for (let i = 0; i < b.need.length; i++) if (b.have[i] !== b.need[i]) return;
    if (st.out.length) return;
    this.completed++; b.leave = LEAVE;
    if (this.events) this.events.push({ type: 'complete', station: k, bus: b.id, tick: this.tick });
    this.updateLocks();
  }

  step() {
    this.tick++;
    for (let i = 0; i < this.transfers.length; i++) {
      const tr = this.transfers[i];
      if (--tr.t > 0) continue;
      this.stations[tr.station].bus.have[tr.comp] = tr.color; this.delivered++;
      if (this.events) this.events.push({ type: 'land', station: tr.station, comp: tr.comp, color: tr.color, tick: this.tick });
      this.transfers.splice(i, 1); i--;
      this.checkComplete(tr.station);
    }
    for (let k = 0; k < this.stations.length; k++) {
      const st = this.stations[k];
      if (st.bus && st.bus.leave > 0 && --st.bus.leave === 0) {
        if (this.events) this.events.push({ type: 'leave', station: k, bus: st.bus.id, tick: this.tick });
        this.gone += st.bus.need.length; st.bus = null;
      }
    }
    const prev = this.offset;
    this.offset = (this.offset + this.v) % this.L;
    for (let k = 0; k < this.C; k++) {
      const p0 = (k * P + prev) % this.L;
      for (let si = 0; si < this.stations.length; si++) {
        const st = this.stations[si], d = (st.s - p0 + this.L) % this.L;
        if (d === 0 || d > this.v) continue;
        if (this.slot[k] >= 0) {
          const i = this.wants(st, this.slot[k]);
          if (i < 0) continue;
          st.bus.have[i] = INCOMING;
          this.transfers.push({ station: si, comp: i, color: this.slot[k], t: TRANSFER });
          if (this.events) this.events.push({ type: 'take', slot: k, station: si, comp: i, color: this.slot[k], tick: this.tick });
          this.slot[k] = -1;
          break;
        } else if (st.out.length) {
          this.slot[k] = st.out.shift();
          if (this.events) this.events.push({ type: 'drop', slot: k, station: si, color: this.slot[k], tick: this.tick });
          this.checkComplete(si);
          break;
        }
      }
      if (this.slot[k] < 0 && this.hopperLeft() > 0 && this.occupied() < this.space) {
        const d = (this.level.feeder - p0 + this.L) % this.L;
        if (d !== 0 && d <= this.v) {
          this.slot[k] = this.hopper[this.hopperPos++];
          if (this.events) this.events.push({ type: 'feed', slot: k, color: this.slot[k], tick: this.tick });
        }
      }
    }
  }

  updateLocks() {
    for (const st of this.stations) if (st.lock && st.lock.type === 'completed' && this.completed >= st.lock.n) st.lock = null;
  }

  // ---------------------------------------------------------------- boosters
  undoLast() { if (!this.undo || !this.undo.length) return false; const used = this.boostersUsed + 1; this.restore(this.undo.pop()); this.boostersUsed = used; return true; }
  extraStation() {
    for (const st of this.stations) if (st.lock && st.lock.type === 'booster') { st.lock = null; this.boostersUsed++; return true; }
    return false;
  }
  shuffle(rand) {
    if (this.queue.length < 2) return false;
    const orig = this.queue.slice();
    for (let attempt = 0; attempt < 20; attempt++) {
      const q = orig.slice();
      for (let i = q.length - 1; i > 0; i--) { const j = Math.floor(rand() * (i + 1)); [q[i], q[j]] = [q[j], q[i]]; }
      this.queue = q;
      if (playGreedy(this.cloneForSim()).won) { this.boostersUsed++; return true; }
    }
    this.queue = orig;
    return false;
  }

  // ---------------------------------------------------------------- state
  snapshot() {
    return JSON.stringify({ offset: this.offset, tick: this.tick, slot: Array.from(this.slot), hopper: this.hopper, hopperPos: this.hopperPos, queue: this.queue,
      stations: this.stations.map((s) => ({ lock: s.lock, bus: s.bus, out: s.out })), transfers: this.transfers,
      completed: this.completed, delivered: this.delivered, released: this.released, gone: this.gone, placed: this.placed, boostersUsed: this.boostersUsed });
  }
  restore(json) {
    const o = JSON.parse(json);
    this.offset = o.offset; this.tick = o.tick; this.slot = Int32Array.from(o.slot); this.hopper = o.hopper; this.hopperPos = o.hopperPos; this.queue = o.queue;
    o.stations.forEach((s, i) => { this.stations[i].lock = s.lock; this.stations[i].bus = s.bus; this.stations[i].out = s.out; });
    this.transfers = o.transfers; this.completed = o.completed; this.delivered = o.delivered; this.released = o.released; this.gone = o.gone; this.placed = o.placed; this.boostersUsed = o.boostersUsed;
  }
  cloneForSim() { const s = new Session(this.level); s.restore(this.snapshot()); return s; }
}

// ------------------------------------------------------------------ players / solver
function demand(s) { // colours still needed by EMPTY compartments of open BUSes, minus cubes already circulating for them
  const d = new Map();
  for (const st of s.stations) if (s.open(st)) st.bus.need.forEach((c, i) => { if (st.bus.have[i] === EMPTY) d.set(c, (d.get(c) || 0) + 1); });
  for (const c of s.slot) if (c >= 0 && d.has(c)) d.set(c, d.get(c) - 1);
  for (const st of s.stations) for (const c of st.out) if (d.has(c)) d.set(c, d.get(c) - 1);
  return d;
}

/** Heuristic action [tick, kind, index] or null: release a wrong cube that an open BUS is waiting for; place a BUS. */
export function bestAction(s) {
  const d = demand(s);
  let best = null, bestScore = 0;
  for (let k = 0; k < s.stations.length; k++) {
    if (!s.canRelease(k)) continue;
    const b = s.stations[k].bus, c = b.have[s.wrongIndex(b)];
    const score = (d.get(c) || 0) > 0 ? 100 : s.occupied() < s.space - 2 ? 10 : 0;
    if (score > bestScore) { bestScore = score; best = [s.tick, RELEASE, k]; }
  }
  for (let i = 0; i < s.visibleCount(); i++) {
    if (!s.canPlace(i)) continue;
    const b = s.queue[i]; let score = 50;
    b.need.forEach((c, j) => { if (b.have[j] === c) score += 3; else if ((d.get(b.have[j]) || 0) > 0) score += 5; });
    if (score > bestScore) { bestScore = score; best = [s.tick, PLACE, i]; }
  }
  return best;
}

/** Greedy player; records its actions in `witness` when given. */
export function playGreedy(s, witness = null) {
  while (!s.isWon() && !s.isStuck() && s.tick < MAX_TICKS) {
    let a, guard = 0;
    while ((a = bestAction(s)) && guard++ < 50) { if (witness) witness.push(a); s.act(a); }
    s.step();
  }
  return { won: s.isWon(), ticks: s.tick };
}

/** Solver: the greedy player; if it fails, rollouts at each decision over every possible action. */
export function solve(level, rollouts = false) {
  { const s = new Session(level), w = []; const r = playGreedy(s, w); if (r.won) return { witness: w, ticks: r.ticks }; }
  if (!rollouts) return null;
  const s = new Session(level), witness = [];
  while (!s.isWon() && !s.isStuck() && s.tick < MAX_TICKS) {
    const opts = [];
    for (let i = 0; i < s.visibleCount(); i++) if (s.canPlace(i)) opts.push([s.tick, PLACE, i]);
    for (let k = 0; k < s.stations.length; k++) if (s.canRelease(k)) opts.push([s.tick, RELEASE, k]);
    if (opts.length && s.tick % 15 === 0) {
      let pick = null, bestTicks = 1e9;
      for (const a of opts) { const p = s.cloneForSim(); p.act(a); const r = playGreedy(p); if (r.won && r.ticks < bestTicks) { bestTicks = r.ticks; pick = a; } }
      if (pick) { witness.push(pick); s.act(pick); continue; }
    }
    s.step();
  }
  return s.isWon() ? { witness, ticks: s.tick } : null;
}

/** Replays a witness [[tick, kind, index], ...]. */
export function replay(level, witness) {
  const s = new Session(level);
  let w = 0;
  while (!s.isWon() && s.tick < MAX_TICKS) {
    while (w < witness.length && witness[w][0] === s.tick) { if (!s.act(witness[w])) return s; w++; }
    if (s.isStuck()) break;
    s.step();
  }
  return s;
}

/** Casual player: a random possible action after a random delay. */
export function casualWinRate(level, runs, rand) {
  let wins = 0;
  for (let r = 0; r < runs; r++) {
    const s = new Session(level); let wait = 30;
    while (!s.isWon() && !s.isStuck() && s.tick < MAX_TICKS) {
      if (wait > 0) wait--;
      else {
        const opts = [];
        for (let i = 0; i < s.visibleCount(); i++) if (s.canPlace(i)) opts.push([0, PLACE, i]);
        for (let k = 0; k < s.stations.length; k++) if (s.canRelease(k)) opts.push([0, RELEASE, k]);
        if (opts.length) { s.act(opts[Math.floor(rand() * opts.length)]); wait = 20 + Math.floor(rand() * 70); continue; }
      }
      s.step();
    }
    if (s.isWon()) wins++;
  }
  return wins / runs;
}
