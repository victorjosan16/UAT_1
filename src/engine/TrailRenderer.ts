import { VFX_CONFIG } from "@/game/VFXConfig";
import { clamp } from "@/utils/math";

interface TrailSample {
  left: number;
  right: number;
  y: number;
  height: number;
  ageMs: number;
  active: boolean;
}

/**
 * Subtle motion trail behind the moving block, intensity scaling with
 * speed. Fixed-size preallocated sample pool — no per-frame allocation.
 * Speed below `minSpeedForTrail` records nothing; `clear()` (called on
 * every placement) makes it vanish immediately once the block stops.
 */
export class TrailRenderer {
  private readonly samples: TrailSample[];
  private msSinceLastSample = Infinity;

  constructor() {
    this.samples = Array.from({ length: VFX_CONFIG.trail.maxSamples }, () => ({ left: 0, right: 0, y: 0, height: 0, ageMs: 0, active: false }));
  }

  record(left: number, right: number, y: number, height: number, speed: number, dtSeconds: number): void {
    this.msSinceLastSample += dtSeconds * 1000;
    if (speed < VFX_CONFIG.trail.minSpeedForTrail) return;
    if (this.msSinceLastSample < VFX_CONFIG.trail.sampleIntervalMs) return;
    this.msSinceLastSample = 0;

    // Reuse the oldest slot (ring buffer) instead of allocating.
    let oldestIndex = 0;
    let oldestAge = -1;
    for (let i = 0; i < this.samples.length; i++) {
      const s = this.samples[i];
      if (!s) continue;
      if (!s.active) {
        oldestIndex = i;
        oldestAge = Infinity;
        break;
      }
      if (s.ageMs > oldestAge) {
        oldestAge = s.ageMs;
        oldestIndex = i;
      }
    }
    const slot = this.samples[oldestIndex];
    if (!slot) return;
    slot.left = left;
    slot.right = right;
    slot.y = y;
    slot.height = height;
    slot.ageMs = 0;
    slot.active = true;
  }

  update(dtSeconds: number): void {
    for (const s of this.samples) {
      if (!s.active) continue;
      s.ageMs += dtSeconds * 1000;
      if (s.ageMs >= VFX_CONFIG.trail.maxAgeMs) s.active = false;
    }
  }

  clear(): void {
    for (const s of this.samples) s.active = false;
    this.msSinceLastSample = Infinity;
  }

  render(ctx: CanvasRenderingContext2D, worldToScreenX: (x: number) => number, worldToScreenY: (y: number) => number, color: string, qualityScale: number): void {
    if (qualityScale <= 0) return;
    for (const s of this.samples) {
      if (!s.active) continue;
      const lifeFrac = 1 - s.ageMs / VFX_CONFIG.trail.maxAgeMs;
      const alpha = clamp(lifeFrac * 0.35 * qualityScale, 0, 0.35);
      if (alpha <= 0.01) continue;
      const x = worldToScreenX(s.left);
      const width = s.right - s.left;
      const yTop = worldToScreenY(s.y + s.height);
      const yBottom = worldToScreenY(s.y);
      ctx.globalAlpha = alpha;
      ctx.fillStyle = color;
      ctx.fillRect(x, yTop, width, yBottom - yTop);
    }
    ctx.globalAlpha = 1;
  }
}
