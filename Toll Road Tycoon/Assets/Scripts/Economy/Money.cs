using System;
using System.Globalization;

namespace TollRoadTycoon.Economy
{
    /// <summary>
    /// Money is stored as a long count of cents (minor units) everywhere in the simulation,
    /// so there is no floating point drift in balances.
    /// </summary>
    public static class Money
    {
        public const long CentsPerDollar = 100;

        public static long FromDollars(long dollars) => checked(dollars * CentsPerDollar);

        /// <summary>Multiplies cents by a basis-point factor (10000 = x1.0) with integer rounding.</summary>
        public static long ApplyBasisPoints(long cents, long basisPoints)
        {
            if (cents <= 0 || basisPoints <= 0) return 0;
            decimal result = (decimal)cents * basisPoints / 10000m;
            return (long)Math.Round(result, MidpointRounding.AwayFromZero);
        }

        /// <summary>Formats cents as "$1,250", "$12.4K", "$3.25M", "$1.1B".</summary>
        public static string Format(long cents)
        {
            bool negative = cents < 0;
            decimal dollars = Math.Abs((decimal)cents) / CentsPerDollar;
            string body;
            if (dollars >= 1_000_000_000m) body = Compact(dollars / 1_000_000_000m) + "B";
            else if (dollars >= 1_000_000m) body = Compact(dollars / 1_000_000m) + "M";
            else if (dollars >= 100_000m) body = Compact(dollars / 1_000m) + "K";
            else if (dollars == Math.Floor(dollars)) body = dollars.ToString("#,0", CultureInfo.InvariantCulture);
            else body = dollars.ToString("#,0.00", CultureInfo.InvariantCulture);
            return (negative ? "-$" : "$") + body;
        }

        private static string Compact(decimal value)
        {
            decimal truncated = value >= 100m
                ? Math.Floor(value)
                : value >= 10m ? Math.Floor(value * 10m) / 10m : Math.Floor(value * 100m) / 100m;
            return truncated.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}
