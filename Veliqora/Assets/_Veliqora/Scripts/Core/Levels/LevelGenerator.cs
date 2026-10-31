using System;
using System.Collections.Generic;
using Veliqora.Core.Puzzle;
using Veliqora.Core.Random;
using Veliqora.Core.Solver;

namespace Veliqora.Core.Levels
{
    public enum CapacityMode
    {
        /// <summary>Every bottle has <see cref="GenerationSpec.Capacity"/>.</summary>
        Uniform,
        /// <summary>Filled bottles of 3/4/5 units, one larger empty bottle.</summary>
        SmallLarge,
        /// <summary>Full range 2–6 units.</summary>
        Wide,
    }

    public sealed class GenerationSpec
    {
        public int Colors = 4;
        public int EmptyBottles = 2;
        public int Capacity = BottleState.DefaultCapacity;
        /// <summary>Reject starts whose benchmark solution is shorter than this (prevents trivial levels).</summary>
        public int MinSolutionLength = 6;
        public int MaxSolutionLength = 999;
        /// <summary>Max adjacent equal-color pairs inside bottles (high = pre-sorted, too easy).</summary>
        public int MaxAdjacentPairs = 99;
        public int MaxAttempts = 400;
        public SolverOptions Solver = SolverOptions.Generation;

        // ---- special mechanics ----
        public CapacityMode CapacityMode = CapacityMode.Uniform;
        /// <summary>Mystery units hidden at the bottom of each mystery bottle (0 = no mystery).</summary>
        public int MysteryDepth;
        /// <summary>How many filled bottles get mystery layers (0 = all filled bottles when MysteryDepth &gt; 0).</summary>
        public int MysteryBottles;
        /// <summary>Corked bottles (0–2).</summary>
        public int Locks;
        /// <summary>Extra ad-locked empty bottles appended (never needed for the solution).</summary>
        public int RewardBottles;

        public bool HasSpecials => CapacityMode != CapacityMode.Uniform || MysteryDepth > 0 || Locks > 0 || RewardBottles > 0;
    }

    public sealed class GeneratedLevel
    {
        public int[][] Bottles;
        /// <summary>Full starting board including capacities, mystery layers, corks and ad-locked bottles.</summary>
        public GameState State;
        public ulong Seed;
        public SolverResult Solve;
        public int Attempts;
        /// <summary>Fraction of naive random playouts that got stuck (see DifficultyAnalyzer). Filled by the builder.</summary>
        public double StuckRate;
        /// <summary>Exact minimum pours (BFS), -1 = not computed / above budget.</summary>
        public int MinimumMoves = -1;

        /// <summary>Difficulty score from measured solution length, branching, search effort and trap density.</summary>
        public double DifficultyScore => Solve.Length + 1.5 * Solve.AverageBranching + Math.Log(1 + Solve.NodesExpanded, 2) + 25.0 * StuckRate;
    }

    /// <summary>
    /// Verified generator: deals a random arrangement from a deterministic seed (optionally with special bottles),
    /// then accepts it ONLY if the solver finds an explicit, replay-verified solution within a bounded budget and the
    /// solution is long enough to be non-trivial. Unsolvable or undecided (timeout) deals are rejected, never shipped.
    /// </summary>
    public static class LevelGenerator
    {
        public static GeneratedLevel Generate(GenerationSpec spec, ulong seed)
        {
            if (spec.Colors < 1) throw new ArgumentException("Need at least one color");
            for (int attempt = 0; attempt < spec.MaxAttempts; attempt++)
            {
                ulong s = DeterministicRandom.Combine(seed, attempt, spec.Colors * 100 + spec.EmptyBottles);
                GameState state;
                if (!spec.HasSpecials)
                {
                    var bottles = Deal(spec.Colors, spec.EmptyBottles, spec.Capacity, s);
                    if (!PassesStructuralFilters(bottles, spec)) continue;
                    state = GameState.FromArrays(spec.Capacity, bottles);
                }
                else
                {
                    state = DealSpecial(spec, s);
                    if (state == null || !PassesStructuralFilters(state, spec)) continue;
                }

                var result = PuzzleSolver.Solve(state, spec.Solver);
                if (result.Status != SolveStatus.Solved) continue;
                if (result.Length < spec.MinSolutionLength || result.Length > spec.MaxSolutionLength) continue;
                if (!PuzzleSolver.VerifySolution(state, result.Moves, out _)) continue;

                return new GeneratedLevel { Bottles = state.ToArrays(), State = state, Seed = s, Solve = result, Attempts = attempt + 1 };
            }
            return null;
        }

        /// <summary>Uniform shuffle of capacity units per color into full bottles, followed by empty bottles.</summary>
        public static int[][] Deal(int colors, int emptyBottles, int capacity, ulong seed)
        {
            var rng = new DeterministicRandom(seed);
            var pool = new int[colors * capacity];
            for (int c = 0; c < colors; c++)
                for (int k = 0; k < capacity; k++)
                    pool[c * capacity + k] = c + 1;
            rng.Shuffle(pool);

            var bottles = new int[colors + emptyBottles][];
            for (int b = 0; b < colors; b++)
            {
                bottles[b] = new int[capacity];
                Array.Copy(pool, b * capacity, bottles[b], 0, capacity);
            }
            for (int b = colors; b < bottles.Length; b++) bottles[b] = new int[0];
            return bottles;
        }

        /// <summary>
        /// Deal with special bottles. Each color's unit count equals the capacity of one filled bottle, so every
        /// color can end up in a single complete bottle. Then mystery layers, corks and ad-locked bottles are placed.
        /// </summary>
        public static GameState DealSpecial(GenerationSpec spec, ulong seed)
        {
            var rng = new DeterministicRandom(seed);
            int n = spec.Colors;
            var caps = new int[n];
            for (int i = 0; i < n; i++)
            {
                switch (spec.CapacityMode)
                {
                    case CapacityMode.SmallLarge: caps[i] = 3 + rng.Range(0, 3); break;      // 3..5
                    case CapacityMode.Wide: caps[i] = 2 + rng.Range(0, 5); break;            // 2..6
                    default: caps[i] = spec.Capacity; break;
                }
            }
            // Color sizes are a permutation of the bottle sizes (so colors don't simply match their start bottle).
            var colorSize = (int[])caps.Clone();
            rng.Shuffle(colorSize);
            int total = 0;
            foreach (var c in caps) total += c;
            var pool = new int[total];
            int k = 0;
            for (int c = 0; c < n; c++) for (int u = 0; u < colorSize[c]; u++) pool[k++] = c + 1;
            rng.Shuffle(pool);

            var bottles = new List<BottleState>();
            k = 0;
            for (int b = 0; b < n; b++)
            {
                var units = new int[caps[b]];
                Array.Copy(pool, k, units, 0, caps[b]);
                k += caps[b];
                bottles.Add(new BottleState(caps[b], units));
            }
            int maxCap = 0;
            foreach (var c in caps) maxCap = Math.Max(maxCap, c);
            for (int e = 0; e < spec.EmptyBottles; e++)
            {
                // With varied sizes, the first empty bottle is the largest size so every color has somewhere to go.
                int cap = spec.CapacityMode == CapacityMode.Uniform ? spec.Capacity : (e == 0 ? Math.Max(maxCap, spec.Capacity) : spec.Capacity);
                bottles.Add(BottleState.Empty(cap));
            }

            // Mystery layers.
            if (spec.MysteryDepth > 0)
            {
                var order = Indices(n, rng);
                int count = spec.MysteryBottles > 0 ? Math.Min(spec.MysteryBottles, n) : n;
                for (int i = 0; i < count; i++)
                {
                    int b = order[i];
                    bottles[b] = bottles[b].WithHidden(Math.Min(spec.MysteryDepth, bottles[b].Count - 1));
                }
            }

            // Corks with achievable conditions.
            if (spec.Locks > 0)
            {
                var order = Indices(n, rng);
                var locked = new List<int>();
                for (int i = 0; i < Math.Min(spec.Locks, n - 2); i++)
                {
                    int b = order[i];
                    BottleLock lk;
                    if (i > 0 && rng.NextDouble() < 0.5)
                        lk = new BottleLock(LockKind.AfterBottle, locked[0]);           // chain: opens after the first cork
                    else
                    {
                        int pick = rng.Range(0, 3);
                        if (pick == 0)
                        {
                            // A color that has no units inside any corked bottle (otherwise it could never be completed).
                            var candidates = new List<int>();
                            for (int c = 1; c <= n; c++)
                            {
                                bool inside = bottles[b].CountOf(c) > 0;
                                foreach (var lb in locked) if (bottles[lb].CountOf(c) > 0) inside = true;
                                if (!inside) candidates.Add(c);
                            }
                            lk = candidates.Count > 0 ? new BottleLock(LockKind.CompleteColor, candidates[rng.Range(0, candidates.Count)])
                                                      : new BottleLock(LockKind.Moves, 4 + rng.Range(0, 5));
                        }
                        else if (pick == 1) lk = new BottleLock(LockKind.Moves, 3 + rng.Range(0, 6));
                        else lk = new BottleLock(LockKind.CompletedBottles, 1 + rng.Range(0, 2));
                    }
                    bottles[b] = bottles[b].WithLock(lk);
                    locked.Add(b);
                }
            }

            for (int r = 0; r < spec.RewardBottles; r++)
                bottles.Add(new BottleState(spec.Capacity, new int[0], 0, null, true));

            return PuzzleRules.ResolveLocks(new GameState(bottles));
        }

        private static int[] Indices(int n, DeterministicRandom rng)
        {
            var a = new int[n];
            for (int i = 0; i < n; i++) a[i] = i;
            rng.Shuffle(a);
            return a;
        }

        public static bool PassesStructuralFilters(int[][] bottles, GenerationSpec spec)
        {
            int pairs = 0;
            foreach (var b in bottles)
            {
                if (b.Length == 0) continue;
                bool mono = true;
                for (int i = 1; i < b.Length; i++)
                {
                    if (b[i] == b[i - 1]) pairs++;
                    else mono = false;
                }
                // A bottle that starts already complete is a freebie.
                if (mono && b.Length == spec.Capacity) return false;
            }
            return pairs <= spec.MaxAdjacentPairs;
        }

        public static bool PassesStructuralFilters(GameState s, GenerationSpec spec)
        {
            int pairs = 0;
            for (int i = 0; i < s.BottleCount; i++)
            {
                var b = s[i];
                if (b.IsEmpty) continue;
                if (s.IsBottleComplete(i)) return false;
                for (int u = 1; u < b.Count; u++) if (b[u] == b[u - 1]) pairs++;
            }
            if (!PuzzleRules.HasValidColorDistribution(s, out _)) return false;
            return pairs <= spec.MaxAdjacentPairs;
        }

        public sealed class ShuffleResult
        {
            public GameState State;
            public SolverResult Solve;
        }

        /// <summary>
        /// Runtime-safe "Shuffle" booster. Re-deals the units of the ELIGIBLE bottles (not corked, not ad-locked) of
        /// <paramref name="layout"/>, keeping every bottle's capacity, unit count, mystery depth and lock, so liquid
        /// quantities and fixed restrictions are preserved. Accepted only if the solver verifies the result; returns
        /// null otherwise (callers keep the current puzzle, refund, and say so).
        /// </summary>
        public static ShuffleResult ShuffleLayout(GameState layout, ulong seed, int attempts = 40, SolverOptions solver = null)
        {
            solver = solver ?? new SolverOptions { MaxNodes = 40_000, TimeoutMs = 600, HeuristicWeight = 2.0 };
            var eligible = new List<int>();
            var pool = new List<int>();
            for (int i = 0; i < layout.BottleCount; i++)
            {
                if (layout[i].IsLocked) continue;
                eligible.Add(i);
                pool.AddRange(layout[i].ToArray());
            }
            if (pool.Count == 0) return null;
            var arr = pool.ToArray();
            string originalKey = layout.WithMoves(0).CanonicalKey();
            for (int a = 0; a < attempts; a++)
            {
                var rng = new DeterministicRandom(DeterministicRandom.Combine(seed, a, 0x5AFF));
                rng.Shuffle(arr);
                var bottles = new BottleState[layout.BottleCount];
                int k = 0;
                for (int i = 0; i < layout.BottleCount; i++)
                {
                    var b = layout[i];
                    if (b.IsLocked) { bottles[i] = b; continue; }
                    var units = new int[b.Count];
                    Array.Copy(arr, k, units, 0, b.Count);
                    k += b.Count;
                    bottles[i] = b.WithUnits(units, b.Hidden);
                }
                var state = PuzzleRules.ResolveLocks(new GameState(bottles));
                if (state.CanonicalKey() == originalKey) continue;
                bool freebie = false;
                for (int i = 0; i < state.BottleCount; i++) if (!state[i].IsEmpty && state.IsBottleComplete(i)) freebie = true;
                if (freebie || PuzzleRules.IsWon(state)) continue;
                var solve = PuzzleSolver.Solve(state, solver);
                if (solve.Status != SolveStatus.Solved || !PuzzleSolver.VerifySolution(state, solve.Moves, out _)) continue;
                return new ShuffleResult { State = state, Solve = solve };
            }
            return null;
        }

        /// <summary>Shuffle of a level's starting layout (used by tests and tools).</summary>
        public static ShuffleResult GenerateShuffle(LevelDefinition level, int shuffleIndex)
            => ShuffleLayout(level.CreateInitialState(), DeterministicRandom.Combine(level.Seed, 7919, shuffleIndex + 1));

        public static int CountColors(int[][] bottles)
        {
            var set = new HashSet<int>();
            foreach (var b in bottles) foreach (var c in b) set.Add(c);
            return set.Count;
        }
    }
}
