// quick difficulty probe: node tools/conveyor/probe.mjs k n st fill clump vis
import { rng } from '../voxel.mjs';
import { Session, solve, casualWinRate, P } from './rules.mjs';
import { generateFor } from './build-levels.mjs';
const [k, n, st, fill, clump, vis] = process.argv.slice(2).map(Number);
let res = [];
for (let a = 0; a < 6; a++) {
  const l = generateFor(99, { k, n, size: [4, 4], st, vis, fill, clump }, 777 + a * 31);
  const sol = solve(l);
  res.push((sol ? 'S' : 'x') + casualWinRate(l, 12, rng(a + 5)).toFixed(2));
}
console.log(process.argv.slice(2).join(' '), '->', res.join(' '));
