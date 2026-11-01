// Reference implementation of the Voxel Bloom 3D rules (mirrored 1:1 by C# VoxelBloom.Core.GameSession).
// Any change here must be reflected in Assets/VoxelBloom/Scripts/Core/Gameplay/GameSession.cs.
export const BOARD_COLS = 5, BOARD_ROWS = 4, BOARD_SIZE = 20;

export class Session {
  constructor(level) {
    const [X, Y, Z] = level.size;
    this.X = X; this.Y = Y; this.Z = Z; this.N = X * Y * Z;
    this.queueSize = level.queueSize;
    this.moveLimit = level.moveLimit || 1000000;
    this.colorCount = level.palette.length;
    this.cell = new Int8Array(this.N);
    for (let i = 0; i < this.N; i++) { const ch = level.voxels[i]; this.cell[i] = ch === '.' ? -1 : parseInt(ch, 36); }
    this.remaining = new Int32Array(this.colorCount);
    for (let i = 0; i < this.N; i++) if (this.cell[i] >= 0) this.remaining[this.cell[i]]++;
    this.totalRemaining = this.remaining.reduce((a, b) => a + b, 0);
    this.totalVoxels = this.totalRemaining;
    this.deck = level.deck.map(([c, p]) => ({ c, p }));
    this.deckIndex = 0;
    this.board = new Array(BOARD_SIZE).fill(null);
    for (let s = 0; s < BOARD_SIZE; s++) this.board[s] = this.drawFromDeck();
    this.queue = [];
    this.moves = 0; this.combo = 0; this.maxCombo = 0; this.score = 0;
    this.order = this.buildPriority();
    this.exposed = new Uint8Array(this.N);
    this.exposedCnt = new Int32Array(this.colorCount);
    for (let i = 0; i < this.N; i++) if (this.cell[i] >= 0 && this.isExposedCalc(i)) { this.exposed[i] = 1; this.exposedCnt[this.cell[i]]++; }
  }
  idx(x, y, z) { return x + this.X * (y + this.Y * z); }
  isExposedCalc(i) {
    const x = i % this.X, y = Math.floor(i / this.X) % this.Y, z = Math.floor(i / (this.X * this.Y));
    const n = [[x - 1, y, z], [x + 1, y, z], [x, y - 1, z], [x, y + 1, z], [x, y, z - 1], [x, y, z + 1]];
    for (const [a, b, c] of n) {
      if (a < 0 || b < 0 || c < 0 || a >= this.X || b >= this.Y || c >= this.Z) return true;
      if (this.cell[this.idx(a, b, c)] < 0) return true;
    }
    return false;
  }
  // Removal priority: highest y first, then farthest from the vertical axis, then lowest index.
  buildPriority() {
    const cx = (this.X - 1) / 2, cz = (this.Z - 1) / 2, keys = [];
    for (let i = 0; i < this.N; i++) if (this.cell[i] >= 0) {
      const x = i % this.X, y = Math.floor(i / this.X) % this.Y, z = Math.floor(i / (this.X * this.Y));
      const d2 = Math.round(((x - cx) * (x - cx) + (z - cz) * (z - cz)) * 4); // integer, deterministic
      keys.push([i, y, d2]);
    }
    keys.sort((a, b) => (b[1] - a[1]) || (b[2] - a[2]) || (a[0] - b[0]));
    const order = keys.map((k) => k[0]);
    const rank = new Int32Array(this.N).fill(-1); order.forEach((v, r) => (rank[v] = r));
    this.rank = rank;
    return order;
  }
  drawFromDeck() {
    while (this.deckIndex < this.deck.length) {
      const b = this.deck[this.deckIndex++];
      if (!this.remaining || this.remaining[b.c] > 0) return { c: b.c, p: b.p };
    }
    return null;
  }
  exposedCount(c) { return this.exposedCnt[c]; }
  canTap(slot) { return !this.isWon() && this.moves < this.moveLimit && this.board[slot] !== null && this.queue.length < this.queueSize; }
  tap(slot) {
    if (!this.canTap(slot)) return false;
    this.queue.push(this.board[slot]);
    this.board[slot] = this.drawFromDeck();
    this.moves++;
    return true;
  }
  destroy(i) {
    const c = this.cell[i];
    this.cell[i] = -1; this.exposed[i] = 0; this.exposedCnt[c]--; this.remaining[c]--; this.totalRemaining--;
    const x = i % this.X, y = Math.floor(i / this.X) % this.Y, z = Math.floor(i / (this.X * this.Y));
    for (const [a, b, d] of [[x - 1, y, z], [x + 1, y, z], [x, y - 1, z], [x, y + 1, z], [x, y, z - 1], [x, y, z + 1]]) {
      if (a < 0 || b < 0 || d < 0 || a >= this.X || b >= this.Y || d >= this.Z) continue;
      const j = this.idx(a, b, d); if (this.cell[j] >= 0 && !this.exposed[j]) { this.exposed[j] = 1; this.exposedCnt[this.cell[j]]++; }
    }
  }
  // Fires the first queued attack that has a target. Returns a result or null when nothing can fire.
  resolveNext() {
    // attacks whose colour is already finished are discarded (consumed, breaks the combo)
    for (let q = 0; q < this.queue.length; q++) if (this.remaining[this.queue[q].c] === 0) {
      const a = this.queue.splice(q, 1)[0]; this.combo = 0; return { attack: a, destroyed: [], wasted: true, colorCleared: false, combo: 0 };
    }
    let qi = -1;
    for (let q = 0; q < this.queue.length; q++) if (this.exposedCount(this.queue[q].c) > 0) { qi = q; break; }
    if (qi < 0) return null;
    const a = this.queue.splice(qi, 1)[0];
    // single wave: only voxels accessible at the moment of firing; exposure is recomputed afterwards
    const wave = [];
    for (let i = 0; i < this.N; i++) if (this.exposed[i] && this.cell[i] === a.c) wave.push(i);
    wave.sort((u, v) => this.rank[u] - this.rank[v]);
    const destroyed = wave.slice(0, a.p);
    for (const i of destroyed) this.destroy(i);
    const full = destroyed.length === a.p;
    this.combo = full ? this.combo + 1 : 0;
    this.maxCombo = Math.max(this.maxCombo, this.combo);
    const mult = Math.min(Math.max(this.combo, 1), 5);
    this.score += destroyed.length * 10 * mult;
    let colorCleared = false;
    if (this.remaining[a.c] === 0) {
      colorCleared = true;
      for (let s = 0; s < BOARD_SIZE; s++) if (this.board[s] && this.board[s].c === a.c) this.board[s] = this.drawFromDeck();
    }
    return { attack: a, destroyed, wasted: false, colorCleared, combo: this.combo };
  }
  resolveAll() { const out = []; let r; while ((r = this.resolveNext())) { out.push(r); if (this.isWon()) break; } return out; }
  isWon() { return this.totalRemaining === 0; }
  boardEmpty() { return this.board.every((b) => b === null); }
  // Only valid after resolveAll(): nothing in the queue can fire.
  failReason() {
    if (this.isWon()) return null;
    if (this.queue.length >= this.queueSize) return 'queue_full';
    if (this.moves >= this.moveLimit) return 'out_of_moves';
    if (this.boardEmpty()) return 'out_of_blocks';
    return null;
  }
}

// Heuristic witness solver: picks the tap that maximises immediately useful destruction.
// It is NOT exhaustive: success proves the level solvable (the move list is a witness),
// failure only means this heuristic did not find a solution.
export function solveGreedy(level, maxMoves = 1000) {
  const s = new Session(level);
  const taps = [];
  while (!s.isWon() && taps.length < maxMoves) {
    let best = -1, bestScore = -Infinity;
    for (let slot = 0; slot < BOARD_SIZE; slot++) {
      const b = s.board[slot]; if (!b || s.queue.length >= s.queueSize) continue;
      const exp = s.exposedCount(b.c);
      const pending = s.queue.filter((q) => q.c === b.c).reduce((a, q) => a + q.p, 0);
      const useful = Math.min(b.p, Math.max(0, s.remaining[b.c] - pending));
      let sc = (exp > 0 ? 1000 : 0) + useful * 4 - Math.max(0, b.p - s.remaining[b.c]) * 2 + (exp > 0 ? Math.min(exp, b.p) : -50);
      if (sc > bestScore) { bestScore = sc; best = slot; }
    }
    if (best < 0) break;
    s.tap(best); taps.push(best);
    s.resolveAll();
    if (s.failReason()) break;
  }
  return { won: s.isWon(), moves: s.moves, taps, score: s.score, maxCombo: s.maxCombo, fail: s.failReason(), remaining: s.totalRemaining };
}

// Monte-Carlo difficulty probe: share of games won by a player tapping random blocks
// that prefers colours with exposed voxels with probability `skill`.
export function randomWinRate(level, runs, seed, skill = 0.6) {
  let wins = 0, a = seed >>> 0;
  const r = () => { a = (a + 0x6D2B79F5) >>> 0; let t = a; t = Math.imul(t ^ (t >>> 15), t | 1); t ^= t + Math.imul(t ^ (t >>> 7), t | 61); return ((t ^ (t >>> 14)) >>> 0) / 4294967296; };
  for (let k = 0; k < runs; k++) {
    const s = new Session(level);
    let guard = 0;
    while (!s.isWon() && !s.failReason() && guard++ < 2000) {
      const slots = []; for (let i = 0; i < 20; i++) if (s.board[i]) slots.push(i);
      if (!slots.length || s.queue.length >= s.queueSize) break;
      let pick = slots[Math.floor(r() * slots.length)];
      if (r() < skill) { const good = slots.filter((i) => s.exposedCount(s.board[i].c) > 0); if (good.length) pick = good[Math.floor(r() * good.length)]; }
      s.tap(pick); s.resolveAll();
    }
    if (s.isWon()) wins++;
  }
  return wins / runs;
}
