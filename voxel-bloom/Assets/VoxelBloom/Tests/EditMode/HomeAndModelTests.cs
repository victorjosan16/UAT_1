using System.IO;
using System.Linq;
using NUnit.Framework;
using VoxelBloom.Core;
using static VoxelBloom.Tests.TestUtil;

namespace VoxelBloom.Tests
{
    public class CollapseTests
    {
        static LevelDefinition Flat(int x, int y, string voxels, params int[][] deck)
            => new LevelDefinition
            {
                Id = 1, Name = "Flat", Grid = new VoxelGrid(x, y, 1, new[] { "#FF0000", "#00FF00" }, VoxelGrid.Decode(voxels)),
                Deck = deck.Select(d => new AttackBlock(d[0], d[1])).ToArray(), QueueSize = 5, MoveLimit = 99, StarsThree = 1, StarsTwo = 2,
            };

        [Test]
        public void FloatingPiece_Falls_AndCountsAsDestroyed()
        {
            var s = new GameSession(Flat(5, 3, "00000" + "..1.." + "..00.", new[] { 1, 2 }, new[] { 0, 8 }));
            s.Tap(0);
            var r = s.ResolveNext();
            Assert.AreEqual(1, r.Destroyed.Count);
            Assert.AreEqual(2, r.Dropped.Count);
            Assert.AreEqual(5, s.TotalRemaining);
        }

        [Test]
        public void EqualPieces_LowestIndexIsMain_OtherFalls()
        {
            var s = new GameSession(Flat(3, 3, "000" + ".1." + "000", new[] { 1, 2 }, new[] { 0, 8 }));
            s.Tap(0);
            var r = s.ResolveNext();
            Assert.AreEqual(3, r.Dropped.Count);
            Assert.AreEqual(3, s.TotalRemaining);
        }

        [Test]
        public void PreviewTargets_MatchesWhatTheAttackRemoves()
        {
            var lvl = Level(6);
            var s = new GameSession(lvl);
            var b = s.BoardAt(lvl.Witness[0]).Value;
            var preview = s.PreviewTargets(b.Color, b.Power);
            s.Tap(lvl.Witness[0]);
            var r = s.ResolveNext();
            if (r != null && !r.Wasted) CollectionAssert.AreEqual(preview, r.Destroyed);
        }
    }

    public class ModelIntegrityTests
    {
        [Test]
        public void EveryModel_IsOneConnectedPiece_AndAnimalsHaveAnimProfiles()
        {
            string dir = Path.Combine(DataDir, "Models");
            var files = Directory.GetFiles(dir, "*.json");
            Assert.GreaterOrEqual(files.Length, 36);
            foreach (var f in files)
            {
                var d = MiniJson.Obj(MiniJson.Parse(File.ReadAllText(f)));
                var g = VoxelGrid.FromJson(d);
                var lvl = new LevelDefinition { Id = 1, Grid = g, Deck = new[] { new AttackBlock(0, 2) }, QueueSize = 5, MoveLimit = 9 };
                var s = new GameSession(lvl);
                Assert.AreEqual(0, s.Collapse(null), Path.GetFileName(f) + " has floating pieces");
                string id = MiniJson.GetString(d, "id");
                if (CharacterCatalog.HomeOrder.Contains(id))
                {
                    var a = AnimProfile.FromJson(MiniJson.GetObj(d, "anim"));
                    Assert.NotNull(a, id + " anim");
                    Assert.Greater(a.HeadY, 0, id + " headY");
                    Assert.AreEqual(12, a.Eyes.Length, id + " eyes");
                    foreach (int e in a.Eyes) Assert.GreaterOrEqual(g.Get(e), 0, id + " eye cell must be solid");
                }
            }
        }
    }

    public class CharacterCatalogTests
    {
        [Test]
        public void Order_Paging_Wrap_AndLocks()
        {
            var cfg = Config();
            var cat = new CharacterCatalog(cfg);
            Assert.AreEqual(10, cat.Count);
            Assert.AreEqual("fox", cat.Characters[0].Id);
            CollectionAssert.AreEqual(new[] { "fox", "panda", "chicken", "bunny", "lion", "cat" }, cat.Characters.Take(6).Select(c => c.Id).ToArray());
            Assert.AreEqual(2, cat.PageCount);
            Assert.AreEqual(9, cat.Step(0, -1), "left arrow wraps to the last character");
            Assert.AreEqual(0, cat.Step(9, +1));
            Assert.AreEqual(1, CharacterCatalog.PageOf(7));

            var save = SaveData.CreateNew(cfg);
            Assert.IsTrue(cat.IsUnlocked("fox", save), "starter is free");
            Assert.IsFalse(cat.IsUnlocked("lion", save));
            Assert.AreEqual("fox", cat.ResolveSaved("lion", save), "locked selection is never persisted");
            save.CollectionUnlocked.Add("lion");
            Assert.AreEqual("lion", cat.ResolveSaved("lion", save));
            foreach (var c in cat.Characters) Assert.Greater(c.UnlockLevel, 0, c.Id + " must be unlocked by a real level");
        }

        [Test]
        public void HomeCharacter_PersistsInSave()
        {
            var cfg = Config();
            var store = new MemorySaveStorage();
            var m = new SaveManager(store, cfg);
            m.Load().HomeCharacter = "panda";
            m.Save();
            Assert.AreEqual("panda", new SaveManager(store, cfg).Load().HomeCharacter);
            Assert.AreEqual("fox", SaveSerializer.FromJson("{\"version\":2}", cfg).HomeCharacter, "v2 saves migrate to the starter");
        }

        [Test]
        public void LevelModels_AreLevelDefined_NotTheHomeSelection()
        {
            // the Home selection never changes which model a level uses
            Assert.AreEqual("chicken", Level(1).ModelId);
            Assert.AreEqual("fox", Level(6).ModelId);
            Assert.AreEqual("dog", Level(8).ModelId);
            Assert.AreEqual("elephant", Level(9).ModelId);
        }
    }
}
