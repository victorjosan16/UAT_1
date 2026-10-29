using System.Collections.Generic;
using UnityEngine;

namespace SugarDrop
{
    /// <summary>
    /// Grafica înlocuibilă: dacă în <c>Assets/SugarDrop/Resources/Art/</c> există un PNG cu numele unui sprite
    /// (ex. <c>piece0.png</c>, <c>sky3.png</c>, <c>crate.png</c>, <c>logo.png</c>), jocul îl folosește în locul
    /// desenului din cod. Lipsește imaginea → rămâne desenul procedural, deci jocul merge cu oricâte imagini.
    /// Doar sprite-urile colorate „finale” se pot înlocui (nu și formele albe care se colorează în joc).
    /// Lista completă, cu mărimi și descrieri: docs/redesign/ART_LIST.md.
    /// </summary>
    public static class ArtLibrary
    {
        public const string Folder = "Art/";

        private static readonly HashSet<string> Replaceable = new()
        {
            "piece0", "piece1", "piece2", "piece3", "piece4", "pearl",
            "sky0", "sky1", "sky2", "sky3", "sky4", "sky5", "sky6", "sky7", "sky8", "sky9", "bggradient",
            "crate", "chocolate", "ice", "chain", "pot", "flame", "bomb", "rocket", "rainbow",
            "bobo", "bobohappy", "boboscared", "bobosad", "bobowave", "bobothumbs", "bobothink", "boboworried",
            "bobosleep", "bobolove", "bobocrystal", "bobostar", "boborun", "bobotoken", "bobotokenscared", "boss0", "boss1", "boss2", "boss3", "boss4", "boss5", "boss6",
            "intro", "introbg", "gift", "trophy", "lifeheart", "flower", "portal", "logo",
        };

        private static readonly Dictionary<string, Sprite> Loaded = new();

        /// <summary>Ilustrațiile scenelor de poveste ale nivelurilor: story1, story2, ... (vezi LevelStory).</summary>
        private static bool IsStoryKey(string key)
        {
            if (!key.StartsWith("story") || key.Length == 5) return false;
            for (int i = 5; i < key.Length; i++)
                if (!char.IsDigit(key[i])) return false;
            return true;
        }

        /// <summary>Imaginea din Resources/Art pentru cheia dată, sau null (atunci se folosește desenul din cod).</summary>
        public static Sprite Find(string key)
        {
            if (!Replaceable.Contains(key) && !IsStoryKey(key)) return null;
            if (Loaded.TryGetValue(key, out Sprite cached)) return cached;
            var texture = Resources.Load<Texture2D>(Folder + key);
            Sprite sprite = null;
            if (texture != null)
            {
                texture.wrapMode = TextureWrapMode.Clamp;
                sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f),
                    Mathf.Max(1f, texture.width / 1.28f), 0, SpriteMeshType.FullRect);
                sprite.name = key;
            }
            Loaded[key] = sprite;
            return sprite;
        }
    }
}
