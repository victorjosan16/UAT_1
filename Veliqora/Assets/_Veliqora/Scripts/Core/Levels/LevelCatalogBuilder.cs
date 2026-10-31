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
        // special mechanics
        public CapacityMode CapMode = CapacityMode.Uniform;
        public int Mystery;          // mystery depth (0 = none)
        public int Locks;            // corked bottles
        public int RewardBottles;    // ad-locked helper bottles
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
        private static CurveSegment Seg(int a, int b, int colors, int minSol, int cand, int empties = 2, int cap = 4, int pairs = 3, CurveSegment alt = null,
            CapacityMode caps = CapacityMode.Uniform, int mystery = 0, int locks = 0, int reward = 0) =>
            new CurveSegment
            {
                FirstLevel = a, LastLevel = b, Colors = colors, MinSolution = minSol, Candidates = cand, EmptyBottles = empties, Capacity = cap,
                MaxAdjacentPairs = pairs, AlternateOnEven = alt, CapMode = caps, Mystery = mystery, Locks = locks, RewardBottles = reward,
            };

        // Curve follows the design brief:
        //   11–30 more colors · 31–70 reduced empty space · 71–120 mystery layers, size variations and corked bottles
        //   (plus optional ad bottles) · 121–200 combinations · 201–300 Master combinations with 9–12 colors.
        // StagePlanner adds random Hard levels and stage-finale Boss levels on top of this curve.
        public static readonly CurveSegment[] DefaultCurve =
        {
            Seg(11, 20, 4, 7, 2),
            Seg(21, 30, 5, 9, 3),
            Seg(31, 45, 5, 10, 3, alt: Seg(31, 45, 4, 8, 3, empties: 1)),
            Seg(46, 60, 6, 12, 3, alt: Seg(46, 60, 5, 10, 3, empties: 1)),
            Seg(61, 70, 7, 14, 3, alt: Seg(61, 70, 5, 11, 3, empties: 1)),
            Seg(71, 85, 6, 12, 3, mystery: 2, alt: Seg(71, 85, 6, 12, 3, mystery: 3)),
            Seg(86, 100, 6, 12, 3, caps: CapacityMode.SmallLarge, alt: Seg(86, 100, 6, 12, 3, caps: CapacityMode.SmallLarge, mystery: 2)),
            Seg(101, 110, 6, 12, 3, locks: 1, alt: Seg(101, 110, 6, 12, 3, locks: 1, mystery: 2)),
            Seg(111, 120, 7, 14, 3, locks: 1, reward: 1, alt: Seg(111, 120, 7, 14, 3, caps: CapacityMode.SmallLarge, locks: 1)),
            Seg(121, 150, 7, 16, 3, caps: CapacityMode.SmallLarge, mystery: 2, locks: 1, alt: Seg(121, 150, 7, 16, 3, mystery: 3, locks: 2)),
            Seg(151, 175, 8, 18, 3, mystery: 3, locks: 1, reward: 1, alt: Seg(151, 175, 7, 16, 3, caps: CapacityMode.Wide, locks: 2)),
            Seg(176, 200, 8, 20, 3, caps: CapacityMode.SmallLarge, mystery: 3, locks: 2, alt: Seg(176, 200, 8, 20, 3, caps: CapacityMode.Wide, mystery: 2, reward: 1)),
            Seg(201, 225, 9, 24, 3, mystery: 2, alt: Seg(201, 225, 9, 24, 3, locks: 2)),
            Seg(226, 250, 10, 26, 3, caps: CapacityMode.SmallLarge, locks: 1, alt: Seg(226, 250, 10, 26, 3, mystery: 3)),
            Seg(251, 275, 11, 30, 3, mystery: 3, locks: 1, alt: Seg(251, 275, 11, 30, 3, empties: 3, caps: CapacityMode.Wide)),
            Seg(276, 300, 12, 34, 3, caps: CapacityMode.SmallLarge, mystery: 2, locks: 2, reward: 1, alt: Seg(276, 300, 12, 34, 3, mystery: 3, locks: 1)),
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
                else def = BuildGenerated(n, baseSeed, FindSegment(curve, n).ForLevel(n), report, StagePlanner.Plan(baseSeed, n));
                if (def == null) throw new InvalidOperationException("Failed to build level " + n + " — see report");
                // Contextual tip the first time a new constraint / mechanic appears.
                if (def.TutorialKey == null && def.HasMystery && !catalog.Levels.Exists(l => l.HasMystery)) def.TutorialKey = "mystery_intro";
                if (def.TutorialKey == null && def.HasMixedCapacities && !catalog.Levels.Exists(l => l.HasMixedCapacities)) def.TutorialKey = "sizes_intro";
                if (def.TutorialKey == null && def.HasLocks && !catalog.Levels.Exists(l => l.HasLocks)) def.TutorialKey = "cork_intro";
                if (def.TutorialKey == null && def.RewardBottles.Length > 0 && !catalog.Levels.Exists(l => l.RewardBottles.Length > 0)) def.TutorialKey = "reward_bottle_intro";
                if (def.TutorialKey == null && def.Objectives.Count > 0 && !catalog.Levels.Exists(l => l.Objectives.Count > 0)) def.TutorialKey = "objectives_intro";
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
                Difficulty = Math.Max(1, Math.Min(3, 1 + solve.Length / 6)),
                RewardCoins = 20,
                DisabledBoosters = n <= 4 ? new List<string> { "Shuffle", "RemoveColor" } : new List<string>(),
            };
        }

        public static LevelDefinition BuildGenerated(int n, ulong baseSeed, CurveSegment seg, List<BuildReportRow> report)
            => BuildGenerated(n, baseSeed, seg, report, new LevelPlan { Number = n, Stage = StagePlanner.StageOf(n), PickQuantile = 0.5 });

        public static LevelDefinition BuildGenerated(int n, ulong baseSeed, CurveSegment seg, List<BuildReportRow> report, LevelPlan plan)
        {
            // Hard/Boss levels add colors; if that layout family is (near) unsolvable within budget, step the bonus down.
            List<GeneratedLevel> candidates = null;
            GenerationSpec spec = null;
            int candidateCount = plan.Kind == LevelKind.Boss ? 8 : (plan.Kind == LevelKind.Hard ? 6 : seg.Candidates);
            for (int bonus = plan.HardBonusColors; bonus >= 0; bonus--)
            {
                var p = plan;
                p.HardBonusColors = bonus;
                int colors = StagePlanner.ColorsFor(p, seg.Colors, n);
                spec = new GenerationSpec
                {
                    Colors = colors,
                    EmptyBottles = seg.EmptyBottles,
                    Capacity = seg.Capacity,
                    MinSolutionLength = Math.Max(6, seg.MinSolution + 2 * (colors - seg.Colors)),
                    MaxAdjacentPairs = seg.MaxAdjacentPairs,
                    Solver = SolverOptions.Generation,
                    CapacityMode = seg.CapMode,
                    MysteryDepth = seg.Mystery,
                    Locks = seg.Locks,
                    RewardBottles = seg.RewardBottles,
                    MaxAttempts = seg.Locks > 0 || seg.Mystery > 0 ? 250 : 400,
                };
                candidates = new List<GeneratedLevel>();
                for (int c = 0; c < candidateCount; c++)
                {
                    var g = LevelGenerator.Generate(spec, DeterministicRandom.Combine(baseSeed, n, c + 100 * bonus));
                    if (g == null) continue;
                    g.StuckRate = DifficultyAnalyzer.StuckRate(g.State, DeterministicRandom.Combine(g.Seed, 1));
                    candidates.Add(g);
                }
                if (candidates.Count > 0) break;
            }
            if (candidates == null || candidates.Count == 0)
            {
                report?.Add(new BuildReportRow { Number = n, Tier = DifficultyTiers.ForLevel(n).ToString(), Colors = seg.Colors, Passed = false, Note = "no verified candidate within budget" });
                return null;
            }

            // Pick by difficulty: Hard/Boss take the hardest verified candidate; normal levels a random easy-to-medium one.
            candidates.Sort((a, b) => a.DifficultyScore.CompareTo(b.DifficultyScore));
            int pick = (int)Math.Round(plan.PickQuantile * (candidates.Count - 1));
            var chosen = candidates[Math.Max(0, Math.Min(candidates.Count - 1, pick))];

            // Tighter benchmark for stars: re-solve the chosen level with plain A* (weight 1). Keep whichever
            // verified solution is shorter. Still reported as a benchmark, not as proven optimal.
            var start = chosen.State;
            var tight = PuzzleSolver.Solve(start, new SolverOptions { MaxNodes = 300_000, TimeoutMs = 5_000, HeuristicWeight = 1.0 });
            if (tight.Status == SolveStatus.Solved && tight.Length < chosen.Solve.Length && PuzzleSolver.VerifySolution(start, tight.Moves, out _))
                chosen.Solve = tight;

            var row = Row(n, chosen.Seed, chosen.Bottles, seg.Capacity, chosen.Solve, false, true, plan.Kind.ToString().ToLowerInvariant() + " stage=" + plan.Stage + " candidates=" + candidates.Count);
            row.StuckRate = chosen.StuckRate;
            report?.Add(row);
            int attempts = 0;
            foreach (var cnd in candidates) attempts += cnd.Attempts;
            var generated = new LevelDefinition
            {
                Number = n,
                Seed = chosen.Seed,
                Capacity = seg.Capacity,
                ColorCount = spec.Colors,
                Bottles = chosen.Bottles,
                Kind = plan.Kind,
                Tier = DifficultyTiers.ForLevel(n),
                Handcrafted = false,
                ParMoves = chosen.Solve.Length,
                Solution = LevelDefinition.EncodeMoves(chosen.Solve.Moves),
                Validation = ToValidation(chosen.Solve, attempts, chosen.StuckRate),
            };
            generated.ApplyLayout(chosen.State);
            Decorate(generated, baseSeed, chosen);
            return generated;
        }

        /// <summary>Objectives, per-level rewards, booster restrictions and a 1–10 difficulty rating.</summary>
        public static void Decorate(LevelDefinition def, ulong baseSeed, GeneratedLevel chosen)
        {
            var rng = new DeterministicRandom(DeterministicRandom.Combine(baseSeed, def.Number, 0x0B1E));
            var start = def.CreateInitialState();
            int filled = 0;
            var colors = new List<int>();
            for (int i = 0; i < start.BottleCount; i++)
            {
                if (start[i].IsEmpty) continue;
                filled++;
                for (int u = 0; u < start[i].Count; u++) if (!colors.Contains(start[i][u])) colors.Add(start[i][u]);
            }
            colors.Sort();

            if (def.Number >= 31)
            {
                def.Objectives.Add(new Objectives.ObjectiveDef { Id = "bottles", Type = Objectives.ObjectiveType.CompleteBottles, Target = Math.Min(colors.Count, 2 + def.Number % 3), RewardCoins = 10 });
                def.Objectives.Add(new Objectives.ObjectiveDef { Id = "color", Type = Objectives.ObjectiveType.CollectColor, Color = colors[rng.Range(0, colors.Count)], Target = 1, RewardCoins = 10 });
                if (def.HasLocks)
                    def.Objectives.Add(new Objectives.ObjectiveDef { Id = "corks", Type = Objectives.ObjectiveType.UnlockBottles, Target = def.Locks.Count, RewardCoins = 15, RequiresId = "bottles" });
                else if (def.HasMystery)
                    def.Objectives.Add(new Objectives.ObjectiveDef { Id = "mystery", Type = Objectives.ObjectiveType.RevealMystery, Target = start.HiddenTotal(), RewardCoins = 15 });
                if (def.Kind == LevelKind.Boss)
                    def.Objectives.Add(new Objectives.ObjectiveDef { Id = "bonus", Type = Objectives.ObjectiveType.FinishLevel, Target = 1, RewardCoins = 40, Rewarded = true });
            }

            int features = (def.HasMystery ? 1 : 0) + (def.HasLocks ? 1 : 0) + (def.HasMixedCapacities ? 1 : 0);
            double score = 1 + def.ParMoves / 6.0 + chosen.StuckRate * 2 + features * 0.7 + (def.Kind == LevelKind.Boss ? 0.8 : 0);
            def.Difficulty = Math.Max(1, Math.Min(10, (int)Math.Round(score)));
            def.RewardCoins = 20 + 2 * (def.Difficulty - 1);
            if (def.Kind == LevelKind.Boss) def.DisabledBoosters.Add("RemoveColor");
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
                bool objectives = true;
                if (replay && l.Objectives.Count > 0)
                {
                    var end = state;
                    foreach (var m in l.SolutionMoves()) end = PuzzleRules.Evaluate(end, m.From, m.To).After;
                    objectives = Objectives.ObjectiveTracker.AllAchievableAtWin(l.Objectives, state, end, out err);
                }
                row.SolutionLength = l.Solution.Length;
                row.Passed = structural && replay && notTrivial && objectives;
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
