using System;
using System.Collections.Generic;
using Veliqora.Core.Puzzle;
using Veliqora.Core.Random;
using Veliqora.Core.Solver;

namespace Veliqora.Core.Levels
{
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
    }

    public sealed class GeneratedLevel
    {
        public int[][] Bottles;
        public ulong Seed;
        public SolverResult Solve;
        public int Attempts;
        /// <summary>Fraction of naive random playouts that got stuck (see DifficultyAnalyzer). Filled by the builder.</summary>
        public double StuckRate;

        /// <summary>Difficulty score from measured solution length, branching, search effort and trap density.</summary>
        public double DifficultyScore => Solve.Length + 1.5 * Solve.AverageBranching + Math.Log(1 + Solve.NodesExpanded, 2) + 25.0 * StuckRate;
    }

    /// <summary>
    /// Verified generator: deals a random arrangement from a deterministic seed, then accepts it ONLY if the
    /// solver finds an explicit solution (replay-verified) within a bounded budget and the solution is long
    /// enough to be non-trivial. Unsolvable or undecided (timeout) deals are rejected, never shipped.
    /// </summary>
    public static class LevelGenerator
    {
        public static GeneratedLevel Generate(GenerationSpec spec, ulong seed)
        {
            if (spec.Colors < 1) throw new ArgumentException("Need at least one color");
            for (int attempt = 0; attempt < spec.MaxAttempts; attempt++)
            {
                ulong s = DeterministicRandom.Combine(seed, attempt, spec.Colors * 100 + spec.EmptyBottles);
                var bottles = Deal(spec.Colors, spec.EmptyBottles, spec.Capacity, s);
                if (!PassesStructuralFilters(bottles, spec)) continue;

                var state = GameState.FromArrays(spec.Capacity, bottles);
                var result = PuzzleSolver.Solve(state, spec.Solver);
                if (result.Status != SolveStatus.Solved) continue;
                if (result.Length < spec.MinSolutionLength || result.Length > spec.MaxSolutionLength) continue;
                if (!PuzzleSolver.VerifySolution(state, result.Moves, out _)) continue;

                return new GeneratedLevel { Bottles = bottles, Seed = s, Solve = result, Attempts = attempt + 1 };
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

        /// <summary>
        /// Runtime-safe "Shuffle" booster: produces a new verified-solvable arrangement using the same colors,
        /// bottle count and capacity as the level. Returns null if no verified arrangement is found in budget
        /// (callers must then keep the current layout and say so).
        /// </summary>
        public static GeneratedLevel GenerateShuffle(LevelDefinition level, int shuffleIndex, int extraEmptyBottles = 0)
        {
            var spec = new GenerationSpec
            {
                Colors = level.ColorCount,
                EmptyBottles = level.EmptyBottleCount + extraEmptyBottles,
                Capacity = level.Capacity,
                MinSolutionLength = Math.Max(1, Math.Min(level.ParMoves / 2, 6)),
                MaxAttempts = 40,
                Solver = new SolverOptions { MaxNodes = 40_000, TimeoutMs = 600, HeuristicWeight = 2.0 },
            };
            return Generate(spec, DeterministicRandom.Combine(level.Seed, 7919, shuffleIndex + 1));
        }

        public static int CountColors(int[][] bottles)
        {
            var set = new HashSet<int>();
            foreach (var b in bottles) foreach (var c in b) set.Add(c);
            return set.Count;
        }
    }
}
