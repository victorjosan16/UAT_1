using System.Collections.Generic;

namespace VoxelBloom.Core
{
    /// <summary>Idle-animation data exported with a model: neck height, eye cells (blink) and optional tail box.</summary>
    public sealed class AnimProfile
    {
        public int HeadY = -1;
        public int Lid = -1;
        public int[] Eyes = System.Array.Empty<int>();
        /// <summary>x0, y0, z0, x1, y1, z1 (inclusive) or null.</summary>
        public int[] Tail;

        public bool HasHead => HeadY > 0;

        public static AnimProfile FromJson(Dictionary<string, object> d)
        {
            if (d == null) return null;
            var p = new AnimProfile
            {
                HeadY = MiniJson.GetInt(d, "headY", -1),
                Lid = MiniJson.GetInt(d, "lid", -1),
                Eyes = MiniJson.GetIntArray(d, "eyes"),
            };
            var t = MiniJson.GetIntArray(d, "tail");
            p.Tail = t.Length == 6 ? t : null;
            return p;
        }

        public bool InTail(int x, int y, int z)
            => Tail != null && x >= Tail[0] && x <= Tail[3] && y >= Tail[1] && y <= Tail[4] && z >= Tail[2] && z <= Tail[5];
    }

    /// <summary>Home carousel characters: fixed order, starter, unlock rules and paging.</summary>
    public sealed class CharacterCatalog
    {
        public static readonly string[] HomeOrder = { "fox", "panda", "chicken", "bunny", "lion", "cat", "bear", "dog", "penguin", "elephant" };
        public const string Starter = "fox";
        public const int PerPage = 6;

        public readonly List<CollectionEntry> Characters = new List<CollectionEntry>();

        public CharacterCatalog(GameConfig cfg)
        {
            foreach (var id in HomeOrder)
            {
                var e = cfg?.Collection.Find(c => c.Id == id);
                Characters.Add(e ?? new CollectionEntry { Id = id, Name = id, Category = "Animals" });
            }
        }

        public int Count => Characters.Count;
        public int PageCount => (Count + PerPage - 1) / PerPage;
        public int IndexOf(string id) { int i = Characters.FindIndex(c => c.Id == id); return i < 0 ? 0 : i; }
        public static int PageOf(int index) => index / PerPage;

        /// <summary>The starter is always unlocked; others unlock when a level using the model is completed.</summary>
        public bool IsUnlocked(string id, SaveData save) => id == Starter || (save != null && save.CollectionUnlocked.Contains(id));

        /// <summary>Next/previous index with wrap-around.</summary>
        public int Step(int index, int dir) => ((index + dir) % Count + Count) % Count;

        public string UnlockText(CollectionEntry e)
            => e.UnlockLevel > 0 ? "Complete level " + e.UnlockLevel + " to unlock" : "Coming soon";

        /// <summary>Selection that may be persisted: only unlocked characters are saved.</summary>
        public string ResolveSaved(string saved, SaveData save)
            => !string.IsNullOrEmpty(saved) && Characters.Exists(c => c.Id == saved) && IsUnlocked(saved, save) ? saved : Starter;
    }
}
