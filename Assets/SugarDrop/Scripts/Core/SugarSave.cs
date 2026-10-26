using System;
using System.Collections.Generic;
using UnityEngine;

namespace SugarDrop
{
    /// <summary>Tot progresul jocului (salvat în PlayerPrefs ca JSON).</summary>
    [Serializable]
    public class SugarSaveData
    {
        public int level = 1;
        public int lives = SugarSave.MaxLives;
        /// <summary>Momentul (Unix, secunde) de la care se numără regenerarea următoarei vieți.</summary>
        public long lifeTimestamp;
        public int coins = 100;
        public int bombs = 3;
        public int extraMoves = 3;
        public int shuffles = 3;
        public bool sound = true;
        public bool music = true;
        public bool vibration = true;
        /// <summary>Fără tremurat și zoom de cameră (pentru cine preferă mișcare redusă).</summary>
        public bool reducedMotion;
        /// <summary>„ro”, „en” sau gol (după limba telefonului).</summary>
        public string language = "";
        public bool tutorialDone;
        /// <summary>Consimțământ pentru reclame personalizate: -1 = încă neîntrebat, 0 = nu, 1 = da.</summary>
        public int adsConsent = -1;
        /// <summary>Numele afișat în clasament (gol = încă neales: la prima pornire apare fereastra de bun venit).</summary>
        public string nickname = "";
        /// <summary>Stelele obținute pe fiecare nivel (index 0 = nivelul 1).</summary>
        public List<int> stars = new();
        /// <summary>Cel mai bun scor pe fiecare nivel (index 0 = nivelul 1); suma lor intră în clasament.</summary>
        public List<int> bestScores = new();
        /// <summary>Câte capitole și-au dat deja cufărul de final (capitolele 0..n-1).</summary>
        public int chaptersRewarded;
        /// <summary>Ziua (DailyRules.DayOf) în care s-a revendicat ultimul cadou zilnic și ziua din serie (1–7).</summary>
        public int giftDay = -1;
        public int giftStreak;
        /// <summary>Misiunile zilei: ziua, nivelul jucătorului când au fost alese, progresul și dacă s-au revendicat (0/1).</summary>
        public int missionDay = -1;
        public int missionLevel = 1;
        public List<int> missionProgress = new();
        public List<int> missionClaimed = new();
    }

    /// <summary>Încărcare/salvare + vieți cu regenerare în timp.</summary>
    public static class SugarSave
    {
        public const int MaxLives = 5;
        public const int LifeRegenSeconds = 20 * 60;
        private const string Key = "sugardrop_save";

        private static SugarSaveData _data;

        public static SugarSaveData Data
        {
            get
            {
                if (_data == null) Load();
                return _data;
            }
        }

        public static void Load()
        {
            try
            {
                string json = PlayerPrefs.GetString(Key, "");
                _data = string.IsNullOrEmpty(json) ? new SugarSaveData() : JsonUtility.FromJson<SugarSaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[SugarDrop] Salvarea nu a putut fi citită: " + e.Message);
                _data = new SugarSaveData();
            }
            _data ??= new SugarSaveData();
        }

        public static void Save()
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(Data));
            PlayerPrefs.Save();
        }

        public static long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        /// <summary>Adaugă viețile regenerate de la ultima verificare.</summary>
        public static void UpdateLives()
        {
            SugarSaveData data = Data;
            int before = data.lives;
            (data.lives, data.lifeTimestamp) = LifeClock.Regenerate(data.lives, data.lifeTimestamp, Now, MaxLives, LifeRegenSeconds);
            if (data.lives != before) Save();
        }

        /// <summary>Secunde până la următoarea viață (0 dacă viețile sunt pline).</summary>
        public static int SecondsToNextLife =>
            LifeClock.SecondsToNext(Data.lives, Data.lifeTimestamp, Now, MaxLives, LifeRegenSeconds);

        public static void LoseLife()
        {
            UpdateLives();
            if (Data.lives >= MaxLives) Data.lifeTimestamp = Now;
            Data.lives = Mathf.Max(0, Data.lives - 1);
            Save();
        }

        public static void AddLife()
        {
            Data.lives = Mathf.Min(MaxLives, Data.lives + 1);
            Save();
        }

        public static int GetStars(int level) =>
            level >= 1 && level <= Data.stars.Count ? Data.stars[level - 1] : 0;

        public static void SetStars(int level, int stars)
        {
            while (Data.stars.Count < level) Data.stars.Add(0);
            Data.stars[level - 1] = Mathf.Max(Data.stars[level - 1], stars);
        }

        public static int GetBestScore(int level) =>
            level >= 1 && level <= Data.bestScores.Count ? Data.bestScores[level - 1] : 0;

        /// <summary>Păstrează scorul dacă e mai mare decât recordul nivelului; true = record nou.</summary>
        public static bool SetBestScore(int level, int score)
        {
            if (level < 1 || score <= GetBestScore(level)) return false;
            while (Data.bestScores.Count < level) Data.bestScores.Add(0);
            Data.bestScores[level - 1] = score;
            return true;
        }

        /// <summary>Punctele pentru clasament: suma celor mai bune scoruri de pe toate nivelurile.</summary>
        public static long TotalScore
        {
            get
            {
                long total = 0;
                foreach (int score in Data.bestScores) total += score;
                return total;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _data = null;
    }
}
