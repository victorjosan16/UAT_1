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
        public bool musicOn = true;
        /// <summary>„ro” sau „en”; gol = după limba telefonului.</summary>
        public string language = "";
        public long lastSaveUnixSeconds;

        /// <summary>Conținutul stivelor (jucător, camion, muncitori) și al rafinăriei, ca id-uri de resurse.</summary>
        public List<StringListEntry> stacks = new();
        /// <summary>Stratul curent: seed-ul grilei și celulele deja sparte (ca stratul să nu se regenereze la repornire).</summary>
        public int layerSeed;
        public List<int> minedCells = new();
        /// <summary>Pasul tutorialului (0 = început, 3 = terminat).</summary>
        public int tutorialStep;

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

        public static List<string> GetList(List<StringListEntry> list, string key)
        {
            foreach (var entry in list)
                if (entry.key == key) return entry.items;
            return null;
        }

        public static void SetList(List<StringListEntry> list, string key, List<string> items)
        {
            foreach (var entry in list)
            {
                if (entry.key != key) continue;
                entry.items.Clear();
                entry.items.AddRange(items);
                return;
            }
            list.Add(new StringListEntry { key = key, items = new List<string>(items) });
        }

        public bool IsUnlocked(string id) => unlocked.Contains(id);

        public void Unlock(string id)
        {
            if (!unlocked.Contains(id)) unlocked.Add(id);
        }
    }

    [Serializable]
    public class StringListEntry
    {
        public string key;
        public List<string> items = new();
    }

    [Serializable]
    public class IntEntry
    {
        public string key;
        public int value;
    }
}
