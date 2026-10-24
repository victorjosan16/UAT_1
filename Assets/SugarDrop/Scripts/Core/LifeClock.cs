namespace SugarDrop
{
    /// <summary>
    /// Calculul pur al regenerării vieților (fără PlayerPrefs sau ceas real), ca să poată fi testat.
    /// „timestamp” = momentul (Unix, secunde) de la care se numără următoarea viață.
    /// </summary>
    public static class LifeClock
    {
        /// <summary>Viețile după ce a trecut timpul până la „now” (și noul moment de referință).</summary>
        public static (int lives, long timestamp) Regenerate(int lives, long timestamp, long now, int maxLives, int regenSeconds)
        {
            if (lives >= maxLives) return (lives, now);
            long elapsed = now - timestamp;
            if (elapsed < regenSeconds) return (lives, timestamp);
            int gained = (int)(elapsed / regenSeconds);
            int result = lives + gained >= maxLives ? maxLives : lives + gained;
            return (result, timestamp + gained * (long)regenSeconds);
        }

        /// <summary>Secunde până la următoarea viață (0 dacă viețile sunt pline).</summary>
        public static int SecondsToNext(int lives, long timestamp, long now, int maxLives, int regenSeconds)
        {
            if (lives >= maxLives) return 0;
            long left = regenSeconds - (now - timestamp);
            return left < 0 ? 0 : (int)left;
        }
    }
}
