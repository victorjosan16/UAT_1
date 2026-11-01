// Original voxel models for Voxel Bloom 3D. Each model returns a Grid + palette.
// Silhouettes are designed to read clearly from the front-right 3/4 view.
import { Grid, rng, connectComponents } from './voxel.mjs';
import { defineAnimals } from './animals.mjs';
import { defineMascots } from './mascots.mjs';

const C = {
  yellow: '#FFD23F', gold: '#F5A623', white: '#F7F4EA', cream: '#FFF1C9', red: '#E8343A', orange: '#FF8A1F',
  black: '#2B2B36', brown: '#8B5A2B', darkBrown: '#5C3A1E', tan: '#D9A86C', pink: '#FF8FB8', hotPink: '#FF4F9A',
  grey: '#9AA3B0', darkGrey: '#5E6673', lightGrey: '#D3D8DF', blue: '#3D8BFF', sky: '#8FD3FF', navy: '#2E4A8C',
  green: '#4CC35A', darkGreen: '#2E8B45', lime: '#A6E35A', purple: '#9B5DE5', lavender: '#C9A7FF', teal: '#2EC4B6',
  mint: '#9BF0D6', choco: '#6B3E26', waffle: '#E0A458', glass: '#BDE8FF',
  rust: '#C9572B', rustDark: '#8E3B1E', ink: '#25232E', elephant: '#A7AEBD', elephantLight: '#C9CEDA',
};

const MODELS = [];
function model(id, name, category, colorNames, X, Y, Z, build) {
  MODELS.push({ id, name, category, colorNames, build: () => { const g = new Grid(X, Y, Z); build(g, Object.fromEntries(colorNames.map((n, i) => [n, i]))); return g; } });
}

// ---------------------------------------------------------------- 100 mascots (model order = mascot number = level)
// 1-10: detailed chibi heroes (animals.mjs); 11-100: parametric templates (mascots.mjs)
defineAnimals(model);
defineMascots(model);

// Converts anim meta from build space to trimmed model space; eyes become front-most cell indices.
function exportAnim(a, g) {
  if (!a) return null;
  const [ox, oy, oz] = g.offset;
  const eyes = [];
  for (const [x0, y0] of a.eyes || []) {
    const x = x0 - ox, y = y0 - oy;
    let found = -1;
    for (let z = 0; z < g.Z; z++) if (g.get(x, y, z) >= 0) { found = g.i(x, y, z); break; }
    eyes.push(found); // keep positions so eye rows/columns stay aligned
  }
  const t = a.tail;
  return { headY: a.headY - oy, lid: a.lid, eyes, tail: t ? [t[0] - ox, t[1] - oy, t[2] - oz, t[3] - ox, t[4] - oy, t[5] - oz] : null };
}

export function buildAll() {
  return MODELS.map((m) => {
    const raw = m.build(); const bridges = connectComponents(raw); const g = raw.trim();
    return { id: m.id, name: m.name, category: m.category, palette: m.colorNames.map((n) => C[n]), paletteNames: m.colorNames, grid: g, bridges, anim: exportAnim(raw.anim, g) };
  });
}
export { C as COLORS };
export const MASCOT_ORDER = () => MODELS.map((m) => m.id);
