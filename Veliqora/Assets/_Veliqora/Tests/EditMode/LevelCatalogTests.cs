using System.Collections.Generic;
using NUnit.Framework;
using Veliqora.Core.Levels;
using Veliqora.Core.Puzzle;
using Veliqora.Core.Solver;

namespace Veliqora.Tests.EditMode
{
    public class LevelCatalogTests
    {
        private static LevelCatalog Catalog => TestUtil.Catalog;

        [Test]
        public void Catalog_Has300ContiguousLevels()
        {
            Assert.AreEqual(300, Catalog.Levels.Count);
            var repo = new LevelRepository(Catalog);
            for (int n = 1; n <= 300; n++) Assert.AreEqual(n, repo.Get(n).Number);
            Assert.IsFalse(repo.Has(0));
            Assert.IsFalse(repo.Has(301));
        }

        [Test]
        public void AllLevels_AreProvablySolvable_ByReplayingStoredSolution()
        {
            var failures = new List<string>();
            foreach (var l in Catalog.Levels)
            {
                var s = l.CreateInitialState();
                if (!PuzzleRules.HasValidColorDistribution(s, out var e1)) failures.Add(l.Id + ": " + e1);
                else if (PuzzleRules.IsWon(s)) failures.Add(l.Id + ": starts solved");
                else if (!PuzzleSolver.VerifySolution(s, l.SolutionMoves(), out var e2)) failures.Add(l.Id + ": " + e2);
                else if (l.Solution.Length != l.ParMoves) failures.Add(l.Id + ": par != stored solution length");
            }
            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        [Test]
        public void AllLevels_AreSolvedIndependently_BySolverWithinBudget()
        {
            var rows = LevelCatalogBuilder.Validate(Catalog, rerunSolver: true, SolverOptions.Validation);
            var failures = rows.FindAll(r => !r.Passed);
            Assert.IsEmpty(failures, string.Join("\n", failures.ConvertAll(f => f.Number + ": " + f.Note)));
            int solverConfirmed = rows.FindAll(r => r.Note.Contains("solver ") && r.Note.Contains(" moves")).Count;
            Assert.AreEqual(Catalog.Levels.Count, solverConfirmed, "independent solver run should confirm every level");
        }

        [Test]
        public void Tiers_Match_AndNoTutorialLevels()
        {
            for (int n = 1; n <= Catalog.Levels.Count; n++) Assert.AreEqual(DifficultyTiers.ForLevel(n), Catalog.Levels[n - 1].Tier);
            Assert.AreEqual(DifficultyTier.Master, Catalog.Levels[250].Tier);
            foreach (var l in Catalog.Levels)
            {
                Assert.IsFalse(l.Handcrafted, l.Id + " must be a generated, verified puzzle (no tutorial levels)");
                Assert.IsTrue(string.IsNullOrEmpty(l.TutorialKey), l.Id + " has a tutorial");
            }
        }

        [Test]
        public void ColorCount_FollowsTheProgression_AndNeverExceedsThePalette()
        {
            foreach (var l in Catalog.Levels)
            {
                StagePlanner.ColorRange(l.Number, out int min, out int max);
                Assert.That(l.ColorCount, Is.InRange(min, max), l.Id);
                Assert.LessOrEqual(l.ColorCount, StagePlanner.MaxColors, l.Id);
                Assert.AreEqual(l.ColorCount, LevelCatalogBuilder.DistinctColors(l.CreateInitialState()), l.Id);
            }
        }

        [Test]
        public void Difficulty_RampsGradually_ByTierAverages()
        {
            double prev = 0;
            foreach (DifficultyTier tier in System.Enum.GetValues(typeof(DifficultyTier)))
            {
                var levels = Catalog.Levels.FindAll(l => l.Tier == tier);
                double avg = 0;
                foreach (var l in levels) avg += l.ParMoves;
                avg /= levels.Count;
                Assert.Greater(avg, prev, "average solution length should increase with tier " + tier);
                prev = avg;
            }
        }

        [Test]
        public void NoGeneratedLevel_IsTrivial()
        {
            foreach (var l in Catalog.Levels)
            {
                Assert.GreaterOrEqual(l.ParMoves, 6, l.Id);
                foreach (var b in l.Bottles)
                    if (b.Length == l.Capacity) Assert.IsFalse(new BottleState(l.Capacity, b).IsMonochrome, l.Id + " starts with a completed bottle");
            }
        }

        [Test]
        public void StoredCatalog_IsReproducibleFromSeed()
        {
            // Rebuild the first 40 levels from the recorded base seed and compare layouts exactly.
            var rebuilt = LevelCatalogBuilder.Build(Catalog.BaseSeed, 40, null);
            for (int i = 0; i < 40; i++)
            {
                var a = Catalog.Levels[i].CreateInitialState().ExactKey();
                var b = rebuilt.Levels[i].CreateInitialState().ExactKey();
                Assert.AreEqual(a, b, "Level " + (i + 1) + " is not reproducible from its seed");
            }
        }

        [Test]
        public void JsonRoundTrip_PreservesLevels()
        {
            var again = LevelCatalog.FromJson(Catalog.ToJson());
            Assert.AreEqual(Catalog.Levels.Count, again.Levels.Count);
            for (int i = 0; i < again.Levels.Count; i++)
            {
                Assert.AreEqual(Catalog.Levels[i].Seed, again.Levels[i].Seed);
                Assert.AreEqual(Catalog.Levels[i].CreateInitialState().ExactKey(), again.Levels[i].CreateInitialState().ExactKey());
            }
        }

        [Test]
        public void StarRating_IsTransparentAndForgiving()
        {
            Assert.AreEqual(3, StarRating.Evaluate(20, 20));
            Assert.AreEqual(3, StarRating.Evaluate(StarRating.ThreeStarLimit(20), 20));
            Assert.AreEqual(2, StarRating.Evaluate(StarRating.ThreeStarLimit(20) + 1, 20));
            Assert.AreEqual(2, StarRating.Evaluate(StarRating.TwoStarLimit(20), 20));
            Assert.AreEqual(1, StarRating.Evaluate(StarRating.TwoStarLimit(20) + 1, 20));
            Assert.AreEqual(3, StarRating.Evaluate(1, 1), "beating par is always 3 stars");
            Assert.AreEqual(3, StarRating.Evaluate(3, 1), "small levels get at least +2 slack");
        }
    }
}
