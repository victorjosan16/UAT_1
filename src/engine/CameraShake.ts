import { prefersReducedMotion } from "@/utils/motion";
import { VFX_CONFIG } from "@/game/VFXConfig";

/**
 * Trauma-based screen shake: `add()` bumps trauma toward 1, it decays
 * smoothly every tick, and offset scales with trauma² so small bumps stay
 * subtle while big ones (game over) feel punchy without ever being
 * disorienting. Respects prefers-reduced-motion by capping trauma hard.
 */
export class CameraShake {
  private trauma = 0;
  private time = 0;
  private readonly freqX = 11;
  private readonly freqY = 13.7;

  add(amount: number): void {
    const reduced = prefersReducedMotion();
    const capped = reduced ? Math.min(amount, 0.05) : amount;
    this.trauma = Math.min(1, this.trauma + capped);
  }

  update(dtSeconds: number): void {
    this.time += dtSeconds;
    if (this.trauma > 0) {
      this.trauma = Math.max(0, this.trauma - VFX_CONFIG.shakeDecayPerSecond * dtSeconds);
    }
  }

  get offset(): { x: number; y: number } {
    if (this.trauma <= 0) return { x: 0, y: 0 };
    const shakePower = this.trauma * this.trauma;
    const max = VFX_CONFIG.shakeMaxOffset;
    return {
      x: max * shakePower * Math.sin(this.time * this.freqX),
      y: max * shakePower * 0.7 * Math.sin(this.time * this.freqY + 1.3),
    };
  }

  reset(): void {
    this.trauma = 0;
    this.time = 0;
  }
}
