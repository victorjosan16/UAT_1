using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Veliqora.Core.Economy;
using Veliqora.Core.Json;
using Veliqora.Core.Save;
using Veliqora.Core.Session;

namespace Veliqora.Tests.EditMode
{
    public class SaveSystemTests
    {
        private static SaveData Sample()
        {
            var d = new SaveData();
            d.Progress.HighestUnlocked = 12;
            d.Progress.CurrentLevel = 12;
            d.Progress.BestStars[1] = 3;
            d.Progress.BestStars[11] = 2;
            d.Economy.Coins = 1250;
            d.Economy.Inventory[BoosterType.Undo] = 3;
            d.Economy.Inventory[BoosterType.Hint] = 1;
            d.Economy.OwnedCosmetics.Add("theme_valley");
            d.Economy.Ledger.Add("tx-1");
            d.Economy.StartingGrantApplied = true;
            d.Settings.ReducedMotion = true;
            d.Settings.AnimationSpeed = 1.5f;
            d.Cosmetics.Theme = "theme_night";
            d.TutorialsSeen.Add("tap_to_pour");
            var level = TestUtil.Catalog.Levels[11];
            var session = new GameSession(level, "att");
            session.TryMove(level.Solution[0][0], level.Solution[0][1]);
            d.Session = session.ToSnapshot();
            return d;
        }

        [Test]
        public void RoundTrip_PreservesEverything()
        {
            var d = SaveSerializer.Deserialize(SaveSerializer.Serialize(Sample()));
            Assert.AreEqual(12, d.Progress.HighestUnlocked);
            Assert.AreEqual(3, d.Progress.Stars(1));
            Assert.AreEqual(2, d.Progress.Stars(11));
            Assert.AreEqual(1250, d.Economy.Coins);
            Assert.AreEqual(3, d.Economy.Count(BoosterType.Undo));
            Assert.IsTrue(d.Economy.OwnedCosmetics.Contains("theme_valley"));
            Assert.IsTrue(d.Economy.StartingGrantApplied);
            Assert.IsTrue(d.Settings.ReducedMotion);
            Assert.AreEqual(1.5f, d.Settings.AnimationSpeed, 1e-5);
            Assert.AreEqual("theme_night", d.Cosmetics.Theme);
            Assert.IsTrue(d.TutorialsSeen.Contains("tap_to_pour"));
            Assert.IsNotNull(d.Session);
            var restored = GameSession.FromSnapshot(TestUtil.Catalog.Levels[11], d.Session);
            Assert.IsNotNull(restored);
            Assert.AreEqual(1, restored.MoveCount);
        }

        [Test]
        public void Corruption_IsDetectedByChecksum()
        {
            string json = SaveSerializer.Serialize(Sample());
            string tampered = json.Replace("1250", "9999");
            Assert.Throws<SaveFormatException>(() => SaveSerializer.Deserialize(tampered));
            Assert.Throws<SaveFormatException>(() => SaveSerializer.Deserialize(json.Substring(0, json.Length / 2)));
            Assert.Throws<SaveFormatException>(() => SaveSerializer.Deserialize("not json"));
        }

        [Test]
        public void NewerSchema_IsRejected_NotMisread()
        {
            var payload = new Dictionary<string, object> { ["x"] = 1L };
            Assert.Throws<SaveFormatException>(() => SaveSerializer.Deserialize(SaveSerializer.WrapPayload(payload, SaveData.CurrentSchemaVersion + 1)));
        }

        [Test]
        public void Migration_V1ToV2_PreservesProgressAndWallet()
        {
            var v1 = new Dictionary<string, object>
            {
                ["level"] = 7L,
                ["unlocked"] = 8L,
                ["coins"] = 420L,
                ["stars"] = new Dictionary<string, object> { ["1"] = 3L, ["2"] = 1L },
                ["boosters"] = new Dictionary<string, object> { ["Undo"] = 2L },
                ["music"] = false,
            };
            var d = SaveSerializer.Deserialize(SaveSerializer.WrapPayload(v1, 1));
            Assert.AreEqual(SaveData.CurrentSchemaVersion, d.SchemaVersion);
            Assert.AreEqual(8, d.Progress.HighestUnlocked);
            Assert.AreEqual(7, d.Progress.CurrentLevel);
            Assert.AreEqual(3, d.Progress.Stars(1));
            Assert.AreEqual(420, d.Economy.Coins);
            Assert.AreEqual(2, d.Economy.Count(BoosterType.Undo));
            Assert.IsFalse(d.Settings.Music);
            Assert.IsTrue(d.Economy.StartingGrantApplied, "migrated players must not get the starting grant twice");
        }

        [Test]
        public void Load_FallsBackToBackup_WhenPrimaryCorrupt()
        {
            var storage = new MemorySaveStorage();
            var svc = new SaveService(storage);
            var first = Sample();
            Assert.IsTrue(svc.Save(first));
            first.Economy.Coins = 1300;
            Assert.IsTrue(svc.Save(first));                        // backup now holds the 1250 version
            storage.Files[SaveService.PrimaryName] = "{garbage";
            var r = svc.Load();
            Assert.AreEqual(SaveLoadSource.Backup, r.Source);
            Assert.IsTrue(r.PrimaryWasCorrupt);
            Assert.IsNotNull(r.UserMessage);
            Assert.AreEqual(1250, r.Data.Economy.Coins);
        }

        [Test]
        public void Load_StartsFresh_WithMessage_WhenEverythingCorrupt()
        {
            var storage = new MemorySaveStorage();
            storage.Files[SaveService.PrimaryName] = "{";
            storage.Files[SaveService.BackupName] = "[]";
            var r = new SaveService(storage).Load();
            Assert.AreEqual(SaveLoadSource.New, r.Source);
            Assert.IsNotNull(r.UserMessage);
            Assert.AreEqual(1, r.Data.Progress.HighestUnlocked);
        }

        [Test]
        public void Load_NewPlayer_NoMessage()
        {
            var r = new SaveService(new MemorySaveStorage()).Load();
            Assert.AreEqual(SaveLoadSource.New, r.Source);
            Assert.IsNull(r.UserMessage);
        }

        [Test]
        public void Save_Failure_IsReported_NotThrown()
        {
            var storage = new MemorySaveStorage { FailWrites = true };
            var svc = new SaveService(storage);
            Assert.IsFalse(svc.Save(Sample()));
            Assert.IsNotNull(svc.LastError);
        }

        [Test]
        public void DamagedSession_IsDropped_RestOfSaveKept()
        {
            var d = Sample();
            var json = SaveSerializer.Serialize(d);
            var env = MiniJson.AsObject(MiniJson.Parse(json));
            var payload = MiniJson.AsObject(env["payload"]);
            payload["session"] = new Dictionary<string, object> { ["level"] = "oops" };
            var loaded = SaveSerializer.Deserialize(SaveSerializer.WrapPayload(payload, SaveData.CurrentSchemaVersion));
            Assert.IsNull(loaded.Session);
            Assert.AreEqual(1250, loaded.Economy.Coins);
        }

        [Test]
        public void FileStorage_AtomicWrite_KeepsBackup()
        {
            string dir = Path.Combine(Path.GetTempPath(), "veliqora_save_test_" + Guid.NewGuid().ToString("N"));
            try
            {
                var svc = new SaveService(new FileSaveStorage(dir));
                var d = Sample();
                Assert.IsTrue(svc.Save(d));
                d.Economy.Coins = 7;
                Assert.IsTrue(svc.Save(d));
                Assert.IsTrue(File.Exists(Path.Combine(dir, SaveService.PrimaryName)));
                Assert.IsTrue(File.Exists(Path.Combine(dir, SaveService.BackupName)));
                Assert.IsFalse(File.Exists(Path.Combine(dir, SaveService.PrimaryName + ".tmp")));
                Assert.AreEqual(7, svc.Load().Data.Economy.Coins);
                File.WriteAllText(Path.Combine(dir, SaveService.PrimaryName), "truncated{");
                var r = svc.Load();
                Assert.AreEqual(SaveLoadSource.Backup, r.Source);
                Assert.AreEqual(1250, r.Data.Economy.Coins);
            }
            finally
            {
                if (Directory.Exists(dir)) Directory.Delete(dir, true);
            }
        }

        [Test]
        public void ProgressRules_UnlockNext_KeepBestStars()
        {
            var p = new ProgressData();
            ProgressRules.RecordCompletion(p, 1, 2, 200);
            Assert.AreEqual(2, p.HighestUnlocked);
            ProgressRules.RecordCompletion(p, 1, 1, 200);
            Assert.AreEqual(2, p.Stars(1), "best stars preserved on worse replay");
            ProgressRules.RecordCompletion(p, 200, 3, 200);
            Assert.AreEqual(200, p.HighestUnlocked, "never unlock beyond the catalog");
            Assert.IsTrue(ProgressRules.IsUnlocked(p, 150));
            Assert.IsFalse(ProgressRules.IsUnlocked(p, 0));
        }
    }
}
