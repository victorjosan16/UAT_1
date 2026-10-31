using System;
using System.Collections.Generic;
using Veliqora.Core.Puzzle;
using Veliqora.Core.Random;
using Veliqora.Core.Solver;

namespace Veliqora.Core.Levels
{
    /// <summary>One segment of the difficulty curve: a contiguous range of level numbers sharing a spec.</summary>
    public sealed class CurveSegment
    {
        public int FirstLevel;
        public int LastLevel;
        public int Colors;
        public int EmptyBottles = 2;
        public int Capacity = BottleState.DefaultCapacity;
        public int MinSolution;
        public int Candidates = 3;
        public int MaxAdjacentPairs = 99;
        /// <summary>Optional variant used on even level numbers (introduces constraints gradually).</summary>
        public CurveSegment AlternateOnEven;

        public CurveSegment ForLevel(int n) => AlternateOnEven != null && n % 2 == 0 ? AlternateOnEven : this;
    }

    public sealed class BuildReportRow
    {
        public int Number;
        public string Tier;
        public ulong Seed;
        public int Colors;
        public int Bottles;
        public int Capacity;
        public int SolutionLength;
        public int Nodes;
        public long Ms;
        public double Branching;
        public double StuckRate;
        public bool Handcrafted;
        public bool Passed;
        public string Note;
    }

    /// <summary>
    /// Builds the full catalog deterministically from a base seed: 10 handcrafted onboarding levels followed by
    /// solver-verified generated levels along a gradual difficulty curve. Intended to run OFFLINE (editor menu or
    /// the Tools/LevelGen console), never at app runtime.
    /// </summary>
    public static class LevelCatalogBuilder
    {
        public const ulong DefaultBaseSeed = 0x5645_4C49_514F_5241UL; // "VELIQORA"
        public const int DefaultLevelCount = 300;

        // Measured with Tools/LevelGen: 1-spare-bottle deals are often provably unsolvable (rejected) and leave far
        // less room for error; capacity-5 "tall" bottles lengthen solutions. Both are introduced late and gradually.
        private static CurveSegment Seg(int a, int b, int colors, int minSol, int cand, int empties = 2, int cap = 4, int pairs = 3, CurveSegment alt = null) =>
            new CurveSegment { FirstLevel = a, LastLevel = b, Colors = colors, MinSolution = minSol, Candidates = cand, EmptyBottles = empties, Capacity = cap, MaxAdjacentPairs = pairs, AlternateOnEven = alt };

        public static readonly CurveSegment[] DefaultCurve =
        {
            Seg(11, 17, 4, 7, 2),
            Seg(18, 25, 5, 9, 2),
            Seg(26, 40, 5, 10, 3, pairs: 2),
            Seg(41, 60, 6, 12, 3),
            Seg(61, 75, 7, 14, 3),
            Seg(76, 90, 7, 16, 3, pairs: 2),
            Seg(91, 100, 7, 16, 3, alt: Seg(91, 100, 4, 8, 3, empties: 1)),
            Seg(101, 110, 7, 17, 3, alt: Seg(101, 110, 5, 10, 3, empties: 1)),
            Seg(111, 140, 8, 18, 3),
            Seg(141, 150, 8, 20, 3, alt: Seg(141, 150, 5, 11, 4, empties: 1)),
            Seg(151, 165, 8, 22, 5, pairs: 2),
            Seg(166, 180, 7, 24, 4, cap: 5),
            Seg(181, 200, 8, 24, 5, pairs: 2, alt: Seg(181, 200, 6, 14, 3, empties: 1)),
            // Master: dense boards (up to 14 bottles) like late-game water-sort levels; 9–12 colors.
            Seg(201, 225, 9, 26, 4, pairs: 2),
            Seg(226, 250, 10, 30, 4, pairs: 2),
            Seg(251, 275, 11, 33, 4, pairs: 2, alt: Seg(251, 275, 12, 34, 3, empties: 3)),
            Seg(276, 300, 12, 38, 4, pairs: 2),
        };

        public static LevelCatalog Build(ulong baseSeed, int levelCount, List<BuildReportRow> report, Action<int, int> progress = null,
            CurveSegment[] curve = null)
        {
            curve = curve ?? DefaultCurve;
            var catalog = new LevelCatalog { BaseSeed = baseSeed, GeneratedAtUtc = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") };

            for (int n = 1; n <= levelCount; n++)
            {
                LevelDefinition def;
                if (n <= HandcraftedLevels.All.Length) def = BuildHandcrafted(n, report);
                else def = BuildGenerated(n, baseSeed, FindSegment(curve, n).ForLevel(n), report);
                if (def == null) throw new InvalidOperationException("Failed to build level " + n + " — see report");
                // Contextual tip the first time a new constraint appears.
                if (def.TutorialKey == null && def.EmptyBottleCount == 1 && !catalog.Levels.Exists(l => l.EmptyBottleCount == 1))
                    def.TutorialKey = "one_spare_bottle";
                if (def.TutorialKey == null && def.Capacity == 5 && !catalog.Levels.Exists(l => l.Capacity == 5))
                    def.TutorialKey = "tall_bottles";
                catalog.Levels.Add(def);
                progress?.Invoke(n, levelCount);
            }
            return catalog;
        }

        private static CurveSegment FindSegment(CurveSegment[] curve, int n)
        {
            foreach (var s in curve) if (n >= s.FirstLevel && n <= s.LastLevel) return s;
            // Beyond the authored curve: reuse the last segment.
            return curve[curve.Length - 1];
        }

        public static LevelDefinition BuildHandcrafted(int n, List<BuildReportRow> report)
        {
            var entry = HandcraftedLevels.All[n - 1];
            int capacity = BottleState.DefaultCapacity;
            var state = GameState.FromArrays(capacity, entry.Bottles);
            var solve = PuzzleSolver.Solve(state, new SolverOptions { MaxNodes = 400_000, TimeoutMs = 10_000, HeuristicWeight = 1.0 });
            bool ok = solve.Status == SolveStatus.Solved && PuzzleSolver.VerifySolution(state, solve.Moves, out _);
            report?.Add(Row(n, 0, entry.Bottles, capacity, solve, true, ok, ok ? "handcrafted" : "handcrafted level failed validation"));
            if (!ok) return null;
            return new LevelDefinition
            {
                Number = n,
                Seed = 0,
                Capacity = capacity,
                ColorCount = LevelGenerator.CountColors(entry.Bottles),
                Bottles = entry.Bottles,
                Tier = DifficultyTiers.ForLevel(n),
                Handcrafted = true,
                ParMoves = solve.Length,
                Solution = LevelDefinition.EncodeMoves(solve.Moves),
                TutorialKey = entry.TutorialKey,
                Validation = ToValidation(solve, 1),
            };
        }

        public static LevelDefinition BuildGenerated(int n, ulong baseSeed, CurveSegment seg, List<BuildReportRow> report)
        {
            var spec = new GenerationSpec
            {
                Colors = seg.Colors,
                EmptyBottles = seg.EmptyBottles,
                Capacity = seg.Capacity,
                MinSolutionLength = seg.MinSolution,
                MaxAdjacentPairs = seg.MaxAdjacentPairs,
                Solver = SolverOptions.Generation,
            };

            var candidates = new List<GeneratedLevel>();
            for (int c = 0; c < seg.Candidates; c++)
            {
                var g = LevelGenerator.Generate(spec, DeterministicRandom.Combine(baseSeed, n, c));
                if (g == null) continue;
                g.StuckRate = DifficultyAnalyzer.StuckRate(GameState.FromArrays(spec.Capacity, g.Bottles), DeterministicRandom.Combine(g.Seed, 1));
                candidates.Add(g);
            }
            if (candidates.Count == 0)
            {
                report?.Add(new BuildReportRow { Number = n, Tier = DifficultyTiers.ForLevel(n).ToString(), Colors = seg.Colors, Passed = false, Note = "no verified candidate within budget" });
                return null;
            }

            // Gentle sawtooth ramp: early in a segment pick the easiest candidate, late in the segment the hardest.
            candidates.Sort((a, b) => a.DifficultyScore.CompareTo(b.DifficultyScore));
            double t = seg.LastLevel == seg.FirstLevel ? 1.0 : (double)(n - seg.FirstLevel) / (seg.LastLevel - seg.FirstLevel);
            int pick = (int)Math.Round(t * (candidates.Count - 1));
            var chosen = candidates[pick];

            // Tighter benchmark for stars: re-solve the chosen level with plain A* (weight 1). Keep whichever
            // verified solution is shorter. Still reported as a benchmark, not as proven optimal.
            var start = GameState.FromArrays(seg.Capacity, chosen.Bottles);
            var tight = PuzzleSolver.Solve(start, new SolverOptions { MaxNodes = 300_000, TimeoutMs = 5_000, HeuristicWeight = 1.0 });
            if (tight.Status == SolveStatus.Solved && tight.Length < chosen.Solve.Length && PuzzleSolver.VerifySolution(start, tight.Moves, out _))
                chosen.Solve = tight;

            var row = Row(n, chosen.Seed, chosen.Bottles, seg.Capacity, chosen.Solve, false, true, "candidates=" + candidates.Count);
            row.StuckRate = chosen.StuckRate;
            report?.Add(row);
            int attempts = 0;
            foreach (var cnd in candidates) attempts += cnd.Attempts;
            return new LevelDefinition
            {
                Number = n,
                Seed = chosen.Seed,
                Capacity = seg.Capacity,
                ColorCount = seg.Colors,
                Bottles = chosen.Bottles,
                Tier = DifficultyTiers.ForLevel(n),
                Handcrafted = false,
                ParMoves = chosen.Solve.Length,
                Solution = LevelDefinition.EncodeMoves(chosen.Solve.Moves),
                Validation = ToValidation(chosen.Solve, attempts, chosen.StuckRate),
            };
        }

        private static LevelValidation ToValidation(SolverResult r, int attempts, double stuck = 0) => new LevelValidation
        {
            StuckRate = stuck,
            Status = r.Status.ToString(),
            SolutionLength = r.Length,
            NodesExpanded = r.NodesExpanded,
            ElapsedMs = r.ElapsedMs,
            AverageBranching = r.AverageBranching,
            Attempts = attempts,
        };

        private static BuildReportRow Row(int n, ulong seed, int[][] bottles, int capacity, SolverResult r, bool handcrafted, bool passed, string note) => new BuildReportRow
        {
            Number = n,
            Tier = DifficultyTiers.ForLevel(n).ToString(),
            Seed = seed,
            Colors = LevelGenerator.CountColors(bottles),
            Bottles = bottles.Length,
            Capacity = capacity,
            SolutionLength = r.Length,
            Nodes = r.NodesExpanded,
            Ms = r.ElapsedMs,
            Branching = r.AverageBranching,
            Handcrafted = handcrafted,
            Passed = passed,
            Note = note,
        };

        /// <summary>
        /// Independent re-validation of a catalog: replays every stored solution and re-runs the solver from the
        /// start state. Used by the batch validator and EditMode tests.
        /// </summary>
        public static List<BuildReportRow> Validate(LevelCatalog catalog, bool rerunSolver, SolverOptions options = null)
        {
            var rows = new List<BuildReportRow>();
            foreach (var l in catalog.Levels)
            {
                var state = l.CreateInitialState();
                var row = new BuildReportRow
                {
                    Number = l.Number, Tier = l.Tier.ToString(), Seed = l.Seed, Colors = l.ColorCount,
                    Bottles = l.Bottles.Length, Capacity = l.Capacity, Handcrafted = l.Handcrafted, StuckRate = l.Validation.StuckRate,
                };
                string err;
                bool structural = PuzzleRules.HasValidColorDistribution(state, out err);
                bool replay = structural && PuzzleSolver.VerifySolution(state, l.SolutionMoves(), out err);
                bool notTrivial = !PuzzleRules.IsWon(state);
                row.SolutionLength = l.Solution.Length;
                row.Passed = structural && replay && notTrivial;
                row.Note = row.Passed ? "replay ok" : ("FAIL: " + (err ?? "already solved"));
                if (rerunSolver && row.Passed)
                {
                    var r = PuzzleSolver.Solve(state, options ?? SolverOptions.Validation);
                    row.Nodes = r.NodesExpanded;
                    row.Ms = r.ElapsedMs;
                    row.Branching = r.AverageBranching;
                    if (r.IsSolved) row.Note += "; solver " + r.Length + " moves";
                    else row.Note += "; solver " + r.Status + " (stored solution still proves solvable)";
                }
                rows.Add(row);
            }
            return rows;
        }

        public static string ToCsv(List<BuildReportRow> rows)
        {
            var sb = new System.Text.StringBuilder();
            sb.Append("level,tier,seed,colors,bottles,capacity,solution_length,nodes,ms,branching,stuck_rate,handcrafted,passed,note\n");
            foreach (var r in rows)
            {
                sb.Append(r.Number).Append(',').Append(r.Tier).Append(',').Append(r.Seed).Append(',').Append(r.Colors).Append(',')
                  .Append(r.Bottles).Append(',').Append(r.Capacity).Append(',').Append(r.SolutionLength).Append(',').Append(r.Nodes).Append(',')
                  .Append(r.Ms).Append(',').Append(r.Branching.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)).Append(',')
                  .Append(r.StuckRate.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)).Append(',')
                  .Append(r.Handcrafted ? "yes" : "no").Append(',').Append(r.Passed ? "PASS" : "FAIL").Append(',')
                  .Append('"').Append((r.Note ?? "").Replace("\"", "'")).Append('"').Append('\n');
            }
            return sb.ToString();
        }
    }
}
