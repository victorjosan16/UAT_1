using System;
using System.Globalization;

namespace TollRoadTycoon.Core
{
    /// <summary>
    /// Offline income: estimated earnings per second × time away, capped (8 h by default).
    /// Invalid, missing or future timestamps give nothing.
    /// </summary>
    public static class OfflineEarnings
    {
        public static double ElapsedSeconds(string lastSeenUtc, DateTime nowUtc, double capHours)
        {
            if (string.IsNullOrEmpty(lastSeenUtc)) return 0;
            if (!DateTime.TryParse(lastSeenUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var last)) return 0;
            double seconds = (nowUtc.ToUniversalTime() - last.ToUniversalTime()).TotalSeconds;
            if (double.IsNaN(seconds) || seconds <= 0) return 0;
            return Math.Min(seconds, capHours * 3600);
        }

        public static long Compute(double centsPerSecond, double elapsedSeconds)
        {
            if (centsPerSecond <= 0 || elapsedSeconds <= 0) return 0;
            double cents = centsPerSecond * elapsedSeconds;
            return cents >= long.MaxValue / 4 ? long.MaxValue / 4 : (long)Math.Floor(cents);
        }
    }
}
