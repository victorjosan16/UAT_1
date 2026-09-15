import { VFX_CONFIG } from "@/game/VFXConfig";
import { prefersReducedMotion } from "@/utils/motion";

interface Particle {
  active: boolean;
  x: number;
  y: number;
  vx: number;
  vy: number;
  life: number;
  maxLife: number;
  size: number;
  color: string;
  gravity: number;
}

const POOL_SIZE = 220;

/**
 * Pooled particle system: a fixed-size preallocated array, reused in
 * place (swap-free — inactive slots are just skipped) so bursts never
 * allocate. `spawnBurst` is the low-level primitive; the named presets
 * below (perfect/combo/cut/nearMiss/record/milestone/gameOver) are what
 * gameplay/effects code should call so every effect shares one
 * implementation instead of bespoke particle code per event.
 */
export class ParticleSystem {
  private readonly pool: Particle[];
  private qualityScale = 1;
  private cursor = 0;

  constructor() {
    this.pool = Array.from({ length: POOL_SIZE }, () => ({
      active: false,
      x: 0,
      y: 0,
      vx: 0,
      vy: 0,
      life: 0,
      maxLife: 1,
      size: 2,
      color: "#fff",
      gravity: 420,
    }));
  }

  setQualityScale(scale: number): void {
    this.qualityScale = Math.min(1, Math.max(0.15, scale));
  }

  private acquire(): Particle | null {
    for (let i = 0; i < this.pool.length; i++) {
      const idx = (this.cursor + i) % this.pool.length;
      const p = this.pool[idx];
      if (p && !p.active) {
        this.cursor = (idx + 1) % this.pool.length;
        return p;
      }
    }
    return null;
  }

  spawnBurst(x: number, y: number, count: number, color: string, speed = 160, gravity = 420): void {
    const scale = prefersReducedMotion() ? Math.min(this.qualityScale, 0.4) : this.qualityScale;
    const actualCount = Math.max(1, Math.round(count * scale));
    for (let i = 0; i < actualCount; i++) {
      const p = this.acquire();
      if (!p) break;
      const angle = (Math.PI * 2 * i) / actualCount + Math.random() * 0.6;
      const velocity = speed * (0.4 + Math.random() * 0.6);
      const maxLife = 0.45 + Math.random() * 0.35;
      p.active = true;
      p.x = x;
      p.y = y;
      p.vx = Math.cos(angle) * velocity;
      p.vy = Math.sin(angle) * velocity - 60;
      p.life = maxLife;
      p.maxLife = maxLife;
      p.size = 2 + Math.random() * 3;
      p.color = color;
      p.gravity = gravity;
    }
  }

  /** Directional shower — used for cut edges and near-miss sparks. */
  spawnDirectional(x: number, y: number, count: number, color: string, directionAngle: number, spread: number, speed: number): void {
    const scale = prefersReducedMotion() ? Math.min(this.qualityScale, 0.4) : this.qualityScale;
    const actualCount = Math.max(1, Math.round(count * scale));
    for (let i = 0; i < actualCount; i++) {
      const p = this.acquire();
      if (!p) break;
      const angle = directionAngle + (Math.random() - 0.5) * spread;
      const velocity = speed * (0.5 + Math.random() * 0.6);
      const maxLife = 0.3 + Math.random() * 0.25;
      p.active = true;
      p.x = x;
      p.y = y;
      p.vx = Math.cos(angle) * velocity;
      p.vy = Math.sin(angle) * velocity;
      p.life = maxLife;
      p.maxLife = maxLife;
      p.size = 1.5 + Math.random() * 2;
      p.color = color;
      p.gravity = 320;
    }
  }

  /** The baseline "thud" every successful placement gets — a small, low, sideways dust puff, not a big burst. */
  landingDust(x: number, y: number, color: string): void {
    const count = Math.max(1, Math.round(VFX_CONFIG.placement.dustParticleCount / 2));
    this.spawnDirectional(x, y, count, color, 0, 0.7, 70);
    this.spawnDirectional(x, y, count, color, Math.PI, 0.7, 70);
  }

  perfect(x: number, y: number, streak: number, color: string): void {
    const count = Math.min(VFX_CONFIG.perfect.particleCountCap, VFX_CONFIG.perfect.particleCountBase + streak * VFX_CONFIG.perfect.particleCountPerStreak);
    this.spawnBurst(x, y, count, color, 190);
  }

  combo(x: number, y: number, tierIndex: number, color: string): void {
    const count = VFX_CONFIG.combo.particleCountBase + tierIndex * VFX_CONFIG.combo.particleCountPerTier;
    this.spawnBurst(x, y, count, color, 240, 500);
  }

  cut(x: number, y: number, direction: number, color: string): void {
    this.spawnDirectional(x, y, VFX_CONFIG.cut.particleCount, color, direction > 0 ? 0 : Math.PI, 0.9, 140);
  }

  nearMiss(x: number, y: number, color = "#ffcf4f"): void {
    this.spawnBurst(x, y, VFX_CONFIG.nearMiss.particleCount, color, 130, 360);
  }

  record(x: number, y: number, color = "#ffffff"): void {
    this.spawnBurst(x, y, VFX_CONFIG.record.particleCount, color, 260, 380);
  }

  milestone(x: number, y: number, color: string): void {
    this.spawnBurst(x, y, VFX_CONFIG.milestone.particleCount, color, 150, 300);
  }

  gameOver(x: number, y: number, color = "#ff5f6d"): void {
    this.spawnBurst(x, y, VFX_CONFIG.gameOver.particleCount, color, 220, 600);
  }

  update(dtSeconds: number): void {
    for (const p of this.pool) {
      if (!p.active) continue;
      p.life -= dtSeconds;
      if (p.life <= 0) {
        p.active = false;
        continue;
      }
      p.vy += p.gravity * dtSeconds;
      p.x += p.vx * dtSeconds;
      p.y += p.vy * dtSeconds;
    }
  }

  render(ctx: CanvasRenderingContext2D, worldToScreenY: (y: number) => number): void {
    for (const p of this.pool) {
      if (!p.active) continue;
      const alpha = Math.max(0, p.life / p.maxLife);
      ctx.globalAlpha = alpha;
      ctx.fillStyle = p.color;
      ctx.beginPath();
      ctx.arc(p.x, worldToScreenY(p.y), p.size, 0, Math.PI * 2);
      ctx.fill();
    }
    ctx.globalAlpha = 1;
  }

  get activeCount(): number {
    let n = 0;
    for (const p of this.pool) if (p.active) n++;
    return n;
  }

  clear(): void {
    for (const p of this.pool) p.active = false;
  }
}
