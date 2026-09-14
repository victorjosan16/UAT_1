import { prefersReducedMotion } from "@/utils/motion";

interface Envelope {
  scale: number;
  downMs: number;
  holdMs: number;
  upMs: number;
  elapsedMs: number;
}

/**
 * Centralized time-scale envelope for short cinematic slow-motion beats
 * (big Perfect streaks, new records, game over). Never touched via
 * scattered setTimeouts — callers call `trigger(...)` once, and every
 * consumer (block motion, particles, trail) reads `.value` each tick.
 * Advances on real elapsed time, so the effect's own duration is never
 * warped by the slow-down it produces.
 */
export class TimeScaleManager {
  private envelope: Envelope | null = null;
  private current = 1;

  trigger(scale: number, downMs: number, holdMs: number, upMs: number): void {
    if (prefersReducedMotion()) return; // no slow-mo when motion is reduced — gameplay speed stays constant
    // A more dramatic (lower-scale or longer) effect always wins over a smaller one already playing.
    if (this.envelope) {
      const totalNew = downMs + holdMs + upMs;
      const remainingOld = this.envelope.downMs + this.envelope.holdMs + this.envelope.upMs - this.envelope.elapsedMs;
      if (scale >= this.envelope.scale && totalNew <= remainingOld) return;
    }
    this.envelope = { scale, downMs, holdMs, upMs, elapsedMs: 0 };
  }

  update(realDtSeconds: number): void {
    if (!this.envelope) {
      this.current = 1;
      return;
    }
    const e = this.envelope;
    e.elapsedMs += realDtSeconds * 1000;
    const total = e.downMs + e.holdMs + e.upMs;

    if (e.elapsedMs >= total) {
      this.envelope = null;
      this.current = 1;
      return;
    }

    if (e.elapsedMs < e.downMs) {
      const t = e.elapsedMs / e.downMs;
      this.current = 1 - (1 - e.scale) * easeOutCubic(t);
    } else if (e.elapsedMs < e.downMs + e.holdMs) {
      this.current = e.scale;
    } else {
      const t = (e.elapsedMs - e.downMs - e.holdMs) / e.upMs;
      this.current = e.scale + (1 - e.scale) * easeInCubic(t);
    }
  }

  get value(): number {
    return this.current;
  }
}

function easeOutCubic(t: number): number {
  return 1 - Math.pow(1 - t, 3);
}

function easeInCubic(t: number): number {
  return t * t * t;
}
