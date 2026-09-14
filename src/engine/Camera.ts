import { damp } from "@/utils/math";

/**
 * Smooth vertical follow camera, decoupled from gameplay math. `targetY`
 * is set by the game after each placement; `update` eases toward it every
 * frame so movement never snaps or jumps.
 */
export class Camera {
  y = 0;
  targetY = 0;
  private readonly smoothing: number;

  constructor(smoothing = 0.0001) {
    this.smoothing = smoothing;
  }

  setTarget(y: number): void {
    this.targetY = y;
  }

  update(dtSeconds: number): void {
    this.y = damp(this.y, this.targetY, this.smoothing, dtSeconds);
    if (Math.abs(this.y - this.targetY) < 0.05) this.y = this.targetY;
  }

  reset(y = 0): void {
    this.y = y;
    this.targetY = y;
  }
}
