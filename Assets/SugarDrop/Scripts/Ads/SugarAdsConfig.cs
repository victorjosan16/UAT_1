namespace SugarDrop
{
    /// <summary>
    /// Cheile Unity LevelPlay (din dashboard: unity.com → Grow → LevelPlay → aplicația Sugar Drop).
    /// Cât timp sunt goale (sau pachetul LevelPlay nu e instalat), jocul folosește reclama de TEST.
    /// Vezi Assets/SugarDrop/RECLAME.md.
    /// </summary>
    public static class SugarAdsConfig
    {
        /// <summary>App Key-ul aplicației Android.</summary>
        public const string AndroidAppKey = "";
        /// <summary>ID-ul unității de reclamă Rewarded pentru Android (Ad Unit ID).</summary>
        public const string AndroidRewardedAdUnitId = "";

        /// <summary>La fel pentru iOS (dacă jocul va apărea și pe iPhone).</summary>
        public const string IosAppKey = "";
        public const string IosRewardedAdUnitId = "";

        /// <summary>
        /// Câte secunde așteptăm să înceapă o reclamă după ce am cerut-o; dacă nu pornește, jucătorul nu rămâne blocat.
        /// </summary>
        public const float ShowTimeoutSeconds = 6f;
    }
}
