using System;
using System.Collections.Generic;
using NUnit.Framework;
using Veliqora.Core.Ads;
using Veliqora.Core.Json;

namespace Veliqora.Tests.EditMode
{
    public class RewardedAdGateTests
    {
        /// <summary>Test double that lets the test fire callbacks manually (including duplicates).</summary>
        private sealed class FakeAds : IAdsService
        {
            public bool Ready = true;
            public readonly List<Action<AdOutcome>> Pending = new List<Action<AdOutcome>>();
            public string ProviderName => "Fake";
            public bool IsMock => true;
            public bool IsRewardedReady(string placement) => Ready;
            public void ShowRewarded(string placement, Action<AdOutcome> callback) => Pending.Add(callback);
            public bool SupportsBanner => false;
            public void ShowBanner(string placement) { }
            public void HideBanner() { }
        }

        private double _now;

        [SetUp]
        public void ResetClock() => _now = 0;

        [Test]
        public void Completed_GrantsExactlyOnce_EvenWithDuplicateCallbacks()
        {
            var ads = new FakeAds();
            var gate = new RewardedAdGate(ads, () => _now);
            int rewards = 0, finished = 0;
            Assert.IsTrue(gate.Request(AdPlacements.DoubleCoins, () => rewards++, o => finished++));
            ads.Pending[0](AdOutcome.Completed);
            ads.Pending[0](AdOutcome.Completed);
            ads.Pending[0](AdOutcome.Failed);
            Assert.AreEqual(1, rewards);
            Assert.AreEqual(1, finished);
            Assert.IsFalse(gate.InFlight);
        }

        [TestCase(AdOutcome.Cancelled)]
        [TestCase(AdOutcome.Failed)]
        [TestCase(AdOutcome.NoFill)]
        public void NonCompletedOutcomes_NeverGrant(AdOutcome outcome)
        {
            var ads = new FakeAds();
            var gate = new RewardedAdGate(ads, () => _now);
            int rewards = 0;
            AdOutcome? final = null;
            gate.Request(AdPlacements.BonusBooster, () => rewards++, o => final = o);
            ads.Pending[0](outcome);
            Assert.AreEqual(0, rewards);
            Assert.AreEqual(outcome, final);
        }

        [Test]
        public void Timeout_ResolvesWithoutReward_AndLateCompletionIsIgnored()
        {
            var ads = new FakeAds();
            var gate = new RewardedAdGate(ads, () => _now, timeoutSeconds: 10);
            int rewards = 0;
            AdOutcome? final = null;
            gate.Request(AdPlacements.DoubleCoins, () => rewards++, o => final = o);
            _now = 11;
            gate.Tick();
            Assert.AreEqual(AdOutcome.Timeout, final);
            ads.Pending[0](AdOutcome.Completed);
            Assert.AreEqual(0, rewards, "late completion after timeout must not grant");
        }

        [Test]
        public void OnlyOneRequestInFlight()
        {
            var ads = new FakeAds();
            var gate = new RewardedAdGate(ads, () => _now);
            Assert.IsTrue(gate.Request("p", null, null));
            Assert.IsFalse(gate.Request("p", null, null));
            Assert.AreEqual(1, ads.Pending.Count);
        }

        [Test]
        public void Unavailable_ReportsImmediately()
        {
            var ads = new FakeAds { Ready = false };
            var gate = new RewardedAdGate(ads, () => _now);
            AdOutcome? final = null;
            Assert.IsFalse(gate.Request("p", () => Assert.Fail("no reward"), o => final = o));
            Assert.AreEqual(AdOutcome.Unavailable, final);
            Assert.IsFalse(gate.CanRequest("p"));
        }

        [Test]
        public void Abort_OnBackgrounding_DoesNotGrant()
        {
            var ads = new FakeAds();
            var gate = new RewardedAdGate(ads, () => _now);
            int rewards = 0;
            gate.Request("p", () => rewards++, null);
            gate.Abort();
            ads.Pending[0](AdOutcome.Completed);
            Assert.AreEqual(0, rewards);
        }

        [Test]
        public void Cooldown_AppliesAfterCompletion()
        {
            var ads = new FakeAds();
            var gate = new RewardedAdGate(ads, () => _now);
            gate.Request("p", null, null, cooldownSeconds: 30);
            ads.Pending[0](AdOutcome.Completed);
            Assert.IsFalse(gate.CanRequest("p"));
            _now = 31;
            Assert.IsTrue(gate.CanRequest("p"));
        }

        [Test]
        public void NoAdsService_NeverBlocks()
        {
            var gate = new RewardedAdGate(new NoAdsService(), () => 0);
            AdOutcome? final = null;
            gate.Request("p", () => Assert.Fail(), o => final = o);
            Assert.AreEqual(AdOutcome.Unavailable, final);
        }
    }

    public class MiniJsonTests
    {
        [Test]
        public void RoundTrip_NestedStructures()
        {
            var obj = new Dictionary<string, object>
            {
                ["s"] = "quote\" backslash\\ newline\n unicode é",
                ["i"] = 42L,
                ["neg"] = -7L,
                ["d"] = 0.25,
                ["b"] = true,
                ["n"] = null,
                ["arr"] = new List<object> { 1L, 2L, new Dictionary<string, object> { ["k"] = "v" } },
            };
            var back = MiniJson.AsObject(MiniJson.Parse(MiniJson.Serialize(obj, pretty: true)));
            Assert.AreEqual(obj["s"], back["s"]);
            Assert.AreEqual(42L, back["i"]);
            Assert.AreEqual(-7L, back["neg"]);
            Assert.AreEqual(0.25, back["d"]);
            Assert.AreEqual(true, back["b"]);
            Assert.IsNull(back["n"]);
            Assert.AreEqual("v", MiniJson.AsObject(MiniJson.AsList(back["arr"])[2])["k"]);
        }

        [TestCase("{")]
        [TestCase("[1,2")]
        [TestCase("{\"a\":}")]
        [TestCase("tru")]
        [TestCase("{} x")]
        public void Malformed_Throws(string bad)
        {
            Assert.Throws<FormatException>(() => MiniJson.Parse(bad));
        }
    }
}
