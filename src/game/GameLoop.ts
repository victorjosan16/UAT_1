const FIXED_STEP_SECONDS = 1 / 120;
const MAX_FRAME_SECONDS = 0.25;

export type UpdateFn = (dtSeconds: number) => void;
export type RenderFn = (alpha: number, frameDtSeconds: number) => void;

/**
 * Fixed-step simulation decoupled from variable-rate rendering, so physics
 * stay consistent across device refresh rates while rendering still runs
 * every rAF tick for smoothness.
 */
export class GameLoop {
  private rafHandle: number | null = null;
  private lastTimeMs = 0;
  private accumulatorSeconds = 0;
  private running = false;

  constructor(
    private readonly update: UpdateFn,
    private readonly render: RenderFn,
  ) {}

  start(): void {
    if (this.running) return;
    this.running = true;
    this.lastTimeMs = performance.now();
    this.accumulatorSeconds = 0;
    this.rafHandle = requestAnimationFrame(this.tick);
  }

  stop(): void {
    this.running = false;
    if (this.rafHandle !== null) cancelAnimationFrame(this.rafHandle);
    this.rafHandle = null;
  }

  private readonly tick = (nowMs: number): void => {
    if (!this.running) return;
    const frameDtSeconds = Math.min(MAX_FRAME_SECONDS, (nowMs - this.lastTimeMs) / 1000);
    this.lastTimeMs = nowMs;
    this.accumulatorSeconds += frameDtSeconds;

    while (this.accumulatorSeconds >= FIXED_STEP_SECONDS) {
      this.update(FIXED_STEP_SECONDS);
      this.accumulatorSeconds -= FIXED_STEP_SECONDS;
    }

    this.render(this.accumulatorSeconds / FIXED_STEP_SECONDS, frameDtSeconds);
    this.rafHandle = requestAnimationFrame(this.tick);
  };
}
