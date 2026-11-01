using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using VoxelBloom.Core;
using static VoxelBloom.Tests.TestUtil;

namespace VoxelBloom.Tests
{
    public class GameplayRulesTests
    {
        [Test]
        public void Exposure_CentreHidden_SurfaceExposed_RecomputedAfterDestruction()
        {
            var s = new GameSession(Cube(new[] { new[] { 0, 32 }, new[] { 1, 2 } }));
            Assert.IsFalse(s.IsExposed(Idx(1, 1, 1)));
            Assert.IsTrue(s.IsExposed(Idx(0, 0, 0)));
            Assert.AreEqual(26, s.ExposedCount(0));
            Assert.AreEqual(0, s.ExposedCount(1));
            Assert.IsTrue(VoxelTopology.IsExposed(s.CellsSnapshot(), 3, 3, 3, Idx(1, 1, 0)));
            var hammer = s.ApplyHammer(Idx(1, 1, 0), 1);
            CollectionAssert.AreEqual(new[] { Idx(1, 1, 0) }, hammer);
            Assert.IsTrue(s.IsExposed(Idx(1, 1, 1)), "centre exposed after its neighbour was destroyed");
            Assert.AreEqual(1, s.ExposedCount(1));
        }

        [Test]
        public void Attack_DestroysUpToPower_OnlyMatchingColour()
        {
            var s = new GameSession(Cube(new[] { new[] { 0, 4 }, new[] { 1, 2 }, new[] { 0, 32 } }));
            Assert.IsTrue(s.Tap(0));
            var r = s.ResolveNext();
            Assert.AreEqual(4, r.Destroyed.Count);
            Assert.IsTrue(r.Destroyed.All(i => i != Idx(1, 1, 1)));
            Assert.AreEqual(22, s.Remaining(0));
            Assert.AreEqual(1, s.Remaining(1));
            Assert.AreEqual(1, s.Moves);
        }

        [Test]
        public void Attack_FewerEligible_RemovesOnlyThose_AndConsumesAttack()
        {
            var s = new GameSession(Cube(new[] { new[] { 1, 8 }, new[] { 0, 64 } }));
            s.Tap(1); s.ResolveAll();
            Assert.AreEqual(0, s.Remaining(0));
            s.Tap(0);
            var rs = s.ResolveAll();
            Assert.AreEqual(1, rs.Count);
            Assert.AreEqual(1, rs[0].Destroyed.Count);
            Assert.AreEqual(0, s.Queue.Count);
            Assert.AreEqual(0, s.Combo, "partial attack breaks the combo");
            Assert.IsTrue(s.IsWon);
        }

        [Test]
        public void Attack_WaitsInQueue_WhileColourHidden()
        {
            var s = new GameSession(Cube(new[] { new[] { 1, 2 }, new[] { 0, 32 } }));
            s.Tap(0);
            Assert.AreEqual(0, s.ResolveAll().Count);
            Assert.AreEqual(1, s.Queue.Count);
            Assert.AreEqual(0, s.FirstStuckQueueIndex());
            s.Tap(1);
            Assert.AreEqual(2, s.ResolveAll().Count);
            Assert.IsTrue(s.IsWon);
        }

        [Test]
        public void Combo_GrowsOnFullAttacks_ScoreMultiplierCappedAtFive()
        {
            var s = new GameSession(Cube(Repeat(0, 2, 7)));
            int last = 0;
            for (int k = 0; k < 7; k++)
            {
                s.Tap(k); s.ResolveAll();
                Assert.AreEqual(2 * 10 * GameSession.ComboMultiplier(s.Combo), s.Score - last, "step " + k);
                last = s.Score;
            }
            Assert.AreEqual(7, s.Combo);
            Assert.AreEqual(5, GameSession.ComboMultiplier(s.Combo));
        }

        [Test]
        public void Fail_QueueFull()
        {
            var deck = Repeat(1, 2, 5).Concat(new[] { new[] { 0, 32 } }).ToArray();
            var s = new GameSession(Cube(deck));
            for (int k = 0; k < 5; k++) { s.Tap(k); s.ResolveAll(); }
            Assert.AreEqual(FailReason.QueueFull, s.GetFailReason());
            Assert.IsFalse(s.CanTap(5));
        }

        [Test]
        public void Fail_OutOfMoves()
        {
            var s = new GameSession(Cube(new[] { new[] { 0, 2 }, new[] { 0, 2 }, new[] { 0, 32 }, new[] { 1, 2 } }, 5, 2));
            s.Tap(0); s.ResolveAll(); s.Tap(1); s.ResolveAll();
            Assert.AreEqual(FailReason.OutOfMoves, s.GetFailReason());
            Assert.IsFalse(s.Tap(2));
        }

        [Test]
        public void Fail_OutOfBlocks()
        {
            var s = new GameSession(Cube(new[] { new[] { 0, 2 } }));
            s.Tap(0); s.ResolveAll();
            Assert.IsTrue(s.BoardEmpty);
            Assert.AreEqual(FailReason.OutOfBlocks, s.GetFailReason());
        }

        [Test]
        public void Board_RefillsFromDeck_AndPurgesClearedColour()
        {
            var deck = new List<int[]> { new[] { 0, 32 }, new[] { 0, 2 }, new[] { 1, 2 } };
            deck.AddRange(Repeat(1, 2, 17));
            deck.Add(new[] { 1, 4 }); deck.Add(new[] { 0, 2 }); deck.Add(new[] { 1, 8 });
            var s = new GameSession(Cube(deck.ToArray()));
            Assert.AreEqual(20, s.DeckIndex);
            s.Tap(0);
            Assert.AreEqual(new AttackBlock(1, 4), s.BoardAt(0).Value);
            var rs = s.ResolveAll();
            Assert.IsTrue(rs[0].ColorCleared);
            for (int i = 0; i < GameSession.BoardSize; i++) Assert.IsTrue(!s.BoardAt(i).HasValue || s.BoardAt(i).Value.Color == 1);
        }

        [Test]
        public void FinishedColourAttack_IsWasted()
        {
            var s = new GameSession(Cube(new[] { new[] { 0, 32 }, new[] { 0, 2 }, new[] { 1, 2 } }));
            s.Tap(0); s.Tap(1);
            var rs = s.ResolveAll();
            Assert.AreEqual(2, rs.Count);
            Assert.AreEqual(26, rs[0].Destroyed.Count);
            Assert.IsTrue(rs[1].Wasted);
            Assert.AreEqual(0, s.Combo);
        }

        [Test]
        public void Boosters_Hammer_ColorBomb_Shuffle_Eliminate()
        {
            var s = new GameSession(Cube(new[] { new[] { 1, 2 }, new[] { 1, 2 }, new[] { 0, 32 } }));
            var h = s.ApplyHammer(Idx(0, 0, 0), 5);
            Assert.AreEqual(5, h.Count);
            Assert.AreEqual(21, s.Remaining(0));

            var bomb = s.ApplyColorBomb(0, 100);
            Assert.AreEqual(21, bomb.Count, "colour bomb peels the whole colour");
            Assert.AreEqual(1, s.Remaining(1), "never touches another colour");

            s.Tap(0); s.Tap(1);
            int before = s.Queue.Count;
            Assert.IsTrue(s.ApplyEliminate(0));
            Assert.AreEqual(before - 1, s.Queue.Count);
            Assert.IsFalse(s.ApplyEliminate(5));
        }

        [Test]
        public void Shuffle_PreservesBlocks_AndKeepsPlayableMove()
        {
            var lvl = Level(30);
            var s = new GameSession(lvl);
            var before = new List<AttackBlock>();
            for (int i = 0; i < GameSession.BoardSize; i++) if (s.BoardAt(i).HasValue) before.Add(s.BoardAt(i).Value);
            int deckLeft = s.DeckRemaining;
            Assert.IsTrue(s.ApplyShuffle(new DeterministicRandom(7)));
            var after = new List<AttackBlock>();
            for (int i = 0; i < GameSession.BoardSize; i++) if (s.BoardAt(i).HasValue) after.Add(s.BoardAt(i).Value);
            Assert.AreEqual(before.Count, after.Count);
            Assert.AreEqual(deckLeft, s.DeckRemaining);
            Assert.IsTrue(s.HasPlayableBlock());
        }

        [Test]
        public void Snapshot_RoundTrip_ContinuesIdentically()
        {
            var lvl = Level(12);
            var a = new GameSession(lvl);
            for (int k = 0; k < 5; k++) { a.Tap(lvl.Witness[k]); a.ResolveAll(); }
            var json = MiniJson.Serialize(a.ToSnapshot());
            var b = GameSession.FromSnapshot(lvl, MiniJson.Obj(MiniJson.Parse(json)));
            Assert.NotNull(b);
            for (int k = 5; k < lvl.Witness.Length; k++) { a.Tap(lvl.Witness[k]); a.ResolveAll(); b.Tap(lvl.Witness[k]); b.ResolveAll(); }
            Assert.IsTrue(a.IsWon && b.IsWon);
            Assert.AreEqual(a.Score, b.Score);
            Assert.AreEqual(a.Moves, b.Moves);
        }

        [Test]
        public void DeterministicRandom_MatchesJavaScriptMulberry32()
        {
            // reference values from tools/voxel.mjs rng(1)
            var r = new DeterministicRandom(1);
            Assert.AreEqual(0.6270739405881613, r.NextDouble(), 1e-12);
            Assert.AreEqual(0.002735721180215478, r.NextDouble(), 1e-12);
        }
    }

    public class LevelDataTests
    {
        [Test]
        public void AllHundredLevels_Load_Validate_AndAreDistinct()
        {
            var sigs = new HashSet<string>();
            for (int id = 1; id <= 100; id++)
            {
                var l = Level(id);
                Assert.AreEqual(id, l.Id);
                Assert.AreEqual(id % 5 == 0, l.Hard, "every 5th level is HARD");
                Assert.LessOrEqual(l.WitnessMoves, l.MoveLimit);
                Assert.IsTrue(sigs.Add(string.Join(",", l.Deck.Select(d => d.ToString())) + l.VoxelCount + l.Name));
            }
        }

        [Test]
        public void Witness_ReplaysToWin_WithIdenticalMovesAndScore_AllLevels()
        {
            // Cross-validates the C# rules against the JavaScript generator (tools/rules.mjs).
            for (int id = 1; id <= 100; id++)
            {
                var l = Level(id);
                var s = new GameSession(l);
                foreach (int slot in l.Witness) { Assert.IsTrue(s.Tap(slot), "level " + id + " tap " + slot); s.ResolveAll(); }
                Assert.IsTrue(s.IsWon, "level " + id);
                Assert.AreEqual(l.WitnessMoves, s.Moves, "level " + id + " moves");
                Assert.AreEqual(l.WitnessScore, s.Score, "level " + id + " score");
            }
        }

        [Test]
        public void GreedySolver_SolvesAllLevels()
        {
            for (int id = 1; id <= 100; id++)
            {
                var l = Level(id);
                var r = GreedySolver.Solve(l);
                Assert.IsTrue(r.Won, "level " + id);
                Assert.AreEqual(l.WitnessMoves, r.Moves, "same heuristic, same result");
            }
        }

        [Test]
        public void Config_HasChaptersCollectionBoostersAndHardRule()
        {
            var c = Config();
            Assert.AreEqual(5, c.Chapters.Count);
            Assert.AreEqual(100, c.TotalLevels);
            Assert.AreEqual(300, c.Booster(BoosterType.Hammer).Price);
            Assert.AreEqual(200, c.Booster(BoosterType.Shuffle).Price);
            Assert.AreEqual(500, c.Booster(BoosterType.ColorBomb).Price);
            Assert.Greater(c.Booster(BoosterType.Eliminate).Price, 0);
            Assert.AreEqual(5, c.HardEvery);
            Assert.AreEqual(100, c.Collection.Count, "100 mascots");
            CollectionAssert.AreEquivalent(c.Chapters.Select(ch => ch.Name).ToList(), c.Collection.Select(e => e.Category).Distinct().ToList(), "collection tabs = chapters");
        }

        [Test]
        public void MiniJson_RoundTrip()
        {
            var o = new Dictionary<string, object> { { "a", 1.5 }, { "b", "x\"y" }, { "c", new List<object> { true, null, 3.0 } } };
            var back = MiniJson.Obj(MiniJson.Parse(MiniJson.Serialize(o)));
            Assert.AreEqual(1.5, back["a"]);
            Assert.AreEqual("x\"y", back["b"]);
            Assert.AreEqual(3, ((List<object>)back["c"]).Count);
            Assert.Throws<System.FormatException>(() => MiniJson.Parse("{\"a\":}"));
        }
    }
}
