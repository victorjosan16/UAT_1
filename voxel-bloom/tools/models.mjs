// Original voxel models for Voxel Bloom 3D. Each model returns a Grid + palette.
// Silhouettes are designed to read clearly from the front-right 3/4 view.
import { Grid, rng } from './voxel.mjs';

const C = {
  yellow: '#FFD23F', gold: '#F5A623', white: '#F7F4EA', cream: '#FFF1C9', red: '#E8343A', orange: '#FF8A1F',
  black: '#2B2B36', brown: '#8B5A2B', darkBrown: '#5C3A1E', tan: '#D9A86C', pink: '#FF8FB8', hotPink: '#FF4F9A',
  grey: '#9AA3B0', darkGrey: '#5E6673', lightGrey: '#D3D8DF', blue: '#3D8BFF', sky: '#8FD3FF', navy: '#2E4A8C',
  green: '#4CC35A', darkGreen: '#2E8B45', lime: '#A6E35A', purple: '#9B5DE5', lavender: '#C9A7FF', teal: '#2EC4B6',
  mint: '#9BF0D6', choco: '#6B3E26', waffle: '#E0A458', glass: '#BDE8FF',
};

const MODELS = [];
function model(id, name, category, colorNames, X, Y, Z, build) {
  MODELS.push({ id, name, category, colorNames, build: () => { const g = new Grid(X, Y, Z); build(g, Object.fromEntries(colorNames.map((n, i) => [n, i]))); return g; } });
}

// ---------------------------------------------------------------- Animals
model('chicken', 'Chicken', 'Animals', ['yellow', 'white', 'red', 'orange', 'black'], 14, 18, 14, (g, k) => {
  g.ell(7, 6, 7, 4.6, 4.3, 4.8, k.yellow);
  g.ell(7, 11.5, 6, 3.4, 3.4, 3.2, k.yellow);
  g.ell(7, 5, 3, 3.4, 3.2, 2.4, k.white, 'paint');
  g.ell(2.4, 6.5, 7.5, 1.3, 2.6, 3, k.white); g.ell(11.6, 6.5, 7.5, 1.3, 2.6, 3, k.white);
  g.box(6, 8, 11, 8, 10, 12, k.white); g.box(7, 10, 12, 7, 11, 12, k.white);
  g.box(6, 10, 1, 8, 11, 2, k.orange); g.box(7, 10, 0, 7, 10, 0, k.orange);
  g.box(7, 9, 2, 7, 9, 2, k.red);
  g.front(5, 12, k.black); g.front(9, 12, k.black);
  g.box(6, 15, 4, 8, 15, 7, k.red); g.box(7, 16, 4, 7, 16, 6, k.red); g.box(6, 16, 6, 6, 16, 6, k.red);
  g.box(5, 0, 6, 5, 2, 6, k.orange); g.box(9, 0, 6, 9, 2, 6, k.orange);
  g.box(4, 0, 4, 6, 0, 5, k.orange); g.box(8, 0, 4, 10, 0, 5, k.orange);
});

model('fox', 'Fox', 'Animals', ['orange', 'white', 'black', 'darkBrown'], 18, 18, 14, (g, k) => {
  g.ell(7, 5.5, 7, 3.8, 4.5, 3.6, k.orange);
  g.ell(7, 5, 4, 2.4, 3.4, 1.6, k.white, 'paint');
  g.ell(7, 11.5, 6, 3.6, 3, 3.2, k.orange);
  g.box(5, 9, 1, 9, 11, 3, k.orange); g.box(6, 9, 0, 8, 10, 0, k.white); g.box(5, 9, 1, 9, 9, 3, k.white, 'paint');
  g.box(7, 11, 0, 7, 11, 0, k.black);
  g.front(5, 12, k.black); g.front(9, 12, k.black);
  for (let i = 0; i < 4; i++) { g.box(4 + (i > 1 ? 0 : 0), 14 + i, 5, 5, 14 + i, 6, k.orange); g.box(9, 14 + i, 5, 10, 14 + i, 6, k.orange); }
  g.box(4, 17, 5, 4, 17, 6, k.black); g.box(10, 17, 5, 10, 17, 6, k.black);
  g.box(4, 14, 5, 4, 15, 5, k.darkBrown, 'paint'); g.box(10, 14, 5, 10, 15, 5, k.darkBrown, 'paint');
  g.box(5, 0, 4, 6, 2, 5, k.black); g.box(8, 0, 4, 9, 2, 5, k.black);
  // big curled tail to the right
  g.ell(13, 3.5, 8, 3.2, 2.4, 2.4, k.orange); g.ell(15, 6.5, 8, 1.8, 3, 2, k.orange);
  g.ell(15.5, 9, 8, 1.6, 1.6, 1.6, k.white);
});

model('cat', 'Cat', 'Animals', ['grey', 'white', 'pink', 'black', 'darkGrey'], 16, 18, 12, (g, k) => {
  g.ell(7, 5, 6, 3.6, 4.6, 3.4, k.grey);
  g.ell(7, 4.5, 3.4, 2.2, 3, 1.4, k.white, 'paint');
  g.ell(7, 11.5, 5.5, 3.8, 3.2, 3.2, k.grey);
  for (let y = 2; y < 15; y += 3) { g.box(3, y, 4, 11, y, 9, k.darkGrey, 'paint'); }
  g.box(6, 10, 2, 8, 11, 2, k.white, 'paint'); g.box(5, 10, 2, 9, 10, 2, k.white, 'paint');
  g.box(7, 11, 1, 7, 11, 1, k.pink);
  g.front(5, 12, k.black); g.front(9, 12, k.black);
  g.box(4, 14, 4, 5, 14, 6, k.grey); g.box(4, 15, 5, 4, 16, 5, k.grey); g.box(5, 15, 5, 5, 15, 5, k.pink);
  g.box(9, 14, 4, 10, 14, 6, k.grey); g.box(10, 15, 5, 10, 16, 5, k.grey); g.box(9, 15, 5, 9, 15, 5, k.pink);
  g.box(5, 0, 3, 6, 1, 4, k.white); g.box(8, 0, 3, 9, 1, 4, k.white);
  // tail rising behind on the right
  g.box(11, 1, 9, 13, 1, 9, k.grey); g.box(13, 1, 9, 13, 8, 9, k.grey); g.box(13, 9, 9, 14, 10, 9, k.darkGrey);
});

model('bear', 'Bear', 'Animals', ['brown', 'tan', 'black', 'darkBrown'], 15, 17, 13, (g, k) => {
  g.ell(7, 5, 6.5, 4.6, 4.8, 4.2, k.brown);
  g.ell(7, 4.5, 3, 3, 3.2, 1.8, k.tan, 'paint');
  g.ell(7, 11.5, 6, 4, 3.6, 3.6, k.brown);
  g.ell(7, 10.2, 2.6, 2.2, 1.6, 1.4, k.tan);
  g.box(7, 11, 1, 7, 11, 1, k.black); g.box(6, 11, 1, 8, 11, 1, k.black, 'paint');
  g.front(5, 13, k.black); g.front(9, 13, k.black);
  g.ell(3.6, 14.6, 6, 1.6, 1.6, 1.2, k.brown); g.ell(10.4, 14.6, 6, 1.6, 1.6, 1.2, k.brown);
  g.front(4, 15, k.darkBrown); g.front(10, 15, k.darkBrown);
  g.ell(2.4, 6, 4.5, 1.3, 2.4, 1.4, k.brown); g.ell(11.6, 6, 4.5, 1.3, 2.4, 1.4, k.brown);
  g.ell(4.5, 1.2, 3, 1.6, 1.2, 1.8, k.brown); g.ell(9.5, 1.2, 3, 1.6, 1.2, 1.8, k.brown);
  g.front(4, 1, k.tan); g.front(10, 1, k.tan);
});

model('bunny', 'Bunny', 'Animals', ['white', 'pink', 'black', 'hotPink'], 14, 22, 12, (g, k) => {
  g.ell(7, 5, 6, 3.8, 4.4, 3.6, k.white);
  g.ell(7, 11, 5.5, 3.6, 3.2, 3.2, k.white);
  g.box(5, 13, 5, 6, 20, 6, k.white); g.box(8, 13, 5, 9, 20, 6, k.white);
  g.box(5, 14, 4, 6, 19, 4, k.pink); g.box(8, 14, 4, 9, 19, 4, k.pink);
  g.box(5, 21, 5, 6, 21, 5, k.white); g.box(8, 21, 5, 9, 21, 5, k.white);
  g.front(5, 11, k.black); g.front(9, 11, k.black);
  g.front(7, 10, k.hotPink); g.front(4, 9, k.pink); g.front(10, 9, k.pink);
  g.ell(7, 4, 10, 1.6, 1.6, 1.4, k.white);
  g.ell(4.5, 1, 3, 1.4, 1, 2, k.white); g.ell(9.5, 1, 3, 1.4, 1, 2, k.white);
});

model('panda', 'Panda', 'Animals', ['white', 'black', 'pink', 'green'], 16, 17, 13, (g, k) => {
  g.ell(7.5, 5, 6.5, 4.6, 4.8, 4.2, k.white);
  g.box(2, 0, 2, 13, 4, 10, k.black, 'paint'); g.box(2, 6, 2, 13, 7, 10, k.black, 'paint');
  g.ell(7.5, 11.5, 6, 4.2, 3.6, 3.6, k.white);
  g.ell(3.6, 14.8, 6, 1.6, 1.6, 1.2, k.black); g.ell(11.4, 14.8, 6, 1.6, 1.6, 1.2, k.black);
  for (const x of [5, 10]) { g.front(x, 12, k.black); g.front(x, 11, k.black); g.front(x + (x < 7 ? 1 : -1), 12, k.black); }
  g.front(7, 10, k.black); g.front(8, 10, k.black);
  g.front(4, 10, k.pink); g.front(11, 10, k.pink);
  // bamboo
  g.box(13, 0, 3, 13, 9, 3, k.green); g.box(14, 7, 3, 15, 7, 3, k.green); g.box(12, 9, 3, 12, 10, 3, k.green);
});

model('penguin', 'Penguin', 'Animals', ['black', 'white', 'orange', 'navy'], 14, 17, 12, (g, k) => {
  g.ell(7, 7, 6, 4.4, 6.8, 4, k.black);
  g.ell(7, 6, 3.2, 3.2, 5, 2, k.white, 'paint');
  g.ell(7, 11.5, 3.6, 2.6, 2.2, 1.6, k.white, 'paint');
  g.box(6, 10, 1, 8, 10, 1, k.orange); g.box(7, 10, 0, 7, 10, 0, k.orange);
  g.front(5, 12, k.navy); g.front(9, 12, k.navy);
  g.box(2, 4, 5, 2, 9, 7, k.black); g.box(12, 4, 5, 12, 9, 7, k.black); g.box(1, 4, 6, 1, 6, 6, k.black); g.box(13, 4, 6, 13, 6, 6, k.black);
  g.box(4, 0, 2, 6, 0, 4, k.orange); g.box(8, 0, 2, 10, 0, 4, k.orange);
  g.box(6, 13, 8, 8, 14, 9, k.navy, 'paint');
});

model('sheep', 'Sheep', 'Animals', ['white', 'black', 'cream', 'pink'], 18, 15, 13, (g, k) => {
  const r = rng(7);
  g.ell(9, 7, 6.5, 6, 4, 4.4, k.white);
  for (let i = 0; i < 26; i++) g.ell(3.5 + r() * 11, 4.5 + r() * 6, 2.8 + r() * 7.5, 1.6, 1.6, 1.6, r() < 0.25 ? k.cream : k.white);
  g.ell(2.6, 8.4, 3.4, 2.2, 2.6, 2.4, k.black);
  g.box(0, 10, 3, 0, 10, 4, k.black); g.box(5, 10, 3, 5, 10, 4, k.black);
  g.ell(2.6, 11, 3.6, 2.6, 1.4, 2.2, k.cream);
  g.front(2, 9, k.white); g.front(4, 9, k.white); g.front(3, 7, k.pink);
  for (const [x, z] of [[5, 4], [12, 4], [5, 9], [12, 9]]) g.box(x, 0, z, x + 1, 3, z + 1, k.black);
});

model('wolf', 'Wolf', 'Animals', ['grey', 'white', 'black', 'darkGrey'], 18, 19, 14, (g, k) => {
  g.ell(7, 6, 7, 3.8, 5, 3.6, k.grey);
  g.ell(7, 5.5, 4, 2.4, 3.6, 1.6, k.white, 'paint');
  g.ell(7, 12, 6, 3.6, 3, 3.2, k.grey);
  g.box(5, 10, 0, 9, 12, 3, k.grey); g.box(5, 10, 0, 9, 10, 3, k.white); g.box(6, 11, 0, 8, 11, 1, k.white, 'paint');
  g.box(7, 12, 0, 7, 12, 0, k.black);
  g.front(5, 13, k.black); g.front(9, 13, k.black);
  g.box(4, 13, 6, 5, 13, 6, k.darkGrey, 'paint'); g.box(9, 13, 6, 10, 13, 6, k.darkGrey, 'paint');
  for (let i = 0; i < 4; i++) { g.box(4, 14 + i, 5, 5 - (i > 2 ? 1 : 0), 14 + i, 6, k.grey); g.box(9 + (i > 2 ? 1 : 0), 14 + i, 5, 10, 14 + i, 6, k.grey); }
  g.box(4, 18, 5, 4, 18, 5, k.darkGrey); g.box(10, 18, 5, 10, 18, 5, k.darkGrey);
  g.box(5, 0, 4, 6, 2, 5, k.darkGrey); g.box(8, 0, 4, 9, 2, 5, k.darkGrey);
  g.ell(13, 3, 9, 3.4, 1.8, 2, k.grey); g.ell(16, 4.5, 9, 1.6, 2, 1.6, k.darkGrey);
});

model('deer', 'Deer', 'Animals', ['brown', 'white', 'tan', 'black'], 18, 22, 10, (g, k) => {
  g.ell(9, 8, 5, 6, 2.8, 2.8, k.brown);
  for (const [x, z] of [[4, 3], [13, 3], [4, 6], [13, 6]]) g.box(x, 0, z, x + 1, 6, z + 1, k.brown);
  for (const [x, z] of [[4, 3], [13, 3], [4, 6], [13, 6]]) g.box(x, 0, z, x + 1, 0, z + 1, k.black);
  g.box(2, 9, 4, 4, 13, 6, k.brown);
  g.ell(2.5, 14.5, 4.5, 2.4, 2, 2.2, k.brown); g.box(0, 13, 4, 1, 14, 5, k.brown); g.box(0, 13, 3, 0, 13, 3, k.black);
  g.front(1, 15, k.black); g.front(4, 15, k.black);
  g.box(8, 9, 2, 13, 9, 2, k.white, 'paint'); g.box(9, 7, 2, 12, 7, 2, k.white, 'paint');
  g.box(3, 10, 3, 4, 12, 3, k.tan, 'paint');
  g.box(15, 9, 4, 15, 10, 6, k.white);
  // antlers
  for (const x of [1, 4]) { g.box(x, 17, 5, x, 20, 5, k.tan); g.box(x - 1, 19, 5, x - 1, 21, 5, k.tan); g.box(x + 1, 18, 5, x + 1, 19, 5, k.tan); }
});

model('lion', 'Lion', 'Animals', ['yellow', 'orange', 'darkBrown', 'black', 'cream'], 18, 17, 14, (g, k) => {
  g.ell(9.5, 5, 7, 5.5, 3.2, 3.2, k.yellow);
  for (const [x, z] of [[5, 5], [13, 5], [5, 8], [13, 8]]) g.box(x, 0, z, x + 1, 3, z + 1, k.yellow);
  g.ell(5, 9.5, 5, 4.6, 4.6, 3, k.orange);
  g.ell(5, 9.5, 5, 4.6, 4.6, 3, k.darkBrown, 'paint'); g.ell(5, 9.5, 4, 4, 4, 2.6, k.orange, 'paint');
  g.ell(5, 9.5, 3, 2.8, 2.8, 2.4, k.yellow);
  g.box(4, 8, 0, 6, 9, 1, k.cream); g.box(5, 9, 0, 5, 9, 0, k.black);
  g.front(4, 11, k.black); g.front(6, 11, k.black);
  g.box(15, 4, 7, 16, 4, 7, k.yellow); g.box(17, 4, 7, 17, 7, 7, k.yellow); g.box(17, 8, 7, 17, 8, 7, k.darkBrown);
});

model('fish', 'Clownfish', 'Animals', ['orange', 'white', 'black', 'blue'], 18, 13, 8, (g, k) => {
  g.ell(8, 6, 4, 6.2, 4.2, 2.6, k.orange);
  for (const x of [5, 10]) g.box(x, 0, 0, x + 1, 12, 8, k.white, 'paint');
  for (const x of [4, 7, 9, 12]) g.box(x, 0, 0, x, 12, 8, k.black, 'paint');
  g.box(14, 4, 3, 15, 8, 5, k.orange); g.box(16, 2, 4, 17, 10, 4, k.orange); g.box(17, 2, 4, 17, 10, 4, k.black);
  g.box(6, 10, 4, 11, 11, 4, k.orange); g.box(8, 12, 4, 9, 12, 4, k.orange);
  g.box(3, 7, 0, 3, 7, 7, k.black, 'paint'); g.front(3, 7, k.black);
  g.box(7, 3, 1, 8, 4, 1, k.orange); g.box(7, 3, 7, 8, 4, 7, k.orange);
});

model('crab', 'Crab', 'Animals', ['red', 'orange', 'white', 'black'], 22, 13, 14, (g, k) => {
  g.ell(11, 4, 8, 6, 2.6, 4.4, k.red);
  g.ell(11, 2.4, 8, 5, 1.2, 3.6, k.orange, 'paint');
  for (const x of [9, 13]) { g.box(x, 6, 5, x, 8, 5, k.red); g.box(x, 9, 5, x, 9, 5, k.white); g.box(x, 9, 4, x, 9, 4, k.black); }
  g.box(10, 4, 3, 12, 4, 3, k.black, 'paint');
  for (const s of [-1, 1]) {
    const cx = 11 + s * 7;
    g.box(11 + s * 5, 4, 5, cx, 4, 5, k.red); g.box(cx, 4, 3, cx, 5, 4, k.red);
    g.ell(cx, 7, 2.5, 2, 2, 1.6, k.red);
    g.box(cx - 1, 9, 2, cx - 1, 11, 3, k.red); g.box(cx + 1, 9, 2, cx + 1, 10, 3, k.orange);
    for (const z of [7, 9, 11]) { g.box(11 + s * 5, 2, z, 11 + s * 8, 2, z, k.orange); g.box(11 + s * 8, 0, z, 11 + s * 8, 1, z, k.orange); }
  }
});

model('octopus', 'Octopus', 'Animals', ['purple', 'lavender', 'white', 'black'], 18, 16, 18, (g, k) => {
  g.ell(9, 10.5, 9, 4.6, 5, 4.6, k.purple);
  for (let i = 0; i < 8; i++) {
    const a = i / 8 * Math.PI * 2;
    for (let t = 0; t < 7; t++) {
      const r = 3 + t * 0.85, y = Math.max(0, 6 - t + (t > 4 ? 1 : 0));
      g.set(Math.round(9 + Math.cos(a) * r), y, Math.round(9 + Math.sin(a) * r), k.purple);
      g.set(Math.round(9 + Math.cos(a) * r), y + 1, Math.round(9 + Math.sin(a) * r), t % 2 ? k.lavender : k.purple);
    }
  }
  g.ell(9, 6, 9, 3.4, 1.4, 3.4, k.purple);
  for (const x of [7, 11]) { g.front(x, 10, k.white); g.front(x, 11, k.white); g.front(x, 10, k.black); }
  g.box(5, 14, 6, 6, 14, 7, k.lavender, 'paint'); g.box(11, 13, 6, 12, 13, 7, k.lavender, 'paint');
});

model('turtle', 'Turtle', 'Animals', ['green', 'darkGreen', 'lime', 'black', 'tan'], 18, 9, 16, (g, k) => {
  g.ell(9, 2.5, 8, 6.2, 4.6, 5.6, k.green); g.box(0, 0, 0, 17, 1, 15, -1, 'clear');
  g.box(2, 2, 2, 15, 2, 13, k.tan, 'paint');
  for (const [x, z] of [[9, 8], [6, 6], [12, 6], [6, 10], [12, 10]]) g.ell(x, 7, z, 1.5, 1, 1.5, k.darkGreen, 'paint');
  g.ell(9, 3.5, 1.8, 2, 2, 2, k.lime); g.front(8, 4, k.black); g.front(10, 4, k.black);
  for (const [x, z] of [[4, 4], [14, 4], [4, 12], [14, 12]]) g.box(x - 1, 0, z - 1, x, 2, z, k.lime);
  g.box(9, 2, 14, 9, 2, 15, k.lime);
});

model('whale', 'Whale', 'Animals', ['blue', 'sky', 'white', 'black', 'navy'], 20, 14, 10, (g, k) => {
  g.ell(8, 5.5, 5, 7.6, 4.8, 4, k.blue);
  g.ell(8, 3, 3, 6, 2.6, 2.4, k.sky, 'paint');
  for (let x = 14; x < 19; x++) g.box(x, 5 + (x - 14) * 0.6 | 0, 4, x, 6 + (x - 14) * 0.6 | 0, 6, k.blue);
  g.box(18, 8, 2, 19, 9, 8, k.navy);
  g.box(4, 1, 0, 6, 2, 1, k.navy); g.box(4, 1, 9, 6, 2, 9, k.navy);
  g.front(3, 7, k.black); g.box(3, 7, 9, 3, 7, 9, k.black, 'paint');
  g.box(7, 11, 5, 7, 12, 5, k.sky); g.box(6, 13, 4, 8, 13, 6, k.white); g.box(5, 12, 5, 5, 12, 5, k.white); g.box(9, 12, 5, 9, 12, 5, k.white);
});

// ---------------------------------------------------------------- Food
model('cake', 'Strawberry Cake', 'Food', ['pink', 'cream', 'white', 'red', 'green', 'yellow', 'choco'], 16, 17, 16, (g, k) => {
  g.cyl('y', 8, 0, 8, 7, 1, k.lightGrey ?? k.white); g.cyl('y', 8, 0, 8, 7.5, 1, k.white);
  g.cyl('y', 8, 1, 8, 6.5, 4, k.pink); g.cyl('y', 8, 5, 8, 6.5, 1, k.cream);
  g.cyl('y', 8, 6, 8, 6.5, 3, k.pink); g.cyl('y', 8, 9, 8, 6.5, 1, k.white);
  for (let a = 0; a < 16; a++) { const t = a / 16 * Math.PI * 2; g.set(Math.round(8 + Math.cos(t) * 6.3), 8, Math.round(8 + Math.sin(t) * 6.3), a % 2 ? k.white : k.pink); }
  for (let a = 0; a < 6; a++) { const t = a / 6 * Math.PI * 2; const x = Math.round(8 + Math.cos(t) * 4.5), z = Math.round(8 + Math.sin(t) * 4.5); g.set(x, 10, z, k.red); g.set(x, 11, z, k.red); g.set(x, 12, z, k.green); }
  for (const [x, z] of [[7, 7], [9, 9], [8, 6]]) { g.box(x, 10, z, x, 13, z, z === 6 ? k.cream : k.pink); g.set(x, 14, z, k.yellow); g.set(x, 15, z, k.yellow); }
  g.box(3, 3, 2, 13, 3, 2, k.cream, 'paint');
});

model('icecream', 'Ice Cream', 'Food', ['waffle', 'tan', 'pink', 'mint', 'choco', 'red', 'white'], 12, 22, 12, (g, k) => {
  g.cyl('y', 6, 0, 6, 0.6, 10, k.waffle, 'set', 3.6);
  for (let y = 0; y < 10; y++) for (let x = 0; x < 12; x++) for (let z = 0; z < 12; z++) if ((x + y + z) % 3 === 0) g.paint(x, y, z, k.tan);
  g.ell(6, 11, 6, 4.4, 2.6, 4.4, k.mint);
  g.ell(6, 14.5, 6, 4, 2.6, 4, k.choco);
  g.ell(6, 17.5, 6, 3.6, 2.6, 3.6, k.pink);
  g.box(5, 20, 5, 7, 20, 7, k.white); g.set(6, 21, 6, k.red);
  for (const [x, y] of [[4, 18], [7, 17], [5, 16], [8, 15]]) g.addFront(x, y, k.white);
});

model('candy', 'Candy', 'Food', ['hotPink', 'white', 'purple', 'yellow'], 22, 11, 10, (g, k) => {
  g.ell(11, 5, 5, 5, 4.6, 4.2, k.hotPink);
  for (let x = 6; x <= 16; x++) for (let y = 0; y < 11; y++) if (((x + y) % 4) < 2) for (let z = 0; z < 10; z++) g.paint(x, y, z, k.white);
  for (const s of [-1, 1]) {
    g.box(11 + s * 6, 4, 4, 11 + s * 6, 6, 6, k.purple);
    for (let t = 0; t < 3; t++) { const x = 11 + s * (7 + t); const w = t >> 1; g.box(x, 4 - t + w, 4 - w, x, 6 + t - w, 6 + w, t === 2 ? k.yellow : k.purple); }
  }
});

model('donut', 'Donut', 'Food', ['tan', 'pink', 'white', 'blue', 'yellow', 'green'], 18, 7, 18, (g, k) => {
  const r = rng(11);
  for (let x = 0; x < 18; x++) for (let z = 0; z < 18; z++) for (let y = 0; y < 7; y++) {
    const d = Math.hypot(x - 8.5, z - 8.5), t = Math.hypot(d - 5.5, (y - 3) * 1.15);
    if (t <= 3.1) g.set(x, y, z, y >= 4 || (y === 3 && r() < 0.6) ? k.pink : k.tan);
  }
  for (let i = 0; i < 40; i++) { const x = Math.floor(r() * 18), z = Math.floor(r() * 18); for (let y = 6; y >= 0; y--) if (g.get(x, y, z) === k.pink) { g.set(x, y, z, [k.white, k.blue, k.yellow, k.green][i % 4]); break; } }
});

model('cupcake', 'Cupcake', 'Food', ['purple', 'pink', 'white', 'red', 'green', 'lavender'], 14, 18, 14, (g, k) => {
  g.cyl('y', 7, 0, 7, 4.2, 7, k.purple, 'set', 5.4);
  for (let a = 0; a < 24; a++) { const t = a / 24 * Math.PI * 2; if (a % 2) for (let y = 0; y < 7; y++) g.paint(Math.round(7 + Math.cos(t) * 5), y, Math.round(7 + Math.sin(t) * 5), k.lavender); }
  g.ell(7, 8.5, 7, 6, 2.4, 6, k.pink); g.ell(7, 11, 7, 4.8, 2.2, 4.8, k.pink); g.ell(7, 13, 7, 3.2, 1.8, 3.2, k.pink);
  for (let i = 0; i < 8; i++) { const t = i / 8 * Math.PI * 2; g.set(Math.round(7 + Math.cos(t) * 4.4), 10 + (i % 2), Math.round(7 + Math.sin(t) * 4.4), k.white); }
  g.ell(7, 15.5, 7, 1.6, 1.6, 1.6, k.red); g.set(7, 17, 7, k.green); g.set(8, 17, 7, k.green);
});

model('watermelon', 'Watermelon', 'Food', ['red', 'white', 'green', 'darkGreen', 'black'], 20, 12, 6, (g, k) => {
  for (let x = 0; x < 20; x++) for (let y = 0; y < 12; y++) {
    const d = Math.hypot(x - 9.5, (y - 11.5) * 1.0);
    if (y <= 11 && d <= 10.5) for (let z = 0; z < 6; z++) g.set(x, y, z, d > 9.6 ? k.darkGreen : d > 8.6 ? k.green : d > 7.8 ? k.white : k.red);
  }
  for (const [x, y] of [[6, 7], [9, 5], [12, 7], [8, 9], [11, 9], [14, 9], [5, 9]]) { g.front(x, y, k.black); g.box(x, y, 5, x, y, 5, k.black, 'paint'); }
});

model('burger', 'Burger', 'Food', ['waffle', 'tan', 'choco', 'yellow', 'green', 'red', 'cream'], 16, 13, 16, (g, k) => {
  g.cyl('y', 8, 0, 8, 6.2, 2, k.waffle);
  g.cyl('y', 8, 2, 8, 6.6, 2, k.choco);
  g.cyl('y', 8, 4, 8, 7, 1, k.yellow); g.box(1, 3, 7, 1, 3, 8, k.yellow);
  g.cyl('y', 8, 5, 8, 6.8, 1, k.red); g.cyl('y', 8, 6, 8, 7.2, 1, k.green);
  g.ell(8, 7.5, 8, 6.6, 4.6, 6.6, k.waffle); g.box(0, 0, 0, 15, 6, 15, k.waffle, 'paint');
  g.cyl('y', 8, 0, 8, 6.2, 2, k.waffle); g.cyl('y', 8, 2, 8, 6.6, 2, k.choco); g.cyl('y', 8, 4, 8, 7, 1, k.yellow); g.cyl('y', 8, 5, 8, 6.8, 1, k.red); g.cyl('y', 8, 6, 8, 7.2, 1, k.green);
  for (const [x, z] of [[6, 5], [10, 6], [8, 9], [5, 9], [11, 10]]) for (let y = 12; y > 6; y--) if (g.get(x, y, z) >= 0) { g.set(x, y, z, k.cream); break; }
});

// ---------------------------------------------------------------- Vehicles
model('car', 'Red Car', 'Vehicles', ['red', 'glass', 'black', 'lightGrey', 'yellow', 'white'], 20, 11, 11, (g, k) => {
  g.box(0, 2, 1, 19, 5, 9, k.red);
  g.box(5, 6, 2, 14, 9, 8, k.red); g.box(6, 6, 1, 13, 8, 9, k.glass, 'paint'); g.box(4, 6, 2, 15, 8, 8, k.glass, 'paint');
  g.box(5, 6, 2, 14, 9, 8, k.red, 'paint'); g.box(6, 6, 2, 13, 8, 2, k.glass, 'paint'); g.box(6, 6, 8, 13, 8, 8, k.glass, 'paint'); g.box(5, 6, 3, 5, 8, 7, k.glass, 'paint'); g.box(14, 6, 3, 14, 8, 7, k.glass, 'paint');
  g.box(9, 6, 2, 10, 9, 2, k.red, 'paint'); g.box(9, 6, 8, 10, 9, 8, k.red, 'paint');
  for (const [x, z] of [[3, 0], [15, 0], [3, 9], [15, 9]]) { g.box(x - 1, 0, z, x + 2, 3, z + 1, k.black); g.box(x, 1, z, x + 1, 2, z + 1, k.lightGrey, 'paint'); }
  g.box(0, 4, 2, 0, 4, 3, k.yellow); g.box(0, 4, 7, 0, 4, 8, k.yellow); g.box(19, 4, 2, 19, 4, 3, k.red); g.box(0, 2, 2, 0, 2, 8, k.lightGrey, 'paint'); g.box(19, 2, 2, 19, 2, 8, k.lightGrey, 'paint');
  g.box(8, 5, 1, 8, 5, 1, k.white, 'paint');
});

model('bus', 'School Bus', 'Vehicles', ['yellow', 'glass', 'black', 'lightGrey', 'red', 'orange'], 24, 13, 11, (g, k) => {
  g.box(0, 2, 1, 23, 11, 9, k.yellow); g.box(0, 2, 1, 3, 6, 9, k.yellow); g.box(0, 7, 1, 2, 11, 9, -1, 'clear');
  g.box(3, 7, 2, 3, 10, 8, k.glass, 'paint');
  for (let x = 5; x < 22; x += 4) { g.box(x, 7, 1, x + 2, 9, 1, k.glass, 'paint'); g.box(x, 7, 9, x + 2, 9, 9, k.glass, 'paint'); }
  g.box(0, 5, 1, 23, 5, 9, k.black, 'paint'); g.box(0, 2, 1, 23, 2, 9, k.orange, 'paint');
  for (const [x, z] of [[3, 0], [18, 0], [3, 9], [18, 9]]) { g.box(x - 1, 0, z, x + 2, 3, z + 1, k.black); g.box(x, 1, z, x + 1, 2, z + 1, k.lightGrey, 'paint'); }
  g.box(0, 4, 2, 0, 4, 2, k.lightGrey); g.box(0, 4, 8, 0, 4, 8, k.lightGrey); g.box(23, 10, 2, 23, 10, 2, k.red); g.box(23, 10, 8, 23, 10, 8, k.red);
  g.box(4, 12, 3, 21, 12, 7, k.lightGrey);
});

model('truck', 'Fire Truck', 'Vehicles', ['red', 'glass', 'black', 'lightGrey', 'white', 'blue'], 26, 13, 11, (g, k) => {
  g.box(0, 2, 1, 25, 6, 9, k.red); g.box(0, 7, 1, 6, 11, 9, k.red);
  g.box(0, 8, 2, 0, 10, 8, k.glass, 'paint'); g.box(1, 8, 1, 5, 10, 1, k.glass, 'paint'); g.box(1, 8, 9, 5, 10, 9, k.glass, 'paint');
  g.box(2, 12, 4, 4, 12, 6, k.blue); g.box(3, 12, 3, 3, 12, 7, k.red);
  g.box(8, 7, 3, 24, 7, 7, k.lightGrey); for (let x = 8; x <= 24; x += 2) g.box(x, 8, 3, x, 8, 7, k.white); g.box(8, 8, 3, 24, 8, 3, k.white); g.box(8, 8, 7, 24, 8, 7, k.white);
  g.box(7, 4, 1, 25, 4, 9, k.white, 'paint');
  for (const [x, z] of [[3, 0], [15, 0], [21, 0], [3, 9], [15, 9], [21, 9]]) { g.box(x - 1, 0, z, x + 2, 3, z + 1, k.black); g.box(x, 1, z, x + 1, 2, z + 1, k.lightGrey, 'paint'); }
});

model('rocket', 'Rocket', 'Vehicles', ['white', 'red', 'blue', 'glass', 'orange', 'yellow', 'lightGrey'], 14, 26, 14, (g, k) => {
  g.cyl('y', 7, 4, 7, 3.6, 14, k.white);
  g.cyl('y', 7, 18, 7, 3.4, 6, k.red, 'set', 0.6);
  g.cyl('y', 7, 4, 7, 3.6, 1, k.red); g.cyl('y', 7, 9, 7, 3.6, 1, k.lightGrey);
  g.box(6, 13, 3, 8, 15, 3, k.blue); g.box(7, 14, 3, 7, 14, 3, k.glass);
  for (const [dx, dz] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) for (let t = 0; t < 4; t++) g.box(7 + dx * (4 + (t >> 1)), 1 + t, 7 + dz * (4 + (t >> 1)), 7 + dx * 4, 6 - t, 7 + dz * 4, k.red);
  g.cyl('y', 7, 2, 7, 2, 2, k.lightGrey); g.cyl('y', 7, 0, 7, 1.6, 2, k.orange); g.set(7, 0, 7, k.yellow); g.set(6, 1, 7, k.yellow);
});

// ---------------------------------------------------------------- Buildings
model('house', 'Cozy House', 'Buildings', ['cream', 'red', 'brown', 'glass', 'green', 'darkGrey', 'white'], 18, 18, 16, (g, k) => {
  g.box(0, 0, 0, 17, 0, 15, k.green);
  g.box(2, 1, 3, 15, 8, 13, k.cream);
  for (let i = 0; i < 7; i++) g.box(1, 9 + i, 2 + i, 16, 9 + i, 14 - i, k.red);
  g.box(2, 9, 3, 15, 15, 13, k.red, 'paint');
  for (let i = 0; i < 7; i++) { g.box(2, 9 + i, 3 + i, 2, 9 + i, 13 - i, k.cream); g.box(15, 9 + i, 3 + i, 15, 9 + i, 13 - i, k.cream); }
  g.box(12, 12, 10, 13, 17, 11, k.darkGrey);
  g.box(7, 1, 3, 10, 5, 3, k.brown); g.box(9, 3, 2, 9, 3, 2, k.white, 'paint');
  for (const x of [3, 12]) { g.box(x, 4, 3, x + 2, 6, 3, k.glass, 'paint'); g.box(x - 0, 3, 2, x + 2, 3, 2, k.brown); }
  g.box(15, 3, 6, 15, 6, 10, k.glass, 'paint'); g.box(6, 0, 0, 11, 0, 2, k.white, 'paint');
  for (const [x, z] of [[1, 1], [16, 1]]) g.ell(x, 2, z, 1.4, 1.6, 1.4, k.green);
});

model('lighthouse', 'Lighthouse', 'Buildings', ['white', 'red', 'glass', 'darkGrey', 'yellow', 'grey'], 14, 26, 14, (g, k) => {
  g.cyl('y', 7, 0, 7, 6.2, 2, k.grey);
  g.cyl('y', 7, 2, 7, 4.6, 16, k.white, 'set', 3.2);
  for (const y of [5, 6, 7, 11, 12, 13]) g.cyl('y', 7, y, 7, 5, 1, k.red, 'paint');
  g.cyl('y', 7, 18, 7, 4.4, 1, k.darkGrey);
  g.cyl('y', 7, 19, 7, 2.6, 3, k.glass); g.box(6, 19, 6, 8, 21, 8, k.yellow);
  g.cyl('y', 7, 22, 7, 3.2, 3, k.red, 'set', 0.5);
  g.box(6, 2, 2, 8, 5, 2, k.darkGrey, 'paint'); g.front(7, 9, k.glass); g.front(7, 15, k.glass);
});

model('windmill', 'Windmill', 'Buildings', ['tan', 'brown', 'white', 'red', 'glass', 'green', 'darkBrown'], 20, 26, 12, (g, k) => {
  g.box(0, 0, 0, 19, 0, 11, k.green);
  g.cyl('y', 10, 1, 6, 4.6, 13, k.tan, 'set', 3.2);
  g.cyl('y', 10, 14, 6, 3.6, 4, k.red, 'set', 0.6);
  g.box(9, 1, 1, 11, 4, 2, k.darkBrown); g.front(10, 9, k.glass); g.front(10, 10, k.glass);
  g.box(10, 13, 1, 10, 13, 1, k.brown);
  for (let t = -9; t <= 9; t++) { if (Math.abs(t) < 1) continue; g.set(10 + t, 13 + t, 0, k.brown); g.set(10 + t, 13 - t, 0, k.brown); }
  for (let t = 3; t <= 9; t++) { const s = [[1, 1], [1, -1], [-1, 1], [-1, -1]]; for (const [a, b] of s) { g.set(10 + a * t + (a * b > 0 ? -1 : 1) * 0, 13 + b * t - b, 0, k.white); g.set(10 + a * t - a, 13 + b * t, 0, k.white); } }
});

model('castle', 'Dream Castle', 'Buildings', ['lavender', 'pink', 'purple', 'glass', 'yellow', 'white', 'darkBrown'], 22, 26, 16, (g, k) => {
  g.box(3, 0, 4, 18, 9, 13, k.lavender);
  for (let x = 3; x <= 18; x += 2) { g.set(x, 10, 4, k.lavender); g.set(x, 10, 13, k.lavender); }
  for (const [x, z, h] of [[3, 4, 13], [18, 4, 13], [3, 13, 13], [18, 13, 13], [10.5, 8.5, 18]]) {
    g.cyl('y', Math.round(x), 0, Math.round(z), x === 10.5 ? 3 : 2.4, h, x === 10.5 ? k.white : k.lavender);
    g.cyl('y', Math.round(x), h, Math.round(z), x === 10.5 ? 3.6 : 3, x === 10.5 ? 7 : 5, k.pink, 'set', 0.4);
    g.set(Math.round(x), h + (x === 10.5 ? 7 : 5), Math.round(z), k.yellow);
  }
  g.box(9, 0, 3, 12, 5, 3, k.darkBrown); g.box(10, 6, 3, 11, 6, 3, k.darkBrown);
  for (const x of [5, 15]) g.box(x, 5, 3, x + 1, 7, 3, k.glass, 'paint');
  g.front(10, 13, k.glass); g.front(11, 13, k.glass); g.front(10, 14, k.glass); g.front(11, 14, k.glass);
  g.box(3, 9, 4, 18, 9, 4, k.purple, 'paint');
});

// ---------------------------------------------------------------- Dream
model('robot', 'Robo Buddy', 'Vehicles', ['lightGrey', 'blue', 'sky', 'red', 'yellow', 'darkGrey'], 18, 22, 10, (g, k) => {
  g.box(6, 0, 3, 7, 4, 6, k.darkGrey); g.box(10, 0, 3, 11, 4, 6, k.darkGrey); g.box(5, 0, 2, 7, 1, 6, k.blue); g.box(10, 0, 2, 12, 1, 6, k.blue);
  g.box(4, 5, 2, 13, 12, 7, k.lightGrey); g.box(6, 7, 1, 11, 10, 1, k.sky); g.box(7, 9, 1, 7, 9, 1, k.red); g.box(9, 8, 1, 10, 8, 1, k.yellow);
  g.box(2, 7, 3, 3, 12, 5, k.blue); g.box(14, 7, 3, 15, 12, 5, k.blue); g.box(1, 5, 3, 3, 6, 5, k.darkGrey); g.box(14, 5, 3, 16, 6, 5, k.darkGrey);
  g.box(8, 13, 4, 9, 13, 5, k.darkGrey);
  g.box(5, 14, 2, 12, 19, 7, k.lightGrey); g.box(6, 15, 1, 11, 18, 1, k.blue);
  g.box(7, 17, 0, 7, 17, 0, k.sky); g.box(10, 17, 0, 10, 17, 0, k.sky); g.box(7, 15, 0, 10, 15, 0, k.yellow);
  g.box(8, 20, 4, 9, 20, 5, k.darkGrey); g.box(8, 21, 4, 9, 21, 5, k.red);
  g.box(4, 16, 4, 4, 17, 5, k.red); g.box(13, 16, 4, 13, 17, 5, k.red);
});

model('dragon', 'Baby Dragon', 'Animals', ['green', 'lime', 'yellow', 'red', 'white', 'black', 'purple'], 22, 20, 18, (g, k) => {
  g.ell(9, 6, 9, 4.6, 4.8, 4.2, k.green);
  g.ell(9, 5.5, 5.6, 3, 3.6, 1.4, k.lime, 'paint');
  g.ell(9, 12.5, 7, 3.8, 3.2, 3.4, k.green);
  g.box(7, 10, 2, 11, 12, 4, k.green); g.box(7, 10, 2, 11, 10, 4, k.lime, 'paint'); g.box(8, 12, 2, 8, 12, 2, k.black); g.box(10, 12, 2, 10, 12, 2, k.black);
  g.front(7, 14, k.white); g.front(11, 14, k.white); g.front(7, 14, k.black); g.front(11, 14, k.black);
  g.box(6, 15, 7, 6, 17, 7, k.yellow); g.box(12, 15, 7, 12, 17, 7, k.yellow); g.box(6, 18, 8, 6, 18, 8, k.yellow); g.box(12, 18, 8, 12, 18, 8, k.yellow);
  for (let y = 4; y <= 16; y += 2) g.set(9, y, y < 10 ? 13 : 10.5 | 0, k.red);
  for (const s of [-1, 1]) for (let t = 0; t < 5; t++) g.box(9 + s * (4 + t), 7 + t, 11, 9 + s * (4 + t), 11 + t - (t > 3 ? 1 : 0), 11 + (t > 2 ? 1 : 0), t === 4 ? k.purple : k.purple);
  for (const s of [-1, 1]) for (let t = 0; t < 4; t++) g.box(9 + s * (4 + t), 7 + t, 11, 9 + s * (4 + t), 8 + t, 11, k.green);
  for (let t = 0; t < 7; t++) g.box(9 + (t > 3 ? t - 3 : 0), 1 + (t > 4 ? 1 : 0), 13 + Math.min(t, 4), 10 + (t > 3 ? t - 3 : 0), 2, 13 + Math.min(t, 4), k.green);
  g.box(14, 2, 17, 15, 3, 17, k.red);
  g.box(6, 0, 6, 7, 1, 8, k.green); g.box(11, 0, 6, 12, 1, 8, k.green); g.box(6, 0, 5, 7, 0, 5, k.white); g.box(11, 0, 5, 12, 0, 5, k.white);
});

model('unicorn', 'Unicorn', 'Animals', ['white', 'pink', 'lavender', 'sky', 'yellow', 'black', 'hotPink'], 20, 22, 10, (g, k) => {
  g.ell(10, 8, 5, 6, 3, 3, k.white);
  for (const [x, z] of [[5, 3], [14, 3], [5, 6], [14, 6]]) { g.box(x, 0, z, x + 1, 6, z + 1, k.white); g.box(x, 0, z, x + 1, 0, z + 1, k.lavender); }
  g.box(3, 9, 4, 5, 14, 6, k.white);
  g.ell(3.5, 15, 5, 2.6, 2, 2, k.white); g.box(0, 13, 4, 1, 15, 6, k.white); g.box(0, 14, 4, 0, 14, 4, k.pink);
  g.front(3, 16, k.black);
  g.box(3, 17, 5, 3, 21, 5, k.yellow); g.set(3, 18, 5, k.gold ?? k.yellow);
  for (let y = 9; y <= 17; y++) { g.set(6, y, 5, [k.pink, k.lavender, k.sky][y % 3]); g.set(6, y, 4, [k.pink, k.lavender, k.sky][(y + 1) % 3]); }
  g.box(4, 17, 4, 5, 17, 6, k.white); g.box(5, 18, 5, 5, 18, 5, k.white);
  for (let t = 0; t < 6; t++) { g.set(16 + (t >> 1), 8 - t, 5, [k.pink, k.lavender, k.sky][t % 3]); g.set(16 + (t >> 1), 8 - t, 4, [k.hotPink, k.lavender, k.sky][t % 3]); }
});

export function buildAll() {
  return MODELS.map((m) => {
    const g = m.build().trim();
    return { id: m.id, name: m.name, category: m.category, palette: m.colorNames.map((n) => C[n]), paletteNames: m.colorNames, grid: g };
  });
}
export { C as COLORS };
