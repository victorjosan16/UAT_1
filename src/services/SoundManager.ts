import { LocalStorageService } from "@/storage/LocalStorage";

/**
 * Every sound is synthesized on the fly with the Web Audio API — no audio
 * asset files to license, host, or bundle. Lazily creates its AudioContext
 * on first use (browsers require a user gesture first anyway, and every
 * call site here is already inside a tap handler).
 */
class SoundManager {
  private ctx: AudioContext | null = null;

  private ensureContext(): AudioContext | null {
    if (typeof window === "undefined") return null;
    const Ctor = window.AudioContext ?? (window as unknown as { webkitAudioContext?: typeof AudioContext }).webkitAudioContext;
    if (!Ctor) return null;
    if (!this.ctx) this.ctx = new Ctor();
    if (this.ctx.state === "suspended") void this.ctx.resume();
    return this.ctx;
  }

  private isEnabled(): boolean {
    try {
      return LocalStorageService.getPreferences().soundEnabled;
    } catch {
      return true;
    }
  }

  /** One tone: a short envelope (fast attack, exponential decay) so notes never click or overlap harshly. */
  private tone(freq: number, startOffsetS: number, durationS: number, type: OscillatorType = "sine", peakGain = 0.16): void {
    const ctx = this.ensureContext();
    if (!ctx) return;

    const osc = ctx.createOscillator();
    const gain = ctx.createGain();
    osc.type = type;
    osc.frequency.value = freq;

    const startAt = ctx.currentTime + startOffsetS;
    gain.gain.setValueAtTime(0, startAt);
    gain.gain.linearRampToValueAtTime(peakGain, startAt + 0.012);
    gain.gain.exponentialRampToValueAtTime(0.001, startAt + durationS);

    osc.connect(gain);
    gain.connect(ctx.destination);
    osc.start(startAt);
    osc.stop(startAt + durationS + 0.02);
  }

  playCorrect(): void {
    if (!this.isEnabled()) return;
    this.tone(660, 0, 0.1);
    this.tone(990, 0.08, 0.16);
  }

  playWrong(): void {
    if (!this.isEnabled()) return;
    this.tone(220, 0, 0.22, "sawtooth", 0.1);
  }

  playStreak(): void {
    if (!this.isEnabled()) return;
    this.tone(523, 0, 0.09);
    this.tone(659, 0.07, 0.09);
    this.tone(784, 0.14, 0.18);
  }

  playComplete(perfect: boolean): void {
    if (!this.isEnabled()) return;
    const notes = perfect ? [523, 659, 784, 1046] : [523, 659, 784];
    notes.forEach((freq, i) => this.tone(freq, i * 0.1, 0.22));
  }

  playCountdownTick(): void {
    if (!this.isEnabled()) return;
    this.tone(440, 0, 0.05, "square", 0.08);
  }

  /** Reserved for a genuine leaderboard milestone (Top 100/50/10/#1) — a bigger, longer fanfare than playComplete, since this fires at most once per milestone ever. */
  playMilestone(): void {
    if (!this.isEnabled()) return;
    [523, 659, 784, 1046, 1318].forEach((freq, i) => this.tone(freq, i * 0.09, 0.3, "triangle", 0.2));
  }
}

export const soundManager = new SoundManager();
