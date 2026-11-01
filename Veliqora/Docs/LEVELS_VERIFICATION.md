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

**Free space** = empty slots on the board (ad-locked bonus bottles excluded): room is often spread over partly filled
bottles and small 2-slot helper bottles instead of two big empty bottles.

Columns: **par** = verified benchmark solution length used for stars; **min** = proven minimum pours;
**solver** = pours found by the independent solver run.

| Level | Kind | Colors | Bottles | Free space | Mechanics | Par | Min | Solver | Result |
|---|---|---|---|---|---|---|---|---|---|
| 1 | normal | 3 | 5 | 8 slots: 2 empty | — | 11 | 10 | 11 | PASS |
| 2 | normal | 4 | 6 | 8 slots: 2 empty | — | 13 | 13 | 13 | PASS |
| 3 | normal | 3 | 5 | 8 slots: 2 empty | — | 9 | 9 | 9 | PASS |
| 4 | normal | 4 | 6 | 8 slots: 2 empty | — | 11 | 11 | 11 | PASS |
| 5 | normal | 4 | 6 | 8 slots: 2 empty | — | 12 | 12 | 13 | PASS |
| 6 | normal | 4 | 7 | 8 slots: 2 partly filled, 2 small empty | — | 14 | 12 | 14 | PASS |
| 7 | normal | 4 | 6 | 8 slots: 2 empty | — | 13 | 13 | 13 | PASS |
| 8 | normal | 4 | 7 | 8 slots: 3 partly filled, 2 small empty | — | 12 | 11 | 12 | PASS |
| 9 | normal | 3 | 5 | 8 slots: 2 empty | — | 8 | 8 | 8 | PASS |
| 10 | normal | 4 | 7 | 8 slots: 3 partly filled, 2 small empty | — | 10 | 10 | 10 | PASS |
| 11 | normal | 4 | 7 | 8 slots: 2 partly filled, 2 small empty | — | 14 | 12 | 14 | PASS |
| 12 | normal | 4 | 7 | 8 slots: 4 partly filled, 2 small empty | — | 12 | 11 | 13 | PASS |
| 13 | hard | 5 | 8 | 8 slots: 2 partly filled, 2 small empty | — | 17 | 16 | 18 | PASS |
| 14 | normal | 5 | 8 | 8 slots: 3 partly filled, 2 small empty | — | 15 | 14 | 15 | PASS |
| 15 | normal | 4 | 7 | 8 slots: 3 partly filled, 2 small empty | — | 11 | 11 | 11 | PASS |
| 16 | normal | 5 | 8 | 8 slots: 4 partly filled, 2 small empty | — | 16 | 15 | 16 | PASS |
| 17 | hard | 5 | 8 | 8 slots: 3 partly filled, 2 small empty | — | 16 | 15 | 18 | PASS |
| 18 | normal | 5 | 8 | 8 slots: 3 partly filled, 2 small empty | — | 16 | 15 | 16 | PASS |
| 19 | normal | 4 | 7 | 8 slots: 3 partly filled, 2 small empty | — | 12 | 11 | 12 | PASS |
| 20 | boss | 5 | 8 | 8 slots: 4 partly filled, 2 small empty | — | 16 | 14 | 16 | PASS |
| 21 | normal | 4 | 7 | 8 slots: 4 partly filled, 2 small empty | — | 13 | 12 | 13 | PASS |
| 22 | hard | 5 | 8 | 8 slots: 4 partly filled, 2 small empty | — | 17 | 15 | 17 | PASS |
| 23 | normal | 4 | 7 | 8 slots: 3 partly filled, 2 small empty | — | 11 | 11 | 11 | PASS |
| 24 | normal | 5 | 8 | 8 slots: 3 partly filled, 2 small empty | — | 13 | 13 | 13 | PASS |
| 25 | normal | 4 | 7 | 8 slots: 3 partly filled, 2 small empty | — | 11 | 10 | 12 | PASS |
| 26 | hard | 6 | 8 | 8 slots: 3 partly filled, 1 empty | — | 21 | 20 | 21 | PASS |
| 27 | normal | 6 | 9 | 8 slots: 3 partly filled, 2 small empty | — | 19 | 17 | 21 | PASS |
| 28 | hard | 6 | 8 | 8 slots: 3 partly filled, 1 empty | — | 20 | 19 | 20 | PASS |
| 29 | normal | 5 | 8 | 8 slots: 4 partly filled, 2 small empty | — | 15 | 14 | 16 | PASS |
| 30 | boss | 6 | 8 | 8 slots: 4 partly filled, 1 empty | — | 19 | 19 | 19 | PASS |
| 31 | normal | 5 | 8 | 8 slots: 3 partly filled, 2 small empty | — | 15 | 14 | 16 | PASS |
| 32 | normal | 6 | 8 | 8 slots: 3 partly filled, 1 empty | — | 19 | 19 | 19 | PASS |
| 33 | normal | 5 | 8 | 8 slots: 4 partly filled, 2 small empty | — | 17 | 16 | 17 | PASS |
| 34 | normal | 6 | 8 | 8 slots: 4 partly filled, 1 empty | — | 17 | 17 | 18 | PASS |
| 35 | normal | 6 | 9 | 8 slots: 3 partly filled, 2 small empty | — | 21 | 17 | 22 | PASS |
| 36 | normal | 5 | 7 | 6 slots: 3 partly filled, 1 small empty | — | 16 | 16 | 16 | PASS |
| 37 | normal | 5 | 8 | 8 slots: 4 partly filled, 2 small empty | — | 16 | 14 | 17 | PASS |
| 38 | normal | 5 | 7 | 6 slots: 4 partly filled, 1 small empty | — | 15 | 15 | 17 | PASS |
| 39 | normal | 6 | 9 | 8 slots: 3 partly filled, 2 small empty | — | 17 | 16 | 17 | PASS |
| 40 | boss | 6 | 8 | 6 slots: 4 partly filled, 1 small empty | — | 21 | 19 | 23 | PASS |
| 41 | normal | 6 | 9 | 8 slots: 4 partly filled, 2 small empty | — | 17 | 16 | 17 | PASS |
| 42 | normal | 5 | 7 | 6 slots: 3 partly filled, 1 small empty | — | 14 | 14 | 14 | PASS |
| 43 | normal | 5 | 8 | 8 slots: 3 partly filled, 2 small empty | — | 14 | 13 | 14 | PASS |
| 44 | normal | 6 | 8 | 6 slots: 3 partly filled, 1 small empty | — | 22 | 20 | 23 | PASS |
| 45 | normal | 5 | 8 | 8 slots: 3 partly filled, 2 small empty | — | 14 | 13 | 14 | PASS |
| 46 | hard | 6 | 8 | 6 slots: 4 partly filled, 1 small empty | — | 21 | 20 | 23 | PASS |
| 47 | normal | 6 | 9 | 8 slots: 4 partly filled, 2 small empty | — | 20 | 18 | 20 | PASS |
| 48 | normal | 6 | 8 | 6 slots: 2 partly filled, 1 small empty | — | 20 | 19 | 21 | PASS |
| 49 | normal | 5 | 8 | 8 slots: 4 partly filled, 2 small empty | — | 17 | 15 | 18 | PASS |
| 50 | boss | 6 | 8 | 6 slots: 2 partly filled, 1 small empty | — | 22 | 20 | 27 | PASS |
| 51 | normal | 7 | 10 | 8 slots: 3 partly filled, 2 small empty | — | 19 | 19 | 19 | PASS |
| 52 | hard | 7 | 9 | 6 slots: 3 partly filled, 1 small empty | — | 26 | 22 | 27 | PASS |
| 53 | normal | 7 | 10 | 8 slots: 3 partly filled, 2 small empty | — | 25 | — | 25 | PASS |
| 54 | normal | 6 | 8 | 6 slots: 3 partly filled, 1 small empty | — | 20 | 19 | 20 | PASS |
| 55 | normal | 7 | 10 | 8 slots: 4 partly filled, 2 small empty | — | 25 | — | 26 | PASS |
| 56 | normal | 6 | 8 | 6 slots: 3 partly filled, 1 small empty | — | 19 | 19 | 19 | PASS |
| 57 | hard | 8 | 11 | 8 slots: 3 partly filled, 2 small empty | — | 26 | — | 26 | PASS |
| 58 | normal | 6 | 8 | 6 slots: 4 partly filled, 1 small empty | — | 23 | 19 | 23 | PASS |
| 59 | normal | 7 | 10 | 8 slots: 3 partly filled, 2 small empty | — | 24 | 23 | 24 | PASS |
| 60 | boss | 8 | 10 | 6 slots: 3 partly filled, 1 small empty | — | 28 | 25 | 28 | PASS |
| 61 | normal | 7 | 10 | 8 slots: 2 partly filled, 2 small empty | — | 24 | 23 | 24 | PASS |
| 62 | normal | 6 | 8 | 6 slots: 4 partly filled, 1 small empty | — | 18 | 18 | 18 | PASS |
| 63 | normal | 7 | 10 | 8 slots: 3 partly filled, 2 small empty | — | 20 | 20 | 22 | PASS |
| 64 | hard | 7 | 9 | 6 slots: 3 partly filled, 1 small empty | — | 25 | 23 | 30 | PASS |
| 65 | normal | 7 | 10 | 8 slots: 4 partly filled, 2 small empty | — | 25 | — | 25 | PASS |
| 66 | hard | 7 | 9 | 6 slots: 4 partly filled, 1 small empty | — | 26 | 23 | 27 | PASS |
| 67 | normal | 8 | 11 | 8 slots: 3 partly filled, 2 small empty | — | 29 | — | 29 | PASS |
| 68 | normal | 6 | 8 | 6 slots: 4 partly filled, 1 small empty | — | 18 | 17 | 18 | PASS |
| 69 | normal | 7 | 10 | 8 slots: 3 partly filled, 2 small empty | — | 19 | 19 | 22 | PASS |
| 70 | boss | 8 | 10 | 6 slots: 4 partly filled, 1 small empty | — | 32 | 27 | 32 | PASS |
| 71 | normal | 6 | 9 | 8 slots: 3 partly filled, 2 small empty | mystery | 18 | 18 | 21 | PASS |
| 72 | normal | 7 | 9 | 8 slots: 2 empty | mystery | 24 | 23 | 24 | PASS |
| 73 | normal | 6 | 9 | 8 slots: 4 partly filled, 2 small empty | mystery | 20 | 18 | 20 | PASS |
| 74 | normal | 8 | 10 | 8 slots: 2 empty | mystery | 28 | 26 | 28 | PASS |
| 75 | normal | 6 | 9 | 8 slots: 4 partly filled, 2 small empty | mystery | 20 | 18 | 22 | PASS |
| 76 | normal | 7 | 9 | 8 slots: 2 empty | mystery | 25 | 23 | 26 | PASS |
| 77 | normal | 6 | 9 | 8 slots: 4 partly filled, 2 small empty | mystery | 22 | 18 | 22 | PASS |
| 78 | normal | 8 | 10 | 8 slots: 2 empty | mystery | 28 | 26 | 30 | PASS |
| 79 | normal | 6 | 9 | 8 slots: 3 partly filled, 2 small empty | mystery | 19 | 19 | 19 | PASS |
| 80 | boss | 8 | 10 | 8 slots: 2 empty | mystery | 27 | 26 | 29 | PASS |
| 81 | normal | 6 | 9 | 8 slots: 3 partly filled, 2 small empty | mystery | 20 | 18 | 20 | PASS |
| 82 | normal | 6 | 8 | 8 slots: 2 empty | mystery | 21 | 20 | 21 | PASS |
| 83 | normal | 6 | 9 | 8 slots: 3 partly filled, 2 small empty | mystery | 20 | 18 | 21 | PASS |
| 84 | normal | 7 | 9 | 8 slots: 2 empty | mystery | 24 | 23 | 25 | PASS |
| 85 | normal | 7 | 10 | 8 slots: 4 partly filled, 2 small empty | mystery | 26 | — | 27 | PASS |
| 86 | normal | 6 | 8 | 8 slots: 3 partly filled, 1 empty | mystery, sizes 3/4/5 | 17 | 17 | 17 | PASS |
| 87 | normal | 8 | 10 | 9 slots: 2 empty | sizes 3/4/5 | 28 | 26 | 28 | PASS |
| 88 | normal | 6 | 8 | 8 slots: 3 partly filled, 1 empty | mystery, sizes 3/4/5 | 21 | 19 | 21 | PASS |
| 89 | normal | 7 | 9 | 9 slots: 2 empty | sizes 4/5 | 27 | 26 | 29 | PASS |
| 90 | boss | 8 | 10 | 10 slots: 4 partly filled, 1 empty | mystery, sizes 3/4/5 | 32 | — | 32 | PASS |
| 91 | normal | 7 | 9 | 9 slots: 2 empty | sizes 3/4/5 | 21 | 20 | 22 | PASS |
| 92 | normal | 7 | 9 | 9 slots: 4 partly filled, 1 empty | mystery, sizes 3/4/5 | 25 | 20 | 25 | PASS |
| 93 | normal | 7 | 9 | 9 slots: 2 empty | sizes 3/4/5 | 21 | 21 | 21 | PASS |
| 94 | normal | 6 | 8 | 8 slots: 3 partly filled, 1 empty | mystery, sizes 3/5 | 24 | 22 | 25 | PASS |
| 95 | normal | 6 | 8 | 9 slots: 2 empty | sizes 3/4/5 | 27 | 23 | 27 | PASS |
| 96 | normal | 6 | 8 | 7 slots: 3 partly filled, 1 empty | mystery, sizes 3/4 | 17 | 17 | 17 | PASS |
| 97 | normal | 8 | 10 | 9 slots: 2 empty | sizes 3/4/5 | 27 | 26 | 27 | PASS |
| 98 | normal | 6 | 8 | 8 slots: 3 partly filled, 1 empty | mystery, sizes 3/4/5 | 19 | 17 | 19 | PASS |
| 99 | normal | 7 | 9 | 9 slots: 2 empty | sizes 3/4/5 | 20 | 18 | 21 | PASS |
| 100 | boss | 8 | 10 | 10 slots: 4 partly filled, 1 empty | mystery, sizes 3/4/5 | 33 | — | 33 | PASS |
