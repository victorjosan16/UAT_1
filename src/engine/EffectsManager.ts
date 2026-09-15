import { ParticleSystem } from "./Particles";
import { TrailRenderer } from "./TrailRenderer";
import { CameraShake } from "./CameraShake";
import { TimeScaleManager } from "./TimeScaleManager";
import { AudioManager } from "./Audio";
import { HapticsManager } from "./Haptics";
import { VFX_CONFIG } from "@/game/VFXConfig";
import { POWERUP_GLOW_COLOR, type PowerUpType } from "@/game/PowerUps";
import type { Grade } from "@/types";

export type GameEvent =
  | { type: "BLOCK_PLACED"; x: number; y: number; grade: Grade; color: string }
  | { type: "BLOCK_CUT"; x: number; y: number; direction: 1 | -1; fractionCut: number; color: string }
  | { type: "PERFECT"; x: number; y: number; streak: number; color: string }
  | { type: "COMBO_CHANGED"; x: number; y: number; multiplier: number; streak: number; color: string }
  | { type: "NEAR_MISS"; x: number; y: number; ratio: number }
  | { type: "MILESTONE"; x: number; y: number; floor: number; color: string }
  | { type: "NEW_RECORD"; x: number; y: number }
  | { type: "GAME_OVER"; x: number; y: number }
  | { type: "POWERUP_COLLECTED"; x: number; y: number; powerUp: PowerUpType };

/**
 * Event-driven "juice" hub: Game.ts owns gameplay math and only calls
 * `handle(event)` when something happens — every camera shake, particle
 * burst, slow-motion dip, sound, and haptic pulse lives here instead of
 * scattered through placement logic. See docs/GAME_DESIGN.md — this layer
 * never influences scoring or physics, only presentation.
 */
export class EffectsManager {
  readonly particles = new ParticleSystem();
  readonly trail = new TrailRenderer();
  readonly shake = new CameraShake();
  readonly timeScale = new TimeScaleManager();
  private readonly audio = new AudioManager();
  private readonly haptics = new HapticsManager();

  private perfectGlowValue = 0;
  private envPulseValue = 0;
  private recordGlowValue = 0;
  private darkenValue = 0;
  private gameOverStartMs: number | null = null;

  setSoundEnabled(enabled: boolean): void {
    this.audio.setEnabled(enabled);
  }

  setHapticsEnabled(enabled: boolean): void {
    this.haptics.setEnabled(enabled);
  }

  resumeAudio(): void {
    this.audio.resume();
  }

  playTap(): void {
    this.audio.tap();
  }

  playSpawnWhoosh(speedFactor: number): void {
    this.audio.spawnWhoosh(speedFactor);
  }

  handle(event: GameEvent): void {
    switch (event.type) {
      case "BLOCK_PLACED":
        this.audio.place();
        this.haptics.place();
        this.particles.landingDust(event.x, event.y, event.color);
        this.shake.add(VFX_CONFIG.placement.shakeTrauma);
        break;

      case "BLOCK_CUT":
        this.audio.cutFall();
        this.particles.cut(event.x, event.y, event.direction, event.color);
        this.shake.add(VFX_CONFIG.cut.shakeTraumaPerFraction * event.fractionCut);
        break;

      case "PERFECT": {
        this.audio.perfect(event.streak);
        this.haptics.perfect(event.streak);
        this.particles.perfect(event.x, event.y, event.streak, event.color);
        this.perfectGlowValue = 1;
        const isBigStreak = event.streak >= 5;
        this.shake.add(isBigStreak ? VFX_CONFIG.perfect.shakeTraumaBigStreak : VFX_CONFIG.perfect.shakeTrauma);
        if (VFX_CONFIG.perfectSlowMo.streakThresholds.includes(event.streak)) {
          const { scale, downMs, holdMs, upMs } = VFX_CONFIG.perfectSlowMo;
          this.timeScale.trigger(scale, downMs, holdMs, upMs);
        }
        break;
      }

      case "COMBO_CHANGED": {
        const tierIndex = Math.max(0, VFX_CONFIG.combo.milestones.indexOf(event.streak));
        this.audio.combo(event.multiplier);
        this.haptics.comboMilestone();
        this.particles.combo(event.x, event.y, tierIndex, event.color);
        this.shake.add(VFX_CONFIG.combo.shakeTrauma);
        break;
      }

      case "NEAR_MISS":
        this.audio.nearMiss();
        this.haptics.nearMiss();
        this.particles.nearMiss(event.x, event.y);
        this.shake.add(VFX_CONFIG.nearMiss.shakeTrauma);
        break;

      case "MILESTONE":
        this.audio.milestone();
        this.haptics.comboMilestone();
        this.particles.milestone(event.x, event.y, event.color);
        this.envPulseValue = 1;
        break;

      case "NEW_RECORD":
        this.audio.newRecord();
        this.haptics.newRecord();
        this.particles.record(event.x, event.y);
        this.shake.add(VFX_CONFIG.record.shakeTrauma);
        this.recordGlowValue = 1;
        this.timeScale.trigger(0.75, 40, 60, 120);
        break;

      case "GAME_OVER":
        this.audio.gameOver();
        this.haptics.failure();
        this.particles.gameOver(event.x, event.y);
        this.shake.add(VFX_CONFIG.gameOver.shakeTrauma);
        this.timeScale.trigger(VFX_CONFIG.gameOver.slowMoScale, 90, VFX_CONFIG.gameOver.slowMoMs - 90, 220);
        this.gameOverStartMs = performance.now();
        this.trail.clear();
        break;

      case "POWERUP_COLLECTED":
        this.audio.powerUp();
        this.haptics.comboMilestone();
        this.particles.milestone(event.x, event.y, POWERUP_GLOW_COLOR[event.powerUp]);
        this.shake.add(VFX_CONFIG.perfect.shakeTrauma);
        break;
    }
  }

  /** Called once per fixed sim step with the *real* (unscaled) dt. */
  update(realDtSeconds: number): void {
    this.shake.update(realDtSeconds);
    this.timeScale.update(realDtSeconds);

    const scaledDt = realDtSeconds * this.timeScale.value;
    this.particles.update(scaledDt);
    this.trail.update(scaledDt);

    if (this.perfectGlowValue > 0) this.perfectGlowValue = Math.max(0, this.perfectGlowValue - realDtSeconds * (1000 / VFX_CONFIG.perfect.durationMs));
    if (this.envPulseValue > 0) this.envPulseValue = Math.max(0, this.envPulseValue - realDtSeconds * (1000 / VFX_CONFIG.milestone.pulseDurationMs));
    if (this.recordGlowValue > 0) this.recordGlowValue = Math.max(0, this.recordGlowValue - realDtSeconds * (1000 / VFX_CONFIG.record.glowDurationMs));

    if (this.gameOverStartMs !== null) {
      const elapsed = performance.now() - this.gameOverStartMs;
      this.darkenValue = Math.min(VFX_CONFIG.gameOver.darkenPeak, (elapsed / VFX_CONFIG.gameOver.transitionMs) * VFX_CONFIG.gameOver.darkenPeak);
    }
  }

  get shakeOffset(): { x: number; y: number } {
    return this.shake.offset;
  }

  get timeScaleValue(): number {
    return this.timeScale.value;
  }

  get perfectGlow(): number {
    return this.perfectGlowValue;
  }

  get envPulse(): number {
    return this.envPulseValue;
  }

  get recordGlow(): number {
    return this.recordGlowValue;
  }

  get darkenAmount(): number {
    return this.darkenValue;
  }

  reset(): void {
    this.shake.reset();
    this.particles.clear();
    this.trail.clear();
    this.perfectGlowValue = 0;
    this.envPulseValue = 0;
    this.recordGlowValue = 0;
    this.darkenValue = 0;
    this.gameOverStartMs = null;
  }
}
