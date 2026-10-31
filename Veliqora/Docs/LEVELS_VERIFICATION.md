# Level verification report

Generated from `Docs/level_validation_report.csv`, written by `dotnet run -c Release --project Tools/LevelGen -- validate`.

**Levels 1–100: 100 / 100 PASS.** Whole catalog: 300 / 300 PASS.

For every level the validator executed:
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

| Level | Kind | Colors | Bottles (empty) | Capacity | Mechanics | Par | Min | Solver | Result |
|---|---|---|---|---|---|---|---|---|---|
| 1 | normal | 3 | 5 (2) | 4 | — | 11 | 10 | 11 | PASS |
| 2 | normal | 4 | 6 (2) | 4 | — | 13 | 13 | 13 | PASS |
| 3 | normal | 3 | 5 (2) | 4 | — | 9 | 9 | 9 | PASS |
| 4 | normal | 4 | 6 (2) | 4 | — | 11 | 11 | 11 | PASS |
| 5 | normal | 4 | 6 (2) | 4 | — | 12 | 12 | 13 | PASS |
| 6 | normal | 4 | 6 (2) | 4 | — | 13 | 12 | 13 | PASS |
| 7 | normal | 4 | 6 (2) | 4 | — | 13 | 13 | 13 | PASS |
| 8 | normal | 4 | 6 (2) | 4 | — | 12 | 12 | 12 | PASS |
| 9 | normal | 3 | 5 (2) | 4 | — | 8 | 8 | 8 | PASS |
| 10 | normal | 4 | 6 (2) | 4 | — | 12 | 11 | 12 | PASS |
| 11 | normal | 4 | 6 (2) | 4 | — | 13 | 12 | 14 | PASS |
| 12 | normal | 4 | 6 (2) | 4 | — | 11 | 11 | 11 | PASS |
| 13 | hard | 5 | 7 (2) | 4 | — | 17 | 16 | 17 | PASS |
| 14 | normal | 5 | 7 (2) | 4 | — | 16 | 15 | 16 | PASS |
| 15 | normal | 4 | 6 (2) | 4 | — | 11 | 11 | 11 | PASS |
| 16 | normal | 5 | 7 (2) | 4 | — | 15 | 14 | 15 | PASS |
| 17 | hard | 5 | 7 (2) | 4 | — | 15 | 14 | 15 | PASS |
| 18 | normal | 5 | 7 (2) | 4 | — | 17 | 16 | 17 | PASS |
| 19 | normal | 4 | 6 (2) | 4 | — | 13 | 12 | 13 | PASS |
| 20 | boss | 5 | 7 (2) | 4 | — | 17 | 16 | 17 | PASS |
| 21 | normal | 4 | 6 (2) | 4 | — | 11 | 11 | 11 | PASS |
| 22 | hard | 5 | 7 (2) | 4 | — | 16 | 16 | 16 | PASS |
| 23 | normal | 4 | 6 (2) | 4 | — | 12 | 12 | 12 | PASS |
| 24 | normal | 5 | 7 (2) | 4 | — | 14 | 14 | 14 | PASS |
| 25 | normal | 4 | 6 (2) | 4 | — | 11 | 11 | 11 | PASS |
| 26 | hard | 6 | 8 (2) | 4 | — | 19 | 18 | 20 | PASS |
| 27 | normal | 6 | 8 (2) | 4 | — | 18 | 17 | 18 | PASS |
| 28 | hard | 6 | 8 (2) | 4 | — | 20 | 19 | 21 | PASS |
| 29 | normal | 5 | 7 (2) | 4 | — | 18 | 16 | 19 | PASS |
| 30 | boss | 6 | 8 (2) | 4 | — | 19 | 18 | 19 | PASS |
| 31 | normal | 5 | 7 (2) | 4 | — | 14 | 14 | 15 | PASS |
| 32 | normal | 6 | 8 (2) | 4 | — | 19 | 18 | 20 | PASS |
| 33 | normal | 5 | 7 (2) | 4 | — | 15 | 15 | 16 | PASS |
| 34 | normal | 6 | 8 (2) | 4 | — | 21 | 19 | 21 | PASS |
| 35 | normal | 6 | 8 (2) | 4 | — | 21 | 20 | 21 | PASS |
| 36 | normal | 5 | 6 (1) | 4 | — | 16 | 16 | 16 | PASS |
| 37 | normal | 5 | 7 (2) | 4 | — | 17 | 15 | 17 | PASS |
| 38 | normal | 5 | 6 (1) | 4 | — | 16 | 16 | 16 | PASS |
| 39 | normal | 6 | 8 (2) | 4 | — | 21 | 20 | 21 | PASS |
| 40 | boss | 6 | 7 (1) | 4 | — | 20 | 20 | 20 | PASS |
| 41 | normal | 6 | 8 (2) | 4 | — | 20 | 19 | 20 | PASS |
| 42 | normal | 5 | 6 (1) | 4 | — | 14 | 14 | 14 | PASS |
| 43 | normal | 5 | 7 (2) | 4 | — | 15 | 13 | 15 | PASS |
| 44 | normal | 6 | 7 (1) | 4 | — | 17 | 17 | 17 | PASS |
| 45 | normal | 5 | 7 (2) | 4 | — | 15 | 15 | 15 | PASS |
| 46 | hard | 6 | 7 (1) | 4 | — | 21 | 20 | 21 | PASS |
| 47 | normal | 6 | 8 (2) | 4 | — | 20 | 19 | 20 | PASS |
| 48 | normal | 6 | 7 (1) | 4 | — | 19 | 18 | 19 | PASS |
| 49 | normal | 5 | 7 (2) | 4 | — | 15 | 15 | 15 | PASS |
| 50 | boss | 6 | 7 (1) | 4 | — | 20 | 20 | 20 | PASS |
| 51 | normal | 7 | 9 (2) | 4 | — | 24 | 23 | 25 | PASS |
| 52 | hard | 7 | 8 (1) | 4 | — | 24 | 23 | 24 | PASS |
| 53 | normal | 7 | 9 (2) | 4 | — | 22 | 21 | 22 | PASS |
| 54 | normal | 6 | 7 (1) | 4 | — | 16 | 16 | 16 | PASS |
| 55 | normal | 7 | 9 (2) | 4 | — | 25 | 23 | 25 | PASS |
| 56 | normal | 6 | 7 (1) | 4 | — | 18 | 18 | 18 | PASS |
| 57 | hard | 8 | 10 (2) | 4 | — | 26 | 24 | 27 | PASS |
| 58 | normal | 6 | 7 (1) | 4 | — | 20 | 19 | 20 | PASS |
| 59 | normal | 7 | 9 (2) | 4 | — | 23 | 22 | 23 | PASS |
| 60 | boss | 8 | 9 (1) | 4 | — | 25 | 25 | 25 | PASS |
| 61 | normal | 7 | 9 (2) | 4 | — | 24 | 22 | 24 | PASS |
| 62 | normal | 6 | 7 (1) | 4 | — | 19 | 19 | 19 | PASS |
| 63 | normal | 7 | 9 (2) | 4 | — | 23 | 21 | 23 | PASS |
| 64 | hard | 7 | 8 (1) | 4 | — | 21 | 21 | 21 | PASS |
| 65 | normal | 7 | 9 (2) | 4 | — | 21 | 20 | 21 | PASS |
| 66 | hard | 7 | 8 (1) | 4 | — | 22 | 22 | 22 | PASS |
| 67 | normal | 8 | 10 (2) | 4 | — | 26 | 26 | 26 | PASS |
| 68 | normal | 6 | 7 (1) | 4 | — | 17 | 16 | 17 | PASS |
| 69 | normal | 7 | 9 (2) | 4 | — | 21 | 21 | 21 | PASS |
| 70 | boss | 8 | 9 (1) | 4 | — | 26 | 26 | 26 | PASS |
| 71 | normal | 6 | 8 (2) | 4 | mystery | 22 | 21 | 23 | PASS |
| 72 | normal | 7 | 9 (2) | 4 | mystery | 24 | 23 | 24 | PASS |
| 73 | normal | 6 | 8 (2) | 4 | mystery | 20 | 19 | 20 | PASS |
| 74 | normal | 8 | 10 (2) | 4 | mystery | 28 | 26 | 28 | PASS |
| 75 | normal | 6 | 8 (2) | 4 | mystery | 21 | 19 | 21 | PASS |
| 76 | normal | 7 | 9 (2) | 4 | mystery | 25 | 23 | 26 | PASS |
| 77 | normal | 6 | 8 (2) | 4 | mystery | 18 | 18 | 18 | PASS |
| 78 | normal | 8 | 10 (2) | 4 | mystery | 28 | 26 | 30 | PASS |
| 79 | normal | 6 | 8 (2) | 4 | mystery | 21 | 20 | 21 | PASS |
| 80 | boss | 8 | 10 (2) | 4 | mystery | 27 | 26 | 29 | PASS |
| 81 | normal | 6 | 8 (2) | 4 | mystery | 21 | 20 | 22 | PASS |
| 82 | normal | 6 | 8 (2) | 4 | mystery | 21 | 20 | 21 | PASS |
| 83 | normal | 6 | 8 (2) | 4 | mystery | 18 | 18 | 19 | PASS |
| 84 | normal | 7 | 9 (2) | 4 | mystery | 24 | 23 | 25 | PASS |
| 85 | normal | 7 | 9 (2) | 4 | mystery | 22 | 22 | 22 | PASS |
| 86 | normal | 6 | 8 (2) | mixed | mystery, sizes 3/4/5 | 19 | 17 | 19 | PASS |
| 87 | normal | 8 | 10 (2) | mixed | sizes 3/4/5 | 28 | 26 | 28 | PASS |
| 88 | normal | 6 | 8 (2) | mixed | mystery, sizes 3/4/5 | 22 | 20 | 22 | PASS |
| 89 | normal | 7 | 9 (2) | mixed | sizes 4/5 | 27 | 26 | 29 | PASS |
| 90 | boss | 8 | 10 (2) | mixed | mystery, sizes 3/4/5 | 35 | — | 35 | PASS |
| 91 | normal | 7 | 9 (2) | mixed | sizes 3/4/5 | 21 | 20 | 22 | PASS |
| 92 | normal | 7 | 9 (2) | mixed | mystery, sizes 3/4/5 | 25 | 21 | 26 | PASS |
| 93 | normal | 7 | 9 (2) | mixed | sizes 3/4/5 | 21 | 21 | 21 | PASS |
| 94 | normal | 6 | 8 (2) | mixed | mystery, sizes 3/4/5 | 21 | 20 | 22 | PASS |
| 95 | normal | 6 | 8 (2) | mixed | sizes 3/4/5 | 27 | 23 | 27 | PASS |
| 96 | normal | 6 | 8 (2) | mixed | mystery, sizes 3/4/5 | 19 | 19 | 19 | PASS |
| 97 | normal | 8 | 10 (2) | mixed | sizes 3/4/5 | 27 | 26 | 27 | PASS |
| 98 | normal | 6 | 8 (2) | mixed | mystery, sizes 3/4 | 18 | 16 | 18 | PASS |
| 99 | normal | 7 | 9 (2) | mixed | sizes 3/4/5 | 20 | 18 | 21 | PASS |
| 100 | boss | 8 | 10 (2) | mixed | mystery, sizes 4/5 | 35 | 31 | 37 | PASS |
