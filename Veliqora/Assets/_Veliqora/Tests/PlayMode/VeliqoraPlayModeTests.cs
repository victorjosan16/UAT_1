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
            MainMenuScreen.AutoOpenDaily = false;
            AppRoot.Instance.Reboot(_dir);
            yield return WaitFor(() => S.Navigator.CurrentId == ScreenId.MainMenu && !S.Navigator.IsTransitioning);
            S.Settings.SetLanguage("en");   // button-label lookups below use the English strings
            Loc.Load("en");
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
            yield return Go(ScreenId.Themes);
            var item = S.EconomyConfig.FindCosmetic("theme_night");
            S.Economy.GrantCoins(item.Price, "test-theme-coins");
            Assert.IsTrue(S.Economy.BuyCosmetic(item.Id, "test-theme").Success);
            Assert.IsTrue(S.Themes.Apply(item.Id));
            S.SaveNow();
            AppRoot.Instance.Reboot(_dir);
            yield return WaitFor(() => S.Navigator.CurrentId == ScreenId.MainMenu && !S.Navigator.IsTransitioning);
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
            yield return WaitFor(() => S.Navigator.CurrentId == ScreenId.MainMenu && !S.Navigator.IsTransitioning);
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
            foreach (var id in new[] { ScreenId.MainMenu, ScreenId.LevelMap, ScreenId.Shop, ScreenId.Themes })
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
            yield return WaitFor(() => S.Navigator.CurrentId == ScreenId.MainMenu && !S.Navigator.IsTransitioning);
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

        [UnityTest]
        public IEnumerator FirstLaunch_OpensLevel1_ReadyToPlay_WithoutTutorial()
        {
            // Fresh save directory = brand-new player.
            var fresh = Path.Combine(Path.GetTempPath(), "veliqora_first_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(fresh);
            AppRoot.Instance.Reboot(fresh, goHome: false);
            Assert.IsTrue(AppRoot.Instance.IsFirstLaunch);
            AppRoot.Instance.GoToStartScreen();
            yield return WaitFor(() => S.Navigator.CurrentId == ScreenId.Gameplay && !S.Navigator.IsTransitioning);
            Assert.AreEqual(1, Gameplay.Controller.Level.Number);
            Assert.AreEqual(5, Gameplay.Controller.Session.Current.BottleCount, "3 mixed bottles + 2 empty");
            Assert.IsFalse(Overlays.AnyOpen, "no intro / instruction overlay");
            foreach (var t in UnityEngine.Object.FindObjectsByType<Text>(FindObjectsSortMode.None))
                Assert.IsFalse(t.gameObject.activeInHierarchy && t.text.Contains("Tap a bottle"), "no tutorial text");
            // The first tap selects a bottle immediately (input is not blocked by explanations).
            var r = Gameplay.Controller.Session.Tap(0);
            Assert.AreEqual(Core.Session.TapKind.Selected, r.Kind);
            try { Directory.Delete(fresh, true); } catch (IOException) { }
        }

        private int FindLevel(Func<Core.Levels.LevelDefinition, bool> pred)
        {
            for (int i = 1; i <= S.Levels.Count; i++) if (pred(S.Levels.Get(i))) return i;
            return -1;
        }

        private void UnlockUpTo(int level)
        {
            S.Save.Progress.HighestUnlocked = Math.Max(S.Save.Progress.HighestUnlocked, level);
            S.MarkDirty();
        }

        [UnityTest]
        public IEnumerator CorkedBottle_RejectsTaps_UntilConditionMet()
        {
            int n = FindLevel(l => l.HasLocks);
            Assert.Greater(n, 0, "catalog contains corked levels");
            UnlockUpTo(n);
            yield return Go(ScreenId.Gameplay, n);
            var c = Gameplay.Controller;
            int corked = -1;
            for (int i = 0; i < c.Session.Current.BottleCount; i++) if (c.Session.Current[i].IsCorked) { corked = i; break; }
            Assert.GreaterOrEqual(corked, 0);
            var before = c.Session.Current;
            var r = c.Session.Tap(corked);
            Assert.AreEqual(Core.Session.TapKind.Invalid, r.Kind);
            Assert.AreEqual(MoveRejection.Locked, r.Move.Rejection);
            Assert.AreEqual(before, c.Session.Current, "a corked bottle never changes the board");
        }

        [UnityTest]
        public IEnumerator RewardBottle_UnlocksOnlyAfterCompletedAd()
        {
            if (!S.Ads.IsMock) Assert.Ignore("Mock ads not active in this configuration.");
            if (Application.internetReachability == NetworkReachability.NotReachable) Assert.Ignore("Mock ads mimic offline as unavailable.");
            int n = FindLevel(l => l.RewardBottles != null && l.RewardBottles.Length > 0);
            Assert.Greater(n, 0, "catalog contains reward-bottle levels");
            UnlockUpTo(n);
            yield return Go(ScreenId.Gameplay, n);
            var c = Gameplay.Controller;
            int idx = -1;
            for (int i = 0; i < c.Session.Current.BottleCount; i++) if (c.Session.IsRewardBottle(i)) { idx = i; break; }
            Assert.GreaterOrEqual(idx, 0);

            bool? result = null;
            c.UnlockRewardBottle(idx, ok => result = ok);
            yield return null;
            ClickButtonContaining("Close early");
            yield return WaitFor(() => result.HasValue);
            Assert.IsFalse(result.Value);
            Assert.IsTrue(c.Session.IsRewardBottle(idx), "cancelled ad leaves the bottle locked");

            yield return WaitFor(() => c.CanUnlockRewardBottle(idx), 30f);
            result = null;
            c.UnlockRewardBottle(idx, ok => result = ok);
            yield return null;
            ClickButtonContaining("Finish");
            yield return WaitFor(() => result.HasValue);
            Assert.IsTrue(result.Value);
            Assert.IsFalse(c.Session.IsRewardBottle(idx));
        }

        [UnityTest]
        public IEnumerator MysteryLevel_HidesLayers_AndObjectivesAreTracked()
        {
            int n = FindLevel(l => l.HasMystery && l.Objectives.Count > 0);
            Assert.Greater(n, 0, "catalog contains mystery levels with objectives");
            UnlockUpTo(n);
            yield return Go(ScreenId.Gameplay, n);
            var c = Gameplay.Controller;
            Assert.Greater(c.Session.Current.HiddenTotal(), 0);
            var objectives = c.Objectives();
            Assert.AreEqual(S.Levels.Get(n).Objectives.Count, objectives.Count);
            foreach (var o in objectives) Assert.IsFalse(o.Complete && o.Def.Type == Core.Objectives.ObjectiveType.FinishLevel);
        }

        [UnityTest]
        public IEnumerator RemoveColor_ChargesOnlyOnSuccess()
        {
            int n = FindLevel(l => !l.IsBoosterDisabled("RemoveColor") && !l.HasLocks && l.Number > 10);
            UnlockUpTo(n);
            yield return Go(ScreenId.Gameplay, n);
            var c = Gameplay.Controller;
            int before = S.Economy.Count(BoosterType.RemoveColor);
            if (before <= 0) { S.Economy.GrantBooster(BoosterType.RemoveColor, 1, "test-remove"); before = 1; }
            int target = -1;
            for (int i = 0; i < c.Session.Current.BottleCount; i++) if (c.Session.CanRemoveColorAt(i)) { target = i; break; }
            Assert.GreaterOrEqual(target, 0);
            bool? result = null;
            c.UseRemoveColor(target, ok => result = ok);
            yield return WaitFor(() => result.HasValue, 15f);
            Assert.AreEqual(result.Value ? before - 1 : before, S.Economy.Count(BoosterType.RemoveColor));
        }

        [UnityTest]
        public IEnumerator DailyReward_ClaimsOncePerDay()
        {
            int today = ProgressionOverlays.Today;
            Assert.IsTrue(S.Daily.CanClaim(today));
            long coins = S.Economy.Coins;
            var reward = S.Daily.Claim(today);
            Assert.IsNotNull(reward);
            Assert.AreEqual(coins + reward.Coins, S.Economy.Coins);
            Assert.IsNull(S.Daily.Claim(today), "second claim on the same day is refused");
            Assert.IsFalse(S.Daily.CanClaim(today));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Language_SwitchesToRomanian_AndPersists()
        {
            S.Settings.SetLanguage("ro");
            Loc.Load("ro");
            Assert.AreEqual("ro", S.Save.Settings.Language);
            Assert.AreNotEqual("Undo", Loc.T("Undo"), "Romanian table is loaded");
            yield return Go(ScreenId.Settings);
            yield return Go(ScreenId.MainMenu);
            S.Settings.SetLanguage("en");
            Loc.Load("en");
            Assert.AreEqual("Undo", Loc.T("Undo"));
        }

        [UnityTest]
        public IEnumerator AchievementNotices_ShowOnce()
        {
            S.Save.Progress.BestStars[1] = 3;
            for (int i = 2; i <= 5; i++) S.Save.Progress.BestStars[i] = 1;   // "First Steps": 5 levels
            ProgressionOverlays.CheckAchievementNotices(S);
            int queued = UnlockNotifications.QueuedCount;
            Assert.Greater(queued, 0);
            ProgressionOverlays.CheckAchievementNotices(S);
            Assert.AreEqual(queued, UnlockNotifications.QueuedCount, "no duplicate notices");
            Assert.Greater(S.Achievements.ClaimableCount(), 0);
            yield return null;
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
