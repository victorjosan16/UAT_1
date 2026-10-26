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
        /// Opțional: Project ID-ul din cloud.unity.com (Project settings). Gol = cel legat deja de proiect
        /// (build-ul din cloud îl leagă singur). Completează-l doar dacă clasamentul nu se conectează.
        /// </summary>
        public const string ProjectId = "";
    }
}
