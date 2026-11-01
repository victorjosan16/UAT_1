using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using VoxelBloom.Core;
using static VoxelBloom.Tests.TestUtil;

namespace VoxelBloom.Tests
{
    /// <summary>Rules v2 (4 columns, 4 launchers). Same cases as tools/test-rules.mjs.</summary>
    public class GameplayRulesTests
    {
        static readonly int[][] Empty = new int[0][];

        [Test]
        public void OnlyTopBlockActive_TapMovesItIntoFirstFreeLauncher()
        {
            var s = new GameSession(Cube(Col(0, 5, 0, 21), Col(1, 1), Empty, Empty));
            Assert.AreEqual(0, s.Tap(0));
            Assert.AreEqual(5, s.LauncherAt(0).Value.Power);
            Assert.AreEqual(1, s.ColumnLength(0));
            Assert.AreEqual(21, s.Top(0).Value.Power);
            Assert.IsFalse(s.CanTap(2), "empty column");
            Assert.AreEqual(1, s.Moves);
        }

        [Test]
        public void Launcher_FiresOneVoxelOfItsColourPerRound_AndFreesWhenEmpty()
        {
            var s = new GameSession(Cube(Col(0, 3), Col(0, 23, 1, 1), Empty, Empty));
            s.Tap(0);
            for (int k = 0; k < 3; k++)
            {
                var r = s.Step();
                Assert.AreEqual(1, r.Shots.Count);
                Assert.AreEqual(-1, s.CellColor(r.Shots[0].voxel));
            }
            Assert.IsFalse(s.LauncherAt(0).HasValue);
            Assert.AreEqual(23, s.Remaining(0));
            Assert.AreEqual(1, s.Remaining(1), "never other colours");
        }

        [Test]
        public void Launcher_WaitsWhileColourHidden_ThenFires()
        {
            var s = new GameSession(Cube(Col(1, 1), Col(0, 26), Empty, Empty));
            s.Tap(0);
            Assert.IsNull(s.Step(), "centre is not visible: launcher waits");
            Assert.IsTrue(s.IsWaiting(0));
            s.Tap(1);
            s.ResolveAll();
            Assert.IsTrue(s.IsWon);
        }

        [Test]
        public void ParallelLaunchers_FireInTheSameRound_ComboScore()
        {
            var s = new GameSession(Cube(Col(0, 13), Col(0, 13), Col(1, 1), Empty));
            s.Tap(0); s.Tap(1);
            var r = s.Step();
            Assert.AreEqual(2, r.Combo);
            Assert.AreEqual(2 * 10 * 2, s.Score);
        }

        [Test]
        public void Jammed_WhenAllFourLaunchersWait()
        {
            var s = new GameSession(Cube(Col(1, 1), Col(1, 1), Col(1, 1), Col(1, 1, 0, 26)));
            for (int c = 0; c < 4; c++) s.Tap(c);
            s.Settle();
            Assert.AreEqual(FailReason.Jammed, s.GetFailReason());
            Assert.IsFalse(s.CanTap(3));
            Assert.IsTrue(s.ApplyEliminate(0), "Eliminare frees a launcher");
            Assert.IsTrue(s.CanTap(3));
        }

        [Test]
        public void FinishedColour_FreesLauncher_AndLeavesColumns()
        {
            var s = new GameSession(Cube(Col(0, 30), Col(0, 5), Col(1, 1), Empty));
            s.Tap(0);
            s.ResolveAll();
            Assert.AreEqual(0, s.Remaining(0));
            Assert.AreEqual(0, s.ColumnLength(1), "colour-0 block purged");
            s.Tap(2);
            s.ResolveAll();
            Assert.IsTrue(s.IsWon);
        }

        [Test]
        public void Collapse_FloatingPieceFalls()
        {
            var s = new GameSession(Shape(5, 3, 1, "00000" + "..1.." + "..00.", new[] { Col(1, 1), Col(0, 7), Empty, Empty }));
            s.Tap(0);
            var r = s.Step();
            Assert.AreEqual(1, r.Shots.Count);
            Assert.AreEqual(2, r.Dropped.Count);
            Assert.AreEqual(5, s.TotalRemaining);
        }

        [Test]
        public void OutOfBlocks_WhenColumnsEmptyAndNothingCanFire()
        {
            var s = new GameSession(Cube(Col(0, 26), Empty, Empty, Empty));
            s.Tap(0);
            s.ResolveAll();
            Assert.AreEqual(FailReason.OutOfBlocks, s.GetFailReason());
        }

        [Test]
        public void Boosters_Hammer_ColorBomb_Shuffle()
        {
            var s = new GameSession(Cube(Col(1, 1), Col(0, 26), Col(0, 1), Empty));
            Assert.AreEqual(5, s.ApplyHammer(Idx(0, 0, 0), 5).Count);
            Assert.AreEqual(21, s.ApplyColorBomb(0, 100).Count, "colour bomb removes the whole visible colour");
            Assert.AreEqual(1, s.Remaining(1), "never another colour");
            Assert.IsTrue(s.ApplyShuffle(new DeterministicRandom(3)));
            Assert.AreEqual(1, s.BlocksLeft, "finished colour blocks were purged, the rest kept");
        }

        [Test]
        public void Snapshot_RoundTrip_ContinuesIdentically()
        {
            var lvl = Level(12);
            var a = new GameSession(lvl);
            for (int k = 0; k < 3; k++) { a.Settle(); a.Tap(lvl.Witness[k]); }
            var b = GameSession.FromSnapshot(lvl, MiniJson.Obj(MiniJson.Parse(MiniJson.Serialize(a.ToSnapshot()))));
            Assert.NotNull(b);
            for (int k = 3; k < lvl.Witness.Length; k++) { a.Settle(); a.Tap(lvl.Witness[k]); b.Settle(); b.Tap(lvl.Witness[k]); }
            a.ResolveAll(); b.ResolveAll();
            Assert.IsTrue(a.IsWon && b.IsWon);
            Assert.AreEqual(a.Score, b.Score);
        }

        [Test]
        public void DeterministicRandom_MatchesJavaScriptMulberry32()
        {
            var r = new DeterministicRandom(1);
            Assert.AreEqual(0.6270739405881613, r.NextDouble(), 1e-12);
            Assert.AreEqual(0.002735721180215478, r.NextDouble(), 1e-12);
        }
    }

    public class LevelDataTests
    {
        static int LevelCount => Config().TotalLevels;

        [Test]
        public void AllLevels_Load_Validate_AreUniqueObjects_WithSixToTenColours()
        {
            Assert.GreaterOrEqual(LevelCount, 100);
            var models = new HashSet<string>();
            for (int id = 1; id <= LevelCount; id++)
            {
                var l = Level(id);
                Assert.AreEqual(id, l.Id);
                Assert.AreEqual(id % 5 == 0, l.Hard, "every 5th level is HARD");
                Assert.GreaterOrEqual(l.Grid.PaletteHex.Length, 6, "level " + id + " colours");
                Assert.IsTrue(models.Add(l.ModelId), "unique object per level");
                Assert.AreEqual(4, l.Columns.Length);
            }
        }

        [Test]
        public void Witness_ReplaysToWin_WithIdenticalScore_AllLevels()
        {
            // Cross-validates the C# rules against the JavaScript generator (tools/rules.mjs).
            for (int id = 1; id <= LevelCount; id++)
            {
                var l = Level(id);
                var s = GreedySolver.Replay(l, l.Witness);
                Assert.NotNull(s, "level " + id + " witness tap invalid");
                Assert.IsTrue(s.IsWon, "level " + id);
                Assert.AreEqual(l.WitnessScore, s.Score, "level " + id + " score");
                Assert.AreEqual(l.WitnessMoves, s.Moves, "level " + id + " moves");
            }
        }

        [Test]
        public void GreedySolver_FindsTheSameWitness()
        {
            for (int id = 1; id <= LevelCount; id += 7)
            {
                var l = Level(id);
                var r = GreedySolver.Solve(l);
                Assert.IsTrue(r.Won, "level " + id);
                CollectionAssert.AreEqual(l.Witness, r.Taps, "level " + id);
            }
        }

        [Test]
        public void Config_ChaptersCollectionBoostersHardRule()
        {
            var c = Config();
            Assert.AreEqual(6, c.Chapters.Count);
            Assert.AreEqual(120, c.TotalLevels);
            Assert.AreEqual("Dream Builds", c.Chapters[0].Name);
            Assert.AreEqual(300, c.Booster(BoosterType.Hammer).Price);
            Assert.AreEqual(200, c.Booster(BoosterType.Shuffle).Price);
            Assert.AreEqual(500, c.Booster(BoosterType.ColorBomb).Price);
            Assert.AreEqual(5, c.HardEvery);
            CollectionAssert.AreEquivalent(c.Chapters.Select(ch => ch.Name).ToList(), c.Collection.Select(e => e.Category).Distinct().ToList());
        }

        [Test]
        public void MiniJson_RoundTrip()
        {
            var o = new Dictionary<string, object> { { "a", 1.5 }, { "b", "x\"y" }, { "c", new List<object> { true, null, 3.0 } } };
            var back = MiniJson.Obj(MiniJson.Parse(MiniJson.Serialize(o)));
            Assert.AreEqual(1.5, back["a"]);
            Assert.AreEqual("x\"y", back["b"]);
            Assert.Throws<System.FormatException>(() => MiniJson.Parse("{\"a\":}"));
        }
    }
}
