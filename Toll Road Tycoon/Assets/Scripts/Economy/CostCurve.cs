using System;

namespace TollRoadTycoon.Economy
{
    /// <summary>Cost = BaseCost * GrowthFactor ^ level, rounded to whole dollars.</summary>
    [Serializable]
    public sealed class CostCurve
    {
        public long BaseCostCents;
        public double GrowthFactor;

        public CostCurve(long baseCostCents, double growthFactor)
        {
            BaseCostCents = baseCostCents;
            GrowthFactor = growthFactor;
        }

        public CostCurve Scaled(double factor) => new CostCurve((long)Math.Round(BaseCostCents * factor), GrowthFactor);

        public long CostForLevel(int currentLevel)
        {
            double raw = BaseCostCents * Math.Pow(GrowthFactor, Math.Max(0, currentLevel));
            if (raw >= long.MaxValue / 2) return long.MaxValue / 2;
            long dollars = (long)Math.Round(raw / Money.CentsPerDollar, MidpointRounding.AwayFromZero);
            return Math.Max(1, dollars) * Money.CentsPerDollar;
        }
    }
}
