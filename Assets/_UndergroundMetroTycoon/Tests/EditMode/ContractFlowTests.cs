using System.Collections.Generic;
using NUnit.Framework;
using UndergroundMetroTycoon.Simulation;

namespace UndergroundMetroTycoon.Tests
{
    public class ContractFlowTests
    {
        private GameSession _session;

        [SetUp]
        public void SetUp()
        {
            _session = GameSession.CreateNew(DefaultContent.Create());
        }

        [Test]
        public void NewGame_StartsWithZeroCoins_AndFirstContractAvailable()
        {
            Assert.AreEqual(0, _session.Wallet.Coins);
            Assert.AreEqual(1, _session.Progression.Level);
            Assert.AreEqual(ContractState.Available, _session.Contracts.GetState(DefaultContent.GardenTrenchId));
            Assert.AreEqual(ContractState.Locked, _session.Contracts.GetState(DefaultContent.SmallTrenchId));
            Assert.AreEqual(ContractState.Locked, _session.Contracts.GetState(DefaultContent.ResidentialFoundationId));
            Assert.AreEqual(DefaultContent.GardenTrenchId, _session.Grid.SiteContractId);
        }

        [Test]
        public void DiggingBeforeAccepting_IsRejected()
        {
            Assert.AreEqual(DigOutcome.NoActiveContract, _session.Dig(0, GameSession.PlayerActorId).Outcome);
            Assert.AreEqual(0, _session.Wallet.Coins);
        }

        [Test]
        public void FirstContract_FullLoop_PaysCellsAndRewardExactlyOnce()
        {
            var states = new List<ContractState>();
            _session.ContractStateChanged += (id, s) => { if (id == DefaultContent.GardenTrenchId) states.Add(s); };

            Assert.AreEqual(ContractActionResult.Ok, _session.AcceptContract(DefaultContent.GardenTrenchId));
            int swings = TestHelpers.DigOutActiveContract(_session);

            Assert.AreEqual(40, swings, "20 soft soil cells x 2 shovel hits");
            Assert.AreEqual(20 * 5, _session.Wallet.Coins);
            Assert.AreEqual(ContractState.ReadyForCompletion, _session.Contracts.GetState(DefaultContent.GardenTrenchId));

            Assert.AreEqual(ContractActionResult.Ok, _session.ClaimContract(DefaultContent.GardenTrenchId));
            Assert.AreEqual(100 + 500, _session.Wallet.Coins);
            Assert.AreEqual(ContractState.RewardClaimed, _session.Contracts.GetState(DefaultContent.GardenTrenchId));

            Assert.AreNotEqual(ContractActionResult.Ok, _session.ClaimContract(DefaultContent.GardenTrenchId));
            Assert.AreEqual(600, _session.Wallet.Coins);

            CollectionAssert.AreEqual(new[]
            {
                ContractState.Accepted, ContractState.InProgress, ContractState.ReadyForCompletion,
                ContractState.Completed, ContractState.RewardClaimed
            }, states);
        }

        [Test]
        public void ClaimBeforeCompletion_IsRejected()
        {
            _session.AcceptContract(DefaultContent.GardenTrenchId);
            _session.Dig(0, GameSession.PlayerActorId);
            Assert.AreEqual(ContractActionResult.InvalidState, _session.ClaimContract(DefaultContent.GardenTrenchId));
            Assert.AreEqual(0, _session.Wallet.Coins);
        }

        [Test]
        public void OnlyOneContract_CanBeActive()
        {
            CompleteGardenTrench();
            _session.AcceptContract(DefaultContent.SmallTrenchId);
            Assert.AreEqual(ContractActionResult.AnotherContractActive, _session.AcceptContract(DefaultContent.ResidentialFoundationId));
        }

        [Test]
        public void RepeatableContract_BecomesAvailableAgain_AndRebuildsSite()
        {
            CompleteGardenTrench();
            Assert.AreEqual(ContractState.Available, _session.Contracts.GetState(DefaultContent.SmallTrenchId));

            for (int round = 1; round <= 3; round++)
            {
                long before = _session.Wallet.Coins;
                Assert.AreEqual(ContractActionResult.Ok, _session.AcceptContract(DefaultContent.SmallTrenchId));
                Assert.AreEqual(0, _session.Grid.ExcavatedContractCellCount, "site must be fresh each round");
                TestHelpers.DigOutActiveContract(_session);
                Assert.AreEqual(ContractActionResult.Ok, _session.ClaimContract(DefaultContent.SmallTrenchId));
                Assert.AreEqual(before + 12 * 5 + 120, _session.Wallet.Coins);
                Assert.AreEqual(ContractState.Available, _session.Contracts.GetState(DefaultContent.SmallTrenchId));
                Assert.AreEqual(round, _session.Contracts.GetProgress(DefaultContent.SmallTrenchId).CompletionCount);
            }
        }

        [Test]
        public void ResidentialFoundation_UnlocksAtLevelTwo_AndIsCompletableWithShovel()
        {
            CompleteGardenTrench();
            Assert.AreEqual(1, _session.Progression.Level);
            Assert.AreEqual(ContractActionResult.Locked, _session.AcceptContract(DefaultContent.ResidentialFoundationId));

            _session.AcceptContract(DefaultContent.SmallTrenchId);
            TestHelpers.DigOutActiveContract(_session);
            _session.ClaimContract(DefaultContent.SmallTrenchId);
            Assert.AreEqual(2, _session.Progression.Level);
            Assert.AreEqual(ContractState.Available, _session.Contracts.GetState(DefaultContent.ResidentialFoundationId));

            Assert.AreEqual(ContractActionResult.Ok, _session.AcceptContract(DefaultContent.ResidentialFoundationId));
            TestHelpers.DigOutActiveContract(_session);
            Assert.AreEqual(ContractState.ReadyForCompletion, _session.Contracts.GetState(DefaultContent.ResidentialFoundationId));
        }

        [Test]
        public void LevelChanged_IsRaisedOnLevelUp()
        {
            int newLevel = 0;
            _session.LevelChanged += (_, level) => newLevel = level;
            CompleteGardenTrench();
            _session.AcceptContract(DefaultContent.SmallTrenchId);
            TestHelpers.DigOutActiveContract(_session);
            _session.ClaimContract(DefaultContent.SmallTrenchId);
            Assert.AreEqual(2, newLevel);
        }

        private void CompleteGardenTrench()
        {
            _session.AcceptContract(DefaultContent.GardenTrenchId);
            TestHelpers.DigOutActiveContract(_session);
            _session.ClaimContract(DefaultContent.GardenTrenchId);
        }
    }
}
