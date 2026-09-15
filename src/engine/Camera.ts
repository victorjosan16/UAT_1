import { clamp } from "@/utils/math";

const EASE_ZONE = 140; // world units within which the camera decelerates into the target
const MIN_EASE_FACTOR = 0.18;
const PUNCH_STIFFNESS = 170;
const PUNCH_DAMPING = 15;

/**
 * Vertical follow camera, decoupled from gameplay math. `targetY` is set
 * by the game after each placement; `update` moves toward it at up to
 * `speed` world-units/second (each `LevelDefinition.cameraSpeed`, via
 * `setSpeed`), easing over the last stretch so it glides to a stop
 * instead of snapping — a consistent, tunable pace rather than an
 * opaque exponential constant.
 *
 * `punch()` adds a brief, directional spring-back dip — the camera
 * "absorbing" a landing — layered on top of the follow position via
 * `renderY`, independent of `targetY`/`y` so it never feeds back into
 * gameplay-facing camera state (see engine/EffectsManager for the
 * separate, jittery multi-axis CameraShake used for bigger moments).
 */
export class Camera {
  y = 0;
  targetY = 0;
  private speed: number;
  private punchOffset = 0;
  private punchVelocity = 0;

  constructor(speed = 260) {
    this.speed = speed;
  }

  setSpeed(speed: number): void {
    this.speed = speed;
  }

  setTarget(y: number): void {
    this.targetY = y;
  }

  update(dtSeconds: number): void {
    const diff = this.targetY - this.y;
    const distance = Math.abs(diff);
    if (distance > 0.02) {
      const easeFactor = distance < EASE_ZONE ? Math.max(MIN_EASE_FACTOR, distance / EASE_ZONE) : 1;
      const maxStep = this.speed * easeFactor * dtSeconds;
      this.y += Math.sign(diff) * Math.min(distance, maxStep);
    } else {
      this.y = this.targetY;
    }

    if (this.punchOffset !== 0 || this.punchVelocity !== 0) {
      const accel = -PUNCH_STIFFNESS * this.punchOffset - PUNCH_DAMPING * this.punchVelocity;
      this.punchVelocity += accel * dtSeconds;
      this.punchOffset += this.punchVelocity * dtSeconds;
      if (Math.abs(this.punchOffset) < 0.02 && Math.abs(this.punchVelocity) < 0.02) {
        this.punchOffset = 0;
        this.punchVelocity = 0;
      }
    }
  }

  /** A small downward impulse that springs back — the felt "weight" of a block landing. */
  punch(amount: number): void {
    this.punchVelocity -= clamp(amount, 0, 400);
  }

  /** What the renderer should actually use — follow position plus the transient landing dip. */
  get renderY(): number {
    return this.y + this.punchOffset;
  }

  reset(y = 0): void {
    this.y = y;
    this.targetY = y;
    this.punchOffset = 0;
    this.punchVelocity = 0;
  }
}
