using System.Collections.Generic;
using UnityEngine;

namespace TollRoadTycoon.Presentation
{
    /// <summary>
    /// Looks up authored art in Assets/Resources/Art (see ASSETS.md for the file list).
    /// Every caller falls back to the procedural placeholder when a file is missing, so art can be
    /// dropped in piece by piece without code changes.
    /// </summary>
    public static class ArtLibrary
    {
        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
        private static readonly Dictionary<string, List<Sprite>> Variants = new Dictionary<string, List<Sprite>>();
        private static Font _font;
        private static bool _fontLoaded;

        /// <summary>Sprite at Resources/Art/{path}, or null.</summary>
        public static Sprite Get(string path)
        {
            if (Sprites.TryGetValue(path, out var sprite)) return sprite;
            sprite = Resources.Load<Sprite>("Art/" + path);
            Sprites[path] = sprite;
            return sprite;
        }

        /// <summary>All sprites named {path}, {path}_1, {path}_2 … (colour or shape variants).</summary>
        public static List<Sprite> GetVariants(string path)
        {
            if (Variants.TryGetValue(path, out var list)) return list;
            list = new List<Sprite>();
            var single = Get(path);
            if (single != null) list.Add(single);
            for (int i = 1; i <= 12; i++)
            {
                var v = Get(path + "_" + i);
                if (v == null) break;
                list.Add(v);
            }
            Variants[path] = list;
            return list;
        }

        /// <summary>Picks a variant deterministically from a seed, or null when none exist.</summary>
        public static Sprite Pick(string path, long seed)
        {
            var list = GetVariants(path);
            if (list.Count == 0) return null;
            return list[(int)((seed % list.Count + list.Count) % list.Count)];
        }

        /// <summary>Game font at Resources/Art/UI/GameFont (TTF/OTF), or null.</summary>
        public static Font Font
        {
            get
            {
                if (_fontLoaded) return _font;
                _fontLoaded = true;
                _font = Resources.Load<Font>("Art/UI/GameFont");
                return _font;
            }
        }

        /// <summary>Scale that makes a sprite's height equal a world length (sprites point up).</summary>
        public static Vector3 FitHeight(Sprite sprite, float worldHeight)
        {
            float h = sprite.bounds.size.y;
            float s = h > 0 ? worldHeight / h : 1f;
            return new Vector3(s, s, 1);
        }
    }
}
