/**
 * Fully procedural sound design via the Web Audio API — no binary audio
 * assets to fetch or bundle, so the game works offline immediately and the
 * bundle stays tiny. Handles the autoplay-restriction dance: the
 * AudioContext is created lazily on the first user gesture.
 */
export class AudioManager {
  private ctx: AudioContext | null = null;
  private enabled = true;

  setEnabled(enabled: boolean): void {
    this.enabled = enabled;
  }

  isEnabled(): boolean {
    return this.enabled;
  }

  /** Call from within a user-gesture handler (e.g. the first tap). */
  resume(): void {
    if (!this.ctx) {
      const Ctor = window.AudioContext ?? (window as unknown as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
      if (!Ctor) return;
      this.ctx = new Ctor();
    }
    if (this.ctx.state === "suspended") void this.ctx.resume();
  }

  private tone(frequency: number, durationMs: number, type: OscillatorType, gainPeak: number, delayMs = 0): void {
    if (!this.enabled || !this.ctx) return;
    const ctx = this.ctx;
    const start = ctx.currentTime + delayMs / 1000;
    const osc = ctx.createOscillator();
    const gain = ctx.createGain();
    osc.type = type;
    osc.frequency.setValueAtTime(frequency, start);
    gain.gain.setValueAtTime(0, start);
    gain.gain.linearRampToValueAtTime(gainPeak, start + 0.01);
    gain.gain.exponentialRampToValueAtTime(0.001, start + durationMs / 1000);
    osc.connect(gain).connect(ctx.destination);
    osc.start(start);
    osc.stop(start + durationMs / 1000 + 0.02);
  }

  /** Instant tactile click on input registration — separate from the placement outcome sound below, for extra responsiveness. */
  tap(): void {
    this.tone(880, 26, "square", 0.05);
  }

  place(): void {
    // Low percussive "thump" layered under the melodic tone — gives every
    // landing a felt, physical weight, not just a musical blip.
    this.tone(95, 90, "sine", 0.16);
    this.tone(220, 70, "sine", 0.18);
  }

  cutFall(): void {
    this.tone(160, 140, "sawtooth", 0.06);
  }

  /** Subtle whoosh on block spawn; pitch/volume scale gently with the block's speed. */
  spawnWhoosh(speedFactor: number): void {
    const clampedFactor = Math.min(1, Math.max(0, speedFactor));
    this.tone(140 + clampedFactor * 60, 90, "sine", 0.03 + clampedFactor * 0.03);
  }

  nearMiss(): void {
    this.tone(700, 90, "square", 0.1);
    this.tone(500, 140, "square", 0.07, 50);
  }

  milestone(): void {
    [0, 80, 160].forEach((delay, i) => this.tone(600 + i * 140, 140, "sine", 0.16, delay));
  }

  perfect(streak: number): void {
    const base = 520;
    const freq = base + Math.min(streak, 10) * 34;
    this.tone(freq, 160, "triangle", 0.22);
    this.tone(freq * 1.5, 120, "sine", 0.12, 40);
  }

  combo(multiplier: number): void {
    this.tone(300 + multiplier * 20, 90, "square", 0.08);
  }

  levelComplete(): void {
    [0, 90, 180].forEach((delay, i) => this.tone(440 + i * 110, 160, "triangle", 0.2, delay));
  }

  newRecord(): void {
    [0, 100, 200, 300].forEach((delay, i) => this.tone(500 + i * 90, 180, "triangle", 0.22, delay));
  }

  gameOver(): void {
    this.tone(180, 260, "sawtooth", 0.16);
    this.tone(90, 380, "sawtooth", 0.14, 90);
  }
}
