import { clamp, lerp } from "@/utils/math";

export interface EnvironmentTheme {
  name: string;
  skyTop: string;
  skyBottom: string;
  towerColor: string;
  towerAccent: string;
  particleColor: string;
  starOpacity: number;
}

interface Stop {
  height: number;
  theme: EnvironmentTheme;
}

/** Height bands per docs/GAME_DESIGN.md §11. Interpolated, never a hard cut. */
const STOPS: readonly Stop[] = [
  { height: 0, theme: { name: "GROUND", skyTop: "#161a24", skyBottom: "#05070c", towerColor: "#3a4256", towerAccent: "#4fd1ff", particleColor: "#4fd1ff", starOpacity: 0 } },
  { height: 10, theme: { name: "CITY", skyTop: "#1b2340", skyBottom: "#0a0e1c", towerColor: "#4a5478", towerAccent: "#ffd54f", particleColor: "#ffd54f", starOpacity: 0 } },
  { height: 25, theme: { name: "SKY", skyTop: "#2f6fb0", skyBottom: "#0e1b30", towerColor: "#5c7aa0", towerAccent: "#ffffff", particleColor: "#ffffff", starOpacity: 0 } },
  { height: 50, theme: { name: "CLOUDS", skyTop: "#7fb8e8", skyBottom: "#1a2c46", towerColor: "#7f9dbf", towerAccent: "#ffffff", particleColor: "#eaf6ff", starOpacity: 0.15 } },
  { height: 75, theme: { name: "UPPER_ATMOSPHERE", skyTop: "#233a72", skyBottom: "#0a0f22", towerColor: "#8f8fd0", towerAccent: "#c9a6ff", particleColor: "#c9a6ff", starOpacity: 0.55 } },
  { height: 100, theme: { name: "SPACE", skyTop: "#04061a", skyBottom: "#000000", towerColor: "#c7c7e6", towerAccent: "#4fd1ff", particleColor: "#ffffff", starOpacity: 1 } },
];

function hexToRgb(hex: string): [number, number, number] {
  const clean = hex.replace("#", "");
  const bigint = parseInt(clean, 16);
  return [(bigint >> 16) & 255, (bigint >> 8) & 255, bigint & 255];
}

function rgbToHex(r: number, g: number, b: number): string {
  const c = (v: number) => clamp(Math.round(v), 0, 255).toString(16).padStart(2, "0");
  return `#${c(r)}${c(g)}${c(b)}`;
}

function lerpColor(a: string, b: string, t: number): string {
  const [ar, ag, ab] = hexToRgb(a);
  const [br, bg, bb] = hexToRgb(b);
  return rgbToHex(lerp(ar, br, t), lerp(ag, bg, t), lerp(ab, bb, t));
}

export function environmentForHeight(height: number): EnvironmentTheme {
  const first = STOPS[0];
  const last = STOPS[STOPS.length - 1];
  if (!first || !last) throw new Error("STOPS must not be empty");
  if (height <= first.height) return first.theme;
  if (height >= last.height) return last.theme;

  for (let i = 0; i < STOPS.length - 1; i++) {
    const a = STOPS[i];
    const b = STOPS[i + 1];
    if (!a || !b) continue;
    if (height >= a.height && height <= b.height) {
      const t = (height - a.height) / (b.height - a.height);
      return {
        name: t < 0.5 ? a.theme.name : b.theme.name,
        skyTop: lerpColor(a.theme.skyTop, b.theme.skyTop, t),
        skyBottom: lerpColor(a.theme.skyBottom, b.theme.skyBottom, t),
        towerColor: lerpColor(a.theme.towerColor, b.theme.towerColor, t),
        towerAccent: lerpColor(a.theme.towerAccent, b.theme.towerAccent, t),
        particleColor: lerpColor(a.theme.particleColor, b.theme.particleColor, t),
        starOpacity: lerp(a.theme.starOpacity, b.theme.starOpacity, t),
      };
    }
  }
  return last.theme;
}
