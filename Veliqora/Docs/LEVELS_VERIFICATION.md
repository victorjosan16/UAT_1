# Level verification report

Generated from `Docs/level_validation_report.csv`, written by `dotnet run -c Release --project Tools/LevelGen -- validate`.

**Levels 1–300: 300 / 300 PASS.** Whole catalog: 300 / 300 PASS.

For every level the validator executed:
1. color distribution check;
2. the start is not already solved, no bottle starts monochrome or complete, every color is spread over at least two bottles;
3. the color count is inside the progression range (1–10: 3 · 11–20: 4–5 · 21–50: 5–6 · 51–75: 6–7 · 76–100: 6–8 · 101+: 7–8);
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
| 2 | normal | 3 | 5 | 8 slots: 2 empty | — | 10 | 10 | 11 | PASS |
| 3 | normal | 3 | 5 | 8 slots: 2 empty | — | 9 | 9 | 9 | PASS |
| 4 | normal | 3 | 5 | 8 slots: 2 empty | — | 8 | 8 | 8 | PASS |
| 5 | normal | 3 | 5 | 8 slots: 2 empty | — | 9 | 9 | 9 | PASS |
| 6 | normal | 3 | 6 | 8 slots: 3 partly filled, 2 small empty | — | 8 | 8 | 8 | PASS |
| 7 | normal | 3 | 6 | 8 slots: 2 partly filled, 2 small empty | — | 8 | 8 | 9 | PASS |
| 8 | normal | 3 | 6 | 8 slots: 3 partly filled, 2 small empty | — | 9 | 8 | 9 | PASS |
| 9 | normal | 3 | 6 | 8 slots: 3 partly filled, 2 small empty | — | 9 | 8 | 9 | PASS |
| 10 | normal | 3 | 6 | 8 slots: 3 partly filled, 2 small empty | — | 8 | 8 | 11 | PASS |
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
| 21 | normal | 5 | 8 | 8 slots: 4 partly filled, 2 small empty | mystery | 18 | 16 | 18 | PASS |
| 22 | hard | 6 | 8 | 8 slots: 2 empty | mystery | 22 | 21 | 22 | PASS |
| 23 | normal | 5 | 8 | 8 slots: 2 partly filled, 2 small empty | mystery | 15 | 15 | 16 | PASS |
| 24 | normal | 6 | 8 | 8 slots: 2 empty | mystery | 21 | 21 | 21 | PASS |
| 25 | normal | 5 | 8 | 8 slots: 4 partly filled, 2 small empty | mystery | 15 | 15 | 15 | PASS |
| 26 | hard | 6 | 8 | 8 slots: 2 empty | mystery | 23 | 20 | 23 | PASS |
| 27 | normal | 6 | 9 | 8 slots: 3 partly filled, 2 small empty | mystery | 19 | 17 | 21 | PASS |
| 28 | hard | 6 | 8 | 8 slots: 2 empty | mystery | 22 | 20 | 22 | PASS |
| 29 | normal | 5 | 8 | 8 slots: 3 partly filled, 2 small empty | mystery | 17 | 16 | 17 | PASS |
| 30 | boss | 6 | 8 | 8 slots: 2 empty | mystery | 21 | 19 | 21 | PASS |
| 31 | normal | 5 | 8 | 8 slots: 3 partly filled, 2 small empty | mystery | 15 | 14 | 15 | PASS |
| 32 | normal | 6 | 8 | 8 slots: 2 empty | mystery | 20 | 18 | 20 | PASS |
| 33 | normal | 5 | 8 | 8 slots: 4 partly filled, 2 small empty | mystery | 16 | 15 | 16 | PASS |
| 34 | normal | 6 | 8 | 8 slots: 2 empty | mystery | 20 | 19 | 20 | PASS |
| 35 | normal | 6 | 9 | 8 slots: 3 partly filled, 2 small empty | mystery | 21 | 19 | 22 | PASS |
| 36 | normal | 6 | 8 | 8 slots: 2 empty | frozen×2 | 22 | 21 | 22 | PASS |
| 37 | normal | 5 | 8 | 8 slots: 4 partly filled, 2 small empty | frozen×1 | 16 | 14 | 16 | PASS |
| 38 | normal | 6 | 8 | 8 slots: 2 empty | frozen×2 | 19 | 19 | 19 | PASS |
| 39 | normal | 6 | 9 | 8 slots: 3 partly filled, 2 small empty | frozen×1 | 20 | 18 | 20 | PASS |
| 40 | boss | 6 | 8 | 8 slots: 2 empty | frozen×2 | 20 | 20 | 20 | PASS |
| 41 | normal | 6 | 9 | 8 slots: 4 partly filled, 2 small empty | frozen×1 | 19 | 16 | 19 | PASS |
| 42 | normal | 5 | 7 | 8 slots: 2 empty | frozen×2 | 15 | 15 | 16 | PASS |
| 43 | normal | 5 | 8 | 8 slots: 4 partly filled, 2 small empty | frozen×1 | 17 | 15 | 17 | PASS |
| 44 | normal | 6 | 8 | 8 slots: 2 empty | frozen×2 | 23 | 20 | 23 | PASS |
| 45 | normal | 5 | 8 | 8 slots: 3 partly filled, 2 small empty | frozen×1 | 16 | 14 | 17 | PASS |
| 46 | hard | 6 | 8 | 8 slots: 2 empty | frozen×2 | 20 | 20 | 21 | PASS |
| 47 | normal | 6 | 9 | 8 slots: 4 partly filled, 2 small empty | frozen×1 | 19 | 17 | 20 | PASS |
| 48 | normal | 6 | 8 | 8 slots: 2 empty | frozen×2 | 21 | 21 | 21 | PASS |
| 49 | normal | 5 | 8 | 8 slots: 4 partly filled, 2 small empty | frozen×1 | 17 | 15 | 17 | PASS |
| 50 | boss | 6 | 8 | 8 slots: 2 empty | frozen×2 | 20 | 20 | 22 | PASS |
| 51 | normal | 6 | 9 | 8 slots: 4 partly filled, 2 small empty | locked×1 | 19 | 17 | 19 | PASS |
| 52 | hard | 7 | 9 | 8 slots: 2 empty | locked×2 | 25 | 23 | 26 | PASS |
| 53 | normal | 6 | 9 | 8 slots: 4 partly filled, 2 small empty | locked×1 | 19 | 17 | 19 | PASS |
| 54 | normal | 7 | 9 | 8 slots: 2 empty | locked×2 | 23 | 23 | 23 | PASS |
| 55 | normal | 6 | 9 | 8 slots: 4 partly filled, 2 small empty | locked×1 | 19 | 17 | 20 | PASS |
| 56 | normal | 7 | 9 | 8 slots: 2 empty | locked×2 | 22 | 22 | 22 | PASS |
| 57 | hard | 7 | 10 | 8 slots: 3 partly filled, 2 small empty | locked×1 | 23 | 22 | 23 | PASS |
| 58 | normal | 7 | 9 | 8 slots: 2 empty | locked×2 | 23 | 23 | 24 | PASS |
| 59 | normal | 6 | 9 | 8 slots: 4 partly filled, 2 small empty | locked×1 | 21 | 20 | 21 | PASS |
| 60 | boss | 7 | 9 | 8 slots: 2 empty | locked×2 | 24 | 23 | 24 | PASS |
| 61 | normal | 6 | 9 | 8 slots: 3 partly filled, 2 small empty | locked×1 | 20 | 18 | 20 | PASS |
| 62 | normal | 7 | 9 | 8 slots: 2 empty | locked×2 | 23 | 23 | 23 | PASS |
| 63 | normal | 6 | 9 | 8 slots: 3 partly filled, 2 small empty | locked×1 | 18 | 16 | 19 | PASS |
| 64 | hard | 7 | 9 | 8 slots: 2 empty | locked×2 | 24 | 20 | 24 | PASS |
| 65 | normal | 6 | 9 | 8 slots: 3 partly filled, 2 small empty | locked×1 | 18 | 18 | 18 | PASS |
| 66 | hard | 7 | 9 | 8 slots: 2 empty | locked×2 | 25 | 24 | 25 | PASS |
| 67 | normal | 7 | 10 | 8 slots: 4 partly filled, 2 small empty | locked×1 | 22 | 21 | 23 | PASS |
| 68 | normal | 7 | 9 | 8 slots: 2 empty | locked×2 | 25 | 23 | 25 | PASS |
| 69 | normal | 6 | 9 | 8 slots: 3 partly filled, 2 small empty | locked×1 | 21 | 19 | 23 | PASS |
| 70 | boss | 7 | 9 | 8 slots: 2 empty | locked×2 | 24 | 24 | 24 | PASS |
| 71 | normal | 6 | 9 | 8 slots: 3 partly filled, 2 small empty | locked×1 | 18 | 18 | 21 | PASS |
| 72 | normal | 7 | 9 | 8 slots: 2 empty | locked×2 | 23 | 21 | 23 | PASS |
| 73 | normal | 6 | 9 | 8 slots: 4 partly filled, 2 small empty | locked×1 | 20 | 18 | 20 | PASS |
| 74 | normal | 7 | 9 | 8 slots: 2 empty | locked×2 | 21 | 21 | 23 | PASS |
| 75 | normal | 6 | 9 | 8 slots: 3 partly filled, 2 small empty | locked×1 | 20 | 18 | 21 | PASS |
| 76 | normal | 7 | 10 | 8 slots: 2 empty | mystery, frozen×1, locked×1, ad bottle | 23 | 21 | 23 | PASS |
| 77 | normal | 7 | 10 | 8 slots: 3 partly filled, 2 small empty | mystery, frozen×1, locked×1 | 23 | — | 24 | PASS |
| 78 | normal | 8 | 11 | 8 slots: 2 empty | mystery, frozen×1, locked×1, ad bottle | 29 | 27 | 29 | PASS |
| 79 | normal | 7 | 10 | 8 slots: 3 partly filled, 2 small empty | mystery, frozen×1, locked×1 | 25 | — | 25 | PASS |
| 80 | boss | 8 | 11 | 8 slots: 2 empty | mystery, locked×2, ad bottle | 28 | 27 | 29 | PASS |
| 81 | normal | 7 | 10 | 8 slots: 3 partly filled, 2 small empty | mystery, frozen×1, locked×1 | 20 | — | 22 | PASS |
| 82 | normal | 6 | 9 | 8 slots: 2 empty | mystery, frozen×1, locked×1, ad bottle | 20 | 19 | 20 | PASS |
| 83 | normal | 7 | 10 | 8 slots: 2 partly filled, 2 small empty | mystery, locked×2 | 27 | 24 | 27 | PASS |
| 84 | normal | 7 | 10 | 8 slots: 2 empty | mystery, frozen×1, locked×1, ad bottle | 22 | 21 | 22 | PASS |
| 85 | normal | 8 | 11 | 8 slots: 4 partly filled, 2 small empty | mystery, frozen×1, locked×1 | 26 | — | 28 | PASS |
| 86 | normal | 6 | 9 | 8 slots: 2 empty | mystery, frozen×1, locked×1, ad bottle | 23 | 20 | 24 | PASS |
| 87 | normal | 8 | 11 | 8 slots: 4 partly filled, 2 small empty | mystery, locked×2 | 28 | — | 31 | PASS |
| 88 | normal | 7 | 10 | 8 slots: 2 empty | mystery, frozen×2, ad bottle | 25 | 24 | 25 | PASS |
| 89 | normal | 7 | 10 | 8 slots: 4 partly filled, 2 small empty | mystery, frozen×1, locked×1 | 25 | — | 26 | PASS |
| 90 | boss | 8 | 11 | 8 slots: 2 empty | mystery, frozen×1, locked×1, ad bottle | 29 | 27 | 29 | PASS |
| 91 | normal | 7 | 10 | 8 slots: 4 partly filled, 2 small empty | mystery, locked×2 | 24 | — | 24 | PASS |
| 92 | normal | 8 | 11 | 8 slots: 2 empty | mystery, frozen×1, locked×1, ad bottle | 24 | 24 | 25 | PASS |
| 93 | normal | 7 | 10 | 8 slots: 3 partly filled, 2 small empty | mystery, frozen×1, locked×1 | 24 | — | 27 | PASS |
| 94 | normal | 7 | 10 | 8 slots: 2 empty | mystery, frozen×1, locked×1, ad bottle | 25 | 24 | 25 | PASS |
| 95 | normal | 6 | 9 | 8 slots: 3 partly filled, 2 small empty | mystery, locked×2 | 23 | 21 | 23 | PASS |
| 96 | normal | 7 | 10 | 8 slots: 2 empty | mystery, frozen×1, locked×1, ad bottle | 24 | 22 | 24 | PASS |
| 97 | normal | 8 | 11 | 8 slots: 3 partly filled, 2 small empty | mystery, frozen×1, locked×1 | 26 | — | 26 | PASS |
| 98 | normal | 7 | 10 | 8 slots: 2 empty | mystery, frozen×2, ad bottle | 24 | 23 | 24 | PASS |
| 99 | normal | 7 | 10 | 8 slots: 3 partly filled, 2 small empty | mystery, frozen×1, locked×1 | 27 | 21 | 27 | PASS |
| 100 | boss | 8 | 11 | 8 slots: 2 empty | mystery, frozen×1, locked×1, ad bottle | 28 | 26 | 29 | PASS |
| 101 | normal | 7 | 13 | 8 slots: 4 partly filled, 2 small empty | — | 33 | — | 33 | PASS |
| 102 | normal | 8 | 15 | 8 slots: 2 empty | mystery, locked×1, ad bottle | 41 | — | 43 | PASS |
| 103 | normal | 8 | 14 | 8 slots: 4 partly filled, 2 small empty | — | 34 | — | 34 | PASS |
| 104 | normal | 7 | 14 | 8 slots: 2 empty | mystery, frozen×1, ad bottle | 34 | — | 34 | PASS |
| 105 | normal | 7 | 13 | 8 slots: 4 partly filled, 2 small empty | — | 28 | — | 28 | PASS |
| 106 | normal | 7 | 14 | 8 slots: 2 empty | mystery, frozen×1, ad bottle | 38 | — | 39 | PASS |
| 107 | normal | 7 | 13 | 8 slots: 4 partly filled, 2 small empty | — | 30 | — | 30 | PASS |
| 108 | normal | 7 | 14 | 8 slots: 2 empty | mystery, frozen×1, ad bottle | 35 | — | 35 | PASS |
| 109 | hard | 8 | 14 | 8 slots: 4 partly filled, 2 small empty | — | 37 | — | 39 | PASS |
| 110 | boss | 8 | 15 | 8 slots: 2 empty | mystery, locked×1, ad bottle | 41 | — | 41 | PASS |
| 111 | normal | 7 | 13 | 8 slots: 4 partly filled, 2 small empty | — | 33 | — | 36 | PASS |
| 112 | normal | 7 | 14 | 8 slots: 2 empty | mystery, frozen×1, ad bottle | 35 | — | 35 | PASS |
| 113 | normal | 7 | 13 | 8 slots: 4 partly filled, 2 small empty | — | 32 | — | 32 | PASS |
| 114 | normal | 7 | 14 | 8 slots: 2 empty | mystery, frozen×1, ad bottle | 35 | — | 36 | PASS |
| 115 | normal | 7 | 13 | 8 slots: 4 partly filled, 2 small empty | — | 32 | — | 33 | PASS |
| 116 | normal | 7 | 14 | 8 slots: 2 empty | mystery, frozen×1, ad bottle | 36 | — | 36 | PASS |
| 117 | normal | 7 | 13 | 8 slots: 3 partly filled, 2 small empty | — | 33 | — | 34 | PASS |
| 118 | hard | 8 | 15 | 8 slots: 2 empty | mystery, frozen×1, ad bottle | 40 | — | 41 | PASS |
| 119 | normal | 7 | 13 | 8 slots: 4 partly filled, 2 small empty | — | 32 | — | 34 | PASS |
| 120 | boss | 8 | 15 | 8 slots: 2 empty | mystery, frozen×1, ad bottle | 41 | — | 41 | PASS |
| 121 | normal | 7 | 13 | 8 slots: 4 partly filled, 2 small empty | — | 31 | — | 31 | PASS |
| 122 | normal | 7 | 14 | 8 slots: 2 empty | mystery, locked×1, ad bottle | 34 | — | 34 | PASS |
| 123 | hard | 8 | 14 | 8 slots: 4 partly filled, 2 small empty | — | 38 | — | 43 | PASS |
| 124 | normal | 7 | 14 | 8 slots: 2 empty | mystery, frozen×1, ad bottle | 36 | — | 36 | PASS |
| 125 | normal | 7 | 13 | 8 slots: 3 partly filled, 2 small empty | — | 31 | — | 31 | PASS |
| 126 | normal | 8 | 15 | 8 slots: 2 empty | mystery, locked×1, ad bottle | 39 | — | 39 | PASS |
| 127 | normal | 7 | 13 | 8 slots: 4 partly filled, 2 small empty | — | 33 | — | 34 | PASS |
| 128 | hard | 8 | 15 | 8 slots: 2 empty | mystery, frozen×1, ad bottle | 41 | — | 42 | PASS |
| 129 | normal | 7 | 13 | 8 slots: 4 partly filled, 2 small empty | — | 32 | — | 32 | PASS |
| 130 | boss | 8 | 15 | 8 slots: 2 empty | mystery, frozen×1, ad bottle | 43 | — | 43 | PASS |
| 131 | normal | 7 | 13 | 8 slots: 4 partly filled, 2 small empty | — | 31 | — | 32 | PASS |
| 132 | normal | 7 | 14 | 8 slots: 2 empty | mystery, frozen×1, ad bottle | 37 | — | 38 | PASS |
| 133 | normal | 7 | 13 | 8 slots: 4 partly filled, 2 small empty | — | 31 | — | 31 | PASS |
| 134 | normal | 7 | 14 | 8 slots: 2 empty | mystery, locked×1, ad bottle | 36 | — | 36 | PASS |
| 135 | normal | 7 | 13 | 8 slots: 4 partly filled, 2 small empty | — | 31 | — | 33 | PASS |
| 136 | normal | 7 | 14 | 8 slots: 2 empty | mystery, frozen×1, ad bottle | 36 | — | 36 | PASS |
| 137 | normal | 7 | 13 | 8 slots: 3 partly filled, 2 small empty | — | 32 | — | 32 | PASS |
| 138 | normal | 7 | 14 | 8 slots: 2 empty | mystery, frozen×1, ad bottle | 36 | — | 37 | PASS |
| 139 | normal | 7 | 13 | 8 slots: 3 partly filled, 2 small empty | — | 31 | — | 31 | PASS |
| 140 | boss | 8 | 15 | 8 slots: 2 empty | mystery, frozen×1, ad bottle | 39 | — | 40 | PASS |
| 141 | normal | 7 | 13 | 8 slots: 4 partly filled, 2 small empty | — | 32 | — | 33 | PASS |
| 142 | normal | 7 | 14 | 8 slots: 2 empty | mystery, locked×1, ad bottle | 35 | — | 35 | PASS |
| 143 | hard | 8 | 14 | 8 slots: 3 partly filled, 2 small empty | — | 36 | — | 38 | PASS |
| 144 | normal | 7 | 14 | 8 slots: 2 empty | mystery, locked×1, ad bottle | 37 | — | 37 | PASS |
| 145 | normal | 7 | 13 | 8 slots: 4 partly filled, 2 small empty | — | 32 | — | 34 | PASS |
| 146 | normal | 7 | 14 | 8 slots: 2 empty | mystery, locked×1, ad bottle | 37 | — | 38 | PASS |
| 147 | normal | 7 | 13 | 8 slots: 4 partly filled, 2 small empty | — | 34 | — | 35 | PASS |
| 148 | normal | 7 | 14 | 8 slots: 2 empty | mystery, frozen×1, ad bottle | 37 | — | 38 | PASS |
| 149 | normal | 7 | 13 | 8 slots: 4 partly filled, 2 small empty | — | 35 | — | 35 | PASS |
| 150 | boss | 8 | 15 | 8 slots: 2 empty | mystery, frozen×1, ad bottle | 43 | — | 45 | PASS |
| 151 | normal | 7 | 9 | 8 slots: 2 empty | mystery, frozen×1, sizes 3/4 | 22 | — | 22 | PASS |
| 152 | normal | 7 | 9 | 9 slots: 3 partly filled, 1 empty | locked×2, sizes 3/4/5 | 26 | — | 27 | PASS |
| 153 | normal | 7 | 9 | 9 slots: 2 empty | mystery, frozen×1, sizes 3/4/5 | 24 | — | 24 | PASS |
| 154 | normal | 8 | 10 | 9 slots: 3 partly filled, 1 empty | locked×2, sizes 3/4/5 | 28 | — | 28 | PASS |
| 155 | normal | 7 | 9 | 9 slots: 2 empty | mystery, frozen×1, sizes 3/4/5 | 30 | — | 30 | PASS |
| 156 | normal | 7 | 9 | 9 slots: 4 partly filled, 1 empty | locked×2, sizes 3/4/5 | 25 | — | 25 | PASS |
| 157 | normal | 7 | 9 | 9 slots: 2 empty | mystery, frozen×1, sizes 4/5 | 30 | — | 30 | PASS |
| 158 | normal | 7 | 9 | 8 slots: 3 partly filled, 1 empty | locked×2, sizes 3/4/5 | 23 | — | 24 | PASS |
| 159 | normal | 7 | 9 | 9 slots: 2 empty | mystery, frozen×1, sizes 3/4/5 | 25 | — | 25 | PASS |
| 160 | boss | 8 | 10 | 9 slots: 4 partly filled, 1 empty | locked×2, sizes 3/4/5 | 29 | — | 31 | PASS |
| 161 | normal | 7 | 9 | 9 slots: 2 empty | mystery, frozen×1, sizes 3/4/5 | 24 | — | 26 | PASS |
| 162 | normal | 7 | 9 | 8 slots: 2 partly filled, 1 empty | locked×2, sizes 3/4/5 | 25 | — | 25 | PASS |
| 163 | normal | 7 | 9 | 9 slots: 2 empty | mystery, frozen×1, sizes 3/4/5 | 27 | — | 27 | PASS |
| 164 | normal | 7 | 9 | 8 slots: 3 partly filled, 1 empty | locked×2, sizes 3/4/5 | 22 | — | 22 | PASS |
| 165 | normal | 7 | 9 | 9 slots: 2 empty | mystery, frozen×1, sizes 3/4/5 | 22 | — | 23 | PASS |
| 166 | normal | 7 | 9 | 9 slots: 4 partly filled, 1 empty | locked×2, sizes 3/4/5 | 19 | — | 19 | PASS |
| 167 | normal | 7 | 9 | 9 slots: 2 empty | mystery, frozen×1, sizes 3/4/5 | 29 | — | 30 | PASS |
| 168 | normal | 7 | 9 | 10 slots: 4 partly filled, 1 empty | locked×2, sizes 3/5 | 26 | — | 27 | PASS |
| 169 | normal | 7 | 9 | 9 slots: 2 empty | mystery, frozen×1, sizes 3/4/5 | 30 | — | 30 | PASS |
| 170 | boss | 8 | 10 | 10 slots: 4 partly filled, 1 empty | locked×2, sizes 3/4/5 | 31 | — | 32 | PASS |
| 171 | normal | 7 | 9 | 9 slots: 2 empty | mystery, frozen×1, sizes 3/4/5 | 29 | — | 29 | PASS |
| 172 | normal | 7 | 9 | 10 slots: 3 partly filled, 1 empty | locked×2 | 27 | — | 27 | PASS |
| 173 | normal | 7 | 9 | 9 slots: 2 empty | mystery, frozen×1, sizes 3/4/5 | 24 | — | 24 | PASS |
| 174 | normal | 7 | 9 | 9 slots: 4 partly filled, 1 empty | locked×2, sizes 4/5 | 30 | — | 30 | PASS |
| 175 | normal | 7 | 9 | 9 slots: 2 empty | mystery, frozen×1, sizes 3/4/5 | 22 | — | 24 | PASS |
| 176 | hard | 8 | 10 | 8 slots: 3 partly filled, 1 empty | locked×2, sizes 3/4/5 | 32 | — | 35 | PASS |
| 177 | normal | 7 | 9 | 9 slots: 2 empty | mystery, frozen×1, sizes 3/4/5 | 26 | — | 29 | PASS |
| 178 | normal | 7 | 9 | 9 slots: 4 partly filled, 1 empty | locked×2, sizes 3/4/5 | 26 | — | 26 | PASS |
| 179 | normal | 7 | 9 | 9 slots: 2 empty | mystery, frozen×1, sizes 3/4/5 | 30 | — | 30 | PASS |
| 180 | boss | 8 | 10 | 10 slots: 5 partly filled, 1 empty | locked×2, sizes 3/4/5 | 29 | — | 30 | PASS |
| 181 | normal | 7 | 9 | 8 slots: 2 empty | mystery, frozen×1, sizes 3/4 | 23 | — | 24 | PASS |
| 182 | normal | 7 | 9 | 10 slots: 4 partly filled, 1 empty | locked×2, sizes 3/5 | 26 | — | 26 | PASS |
| 183 | hard | 8 | 10 | 9 slots: 2 empty | mystery, frozen×1, sizes 3/4/5 | 34 | — | 36 | PASS |
| 184 | normal | 7 | 9 | 8 slots: 3 partly filled, 1 empty | locked×2, sizes 3/4/5 | 21 | — | 22 | PASS |
| 185 | normal | 7 | 9 | 9 slots: 2 empty | mystery, frozen×1, sizes 3/4/5 | 21 | — | 24 | PASS |
| 186 | hard | 8 | 10 | 8 slots: 3 partly filled, 1 empty | locked×2, sizes 3/4/5 | 34 | — | 34 | PASS |
| 187 | normal | 8 | 10 | 9 slots: 2 empty | mystery, frozen×1, sizes 3/4/5 | 32 | — | 32 | PASS |
| 188 | normal | 7 | 9 | 9 slots: 4 partly filled, 1 empty | locked×2, sizes 3/4/5 | 24 | — | 24 | PASS |
| 189 | normal | 7 | 9 | 9 slots: 2 empty | mystery, frozen×1, sizes 3/4/5 | 24 | — | 25 | PASS |
| 190 | boss | 8 | 10 | 9 slots: 3 partly filled, 1 empty | locked×2, sizes 3/4/5 | 33 | — | 35 | PASS |
| 191 | normal | 7 | 9 | 9 slots: 2 empty | mystery, frozen×1, sizes 3/4/5 | 25 | — | 25 | PASS |
| 192 | normal | 7 | 9 | 10 slots: 4 partly filled, 1 empty | locked×2, sizes 3/5 | 19 | — | 19 | PASS |
| 193 | normal | 7 | 9 | 9 slots: 2 empty | mystery, frozen×1, sizes 3/4/5 | 26 | — | 26 | PASS |
| 194 | normal | 7 | 9 | 10 slots: 4 partly filled, 1 empty | locked×2, sizes 3/4/5 | 22 | — | 22 | PASS |
| 195 | normal | 7 | 9 | 9 slots: 2 empty | mystery, frozen×1, sizes 3/4/5 | 24 | — | 24 | PASS |
| 196 | normal | 7 | 9 | 10 slots: 3 partly filled, 1 empty | locked×2, sizes 3/4/5 | 25 | — | 26 | PASS |
| 197 | normal | 7 | 9 | 9 slots: 2 empty | mystery, frozen×1, sizes 3/4/5 | 24 | — | 24 | PASS |
| 198 | normal | 7 | 9 | 10 slots: 4 partly filled, 1 empty | locked×2, sizes 3/4/5 | 19 | — | 19 | PASS |
| 199 | normal | 7 | 9 | 9 slots: 2 empty | mystery, frozen×1, sizes 3/5 | 21 | — | 22 | PASS |
| 200 | boss | 8 | 10 | 9 slots: 3 partly filled, 1 empty | locked×2, sizes 3/4/5 | 38 | — | 40 | PASS |
| 201 | normal | 8 | 11 | 10 slots: 2 empty | mystery, frozen×2, ad bottle, sizes 2/3/5/6 | 24 | — | 27 | PASS |
| 202 | normal | 8 | 10 | 9 slots: 2 empty | mystery, frozen×1, locked×1, sizes 3/4/5 | 29 | — | 29 | PASS |
| 203 | hard | 8 | 11 | 10 slots: 2 empty | mystery, frozen×2, ad bottle, sizes 2/3/5/6 | 38 | — | 38 | PASS |
| 204 | normal | 7 | 9 | 9 slots: 2 empty | mystery, frozen×2, sizes 3/4/5 | 28 | — | 28 | PASS |
| 205 | normal | 8 | 11 | 9 slots: 2 empty | mystery, frozen×2, ad bottle, sizes 2/3/4/5 | 26 | — | 27 | PASS |
| 206 | hard | 8 | 10 | 9 slots: 2 empty | mystery, frozen×2, sizes 3/4/5 | 38 | — | 38 | PASS |
| 207 | normal | 8 | 11 | 10 slots: 2 empty | mystery, frozen×1, locked×1, ad bottle, sizes 2/3/4/6 | 27 | — | 27 | PASS |
| 208 | normal | 7 | 9 | 9 slots: 2 empty | mystery, frozen×1, locked×1, sizes 3/4/5 | 27 | — | 28 | PASS |
| 209 | normal | 7 | 10 | 10 slots: 2 empty | mystery, frozen×2, ad bottle, sizes 2/4/6 | 25 | — | 27 | PASS |
| 210 | boss | 8 | 10 | 9 slots: 2 empty | mystery, frozen×2, sizes 3/4/5 | 34 | — | 35 | PASS |
| 211 | normal | 8 | 11 | 10 slots: 2 empty | mystery, frozen×1, locked×1, ad bottle, sizes 3/4/5/6 | 31 | — | 31 | PASS |
| 212 | normal | 8 | 10 | 9 slots: 2 empty | mystery, frozen×1, locked×1, sizes 3/4/5 | 28 | — | 29 | PASS |
| 213 | normal | 8 | 11 | 10 slots: 2 empty | mystery, frozen×1, locked×1, ad bottle, sizes 2/3/4/5/6 | 27 | — | 27 | PASS |
| 214 | normal | 8 | 10 | 9 slots: 2 empty | mystery, frozen×2, sizes 3/4/5 | 30 | — | 30 | PASS |
| 215 | normal | 8 | 11 | 10 slots: 2 empty | mystery, locked×2, ad bottle, sizes 2/4/6 | 27 | — | 27 | PASS |
| 216 | normal | 8 | 10 | 9 slots: 2 empty | mystery, frozen×1, locked×1, sizes 3/4/5 | 30 | — | 30 | PASS |
| 217 | hard | 8 | 11 | 10 slots: 2 empty | mystery, frozen×2, ad bottle, sizes 2/4/5/6 | 30 | — | 30 | PASS |
| 218 | normal | 7 | 9 | 9 slots: 2 empty | mystery, frozen×1, locked×1, sizes 3/5 | 29 | — | 29 | PASS |
| 219 | hard | 8 | 11 | 10 slots: 2 empty | mystery, locked×2, ad bottle, sizes 2/3/4/6 | 33 | — | 36 | PASS |
| 220 | boss | 8 | 10 | 9 slots: 2 empty | mystery, frozen×1, locked×1, sizes 3/4/5 | 30 | — | 33 | PASS |
| 221 | normal | 8 | 11 | 9 slots: 2 empty | mystery, locked×2, ad bottle, sizes 3/4/5 | 29 | — | 32 | PASS |
| 222 | hard | 8 | 10 | 9 slots: 2 empty | mystery, frozen×1, locked×1, sizes 3/4/5 | 39 | — | 41 | PASS |
| 223 | normal | 8 | 11 | 10 slots: 2 empty | mystery, frozen×1, locked×1, ad bottle, sizes 2/3/5/6 | 30 | — | 33 | PASS |
| 224 | normal | 7 | 9 | 9 slots: 2 empty | mystery, frozen×1, locked×1, sizes 3/4/5 | 30 | — | 30 | PASS |
| 225 | normal | 8 | 11 | 10 slots: 2 empty | mystery, locked×2, ad bottle, sizes 2/3/4/5/6 | 26 | — | 27 | PASS |
| 226 | normal | 7 | 9 | 9 slots: 2 empty | mystery, frozen×1, locked×1, sizes 3/4/5 | 26 | — | 26 | PASS |
| 227 | normal | 8 | 11 | 10 slots: 2 empty | mystery, frozen×1, locked×1, ad bottle, sizes 2/3/6 | 30 | — | 32 | PASS |
| 228 | hard | 8 | 10 | 9 slots: 2 empty | mystery, frozen×2, sizes 3/4/5 | 39 | — | 38 | PASS |
| 229 | normal | 8 | 11 | 10 slots: 2 empty | mystery, frozen×1, locked×1, ad bottle, sizes 2/3/4/5/6 | 28 | — | 30 | PASS |
| 230 | boss | 8 | 10 | 9 slots: 2 empty | mystery, frozen×1, locked×1, sizes 3/4/5 | 35 | — | 35 | PASS |
| 231 | normal | 8 | 11 | 10 slots: 2 empty | mystery, frozen×2, ad bottle, sizes 2/3/5/6 | 23 | — | 23 | PASS |
| 232 | normal | 8 | 10 | 9 slots: 2 empty | mystery, frozen×2, sizes 3/4/5 | 26 | — | 26 | PASS |
| 233 | hard | 8 | 11 | 10 slots: 2 empty | mystery, frozen×2, ad bottle, sizes 2/3/4/5/6 | 33 | — | 33 | PASS |
| 234 | normal | 8 | 10 | 9 slots: 2 empty | mystery, frozen×2, sizes 3/4/5 | 31 | — | 35 | PASS |
| 235 | normal | 8 | 11 | 10 slots: 2 empty | mystery, frozen×1, locked×1, ad bottle, sizes 2/3/5/6 | 24 | — | 24 | PASS |
| 236 | normal | 7 | 9 | 9 slots: 2 empty | mystery, frozen×1, locked×1, sizes 4/5 | 30 | — | 30 | PASS |
| 237 | normal | 8 | 11 | 10 slots: 2 empty | mystery, frozen×1, locked×1, ad bottle, sizes 2/3/4/6 | 22 | — | 23 | PASS |
| 238 | normal | 8 | 10 | 9 slots: 2 empty | mystery, frozen×2, sizes 3/4/5 | 25 | — | 28 | PASS |
| 239 | normal | 8 | 11 | 9 slots: 2 empty | mystery, frozen×1, locked×1, ad bottle, sizes 2/3/4/5 | 33 | — | 34 | PASS |
| 240 | boss | 8 | 10 | 9 slots: 2 empty | mystery, locked×2, sizes 3/4/5 | 39 | — | 39 | PASS |
| 241 | normal | 8 | 11 | 10 slots: 2 empty | mystery, frozen×2, ad bottle, sizes 2/4/5/6 | 29 | — | 31 | PASS |
| 242 | normal | 8 | 10 | 9 slots: 2 empty | mystery, frozen×1, locked×1, sizes 3/4/5 | 35 | — | 35 | PASS |
| 243 | normal | 8 | 11 | 10 slots: 2 empty | mystery, frozen×1, locked×1, ad bottle, sizes 2/3/4/5/6 | 26 | — | 26 | PASS |
| 244 | hard | 8 | 10 | 9 slots: 2 empty | mystery, frozen×2, sizes 3/4/5 | 33 | — | 37 | PASS |
| 245 | normal | 8 | 11 | 9 slots: 2 empty | mystery, frozen×2, ad bottle, sizes 3/4/5 | 28 | — | 28 | PASS |
| 246 | hard | 8 | 10 | 9 slots: 2 empty | mystery, frozen×1, locked×1, sizes 3/4/5 | 33 | — | 34 | PASS |
| 247 | normal | 8 | 11 | 10 slots: 2 empty | mystery, frozen×1, locked×1, ad bottle, sizes 2/3/4/6 | 27 | — | 28 | PASS |
| 248 | normal | 8 | 10 | 9 slots: 2 empty | mystery, locked×2, sizes 3/4/5 | 26 | — | 26 | PASS |
| 249 | hard | 8 | 11 | 10 slots: 2 empty | mystery, frozen×1, locked×1, ad bottle, sizes 2/4/5/6 | 38 | — | 38 | PASS |
| 250 | boss | 8 | 10 | 9 slots: 2 empty | mystery, frozen×1, locked×1, sizes 3/4/5 | 32 | — | 33 | PASS |
| 251 | normal | 8 | 17 | 8 slots: 4 partly filled, 2 small empty | mystery, frozen×1, locked×1 | 45 | — | 49 | PASS |
| 252 | normal | 8 | 18 | 8 slots: 2 empty | locked×2, ad bottle | 48 | — | 49 | PASS |
| 253 | normal | 8 | 17 | 8 slots: 4 partly filled, 2 small empty | mystery, frozen×1, locked×1 | 44 | — | 45 | PASS |
| 254 | normal | 8 | 18 | 8 slots: 2 empty | frozen×1, locked×1, ad bottle | 52 | — | 53 | PASS |
| 255 | normal | 8 | 17 | 8 slots: 4 partly filled, 2 small empty | mystery, frozen×2 | 47 | — | 48 | PASS |
| 256 | normal | 8 | 18 | 8 slots: 2 empty | locked×2, ad bottle | 46 | — | 46 | PASS |
| 257 | normal | 8 | 17 | 8 slots: 3 partly filled, 2 small empty | mystery, frozen×1, locked×1 | 45 | — | 46 | PASS |
| 258 | normal | 8 | 18 | 8 slots: 2 empty | frozen×1, locked×1, ad bottle | 50 | — | 52 | PASS |
| 259 | hard | 8 | 17 | 8 slots: 4 partly filled, 2 small empty | mystery, frozen×1, locked×1 | 48 | — | 49 | PASS |
| 260 | boss | 8 | 18 | 8 slots: 2 empty | frozen×1, locked×1, ad bottle | 50 | — | 50 | PASS |
| 261 | normal | 7 | 16 | 8 slots: 3 partly filled, 2 small empty | mystery, locked×2 | 44 | — | 45 | PASS |
| 262 | normal | 7 | 17 | 8 slots: 2 empty | frozen×2, ad bottle | 44 | — | 45 | PASS |
| 263 | normal | 8 | 17 | 8 slots: 4 partly filled, 2 small empty | mystery, frozen×1, locked×1 | 49 | — | 49 | PASS |
| 264 | normal | 8 | 18 | 8 slots: 2 empty | frozen×1, locked×1, ad bottle | 44 | — | 44 | PASS |
| 265 | normal | 8 | 17 | 8 slots: 4 partly filled, 2 small empty | mystery, frozen×1, locked×1 | 48 | — | 48 | PASS |
| 266 | normal | 7 | 17 | 8 slots: 2 empty | frozen×1, locked×1, ad bottle | 43 | — | 43 | PASS |
| 267 | normal | 7 | 16 | 8 slots: 3 partly filled, 2 small empty | mystery, frozen×2 | 46 | — | 46 | PASS |
| 268 | normal | 8 | 18 | 8 slots: 2 empty | frozen×1, locked×1, ad bottle | 51 | — | 51 | PASS |
| 269 | hard | 8 | 17 | 8 slots: 4 partly filled, 2 small empty | mystery, frozen×2 | 46 | — | 51 | PASS |
| 270 | boss | 8 | 18 | 8 slots: 2 empty | frozen×1, locked×1, ad bottle | 50 | — | 51 | PASS |
| 271 | normal | 8 | 17 | 8 slots: 4 partly filled, 2 small empty | mystery, frozen×1, locked×1 | 47 | — | 46 | PASS |
| 272 | normal | 8 | 18 | 8 slots: 2 empty | frozen×1, locked×1, ad bottle | 49 | — | 49 | PASS |
| 273 | normal | 8 | 17 | 8 slots: 3 partly filled, 2 small empty | mystery, frozen×1, locked×1 | 46 | — | 48 | PASS |
| 274 | normal | 8 | 18 | 8 slots: 2 empty | locked×2, ad bottle | 50 | — | 54 | PASS |
| 275 | normal | 8 | 17 | 8 slots: 4 partly filled, 2 small empty | mystery, locked×2 | 48 | — | 48 | PASS |
| 276 | normal | 8 | 18 | 8 slots: 2 empty | mystery, frozen×1, locked×2 | 54 | — | 56 | PASS |
| 277 | normal | 8 | 20 | 8 slots: 4 partly filled, 2 small empty | mystery, frozen×1, locked×1, ad bottle | 51 | — | 52 | PASS |
| 278 | normal | 7 | 16 | 8 slots: 2 empty | mystery, frozen×2, locked×1 | 47 | — | 48 | PASS |
| 279 | normal | 8 | 20 | 8 slots: 4 partly filled, 2 small empty | mystery, frozen×2, ad bottle | 54 | — | 56 | PASS |
| 280 | boss | 8 | 18 | 8 slots: 2 empty | mystery, frozen×1, locked×2 | 53 | — | 57 | PASS |
| 281 | normal | 8 | 20 | 8 slots: 2 partly filled, 2 small empty | mystery, frozen×2, ad bottle | 53 | — | 53 | PASS |
| 282 | normal | 8 | 18 | 8 slots: 2 empty | mystery, frozen×1, locked×2 | 48 | — | 48 | PASS |
| 283 | normal | 8 | 20 | 8 slots: 4 partly filled, 2 small empty | mystery, locked×2, ad bottle | 56 | — | 58 | PASS |
| 284 | normal | 8 | 18 | 8 slots: 2 empty | mystery, frozen×2, locked×1 | 55 | — | 55 | PASS |
| 285 | normal | 8 | 20 | 8 slots: 3 partly filled, 2 small empty | mystery, locked×2, ad bottle | 52 | — | 56 | PASS |
| 286 | hard | 8 | 18 | 8 slots: 2 empty | mystery, frozen×1, locked×2 | 55 | — | 57 | PASS |
| 287 | normal | 8 | 20 | 8 slots: 3 partly filled, 2 small empty | mystery, locked×2, ad bottle | 54 | — | 55 | PASS |
| 288 | hard | 8 | 18 | 8 slots: 2 empty | mystery, frozen×2, locked×1 | 53 | — | 53 | PASS |
| 289 | normal | 8 | 20 | 8 slots: 3 partly filled, 2 small empty | mystery, locked×2, ad bottle | 53 | — | 54 | PASS |
| 290 | boss | 8 | 18 | 8 slots: 2 empty | mystery, frozen×1, locked×2 | 53 | — | 56 | PASS |
| 291 | normal | 8 | 20 | 8 slots: 3 partly filled, 2 small empty | mystery, frozen×1, locked×1, ad bottle | 51 | — | 52 | PASS |
| 292 | normal | 8 | 18 | 8 slots: 2 empty | mystery, frozen×2, locked×1 | 51 | — | 51 | PASS |
| 293 | normal | 8 | 20 | 8 slots: 4 partly filled, 2 small empty | mystery, frozen×1, locked×1, ad bottle | 52 | — | 54 | PASS |
| 294 | normal | 8 | 18 | 8 slots: 2 empty | mystery, frozen×1, locked×2 | 53 | — | 54 | PASS |
| 295 | normal | 7 | 18 | 8 slots: 3 partly filled, 2 small empty | mystery, frozen×1, locked×1, ad bottle | 47 | — | 47 | PASS |
| 296 | hard | 8 | 18 | 8 slots: 2 empty | mystery, locked×3 | 56 | — | 56 | PASS |
| 297 | normal | 8 | 20 | 8 slots: 4 partly filled, 2 small empty | mystery, locked×2, ad bottle | 52 | — | 54 | PASS |
| 298 | normal | 8 | 18 | 8 slots: 2 empty | mystery, frozen×2, locked×1 | 54 | — | 54 | PASS |
| 299 | normal | 7 | 18 | 8 slots: 4 partly filled, 2 small empty | mystery, frozen×1, locked×1, ad bottle | 44 | — | 45 | PASS |
| 300 | boss | 8 | 18 | 8 slots: 2 empty | mystery, frozen×1, locked×2 | 55 | — | 56 | PASS |
