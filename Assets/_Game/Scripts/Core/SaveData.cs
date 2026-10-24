using System;
using System.Collections.Generic;

namespace QuarryEmpire
{
    /// <summary>
    /// Tot ce se salvează pe disc. JsonUtility nu știe Dictionary, deci folosim liste de perechi.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public int version = 1;
        public long money;
        public int layerIndex;
        public List<IntEntry> upgradeLevels = new();
        public List<IntEntry> zonePaid = new();
        public List<string> unlocked = new();
        public bool soundOn = true;
        public bool vibrationOn = true;
        public long lastSaveUnixSeconds;

        public static int GetInt(List<IntEntry> list, string key)
        {
            foreach (var entry in list)
                if (entry.key == key) return entry.value;
            return 0;
        }

        public static void SetInt(List<IntEntry> list, string key, int value)
        {
            foreach (var entry in list)
            {
                if (entry.key != key) continue;
                entry.value = value;
                return;
            }
            list.Add(new IntEntry { key = key, value = value });
        }

        public bool IsUnlocked(string id) => unlocked.Contains(id);

        public void Unlock(string id)
        {
            if (!unlocked.Contains(id)) unlocked.Add(id);
        }
    }

    [Serializable]
    public class IntEntry
    {
        public string key;
        public int value;
    }
}
