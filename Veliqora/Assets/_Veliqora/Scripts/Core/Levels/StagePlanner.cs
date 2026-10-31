using System;
using Veliqora.Core.Random;

namespace Veliqora.Core.Levels
{
    public enum LevelKind { Normal = 0, Hard = 1, Boss = 2 }

    /// <summary>What the planner decided for one level number.</summary>
    public struct LevelPlan
    {
        public int Number;
        public int Stage;
        public LevelKind Kind;
        /// <summary>Random color offset applied to the curve's base color count (normal levels: -1..+1).</summary>
        public int ColorDelta;
        /// <summary>Extra colors for Hard/Boss levels.</summary>
        public int HardBonusColors;
        /// <summary>0..1: which candidate to pick after sorting by difficulty (1 = hardest).</summary>
        public double PickQuantile;
    }

    /// <summary>
    /// Stage-based pacing with deterministic randomness, so difficulty is not a straight line:
    /// - the game is split into stages of 10 levels; the last level of every stage (20, 30, …) is a BOSS level;
    /// - from level 12 on, about 18% of the other levels are randomly HARD (never two hard levels in a row);
    /// - normal levels randomly vary by ±1 color and pick a random (easy-to-medium) candidate.
    /// Levels 1–10 are the handcrafted tutorial stage. The same seed always yields the same plan.
    /// </summary>
    public static class StagePlanner
    {
        public const int StageSize = 10;
        public const int FirstRandomHardLevel = 12;
        public const double HardChance = 0.18;

        public static int StageOf(int number) => (number - 1) / StageSize + 1;

        public static bool IsBossNumber(int number) => number > StageSize && number % StageSize == 0;

        public static LevelPlan Plan(ulong baseSeed, int number)
        {
            var plan = new LevelPlan { Number = number, Stage = StageOf(number), Kind = LevelKind.Normal, PickQuantile = 0.5 };
            if (number <= HandcraftedLevels.All.Length) return plan;

            var rng = new DeterministicRandom(DeterministicRandom.Combine(baseSeed, number, 0x57A6E));
            if (IsBossNumber(number))
            {
                plan.Kind = LevelKind.Boss;
                plan.HardBonusColors = 2;
                plan.PickQuantile = 1.0;
                return plan;
            }
            if (number >= FirstRandomHardLevel && RawHard(baseSeed, number) && !RawHardOrBoss(baseSeed, number - 1))
            {
                plan.Kind = LevelKind.Hard;
                plan.HardBonusColors = 1;
                plan.PickQuantile = 1.0;
                return plan;
            }
            // Normal: random wobble so consecutive levels don't feel like a strict ramp.
            double roll = rng.NextDouble();
            plan.ColorDelta = roll < 0.2 ? -1 : (roll > 0.85 ? 1 : 0);
            plan.PickQuantile = rng.NextDouble() * 0.7; // easy..medium candidates
            return plan;
        }

        /// <summary>Independent coin flip for "hard" (before the no-two-in-a-row rule).</summary>
        private static bool RawHard(ulong baseSeed, int number)
        {
            if (number < FirstRandomHardLevel || IsBossNumber(number)) return false;
            var r = new DeterministicRandom(DeterministicRandom.Combine(baseSeed, number, 0x4A2D));
            return r.NextDouble() < HardChance;
        }

        private static bool RawHardOrBoss(ulong baseSeed, int number)
        {
            if (number < FirstRandomHardLevel) return false;
            if (IsBossNumber(number)) return true;
            // A level is hard only if it rolled hard and its predecessor wasn't hard/boss; resolve recursively
            // (bounded: the chain stops at the first non-hard level or at level 11).
            return RawHard(baseSeed, number) && !RawHardOrBoss(baseSeed, number - 1);
        }

        /// <summary>Applies the plan to a curve segment, respecting color caps (9 before Master, 12 in Master).</summary>
        public static int ColorsFor(LevelPlan plan, int baseColors, int number)
        {
            int cap = DifficultyTiers.ForLevel(number) == DifficultyTier.Master ? 12 : 9;
            int colors = baseColors + plan.ColorDelta + plan.HardBonusColors;
            return Math.Max(3, Math.Min(cap, colors));
        }
    }
}
