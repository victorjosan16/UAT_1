using System;
using System.Collections.Generic;
using UnityEngine;

namespace SugarDrop
{
    /// <summary>
    /// Piesele de pe tablă, desenate din cod ca bomboane de jeleu lucioase: fiecare aromă are forma ei
    /// (căpșună = inimă, afină = picătură, mentă = hexagon, lămâie = stea, strugure = drajeu oval),
    /// cu margine mai închisă, lumină reflectată jos și un luciu alb sus. Sprite-urile sunt deja colorate
    /// (nu se mai colorează prin SpriteRenderer.color), ca luciul să rămână alb.
    /// </summary>
    public static class PieceSprites
    {
        private const int Size = 128;
        private static readonly Dictionary<string, Sprite> Cache = new();

        /// <summary>Piesa colorată pentru o aromă.</summary>
        public static Sprite Piece(int color) => Get("piece" + color, () => Render(Size, color, PieceKind.Body));

        /// <summary>Umbra moale a piesei (albă; se colorează).</summary>
        public static Sprite Shadow(int color) => Get("pieceshadow" + color, () => Render(Size, color, PieceKind.Shadow));

        /// <summary>Dungi albe decupate după forma piesei (capac dungat), orizontale sau verticale.</summary>
        public static Sprite Stripes(int color, bool horizontal) =>
            Get($"piecestripes{color}{horizontal}", () => Render(Size, color, horizontal ? PieceKind.StripesH : PieceKind.StripesV));

        /// <summary>Perlă albă lucioasă (baza capacului curcubeu).</summary>
        public static Sprite Pearl => Get("pearl", () => Render(Size, -1, PieceKind.Body));

        /// <summary>Doar pixelii (pentru unelte, ex. generarea iconiței), la orice mărime.</summary>
        public static Color[] Pixels(int size, int color, out int width)
        {
            width = size;
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = Shade(x, y, size, color, PieceKind.Body);
            return pixels;
        }

        private enum PieceKind { Body, Shadow, StripesH, StripesV }

        private static readonly Color[] Colors =
        {
            new(1f, 0.2f, 0.32f),    // căpșună
            new(0.12f, 0.56f, 1f),   // afină
            new(0.18f, 0.8f, 0.31f), // mentă
            new(1f, 0.76f, 0.05f),   // lămâie
            new(0.62f, 0.3f, 1f),    // strugure
        };

        // ------------------------------------------------------------ Forme (distanță semnată, spațiu 128×128)

        private static float Distance(int color, Vector2 p)
        {
            switch (color)
            {
                case 0: return HeartShape(p);
                case 1: return UnevenCapsule(new Vector2(p.x - 64f, p.y - 46f), 38f, 7f, 66f) - 2f;
                case 2: return Hexagon(new Vector2(p.x - 64f, p.y - 64f), 46f) - 10f;
                case 3: return Star5(new Vector2(p.x - 64f, p.y - 60f), 54f, 0.5f) - 8f;
                case 4: return Ellipse(Rotate(new Vector2(p.x - 64f, p.y - 64f), 28f), 56f, 40f);
                default: return (p - new Vector2(64f, 64f)).magnitude - 54f;
            }
        }

        /// <summary>Inimă exactă: un pătrat rotit cu 45° și două cercuri pe laturile de sus, rotunjite.</summary>
        private static float HeartShape(Vector2 p)
        {
            Vector2 q = Rotate(new Vector2(p.x - 64f, p.y - 58f), -45f);
            const float half = 27f;
            float square = Box(q, new Vector2(half, half));
            float c1 = (q - new Vector2(0f, half)).magnitude - half;
            float c2 = (q - new Vector2(half, 0f)).magnitude - half;
            return Mathf.Min(square, Mathf.Min(c1, c2)) - 6f;
        }

        private static float Box(Vector2 p, Vector2 b)
        {
            Vector2 d = new(Mathf.Abs(p.x) - b.x, Mathf.Abs(p.y) - b.y);
            return new Vector2(Mathf.Max(d.x, 0f), Mathf.Max(d.y, 0f)).magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0f);
        }

        /// <summary>Picătură: un cerc mare jos care se îngustează lin spre un vârf rotunjit sus.</summary>
        private static float UnevenCapsule(Vector2 p, float r1, float r2, float h)
        {
            p.x = Mathf.Abs(p.x);
            float b = (r1 - r2) / h;
            float a = Mathf.Sqrt(1f - b * b);
            float k = Vector2.Dot(p, new Vector2(-b, a));
            if (k < 0f) return p.magnitude - r1;
            if (k > a * h) return (p - new Vector2(0f, h)).magnitude - r2;
            return Vector2.Dot(p, new Vector2(a, b)) - r1;
        }

        private static float Hexagon(Vector2 p, float r)
        {
            const float kx = -0.866025404f, ky = 0.5f, kz = 0.577350269f;
            p = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y));
            float dot = Mathf.Min(kx * p.x + ky * p.y, 0f);
            p -= 2f * dot * new Vector2(kx, ky);
            p -= new Vector2(Mathf.Clamp(p.x, -kz * r, kz * r), r);
            return p.magnitude * Mathf.Sign(p.y);
        }

        private static float Star5(Vector2 p, float r, float rf)
        {
            var k1 = new Vector2(0.809016994375f, -0.587785252292f);
            var k2 = new Vector2(-k1.x, k1.y);
            p.x = Mathf.Abs(p.x);
            p -= 2f * Mathf.Max(Vector2.Dot(k1, p), 0f) * k1;
            p -= 2f * Mathf.Max(Vector2.Dot(k2, p), 0f) * k2;
            p.x = Mathf.Abs(p.x);
            p.y -= r;
            Vector2 ba = rf * new Vector2(-k1.y, k1.x) - new Vector2(0f, 1f);
            float h = Mathf.Clamp(Vector2.Dot(p, ba) / Vector2.Dot(ba, ba), 0f, r);
            return (p - ba * h).magnitude * Mathf.Sign(p.y * ba.x - p.x * ba.y);
        }

        private static float Ellipse(Vector2 p, float a, float b) =>
            (new Vector2(p.x / a, p.y / b).magnitude - 1f) * Mathf.Min(a, b);

        private static Vector2 Rotate(Vector2 p, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            float c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(p.x * c - p.y * s, p.x * s + p.y * c);
        }

        // ------------------------------------------------------------ Umbrire

        private static Color Shade(int x, int y, int size, int color, PieceKind kind)
        {
            float scale = 128f / size;
            var p = new Vector2((x + 0.5f) * scale, (y + 0.5f) * scale);
            float d = Distance(color, p);
            float pixel = scale; // un pixel în spațiul 128
            float alpha = Mathf.Clamp01(0.5f - d / pixel);

            switch (kind)
            {
                case PieceKind.Shadow:
                    return new Color(1f, 1f, 1f, Mathf.Clamp01(0.5f - d / 7f));
                case PieceKind.StripesH:
                case PieceKind.StripesV:
                {
                    float coord = kind == PieceKind.StripesH ? p.y : p.x;
                    float band = Mathf.Clamp01(4.5f - Mathf.Abs(Mathf.Repeat(coord, 24f) - 12f)) ;
                    float inner = Mathf.Clamp01((-d - 5f) / 3f);
                    return new Color(1f, 1f, 1f, band * inner * 0.9f);
                }
            }

            Color baseColor = color >= 0 && color < Colors.Length ? Colors[color] : new Color(0.97f, 0.95f, 1f);
            float depth = Mathf.Clamp01(-d / 16f);
            // Margine mai închisă (contur de jeleu) care se luminează spre interior.
            float body = Mathf.Lerp(0.6f, 1f, SmoothStep(depth));
            // Lumină de sus, ușor mai închis jos.
            float vertical = Mathf.Lerp(0.88f, 1.06f, p.y / 128f);
            Color c = baseColor * (body * vertical);
            // Lumină reflectată jos (jeleul pare translucid).
            float bounce = Mathf.Clamp01(1f - (p - new Vector2(64f, 22f)).magnitude / 34f) * depth;
            c = Color.Lerp(c, Color.Lerp(baseColor, Color.white, 0.45f), bounce * 0.45f);
            // Luciul mare, oblic, sus-stânga, și o sclipire mică.
            Vector2 g = Rotate(p - new Vector2(46f, 88f), 35f);
            float gloss = Mathf.Clamp01(1.15f - new Vector2(g.x / 26f, g.y / 12f).magnitude) * Mathf.Clamp01((-d - 3f) / 4f);
            float spark = Mathf.Clamp01(1f - (p - new Vector2(84f, 92f)).magnitude / 5f) * Mathf.Clamp01(-d / 6f);
            c = Color.Lerp(c, Color.white, Mathf.Clamp01(SmoothStep(gloss) * 0.85f + spark * 0.9f));
            c.a = alpha;
            return c;
        }

        private static float SmoothStep(float t) => t * t * (3f - 2f * t);

        private static Sprite Render(int size, int color, PieceKind kind)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
            };
            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    pixels[y * size + x] = Shade(x, y, size, color, kind);
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }

        private static Sprite Get(string key, Func<Sprite> create)
        {
            if (Cache.TryGetValue(key, out Sprite sprite) && sprite != null) return sprite;
            sprite = create();
            sprite.name = key;
            Cache[key] = sprite;
            return sprite;
        }
    }
}
