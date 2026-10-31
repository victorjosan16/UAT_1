using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Veliqora.Core.Ads;
using Veliqora.Core.Economy;
using Veliqora.Core.Puzzle;
using Veliqora.Infrastructure;
using Veliqora.UI;
using Veliqora.UI.Screens;

namespace Veliqora.Tests.PlayMode
{
    /// <summary>
    /// End-to-end flows through the real composition root with an isolated temporary save directory.
    /// These require the Unity Editor (Test Runner → PlayMode). They have NOT been executed in CI yet.
    /// </summary>
    public class VeliqoraPlayModeTests
    {
        private string _dir;
        private AppServices S => AppRoot.Instance.Services;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "veliqora_playmode_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            yield return null; // AppRoot auto-creates before the first scene
            Assert.IsNotNull(AppRoot.Instance, "AppRoot should auto-create");
            AppRoot.Instance.Reboot(_dir);
            yield return WaitFor(() => S.Navigator.CurrentId == ScreenId.Home && !S.Navigator.IsTransitioning);
        }

        [TearDown]
        public void TearDown()
        {
            Overlays.CloseAll();
            try { if (Directory.Exists(_dir)) Directory.Delete(_dir, true); } catch (IOException) { }
        }

        private static IEnumerator WaitFor(Func<bool> condition, float timeout = 10f)
        {
            float t = 0;
            while (!condition())
            {
                t += Time.unscaledDeltaTime;
                if (t > timeout) Assert.Fail("Timed out waiting for condition");
                yield return null;
            }
        }

        private IEnumerator Go(ScreenId id, object args = null)
        {
            yield return WaitFor(() => !S.Navigator.IsTransitioning);
            S.Navigator.Go(id, args);
            yield return WaitFor(() => S.Navigator.CurrentId == id && !S.Navigator.IsTransitioning);
        }

        private GameplayScreen Gameplay => S.Navigator.Current as GameplayScreen;

        /// <summary>Plays the stored solution by tapping bottle views (select + pour), waiting for animations.</summary>
        private IEnumerator SolveCurrentLevel()
        {
            var g = Gameplay;
            var level = g.Controller.Level;
            foreach (var step in level.Solution)
            {
                yield return WaitFor(() => !g.Controller.InputLocked);
                g.Board.BottleTapped(step[0]);
                g.Board.BottleTapped(step[1]);
            }
        }

        [UnityTest]
        public IEnumerator Home_To_Map_To_Level_To_Win_To_Next()
        {
            yield return Go(ScreenId.LevelMap);
            yield return Go(ScreenId.Gameplay, 1);
            Assert.AreEqual(1, Gameplay.Controller.Level.Number);
            yield return SolveCurrentLevel();
            yield return WaitFor(() => S.Navigator.CurrentId == ScreenId.LevelComplete && !S.Navigator.IsTransitioning, 15f);
            Assert.GreaterOrEqual(S.Save.Progress.Stars(1), 1);
            Assert.AreEqual(2, S.Save.Progress.HighestUnlocked);
            yield return Go(ScreenId.Gameplay, 2);
            Assert.AreEqual(2, Gameplay.Controller.Level.Number);
        }

        [UnityTest]
        public IEnumerator Restart_ResetsBoard()
        {
            yield return Go(ScreenId.Gameplay, 2);
            var c = Gameplay.Controller;
            var start = c.Session.Initial;
            c.Session.TryMove(c.Level.Solution[0][0], c.Level.Solution[0][1]);
            c.Restart();
            yield return null;
            Assert.AreEqual(start, c.Session.Current);
            Assert.AreEqual(0, c.Session.MoveCount);
        }

        [UnityTest]
        public IEnumerator Pause_Resume_SavesAndShowsPause()
        {
            yield return Go(ScreenId.Gameplay, 3);
            var c = Gameplay.Controller;
            Gameplay.Board.BottleTapped(c.Level.Solution[0][0]);
            Gameplay.Board.BottleTapped(c.Level.Solution[0][1]);
            yield return WaitFor(() => !c.InputLocked);
            AppRoot.Instance.SendMessage("OnApplicationPause", true);
            Assert.IsTrue(File.Exists(Path.Combine(_dir, Core.Save.SaveService.PrimaryName)));
            Assert.IsNotNull(S.Save.Session);
            Assert.AreEqual(1, S.Save.Session.Moves.Count);
            AppRoot.Instance.SendMessage("OnApplicationPause", false);
            yield return null;
            Assert.IsTrue(Overlays.AnyOpen, "resuming during a level opens the pause overlay");
        }

        [UnityTest]
        public IEnumerator RapidTapping_KeepsModelAndViewConsistent()
        {
            yield return Go(ScreenId.Gameplay, 30);
            var g = Gameplay;
            var counts = g.Controller.Session.Current.ColorCounts();
            var rng = new System.Random(5);
            for (int frame = 0; frame < 120; frame++)
            {
                for (int k = 0; k < 4; k++) g.Board.BottleTapped(rng.Next(0, g.Board.Views.Count));
                yield return null;
            }
            yield return WaitFor(() => !g.Controller.InputLocked || g.Controller.Session.IsComplete);
            CollectionAssert.AreEquivalent(counts, g.Controller.Session.Current.ColorCounts());
            Assert.AreEqual(g.Controller.Session.Current.BottleCount, g.Board.Views.Count);
        }

        [UnityTest]
        public IEnumerator Boosters_UndoAndExtraBottle_ConsumeInventory()
        {
            yield return Go(ScreenId.Gameplay, 12);
            var c = Gameplay.Controller;
            S.Economy.GrantBooster(BoosterType.Undo, 1, "test-undo");
            S.Economy.GrantBooster(BoosterType.ExtraBottle, 1, "test-extra");
            int undo = S.Economy.Count(BoosterType.Undo);
            int extra = S.Economy.Count(BoosterType.ExtraBottle);
            Gameplay.Board.BottleTapped(c.Level.Solution[0][0]);
            Gameplay.Board.BottleTapped(c.Level.Solution[0][1]);
            yield return WaitFor(() => !c.InputLocked);
            c.UseUndo();
            yield return WaitFor(() => !c.InputLocked);
            Assert.AreEqual(undo - 1, S.Economy.Count(BoosterType.Undo));
            Assert.AreEqual(0, c.Session.MoveCount);
            int bottles = c.Session.Current.BottleCount;
            c.UseExtraBottle();
            yield return null;
            Assert.AreEqual(extra - 1, S.Economy.Count(BoosterType.ExtraBottle));
            Assert.AreEqual(bottles + 1, Gameplay.Board.Views.Count);
        }

        [UnityTest]
        public IEnumerator Shop_Purchase_And_InsufficientFunds()
        {
            yield return Go(ScreenId.Shop);
            var offer = S.EconomyConfig.FindOffer("offer_undo_3");
            S.Economy.GrantCoins(offer.Price, "test-coins");
            long before = S.Economy.Coins;
            Assert.IsTrue(S.Economy.BuyOffer(offer.Id, "test-buy").Success);
            Assert.AreEqual(before - offer.Price, S.Economy.Coins);
            while (S.Economy.Coins >= offer.Price) S.Economy.BuyOffer(offer.Id, Guid.NewGuid().ToString());
            Assert.AreEqual(TxStatus.InsufficientFunds, S.Economy.BuyOffer(offer.Id, "test-poor").Status);
            Assert.GreaterOrEqual(S.Economy.Coins, 0);
        }

        [UnityTest]
        public IEnumerator Theme_Unlock_Apply_PersistsAcrossReboot()
        {
            yield return Go(ScreenId.Customization);
            var item = S.EconomyConfig.FindCosmetic("theme_night");
            S.Economy.GrantCoins(item.Price, "test-theme-coins");
            Assert.IsTrue(S.Economy.BuyCosmetic(item.Id, "test-theme").Success);
            Assert.IsTrue(S.Themes.Apply(item.Id));
            S.SaveNow();
            AppRoot.Instance.Reboot(_dir);
            yield return WaitFor(() => S.Navigator.CurrentId == ScreenId.Home && !S.Navigator.IsTransitioning);
            Assert.AreEqual("theme_night", S.Themes.ThemeId);
        }

        [UnityTest]
        public IEnumerator SaveReload_RestoresInProgressLevel()
        {
            yield return Go(ScreenId.Gameplay, 15);
            var c = Gameplay.Controller;
            Gameplay.Board.BottleTapped(c.Level.Solution[0][0]);
            Gameplay.Board.BottleTapped(c.Level.Solution[0][1]);
            yield return WaitFor(() => !c.InputLocked);
            var expected = c.Session.Current;
            S.SaveNow();
            AppRoot.Instance.Reboot(_dir);
            yield return WaitFor(() => S.Navigator.CurrentId == ScreenId.Home && !S.Navigator.IsTransitioning);
            yield return Go(ScreenId.Gameplay, 15);
            Assert.AreEqual(expected, Gameplay.Controller.Session.Current);
            Assert.AreEqual(1, Gameplay.Controller.Session.MoveCount);
        }

        [UnityTest]
        public IEnumerator Ads_OfflineOrFailure_DisablesRewarded_WithoutBlocking()
        {
            S.Settings.SetSimulateAdFailure(true);
            Assert.IsFalse(S.AdGate.CanRequest(AdPlacements.DoubleCoins));
            AdOutcome? outcome = null;
            S.AdGate.Request(AdPlacements.DoubleCoins, () => Assert.Fail("must not reward"), o => outcome = o);
            yield return null;
            Assert.AreEqual(AdOutcome.Unavailable, outcome);
            S.Settings.SetSimulateAdFailure(false);
        }

        [UnityTest]
        public IEnumerator MockRewarded_Success_And_Cancel()
        {
            if (!S.Ads.IsMock) Assert.Ignore("Mock ads not active in this configuration.");
            if (Application.internetReachability == NetworkReachability.NotReachable) Assert.Ignore("Mock ads mimic offline as unavailable.");
            int rewards = 0;
            AdOutcome? outcome = null;
            S.AdGate.Request(AdPlacements.BonusBooster, () => rewards++, o => outcome = o);
            yield return null;
            ClickButtonContaining("Finish");
            yield return WaitFor(() => outcome.HasValue);
            Assert.AreEqual(AdOutcome.Completed, outcome);
            Assert.AreEqual(1, rewards);

            outcome = null;
            S.AdGate.Request(AdPlacements.BonusBooster, () => rewards++, o => outcome = o);
            yield return null;
            ClickButtonContaining("Close early");
            yield return WaitFor(() => outcome.HasValue);
            Assert.AreEqual(AdOutcome.Cancelled, outcome);
            Assert.AreEqual(1, rewards, "cancel must not grant");
        }

        [UnityTest]
        public IEnumerator Screens_UseSafeArea()
        {
            foreach (var id in new[] { ScreenId.Home, ScreenId.LevelMap, ScreenId.Shop, ScreenId.Customization })
            {
                yield return Go(id);
                Assert.IsNotNull(S.Navigator.Current.GetComponentInChildren<SafeAreaFitter>(), id + " lacks a SafeAreaFitter");
            }
        }

        [UnityTest]
        public IEnumerator BackButton_Behaviour()
        {
            yield return Go(ScreenId.Gameplay, 4);
            AppRoot.Instance.HandleBack();
            Assert.IsTrue(Overlays.AnyOpen, "Back in gameplay opens Pause");
            AppRoot.Instance.HandleBack();
            yield return null;
            Assert.IsFalse(Overlays.AnyOpen, "Back again closes the overlay");
            yield return Go(ScreenId.LevelMap);
            AppRoot.Instance.HandleBack();
            yield return WaitFor(() => S.Navigator.CurrentId == ScreenId.Home && !S.Navigator.IsTransitioning);
        }

        [UnityTest]
        public IEnumerator TwelveDarkBackgrounds_AreSelectable_AndRenderInGameplay()
        {
            int dark = 0;
            foreach (var c in S.EconomyConfig.Cosmetics)
            {
                if (!c.Id.StartsWith("dark_")) continue;
                dark++;
                Assert.IsTrue(S.Economy.Owns(c.Id), c.Id + " should be free to select");
                Assert.IsTrue(S.Themes.Apply(c.Id));
                Assert.AreEqual(c.Id, S.Themes.ThemeId);
                Assert.IsTrue(S.Themes.Theme.IsDark);
                Assert.IsNotNull(Presentation.BackgroundArt.Valley(S.Themes.Theme), c.Id + " background");
            }
            Assert.AreEqual(12, dark);
            yield return Go(ScreenId.Gameplay, 5);
            Assert.IsNotNull(Gameplay.Board.Fx, "particle layer exists on the board");
        }

        [UnityTest]
        public IEnumerator RewardedAd_AddsEmptyBottle_OnlyWhenCompleted()
        {
            if (!S.Ads.IsMock) Assert.Ignore("Mock ads not active in this configuration.");
            if (Application.internetReachability == NetworkReachability.NotReachable) Assert.Ignore("Mock ads mimic offline as unavailable.");
            yield return Go(ScreenId.Gameplay, 14);
            var c = Gameplay.Controller;
            int bottles = c.Session.Current.BottleCount;

            bool? result = null;
            c.AddBottleFromAd(ok => result = ok);
            yield return null;
            ClickButtonContaining("Close early");
            yield return WaitFor(() => result.HasValue);
            Assert.IsFalse(result.Value);
            Assert.AreEqual(bottles, c.Session.Current.BottleCount, "cancelled ad gives nothing");

            result = null;
            c.AddBottleFromAd(ok => result = ok);
            yield return null;
            ClickButtonContaining("Finish");
            yield return WaitFor(() => result.HasValue);
            Assert.IsTrue(result.Value);
            Assert.AreEqual(bottles + 1, c.Session.Current.BottleCount);
            Assert.AreEqual(bottles + 1, Gameplay.Board.Views.Count);
            Assert.IsTrue(c.Session.Current[bottles].IsEmpty);
        }

        private static void ClickButtonContaining(string text)
        {
            foreach (var t in Overlays.Layer.GetComponentsInChildren<Text>(true))
            {
                if (!t.text.Contains(text)) continue;
                var b = t.GetComponentInParent<Transform>().parent.GetComponentInChildren<Button>();
                if (b != null) { b.onClick.Invoke(); return; }
            }
            Assert.Fail("Button '" + text + "' not found");
        }
    }
}
