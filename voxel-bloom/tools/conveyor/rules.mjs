// VOXEL BLOOM 3D — conveyor sort rules (rules v3). Reference implementation, mirrored 1:1 by
// Assets/VoxelBloom/Scripts/Core/Conveyor/ConveyorSession.cs. All gameplay math is integer and tick based (60 ticks/s),
// so both implementations produce identical results (witness replays prove it).
//
// Board: one closed loop of `slots` evenly spaced belt slots (pitch P = 100 path units, length L = slots*P).
// Slots move together by `speed` units per tick (no overtaking, constant spacing). A slot holds one cube or is empty.
// Feeder: when an EMPTY slot crosses the feeder point, the next hopper cube enters it.
// Stations: fixed points on the inner side of the loop. A station holds one tray. When a slot carrying a cube crosses
//   the station point and the cube's colour equals the tray's next required compartment, the cube is reserved for that
//   compartment and leaves the belt; it fills the compartment `TRANSFER` ticks later (visible flight).
// Tray complete: all compartments filled -> objective +1; the tray leaves the station during `LEAVE` ticks.
// Player: taps one of the first `visible` queued trays; it goes to the first free, unlocked station with enough capacity.
// Locks: {type:'completed', n} unlocks after n completed trays; {type:'booster'} (green plus) unlocks with Extra Station.
// Win: every tray completed. Loss (deadlock): nothing in flight or leaving, no belt cube matches any station's next
//   colour, no cube can enter (hopper empty or belt full), and no queued tray can be placed.
// Supply equals demand per colour (validated), so a placed tray can always progress unless the belt is full of cubes
// nobody at a station needs while the needed ones wait in the hopper: that is the puzzle.

export const P = 100, TRANSFER = 18, LEAVE = 24, TICKS_PER_SECOND = 60;

export class Session {
  constructor(level, opts = {}) {
    this.level = level;
    this.C = level.slots; this.L = this.C * P; this.v = level.speed;
    this.offset = 0; this.tick = 0;
    this.slot = Int32Array.from(level.belt);          // cube colour or -1, by slot identity
    this.hopper = level.hopper.slice(); this.hopperPos = 0;
    this.queue = level.queue.map((t, i) => ({ id: i, colors: t.slice() }));
    this.stations = level.stations.map((st) => ({ s: st.s, cap: st.capacity, lock: st.lock ? { ...st.lock } : null, tray: null, leave: 0 }));
    this.transfers = []; // {station, color, t}
    this.completed = 0; this.delivered = 0; this.total = level.queue.length; this.placed = 0; this.boostersUsed = 0;
    this.undo = opts.undo ? [] : null;
    this.events = opts.events ? [] : null;
    this.updateLocks();
  }

  // ---------------------------------------------------------------- queries
  slotPos(k) { return (k * P + this.offset) % this.L; }
  isWon() { return this.completed === this.total; }
  beltCount() { let n = 0; for (const c of this.slot) if (c >= 0) n++; return n; }
  hopperLeft() { return this.hopper.length - this.hopperPos; }
  visibleCount() { return Math.min(this.level.visible, this.queue.length); }
  nextColor(st) { const t = st.tray; return t && t.reserved < t.colors.length ? t.colors[t.reserved] : -1; }
  freeStation(len) {
    for (let k = 0; k < this.stations.length; k++) { const st = this.stations[k]; if (!st.lock && !st.tray && st.leave === 0 && st.cap >= len) return k; }
    return -1;
  }
  canPlace(qi) { return qi >= 0 && qi < this.visibleCount() && this.freeStation(this.queue[qi].colors.length) >= 0 && !this.isWon(); }
  anyPlaceable() { for (let i = 0; i < this.visibleCount(); i++) if (this.canPlace(i)) return true; return false; }

  /** Deadlock: no progress can ever happen without boosters. */
  isStuck() {
    if (this.isWon() || this.transfers.length) return false;
    for (const st of this.stations) if (st.leave > 0) return false;
    if (this.anyPlaceable()) return false;
    if (this.hopperLeft() > 0 && this.beltCount() < this.C) return false;
    const need = new Set(); for (const st of this.stations) { const c = this.nextColor(st); if (c >= 0) need.add(c); }
    for (const c of this.slot) if (c >= 0 && need.has(c)) return false;
    return true;
  }

  // ---------------------------------------------------------------- actions
  /** Sends visible queued tray `qi` to the first free station; returns the station index or -1 (nothing consumed). */
  place(qi) {
    if (!this.canPlace(qi)) return -1;
    if (this.undo) this.undo.push(this.snapshot());
    const tray = this.queue.splice(qi, 1)[0];
    const k = this.freeStation(tray.colors.length);
    this.stations[k].tray = { id: tray.id, colors: tray.colors, reserved: 0, filled: 0 };
    this.placed++;
    if (this.events) this.events.push({ type: 'place', tray: tray.id, station: k, tick: this.tick });
    return k;
  }

  step() {
    this.tick++;
    // transfers in flight land
    for (let i = 0; i < this.transfers.length; i++) {
      const tr = this.transfers[i];
      if (--tr.t > 0) continue;
      const st = this.stations[tr.station];
      st.tray.filled++; this.delivered++;
      if (this.events) this.events.push({ type: 'land', station: tr.station, tick: this.tick });
      if (st.tray.filled === st.tray.colors.length) {
        this.completed++; st.leave = LEAVE;
        if (this.events) this.events.push({ type: 'complete', station: tr.station, tray: st.tray.id, tick: this.tick });
        this.updateLocks();
      }
      this.transfers.splice(i, 1); i--;
    }
    // completed trays leave their station
    for (const st of this.stations) if (st.leave > 0 && --st.leave === 0) st.tray = null;
    // belt moves; slots crossing station points / the feeder
    const prev = this.offset;
    this.offset = (this.offset + this.v) % this.L;
    for (let k = 0; k < this.C; k++) {
      const p0 = (k * P + prev) % this.L;
      if (this.slot[k] >= 0) {
        for (let si = 0; si < this.stations.length; si++) {
          const st = this.stations[si];
          if (!st.tray || st.leave > 0) continue;
          const d = (st.s - p0 + this.L) % this.L;
          if (d === 0 || d > this.v) continue;
          if (this.nextColor(st) !== this.slot[k]) continue;
          st.tray.reserved++;
          this.transfers.push({ station: si, color: this.slot[k], t: TRANSFER });
          if (this.events) this.events.push({ type: 'take', slot: k, station: si, color: this.slot[k], tick: this.tick });
          this.slot[k] = -1;
          break;
        }
      }
      if (this.slot[k] < 0 && this.hopperLeft() > 0) {
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
  /** Reorders the queued trays (seeded); keeps the order only if the greedy player can still win. */
  shuffle(rand) {
    if (this.queue.length < 2) return false;
    const orig = this.queue.slice();
    for (let attempt = 0; attempt < 20; attempt++) {
      const q = orig.slice();
      for (let i = q.length - 1; i > 0; i--) { const j = Math.floor(rand() * (i + 1)); [q[i], q[j]] = [q[j], q[i]]; }
      this.queue = q;
      const probe = this.cloneForSim();
      if (playGreedy(probe).won) { this.boostersUsed++; return true; }
    }
    this.queue = orig;
    return false;
  }

  // ---------------------------------------------------------------- state
  snapshot() {
    return JSON.stringify({ offset: this.offset, tick: this.tick, slot: Array.from(this.slot), hopperPos: this.hopperPos, queue: this.queue,
      stations: this.stations.map((s) => ({ lock: s.lock, tray: s.tray, leave: s.leave })), transfers: this.transfers, completed: this.completed, delivered: this.delivered, placed: this.placed, boostersUsed: this.boostersUsed });
  }
  restore(json) {
    const o = JSON.parse(json);
    this.offset = o.offset; this.tick = o.tick; this.slot = Int32Array.from(o.slot); this.hopperPos = o.hopperPos; this.queue = o.queue;
    o.stations.forEach((s, i) => { this.stations[i].lock = s.lock; this.stations[i].tray = s.tray; this.stations[i].leave = s.leave; });
    this.transfers = o.transfers; this.completed = o.completed; this.delivered = o.delivered; this.placed = o.placed; this.boostersUsed = o.boostersUsed;
  }
  cloneForSim() { const s = new Session(this.level); s.restore(this.snapshot()); return s; }
}

// ------------------------------------------------------------------ players / solver
/** Heuristic choice: the visible tray whose first required colours are most available on the belt (ties: lowest index). */
export function bestTray(s) {
  const count = new Map(); for (const c of s.slot) if (c >= 0) count.set(c, (count.get(c) || 0) + 1);
  let best = -1, bestScore = -1e9;
  for (let i = 0; i < s.visibleCount(); i++) {
    if (!s.canPlace(i)) continue;
    const cols = s.queue[i].colors, avail = new Map(count);
    let score = 0, w = 1;
    for (const c of cols) { const a = avail.get(c) || 0; if (a > 0) { score += w * 10; avail.set(c, a - 1); } else break; w *= 0.7; }
    score -= cols.length * 0.5;
    if (score > bestScore) { bestScore = score; best = i; }
  }
  return best;
}

export const MAX_TICKS = 60 * 60 * 6; // 6 minutes of play per level at most

/** Greedy player: places the best tray as soon as a station is free. */
export function playGreedy(s, witness = null) {
  while (!s.isWon() && !s.isStuck() && s.tick < MAX_TICKS) {
    let qi;
    while ((qi = bestTray(s)) >= 0) { if (witness) witness.push([s.tick, qi]); s.place(qi); }
    s.step();
  }
  return { won: s.isWon(), ticks: s.tick };
}

/** Rollout solver: at each decision tries every visible tray and keeps one whose greedy continuation wins. */
export function solve(level) {
  const s = new Session(level), witness = [];
  while (!s.isWon() && !s.isStuck() && s.tick < MAX_TICKS) {
    if (s.anyPlaceable()) {
      let pick = -1, bestTicks = 1e9;
      for (let i = 0; i < s.visibleCount(); i++) {
        if (!s.canPlace(i)) continue;
        const probe = s.cloneForSim(); probe.place(i);
        const r = playGreedy(probe);
        if (r.won && r.ticks < bestTicks) { bestTicks = r.ticks; pick = i; }
      }
      if (pick < 0) return null;
      witness.push([s.tick, pick]); s.place(pick);
      continue;
    }
    s.step();
  }
  return s.isWon() ? { witness, ticks: s.tick } : null;
}

/** Replays a witness [[tick, queueIndex], ...]; returns the final session. */
export function replay(level, witness) {
  const s = new Session(level);
  let w = 0;
  while (!s.isWon() && s.tick < MAX_TICKS) {
    while (w < witness.length && witness[w][0] === s.tick) { if (s.place(witness[w][1]) < 0) return s; w++; }
    if (s.isStuck()) break;
    s.step();
  }
  return s;
}

/** Casual player: random visible tray after a random delay. */
export function casualWinRate(level, runs, rand) {
  let wins = 0;
  for (let r = 0; r < runs; r++) {
    const s = new Session(level); let wait = 0;
    while (!s.isWon() && !s.isStuck() && s.tick < MAX_TICKS) {
      if (wait > 0) wait--;
      else if (s.anyPlaceable()) {
        const opts = []; for (let i = 0; i < s.visibleCount(); i++) if (s.canPlace(i)) opts.push(i);
        s.place(opts[Math.floor(rand() * opts.length)]);
        wait = Math.floor(rand() * 90);
        continue;
      }
      s.step();
    }
    if (s.isWon()) wins++;
  }
  return wins / runs;
}
