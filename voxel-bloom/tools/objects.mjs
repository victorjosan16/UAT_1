// Chapter 6 "Colorful Builds": 20 static multicolour objects (restored food/vehicle/building models + new ones).
// Level colours are re-mapped to 6-10 vivid colours by build-data.mjs (multicolour recolouring).
import { rng } from './voxel.mjs';

export function defineObjects(model) {
  model('cake', 'Strawberry Cake', 'Colorful Builds', ['pink', 'cream', 'white', 'red', 'green', 'yellow', 'choco'], 16, 17, 16, (g, k) => {
    g.cyl('y', 8, 0, 8, 7, 1, k.lightGrey ?? k.white); g.cyl('y', 8, 0, 8, 7.5, 1, k.white);
    g.cyl('y', 8, 1, 8, 6.5, 4, k.pink); g.cyl('y', 8, 5, 8, 6.5, 1, k.cream);
    g.cyl('y', 8, 6, 8, 6.5, 3, k.pink); g.cyl('y', 8, 9, 8, 6.5, 1, k.white);
    for (let a = 0; a < 16; a++) { const t = a / 16 * Math.PI * 2; g.set(Math.round(8 + Math.cos(t) * 6.3), 8, Math.round(8 + Math.sin(t) * 6.3), a % 2 ? k.white : k.pink); }
    for (let a = 0; a < 6; a++) { const t = a / 6 * Math.PI * 2; const x = Math.round(8 + Math.cos(t) * 4.5), z = Math.round(8 + Math.sin(t) * 4.5); g.set(x, 10, z, k.red); g.set(x, 11, z, k.red); g.set(x, 12, z, k.green); }
    for (const [x, z] of [[7, 7], [9, 9], [8, 6]]) { g.box(x, 10, z, x, 13, z, z === 6 ? k.cream : k.pink); g.set(x, 14, z, k.yellow); g.set(x, 15, z, k.yellow); }
    g.box(3, 3, 2, 13, 3, 2, k.cream, 'paint');
  });

  model('icecream', 'Ice Cream', 'Colorful Builds', ['waffle', 'tan', 'pink', 'mint', 'choco', 'red', 'white'], 12, 22, 12, (g, k) => {
    g.cyl('y', 6, 0, 6, 0.6, 10, k.waffle, 'set', 3.6);
    for (let y = 0; y < 10; y++) for (let x = 0; x < 12; x++) for (let z = 0; z < 12; z++) if ((x + y + z) % 3 === 0) g.paint(x, y, z, k.tan);
    g.ell(6, 11, 6, 4.4, 2.6, 4.4, k.mint);
    g.ell(6, 14.5, 6, 4, 2.6, 4, k.choco);
    g.ell(6, 17.5, 6, 3.6, 2.6, 3.6, k.pink);
    g.box(5, 20, 5, 7, 20, 7, k.white); g.set(6, 21, 6, k.red);
    for (const [x, y] of [[4, 18], [7, 17], [5, 16], [8, 15]]) g.addFront(x, y, k.white);
  });

  model('candy', 'Candy', 'Colorful Builds', ['hotPink', 'white', 'purple', 'yellow'], 22, 11, 10, (g, k) => {
    g.ell(11, 5, 5, 5, 4.6, 4.2, k.hotPink);
    for (let x = 6; x <= 16; x++) for (let y = 0; y < 11; y++) if (((x + y) % 4) < 2) for (let z = 0; z < 10; z++) g.paint(x, y, z, k.white);
    for (const s of [-1, 1]) {
      g.box(11 + s * 6, 4, 4, 11 + s * 6, 6, 6, k.purple);
      for (let t = 0; t < 3; t++) { const x = 11 + s * (7 + t); const w = t >> 1; g.box(x, 4 - t + w, 4 - w, x, 6 + t - w, 6 + w, t === 2 ? k.yellow : k.purple); }
    }
  });

  model('donut', 'Donut', 'Colorful Builds', ['tan', 'pink', 'white', 'blue', 'yellow', 'green'], 18, 7, 18, (g, k) => {
    const r = rng(11);
    for (let x = 0; x < 18; x++) for (let z = 0; z < 18; z++) for (let y = 0; y < 7; y++) {
      const d = Math.hypot(x - 8.5, z - 8.5), t = Math.hypot(d - 5.5, (y - 3) * 1.15);
      if (t <= 3.1) g.set(x, y, z, y >= 4 || (y === 3 && r() < 0.6) ? k.pink : k.tan);
    }
    for (let i = 0; i < 40; i++) { const x = Math.floor(r() * 18), z = Math.floor(r() * 18); for (let y = 6; y >= 0; y--) if (g.get(x, y, z) === k.pink) { g.set(x, y, z, [k.white, k.blue, k.yellow, k.green][i % 4]); break; } }
  });

  model('cupcake', 'Cupcake', 'Colorful Builds', ['purple', 'pink', 'white', 'red', 'green', 'lavender'], 14, 18, 14, (g, k) => {
    g.cyl('y', 7, 0, 7, 4.2, 7, k.purple, 'set', 5.4);
    for (let a = 0; a < 24; a++) { const t = a / 24 * Math.PI * 2; if (a % 2) for (let y = 0; y < 7; y++) g.paint(Math.round(7 + Math.cos(t) * 5), y, Math.round(7 + Math.sin(t) * 5), k.lavender); }
    g.ell(7, 8.5, 7, 6, 2.4, 6, k.pink); g.ell(7, 11, 7, 4.8, 2.2, 4.8, k.pink); g.ell(7, 13, 7, 3.2, 1.8, 3.2, k.pink);
    for (let i = 0; i < 8; i++) { const t = i / 8 * Math.PI * 2; g.set(Math.round(7 + Math.cos(t) * 4.4), 10 + (i % 2), Math.round(7 + Math.sin(t) * 4.4), k.white); }
    g.ell(7, 15.5, 7, 1.6, 1.6, 1.6, k.red); g.set(7, 17, 7, k.green); g.set(8, 17, 7, k.green);
  });

  model('watermelon', 'Watermelon', 'Colorful Builds', ['red', 'white', 'green', 'darkGreen', 'black'], 20, 12, 6, (g, k) => {
    for (let x = 0; x < 20; x++) for (let y = 0; y < 12; y++) {
      const d = Math.hypot(x - 9.5, (y - 11.5) * 1.0);
      if (y <= 11 && d <= 10.5) for (let z = 0; z < 6; z++) g.set(x, y, z, d > 9.6 ? k.darkGreen : d > 8.6 ? k.green : d > 7.8 ? k.white : k.red);
    }
    for (const [x, y] of [[6, 7], [9, 5], [12, 7], [8, 9], [11, 9], [14, 9], [5, 9]]) { g.front(x, y, k.black); g.box(x, y, 5, x, y, 5, k.black, 'paint'); }
  });

  model('burger', 'Burger', 'Colorful Builds', ['waffle', 'tan', 'choco', 'yellow', 'green', 'red', 'cream'], 16, 13, 16, (g, k) => {
    g.cyl('y', 8, 0, 8, 6.2, 2, k.waffle);
    g.cyl('y', 8, 2, 8, 6.6, 2, k.choco);
    g.cyl('y', 8, 4, 8, 7, 1, k.yellow); g.box(1, 3, 7, 1, 3, 8, k.yellow);
    g.cyl('y', 8, 5, 8, 6.8, 1, k.red); g.cyl('y', 8, 6, 8, 7.2, 1, k.green);
    g.ell(8, 7.5, 8, 6.6, 4.6, 6.6, k.waffle); g.box(0, 0, 0, 15, 6, 15, k.waffle, 'paint');
    g.cyl('y', 8, 0, 8, 6.2, 2, k.waffle); g.cyl('y', 8, 2, 8, 6.6, 2, k.choco); g.cyl('y', 8, 4, 8, 7, 1, k.yellow); g.cyl('y', 8, 5, 8, 6.8, 1, k.red); g.cyl('y', 8, 6, 8, 7.2, 1, k.green);
    for (const [x, z] of [[6, 5], [10, 6], [8, 9], [5, 9], [11, 10]]) for (let y = 12; y > 6; y--) if (g.get(x, y, z) >= 0) { g.set(x, y, z, k.cream); break; }
  });

  model('car', 'Red Car', 'Colorful Builds', ['red', 'glass', 'black', 'lightGrey', 'yellow', 'white'], 20, 11, 11, (g, k) => {
    g.box(0, 2, 1, 19, 5, 9, k.red);
    g.box(5, 6, 2, 14, 9, 8, k.red); g.box(6, 6, 1, 13, 8, 9, k.glass, 'paint'); g.box(4, 6, 2, 15, 8, 8, k.glass, 'paint');
    g.box(5, 6, 2, 14, 9, 8, k.red, 'paint'); g.box(6, 6, 2, 13, 8, 2, k.glass, 'paint'); g.box(6, 6, 8, 13, 8, 8, k.glass, 'paint'); g.box(5, 6, 3, 5, 8, 7, k.glass, 'paint'); g.box(14, 6, 3, 14, 8, 7, k.glass, 'paint');
    g.box(9, 6, 2, 10, 9, 2, k.red, 'paint'); g.box(9, 6, 8, 10, 9, 8, k.red, 'paint');
    for (const [x, z] of [[3, 0], [15, 0], [3, 9], [15, 9]]) { g.box(x - 1, 0, z, x + 2, 3, z + 1, k.black); g.box(x, 1, z, x + 1, 2, z + 1, k.lightGrey, 'paint'); }
    g.box(0, 4, 2, 0, 4, 3, k.yellow); g.box(0, 4, 7, 0, 4, 8, k.yellow); g.box(19, 4, 2, 19, 4, 3, k.red); g.box(0, 2, 2, 0, 2, 8, k.lightGrey, 'paint'); g.box(19, 2, 2, 19, 2, 8, k.lightGrey, 'paint');
    g.box(8, 5, 1, 8, 5, 1, k.white, 'paint');
  });

  model('bus', 'School Bus', 'Colorful Builds', ['yellow', 'glass', 'black', 'lightGrey', 'red', 'orange'], 24, 13, 11, (g, k) => {
    g.box(0, 2, 1, 23, 11, 9, k.yellow); g.box(0, 2, 1, 3, 6, 9, k.yellow); g.box(0, 7, 1, 2, 11, 9, -1, 'clear');
    g.box(3, 7, 2, 3, 10, 8, k.glass, 'paint');
    for (let x = 5; x < 22; x += 4) { g.box(x, 7, 1, x + 2, 9, 1, k.glass, 'paint'); g.box(x, 7, 9, x + 2, 9, 9, k.glass, 'paint'); }
    g.box(0, 5, 1, 23, 5, 9, k.black, 'paint'); g.box(0, 2, 1, 23, 2, 9, k.orange, 'paint');
    for (const [x, z] of [[3, 0], [18, 0], [3, 9], [18, 9]]) { g.box(x - 1, 0, z, x + 2, 3, z + 1, k.black); g.box(x, 1, z, x + 1, 2, z + 1, k.lightGrey, 'paint'); }
    g.box(0, 4, 2, 0, 4, 2, k.lightGrey); g.box(0, 4, 8, 0, 4, 8, k.lightGrey); g.box(23, 10, 2, 23, 10, 2, k.red); g.box(23, 10, 8, 23, 10, 8, k.red);
    g.box(4, 12, 3, 21, 12, 7, k.lightGrey);
  });

  model('truck', 'Fire Truck', 'Colorful Builds', ['red', 'glass', 'black', 'lightGrey', 'white', 'blue'], 26, 13, 11, (g, k) => {
    g.box(0, 2, 1, 25, 6, 9, k.red); g.box(0, 7, 1, 6, 11, 9, k.red);
    g.box(0, 8, 2, 0, 10, 8, k.glass, 'paint'); g.box(1, 8, 1, 5, 10, 1, k.glass, 'paint'); g.box(1, 8, 9, 5, 10, 9, k.glass, 'paint');
    g.box(2, 12, 4, 4, 12, 6, k.blue); g.box(3, 12, 3, 3, 12, 7, k.red);
    g.box(8, 7, 3, 24, 7, 7, k.lightGrey); for (let x = 8; x <= 24; x += 2) g.box(x, 8, 3, x, 8, 7, k.white); g.box(8, 8, 3, 24, 8, 3, k.white); g.box(8, 8, 7, 24, 8, 7, k.white);
    g.box(7, 4, 1, 25, 4, 9, k.white, 'paint');
    for (const [x, z] of [[3, 0], [15, 0], [21, 0], [3, 9], [15, 9], [21, 9]]) { g.box(x - 1, 0, z, x + 2, 3, z + 1, k.black); g.box(x, 1, z, x + 1, 2, z + 1, k.lightGrey, 'paint'); }
  });

  model('rocket', 'Rocket', 'Colorful Builds', ['white', 'red', 'blue', 'glass', 'orange', 'yellow', 'lightGrey'], 14, 26, 14, (g, k) => {
    g.cyl('y', 7, 4, 7, 3.6, 14, k.white);
    g.cyl('y', 7, 18, 7, 3.4, 6, k.red, 'set', 0.6);
    g.cyl('y', 7, 4, 7, 3.6, 1, k.red); g.cyl('y', 7, 9, 7, 3.6, 1, k.lightGrey);
    g.box(6, 13, 3, 8, 15, 3, k.blue); g.box(7, 14, 3, 7, 14, 3, k.glass);
    for (const [dx, dz] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) for (let t = 0; t < 4; t++) g.box(7 + dx * (4 + (t >> 1)), 1 + t, 7 + dz * (4 + (t >> 1)), 7 + dx * 4, 6 - t, 7 + dz * 4, k.red);
    g.cyl('y', 7, 2, 7, 2, 2, k.lightGrey); g.cyl('y', 7, 0, 7, 1.6, 2, k.orange); g.set(7, 0, 7, k.yellow); g.set(6, 1, 7, k.yellow);
  });

  model('house', 'Cozy House', 'Colorful Builds', ['cream', 'red', 'brown', 'glass', 'green', 'darkGrey', 'white'], 18, 18, 16, (g, k) => {
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

  model('lighthouse', 'Lighthouse', 'Colorful Builds', ['white', 'red', 'glass', 'darkGrey', 'yellow', 'grey'], 14, 26, 14, (g, k) => {
    g.cyl('y', 7, 0, 7, 6.2, 2, k.grey);
    g.cyl('y', 7, 2, 7, 4.6, 16, k.white, 'set', 3.2);
    for (const y of [5, 6, 7, 11, 12, 13]) g.cyl('y', 7, y, 7, 5, 1, k.red, 'paint');
    g.cyl('y', 7, 18, 7, 4.4, 1, k.darkGrey);
    g.cyl('y', 7, 19, 7, 2.6, 3, k.glass); g.box(6, 19, 6, 8, 21, 8, k.yellow);
    g.cyl('y', 7, 22, 7, 3.2, 3, k.red, 'set', 0.5);
    g.box(6, 2, 2, 8, 5, 2, k.darkGrey, 'paint'); g.front(7, 9, k.glass); g.front(7, 15, k.glass);
  });

  model('windmill', 'Windmill', 'Colorful Builds', ['tan', 'brown', 'white', 'red', 'glass', 'green', 'darkBrown'], 20, 26, 12, (g, k) => {
    g.box(0, 0, 0, 19, 0, 11, k.green);
    g.cyl('y', 10, 1, 6, 4.6, 13, k.tan, 'set', 3.2);
    g.cyl('y', 10, 14, 6, 3.6, 4, k.red, 'set', 0.6);
    g.box(9, 1, 1, 11, 4, 2, k.darkBrown); g.front(10, 9, k.glass); g.front(10, 10, k.glass);
    g.box(10, 13, 1, 10, 13, 1, k.brown);
    for (let t = -9; t <= 9; t++) { if (Math.abs(t) < 1) continue; g.set(10 + t, 13 + t, 0, k.brown); g.set(10 + t, 13 - t, 0, k.brown); }
    for (let t = 3; t <= 9; t++) { const s = [[1, 1], [1, -1], [-1, 1], [-1, -1]]; for (const [a, b] of s) { g.set(10 + a * t + (a * b > 0 ? -1 : 1) * 0, 13 + b * t - b, 0, k.white); g.set(10 + a * t - a, 13 + b * t, 0, k.white); } }
  });

  model('castle', 'Dream Castle', 'Colorful Builds', ['lavender', 'pink', 'purple', 'glass', 'yellow', 'white', 'darkBrown'], 22, 26, 16, (g, k) => {
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

  model('robot', 'Robo Buddy', 'Colorful Builds', ['lightGrey', 'blue', 'sky', 'red', 'yellow', 'darkGrey'], 18, 22, 10, (g, k) => {
    g.box(6, 0, 3, 7, 4, 6, k.darkGrey); g.box(10, 0, 3, 11, 4, 6, k.darkGrey); g.box(5, 0, 2, 7, 1, 6, k.blue); g.box(10, 0, 2, 12, 1, 6, k.blue);
    g.box(4, 5, 2, 13, 12, 7, k.lightGrey); g.box(6, 7, 1, 11, 10, 1, k.sky); g.box(7, 9, 1, 7, 9, 1, k.red); g.box(9, 8, 1, 10, 8, 1, k.yellow);
    g.box(2, 7, 3, 3, 12, 5, k.blue); g.box(14, 7, 3, 15, 12, 5, k.blue); g.box(1, 5, 3, 3, 6, 5, k.darkGrey); g.box(14, 5, 3, 16, 6, 5, k.darkGrey);
    g.box(8, 13, 4, 9, 13, 5, k.darkGrey);
    g.box(5, 14, 2, 12, 19, 7, k.lightGrey); g.box(6, 15, 1, 11, 18, 1, k.blue);
    g.box(7, 17, 0, 7, 17, 0, k.sky); g.box(10, 17, 0, 10, 17, 0, k.sky); g.box(7, 15, 0, 10, 15, 0, k.yellow);
    g.box(8, 20, 4, 9, 20, 5, k.darkGrey); g.box(8, 21, 4, 9, 21, 5, k.red);
    g.box(4, 16, 4, 4, 17, 5, k.red); g.box(13, 16, 4, 13, 17, 5, k.red);
  });

  model('pyramid', 'Rainbow Pyramid', 'Colorful Builds', ['red', 'orange', 'yellow', 'green', 'teal', 'blue', 'purple', 'hotPink', 'white'], 21, 11, 21, (g, k) => {
    const cols = [k.red, k.orange, k.yellow, k.green, k.teal, k.blue, k.purple, k.hotPink, k.white, k.yellow, k.red];
    for (let y = 0; y < 11; y++) g.box(y, y, y, 20 - y, y, 20 - y, cols[y]);
    g.box(9, 0, 0, 11, 2, 0, k.purple);
  });

  model('balloon', 'Hot-Air Balloon', 'Colorful Builds', ['red', 'orange', 'yellow', 'green', 'blue', 'purple', 'brown', 'tan'], 16, 25, 16, (g, k) => {
    const stripes = [k.red, k.orange, k.yellow, k.green, k.blue, k.purple];
    for (let x = 0; x < 16; x++) for (let y = 0; y < 25; y++) for (let z = 0; z < 16; z++) {
      const dx = x - 7.5, dy = y - 15, dz = z - 7.5;
      const r = y >= 15 ? 7.5 : 7.5 * Math.max(0.25, 1 - (15 - y) / 11);
      if (y >= 6 && Math.hypot(dx, dz) <= r && (y < 15 || Math.hypot(dx, dy, dz) <= 7.8)) {
        const a = Math.atan2(dz, dx); g.set(x, y, z, stripes[Math.floor(((a + Math.PI) / (2 * Math.PI)) * 6) % 6]);
      }
    }
    g.box(5, 0, 5, 10, 2, 10, k.brown); g.box(5, 2, 5, 10, 2, 10, k.tan);
    for (const [x, z] of [[5, 5], [10, 5], [5, 10], [10, 10]]) g.box(x, 3, z, x, 6, z, k.tan);
  });

  model('train', 'Steam Train', 'Colorful Builds', ['red', 'blue', 'yellow', 'black', 'darkGrey', 'green', 'glass', 'orange'], 26, 14, 10, (g, k) => {
    g.box(0, 2, 1, 25, 3, 8, k.darkGrey);
    g.cyl('x', 1, 6, 4.5, 3, 11, k.red); g.box(1, 3, 2, 11, 3, 7, k.red);
    g.box(12, 3, 1, 18, 10, 8, k.blue); g.box(13, 6, 1, 17, 8, 1, k.glass, 'paint'); g.box(11, 11, 0, 19, 11, 9, k.yellow);
    g.box(3, 9, 3, 5, 12, 6, k.black); g.box(2, 13, 2, 6, 13, 7, k.orange);
    g.box(20, 4, 1, 25, 8, 8, k.green); g.box(20, 8, 1, 25, 8, 8, k.yellow);
    for (const x of [2, 7, 13, 17, 21, 24]) { g.box(x, 0, 0, x + 1, 2, 1, k.black); g.box(x, 0, 8, x + 1, 2, 9, k.black); }
    g.box(0, 3, 3, 0, 5, 6, k.yellow);
  });

  model('ship', 'Cruise Ship', 'Colorful Builds', ['red', 'white', 'navy', 'yellow', 'glass', 'orange', 'blue'], 26, 15, 10, (g, k) => {
    for (let y = 0; y < 5; y++) g.box(2 - Math.min(y, 2) + 2, y, 1 + (y < 2 ? 1 : 0), 23 + Math.min(y, 2) - 2, y, 8 - (y < 2 ? 1 : 0), y < 2 ? k.red : k.navy);
    g.box(1, 5, 1, 24, 5, 8, k.white);
    g.box(5, 6, 2, 20, 8, 7, k.white); for (let x = 6; x <= 19; x += 2) { g.front(x, 7, k.glass); }
    g.box(8, 9, 3, 17, 10, 6, k.yellow); for (let x = 9; x <= 16; x += 2) g.front(x, 10, k.blue);
    g.box(11, 11, 4, 13, 14, 5, k.orange); g.box(11, 14, 4, 13, 14, 5, k.navy);
    for (let x = 4; x <= 21; x += 3) g.front(x, 3, k.yellow);
  });

  model('excavator', 'Excavator', 'Colorful Builds', ['yellow', 'black', 'darkGrey', 'glass', 'orange', 'lightGrey', 'red'], 26, 16, 12, (g, k) => {
    g.box(0, 0, 1, 15, 2, 10, k.black); for (let x = 1; x <= 14; x += 3) { g.box(x, 1, 0, x + 1, 1, 0, k.darkGrey); g.box(x, 1, 11, x + 1, 1, 11, k.darkGrey); }
    g.box(1, 3, 2, 14, 6, 9, k.yellow); g.box(8, 7, 3, 14, 12, 9, k.yellow); g.box(9, 8, 3, 13, 11, 3, k.glass, 'paint'); g.box(14, 8, 4, 14, 11, 8, k.glass, 'paint');
    g.box(8, 13, 3, 14, 13, 9, k.orange); g.box(2, 7, 7, 3, 9, 8, k.darkGrey);
    for (let t = 0; t < 7; t++) g.box(15 + t, 7 + Math.min(t, 4), 5, 16 + t, 8 + Math.min(t, 4), 6, k.orange);
    g.box(21, 4, 5, 22, 11, 6, k.orange); g.box(20, 1, 4, 25, 4, 7, k.lightGrey); g.box(25, 0, 4, 25, 1, 7, k.darkGrey);
    g.box(1, 5, 2, 14, 5, 9, k.red, 'paint');
  });

  model('plane', 'Jet Plane', 'Colorful Builds', ['white', 'red', 'blue', 'glass', 'yellow', 'lightGrey', 'orange'], 26, 12, 22, (g, k) => {
    g.cyl('x', 2, 5, 11, 2.6, 22, k.white); g.box(0, 4, 10, 2, 6, 12, k.lightGrey);
    g.box(3, 6, 9, 5, 7, 13, k.glass); g.box(6, 6, 9, 21, 6, 9, k.blue, 'paint'); g.box(6, 6, 13, 21, 6, 13, k.blue, 'paint');
    for (let t = 0; t < 9; t++) { g.box(11 - (t >> 2), 4, 2 + t, 14 - (t >> 2), 4, 2 + t, k.red); g.box(11 - (t >> 2), 4, 20 - t, 14 - (t >> 2), 4, 20 - t, k.red); }
    g.box(20, 5, 7, 23, 5, 15, k.red); g.box(21, 6, 11, 23, 10, 11, k.yellow); g.box(23, 4, 10, 25, 6, 12, k.orange);
    g.box(10, 1, 7, 10, 3, 7, k.lightGrey); g.box(10, 1, 15, 10, 3, 15, k.lightGrey); g.box(3, 1, 11, 3, 3, 11, k.lightGrey);
  });

  model('ferriswheel', 'Ferris Wheel', 'Colorful Builds', ['red', 'orange', 'yellow', 'green', 'blue', 'purple', 'hotPink', 'white', 'lightGrey'], 25, 26, 8, (g, k) => {
    const cabins = [k.red, k.orange, k.yellow, k.green, k.blue, k.purple, k.hotPink, k.red];
    for (let a = 0; a < 96; a++) { const t = a / 96 * Math.PI * 2; g.set(Math.round(12 + Math.cos(t) * 10), Math.round(14 + Math.sin(t) * 10), 3, k.white); g.set(Math.round(12 + Math.cos(t) * 10), Math.round(14 + Math.sin(t) * 10), 4, k.white); }
    for (let i = 0; i < 8; i++) { const t = i / 8 * Math.PI * 2; for (let r = 0; r < 10; r++) g.set(Math.round(12 + Math.cos(t) * r), Math.round(14 + Math.sin(t) * r), 3, k.lightGrey);
      const cx = Math.round(12 + Math.cos(t) * 10), cy = Math.round(14 + Math.sin(t) * 10); g.box(cx - 1, cy - 2, 1, cx + 1, cy, 6, cabins[i]); }
    g.box(11, 13, 2, 13, 15, 5, k.yellow);
    for (let y = 0; y < 14; y++) { const o = Math.round((14 - y) * 0.45); g.box(12 - 6 + o, y, 1, 12 - 5 + o, y, 2, k.lightGrey); g.box(12 + 5 - o, y, 1, 12 + 6 - o, y, 2, k.lightGrey); g.box(12 - 6 + o, y, 5, 12 - 5 + o, y, 6, k.lightGrey); g.box(12 + 5 - o, y, 5, 12 + 6 - o, y, 6, k.lightGrey); }
    g.box(3, 0, 0, 21, 0, 7, k.green);
  });

  model('stadium', 'Stadium', 'Colorful Builds', ['red', 'orange', 'yellow', 'green', 'blue', 'purple', 'white', 'lightGrey', 'lime'], 26, 9, 22, (g, k) => {
    const tiers = [k.red, k.orange, k.yellow, k.green, k.blue, k.purple];
    for (let x = 0; x < 26; x++) for (let z = 0; z < 22; z++) {
      const e = Math.hypot((x - 12.5) / 12.5, (z - 10.5) / 10.5);
      if (e > 1) continue;
      if (e < 0.55) { g.set(x, 0, z, (x + z) % 4 < 2 ? k.green : k.lime); continue; }
      const h = Math.round((e - 0.55) / 0.45 * 7);
      for (let y = 0; y <= h; y++) g.set(x, y, z, y === h ? tiers[Math.floor(Math.atan2(z - 10.5, x - 12.5) / Math.PI * 3 + 3) % 6] : k.lightGrey);
    }
    g.box(12, 1, 10, 13, 1, 11, k.white); for (const [x, z] of [[2, 10], [23, 10]]) g.box(x, 7, z, x, 8, z + 1, k.white);
  });
}
