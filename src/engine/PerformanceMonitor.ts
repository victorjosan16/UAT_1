export type QualityLevel = "HIGH" | "MEDIUM" | "LOW";

const HISTORY_SIZE = 60;
const DOWNGRADE_FRAME_MS = 1000 / 45; // sustained sub-45fps
const UPGRADE_FRAME_MS = 1000 / 58;

/**
 * Rolling average frame-time monitor. Effects (particle counts, trail
 * density, glow, parallax layers) read `quality` and scale themselves
 * down under sustained pressure — gameplay physics never changes based on
 * this, only presentation (see docs/GAME_DESIGN.md — determinism first).
 */
export class PerformanceMonitor {
  private samples: number[] = [];
  private cursor = 0;
  private filled = false;
  private _quality: QualityLevel = "HIGH";
  private framesSinceChange = 0;

  recordFrame(frameMs: number): void {
    this.samples[this.cursor] = frameMs;
    this.cursor = (this.cursor + 1) % HISTORY_SIZE;
    if (this.cursor === 0) this.filled = true;
    this.framesSinceChange += 1;

    if (!this.filled || this.framesSinceChange < HISTORY_SIZE) return;

    const avg = this.average();
    if (avg > DOWNGRADE_FRAME_MS && this._quality !== "LOW") {
      this._quality = this._quality === "HIGH" ? "MEDIUM" : "LOW";
      this.framesSinceChange = 0;
    } else if (avg < UPGRADE_FRAME_MS && this._quality !== "HIGH") {
      this._quality = this._quality === "LOW" ? "MEDIUM" : "HIGH";
      this.framesSinceChange = 0;
    }
  }

  private average(): number {
    let sum = 0;
    for (const s of this.samples) sum += s;
    return sum / this.samples.length;
  }

  get quality(): QualityLevel {
    return this._quality;
  }

  get particleScale(): number {
    return this._quality === "HIGH" ? 1 : this._quality === "MEDIUM" ? 0.6 : 0.3;
  }

  get trailEnabled(): boolean {
    return this._quality !== "LOW";
  }

  get parallaxLayers(): number {
    return this._quality === "HIGH" ? 2 : this._quality === "MEDIUM" ? 1 : 0;
  }

  get glowEnabled(): boolean {
    return this._quality !== "LOW";
  }
}
