using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UndergroundMetroTycoon.Simulation;

namespace UndergroundMetroTycoon.Tests
{
    public class SaveSystemTests
    {
        private string _dir;
        private SaveStore _store;
        private GameContent _content;

        [SetUp]
        public void SetUp()
        {
            _dir = TestHelpers.CreateTempDirectory();
            _store = new SaveStore(_dir, "save.json", TestHelpers.CreateSerializer());
            _content = DefaultContent.Create();
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
        }

        [Test]
        public void Load_WithoutFile_ReportsNoSave()
        {
            Assert.AreEqual(SaveLoadStatus.NoSave, _store.Load().Status);
        }

        [Test]
        public void MidContractProgress_RoundTrips()
        {
            GameSession session = GameSession.CreateNew(_content);
            session.AcceptContract(DefaultContent.GardenTrenchId);
            for (int i = 0; i < 5; i++) { session.Dig(i, GameSession.PlayerActorId); session.Dig(i, GameSession.PlayerActorId); }
            session.Dig(5, GameSession.PlayerActorId);
            session.PlayerPose = new PlayerPose { IsSet = true, X = 1.5f, Y = -0.5f, Z = 2f, Yaw = 90f };
            session.Settings.SoundEnabled = false;

            Assert.IsTrue(_store.Save(session.CreateSaveData(123), out string error), error);
            SaveLoadResult result = _store.Load();
            Assert.AreEqual(SaveLoadStatus.Loaded, result.Status);

            GameSession restored = GameSession.Restore(_content, result.Data, new List<string>());
            Assert.AreEqual(25, restored.Wallet.Coins);
            Assert.AreEqual(ContractState.InProgress, restored.Contracts.GetState(DefaultContent.GardenTrenchId));
            Assert.AreEqual(5, restored.Grid.ExcavatedContractCellCount);
            Assert.AreEqual(5, restored.Grid.GetCell(5).CurrentHp);
            Assert.IsTrue(restored.PlayerPose.IsSet);
            Assert.AreEqual(1.5f, restored.PlayerPose.X, 1e-5f);
            Assert.IsFalse(restored.Settings.SoundEnabled);
            Assert.AreEqual(123, result.Data.SavedAtUtcTicks);

            // Restored cells keep paying exactly once and the contract can still be finished.
            Assert.AreEqual(DigOutcome.AlreadyExcavated, restored.Dig(0, GameSession.PlayerActorId).Outcome);
            TestHelpers.DigOutActiveContract(restored);
            Assert.AreEqual(100, restored.Wallet.Coins);
        }

        [Test]
        public void ClaimedReward_IsNotPaidAgainAfterReload()
        {
            GameSession session = GameSession.CreateNew(_content);
            session.AcceptContract(DefaultContent.GardenTrenchId);
            TestHelpers.DigOutActiveContract(session);
            session.ClaimContract(DefaultContent.GardenTrenchId);
            _store.Save(session.CreateSaveData(1), out _);

            GameSession restored = GameSession.Restore(_content, _store.Load().Data, new List<string>());
            Assert.AreEqual(600, restored.Wallet.Coins);
            Assert.AreNotEqual(ContractActionResult.Ok, restored.ClaimContract(DefaultContent.GardenTrenchId));
            Assert.AreEqual(600, restored.Wallet.Coins);
        }

        [Test]
        public void ReadyContract_StaysClaimableAfterReload()
        {
            GameSession session = GameSession.CreateNew(_content);
            session.AcceptContract(DefaultContent.GardenTrenchId);
            TestHelpers.DigOutActiveContract(session);
            _store.Save(session.CreateSaveData(1), out _);

            GameSession restored = GameSession.Restore(_content, _store.Load().Data, new List<string>());
            Assert.AreEqual(ContractState.ReadyForCompletion, restored.Contracts.GetState(DefaultContent.GardenTrenchId));
            Assert.AreEqual(ContractActionResult.Ok, restored.ClaimContract(DefaultContent.GardenTrenchId));
            Assert.AreEqual(600, restored.Wallet.Coins);
        }

        [Test]
        public void CorruptMainFile_FallsBackToBackup()
        {
            GameSession session = GameSession.CreateNew(_content);
            session.Wallet.Credit(10);
            _store.Save(session.CreateSaveData(1), out _);
            session.Wallet.Credit(5);
            _store.Save(session.CreateSaveData(2), out _);
            Assert.IsTrue(File.Exists(_store.BackupPath));

            File.WriteAllText(_store.MainPath, "{ this is not json");
            SaveLoadResult result = _store.Load();
            Assert.AreEqual(SaveLoadStatus.LoadedFromBackup, result.Status);
            Assert.AreEqual(10, result.Data.Coins);
            Assert.IsTrue(File.Exists(_store.MainPath + ".corrupt"));
        }

        [Test]
        public void TamperedPayload_FailsChecksum()
        {
            GameSession session = GameSession.CreateNew(_content);
            _store.Save(session.CreateSaveData(1), out _);
            ISaveSerializer serializer = TestHelpers.CreateSerializer();
            SaveEnvelope envelope = serializer.Deserialize<SaveEnvelope>(File.ReadAllText(_store.MainPath));
            SaveData data = serializer.Deserialize<SaveData>(envelope.Payload);
            data.Coins = 999999;
            envelope.Payload = serializer.Serialize(data);
            File.WriteAllText(_store.MainPath, serializer.Serialize(envelope));
            Assert.AreEqual(SaveLoadStatus.Corrupted, _store.Load().Status);
        }

        [Test]
        public void NewerSaveVersion_IsRejected()
        {
            var data = GameSession.CreateNew(_content).CreateSaveData(1);
            data.Version = SaveData.CurrentVersion + 1;
            _store.Save(data, out _);
            Assert.AreEqual(SaveLoadStatus.Corrupted, _store.Load().Status);
        }

        [Test]
        public void Migrator_UpgradesUnversionedData()
        {
            var data = new SaveData { Version = 0, Contracts = null, SiteCellHp = null, Settings = null };
            Assert.IsTrue(SaveMigrator.TryMigrate(data, out _));
            Assert.AreEqual(SaveData.CurrentVersion, data.Version);
            Assert.IsNotNull(data.Contracts);
            Assert.IsNotNull(data.SiteCellHp);
            Assert.IsNotNull(data.Settings);
        }

        [Test]
        public void Restore_RepairsInconsistentData()
        {
            GameSession session = GameSession.CreateNew(_content);
            session.AcceptContract(DefaultContent.GardenTrenchId);
            SaveData data = session.CreateSaveData(1);
            data.Coins = -500;
            data.SiteCellHp.RemoveAt(0);
            data.Contracts.Add(new ContractSaveData { Id = "does_not_exist", State = 3 });

            var warnings = new List<string>();
            GameSession restored = GameSession.Restore(_content, data, warnings);
            Assert.AreEqual(0, restored.Wallet.Coins);
            Assert.AreEqual(ContractState.Accepted, restored.Contracts.GetState(DefaultContent.GardenTrenchId));
            Assert.AreEqual(0, restored.Grid.ExcavatedContractCellCount);
            Assert.IsNotEmpty(warnings);
        }

        [Test]
        public void Restore_KeepsOnlyOneActiveContract()
        {
            SaveData data = GameSession.CreateNew(_content).CreateSaveData(1);
            data.Contracts = new List<ContractSaveData>
            {
                new ContractSaveData { Id = DefaultContent.GardenTrenchId, State = (int)ContractState.InProgress, Instance = 1 },
                new ContractSaveData { Id = DefaultContent.SmallTrenchId, State = (int)ContractState.InProgress, Instance = 1, CompletionCount = 0 }
            };
            GameSession restored = GameSession.Restore(_content, data, new List<string>());
            int active = 0;
            foreach (ContractDefinition def in restored.Contracts.Definitions)
                if (ContractManager.IsActiveState(restored.Contracts.GetState(def.Id))) active++;
            Assert.AreEqual(1, active);
        }
    }
}
