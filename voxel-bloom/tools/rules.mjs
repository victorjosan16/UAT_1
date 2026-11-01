// Reference implementation of the Voxel Bloom 3D rules, v2 "launchers" (mirrored 1:1 by C# GameSession).
// Any change here must be reflected in Assets/VoxelBloom/Scripts/Core/Gameplay/GameSession.cs.
//
//  * The board has 4 columns of blocks; only the TOP block of each column is active.
//  * Tapping a column moves its top block into the first free launcher (4 launchers).
//  * Each firing round, every occupied launcher (slot order 0..3) shoots ONE projectile at the
//    highest-priority exposed voxel of its colour and decrements its count. Empty launchers free up.
//    A launcher whose colour is finished is freed (rest wasted); one whose colour has no exposed voxel waits.
//  * After each round: structural collapse (detached pieces fall), finished colours are removed from the columns.
//  * Score: 10 per voxel x number of launchers firing in that round (multi-launcher combo, max x4).
//  * Win: all voxels destroyed. Fail: all launchers occupied and none can fire ("jammed"),
//    or no blocks left and no launcher can fire ("out_of_blocks").
export const COLUMNS = 4, LAUNCHERS = 4;

class MinHeap { // of voxel indices ordered by rank
  constructor(rank) { this.a = []; this.rank = rank; }
  push(v) { const a = this.a, r = this.rank; a.push(v); let i = a.length - 1; while (i > 0) { const p = (i - 1) >> 1; if (r[a[p]] <= r[a[i]]) break; [a[p], a[i]] = [a[i], a[p]]; i = p; } }
  peek() { return this.a[0]; }
  pop() { const a = this.a, r = this.rank; const top = a[0], last = a.pop(); if (a.length) { a[0] = last; let i = 0; for (;;) { const l = 2 * i + 1, rr = l + 1; let m = i; if (l < a.length && r[a[l]] < r[a[m]]) m = l; if (rr < a.length && r[a[rr]] < r[a[m]]) m = rr; if (m === i) break; [a[m], a[i]] = [a[i], a[m]]; i = m; } } return top; }
  get size() { return this.a.length; }
}

export class Session {
  constructor(level) {
    const [X, Y, Z] = level.size;
    this.X = X; this.Y = Y; this.Z = Z; this.N = X * Y * Z;
    this.colorCount = level.palette.length;
    this.cell = new Int8Array(this.N);
    for (let i = 0; i < this.N; i++) { const ch = level.voxels[i]; this.cell[i] = ch === '.' ? -1 : parseInt(ch, 36); }
    this.remaining = new Int32Array(this.colorCount);
    for (let i = 0; i < this.N; i++) if (this.cell[i] >= 0) this.remaining[this.cell[i]]++;
    this.totalRemaining = this.remaining.reduce((a, b) => a + b, 0);
    this.totalVoxels = this.totalRemaining;
    this.columns = level.columns.map((col) => col.map(([c, n]) => ({ c, n })));
    this.launchers = new Array(LAUNCHERS).fill(null);
    this.moves = 0; this.score = 0; this.maxCombo = 0; this.rounds = 0;
    this.buildPriority();
    this.exposed = new Uint8Array(this.N);
    this.exposedCnt = new Int32Array(this.colorCount);
    this.heaps = Array.from({ length: this.colorCount }, () => new MinHeap(this.rank));
    for (let i = 0; i < this.N; i++) if (this.cell[i] >= 0 && this.isExposedCalc(i)) this.expose(i);
  }
  idx(x, y, z) { return x + this.X * (y + this.Y * z); }
  isExposedCalc(i) {
    const x = i % this.X, y = Math.floor(i / this.X) % this.Y, z = Math.floor(i / (this.X * this.Y));
    for (const [a, b, c] of [[x - 1, y, z], [x + 1, y, z], [x, y - 1, z], [x, y + 1, z], [x, y, z - 1], [x, y, z + 1]]) {
      if (a < 0 || b < 0 || c < 0 || a >= this.X || b >= this.Y || c >= this.Z) return true;
      if (this.cell[this.idx(a, b, c)] < 0) return true;
    }
    return false;
  }
  // Removal priority: highest y first, then farthest from the vertical axis, then lowest index.
  buildPriority() {
    const keys = [];
    for (let i = 0; i < this.N; i++) if (this.cell[i] >= 0) {
      const x = i % this.X, y = Math.floor(i / this.X) % this.Y, z = Math.floor(i / (this.X * this.Y));
      const dx = 2 * x - (this.X - 1), dz = 2 * z - (this.Z - 1);
      keys.push([i, y, dx * dx + dz * dz]);
    }
    keys.sort((a, b) => (b[1] - a[1]) || (b[2] - a[2]) || (a[0] - b[0]));
    this.rank = new Int32Array(this.N).fill(0x7fffffff);
    keys.forEach((k, r) => (this.rank[k[0]] = r));
  }
  expose(i) { if (this.exposed[i]) return; this.exposed[i] = 1; const c = this.cell[i]; this.exposedCnt[c]++; this.heaps[c].push(i); }
  exposedCount(c) { return this.exposedCnt[c]; }
  // best exposed voxel of a colour (lazy deletion of destroyed entries)
  target(c) { const h = this.heaps[c]; while (h.size && (this.cell[h.peek()] !== c || !this.exposed[h.peek()])) h.pop(); return h.size ? h.peek() : -1; }
  destroy(i) {
    const c = this.cell[i];
    this.cell[i] = -1; if (this.exposed[i]) { this.exposed[i] = 0; this.exposedCnt[c]--; } this.remaining[c]--; this.totalRemaining--;
    const x = i % this.X, y = Math.floor(i / this.X) % this.Y, z = Math.floor(i / (this.X * this.Y));
    for (const [a, b, d] of [[x - 1, y, z], [x + 1, y, z], [x, y - 1, z], [x, y + 1, z], [x, y, z - 1], [x, y, z + 1]]) {
      if (a < 0 || b < 0 || d < 0 || a >= this.X || b >= this.Y || d >= this.Z) continue;
      const j = this.idx(a, b, d); if (this.cell[j] >= 0) this.expose(j);
    }
  }
  freeLauncher() { return this.launchers.indexOf(null); }
  topOf(col) { const c = this.columns[col]; return c.length ? c[0] : null; }
  canTap(col) { return !this.isWon() && col >= 0 && col < COLUMNS && this.columns[col].length > 0 && this.freeLauncher() >= 0; }
  tap(col) {
    if (!this.canTap(col)) return false;
    this.launchers[this.freeLauncher()] = this.columns[col].shift();
    this.moves++;
    return true;
  }
  canFire(li) { const L = this.launchers[li]; return !!L && (this.remaining[L.c] === 0 || this.exposedCnt[L.c] > 0); }
  // One firing round. Returns { shots: [[launcher, voxel]], freed: [launcher], dropped: [voxel], wasted } or null if nothing happened.
  step() {
    const shots = [], freed = [];
    let wasted = 0;
    for (let li = 0; li < LAUNCHERS; li++) {
      const L = this.launchers[li]; if (!L) continue;
      if (this.remaining[L.c] === 0) { wasted += L.n; this.launchers[li] = null; freed.push(li); continue; }
      const v = this.target(L.c); if (v < 0) continue;
      this.destroy(v); shots.push([li, v]); L.n--;
      if (L.n === 0) { this.launchers[li] = null; freed.push(li); }
    }
    if (!shots.length && !freed.length) return null;
    this.rounds++;
    const combo = shots.length;
    this.maxCombo = Math.max(this.maxCombo, combo);
    this.score += combo * 10 * Math.max(1, combo);
    const dropped = shots.length ? this.collapse() : [];
    this.purgeFinished();
    return { shots, freed, dropped, wasted, combo };
  }
  // Blocks of finished colours are removed from the columns (they could never fire).
  purgeFinished() { for (let k = 0; k < COLUMNS; k++) this.columns[k] = this.columns[k].filter((b) => this.remaining[b.c] > 0); }
  // Runs rounds until a launcher is free (the player may tap) or nothing can happen. Used by solver/witness replay.
  settle() { let r; while (this.freeLauncher() < 0 && !this.isWon() && (r = this.step())); }
  resolveAll() { let r; while (!this.isWon() && (r = this.step())); }
  // Structural rule: the largest connected piece stays together with every piece touching y = 0;
  // any other detached piece falls and counts as destroyed. Ties: piece containing the lowest index.
  collapse() {
    const N = this.N, X = this.X, Y = this.Y, Z = this.Z, comp = new Int32Array(N).fill(-1);
    const sizes = [], grounded = [], members = [];
    for (let i = 0; i < N; i++) {
      if (this.cell[i] < 0 || comp[i] >= 0) continue;
      const id = sizes.length, list = [i]; comp[i] = id; let g = false;
      for (let h = 0; h < list.length; h++) {
        const j = list[h], x = j % X, y = Math.floor(j / X) % Y, z = Math.floor(j / (X * Y));
        if (y === 0) g = true;
        if (x > 0 && this.cell[j - 1] >= 0 && comp[j - 1] < 0) { comp[j - 1] = id; list.push(j - 1); }
        if (x < X - 1 && this.cell[j + 1] >= 0 && comp[j + 1] < 0) { comp[j + 1] = id; list.push(j + 1); }
        if (y > 0 && this.cell[j - X] >= 0 && comp[j - X] < 0) { comp[j - X] = id; list.push(j - X); }
        if (y < Y - 1 && this.cell[j + X] >= 0 && comp[j + X] < 0) { comp[j + X] = id; list.push(j + X); }
        if (z > 0 && this.cell[j - X * Y] >= 0 && comp[j - X * Y] < 0) { comp[j - X * Y] = id; list.push(j - X * Y); }
        if (z < Z - 1 && this.cell[j + X * Y] >= 0 && comp[j + X * Y] < 0) { comp[j + X * Y] = id; list.push(j + X * Y); }
      }
      sizes.push(list.length); grounded.push(g); members.push(list);
    }
    if (sizes.length <= 1) return [];
    let main = 0; for (let k = 1; k < sizes.length; k++) if (sizes[k] > sizes[main]) main = k;
    const dropped = [];
    for (let k = 0; k < sizes.length; k++) {
      if (k === main || grounded[k]) continue;
      for (const i of members[k].slice().sort((a, b) => a - b)) { this.destroy(i); dropped.push(i); }
    }
    return dropped;
  }
  isWon() { return this.totalRemaining === 0; }
  boardEmpty() { return this.columns.every((c) => c.length === 0); }
  anyCanFire() { for (let li = 0; li < LAUNCHERS; li++) if (this.canFire(li)) return true; return false; }
  // Valid once settled/resolved.
  failReason() {
    if (this.isWon()) return null;
    if (this.freeLauncher() < 0 && !this.anyCanFire()) return 'jammed';
    if (this.boardEmpty() && !this.anyCanFire()) return 'out_of_blocks';
    return null;
  }
}

// Heuristic witness solver: tap the column whose top colour is most reachable. Not exhaustive: a win proves
// the level solvable (the column list is a witness); a loss only means this heuristic did not find a solution.
export function bestColumn(s) {
  let best = -1, bestScore = -Infinity;
  for (let col = 0; col < COLUMNS; col++) {
    const b = s.topOf(col); if (!b) continue;
    const exp = s.exposedCount(b.c);
    let pending = 0; for (const L of s.launchers) if (L && L.c === b.c) pending += L.n;
    const need = Math.max(0, s.remaining[b.c] - pending);
    const sc = (exp > 0 ? 1000 : 0) + Math.min(exp, b.n) * 2 + (need > 0 ? 50 : -500) - (s.columns[col].length > 1 && s.exposedCount(s.columns[col][1].c) > 0 ? 0 : 5);
    if (sc > bestScore) { bestScore = sc; best = col; }
  }
  return best;
}

export function solveGreedy(level, maxMoves = 1000) {
  const s = new Session(level);
  const taps = [];
  while (!s.isWon() && taps.length < maxMoves) {
    s.settle();
    if (s.isWon() || s.failReason()) break;
    if (s.freeLauncher() < 0) break;
    const col = bestColumn(s);
    if (col < 0) { s.resolveAll(); break; }
    s.tap(col); taps.push(col);
  }
  if (!s.isWon()) s.resolveAll();
  return { won: s.isWon(), moves: s.moves, taps, score: s.score, maxCombo: s.maxCombo, fail: s.failReason(), remaining: s.totalRemaining };
}

/** Replays a witness exactly like the C# test: tap, settle, ..., then resolve. */
export function replay(level, taps) {
  const s = new Session(level);
  for (const col of taps) { s.settle(); if (!s.tap(col)) return { ok: false, s }; }
  s.resolveAll();
  return { ok: true, s };
}

// Monte-Carlo difficulty probe: casual player taps a random column when a launcher is free,
// preferring columns whose top colour is reachable with probability `skill`.
export function randomWinRate(level, runs, seed, skill = 0.6) {
  let wins = 0, a = seed >>> 0;
  const r = () => { a = (a + 0x6D2B79F5) >>> 0; let t = a; t = Math.imul(t ^ (t >>> 15), t | 1); t ^= t + Math.imul(t ^ (t >>> 7), t | 61); return ((t ^ (t >>> 14)) >>> 0) / 4294967296; };
  for (let k = 0; k < runs; k++) {
    const s = new Session(level);
    for (let guard = 0; guard < 2000 && !s.isWon(); guard++) {
      s.settle();
      if (s.isWon() || s.failReason() || s.freeLauncher() < 0) break;
      const cols = []; for (let c = 0; c < COLUMNS; c++) if (s.columns[c].length) cols.push(c);
      if (!cols.length) { s.resolveAll(); break; }
      let pick = cols[Math.floor(r() * cols.length)];
      if (r() < skill) { const good = cols.filter((c) => s.exposedCount(s.columns[c][0].c) > 0); if (good.length) pick = good[Math.floor(r() * good.length)]; }
      s.tap(pick);
    }
    if (!s.isWon()) s.resolveAll();
    if (s.isWon()) wins++;
  }
  return wins / runs;
}
