import type { Interval } from "@/types";

export interface FallingPieceOptions extends Interval {
  y: number;
  height: number;
  /** Initial horizontal drift, world units/sec. */
  vx?: number;
}

/**
 * A cut-away fragment. Purely cosmetic — never affects gameplay state —
 * with simple constant-gravity kinematics for a satisfying fall + tumble.
 */
export class FallingPiece {
  left: number;
  right: number;
  y: number;
  readonly height: number;
  vx: number;
  vy = 0;
  rotation = 0;
  angularVelocity: number;
  ageMs = 0;
  readonly maxAgeMs = 1400;

  private static readonly GRAVITY = 2600; // world units / s^2

  constructor(options: FallingPieceOptions) {
    this.left = options.left;
    this.right = options.right;
    this.y = options.y;
    this.height = options.height;
    this.vx = options.vx ?? 0;
    this.angularVelocity = (options.vx ?? 0) * 0.004 + (Math.random() - 0.5) * 1.2;
  }

  get width(): number {
    return this.right - this.left;
  }

  get isExpired(): boolean {
    return this.ageMs >= this.maxAgeMs;
  }

  update(dtSeconds: number): void {
    this.vy += FallingPiece.GRAVITY * dtSeconds;
    this.left += this.vx * dtSeconds;
    this.right += this.vx * dtSeconds;
    this.y -= this.vy * dtSeconds;
    this.rotation += this.angularVelocity * dtSeconds;
    this.ageMs += dtSeconds * 1000;
  }
}
