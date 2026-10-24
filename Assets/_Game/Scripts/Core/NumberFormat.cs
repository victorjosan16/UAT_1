namespace QuarryEmpire
{
    /// <summary>Formatare scurtă a numerelor mari: 1.250 → „1.2K”, 3.400.000 → „3.4M”.</summary>
    public static class NumberFormat
    {
        public static string Short(long value)
        {
            if (value < 0) return "-" + Short(-value);
            if (value < 1_000) return value.ToString();
            if (value < 1_000_000) return Trim(value / 1_000d) + "K";
            if (value < 1_000_000_000) return Trim(value / 1_000_000d) + "M";
            return Trim(value / 1_000_000_000d) + "B";
        }

        public static string Money(long value) => "$" + Short(value);

        private static string Trim(double value) =>
            value >= 100 ? ((long)value).ToString() : value.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture);
    }
}
