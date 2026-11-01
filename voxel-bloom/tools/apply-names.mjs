// Applies Romanian names to already generated data (no level regeneration needed): node tools/apply-names.mjs
import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
import { RO, CHAPTERS_RO } from './names-ro.mjs';
const OUT = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../Assets/VoxelBloom/Data/Resources/VoxelBloom');
let n = 0;
for (const dir of ['Models', 'Levels']) for (const f of fs.readdirSync(path.join(OUT, dir))) {
  const p = path.join(OUT, dir, f), j = JSON.parse(fs.readFileSync(p));
  const id = dir === 'Models' ? j.id : j.model;
  if (RO[id]) { j.name = RO[id]; fs.writeFileSync(p, JSON.stringify(j)); n++; }
}
const cp = path.join(OUT, 'Config/game_config.json'), cfg = JSON.parse(fs.readFileSync(cp));
const oldNames = cfg.chapters.map((c) => c.name);
cfg.chapters.forEach((c, i) => { if (CHAPTERS_RO[i]) c.name = CHAPTERS_RO[i]; });
cfg.collection.forEach((e) => { if (RO[e.id]) e.name = RO[e.id]; const k = oldNames.indexOf(e.category); if (k >= 0) e.category = CHAPTERS_RO[k]; });
fs.writeFileSync(cp, JSON.stringify(cfg, null, 2));
// model JSON categories follow the chapters too
for (const f of fs.readdirSync(path.join(OUT, 'Models'))) {
  const p = path.join(OUT, 'Models', f), j = JSON.parse(fs.readFileSync(p)); const k = oldNames.indexOf(j.category);
  if (k >= 0) { j.category = CHAPTERS_RO[k]; fs.writeFileSync(p, JSON.stringify(j)); }
}
console.log('renamed', n, 'files');
