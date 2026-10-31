#!/usr/bin/env python3
"""Writes Docs/LEVELS_VERIFICATION.md from levels.json + the validator CSV (Tools/LevelGen -- validate).

Usage (from Veliqora/):  python3 -I Tools/LevelReport/level_report.py [count=100]
Only reports what the validator actually executed; it never marks a level PASS on its own.
"""
import csv, json, sys, os

root = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
count = int(sys.argv[1]) if len(sys.argv) > 1 else 100
levels = json.load(open(os.path.join(root, "Assets/_Veliqora/Resources/Veliqora/Levels/levels.json")))["levels"]
rows = {int(r["level"]): r for r in csv.DictReader(open(os.path.join(root, "Docs/level_validation_report.csv")))}

def mech(l):
    m = []
    if l.get("hidden"): m.append("mystery")
    if l.get("locks"): m.append("cork×%d" % len(l["locks"]))
    if l.get("rewardBottles"): m.append("ad bottle")
    caps = l.get("capacities")
    if caps and len(set(caps)) > 1: m.append("sizes " + "/".join(str(c) for c in sorted(set(caps))))
    return ", ".join(m) or "—"

out = []
total_pass = sum(1 for n in range(1, count + 1) if rows.get(n, {}).get("passed") == "PASS")
all_pass = sum(1 for r in rows.values() if r["passed"] == "PASS")
out.append("# Level verification report\n")
out.append("Generated from `Docs/level_validation_report.csv`, written by `dotnet run -c Release --project Tools/LevelGen -- validate`.\n")
out.append(f"**Levels 1–{count}: {total_pass} / {count} PASS.** Whole catalog: {all_pass} / {len(rows)} PASS.\n")
out.append("""For every level the validator executed:
1. color distribution check;
2. the start is not already solved, no bottle starts monochrome or complete, every color is spread over at least two bottles;
3. the color count is inside the published range (1–10: 3–4 · 11–25: 4–5 · 26–50: 5–6 · 51–100: 6–8 · 101+: 6–8);
4. the layout is not a duplicate of any earlier level (canonical board key);
5. the stored solution is replayed move by move to a win (objectives checked at the win);
6. an independent solver run from the start state;
7. levels 1–100: exact minimum number of pours by exhaustive breadth-first search
   (level 1 must need ≥ 6, levels 2–10 ≥ 7). "—" = state space above the 300 000-state search budget; the benchmark is then an upper bound.

Columns: **par** = verified benchmark solution length used for stars; **min** = proven minimum pours;
**solver** = pours found by the independent solver run.
""")
out.append("| Level | Kind | Colors | Bottles (empty) | Capacity | Mechanics | Par | Min | Solver | Result |")
out.append("|---|---|---|---|---|---|---|---|---|---|")
for n in range(1, count + 1):
    l = levels[n - 1]
    r = rows.get(n)
    bottles = l["bottles"]
    empties = sum(1 for b in bottles if len(b) == 0)
    caps = l.get("capacities")
    cap = str(l.get("capacity", 4)) if not caps or len(set(caps)) == 1 else "mixed"
    note = r["note"] if r else ""
    solver = ""
    if "solver " in note and " moves" in note:
        solver = note.split("solver ")[1].split(" moves")[0]
    out.append(f"| {n} | {l.get('kind', 'normal') if isinstance(l.get('kind'), str) else ['normal','hard','boss'][l.get('kind', 0)]} | {l['colors'] if 'colors' in l else ''} | "
               f"{len(bottles)} ({empties}) | {cap} | {mech(l)} | {l['par']} | {r['min_moves'] or '—' if r else '—'} | {solver} | {r['passed'] if r else 'not run'} |")
open(os.path.join(root, "Docs/LEVELS_VERIFICATION.md"), "w").write("\n".join(out) + "\n")
print(f"levels 1-{count}: {total_pass}/{count} PASS; catalog {all_pass}/{len(rows)} PASS")
