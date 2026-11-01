// Mascots 11-100 for Voxel Bloom 3D, built from five parametric chibi templates
// (quadruped, bird, fish, reptile/amphibian, bug) plus a few specials. Mascots 1-10 live in animals.mjs.
// Every mascot: big 2x3 eyes with a highlight, idle-animation profile (neck height + eye cells), front faces -z.
import { rbox, sym, boxS, pointyEar, setAnim, setTail, chibi, mx } from './animals.mjs';
import { rng } from './voxel.mjs';

// ------------------------------------------------------------------ shared helpers
/** View of a grid with x shifted by dx (templates are authored on a 16-wide frame). */
export function shift(g, dx) {
  const v = Object.create(g);
  v.base = g; v.offsetX = dx;
  v.op = (m, x, y, z, c) => g.op(m, x + dx, y, z, c);
  v.set = (x, y, z, c) => g.set(x + dx, y, z, c);
  v.paint = (x, y, z, c) => g.paint(x + dx, y, z, c);
  v.get = (x, y, z) => g.get(x + dx, y, z);
  v.in = (x, y, z) => g.in(x + dx, y, z);
  v.front = (x, y, c) => g.front(x + dx, y, c);
  v.addFront = (x, y, c, d) => g.addFront(x + dx, y, c, d);
  return v;
}
function eyes(g, k, x0, ey, eye, lid, headY) {
  sym((f) => { for (let x = x0; x <= x0 + 1; x++) for (let y = ey; y <= ey + 2; y++) g.front(f(x), y, eye); });
  g.front(x0, ey + 2, k.white); g.front(mx(x0 + 1), ey + 2, k.white);
  setAnim(g, headY, ey, lid, x0);
}
const H = 9; // chibi head base

function ears(g, k, o, top) {
  const c = o.earColor ?? o.body, inner = o.earInner;
  switch (o.ears) {
    case 'pointy': pointyEar(g, top - 1, c, o.earTip ?? c, inner, o.earRows ?? 5); break;
    case 'round': sym((f) => { rbox(g, Math.min(f(1), f(4)), top, 5, Math.max(f(1), f(4)), top + 3, 8, 1.4, c); if (inner != null) { g.front(f(2), top + 1, inner); g.front(f(3), top + 1, inner); } }); break;
    case 'small': sym((f) => rbox(g, Math.min(f(2), f(4)), top - 1, 6, Math.max(f(2), f(4)), top + 1, 7, 0.8, c)); break;
    case 'big': sym((f) => { rbox(g, Math.min(f(-1), f(3)), top - 3, 5, Math.max(f(-1), f(3)), top + 2, 8, 2, c); if (inner != null) rbox(g, Math.min(f(0), f(2)), top - 2, 4, Math.max(f(0), f(2)), top + 1, 4, 1, inner, 'paint'); }); break;
    case 'long': sym((f) => { boxS(g, f, 3, top, 5, 5, top + 7, 7, c); if (inner != null) boxS(g, f, 4, top + 1, 4, 4, top + 6, 4, inner); }); break;
    case 'floppy': sym((f) => { boxS(g, f, 0, top - 7, 4, 1, top - 1, 8, c); boxS(g, f, 1, top - 2, 4, 3, top, 8, c); }); break;
    case 'side': sym((f) => { boxS(g, f, 0, top - 4, 5, 1, top - 2, 7, c); }); break;
    default: break;
  }
  if (o.horns) sym((f) => { boxS(g, f, 3, top, 6, 3, top + 2, 6, o.horns); boxS(g, f, 2, top + 2, 6, 2, top + 3, 6, o.horns); });
  if (o.antlers) sym((f) => { boxS(g, f, 4, top, 6, 4, top + 5, 6, o.antlers); boxS(g, f, 2, top + 3, 6, 3, top + 3, 6, o.antlers); boxS(g, f, 2, top + 4, 6, 2, top + 6, 6, o.antlers); boxS(g, f, 5, top + 5, 6, 6, top + 5, 6, o.antlers); boxS(g, f, 6, top + 6, 6, 6, top + 7, 6, o.antlers); });
  if (o.uniHorn) { for (let t = 0; t < 5; t++) g.box(7, top + t, 3 + (t >> 1), 8 - (t >> 2), top + t, 4 + (t >> 1), o.uniHorn); }
}

function tail(g, k, o) {
  const c = o.tailColor ?? o.body;
  switch (o.tail) {
    case 'fluffy': rbox(g, 11, 1, 11, 15, 5, 17, 2, c); rbox(g, 12, 5, 14, 15, 10, 18, 1.8, c); if (o.tailTip != null) rbox(g, 12, 9, 15, 15, 12, 19, 1.5, o.tailTip); setTail(g, [11, 1, 14, 15, 12, 19]); break;
    case 'big': rbox(g, 5, 2, 13, 10, 8, 18, 2.2, c); rbox(g, 5, 8, 15, 10, 16, 19, 2.2, c); setTail(g, [5, 2, 13, 10, 16, 19]); break;
    case 'thin': g.box(11, 1, 14, 13, 1, 14, c); g.box(13, 1, 15, 13, 9, 15, c); g.box(12, 9, 15, 12, 11, 15, o.tailTip ?? c); setTail(g, [12, 2, 15, 13, 11, 15]); break;
    case 'ringed': for (let y = 1; y <= 10; y++) g.box(12, y, 14, 13, y, 15, y % 3 === 0 ? (o.tailTip ?? k.black) : c); setTail(g, [12, 1, 14, 13, 10, 15]); break;
    case 'curl': g.box(7, 4, 14, 8, 4, 15, c); g.box(7, 5, 15, 7, 6, 15, c); g.box(8, 6, 15, 8, 6, 15, c); break;
    case 'tuft': g.box(7, 3, 14, 8, 3, 16, c); g.box(7, 2, 17, 8, 3, 17, o.tailTip ?? c); break;
    case 'flat': rbox(g, 5, 0, 14, 10, 1, 19, 1, o.tailTip ?? c); break;
    case 'stub': rbox(g, 6, 2, 13, 9, 4, 15, 1, c); break;
    case 'long': for (let t = 0; t < 7; t++) g.box(7, 1, 14 + t, 8, 2, 14 + t, c); g.box(7, 1, 21, 8, 1, 21, o.tailTip ?? c); break;
    case 'scorpion': for (let t = 0; t < 6; t++) g.box(7, 2 + t * 2, 14 + Math.min(t, 3), 8, 3 + t * 2, 14 + Math.min(t, 3), c); g.box(7, 14, 15, 8, 15, 15, o.tailTip ?? k.black); break;
    default: break;
  }
}

function pattern(g, k, o, r) {
  if (o.stripes != null) for (let x = 1; x < 16; x += 3) for (let y = 0; y < 22; y++) for (let z = 4; z < 20; z++) if ((y + (x >> 1)) % 4 === 0) g.paint(x, y, z, o.stripes);
  if (o.headStripes != null) { for (const x of [5, 7, 8, 10]) g.box(x, H + 8, 2, x, H + 10, 4, o.headStripes, 'paint'); for (const y of [H + 2, H + 5]) { g.box(1, y, 3, 1, y + 1, 9, o.headStripes, 'paint'); g.box(14, y, 3, 14, y + 1, 9, o.headStripes, 'paint'); } }
  if (o.spots != null) for (let i = 0; i < (o.spotCount ?? 28); i++) { const x = Math.floor(r() * 16), y = Math.floor(r() * 20), z = 2 + Math.floor(r() * 14); g.box(x, y, z, x + 1, y, z + 1, o.spots, 'paint'); }
  if (o.mask != null) rbox(g, 1, H + 5, 0, 14, H + 8, 4, 1, o.mask, 'paint');
  if (o.back != null) rbox(g, 3, 4, 10, 12, 10, 14, 1.5, o.back, 'paint');
}

/** Sitting chibi quadruped with species features. */
function quad(g, k, o, seed = 1) {
  const r = rng(seed);
  const h = chibi(g, k, o);
  pattern(g, k, o, r);
  ears(g, k, o, h.top);
  tail(g, k, o);
  if (o.snout === 'pig' || o.snout === 'cow') {
    rbox(g, 4, h.hy0 + 1, 0, 11, h.hy0 + 4, 2, 1.2, o.snoutColor);
    g.box(6, h.hy0 + 2, 0, 6, h.hy0 + 3, 0, o.nostril ?? k.black); g.box(9, h.hy0 + 2, 0, 9, h.hy0 + 3, 0, o.nostril ?? k.black);
  }
  if (o.hornNose) { g.box(7, h.hy0 + 4, 0, 8, h.hy0 + 5, 1, o.hornNose); g.box(7, h.hy0 + 6, 0, 8, h.hy0 + 7, 0, o.hornNose); }
  if (o.mane) { rbox(g, 5, h.hy0 + 6, 9, 10, h.top + 1, 12, 1, o.mane); rbox(g, 6, h.top, 3, 9, h.top + 2, 9, 1, o.mane); }
  if (o.wings) sym((f) => { for (let t = 0; t < 5; t++) boxS(g, f, -t - 0, 6 + t, 11, -t - 0, 10 + t - (t > 3 ? 1 : 0), 12, o.wings); });
  if (o.spikes) for (let y = 4; y <= h.top; y += 2) g.box(7, y, y < h.hy0 ? 14 : 11, 8, y + 1, y < h.hy0 ? 14 : 11, o.spikes);
  if (o.quills) for (let i = 0; i < 40; i++) { const x = 1 + Math.floor(r() * 14), y = 3 + Math.floor(r() * (h.top - 2)), z = 10 + Math.floor(r() * 5); g.set(x, y, z, r() < 0.5 ? o.quills : o.body); }
  if (o.wool) for (let i = 0; i < 22; i++) g.ell(2 + r() * 11, 2 + r() * 7, 6 + r() * 8, 1.7, 1.7, 1.7, o.wool);
  if (o.woolHead) rbox(g, 3, h.top - 2, 3, 12, h.top + 1, 9, 1.5, o.woolHead);
  if (o.hump) rbox(g, 5, 9, 9, 10, 12, 13, 1.5, o.hump);
  if (o.tusks && !o.trunk) for (const x of [6, 9]) g.box(x, h.hy0 - 2, 0, x, h.hy0 + 1, 0, o.tusks);
  if (o.trunk) { rbox(g, 6, h.hy0 + 1, 0, 9, h.hy0 + 4, 2, 1, o.trunk); g.box(7, h.hy0 - 3, 0, 8, h.hy0, 1, o.trunk); if (o.tusks) for (const x of [5, 10]) { g.box(x, h.hy0, 0, x, h.hy0 + 1, 1, o.tusks); g.box(x, h.hy0 - 1, -1 + 1, x, h.hy0 - 1, 0, o.tusks); } }
  if (o.bamboo) { g.box(12, 0, 3, 12, 12, 3, o.bamboo); g.box(13, 11, 3, 14, 12, 3, o.bamboo); }
  if (o.cheeks) sym((f) => rbox(g, Math.min(f(0), f(2)), h.hy0 + 1, 3, Math.max(f(0), f(2)), h.hy0 + 4, 7, 1.2, o.cheeks));
  return h;
}

/** Round chibi bird (optionally long legs or long neck). */
function bird(g, k, o) {
  if (o.longNeck) {
    rbox(g, 2, 1, 4, 13, 8, 15, 3, o.body);
    rbox(g, 6, 7, 2, 9, 15, 5, 1.2, o.neck ?? o.body);
    rbox(g, 3, 13, 0, 12, 20, 7, 2.2, o.head ?? o.body);
    sym((f) => boxS(g, f, 1, 3, 7, 2, 7, 13, o.wing ?? o.body));
    g.box(6, 15, -1 + 1, 9, 16, 0, o.beak); // beak on the head front
    if (o.beakKnob) g.box(7, 17, 0, 8, 17, 0, o.beakKnob);
    eyes(g, k, 3, 16, o.eye ?? k.black, o.head ?? o.body, 12);
    sym((f) => boxS(g, f, 5, 0, 3, 6, 0, 6, o.feet));
    return;
  }
  const y0 = o.longLegs ? 7 : 2;
  if (o.longLegs) sym((f) => { boxS(g, f, 6, 0, 7, 6, y0, 7, o.feet); boxS(g, f, 5, 0, 5, 7, 0, 7, o.feet); });
  rbox(g, 2, y0, 2, 13, y0 + 13, 13, 4.5, o.body);
  if (o.belly != null) rbox(g, 3, y0, 1, 12, y0 + 6, 3, 2, o.belly, 'paint');
  if (o.face != null) rbox(g, 3, y0 + 6, 1, 12, y0 + 12, 3, 2.2, o.face, 'paint');
  sym((f) => rbox(g, Math.min(f(0), f(2)), y0 + 3, 6, Math.max(f(0), f(2)), y0 + 8, 11, 1.2, o.wing ?? o.body));
  const by = y0 + 7;
  switch (o.beakType) {
    case 'hook': g.box(6, by, 0, 9, by + 1, 1, o.beak); g.box(7, by - 1, 0, 8, by - 1, 0, o.beak); break;
    case 'duck': g.box(5, by - 1, -1 + 1, 10, by, 1, o.beak); break;
    case 'long': g.box(7, by, 0, 8, by + 1, 1, o.beak); g.box(7, by - 2, 0, 8, by - 1, 0, o.beak); break;
    default: g.box(6, by, 0, 9, by + 1, 1, o.beak); g.box(7, by - 1, 0, 8, by - 1, 0, o.beak);
  }
  eyes(g, k, 3, y0 + 9, o.eye ?? k.black, o.lid ?? o.face ?? o.body, y0 + 6);
  if (o.cheek != null) sym((f) => g.front(f(2), y0 + 8, o.cheek));
  if (o.crest === 'comb') { g.box(6, y0 + 14, 6, 9, y0 + 14, 9, o.crestColor); g.box(7, y0 + 15, 6, 8, y0 + 16, 8, o.crestColor); }
  if (o.crest === 'tuft') { g.box(7, y0 + 14, 5, 8, y0 + 16, 6, o.crestColor); g.box(6, y0 + 16, 6, 6, y0 + 17, 6, o.crestColor); }
  if (o.crest === 'ears') sym((f) => { boxS(g, f, 3, y0 + 13, 6, 4, y0 + 15, 7, o.crestColor); });
  if (o.crest === 'crown') for (const x of [4, 7, 10]) g.box(x, y0 + 14, 6, x + 1, y0 + 16, 6, o.crestColor);
  if (o.tailFeathers != null) { g.box(5, y0 + 2, 14, 10, y0 + 6, 15, o.tailFeathers); if (o.longTail) g.box(6, 0, 15, 9, y0 + 2, 16, o.tailFeathers); }
  if (!o.longLegs) sym((f) => boxS(g, f, 4, 0, 4, 6, 1, 8, o.feet));
  if (o.chest != null) rbox(g, 5, y0 + 3, 1, 10, y0 + 6, 2, 1, o.chest, 'paint');
}

/** Side-on chibi fish / sea mammal: the front face shows the side with one big eye. */
function fish(g, k, o) {
  const L = o.length ?? 7, R = o.height ?? 5;
  g.ell(9, 7, 4, L, R, 3.2, o.body);
  if (o.belly != null) g.ell(9, 4.5, 2.5, L - 1, 2.6, 2.2, o.belly, 'paint');
  if (o.stripes != null) for (const x of o.stripeX ?? [6, 10]) g.box(x, 0, 0, x + 1, 13, 8, o.stripes, 'paint');
  if (o.edge != null) for (const x of o.stripeX ?? [6, 10]) { g.box(x - 1, 0, 0, x - 1, 13, 8, o.edge, 'paint'); g.box(x + 2, 0, 0, x + 2, 13, 8, o.edge, 'paint'); }
  const tx = 9 + L;
  if (o.tailType === 'whale') { g.box(tx - 1, 8, 3, tx, 9, 5, o.body); g.box(tx + 1, 9, 1, tx + 2, 10, 7, o.fin ?? o.body); }
  else { g.box(tx - 1, 5, 3, tx, 9, 5, o.body); g.box(tx + 1, 3, 4, tx + 2, 11, 4, o.fin ?? o.body); }
  if (o.dorsal !== false) { const dx = o.dorsalX ?? 9; for (let t = 0; t < (o.dorsalH ?? 3); t++) g.box(dx - 1 + t, 7 + R + t, 4, dx + 2, 7 + R + t, 4, o.fin ?? o.body); }
  g.box(7, 3, 1, 8, 4, 1, o.fin ?? o.body); g.box(7, 3, 7, 8, 4, 7, o.fin ?? o.body);
  const ex = 9 - L + 3;
  // one big eye on each side (front face and back face)
  for (const zFace of ['front']) for (let x = ex; x <= ex + 1; x++) for (let y = 7; y <= 9; y++) g.front(x, y, o.eye ?? k.black);
  g.front(ex, 9, k.white);
  const t = g.base || g;
  t.anim = { headY: 1, lid: o.body, eyes: [] };
  for (let x = ex; x <= ex + 1; x++) for (let y = 7; y <= 9; y++) t.anim.eyes.push([x, y], [x, y]);
  if (o.smile != null) g.front(ex - 1, 5, o.smile);
  if (o.blow) { g.box(9, 7 + R + 1, 4, 9, 7 + R + 2, 4, o.blow); g.box(8, 7 + R + 3, 3, 10, 7 + R + 3, 5, o.blow); }
}

/** Low chibi reptile/amphibian: big head in front, body and tail behind. */
function reptile(g, k, o) {
  rbox(g, 1, 1, 0, 14, 9, 8, 2.5, o.body);
  if (o.belly != null) rbox(g, 3, 1, 0, 12, 3, 2, 1, o.belly, 'paint');
  rbox(g, 3, 0, 7, 12, 6, o.short ? 13 : 16, 2, o.back ?? o.body);
  sym((f) => { boxS(g, f, 2, 0, 3, 3, 2, 5, o.body); boxS(g, f, 2, 0, 11, 3, 2, 13, o.body); });
  if (!o.noTail) { for (let t = 0; t < 7; t++) g.box(7 - (t < 3 ? 1 : 0), 1, 16 + t, 8 + (t < 3 ? 1 : 0), 2 - (t > 4 ? 1 : 0), 16 + t, o.tail ?? o.body); setTail(g, [6, 1, 16, 9, 2, 22]); }
  if (o.curlTail) { g.box(7, 1, 17, 8, 5, 17, o.body); g.box(7, 5, 15, 8, 5, 16, o.body); g.box(7, 3, 15, 8, 4, 15, o.body); }
  if (o.snout) { rbox(g, 4, 1, -1 + 1, 11, 4, 1, 1, o.body); for (const x of [4, 6, 9, 11]) g.box(x, 3, 0, x, 3, 0, k.white); }
  if (o.bulge) { sym((f) => rbox(g, Math.min(f(1), f(5)), 8, 2, Math.max(f(1), f(5)), 11, 5, 1.4, o.body)); eyes(g, k, 2, 8, o.eye ?? k.black, o.body, 1); }
  else eyes(g, k, 3, 5, o.eye ?? k.black, o.body, 1);
  if (o.cheek != null) sym((f) => g.front(f(2), 3, o.cheek));
  if (o.mouth != null) g.box(5, 2, 0, 10, 2, 0, o.mouth, 'paint');
  if (o.spikes != null) for (let z = 3; z <= 15; z += 2) g.box(7, z < 8 ? 10 : 7, z, 8, z < 8 ? 10 : 7, z, o.spikes);
  if (o.dots != null) { const r = rng(o.seed ?? 3); for (let i = 0; i < 16; i++) { const x = 2 + Math.floor(r() * 12), z = 2 + Math.floor(r() * 14); for (let y = 12; y >= 0; y--) if (g.get(x, y, z) >= 0) { g.set(x, y, z, o.dots); break; } } }
  if (o.shell != null) { rbox(g, 1, 3, 6, 14, 11, 17, 3, o.shell); for (const [x, z] of [[7, 11], [4, 9], [11, 9], [4, 14], [11, 14]]) for (let y = 13; y >= 0; y--) if (g.get(x, y, z) >= 0) { g.box(x - 1, y, z - 1, x + 1, y, z + 1, o.shellDark ?? o.shell, 'paint'); break; } }
  if (o.crest != null) for (let z = 1; z <= 7; z++) g.box(7, 10, z, 8, 10 + (z % 2), z, o.crest);
}

/** Chibi bug: big head, segmented body, thin legs, optional wings. */
function bug(g, k, o) {
  const rH = 7;
  rbox(g, 3, 5, 0, 12, 13, 7, 2.5, o.head ?? o.body);
  if (o.slim) rbox(g, 6, 3, 7, 9, 9, 15, 1.5, o.body); else rbox(g, 4, 3, 7, 11, 10, o.long ? 18 : 14, 2.5, o.body);
  if (o.stripes != null) for (let z = 9; z <= 14; z += 2) g.box(3, 0, z, 12, 14, z, o.stripes, 'paint');
  sym((f) => { for (const z of [7, 10, 13]) boxS(g, f, 3, 0, z, 4, 3, z, o.legs ?? k.black); });
  if (o.legs8) sym((f) => { for (const z of [5, 15]) boxS(g, f, 3, 0, z, 4, 3, z, o.legs ?? k.black); });
  if (o.antennae !== false) sym((f) => { boxS(g, f, 5, 14, 3, 5, 16, 3, o.legs ?? k.black); boxS(g, f, 4, 17, 3, 4, 17, 3, o.antennaTip ?? o.legs ?? k.black); });
  if (o.wings === 'bee') sym((f) => rbox(g, Math.min(f(0), f(3)), 10, 8, Math.max(f(0), f(3)), 14, 11, 1, o.wingColor));
  if (o.wings === 'butterfly') sym((f) => {
    rbox(g, Math.min(f(-4), f(5)), 8, 8, Math.max(f(-4), f(5)), 19, 9, 2.5, o.wingColor);    // upper wing
    rbox(g, Math.min(f(-3), f(5)), 2, 9, Math.max(f(-3), f(5)), 8, 10, 2, o.wingColor);     // lower wing
    rbox(g, Math.min(f(-2), f(2)), 12, 7, Math.max(f(-2), f(2)), 16, 7, 1.2, o.wingSpot);   // spots on the front face
    rbox(g, Math.min(f(-1), f(2)), 4, 8, Math.max(f(-1), f(2)), 6, 8, 0.8, o.wingSpot);
  });
  if (o.wings === 'dragonfly') sym((f) => { boxS(g, f, -3, 10, 7, 3, 11, 8, o.wingColor); boxS(g, f, -2, 10, 11, 3, 11, 12, o.wingColor); });
  if (o.shell != null) { rbox(g, 2, 4, 6, 13, 13, 16, 3.5, o.shell); g.box(7, 4, 6, 8, 14, 16, o.shellLine ?? k.black, 'paint'); const r = rng(5); for (let i = 0; i < 7; i++) { const x = 3 + Math.floor(r() * 10), z = 8 + Math.floor(r() * 7); for (let y = 15; y >= 0; y--) if (g.get(x, y, z) >= 0) { g.box(x, y, z, x + 1, y, z + 1, o.shellDot ?? k.black, 'paint'); break; } } }
  if (o.claws != null) sym((f) => { boxS(g, f, 1, 3, 0, 3, 5, 0, o.claws); rbox(g, Math.min(f(-1), f(2)), 4, -1 + 1, Math.max(f(-1), f(2)), 7, 0, 0.8, o.claws); });
  if (o.stinger != null) { for (let t = 0; t < 6; t++) g.box(7, 4 + t * 2, 14 + Math.min(t, 3), 8, 5 + t * 2, 14 + Math.min(t, 3), o.body); g.box(7, 16, 15, 8, 17, 15, o.stinger); }
  eyes(g, k, 4, 8, o.eye ?? k.black, o.head ?? o.body, 5);
  if (o.cheek != null) sym((f) => g.front(f(3), 7, o.cheek));
}

// ------------------------------------------------------------------ the catalogue (11-100)
// [number, id, name, category, colours, template, options]
const S = 16;
export function defineMascots(model) {
  const Q = (n, id, name, cat, cols, o, dims = [S + 6, 30, 24]) => model(id, name, cat, cols, ...dims, (g, k) => quad(shift(g, 3), k, o(k), n));
  const B = (n, id, name, cat, cols, o) => model(id, name, cat, cols, S + 4, 26, 20, (g, k) => bird(g, k, o(k)));
  const F = (n, id, name, cat, cols, o) => model(id, name, cat, cols, 26, 18, 9, (g, k) => fish(g, k, o(k)));
  const R = (n, id, name, cat, cols, o) => model(id, name, cat, cols, S + 2, 16, 26, (g, k) => reptile(g, k, o(k)));
  const U = (n, id, name, cat, cols, o) => model(id, name, cat, cols, S + 8, 20, 22, (g, k) => bug(shift(g, 4), k, o(k)));
  const C1 = 'Happy Friends', C2 = 'Wild Safari', C3 = 'Sky & Sea', C4 = 'Tiny Wonders', C5 = 'Wild North';

  // ---- Happy Friends (11-20)
  Q(11, 'deer', 'Fawn', C1, ['tan', 'cream', 'black', 'white', 'darkBrown', 'pink'], (k) => ({ body: k.tan, belly: k.cream, paw: k.darkBrown, muzzle: k.cream, nose: k.black, ears: 'pointy', earInner: k.pink, antlers: k.darkBrown, tail: 'stub', tailColor: k.white, spots: k.white, spotCount: 10 }));
  Q(12, 'unicorn', 'Unicorn', C1, ['white', 'pink', 'lavender', 'sky', 'gold', 'black', 'hotPink'], (k) => ({ body: k.white, paw: k.lavender, muzzle: k.white, nose: k.pink, blush: k.pink, ears: 'pointy', earInner: k.pink, earRows: 4, uniHorn: k.gold, mane: k.hotPink, tail: 'big', tailColor: k.lavender }));
  Q(13, 'raccoon', 'Raccoon', C1, ['grey', 'darkGrey', 'white', 'black', 'lightGrey'], (k) => ({ body: k.grey, belly: k.lightGrey, paw: k.darkGrey, muzzle: k.white, nose: k.black, mask: k.darkGrey, ears: 'pointy', earInner: k.white, earRows: 4, tail: 'ringed', tailColor: k.grey, tailTip: k.darkGrey }));
  Q(14, 'squirrel', 'Squirrel', C1, ['orange', 'cream', 'black', 'darkBrown', 'white'], (k) => ({ body: k.orange, belly: k.cream, paw: k.darkBrown, muzzle: k.cream, nose: k.darkBrown, ears: 'pointy', earTip: k.darkBrown, earRows: 4, tail: 'big', tailColor: k.orange }));
  Q(15, 'cow', 'Cow', C1, ['white', 'black', 'pink', 'cream', 'tan'], (k) => ({ body: k.white, paw: k.black, muzzle: k.white, nose: null, snout: 'cow', snoutColor: k.pink, nostril: k.hotPink ?? k.tan, ears: 'side', earColor: k.white, horns: k.cream, spots: k.black, spotCount: 18, tail: 'tuft', tailTip: k.black }));
  Q(16, 'pig', 'Piglet', C1, ['pink', 'hotPink', 'black', 'white'], (k) => ({ body: k.pink, muzzle: null, nose: null, snout: 'pig', snoutColor: k.hotPink, nostril: k.black, ears: 'pointy', earRows: 3, tail: 'curl', tailColor: k.hotPink, blush: k.hotPink }));
  Q(17, 'sheep', 'Sheep', C1, ['white', 'cream', 'black', 'pink', 'tan'], (k) => ({ body: k.white, head: k.cream, paw: k.tan, muzzle: k.cream, nose: k.pink, ears: 'side', earColor: k.tan, woolHead: k.white, wool: k.white, tail: 'stub', tailColor: k.white }));
  Q(18, 'tiger', 'Tiger', C1, ['orange', 'black', 'white', 'cream', 'pink'], (k) => ({ body: k.orange, belly: k.white, paw: k.orange, muzzle: k.white, nose: k.pink, ears: 'small', stripes: k.black, headStripes: k.black, tail: 'ringed', tailColor: k.orange, tailTip: k.black }));
  Q(19, 'koala', 'Koala', C1, ['grey', 'lightGrey', 'black', 'white', 'green'], (k) => ({ body: k.grey, belly: k.lightGrey, paw: k.grey, muzzle: null, nose: k.black, ears: 'big', earInner: k.white, bamboo: k.green, tail: 'none' }));
  Q(20, 'dragon', 'Dragon', C1, ['purple', 'lavender', 'gold', 'black', 'hotPink', 'white'], (k) => ({ body: k.purple, belly: k.lavender, paw: k.purple, muzzle: k.lavender, nose: k.purple, horns: k.gold, ears: 'none', wings: k.hotPink, spikes: k.gold, tail: 'long', tailTip: k.gold }));

  // ---- Wild Safari (21-40)
  R(21, 'dino', 'Dino', C2, ['green', 'lime', 'black', 'white', 'yellow'], (k) => ({ body: k.green, belly: k.lime, spikes: k.yellow, cheek: k.lime }));
  Q(22, 'giraffe', 'Giraffe', C2, ['yellow', 'brown', 'black', 'cream', 'darkBrown'], (k) => ({ body: k.yellow, headY: 14, muzzle: k.cream, nose: k.darkBrown, ears: 'small', horns: k.darkBrown, spots: k.brown, spotCount: 36, tail: 'tuft', tailTip: k.darkBrown }));
  Q(23, 'zebra', 'Zebra', C2, ['white', 'black', 'grey', 'pink'], (k) => ({ body: k.white, muzzle: k.grey, nose: k.black, ears: 'pointy', earRows: 4, stripes: k.black, headStripes: k.black, mane: k.black, tail: 'tuft', tailTip: k.black }));
  Q(24, 'hippo', 'Hippo', C2, ['lavender', 'purple', 'pink', 'black', 'white'], (k) => ({ body: k.lavender, muzzle: null, nose: null, snout: 'cow', snoutColor: k.purple, nostril: k.black, ears: 'small', blush: k.pink, tail: 'stub' }));
  Q(25, 'rhino', 'Rhino', C2, ['grey', 'lightGrey', 'white', 'black', 'pink'], (k) => ({ body: k.sky ?? k.grey, muzzle: k.lightGrey, nose: null, hornNose: k.white, ears: 'small', tail: 'stub' }));
  R(26, 'crocodile', 'Crocodile', C2, ['green', 'darkGreen', 'lime', 'white', 'black'], (k) => ({ body: k.green, back: k.darkGreen, belly: k.lime, snout: true, spikes: k.darkGreen }));
  Q(27, 'wolf', 'Wolf', C2, ['grey', 'white', 'black', 'darkGrey', 'sky'], (k) => ({ body: k.grey, belly: k.white, paw: k.darkGrey, muzzle: k.white, nose: k.black, face: k.white, faceTop: 4, ears: 'pointy', earInner: k.white, eye: k.black, tail: 'fluffy', tailColor: k.grey, tailTip: k.white }));
  Q(28, 'monkey', 'Monkey', C2, ['brown', 'tan', 'black', 'darkBrown', 'pink'], (k) => ({ body: k.brown, belly: k.tan, muzzle: k.tan, face: k.tan, faceTop: 7, nose: k.darkBrown, ears: 'side', earColor: k.tan, tail: 'thin', tailColor: k.brown }));
  Q(29, 'gorilla', 'Gorilla', C2, ['darkGrey', 'grey', 'black', 'lightGrey', 'white'], (k) => ({ body: k.darkGrey, belly: k.grey, muzzle: k.grey, face: k.grey, faceTop: 7, nose: k.black, ears: 'small', tail: 'none' }));
  Q(30, 'capybara', 'Capybara', C2, ['tan', 'brown', 'black', 'darkBrown', 'orange'], (k) => ({ body: k.tan, muzzle: k.brown, nose: k.darkBrown, ears: 'small', tail: 'none', woolHead: k.orange }));
  Q(31, 'sloth', 'Sloth', C2, ['tan', 'cream', 'darkBrown', 'black', 'brown'], (k) => ({ body: k.tan, face: k.cream, faceTop: 8, mask: k.darkBrown, muzzle: k.cream, nose: k.black, ears: 'none', tail: 'none' }));
  Q(32, 'beaver', 'Beaver', C2, ['brown', 'tan', 'black', 'white', 'darkBrown'], (k) => ({ body: k.brown, belly: k.tan, muzzle: k.tan, nose: k.black, mouth: k.white, ears: 'small', tail: 'flat', tailTip: k.darkBrown }));
  Q(33, 'hedgehog', 'Hedgehog', C2, ['tan', 'brown', 'darkBrown', 'black', 'cream', 'pink'], (k) => ({ body: k.brown, head: k.tan, face: k.cream, faceTop: 6, muzzle: k.cream, nose: k.black, blush: k.pink, ears: 'small', earColor: k.tan, quills: k.darkBrown, woolHead: k.brown, tail: 'none' }));
  Q(34, 'otter', 'Otter', C2, ['brown', 'cream', 'black', 'darkBrown', 'white'], (k) => ({ body: k.brown, belly: k.cream, muzzle: k.cream, face: k.cream, faceTop: 4, nose: k.black, ears: 'small', tail: 'long' }));
  Q(35, 'walrus', 'Walrus', C2, ['brown', 'tan', 'white', 'black', 'darkBrown'], (k) => ({ body: k.brown, muzzle: k.tan, nose: k.darkBrown, ears: 'none', trunk: null, tusks: k.white, cheeks: k.tan, tail: 'none', tailColor: k.brown }));
  Q(36, 'seal', 'Seal', C2, ['sky', 'lightGrey', 'black', 'white', 'grey'], (k) => ({ body: k.lightGrey, belly: k.white, muzzle: k.white, nose: k.black, ears: 'none', spots: k.grey, spotCount: 10, tail: 'flat', tailTip: k.grey }));
  B(37, 'emperor', 'Emperor Penguin', C2, ['ink', 'white', 'gold', 'orange', 'black', 'darkGrey'], (k) => ({ body: k.ink, belly: k.white, face: k.white, beak: k.orange, cheek: k.gold, feet: k.black, wing: k.ink }));
  B(38, 'owl', 'Owl', C2, ['brown', 'tan', 'cream', 'gold', 'black', 'darkBrown'], (k) => ({ body: k.brown, belly: k.tan, face: k.cream, beak: k.gold, beakType: 'hook', eye: k.black, crest: 'ears', crestColor: k.darkBrown, feet: k.gold, wing: k.darkBrown }));
  B(39, 'eagle', 'Eagle', C2, ['darkBrown', 'white', 'gold', 'black', 'brown'], (k) => ({ body: k.darkBrown, face: k.white, belly: null, beak: k.gold, beakType: 'hook', feet: k.gold, wing: k.brown, tailFeathers: k.white }));
  B(40, 'parrot', 'Parrot', C2, ['red', 'yellow', 'blue', 'green', 'black', 'white', 'cream'], (k) => ({ body: k.red, face: k.white, beak: k.cream, beakType: 'hook', wing: k.blue, chest: k.yellow, tailFeathers: k.green, longTail: true, feet: k.black }));

  // ---- Sky & Sea (41-60)
  B(41, 'flamingo', 'Flamingo', C3, ['pink', 'hotPink', 'black', 'white', 'cream'], (k) => ({ body: k.pink, beak: k.black, beakType: 'long', wing: k.hotPink, feet: k.hotPink, longLegs: true }));
  B(42, 'swan', 'Swan', C3, ['white', 'orange', 'black', 'lightGrey'], (k) => ({ body: k.white, beak: k.orange, beakKnob: k.black, wing: k.lightGrey, feet: k.black, longNeck: true }));
  B(43, 'duck', 'Duckling', C3, ['yellow', 'orange', 'black', 'white', 'gold'], (k) => ({ body: k.yellow, beak: k.orange, beakType: 'duck', wing: k.gold, feet: k.orange, cheek: k.orange }));
  B(44, 'goose', 'Goose', C3, ['white', 'orange', 'black', 'lightGrey'], (k) => ({ body: k.white, beak: k.orange, wing: k.lightGrey, feet: k.orange, longNeck: true }));
  B(45, 'raven', 'Raven', C3, ['ink', 'darkGrey', 'black', 'white', 'navy'], (k) => ({ body: k.ink, beak: k.darkGrey, beakType: 'hook', wing: k.navy, feet: k.darkGrey, eye: k.white, lid: k.ink }));
  B(46, 'crow', 'Crow', C3, ['darkGrey', 'ink', 'black', 'white', 'grey'], (k) => ({ body: k.darkGrey, beak: k.ink, wing: k.ink, feet: k.ink, eye: k.white, lid: k.darkGrey }));
  B(47, 'cuckoo', 'Cuckoo', C3, ['grey', 'white', 'gold', 'black', 'darkGrey'], (k) => ({ body: k.grey, belly: k.white, beak: k.gold, wing: k.darkGrey, feet: k.gold, tailFeathers: k.darkGrey, longTail: true }));
  B(48, 'goldfinch', 'Goldfinch', C3, ['yellow', 'red', 'black', 'white', 'cream'], (k) => ({ body: k.yellow, face: k.red, beak: k.cream, wing: k.black, feet: k.cream, crest: 'crown', crestColor: k.black }));
  B(49, 'sparrow', 'Sparrow', C3, ['brown', 'cream', 'darkBrown', 'black', 'tan'], (k) => ({ body: k.brown, belly: k.cream, beak: k.darkBrown, wing: k.darkBrown, feet: k.tan, chest: k.tan }));
  B(50, 'pigeon', 'Dove', C3, ['white', 'lightGrey', 'orange', 'black', 'pink'], (k) => ({ body: k.white, beak: k.orange, wing: k.lightGrey, feet: k.pink, cheek: k.pink }));
  F(51, 'clownfish', 'Clownfish', C3, ['orange', 'white', 'black'], (k) => ({ body: k.orange, stripes: k.white, edge: k.black, fin: k.orange }));
  F(52, 'bluefish', 'Blue Tang', C3, ['blue', 'yellow', 'navy', 'black', 'white'], (k) => ({ body: k.blue, fin: k.yellow, stripes: k.navy, stripeX: [8], belly: null }));
  F(53, 'shark', 'Shark', C3, ['grey', 'white', 'black', 'darkGrey'], (k) => ({ body: k.grey, belly: k.white, fin: k.darkGrey, length: 8, dorsalH: 4, smile: k.white }));
  F(54, 'dolphin', 'Dolphin', C3, ['sky', 'white', 'black', 'blue'], (k) => ({ body: k.sky, belly: k.white, fin: k.blue, length: 8, tailType: 'whale', dorsalH: 3 }));
  F(55, 'whale', 'Whale', C3, ['blue', 'sky', 'white', 'black', 'navy'], (k) => ({ body: k.blue, belly: k.sky, fin: k.navy, length: 8, height: 5.5, tailType: 'whale', dorsal: false, blow: k.white }));
  model('jellyfish', 'Jellyfish', C3, ['lavender', 'pink', 'hotPink', 'black', 'white'], 16, 22, 16, (g, k) => {
    rbox(g, 1, 10, 1, 14, 20, 14, 5, k.lavender); g.box(1, 10, 1, 14, 10, 14, k.pink, 'paint');
    for (const [x, z] of [[3, 4], [6, 2], [9, 2], [12, 4], [4, 10], [11, 10], [7, 12]]) for (let y = 1; y < 10; y++) g.set(x + ((y >> 1) % 2), y, z, y % 3 ? k.pink : k.hotPink);
    eyes(g, k, 4, 13, k.black, k.lavender, 1); sym((f) => g.front(f(3), 12, k.hotPink));
  });
  model('octopus', 'Octopus', C3, ['hotPink', 'pink', 'black', 'white', 'purple'], 20, 18, 20, (g, k) => {
    rbox(g, 4, 6, 4, 15, 17, 15, 4.5, k.hotPink);
    for (let i = 0; i < 8; i++) { const a = i / 8 * Math.PI * 2; for (let t = 0; t < 6; t++) { const r = 4 + t * 0.9; g.set(Math.round(9.5 + Math.cos(a) * r), Math.max(0, 5 - t + (t > 3 ? 1 : 0)), Math.round(9.5 + Math.sin(a) * r), t % 2 ? k.pink : k.hotPink); g.set(Math.round(9.5 + Math.cos(a) * r), Math.max(0, 5 - t + (t > 3 ? 1 : 0)) + 1, Math.round(9.5 + Math.sin(a) * r), k.hotPink); } }
    const sub = shift(g, 2);
    eyes(sub, k, 3, 10, k.black, k.hotPink, 6); sym((f) => sub.front(f(2), 9, k.pink));
  });
  model('crab', 'Crab', C3, ['red', 'orange', 'white', 'black'], 22, 14, 14, (g, k) => {
    rbox(g, 4, 2, 4, 17, 8, 12, 2.5, k.red);
    const sub = shift(g, 3);
    eyes(sub, k, 3, 4, k.black, k.red, 2);
    for (const s of [-1, 1]) { const cx = 10.5 + s * 9; g.box(Math.round(10.5 + s * 6.5), 4, 4, Math.round(cx), 5, 4, k.red); rbox(g, Math.round(cx) - 1, 5, 1, Math.round(cx) + 1, 9, 3, 1, k.red); g.box(Math.round(cx) - 1, 9, 1, Math.round(cx) - 1, 11, 2, k.orange); for (const z of [6, 8, 10]) { g.box(Math.round(10.5 + s * 6), 1, z, Math.round(10.5 + s * 8), 1, z, k.orange); g.box(Math.round(10.5 + s * 8), 0, z, Math.round(10.5 + s * 8), 0, z, k.orange); } }
  });
  model('seahorse', 'Seahorse', C3, ['gold', 'orange', 'yellow', 'black', 'white'], 14, 24, 10, (g, k) => {
    rbox(g, 3, 13, 2, 11, 21, 8, 2.5, k.gold); g.box(1, 15, 3, 3, 17, 6, k.gold);
    rbox(g, 4, 5, 3, 10, 14, 8, 2, k.gold); g.box(4, 5, 3, 10, 12, 3, k.yellow, 'paint');
    for (let t = 0; t < 5; t++) g.box(6 + (t > 2 ? 2 : 0), 4 - t, 4, 8 + (t > 2 ? 2 : 0), 4 - t, 6, k.orange);
    g.box(9, 0, 4, 11, 1, 6, k.orange);
    for (let y = 8; y <= 20; y += 2) g.box(10, y, 5, 11, y, 6, k.orange);
    eyes(g, k, 4, 17, k.black, k.gold, 12);
  });
  R(60, 'seaturtle', 'Sea Turtle', C3, ['teal', 'green', 'mint', 'black', 'white', 'darkGreen'], (k) => ({ body: k.mint, shell: k.teal, shellDark: k.darkGreen, short: true, noTail: true }));

  // ---- Tiny Wonders (61-80)
  R(61, 'turtle', 'Turtle', C4, ['lime', 'green', 'darkGreen', 'black', 'white', 'tan'], (k) => ({ body: k.lime, shell: k.green, shellDark: k.darkGreen, short: true, noTail: true }));
  R(62, 'frog', 'Frog', C4, ['green', 'lime', 'black', 'white', 'pink'], (k) => ({ body: k.green, belly: k.lime, bulge: true, short: true, noTail: true, cheek: k.pink, mouth: k.darkGreen ?? k.black }));
  R(63, 'treefrog', 'Tree Frog', C4, ['lime', 'yellow', 'black', 'white', 'orange', 'red'], (k) => ({ body: k.lime, belly: k.yellow, bulge: true, eye: k.red, short: true, noTail: true, cheek: k.orange }));
  R(64, 'salamander', 'Salamander', C4, ['orange', 'black', 'yellow', 'white', 'red'], (k) => ({ body: k.black, dots: k.yellow, belly: k.orange, tail: k.black, eye: k.black }));
  model('snake', 'Snake', C4, ['lime', 'green', 'yellow', 'black', 'white', 'red'], 18, 16, 18, (g, k) => {
    for (let ring = 0; ring < 3; ring++) for (let a = 0; a < 40; a++) { const t = a / 40 * Math.PI * 2, r = 6.5 - ring * 1.6; g.ell(9 + Math.cos(t) * r, 1.5 + ring * 2.2, 9 + Math.sin(t) * r, 1.6, 1.4, 1.6, (a + ring * 7) % 8 < 2 ? k.yellow : k.lime); }
    rbox(g, 3, 7, 3, 14, 14, 11, 2.6, k.lime); g.box(6, 7, 3, 11, 13, 11, k.lime);
    const sub = shift(g, 1);
    eyes(sub, k, 3, 10, k.black, k.lime, 7); g.box(8, 7, 2, 9, 7, 2, k.red); g.box(8, 6, 2, 8, 6, 2, k.red); g.box(9, 6, 2, 9, 6, 2, k.red);
  });
  R(66, 'chameleon', 'Chameleon', C4, ['lime', 'teal', 'yellow', 'black', 'white', 'orange'], (k) => ({ body: k.lime, back: k.teal, crest: k.orange, curlTail: true, noTail: true, dots: k.yellow, seed: 6 }));
  R(67, 'lizard', 'Lizard', C4, ['green', 'lime', 'yellow', 'black', 'white'], (k) => ({ body: k.green, belly: k.yellow, back: k.green, dots: k.lime, seed: 9 }));
  R(68, 'iguana', 'Iguana', C4, ['teal', 'green', 'orange', 'black', 'white', 'lime'], (k) => ({ body: k.teal, back: k.green, spikes: k.orange, crest: k.orange, tail: k.green }));
  R(69, 'gecko', 'Gecko', C4, ['yellow', 'orange', 'black', 'white', 'lime'], (k) => ({ body: k.yellow, dots: k.orange, belly: k.lime, eye: k.black, seed: 4 }));
  Q(70, 'manticore', 'Manticore', C4, ['gold', 'rust', 'rustDark', 'black', 'cream', 'purple', 'red'], (k) => ({ body: k.gold, muzzle: k.cream, nose: k.rustDark, ears: 'small', mane: k.rust, wings: k.purple, tail: 'scorpion', tailColor: k.rust, tailTip: k.red }));
  model('bat', 'Bat', C4, ['purple', 'lavender', 'pink', 'black', 'white'], 26, 22, 16, (g, k) => {
    const sub = shift(g, 5);
    const h = chibi(sub, k, { body: k.purple, belly: k.lavender, muzzle: k.lavender, nose: k.pink, blush: k.pink });
    pointyEar(sub, h.top - 1, k.purple, k.purple, k.pink, 5);
    for (const s of [-1, 1]) for (let t = 0; t < 6; t++) { const x = s < 0 ? 4 - t : 21 + t; g.box(x, 6 + (t >> 1), 9, x, 13 - (t > 3 ? t - 3 : 0), 10, t % 2 ? k.lavender : k.purple); }
  });
  Q(72, 'desertheg', 'Desert Hedgehog', C4, ['cream', 'tan', 'brown', 'black', 'pink', 'white'], (k) => ({ body: k.tan, head: k.cream, face: k.white, faceTop: 6, muzzle: k.white, nose: k.black, blush: k.pink, ears: 'big', earColor: k.cream, earInner: k.pink, quills: k.brown, woolHead: k.tan, tail: 'none' }));
  U(73, 'scorpion', 'Scorpion', C4, ['orange', 'rust', 'black', 'white', 'red'], (k) => ({ body: k.orange, head: k.orange, claws: k.rust, stinger: k.red, antennae: false, legs: k.rust }));
  U(74, 'spider', 'Spider', C4, ['purple', 'ink', 'black', 'white', 'hotPink'], (k) => ({ body: k.ink, head: k.purple, legs: k.ink, legs8: true, antennae: false, cheek: k.hotPink }));
  U(75, 'ant', 'Ant', C4, ['red', 'darkBrown', 'black', 'white'], (k) => ({ body: k.red, head: k.red, legs: k.darkBrown, long: true }));
  U(76, 'bee', 'Bee', C4, ['yellow', 'ink', 'black', 'white', 'sky', 'pink'], (k) => ({ body: k.yellow, head: k.yellow, stripes: k.ink, wings: 'bee', wingColor: k.sky, legs: k.ink, cheek: k.pink }));
  U(77, 'butterfly', 'Butterfly', C4, ['ink', 'hotPink', 'lavender', 'black', 'white', 'yellow'], (k) => ({ body: k.ink, head: k.ink, slim: true, wings: 'butterfly', wingColor: k.hotPink, wingSpot: k.yellow, legs: k.ink, antennaTip: k.hotPink, eye: k.white, lid: k.ink }));
  U(78, 'ladybug', 'Ladybug', C4, ['red', 'ink', 'black', 'white'], (k) => ({ body: k.ink, head: k.ink, shell: k.red, shellDot: k.ink, shellLine: k.ink, legs: k.ink, eye: k.white }));
  U(79, 'dragonfly', 'Dragonfly', C4, ['teal', 'sky', 'blue', 'black', 'white', 'lime'], (k) => ({ body: k.teal, head: k.blue, wings: 'dragonfly', wingColor: k.sky, long: true, legs: k.blue }));
  model('snail', 'Snail', C4, ['cream', 'orange', 'brown', 'black', 'white', 'pink'], 16, 18, 22, (g, k) => {
    rbox(g, 3, 0, 0, 12, 4, 20, 2, k.cream);
    rbox(g, 4, 3, 0, 11, 11, 6, 2.2, k.cream);
    for (let i = 0; i < 4; i++) rbox(g, 2 + i, 4 + i, 8 + i, 13 - i, 15 - i, 19 - i, 3 - i * 0.5, i % 2 ? k.brown : k.orange);
    sym((f) => { boxS(g, f, 5, 12, 2, 5, 15, 2, k.cream); });
    const sub = shift(g, 1);
    eyes(sub, k, 3, 7, k.black, k.cream, 3); sym((f) => sub.front(f(2), 6, k.pink));
  });

  // ---- Wild North (81-100)
  Q(81, 'redpanda', 'Red Panda', C5, ['orange', 'white', 'darkBrown', 'black', 'cream'], (k) => ({ body: k.orange, belly: k.darkBrown, paw: k.darkBrown, muzzle: k.white, face: k.white, faceTop: 4, nose: k.black, ears: 'pointy', earInner: k.white, earRows: 4, tail: 'ringed', tailColor: k.orange, tailTip: k.darkBrown }));
  Q(82, 'ferret', 'Ferret', C5, ['cream', 'brown', 'black', 'white', 'pink'], (k) => ({ body: k.brown, head: k.cream, mask: k.brown, muzzle: k.white, nose: k.pink, ears: 'small', earColor: k.cream, tail: 'long' }));
  F(83, 'pike', 'Pike', C5, ['green', 'lime', 'cream', 'black', 'darkGreen'], (k) => ({ body: k.green, belly: k.cream, fin: k.darkGreen, length: 8, height: 4, stripes: k.lime, stripeX: [5, 9, 13] }));
  Q(84, 'lynx', 'Lynx', C5, ['tan', 'cream', 'black', 'darkBrown', 'white', 'pink'], (k) => ({ body: k.tan, belly: k.cream, muzzle: k.white, face: k.cream, faceTop: 4, nose: k.pink, ears: 'pointy', earTip: k.black, spots: k.darkBrown, spotCount: 14, tail: 'stub', tailColor: k.black }));
  Q(85, 'wildcat', 'Wildcat', C5, ['grey', 'darkGrey', 'cream', 'black', 'pink'], (k) => ({ body: k.grey, belly: k.cream, muzzle: k.cream, nose: k.pink, ears: 'pointy', earRows: 4, earInner: k.pink, stripes: k.darkGrey, tail: 'ringed', tailColor: k.grey, tailTip: k.black }));
  Q(86, 'cheetah', 'Cheetah', C5, ['gold', 'cream', 'black', 'white', 'darkBrown'], (k) => ({ body: k.gold, belly: k.cream, muzzle: k.white, nose: k.black, ears: 'small', spots: k.black, spotCount: 40, tail: 'ringed', tailColor: k.gold, tailTip: k.black }));
  Q(87, 'jaguar', 'Jaguar', C5, ['orange', 'darkBrown', 'black', 'cream', 'white'], (k) => ({ body: k.orange, belly: k.cream, muzzle: k.cream, nose: k.darkBrown, ears: 'small', spots: k.darkBrown, spotCount: 34, tail: 'ringed', tailColor: k.orange, tailTip: k.darkBrown }));
  Q(88, 'leopard', 'Leopard', C5, ['yellow', 'black', 'cream', 'white', 'pink'], (k) => ({ body: k.yellow, belly: k.white, muzzle: k.white, nose: k.pink, ears: 'small', spots: k.black, spotCount: 30, tail: 'thin', tailColor: k.yellow, tailTip: k.black }));
  Q(89, 'aurochs', 'Wild Ox', C5, ['brown', 'darkBrown', 'cream', 'black', 'tan'], (k) => ({ body: k.darkBrown, head: k.brown, muzzle: k.tan, nose: null, snout: 'cow', snoutColor: k.tan, nostril: k.black, ears: 'side', earColor: k.brown, horns: k.cream, woolHead: k.darkBrown, tail: 'tuft', tailTip: k.black }));
  Q(90, 'moose', 'Moose', C5, ['brown', 'darkBrown', 'tan', 'black', 'cream'], (k) => ({ body: k.darkBrown, head: k.brown, muzzle: k.tan, nose: k.black, ears: 'side', earColor: k.brown, antlers: k.tan, tail: 'stub' }));
  Q(91, 'mammoth', 'Mammoth', C5, ['brown', 'darkBrown', 'cream', 'black', 'white', 'tan'], (k) => ({ body: k.brown, muzzle: null, nose: null, trunk: k.brown, tusks: k.cream, ears: 'side', earColor: k.darkBrown, woolHead: k.darkBrown, tail: 'tuft', tailTip: k.darkBrown }));
  Q(92, 'stag', 'Stag', C5, ['brown', 'cream', 'black', 'darkBrown', 'white'], (k) => ({ body: k.brown, belly: k.cream, muzzle: k.cream, nose: k.black, ears: 'pointy', earRows: 4, antlers: k.darkBrown, tail: 'stub', tailColor: k.white }));
  Q(93, 'polarwolf', 'Arctic Wolf', C5, ['white', 'lightGrey', 'black', 'sky', 'pink'], (k) => ({ body: k.white, belly: k.white, paw: k.lightGrey, muzzle: k.white, nose: k.black, ears: 'pointy', earInner: k.pink, eye: k.black, tail: 'fluffy', tailColor: k.white, tailTip: k.lightGrey }));
  Q(94, 'polarbear', 'Polar Bear', C5, ['white', 'lightGrey', 'black', 'cream', 'pink'], (k) => ({ body: k.white, belly: k.cream, paw: k.lightGrey, muzzle: k.cream, nose: k.black, blush: k.pink, ears: 'round', earInner: k.lightGrey, tail: 'stub' }));
  B(95, 'babypenguin', 'Baby Penguin', C5, ['grey', 'white', 'ink', 'black', 'orange', 'pink'], (k) => ({ body: k.grey, face: k.white, belly: k.white, beak: k.ink, feet: k.ink, wing: k.grey, cheek: k.pink, crest: 'tuft', crestColor: k.grey }));
  Q(96, 'reindeer', 'Reindeer', C5, ['brown', 'cream', 'red', 'black', 'tan', 'white'], (k) => ({ body: k.brown, belly: k.cream, muzzle: k.cream, nose: k.red, ears: 'pointy', earRows: 4, antlers: k.tan, tail: 'stub', tailColor: k.white }));
  Q(97, 'alpaca', 'Alpaca', C5, ['cream', 'white', 'black', 'pink', 'tan'], (k) => ({ body: k.cream, headY: 12, muzzle: k.white, nose: k.tan, blush: k.pink, ears: 'pointy', earRows: 3, woolHead: k.white, wool: k.white, tail: 'stub' }));
  Q(98, 'camel', 'Camel', C5, ['tan', 'cream', 'black', 'brown', 'white'], (k) => ({ body: k.tan, headY: 12, muzzle: k.cream, nose: k.brown, ears: 'small', hump: k.tan, woolHead: k.brown, tail: 'tuft', tailTip: k.brown }));
  Q(99, 'llama', 'Llama', C5, ['white', 'cream', 'black', 'pink', 'hotPink', 'sky'], (k) => ({ body: k.white, headY: 13, muzzle: k.cream, nose: k.pink, ears: 'long', earInner: k.pink, woolHead: k.cream, tail: 'stub', back: k.hotPink }));
  Q(100, 'bison', 'Bison', C5, ['darkBrown', 'brown', 'black', 'cream', 'tan'], (k) => ({ body: k.brown, head: k.darkBrown, muzzle: k.tan, nose: k.black, ears: 'side', earColor: k.darkBrown, horns: k.cream, woolHead: k.darkBrown, hump: k.darkBrown, tail: 'tuft', tailTip: k.black }));
}
