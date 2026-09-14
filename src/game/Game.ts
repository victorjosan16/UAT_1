import { Renderer, type RenderFrame, worldXToScreenX } from "@/engine/Renderer";
import { Camera } from "@/engine/Camera";
import { InputController } from "@/engine/Input";
import { EffectsManager } from "@/engine/EffectsManager";
import { PerformanceMonitor } from "@/engine/PerformanceMonitor";
import { Tower, computeOverlap } from "@/entities/Tower";
import { FallingPiece } from "@/entities/FallingPiece";
import { getLevel, MAX_LEVEL, BASE_BLOCK_HEIGHT } from "@/levels/levels";
import type { LevelDefinition } from "@/levels/LevelDefinition";
import { DifficultyEngine } from "@/levels/DifficultyEngine";
import { ScoreEngine } from "@/scoring/ScoreEngine";
import { SeededRandom } from "@/utils/rng";
import { environmentForHeight } from "@/ui/theme/environment";
import { DEFAULT_TOWER_SKIN, type TowerSkin } from "@/ui/skins/TowerSkin";
import { resolveBlockColors } from "@/ui/theme/blockColor";
import { GAME_VERSION, RULES_VERSION } from "@/branding";
import { VFX_CONFIG } from "./VFXConfig";
import { createInitialState, type GameState } from "./GameState";
import { GameLoop } from "./GameLoop";
import {
  PLAY_LEFT_BOUND,
  PLAY_RIGHT_BOUND,
  MILESTONES,
  WIND_AMPLITUDE,
  WIND_FREQUENCY_HZ,
  SPEED_SHIFT_AMPLITUDE,
  SPEED_SHIFT_FREQUENCY_HZ,
  SMALL_START_WIDTH_FACTOR,
  PRECISION_TOLERANCE_FACTOR,
  DOUBLE_SPEED_FACTOR,
  FOG_AMOUNT,
  MOVING_BASE_AMPLITUDE,
  MOVING_BASE_FREQUENCY_HZ,
  REVERSE_FRACTION_MIN,
  REVERSE_FRACTION_MAX,
} from "./GameConfig";
import type { Direction, GameMode, Grade, Interval, PlacementResult, RunSummary } from "@/types";

export interface GameCallbacks {
  onScoreChange?: (score: number) => void;
  onFeedback?: (text: string, kind: "nice" | "close" | "perfect") => void;
  onPlacement?: (result: PlacementResult, grade: Grade) => void;
  onLevelComplete?: (level: number) => void;
  onEndlessUnlocked?: () => void;
  onMilestone?: (floor: number) => void;
  /** Fires once, the instant the running score first exceeds the player's prior best — separate from the end-of-run "isNewRecord" stat. */
  onNewRecordCrossed?: () => void;
  onGameOver?: (summary: RunSummary) => void;
}

interface MovingBlockState extends Interval {
  y: number;
  height: number;
  baseCenter: number;
  direction: Direction;
  passElapsedSeconds: number;
  reverseAtSeconds: number;
  reversedThisPass: boolean;
}

const GAME_OVER_STATUS_DELAY_MS = VFX_CONFIG.gameOver.transitionMs;

/**
 * Orchestrates the full gameplay simulation: level/difficulty source,
 * tower + moving block physics, scoring, feedback, and rendering. A "mode"
 * (see src/modes) only supplies seed + which levels/difficulty to draw
 * from + when the run ends; everything else is shared here.
 *
 * All game-feel/juice (shake, slow-mo, particles, sound, haptics) is
 * delegated to `EffectsManager` via discrete events — this class only
 * ever decides *that* something happened (a placement, a cut, a miss),
 * never *how it looks*.
 */
export class Game {
  private readonly renderer: Renderer;
  private readonly camera = new Camera();
  private readonly input: InputController;
  private readonly effects = new EffectsManager();
  private readonly performanceMonitor = new PerformanceMonitor();
  private readonly loop: GameLoop;
  private readonly rng: SeededRandom;
  private readonly difficultyEngine: DifficultyEngine;
  private readonly scoreEngine = new ScoreEngine();
  private readonly skin: TowerSkin = DEFAULT_TOWER_SKIN;
  private readonly milestonesFired = new Set<number>();

  private tower!: Tower;
  private moving: MovingBlockState | null = null;
  private fallingPieces: FallingPiece[] = [];
  private currentLevelDef!: LevelDefinition;
  private currentSpeed = 0;
  private blocksPlacedInLevel = 0;
  private movingBaseAnchor: Interval | null = null;
  private lastDirection: Direction = 1;
  private recordCrossed = false;
  private gameOverAtMs: number | null = null;
  private gameOverSummaryFired = false;

  state: GameState;

  constructor(
    canvas: HTMLCanvasElement,
    mode: GameMode,
    seed: string,
    private readonly callbacks: GameCallbacks = {},
    targetScoreToBeat: number | null = null,
    private readonly startLevel: number = 1,
    private readonly personalBestScore: number = 0,
  ) {
    this.renderer = new Renderer(canvas);
    this.input = new InputController(canvas);
    this.rng = new SeededRandom(seed);
    this.difficultyEngine = new DifficultyEngine(seed);
    this.state = createInitialState(mode, seed, targetScoreToBeat);
    this.loop = new GameLoop(this.update, this.renderFrame);

    this.input.onPlace(() => {
      this.effects.resumeAudio();
      if (this.state.status === "PLAYING") this.effects.playTap();
      this.place();
    });

    window.addEventListener("resize", this.handleResize);
    this.handleResize();
  }

  setSoundEnabled(enabled: boolean): void {
    this.effects.setSoundEnabled(enabled);
  }

  setHapticsEnabled(enabled: boolean): void {
    this.effects.setHapticsEnabled(enabled);
  }

  private readonly handleResize = (): void => {
    this.renderer.resize();
  };

  start(): void {
    this.state = createInitialState(this.state.mode, this.state.seed, this.state.targetScoreToBeat);
    this.state.status = "PLAYING";
    this.state.startedAtMs = performance.now();
    this.milestonesFired.clear();
    this.fallingPieces = [];
    this.blocksPlacedInLevel = 0;
    this.lastDirection = 1;
    this.recordCrossed = false;
    this.gameOverAtMs = null;
    this.gameOverSummaryFired = false;
    this.effects.reset();

    this.state.level = this.startLevel;
    const startDef = this.levelDefFor(this.startLevel);
    const baseWidth = startDef.startingBlockWidth;
    const baseColor = resolveBlockColors(this.skin, 0) ?? undefined;
    this.tower = new Tower({ left: -baseWidth / 2, right: baseWidth / 2 }, BASE_BLOCK_HEIGHT, baseColor);
    this.camera.reset(0);
    this.camera.setTarget(0);

    this.currentLevelDef = startDef;
    this.currentSpeed = this.currentLevelDef.movementSpeed;
    this.movingBaseAnchor = this.tower.topBlock.toInterval();
    this.spawnNextBlock(this.tower.topBlock.width);

    this.loop.start();
  }

  stop(): void {
    this.loop.stop();
  }

  destroy(): void {
    this.loop.stop();
    this.input.destroy();
    window.removeEventListener("resize", this.handleResize);
  }

  private levelDefFor(level: number): LevelDefinition {
    return level <= MAX_LEVEL ? getLevel(level) : this.difficultyEngine.definitionForFloor(level - MAX_LEVEL);
  }

  private directionForSpawn(pattern: LevelDefinition["directionPattern"]): Direction {
    if (pattern === "ALTERNATING") return this.lastDirection === 1 ? -1 : 1;
    if (pattern === "VARIABLE") return this.rng.sign();
    return this.lastDirection;
  }

  private spawnNextBlock(width: number): void {
    const top = this.tower.topBlock;
    const def = this.currentLevelDef;
    let effectiveWidth = width;
    if (def.specialModifier === "SMALL_START" && this.blocksPlacedInLevel === 0) {
      effectiveWidth *= SMALL_START_WIDTH_FACTOR;
    }

    const direction = this.directionForSpawn(def.directionPattern);
    this.lastDirection = direction;

    const half = effectiveWidth / 2;
    const startCenter = direction === 1 ? PLAY_LEFT_BOUND + half : PLAY_RIGHT_BOUND - half;

    this.moving = {
      left: startCenter - half,
      right: startCenter + half,
      y: top.y + top.height,
      height: BASE_BLOCK_HEIGHT,
      baseCenter: startCenter,
      direction,
      passElapsedSeconds: 0,
      reverseAtSeconds: this.rng.range(REVERSE_FRACTION_MIN, REVERSE_FRACTION_MAX) * 2,
      reversedThisPass: false,
    };
    this.effects.trail.clear();

    const speedFactor = Math.min(1, this.currentSpeed / VFX_CONFIG.trail.maxReferenceSpeed);
    this.effects.playSpawnWhoosh(speedFactor);

    this.movingBaseAnchor = top.toInterval();
    this.camera.setTarget(top.y + top.height - this.renderer.baselineWorldY + this.renderer.visibleWorldHeight * 0.5);
  }

  private effectiveTolerance(): number {
    const def = this.currentLevelDef;
    return def.specialModifier === "PRECISION" ? def.perfectTolerance * PRECISION_TOLERANCE_FACTOR : def.perfectTolerance;
  }

  private effectiveSpeed(passElapsed: number): number {
    const def = this.currentLevelDef;
    let speed = this.currentSpeed;
    if (def.specialModifier === "DOUBLE_SPEED") speed *= DOUBLE_SPEED_FACTOR;
    if (def.specialModifier === "SPEED_SHIFT") {
      speed *= 1 + SPEED_SHIFT_AMPLITUDE * Math.sin(passElapsed * 2 * Math.PI * SPEED_SHIFT_FREQUENCY_HZ);
    }
    return speed;
  }

  private readonly update = (realDtSeconds: number): void => {
    this.effects.update(realDtSeconds);
    this.performanceMonitor.recordFrame(realDtSeconds * 1000);
    this.effects.particles.setQualityScale(this.performanceMonitor.particleScale);

    if (this.state.status === "GAME_OVER") {
      // Keep the falling piece / shake / darken cinematic playing briefly before the summary fires,
      // at the reduced time-scale the GAME_OVER effect triggers (see EffectsManager).
      const scaledDt = realDtSeconds * this.effects.timeScaleValue;
      const piece0 = this.fallingPieces;
      for (const piece of piece0) piece.update(scaledDt);
      this.fallingPieces = piece0.filter((p) => !p.isExpired);

      if (!this.gameOverSummaryFired && this.gameOverAtMs !== null && performance.now() - this.gameOverAtMs >= GAME_OVER_STATUS_DELAY_MS) {
        this.gameOverSummaryFired = true;
        this.fireGameOverSummary();
      }
      return;
    }

    if (this.state.status !== "PLAYING") return;

    this.camera.update(realDtSeconds);

    const scaledDt = realDtSeconds * this.effects.timeScaleValue;
    for (const piece of this.fallingPieces) piece.update(scaledDt);
    this.fallingPieces = this.fallingPieces.filter((p) => !p.isExpired);

    if (this.movingBaseAnchor && this.currentLevelDef.specialModifier === "MOVING_BASE") {
      const t = performance.now() / 1000;
      const offset = MOVING_BASE_AMPLITUDE * Math.sin(t * 2 * Math.PI * MOVING_BASE_FREQUENCY_HZ);
      const anchor = this.movingBaseAnchor;
      this.tower.topBlock.left = anchor.left + offset;
      this.tower.topBlock.right = anchor.right + offset;
    }

    const moving = this.moving;
    if (!moving) return;

    moving.passElapsedSeconds += scaledDt;
    if (this.currentLevelDef.specialModifier === "REVERSE" && !moving.reversedThisPass && moving.passElapsedSeconds >= moving.reverseAtSeconds) {
      moving.direction = moving.direction === 1 ? -1 : 1;
      moving.reversedThisPass = true;
    }

    const speed = this.effectiveSpeed(moving.passElapsedSeconds);
    const width = moving.right - moving.left;
    const half = width / 2;
    let center = moving.baseCenter + moving.direction * speed * scaledDt;

    const leftBound = PLAY_LEFT_BOUND + half;
    const rightBound = PLAY_RIGHT_BOUND - half;
    if (center <= leftBound) {
      center = leftBound;
      moving.direction = 1;
    } else if (center >= rightBound) {
      center = rightBound;
      moving.direction = -1;
    }
    moving.baseCenter = center;

    let displayCenter = center;
    if (this.currentLevelDef.specialModifier === "WIND") {
      displayCenter += WIND_AMPLITUDE * Math.sin(moving.passElapsedSeconds * 2 * Math.PI * WIND_FREQUENCY_HZ);
    }

    moving.left = displayCenter - half;
    moving.right = displayCenter + half;

    this.effects.trail.record(moving.left, moving.right, moving.y, moving.height, Math.abs(speed), realDtSeconds);
  };

  private readonly renderFrame = (): void => {
    if (!this.tower) return;
    const height = this.tower.height;
    const previewColors = this.moving ? resolveBlockColors(this.skin, height + 1) : null;
    const frame: RenderFrame = {
      blocks: this.tower.allBlocks,
      movingBlock: this.moving
        ? {
            left: this.moving.left,
            right: this.moving.right,
            y: this.moving.y,
            height: this.moving.height,
            isDrifting: this.currentLevelDef?.specialModifier === "WIND",
            speed: this.effectiveSpeed(this.moving.passElapsedSeconds),
            fillColor: previewColors?.fillColor,
            gradientTopColor: previewColors?.gradientTopColor,
          }
        : null,
      fallingPieces: this.fallingPieces,
      cameraY: this.camera.y,
      environment: environmentForHeight(height),
      skin: this.skin,
      particles: this.effects.particles,
      trail: this.effects.trail,
      perfectPulse: this.effects.perfectGlow,
      fogAmount: this.currentLevelDef?.specialModifier === "FOG" ? FOG_AMOUNT : 0,
      shakeOffset: this.effects.shakeOffset,
      envPulse: this.effects.envPulse,
      recordGlow: this.effects.recordGlow,
      darkenAmount: this.effects.darkenAmount,
      qualityScale: this.performanceMonitor.particleScale,
    };
    this.renderer.render(frame);
  };

  private place(): void {
    if (this.state.status !== "PLAYING" || !this.moving) return;

    const previous = this.tower.topBlock;
    const previousWidth = previous.width;
    const tolerance = this.effectiveTolerance();
    const overlapResult = computeOverlap(previous.toInterval(), { left: this.moving.left, right: this.moving.right }, tolerance);

    const speedAtDrop = this.effectiveSpeed(this.moving.passElapsedSeconds);
    const flingDirection = this.moving.direction;
    const dropY = this.moving.y;
    const dropHeight = this.moving.height;
    const dropColors = resolveBlockColors(this.skin, previous.floor + 1);

    for (const fragment of overlapResult.fallingFragments) {
      const isRightSide = fragment.left >= (overlapResult.overlap?.right ?? previous.center);
      const vx = (isRightSide ? 1 : -1) * Math.max(60, speedAtDrop * 0.5) + flingDirection * 20;
      this.fallingPieces.push(new FallingPiece({ ...fragment, y: dropY, height: dropHeight, vx, fillColor: dropColors?.fillColor }));
    }

    if (!overlapResult.overlap) {
      this.beginGameOver(worldXToScreenX((this.moving.left + this.moving.right) / 2), dropY);
      return;
    }

    const movingWidth = this.moving.right - this.moving.left;
    const placedBlock = this.tower.place(overlapResult.overlap, BASE_BLOCK_HEIGHT, dropColors ?? undefined);
    const blockCenterScreenX = worldXToScreenX((placedBlock.left + placedBlock.right) / 2);

    const result = this.scoreEngine.place(
      overlapResult.overlap.right - overlapResult.overlap.left,
      previousWidth,
      placedBlock.floor,
      overlapResult.isPerfect,
      this.currentLevelDef.scoreMultiplier,
    );

    this.state.score = this.scoreEngine.totalScore;
    this.state.combo = this.scoreEngine.comboState;
    this.state.floor = placedBlock.floor;
    if (overlapResult.isPerfect) this.state.perfectCount += 1;

    const placement: PlacementResult = {
      index: this.state.placements.length,
      floor: placedBlock.floor,
      accuracy: result.accuracy,
      grade: result.grade,
      isPerfect: overlapResult.isPerfect,
      perfectStreak: result.combo.perfectStreak,
      comboMultiplier: result.combo.multiplier,
      overlapWidth: overlapResult.overlap.right - overlapResult.overlap.left,
      blockWidthBefore: previousWidth,
      blockWidthAfter: placedBlock.width,
      scoreGained: result.gained,
      totalScore: this.state.score,
      timestampMs: performance.now() - this.state.startedAtMs,
    };
    this.state.placements.push(placement);

    this.callbacks.onScoreChange?.(this.state.score);
    this.callbacks.onPlacement?.(placement, result.grade);

    const isNearMiss = !overlapResult.isPerfect && result.accuracy <= VFX_CONFIG.nearMiss.survivalRatioThreshold * 100;

    if (overlapResult.isPerfect) {
      this.effects.handle({ type: "PERFECT", x: blockCenterScreenX, y: placedBlock.y + placedBlock.height, streak: result.combo.perfectStreak, color: this.skin.perfectGlowColor });
      this.callbacks.onFeedback?.(result.combo.perfectStreak > 1 ? `PERFECT ×${result.combo.perfectStreak}` : "PERFECT!", "perfect");
    } else {
      this.effects.handle({ type: "BLOCK_PLACED", x: blockCenterScreenX, y: placedBlock.y + placedBlock.height, grade: result.grade });

      if (overlapResult.fallingFragments.length > 0) {
        const fractionCut = movingWidth === 0 ? 0 : 1 - (overlapResult.overlap.right - overlapResult.overlap.left) / movingWidth;
        this.effects.handle({ type: "BLOCK_CUT", x: blockCenterScreenX, y: placedBlock.y + placedBlock.height, direction: flingDirection, fractionCut, color: dropColors?.fillColor ?? this.skin.blockFill });
      }

      if (isNearMiss) {
        this.effects.handle({ type: "NEAR_MISS", x: blockCenterScreenX, y: placedBlock.y + placedBlock.height, ratio: result.accuracy / 100 });
        this.callbacks.onFeedback?.(result.accuracy < 5 ? "LUCKY!" : "CLOSE!", "close");
      } else {
        this.callbacks.onFeedback?.(placement.index === 0 ? "NICE!" : result.grade === "GREAT" ? "SO CLOSE!" : result.grade === "GOOD" ? "NICE!" : "RISKY!", result.grade === "GREAT" ? "close" : "nice");
      }
    }

    if (VFX_CONFIG.combo.milestones.includes(result.combo.streak)) {
      this.effects.handle({ type: "COMBO_CHANGED", x: blockCenterScreenX, y: placedBlock.y + placedBlock.height, multiplier: result.combo.multiplier, streak: result.combo.streak, color: dropColors?.fillColor ?? this.skin.blockFill });
    }

    if (!this.recordCrossed && this.personalBestScore > 0 && this.state.score > this.personalBestScore) {
      this.recordCrossed = true;
      this.effects.handle({ type: "NEW_RECORD", x: blockCenterScreenX, y: placedBlock.y + placedBlock.height });
      this.callbacks.onNewRecordCrossed?.();
    }

    for (const milestone of MILESTONES) {
      if (placedBlock.floor >= milestone && !this.milestonesFired.has(milestone)) {
        this.milestonesFired.add(milestone);
        this.effects.handle({ type: "MILESTONE", x: blockCenterScreenX, y: placedBlock.y + placedBlock.height, floor: milestone, color: this.skin.perfectGlowColor });
        this.callbacks.onMilestone?.(milestone);
      }
    }

    this.blocksPlacedInLevel += 1;
    if (this.blocksPlacedInLevel >= this.currentLevelDef.requiredBlocks) {
      const finishedLevel = this.state.level;
      this.state.level += 1;
      this.blocksPlacedInLevel = 0;
      this.currentLevelDef = this.levelDefFor(this.state.level);
      this.currentSpeed = this.currentLevelDef.movementSpeed;
      if (finishedLevel === MAX_LEVEL) {
        this.callbacks.onEndlessUnlocked?.();
      } else if (finishedLevel < MAX_LEVEL) {
        this.callbacks.onLevelComplete?.(finishedLevel);
      }
    } else {
      this.currentSpeed += this.currentLevelDef.speedIncrease;
    }

    this.spawnNextBlock(placedBlock.width);
  }

  private beginGameOver(screenX: number, worldY: number): void {
    this.state.status = "GAME_OVER";
    this.gameOverAtMs = performance.now();
    this.effects.handle({ type: "GAME_OVER", x: screenX, y: worldY });
  }

  private fireGameOverSummary(): void {
    const durationMs = performance.now() - this.state.startedAtMs;
    const summary: RunSummary = {
      mode: this.state.mode,
      seed: this.state.seed,
      gameVersion: GAME_VERSION,
      rulesVersion: RULES_VERSION,
      score: this.state.score,
      height: this.state.floor,
      perfectCount: this.state.perfectCount,
      bestCombo: this.scoreEngine.comboState.bestCombo,
      bestPerfectStreak: this.scoreEngine.comboState.bestPerfectStreak,
      averageAccuracy: this.scoreEngine.averageAccuracy,
      placements: this.state.placements,
      durationMs,
    };
    this.callbacks.onGameOver?.(summary);
  }
}
