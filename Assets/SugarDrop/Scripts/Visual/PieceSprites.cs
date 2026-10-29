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
        /// <summary>Umbra piesei: după forma desenată din cod, sau o umbră rotundă, moale, dacă piesa e o imagine pictată.</summary>
        public static Sprite Shadow(int color) =>
            color >= 0 && ArtLibrary.Find("piece" + color) != null ? SugarSprites.SoftDot : Get("pieceshadow" + color, () => Render(Size, color, PieceKind.Shadow));

        /// <summary>Dungi albe decupate după forma piesei (capac dungat), orizontale sau verticale.</summary>
        public static Sprite Stripes(int color, bool horizontal) =>
            Get($"piecestripes{color}{horizontal}", () => Render(Size, color, horizontal ? PieceKind.StripesH : PieceKind.StripesV));

        /// <summary>Perlă albă lucioasă (baza capacului curcubeu).</summary>
        public static Sprite Pearl => Get("pearl", () => Render(Size, -1, PieceKind.Body));

        private enum PieceKind { Body, Shadow, StripesH, StripesV }

        /// <summary>Bulinele de pe pălăria ciupercii (x, y, rază) și nervurile laterale ale frunzei.</summary>
        private static readonly Vector3[] MushroomSpots = { new(44f, 82f, 9f), new(72f, 92f, 7f), new(88f, 74f, 8f), new(60f, 70f, 5f), new(34f, 66f, 5f) };
        private static readonly float[] LeafVeins = { -24f, -6f, 12f };

        /// <summary>Direcția luminii (din stânga sus), pentru fațete.</summary>
        private const float LightX = -0.5547f, LightY = 0.8321f;

        private static readonly Color[] Colors =
        {
            // Drop Quest: ciupercă roșie, picătură de apă, frunză, gemă aurie, ametist.
            new(0.93f, 0.26f, 0.24f),
            new(0.22f, 0.6f, 1f),
            new(0.33f, 0.74f, 0.26f),
            new(1f, 0.77f, 0.18f),
            new(0.62f, 0.36f, 0.93f),
        };

        // ------------------------------------------------------------ Forme (distanță semnată, spațiu 128×128)

        private static float Distance(int color, Vector2 p)
        {
            switch (color)
            {
                case 0: return Mathf.Min(MushroomCap(p), MushroomStem(p));
                case 1: return UnevenCapsule(new Vector2(p.x - 64f, p.y - 44f), 38f, 7f, 66f) - 2f;
                case 2: return Leaf(p);
                case 3: return Polygon(new Vector2(p.x - 64f, p.y - 64f), 8, 48f) - 6f;
                case 4: return Polygon(new Vector2(p.x - 64f, (p.y - 64f) / AmethystStretch), 6, 40f) * 1.15f - 6f;
                default: return (p - new Vector2(64f, 64f)).magnitude - 54f;
            }
        }

        private const float AmethystStretch = 1.32f;

        /// <summary>Pălăria ciupercii: o jumătate de elipsă cu marginea de jos rotunjită.</summary>
        private static float MushroomCap(Vector2 p)
        {
            float dome = Mathf.Max(new Vector2((p.x - 64f) / 56f, (p.y - 62f) / 44f).magnitude * 44f - 44f, 52f - p.y);
            return Mathf.Min(dome, RoundBox(new Vector2(p.x - 64f, p.y - 58f), new Vector2(56f, 8f), 8f));
        }

        private static float MushroomStem(Vector2 p) => RoundBox(new Vector2(p.x - 64f, p.y - 36f), new Vector2(17f, 26f), 12f);

        /// <summary>Frunză: lentila dintre două cercuri, înclinată, cu codiță.</summary>
        private static float Leaf(Vector2 p)
        {
            Vector2 q = Rotate(p - new Vector2(66f, 68f), 40f);
            float lens = Mathf.Max((q - new Vector2(30f, 0f)).magnitude - 62f, (q + new Vector2(30f, 0f)).magnitude - 62f);
            return Mathf.Min(lens, RoundBox(q + new Vector2(0f, 58f), new Vector2(3.5f, 10f), 3.5f));
        }

        private static float RoundBox(Vector2 p, Vector2 half, float radius) => Box(p, half - new Vector2(radius, radius)) - radius;

        /// <summary>Poligon regulat cu un vârf în sus (intersecția semiplanelor laturilor).</summary>
        private static float Polygon(Vector2 p, int sides, float radius)
        {
            float d = float.MinValue;
            float apothem = radius * Mathf.Cos(Mathf.PI / sides);
            for (int i = 0; i < sides; i++)
            {
                float a = Mathf.PI * 0.5f + Mathf.PI * 2f * i / sides + Mathf.PI / sides;
                d = Mathf.Max(d, p.x * Mathf.Cos(a) + p.y * Mathf.Sin(a) - apothem);
            }
            return d;
        }

        /// <summary>Centrul vizual al fiecărei forme (pentru lumină).</summary>
        private static Vector2 Center(int color) => color switch
        {
            0 => new Vector2(64f, 70f),
            1 => new Vector2(64f, 56f),
            _ => new Vector2(64f, 64f),
        };

        /// <summary>Gemele au fațete (aurie: 8, ametistul: 6); ciuperca, picătura și frunza sunt netede.</summary>
        private static int Facets(int color) => color switch { 3 => 8, 4 => 6, _ => 0 };

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
            float depth = -d;
            Vector2 v = p - Center(color);
            int facets = color >= 0 && color < Colors.Length ? Facets(color) : 0;
            Color c;
            if (facets == 0)
            {
                // Suprafață netedă și rotunjită (ciupercă, picătură, frunză), luminată din stânga sus.
                float rx = v.x / 52f, ry = v.y / 48f;
                float lambert = Mathf.Clamp((-0.45f * rx + 0.6f * ry) * 0.6f + Mathf.Clamp01(depth / 18f) * 0.6f, 0f, 1.2f);
                c = baseColor * (0.62f + 0.5f * lambert);
                if (color == 0)
                {
                    // Piciorul crem al ciupercii și bulinele albe de pe pălărie.
                    float cap = MushroomCap(p), stem = MushroomStem(p);
                    if (stem < cap && stem < 0.5f) c = new Color(1f, 0.94f, 0.82f) * (0.8f + 0.25f * Mathf.Clamp01(-stem / 12f));
                    foreach (Vector3 spot in MushroomSpots)
                    {
                        float s = Mathf.Clamp01(0.5f - ((p - (Vector2)spot).magnitude - spot.z)) * (cap < -2f ? 1f : 0f);
                        c = Color.Lerp(c, new Color(1f, 0.97f, 0.93f), s);
                    }
                }
                else if (color == 1)
                {
                    // Apa: lumină în adânc, jos.
                    float glow = Mathf.Clamp01(1f - (p - new Vector2(64f, 32f)).magnitude / 30f) * 0.35f;
                    c = Color.Lerp(c, new Color(0.75f, 0.9f, 1f), glow);
                }
                else if (color == 2)
                {
                    // Nervurile frunzei: mijlocul și câteva laterale.
                    Vector2 q = Rotate(p - new Vector2(66f, 68f), 40f);
                    float vein = Mathf.Clamp01(1f - Mathf.Abs(q.x) / 2.2f) * (Mathf.Abs(q.y) < 44f && depth > 3f ? 1f : 0f);
                    foreach (float k in LeafVeins)
                    {
                        float side = Mathf.Clamp01(1f - Mathf.Abs(Mathf.Abs(q.x) * 0.9f - (q.y - k)) / 1.8f);
                        if (Mathf.Abs(q.x) < 22f && depth > 4f) vein = Mathf.Max(vein, side * 0.7f);
                    }
                    c *= 1f - 0.35f * vein;
                }
            }
            else
            {
                // Gemă cu fațete: o „masă” plată în mijloc și fațete în jur, fiecare luminată după orientarea ei.
                Vector2 f = color == 4 ? new Vector2(v.x, v.y / AmethystStretch) : v;
                float step = Mathf.PI * 2f / facets;
                float angle = Mathf.Atan2(f.y, f.x);
                float middle = (Mathf.Floor((angle + Mathf.PI) / step) + 0.5f) * step - Mathf.PI;
                float facet = Mathf.Cos(middle) * LightX + Mathf.Sin(middle) * LightY;
                float bevel = color == 4 ? 11f : 13f;
                float light = depth > bevel ? 0.2f + 0.12f * (f.y / 60f) : facet * 0.34f;
                c = baseColor * (0.92f + light);
                // O linie fină și luminoasă între masă și fațete.
                float edge = Mathf.Clamp01(1f - Mathf.Abs(depth - bevel) / 1.5f) * 0.28f;
                c = Color.Lerp(c, Color.white, edge);
            }
            // Margine subțire, puțin mai închisă (fără contur gros de desen animat).
            c *= 1f - 0.3f * Mathf.Clamp01(1f - depth / 3f);
            // Luciu oblic sus-stânga și o sclipire mică.
            Vector2 g = Rotate(p - new Vector2(46f, 90f), 35f);
            float gloss = Mathf.Clamp01(1.1f - new Vector2(g.x / 20f, g.y / 9f).magnitude) * Mathf.Clamp01((depth - 4f) / 4f);
            float spark = Mathf.Clamp01(1f - (p - new Vector2(86f, 92f)).magnitude / 4.5f) * Mathf.Clamp01(depth / 6f);
            c = Color.Lerp(c, Color.white, Mathf.Clamp01(SmoothStep(gloss) * 0.75f + spark * 0.9f));
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
            // O imagine PNG cu același nume (Resources/Art) are prioritate față de desenul din cod.
            sprite = ArtLibrary.Find(key) ?? create();
            sprite.name = key;
            Cache[key] = sprite;
            return sprite;
        }
    }
}
