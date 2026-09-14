import { LEVELS } from "@/levels/levels";
import { lerpHueShortest } from "@/utils/color";

/**
 * Curated hue stops (degrees), one per classic level, hand-picked to stay
 * "premium" against a dark background — deliberately skips the muddy
 * yellow/olive band (~65-95°) that looks cheap. Endless keeps cycling
 * through the same list so the palette never needs to grow unbounded.
 */
const HUE_PALETTE: readonly number[] = [195, 210, 225, 240, 255, 270, 285, 300, 318, 335, 350, 8, 22, 38, 130, 148, 162, 178, 188, 200];

interface HueStop {
  floor: number;
  hue: number;
}

const AVERAGE_LEVEL_FLOORS = Math.round(LEVELS.reduce((sum, level) => sum + level.requiredBlocks, 0) / LEVELS.length);

function paletteHue(index: number): number {
  const hue = HUE_PALETTE[((index % HUE_PALETTE.length) + HUE_PALETTE.length) % HUE_PALETTE.length];
  return hue ?? 195;
}

function buildClassicStops(): HueStop[] {
  const stops: HueStop[] = [];
  let floor = 0;
  LEVELS.forEach((level, i) => {
    stops.push({ floor, hue: paletteHue(i) });
    floor += level.requiredBlocks;
  });
  stops.push({ floor, hue: paletteHue(LEVELS.length) });
  return stops;
}

const CLASSIC_STOPS = buildClassicStops();
const LAST_STOP: HueStop = (() => {
  const stop = CLASSIC_STOPS[CLASSIC_STOPS.length - 1];
  if (!stop) throw new Error("CLASSIC_STOPS must not be empty");
  return stop;
})();
const TOTAL_CLASSIC_FLOORS = LAST_STOP.floor;

function endlessBandStop(bandIndex: number): HueStop {
  return { floor: TOTAL_CLASSIC_FLOORS + bandIndex * AVERAGE_LEVEL_FLOORS, hue: paletteHue(LEVELS.length + bandIndex) };
}

/**
 * Smoothly interpolated hue (degrees) for an absolute floor number. Bands
 * align with classic level boundaries (so color changes roughly "per
 * level", as intended), blending across the transition rather than
 * cutting hard; Endless continues at the same visual pace, cycling
 * through the same curated palette indefinitely.
 */
export function hueForFloor(floor: number): number {
  const clampedFloor = Math.max(0, floor);

  if (clampedFloor <= TOTAL_CLASSIC_FLOORS) {
    for (let i = 0; i < CLASSIC_STOPS.length - 1; i++) {
      const a = CLASSIC_STOPS[i];
      const b = CLASSIC_STOPS[i + 1];
      if (!a || !b) continue;
      if (clampedFloor >= a.floor && clampedFloor <= b.floor) {
        const t = b.floor === a.floor ? 0 : (clampedFloor - a.floor) / (b.floor - a.floor);
        return lerpHueShortest(a.hue, b.hue, t);
      }
    }
    return LAST_STOP.hue;
  }

  const extra = clampedFloor - TOTAL_CLASSIC_FLOORS;
  const bandIndex = Math.floor(extra / AVERAGE_LEVEL_FLOORS);
  const a = endlessBandStop(bandIndex);
  const b = endlessBandStop(bandIndex + 1);
  const t = (extra - bandIndex * AVERAGE_LEVEL_FLOORS) / AVERAGE_LEVEL_FLOORS;
  return lerpHueShortest(a.hue, b.hue, t);
}
