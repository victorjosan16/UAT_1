import { Block } from "./Block";
import type { Interval } from "@/types";

export interface OverlapResult {
  /** Signed distance between moving and previous block centers. */
  offset: number;
  /** Overlapping interval, or null when there is zero/negative overlap (miss). */
  overlap: Interval | null;
  isPerfect: boolean;
  /** Cut-away fragments (0, 1, or 2), each already positioned in world space. */
  fallingFragments: Interval[];
}

/**
 * Pure geometry: given the block below and the block being dropped, compute
 * the overlap, whether it's a PERFECT placement, and any fragments that get
 * cut away. No rendering, no timing — fully unit-testable.
 */
export function computeOverlap(previous: Interval, moving: Interval, perfectTolerance: number): OverlapResult {
  const previousCenter = (previous.left + previous.right) / 2;
  const movingCenter = (moving.left + moving.right) / 2;
  const offset = movingCenter - previousCenter;

  if (Math.abs(offset) <= perfectTolerance) {
    return { offset, overlap: { left: previous.left, right: previous.right }, isPerfect: true, fallingFragments: [] };
  }

  const left = Math.max(previous.left, moving.left);
  const right = Math.min(previous.right, moving.right);

  if (right <= left) {
    return { offset, overlap: null, isPerfect: false, fallingFragments: [{ left: moving.left, right: moving.right }] };
  }

  const fragments: Interval[] = [];
  if (moving.left < left) fragments.push({ left: moving.left, right: left });
  if (moving.right > right) fragments.push({ left: right, right: moving.right });

  return { offset, overlap: { left, right }, isPerfect: false, fallingFragments: fragments };
}

export class Tower {
  private readonly blocks: Block[] = [];
  readonly baseWidth: number;

  constructor(baseInterval: Interval, floorHeight: number) {
    const base = new Block({ ...baseInterval, floor: 0, y: 0, height: floorHeight });
    this.blocks.push(base);
    this.baseWidth = base.width;
  }

  get topBlock(): Block {
    const top = this.blocks[this.blocks.length - 1];
    if (!top) throw new Error("Tower has no blocks");
    return top;
  }

  get height(): number {
    return this.blocks.length - 1; // floors placed, excluding base
  }

  get allBlocks(): readonly Block[] {
    return this.blocks;
  }

  /** Adds a new placed block on top, at the given already-clipped interval. */
  place(interval: Interval, floorHeight: number): Block {
    const top = this.topBlock;
    const block = new Block({
      left: interval.left,
      right: interval.right,
      floor: top.floor + 1,
      y: top.y + top.height,
      height: floorHeight,
    });
    this.blocks.push(block);
    return block;
  }
}
