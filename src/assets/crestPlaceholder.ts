import { xmur3 } from "@/utils/rng";
import { hslToHex } from "@/utils/color";
import type { Club } from "@/types";

/**
 * Real club crests are trademarked and we have no confirmed usage rights
 * for any club here — see docs/ASSETS_AND_RIGHTS.md. This generates a
 * deterministic PLACEHOLDER badge from the club's own id instead of using
 * (or scraping/imitating) a real logo. `ClubCrest` renders what this
 * returns; when real, licensed artwork is available, only the asset
 * resolver needs to change (e.g. return an image URL keyed by clubId) —
 * nothing in the quiz engine or components reads crest data directly.
 */

export type CrestShape = "shield" | "circle" | "hexagon" | "pentagon";
export type CrestPattern = "solid" | "halves" | "diagonal" | "quarters" | "stripes";

export interface CrestSpec {
  shape: CrestShape;
  pattern: CrestPattern;
  primaryColor: string;
  secondaryColor: string;
  ringColor: string;
  highlightColor: string;
  initials: string;
  /** Purely decorative "prestige" stars above the initials — cosmetic randomness, not tied to any real honours. */
  starCount: 0 | 1 | 2 | 3;
  mirrorDiagonal: boolean;
  /** 0..1 focus point used by ZOOM reveal — an off-center fragment to peek at first, not always the dead center. */
  zoomFocus: { x: number; y: number };
}

const SHAPES: readonly CrestShape[] = ["shield", "circle", "hexagon", "pentagon"];
const PATTERNS: readonly CrestPattern[] = ["solid", "solid", "halves", "diagonal", "quarters", "stripes"];

function hashToUnitFloat(hash: number): number {
  return (hash >>> 0) / 4294967296;
}

function initialsFor(name: string): string {
  const words = name
    .replace(/&/g, " ")
    .split(/\s+/)
    .filter((w) => w.length > 0 && !/^(fc|cf|sc|de|the)$/i.test(w));
  if (words.length >= 2) {
    return ((words[0]?.[0] ?? "") + (words[1]?.[0] ?? "")).toUpperCase();
  }
  return (words[0]?.slice(0, 2) ?? "??").toUpperCase();
}

/** Pure and deterministic — the same club always renders the same placeholder badge. */
export function crestSpecFor(club: Pick<Club, "id" | "name">): CrestSpec {
  const hash = xmur3(club.id);
  const h1 = hashToUnitFloat(hash());
  const h2 = hashToUnitFloat(hash());
  const h3 = hashToUnitFloat(hash());
  const h4 = hashToUnitFloat(hash());
  const h5 = hashToUnitFloat(hash());
  const h6 = hashToUnitFloat(hash());
  const h7 = hashToUnitFloat(hash());
  const h8 = hashToUnitFloat(hash());

  const hue = Math.round(h1 * 360);
  const shape = SHAPES[Math.floor(h2 * SHAPES.length)] ?? "shield";
  const pattern = PATTERNS[Math.floor(h6 * PATTERNS.length)] ?? "solid";
  const secondaryHue = (hue + 40 + Math.round(h3 * 60)) % 360;
  const starCount = h7 < 0.55 ? 0 : h7 < 0.8 ? 1 : h7 < 0.93 ? 2 : 3;

  return {
    shape,
    pattern,
    primaryColor: hslToHex(hue, 0.62, 0.42),
    secondaryColor: hslToHex(secondaryHue, 0.55, 0.3),
    ringColor: hslToHex(hue, 0.5, 0.16),
    highlightColor: hslToHex(hue, 0.35, 0.88),
    initials: initialsFor(club.name),
    starCount,
    mirrorDiagonal: h8 < 0.5,
    zoomFocus: { x: 0.25 + h4 * 0.5, y: 0.25 + h5 * 0.5 },
  };
}
