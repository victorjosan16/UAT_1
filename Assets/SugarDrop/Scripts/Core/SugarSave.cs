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
        /// <summary>Stelele obținute pe fiecare nivel (index 0 = nivelul 1).</summary>
        public List<int> stars = new();
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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _data = null;
    }
}
