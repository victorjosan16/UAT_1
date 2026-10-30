namespace SugarDrop
{
    /// <summary>
    /// Setările clasamentului online (Unity Gaming Services → Leaderboards). Vezi Assets/SugarDrop/CLASAMENT.md.
    /// </summary>
    public static class SugarOnlineConfig
    {
        /// <summary>ID-ul clasamentului creat în dashboard (cloud.unity.com → Leaderboards).</summary>
        public const string LeaderboardId = "total_score";

        /// <summary>Câți jucători arătăm în clasament (primii N).</summary>
        public const int TopCount = 50;

        /// <summary>
        /// Project ID-ul din cloud.unity.com (Project settings). Depozitul nu conține setările proiectului, iar build-ul
        /// din cloud nu leagă singur proiectul (avertisment „link your Unity project”), așa că îl scriem la build.
        /// Nu e secret.
        /// </summary>
        public const string ProjectId = "26df7d45-39bb-4299-bfeb-f6a4ee3be4a8";
    }
}
