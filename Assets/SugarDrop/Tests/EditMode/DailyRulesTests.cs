using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace SugarDrop.Tests
{
    public class DailyRulesTests
    {
        [Test]
        public void Streak_ContinuesOnlyOnConsecutiveDays_AndWrapsAfterSeven()
        {
            Assert.AreEqual(2, DailyRules.NextStreak(100, 1, 101));
            Assert.AreEqual(1, DailyRules.NextStreak(100, 3, 102), "o zi pierdută o ia de la capăt");
            Assert.AreEqual(1, DailyRules.NextStreak(100, 7, 101), "după ziua 7 seria reîncepe");
            Assert.AreEqual(1, DailyRules.NextStreak(-1, 0, 5));
            Assert.IsTrue(DailyRules.CanClaimGift(100, 101));
            Assert.IsFalse(DailyRules.CanClaimGift(101, 101));
        }

        [Test]
        public void Gifts_AreDefinedForEveryDay_AndTheLastIsTheBiggest()
        {
            for (int day = 1; day <= DailyRules.StreakLength; day++)
            {
                DailyGift gift = DailyRules.GiftFor(day);
                Assert.Greater(gift.Coins + gift.Bombs + gift.ExtraMoves + gift.Shuffles, 0, $"ziua {day}");
            }
            Assert.AreEqual(300, DailyRules.GiftFor(7).Coins);
        }

        [Test]
        public void Missions_AreThreeDifferentKinds_AndTheSameForTheSameDay()
        {
            for (int day = 0; day < 200; day++)
            {
                Mission[] a = DailyRules.MissionsFor(day, 20), b = DailyRules.MissionsFor(day, 20);
                Assert.AreEqual(DailyRules.MissionsPerDay, a.Length);
                var kinds = new HashSet<MissionKind>();
                for (int i = 0; i < a.Length; i++)
                {
                    Assert.IsTrue(kinds.Add(a[i].Kind), $"ziua {day}: misiuni repetate");
                    Assert.AreEqual(a[i].Kind, b[i].Kind);
                    Assert.AreEqual(a[i].Target, b[i].Target);
                    Assert.Greater(a[i].Target, 0);
                    Assert.Greater(a[i].Reward, 0);
                }
            }
        }

        [Test]
        public void NewPlayers_GetNoObstacleMissions()
        {
            for (int day = 0; day < 100; day++)
                foreach (Mission mission in DailyRules.MissionsFor(day, 1))
                    Assert.AreNotEqual(MissionKind.BreakObstacles, mission.Kind);
        }

        [Test]
        public void EveryMissionText_IsTranslated_AndHasItsTarget()
        {
            foreach (MissionKind kind in Enum.GetValues(typeof(MissionKind)))
            {
                string key = new Mission(kind, 5, 10).TextKey;
                Assert.AreNotEqual(key, SugarLoc.Translate(key, SugarLanguage.English), "Lipsește traducerea: " + key);
                StringAssert.Contains("{0}", key);
            }
        }
    }
}
