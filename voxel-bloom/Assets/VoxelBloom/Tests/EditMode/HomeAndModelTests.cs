using System.IO;
using System.Linq;
using NUnit.Framework;
using VoxelBloom.Core;
using static VoxelBloom.Tests.TestUtil;

namespace VoxelBloom.Tests
{
    public class ModelIntegrityTests
    {
        [Test]
        public void EveryModel_IsOneConnectedPiece_AndAnimalsHaveAnimProfiles()
        {
            string dir = Path.Combine(DataDir, "Models");
            var files = Directory.GetFiles(dir, "*.json");
            Assert.GreaterOrEqual(files.Length, 120, "100 mascots + objects");
            foreach (var f in files)
            {
                var d = MiniJson.Obj(MiniJson.Parse(File.ReadAllText(f)));
                var g = VoxelGrid.FromJson(d);
                var lvl = new LevelDefinition { Id = 1, Grid = g };
                var s = new GameSession(lvl);
                Assert.AreEqual(0, s.Collapse(null), Path.GetFileName(f) + " has floating pieces");
                string id = MiniJson.GetString(d, "id");
                var a = AnimProfile.FromJson(MiniJson.GetObj(d, "anim"));
                if (a != null)
                {
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
            Assert.AreEqual(120, cat.Count, "every level object is in the carousel");
            Assert.AreEqual("fox", cat.Characters[0].Id, "starter leads");
            CollectionAssert.AreEqual(new[] { "fox", "castle", "house", "car", "cake", "rocket" }, cat.Characters.Take(6).Select(c => c.Id).ToArray(), "then level order");
            Assert.AreEqual(20, cat.PageCount);
            Assert.AreEqual(119, cat.Step(0, -1), "left arrow wraps to the last character");
            Assert.AreEqual(0, cat.Step(119, +1));
            Assert.AreEqual(1, CharacterCatalog.PageOf(7));

            var save = SaveData.CreateNew(cfg);
            Assert.IsTrue(cat.IsUnlocked("fox", save), "starter is free");
            Assert.IsFalse(cat.IsUnlocked("lion", save));
            Assert.AreEqual(120, cat.Characters.Select(c => c.UnlockLevel).Distinct().Count(), "one object per level");
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
            Assert.AreEqual("castle", Level(1).ModelId, "chapter 1 = the objects of the reference image");
            Assert.AreEqual("pyramid", Level(20).ModelId);
            Assert.AreEqual("dragon", Level(16).ModelId);
            Assert.AreEqual("fox", Level(21).ModelId);
        }
    }
}
