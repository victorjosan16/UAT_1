using System;
using System.Collections.Generic;
using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Sprite-uri generate din cod (panouri cu colțuri rotunjite, cercuri, inele și iconițe simple),
    /// cu margini netede (anti-aliasing). Se creează o singură dată și se refolosesc.
    /// Pot fi înlocuite oricând cu grafică desenată în Illustrator, din UiTheme.
    /// </summary>
    public static class ProceduralSprites
    {
        private static readonly Dictionary<string, Sprite> Cache = new();

        /// <summary>Dreptunghi cu colțuri rotunjite, 9-slice (se întinde fără să deformeze colțurile).</summary>
        public static Sprite RoundedRect => Get("rounded", () =>
        {
            const int size = 96;
            const float radius = 32f;
            return Make("RoundedRect", size, (x, y) => RoundedRectDistance(x, y, size, size, radius), new Vector4(40, 40, 40, 40));
        });

        public static Sprite Circle => Get("circle", () => Make("Circle", 128, (x, y) => Vector2.Distance(new Vector2(x, y), new Vector2(64, 64)) - 62f));

        public static Sprite Ring => Get("ring", () => Make("Ring", 128, (x, y) =>
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(64, 64));
            return Mathf.Abs(d - 54f) - 8f;
        }));

        /// <summary>Monedă: disc auriu cu un inel interior mai deschis (culoarea vine din Image.color).</summary>
        public static Sprite Coin => Get("coin", () => MakeColored("Coin", 128, (x, y) =>
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(64, 64));
            float outer = Mathf.Clamp01(62f - d + 0.5f);
            float rim = Mathf.Clamp01(Mathf.Abs(d - 44f) - 5f);
            float shade = Mathf.Lerp(0.78f, 1f, rim);
            return new Color(1f, 0.82f * shade + 0.1f, 0.25f * shade, outer);
        }));

        public static Sprite Pause => Get("pause", () => Make("Pause", 128, (x, y) =>
            Mathf.Min(RoundedRectDistance(x - 30, y - 24, 26, 80, 10), RoundedRectDistance(x - 72, y - 24, 26, 80, 10))));

        public static Sprite Play => Get("play", () => Make("Play", 128, (x, y) =>
        {
            // Triunghi spre dreapta, cu colțuri ușor rotunjite.
            Vector2 p = new(x - 64, y - 64);
            float d1 = Vector2.Dot(p - new Vector2(-28, 40), new Vector2(-1, 0));
            float d2 = Vector2.Dot(p - new Vector2(40, 0), new Vector2(0.5f, 0.866f));
            float d3 = Vector2.Dot(p - new Vector2(40, 0), new Vector2(0.5f, -0.866f));
            return Mathf.Max(d1, Mathf.Max(d2, d3)) + 4f;
        }));

        public static Sprite Gem => Get("gem", () => Make("Gem", 128, (x, y) =>
            Mathf.Abs(x - 64) * 0.9f + Mathf.Abs(y - 64) - 56f));

        public static Sprite Crate => Get("crate", () => Make("Crate", 128, (x, y) =>
        {
            float box = RoundedRectDistance(x - 16, y - 20, 96, 88, 12);
            float slot = Mathf.Abs(y - 64) - 5f; // fanta orizontală din mijloc
            return Mathf.Max(box, -slot);
        }));

        public static Sprite ArrowUp => Get("arrowup", () => Make("ArrowUp", 128, (x, y) =>
        {
            float shaft = RoundedRectDistance(x - 50, y - 14, 28, 60, 8);
            Vector2 p = new(x - 64, y - 64);
            float head = Mathf.Max(Mathf.Abs(p.x) * 0.9f + (p.y - 50f), -(p.y - 4f));
            return Mathf.Min(shaft, head);
        }));

        /// <summary>Săgeată circulară („reîncepe”).</summary>
        public static Sprite Restart => Get("restart", () => Make("Restart", 128, (x, y) =>
        {
            Vector2 p = new(x - 64, y - 64);
            float angle = Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg; // -180..180, 0 = dreapta
            float ring = Mathf.Abs(p.magnitude - 40f) - 9f;
            // Golul din inel (sus-dreapta), unde stă vârful săgeții.
            if (angle > 20f && angle < 80f) ring = Mathf.Max(ring, 4f);
            // Vârful săgeții: triunghi la capătul inelului (spre 80°).
            Vector2 tip = new(Mathf.Cos(80f * Mathf.Deg2Rad) * 40f, Mathf.Sin(80f * Mathf.Deg2Rad) * 40f);
            Vector2 q = p - tip;
            float head = Mathf.Max(Mathf.Abs(q.y) * 0.95f + q.x * 0.6f - 18f, -q.x - 10f);
            return Mathf.Min(ring, head);
        }));

        /// <summary>Stea cu 5 colțuri (rezultatul nivelului).</summary>
        public static Sprite Star => Get("star", () => Make("Star", 128, (x, y) =>
        {
            Vector2 p = new(x - 64, y - 60);
            float angle = Mathf.Atan2(p.x, p.y); // 0 = sus
            float sector = Mathf.Repeat(angle, Mathf.PI * 2f / 5f) / (Mathf.PI * 2f / 5f); // 0..1 între două vârfuri
            float radius = Mathf.Lerp(58f, 26f, 1f - Mathf.Abs(sector * 2f - 1f));
            return (p.magnitude - radius) * 0.8f;
        }));

        // ------------------------------------------------------------ Implementare

        private static Sprite Get(string key, Func<Sprite> create)
        {
            if (Cache.TryGetValue(key, out Sprite sprite) && sprite != null) return sprite;
            sprite = create();
            Cache[key] = sprite;
            return sprite;
        }

        /// <summary>Distanța semnată până la un dreptunghi rotunjit (negativ = în interior).</summary>
        private static float RoundedRectDistance(float x, float y, float width, float height, float radius)
        {
            float qx = Mathf.Abs(x + 0.5f - width * 0.5f) - (width * 0.5f - radius);
            float qy = Mathf.Abs(y + 0.5f - height * 0.5f) - (height * 0.5f - radius);
            float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
            return outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
        }

        /// <summary>Sprite alb cu transparență după o funcție de distanță (alpha netezit pe 1 pixel).</summary>
        private static Sprite Make(string name, int size, Func<int, int, float> distance, Vector4 border = default)
        {
            return MakeColored(name, size, (x, y) => new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - distance(x, y))), border);
        }

        private static Sprite MakeColored(string name, int size, Func<int, int, Color> pixel, Vector4 border = default)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = pixel(x, y);
            texture.SetPixels32(pixels);
            texture.Apply(false, true); // nu mai e nevoie de copia din RAM

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, border);
            sprite.name = name;
            return sprite;
        }
    }
}
