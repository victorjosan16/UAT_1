import type { Interval } from "@/types";

export interface BlockGeometry extends Interval {
  /** Floor index, 0 = base platform. */
  floor: number;
  /** Vertical center of this block in world units (grows upward as floor increases). */
  y: number;
  height: number;
  /** Baked-in per-floor color (default skin's level hue-shift) — undefined means "use the current skin's flat color". */
  fillColor?: string;
  gradientTopColor?: string;
}

export class Block {
  left: number;
  right: number;
  readonly floor: number;
  readonly y: number;
  readonly height: number;
  readonly fillColor?: string;
  readonly gradientTopColor?: string;

  constructor(geometry: BlockGeometry) {
    this.left = geometry.left;
    this.right = geometry.right;
    this.floor = geometry.floor;
    this.y = geometry.y;
    this.height = geometry.height;
    this.fillColor = geometry.fillColor;
    this.gradientTopColor = geometry.gradientTopColor;
  }

  get width(): number {
    return this.right - this.left;
  }

  get center(): number {
    return (this.left + this.right) / 2;
  }

  toInterval(): Interval {
    return { left: this.left, right: this.right };
  }

  clone(): Block {
    return new Block({
      left: this.left,
      right: this.right,
      floor: this.floor,
      y: this.y,
      height: this.height,
      fillColor: this.fillColor,
      gradientTopColor: this.gradientTopColor,
    });
  }
}
