// Chibi voxel animals (v2) for the premium Home carousel and gameplay.
// Big rounded head, large 2x3 eyes with a white highlight, short body, distinct ears/tails.
// Front faces -z. Grid is 16 wide (x 0..15, mirror pairs x <-> 15-x).
const X = 16;
export const mx = (x) => X - 1 - x;

// rounded box (inclusive voxel ranges), r = corner radius in voxels
export function rbox(g, x0, y0, z0, x1, y1, z1, r, c, mode = 'set') {
  const cx = (x0 + x1) / 2, cy = (y0 + y1) / 2, cz = (z0 + z1) / 2;
  const hx = (x1 - x0 + 1) / 2, hy = (y1 - y0 + 1) / 2, hz = (z1 - z0 + 1) / 2;
  for (let x = x0; x <= x1; x++) for (let y = y0; y <= y1; y++) for (let z = z0; z <= z1; z++) {
    const qx = Math.max(Math.abs(x - cx) + 0.5 - hx + r, 0), qy = Math.max(Math.abs(y - cy) + 0.5 - hy + r, 0), qz = Math.max(Math.abs(z - cz) + 0.5 - hz + r, 0);
    if (Math.hypot(qx, qy, qz) <= r + 0.05) g.op(mode, x, y, z, c);
  }
}
function sym(fn) { fn((x) => x); fn(mx); }
const boxS = (g, f, x0, y0, z0, x1, y1, z1, c, mode) => g.box(f(x0), y0, z0, f(x1), y1, z1, c, mode);


// triangular ear: rows of decreasing width, tip pointing up/outward; inner colour on the front face
function pointyEar(g, top, outer, tip, inner, rows = 5) {
  const spans = [[1, 5], [1, 4], [2, 4], [2, 3], [2, 2]].slice(0, rows);
  sym((f) => {
    spans.forEach(([a, b], t) => boxS(g, f, a, top + 1 + t, 5, b, top + 1 + t, 7, t >= rows - 2 ? tip : outer));
    if (inner != null) { boxS(g, f, 2, top + 1, 5, 4, top + 1, 5, inner, 'paint'); boxS(g, f, 2, top + 2, 5, 3, top + 2, 5, inner, 'paint'); }
  });
}

// Idle-animation profile stored on the grid (exported to the model JSON): neck height, eye cells
// (x, y on the front face, closed with the lid colour when blinking) and an optional tail box.
function setAnim(g, headY, ey, lid, x0 = 3) {
  const target = g.base || g;
  const ox = g.offsetX || 0;
  target.anim = target.anim || {};
  target.anim.headY = headY;
  target.anim.lid = lid;
  target.anim.eyes = [];
  for (const x of [x0, x0 + 1]) for (let y = ey; y <= ey + 2; y++) { target.anim.eyes.push([x + ox, y], [mx(x) + ox, y]); }
}
export function setTail(g, box) { (g.base || g).anim = Object.assign((g.base || g).anim || {}, { tail: box }); }

/** Shared chibi body. o: colours + options. Returns key heights. */
function chibi(g, k, o) {
  const body = o.body, belly = o.belly ?? body, paw = o.paw ?? body;
  // body (sitting) and haunches
  rbox(g, 4, 1, 6, 11, 9, 13, 2, body);
  sym((f) => rbox(g, Math.min(f(2), f(5)), 0, 9, Math.max(f(2), f(5)), 4, 14, 1.5, body));
  if (belly !== body) rbox(g, 5, 2, 5, 10, 8, 7, 1.5, belly, 'paint');
  // front legs + paws
  sym((f) => { boxS(g, f, 5, 0, 5, 6, 4, 7, body); boxS(g, f, 5, 0, 4, 6, 1, 7, paw); });
  // head
  const hy0 = o.headY ?? 9;
  rbox(g, 1, hy0, 2, 14, hy0 + 10, 10, 2.6, o.head ?? body);
  if (o.face) rbox(g, 1, hy0, 0, 14, hy0 + (o.faceTop ?? 6), 4, 1.6, o.face, 'paint');
  // muzzle
  if (o.muzzle !== null) rbox(g, 5, hy0 + 1, 0, 10, hy0 + 4, 2, 1.2, o.muzzle ?? belly);
  // nose + mouth
  if (o.nose !== null) { g.box(7, hy0 + 3, 0, 8, hy0 + 4, 0, o.nose ?? k.black); }
  if (o.mouth) g.box(7, hy0 + 1, 0, 8, hy0 + 1, 0, o.mouth, 'paint');
  // eyes (2x3) with highlight + optional blush
  const ey = hy0 + 5;
  sym((f) => {
    for (let x = 3; x <= 4; x++) for (let y = ey; y <= ey + 2; y++) g.front(f(x), y, o.eye ?? k.black);
  });
  setAnim(g, hy0, ey, o.lid ?? o.head ?? body);
  g.front(3, ey + 2, k.white); g.front(mx(4), ey + 2, k.white);
  if (o.blush) sym((f) => g.front(f(2), ey - 1, o.blush));
  return { hy0, top: hy0 + 10 };
}

export function defineAnimals(model) {
  model('fox', 'Fox', 'Animals', ['orange', 'white', 'black', 'darkBrown', 'cream'], X, 26, 20, (g, k) => {
    const h = chibi(g, k, { body: k.orange, belly: k.white, paw: k.darkBrown, muzzle: k.white, face: null, nose: k.black });
    rbox(g, 2, h.hy0, 1, 13, h.hy0 + 4, 3, 1.4, k.white, 'paint'); // white cheeks
    g.box(7, h.hy0 + 3, 0, 8, h.hy0 + 4, 0, k.black);
    // pointy ears with dark tips and cream inner
    pointyEar(g, h.top - 1, k.orange, k.orange, k.cream, 5);
    // big fluffy tail curling up on the right side, white tip
    rbox(g, 11, 1, 11, 15, 5, 17, 2, k.orange);
    rbox(g, 12, 5, 14, 15, 10, 18, 1.8, k.orange);
    rbox(g, 12, 9, 15, 15, 12, 19, 1.5, k.white);
    setTail(g, [11, 1, 14, 15, 12, 19]);
  });

  model('panda', 'Panda', 'Animals', ['white', 'black', 'green', 'darkGreen', 'pink'], X, 24, 18, (g, k) => {
    const h = chibi(g, k, { body: k.white, paw: k.black, muzzle: k.white, nose: k.black, eye: k.white, blush: k.pink, lid: k.black });
    // black arms/legs band and shoulders
    rbox(g, 3, 5, 5, 12, 9, 12, 1.5, k.black, 'paint');
    sym((f) => { boxS(g, f, 5, 0, 4, 6, 4, 7, k.black); rbox(g, Math.min(f(2), f(5)), 0, 9, Math.max(f(2), f(5)), 4, 14, 1.5, k.black, 'paint'); });
    // eye patches (black ovals) with white eyes + black pupils
    const ey = h.hy0 + 5;
    sym((f) => { for (let x = 2; x <= 5; x++) for (let y = ey - 1; y <= ey + 3; y++) if (!((x === 2 || x === 5) && (y === ey - 1 || y === ey + 3))) g.front(f(x), y, k.black); });
    sym((f) => { g.front(f(4), ey + 1, k.white); g.front(f(4), ey + 2, k.white); g.front(f(3), ey + 1, k.black); });
    // round black ears
    sym((f) => rbox(g, Math.min(f(1), f(4)), h.top, 5, Math.max(f(1), f(4)), h.top + 3, 8, 1.4, k.black));
    // bamboo held in front
    g.box(12, 0, 3, 12, 12, 3, k.green); g.box(12, 4, 3, 12, 4, 3, k.darkGreen); g.box(12, 8, 3, 12, 8, 3, k.darkGreen);
    g.box(13, 11, 3, 14, 12, 3, k.green); g.box(13, 10, 2, 13, 10, 2, k.green);
  });

  model('chicken', 'Chick', 'Animals', ['yellow', 'gold', 'orange', 'red', 'black', 'white', 'pink'], X, 20, 16, (g, k) => {
    // round body+head in one, chibi chick
    rbox(g, 2, 2, 2, 13, 15, 13, 4.5, k.yellow);
    rbox(g, 3, 2, 1, 12, 7, 3, 2, k.gold, 'paint');
    // wings
    sym((f) => rbox(g, Math.min(f(0), f(2)), 5, 6, Math.max(f(0), f(2)), 10, 11, 1.2, k.gold));
    // beak
    g.box(6, 9, 0, 9, 10, 1, k.orange); g.box(7, 8, 0, 8, 8, 0, k.orange);
    // eyes 2x3 + highlight
    sym((f) => { for (let x = 3; x <= 4; x++) for (let y = 11; y <= 13; y++) g.front(f(x), y, k.black); });
    g.front(3, 13, k.white); g.front(mx(4), 13, k.white);
    sym((f) => g.front(f(2), 9, k.pink));
    // comb
    g.box(6, 16, 6, 9, 16, 9, k.red); g.box(7, 17, 6, 8, 18, 8, k.red); g.box(6, 17, 7, 6, 17, 7, k.red); g.box(9, 17, 7, 9, 17, 7, k.red);
    // feet
    sym((f) => { boxS(g, f, 4, 0, 4, 6, 0, 8, k.orange); boxS(g, f, 5, 1, 7, 5, 1, 7, k.orange); });
    // tail feathers
    g.box(6, 9, 14, 9, 12, 14, k.gold); g.box(7, 12, 15, 8, 13, 15, k.gold);
    setAnim(g, 9, 11, k.yellow);
  });

  model('bunny', 'Bunny', 'Animals', ['white', 'pink', 'hotPink', 'black', 'lightGrey'], X, 30, 18, (g, k) => {
    const h = chibi(g, k, { body: k.white, belly: k.white, paw: k.lightGrey, muzzle: k.white, nose: k.hotPink, blush: k.pink, mouth: null });
    g.box(7, h.hy0 + 3, 0, 8, h.hy0 + 4, 0, k.hotPink);
    // long ears (slightly tilted), pink inner
    sym((f) => { boxS(g, f, 3, h.top, 5, 5, h.top + 8, 7, k.white); boxS(g, f, 4, h.top + 1, 4, 4, h.top + 7, 4, k.pink); boxS(g, f, 3, h.top + 9, 6, 5, h.top + 9, 6, k.white); });
    // round fluffy tail
    rbox(g, 6, 2, 14, 9, 5, 16, 1.4, k.white);
  });

  model('lion', 'Lion', 'Animals', ['gold', 'rust', 'rustDark', 'black', 'cream', 'white', 'yellow'], X + 2, 26, 20, (g, k) => {
    // lion is built on a 16-wide head with a mane ring 1 voxel larger on each side (shift by 1)
    const off = 1;
    const sub = { base: g, offsetX: off, op: (m, x, y, z, c) => g.op(m, x + off, y, z, c), front: (x, y, c) => g.front(x + off, y, c), box: (...a) => { a[0] += off; a[3] += off; return g.box(...a); } };
    // mane behind/around the head
    rbox(g, 0, 7, 4, 17, 22, 12, 3.5, k.rust);
    rbox(g, 0, 7, 9, 17, 22, 12, 3.5, k.rustDark, 'paint');
    const h = chibi(sub, k, { body: k.yellow, belly: k.cream, paw: k.yellow, muzzle: k.cream, nose: k.rustDark });
    // mane tufts on top
    for (const x of [3, 7, 10, 14]) rbox(g, x, 22, 6, x + 1, 23, 8, 0.6, k.rust);
    // small round ears
    sym((f) => rbox(g, Math.min(f(2), f(4)) + off, h.top - 1, 6, Math.max(f(2), f(4)) + off, h.top + 1, 7, 0.8, k.yellow));
    // tail with dark tuft
    g.box(13, 2, 13, 15, 2, 13, k.yellow); g.box(16, 2, 13, 16, 8, 13, k.yellow); rbox(g, 15, 8, 12, 17, 10, 14, 0.8, k.rustDark);
    setTail(g, [15, 3, 12, 17, 10, 14]);
  });

  model('cat', 'Cat', 'Animals', ['grey', 'darkGrey', 'white', 'pink', 'black', 'lightGrey'], X, 26, 20, (g, k) => {
    const h = chibi(g, k, { body: k.grey, belly: k.white, paw: k.white, muzzle: k.white, face: k.white, faceTop: 4, nose: k.pink, blush: k.pink });
    g.box(7, h.hy0 + 3, 0, 8, h.hy0 + 3, 0, k.pink); g.box(7, h.hy0 + 4, 0, 8, h.hy0 + 4, 0, k.white, 'paint');
    pointyEar(g, h.top - 1, k.grey, k.grey, k.pink, 4);
    // tail curling up behind, darker tip
    g.box(11, 1, 14, 13, 1, 14, k.grey); g.box(13, 1, 15, 13, 9, 15, k.grey); g.box(12, 9, 15, 12, 11, 15, k.darkGrey);
    setTail(g, [12, 2, 15, 13, 11, 15]);
  });

  model('bear', 'Bear', 'Animals', ['brown', 'tan', 'black', 'darkBrown', 'pink'], X, 25, 18, (g, k) => {
    const h = chibi(g, k, { body: k.brown, belly: k.tan, paw: k.darkBrown, muzzle: k.tan, nose: k.black, blush: k.pink });
    sym((f) => { rbox(g, Math.min(f(1), f(4)), h.top, 5, Math.max(f(1), f(4)), h.top + 3, 8, 1.4, k.brown); g.front(f(3), h.top + 1, k.tan); g.front(f(2), h.top + 1, k.tan); });
    rbox(g, 6, 2, 13, 9, 4, 15, 1, k.brown);
  });

  model('dog', 'Puppy', 'Animals', ['white', 'brown', 'black', 'pink', 'red', 'gold', 'cream'], X, 24, 18, (g, k) => {
    const h = chibi(g, k, { body: k.white, belly: k.white, paw: k.white, muzzle: k.white, nose: k.black });
    // brown head top and eye sides, white blaze down the middle
    for (let x = 0; x < X; x++) for (let y = h.hy0 + 5; y <= h.top + 1; y++) for (let z = 0; z < 18; z++) {
      const blaze = x >= 6 && x <= 9 && y <= h.hy0 + 8;
      if (!blaze) g.paint(x, y, z, k.brown);
    }
    rbox(g, 4, 6, 9, 11, 9, 13, 1.5, k.brown, 'paint'); // back patch
    // eyes again on top of the brown paint
    for (const f of [(x) => x, mx]) for (let x = 3; x <= 4; x++) for (let y = h.hy0 + 5; y <= h.hy0 + 7; y++) g.front(f(x), y, k.black);
    g.front(3, h.hy0 + 7, k.white); g.front(mx(4), h.hy0 + 7, k.white);
    g.box(7, h.hy0, 0, 8, h.hy0 + 1, 1, k.pink); // tongue
    // floppy brown ears
    sym((f) => { boxS(g, f, 0, h.hy0 + 3, 4, 1, h.top, 8, k.brown); boxS(g, f, 1, h.top - 1, 4, 3, h.top + 1, 8, k.brown); });
    rbox(g, 4, 9, 4, 11, 9, 12, 1, k.red, 'paint'); g.box(7, 8, 3, 8, 9, 4, k.gold); // collar + tag
    g.box(7, 4, 14, 8, 5, 15, k.white); g.box(7, 6, 16, 8, 9, 16, k.white); g.box(7, 10, 16, 8, 10, 16, k.brown);
    setTail(g, [7, 6, 16, 8, 10, 16]);
  });

  model('penguin', 'Penguin', 'Animals', ['ink', 'white', 'orange', 'pink', 'blue', 'darkGrey'], X, 22, 16, (g, k) => {
    rbox(g, 2, 1, 2, 13, 19, 13, 4.5, k.ink);
    rbox(g, 3, 1, 1, 12, 12, 4, 3, k.white, 'paint');
    rbox(g, 3, 11, 1, 12, 17, 4, 2.4, k.white, 'paint'); // face mask
    g.box(7, 14, 1, 8, 15, 4, k.ink, 'paint'); // widow's peak
    sym((f) => { for (let x = 4; x <= 5; x++) for (let y = 13; y <= 15; y++) g.front(f(x), y, k.blue); });
    g.front(4, 15, k.white); g.front(mx(5), 15, k.white);
    sym((f) => g.front(f(3), 12, k.pink));
    g.box(6, 11, 0, 9, 12, 1, k.orange); g.box(7, 10, 0, 8, 10, 0, k.orange);
    sym((f) => { boxS(g, f, 0, 5, 6, 1, 11, 9, k.ink); boxS(g, f, 0, 4, 7, 0, 5, 8, k.ink); });
    sym((f) => boxS(g, f, 3, 0, 1, 6, 0, 6, k.orange));
    g.box(6, 2, 14, 9, 3, 14, k.ink);
    setAnim(g, 11, 13, k.white, 4);
  });

  model('elephant', 'Elephant', 'Animals', ['elephant', 'elephantLight', 'pink', 'black', 'white', 'darkGrey'], X + 4, 24, 18, (g, k) => {
    const off = 2;
    const sub = { base: g, offsetX: off, op: (m, x, y, z, c) => g.op(m, x + off, y, z, c), front: (x, y, c) => g.front(x + off, y, c), box: (...a) => { a[0] += off; a[3] += off; return g.box(...a); } };
    const h = chibi(sub, k, { body: k.elephant, belly: k.elephantLight, paw: k.elephantLight, muzzle: null, nose: null, blush: k.pink });
    // big round ears
    for (const s of [0, 1]) { const x0 = s ? 16 : 0; rbox(g, x0, h.hy0 + 1, 6, x0 + 3, h.top + 1, 8, 2, k.elephant); rbox(g, x0 + (s ? 0 : 1), h.hy0 + 2, 5, x0 + (s ? 2 : 3), h.top, 5, 1.2, k.pink); }
    // trunk curling down and forward
    rbox(g, 8, h.hy0 + 1, 0, 11, h.hy0 + 4, 2, 1, k.elephant);
    g.box(9, h.hy0 - 3, 0, 10, h.hy0, 1, k.elephant); g.box(9, h.hy0 - 4, -1 < 0 ? 0 : 0, 10, h.hy0 - 4, 0, k.elephant);
    g.box(9, h.hy0 - 4, 0, 10, h.hy0 - 4, 0, k.elephantLight, 'paint');
    // tusks
    for (const x of [7, 12]) g.box(x, h.hy0, 0, x, h.hy0 + 1, 1, k.white);
    // tail
    g.box(10, 3, 14, 10, 6, 15, k.elephant); g.box(10, 2, 15, 10, 2, 15, k.black);
  });
}
