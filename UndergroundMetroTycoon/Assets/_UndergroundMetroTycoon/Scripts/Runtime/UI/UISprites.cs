using UnityEngine;

namespace UndergroundMetroTycoon.Game
{
    /// <summary>Generates the few sprites the UI needs (rounded panels, circles, coin) at runtime.</summary>
    public static class UISprites
    {
        private static Sprite _rounded;
        private static Sprite _circle;
        private static Sprite _coin;

        /// <summary>White rounded rectangle, 9-sliced; tint with Image.color.</summary>
        public static Sprite Rounded => _rounded != null ? _rounded : _rounded = CreateRounded(96, 40);
        public static Sprite Circle => _circle != null ? _circle : _circle = CreateCircle(128);
        public static Sprite Coin => _coin != null ? _coin : _coin = CreateCoin(128);

        private static Sprite CreateRounded(int size, int radius)
        {
            Texture2D tex = NewTexture(size);
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                    float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    byte a = (byte)(Mathf.Clamp01(radius - d + 0.5f) * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, a);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                new Vector4(radius, radius, radius, radius));
        }

        private static Sprite CreateCircle(int size)
        {
            Texture2D tex = NewTexture(size);
            var pixels = new Color32[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(r - d) * 255f));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Sprite CreateCoin(int size)
        {
            Texture2D tex = NewTexture(size);
            var pixels = new Color32[size * size];
            float r = size * 0.5f;
            Color rim = new Color(0.85f, 0.55f, 0.05f);
            Color face = new Color(1f, 0.78f, 0.12f);
            Color shine = new Color(1f, 0.92f, 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float d = Vector2.Distance(p, new Vector2(r, r));
                    float alpha = Mathf.Clamp01(r - d);
                    Color c = d > r * 0.8f ? rim : face;
                    if (d < r * 0.62f && d > r * 0.5f) c = rim;
                    if (Vector2.Distance(p, new Vector2(r * 0.72f, r * 1.3f)) < r * 0.16f) c = shine;
                    c.a = alpha;
                    pixels[y * size + x] = c;
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        private static Texture2D NewTexture(int size) =>
            new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
    }
}
