import type { Block } from "@/entities/Block";
import type { FallingPiece } from "@/entities/FallingPiece";
import type { ParticleSystem } from "./Particles";
import type { TrailRenderer } from "./TrailRenderer";
import type { EnvironmentTheme } from "@/ui/theme/environment";
import type { TowerSkin } from "@/ui/skins/TowerSkin";
import { VFX_CONFIG } from "@/game/VFXConfig";

export const VIRTUAL_WIDTH = 360;

/** World x=0 is screen-center; gameplay/effects code converts world→screen X through here. */
export function worldXToScreenX(worldX: number): number {
  return VIRTUAL_WIDTH / 2 + worldX;
}
const BASELINE_FRACTION = 0.76;
const MAX_DPR = 2;

export interface MovingBlockView {
  left: number;
  right: number;
  y: number;
  height: number;
  isDrifting: boolean;
  speed: number;
  /** Preview of the level-hue color this block will be baked with once placed (default skin only). */
  fillColor?: string;
  gradientTopColor?: string;
}

export interface RenderFrame {
  blocks: readonly Block[];
  movingBlock: MovingBlockView | null;
  fallingPieces: readonly FallingPiece[];
  cameraY: number;
  environment: EnvironmentTheme;
  skin: TowerSkin;
  particles: ParticleSystem;
  trail: TrailRenderer;
  perfectPulse: number; // 0..1 decaying pulse for the top block glow + scale
  fogAmount: number; // 0..1 visual-only reduction of visible floors below
  shakeOffset: { x: number; y: number };
  envPulse: number; // 0..1 decaying brightness pulse (milestones)
  recordGlow: number; // 0..1 decaying whole-scene flash (new record)
  darkenAmount: number; // 0..1 game-over vignette
  qualityScale: number; // 0..1 effect density from PerformanceMonitor
}

/**
 * Canvas renderer. Pure presentation: reads game/entity state, draws it.
 * No gameplay logic lives here. Handles devicePixelRatio (capped) and a
 * width-fit virtual coordinate system so the same code works portrait,
 * tablet, and desktop-centered.
 */
export class Renderer {
  private readonly ctx: CanvasRenderingContext2D;
  private scale = 1;
  private worldHeight = 640;
  private cssWidth = 360;
  private cssHeight = 640;

  constructor(private readonly canvas: HTMLCanvasElement) {
    const ctx = canvas.getContext("2d");
    if (!ctx) throw new Error("Canvas 2D context unavailable");
    this.ctx = ctx;
  }

  resize(): void {
    const rect = this.canvas.getBoundingClientRect();
    this.cssWidth = Math.max(1, rect.width);
    this.cssHeight = Math.max(1, rect.height);
    const dpr = Math.min(MAX_DPR, window.devicePixelRatio || 1);

    this.canvas.width = Math.round(this.cssWidth * dpr);
    this.canvas.height = Math.round(this.cssHeight * dpr);

    this.scale = this.cssWidth / VIRTUAL_WIDTH;
    this.worldHeight = this.cssHeight / this.scale;

    this.ctx.setTransform(dpr * this.scale, 0, 0, dpr * this.scale, 0, 0);
  }

  get baselineWorldY(): number {
    return this.worldHeight * BASELINE_FRACTION;
  }

  get visibleWorldHeight(): number {
    return this.worldHeight;
  }

  private worldToScreenX(worldX: number): number {
    return worldXToScreenX(worldX);
  }

  private worldToScreenY(worldY: number, cameraY: number): number {
    return this.baselineWorldY - (worldY - cameraY);
  }

  render(frame: RenderFrame): void {
    const ctx = this.ctx;
    ctx.clearRect(0, 0, VIRTUAL_WIDTH, this.worldHeight);
    this.drawSky(frame.environment, frame.envPulse);
    this.drawStars(frame.environment, frame.cameraY);

    const worldToScreenY = (y: number) => this.worldToScreenY(y, frame.cameraY);
    const worldToScreenX = (x: number) => this.worldToScreenX(x);

    ctx.save();
    ctx.translate(frame.shakeOffset.x, frame.shakeOffset.y);

    if (frame.movingBlock) {
      frame.trail.render(ctx, worldToScreenX, worldToScreenY, frame.movingBlock.fillColor ?? frame.skin.blockFill, frame.qualityScale);
    }

    for (const block of frame.blocks) {
      const isTop = block === frame.blocks[frame.blocks.length - 1];
      const fogFade = frame.fogAmount > 0 ? this.fogFadeFor(block, frame) : 1;
      if (fogFade <= 0.02) continue;
      const colors = block.fillColor ? { fillColor: block.fillColor, gradientTopColor: block.gradientTopColor ?? block.fillColor } : undefined;
      this.drawBlock(block.left, block.right, block.y, block.height, frame.skin, worldToScreenY, isTop && frame.perfectPulse > 0 ? frame.perfectPulse : 0, fogFade, colors);
    }

    for (const piece of frame.fallingPieces) {
      this.drawFallingPiece(piece, frame.skin, worldToScreenY);
    }

    if (frame.movingBlock) {
      const colors = frame.movingBlock.fillColor ? { fillColor: frame.movingBlock.fillColor, gradientTopColor: frame.movingBlock.gradientTopColor ?? frame.movingBlock.fillColor } : undefined;
      this.drawBlock(frame.movingBlock.left, frame.movingBlock.right, frame.movingBlock.y, frame.movingBlock.height, frame.skin, worldToScreenY, 0, 1, colors);
    }

    frame.particles.render(ctx, worldToScreenY);
    ctx.restore();

    if (frame.recordGlow > 0) {
      ctx.fillStyle = `rgba(255,255,255,${0.14 * frame.recordGlow})`;
      ctx.fillRect(0, 0, VIRTUAL_WIDTH, this.worldHeight);
    }

    if (frame.darkenAmount > 0) {
      ctx.fillStyle = `rgba(0,0,0,${frame.darkenAmount})`;
      ctx.fillRect(0, 0, VIRTUAL_WIDTH, this.worldHeight);
    }
  }

  private fogFadeFor(block: Block, frame: RenderFrame): number {
    const topFloor = frame.blocks[frame.blocks.length - 1]?.floor ?? block.floor;
    const distance = topFloor - block.floor;
    const visibleFloors = Math.max(3, Math.round(10 * (1 - frame.fogAmount)));
    if (distance <= visibleFloors) return 1;
    const fadeRange = 3;
    return Math.max(0, 1 - (distance - visibleFloors) / fadeRange);
  }

  private drawSky(env: EnvironmentTheme, envPulse: number): void {
    const ctx = this.ctx;
    const gradient = ctx.createLinearGradient(0, 0, 0, this.worldHeight);
    gradient.addColorStop(0, env.skyTop);
    gradient.addColorStop(1, env.skyBottom);
    ctx.fillStyle = gradient;
    ctx.fillRect(0, 0, VIRTUAL_WIDTH, this.worldHeight);

    if (envPulse > 0.01) {
      ctx.fillStyle = `rgba(255,255,255,${0.1 * envPulse})`;
      ctx.fillRect(0, 0, VIRTUAL_WIDTH, this.worldHeight);
    }
  }

  private starsSeed: { x: number; y: number; r: number; layer: number }[] | null = null;
  /** Two depth layers drift at different fractions of camera speed — a cheap, subtle parallax. */
  private drawStars(env: EnvironmentTheme, cameraY: number): void {
    if (env.starOpacity <= 0.01) return;
    if (!this.starsSeed) {
      const stars = [];
      for (let i = 0; i < 70; i++) {
        stars.push({ x: Math.random() * VIRTUAL_WIDTH, y: Math.random() * this.worldHeight, r: Math.random() * 1.4 + 0.3, layer: i % 2 === 0 ? 0.02 : 0.05 });
      }
      this.starsSeed = stars;
    }
    const ctx = this.ctx;
    ctx.fillStyle = `rgba(255,255,255,${env.starOpacity})`;
    for (const s of this.starsSeed) {
      const wrappedY = ((s.y - cameraY * s.layer) % this.worldHeight + this.worldHeight) % this.worldHeight;
      ctx.beginPath();
      ctx.arc(s.x, wrappedY, s.r, 0, Math.PI * 2);
      ctx.fill();
    }
  }

  private drawBlock(
    left: number,
    right: number,
    worldY: number,
    height: number,
    skin: TowerSkin,
    worldToScreenY: (y: number) => number,
    glowPulse: number,
    alpha: number,
    colorOverride?: { fillColor: string; gradientTopColor: string },
  ): void {
    const ctx = this.ctx;
    const x = this.worldToScreenX(left);
    const width = right - left;
    const yTop = worldToScreenY(worldY + height);
    const yBottom = worldToScreenY(worldY);
    const h = yBottom - yTop;

    ctx.save();
    ctx.globalAlpha = alpha;

    if (glowPulse > 0) {
      ctx.shadowColor = skin.perfectGlowColor;
      ctx.shadowBlur = 18 * glowPulse;
      // Quick scale pulse on the block that just landed, centered on itself.
      const pulseScale = 1 + (VFX_CONFIG.perfect.scalePulse - 1) * glowPulse;
      const cx = x + width / 2;
      const cy = (yTop + yBottom) / 2;
      ctx.translate(cx, cy);
      ctx.scale(pulseScale, pulseScale);
      ctx.translate(-cx, -cy);
    }

    const fillColor = colorOverride?.fillColor ?? skin.blockFill;
    const gradientTopColor = colorOverride?.gradientTopColor ?? skin.blockGradientTop ?? skin.blockFill;
    const grad = ctx.createLinearGradient(0, yTop, 0, yBottom);
    grad.addColorStop(0, gradientTopColor);
    grad.addColorStop(1, fillColor);
    ctx.fillStyle = grad;

    const radius = Math.min(6, h / 3, width / 3);
    this.roundRect(x, yTop, width, h, Math.max(0, radius));
    ctx.fill();

    ctx.lineWidth = 1;
    ctx.strokeStyle = skin.blockStroke;
    ctx.globalAlpha = alpha * 0.5;
    ctx.stroke();

    ctx.restore();
  }

  private drawFallingPiece(piece: FallingPiece, skin: TowerSkin, worldToScreenY: (y: number) => number): void {
    const ctx = this.ctx;
    const alpha = Math.max(0, 1 - piece.ageMs / piece.maxAgeMs);
    const cx = this.worldToScreenX((piece.left + piece.right) / 2);
    const yTop = worldToScreenY(piece.y + piece.height);
    const yBottom = worldToScreenY(piece.y);
    const cy = (yTop + yBottom) / 2;
    const width = piece.right - piece.left;
    const h = yBottom - yTop;

    ctx.save();
    ctx.globalAlpha = alpha;
    ctx.translate(cx, cy);
    ctx.rotate(piece.rotation);
    ctx.fillStyle = piece.fillColor ?? skin.blockFill;
    ctx.fillRect(-width / 2, -h / 2, width, h);
    ctx.restore();
  }

  private roundRect(x: number, y: number, w: number, h: number, r: number): void {
    const ctx = this.ctx;
    ctx.beginPath();
    ctx.moveTo(x + r, y);
    ctx.arcTo(x + w, y, x + w, y + h, r);
    ctx.arcTo(x + w, y + h, x, y + h, r);
    ctx.arcTo(x, y + h, x, y, r);
    ctx.arcTo(x, y, x + w, y, r);
    ctx.closePath();
  }
}
