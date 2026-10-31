using System.Collections.Generic;
using UnityEngine;

namespace TollRoadTycoon.Presentation
{
    /// <summary>
    /// Generates the interim art procedurally (white shapes tinted at runtime), so the project has
    /// coherent visuals without imported image assets. Replace with authored sprites later.
    /// </summary>
    public static class SpriteFactory
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
        private static Font _font;

        /// <summary>1x1 world unit white square.</summary>
        public static Sprite Square => Get("square", () => Make(BuildRounded(8, 8, 0), 8, Vector4.zero));

        /// <summary>1x1 world unit white circle.</summary>
        public static Sprite Circle => Get("circle", () => Make(BuildCircle(64), 64, Vector4.zero));

        /// <summary>1x1 world unit rounded rectangle used for vehicle bodies and buildings.</summary>
        public static Sprite RoundedBody => Get("body", () => Make(BuildRounded(32, 32, 9), 32, Vector4.zero));

        /// <summary>9-sliced rounded panel for UI (border 24px).</summary>
        public static Sprite Panel => ArtLibrary.Get("UI/panel") ?? Get("panel", () => Make(BuildRounded(64, 64, 22), 100, new Vector4(24, 24, 24, 24)));

        /// <summary>9-sliced pill/button shape.</summary>
        public static Sprite Pill => ArtLibrary.Get("UI/button") ?? Get("pill", () => Make(BuildRounded(64, 64, 30), 100, new Vector4(31, 31, 31, 31)));

        /// <summary>Soft round shadow.</summary>
        public static Sprite SoftShadow => Get("shadow", () => Make(BuildSoft(32), 32, Vector4.zero));

        public static Font Font
        {
            get
            {
                if (_font != null) return _font;
                _font = ArtLibrary.Font;
                if (_font != null) return _font;
                // Unity 2022.2+ ships LegacyRuntime.ttf; older versions ship Arial.ttf.
                _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                return _font;
            }
        }

        private static Sprite Get(string key, System.Func<Sprite> create)
        {
            if (Cache.TryGetValue(key, out var sprite) && sprite != null) return sprite;
            sprite = create();
            Cache[key] = sprite;
            return sprite;
        }

        private static Sprite Make(Texture2D tex, float pixelsPerUnit, Vector4 border)
        {
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), pixelsPerUnit, 0,
                SpriteMeshType.FullRect, border);
        }

        private static Texture2D NewTexture(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.DontSave,
            };
            return tex;
        }

        private static Texture2D BuildRounded(int w, int h, float radius)
        {
            var tex = NewTexture(w, h);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, radius, w - radius);
                    float cy = Mathf.Clamp(y + 0.5f, radius, h - radius);
                    float d = radius <= 0 ? 0 : Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy)) - radius;
                    byte a = (byte)(Mathf.Clamp01(0.5f - d) * 255);
                    px[y * w + x] = new Color32(255, 255, 255, a);
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        private static Texture2D BuildCircle(int size)
        {
            var tex = NewTexture(size, size);
            var px = new Color32[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) - (r - 1);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(0.5f - d) * 255));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }

        private static Texture2D BuildSoft(int size)
        {
            var tex = NewTexture(size, size);
            var px = new Color32[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r)) / r;
                    float a = Mathf.Clamp01(1 - d);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * a * 255));
                }
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return tex;
        }
    }
}
