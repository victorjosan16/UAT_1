using System;
using System.Collections.Generic;
using UnityEngine;

namespace SugarDrop
{
    /// <summary>
    /// Toată grafica jocului, desenată din cod (forme cu margini netede). Sprite-urile albe se colorează
    /// prin SpriteRenderer.color / Image.color; cele colorate (curcubeu, biscuit, cupcake) au culorile lor.
    /// Oricare poate fi înlocuit cu un desen propriu din Illustrator, cu același nume.
    /// </summary>
    public static class SugarSprites
    {
        private static readonly Dictionary<string, Sprite> Cache = new();

        // ------------------------------------------------------------ Capace și pictograme

        /// <summary>Capac rotunjit, lucios (alb; se colorează): mai deschis sus, cu o dungă de lumină.</summary>
        public static Sprite Cap => Get("cap", () => Paint(128, (x, y) =>
        {
            float d = RoundRect(x, y, 6, 6, 116, 116, 30);
            float a = Alpha(d);
            float shade = Mathf.Lerp(0.82f, 1f, y / 128f);
            float highlight = Alpha(RoundRect(x, y, 22, 82, 84, 26, 13)) * 0.35f;
            float v = Mathf.Clamp01(shade + highlight);
            return new Color(v, v, v, a);
        }));

        /// <summary>Pătrat rotunjit alb (fundalul unei celule de pe tablă; se colorează).</summary>
        public static Sprite CellMask => Get("cellmask", () => Mask(128, (x, y) => RoundRect(x, y, 6, 6, 116, 116, 30)));

        public static Sprite CapShadow => Get("capshadow", () => Paint(128, (x, y) =>
            new Color(0f, 0f, 0f, Alpha(RoundRect(x, y, 6, 6, 116, 116, 30)))));

        /// <summary>Pictograma unei culori: fiecare aromă are forma ei.</summary>
        public static Sprite Icon(int color) => color switch
        {
            0 => Heart,
            1 => Drop,
            2 => Leaf,
            3 => Star,
            _ => Gem,
        };

        public static Sprite Heart => Get("heart", () => Mask(128, (x, y) => HeartDistance(x, y)));

        /// <summary>Inimă din două cercuri și un vârf (distanță exactă → margini netede).</summary>
        private static float HeartDistance(float x, float y)
        {
            float left = Vector2.Distance(new Vector2(x, y), new Vector2(46f, 78f)) - 24f;
            float right = Vector2.Distance(new Vector2(x, y), new Vector2(82f, 78f)) - 24f;
            float wedge = (Mathf.Abs(x - 64f) * 58f - (y - 20f) * 40f) / 70.4f;
            return Mathf.Min(Mathf.Min(left, right), Mathf.Max(wedge, y - 76f));
        }

        public static Sprite Drop => Get("drop", () => Mask(128, (x, y) =>
        {
            Vector2 p = new(x - 64f, y - 52f);
            float circle = p.magnitude - 34f;
            // Vârful picăturii, în sus.
            float tip = Mathf.Abs(p.x) * 1.15f + (p.y - 72f) * 0.55f;
            return p.y > 0f ? Mathf.Min(circle, Mathf.Max(tip, -p.y)) : circle;
        }));

        public static Sprite Leaf => Get("leaf", () => Mask(128, (x, y) =>
        {
            // Frunză = intersecția a două cercuri, rotită 45°, cu o nervură.
            Vector2 p = Rotate(new Vector2(x - 64f, y - 64f), 45f);
            float a = Vector2.Distance(p, new Vector2(0f, -34f)) - 62f;
            float b = Vector2.Distance(p, new Vector2(0f, 34f)) - 62f;
            float leaf = Mathf.Max(a, b);
            float vein = Mathf.Abs(p.x) - 2f;
            return Mathf.Abs(p.y) < 24f ? Mathf.Max(leaf, -vein) : leaf;
        }));

        public static Sprite Star => Get("star", () => Mask(128, (x, y) =>
        {
            Vector2 p = new(x - 64f, y - 60f);
            float angle = Mathf.Atan2(p.x, p.y);
            float sector = Mathf.Repeat(angle, Mathf.PI * 2f / 5f) / (Mathf.PI * 2f / 5f);
            float radius = Mathf.Lerp(56f, 26f, 1f - Mathf.Abs(sector * 2f - 1f));
            return (p.magnitude - radius) * 0.8f;
        }));

        public static Sprite Gem => Get("gem", () => Paint(128, (x, y) =>
        {
            float d = Mathf.Abs(x - 64f) * 0.95f + Mathf.Abs(y - 64f) - 54f;
            float a = Alpha(d);
            // Fațete: jumătatea stângă mai deschisă.
            float v = x < 64f ? (y > 64f ? 1f : 0.88f) : (y > 64f ? 0.9f : 0.76f);
            return new Color(v, v, v, a);
        }));

        public static Sprite Circle => Get("circle", () => Mask(128, (x, y) => Vector2.Distance(new Vector2(x, y), new Vector2(64f, 64f)) - 60f));

        public static Sprite Ring => Get("ring", () => Mask(128, (x, y) =>
            Mathf.Abs(Vector2.Distance(new Vector2(x, y), new Vector2(64f, 64f)) - 52f) - 8f));

        public static Sprite SoftDot => Get("softdot", () => Paint(64, (x, y) =>
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(32f, 32f)) / 30f;
            return new Color(1f, 1f, 1f, Mathf.Clamp01(1f - d * d));
        }));

        public static Sprite Sparkle => Get("sparkle", () => Mask(64, (x, y) =>
        {
            Vector2 p = new(Mathf.Abs(x - 32f), Mathf.Abs(y - 32f));
            return Mathf.Pow(p.x, 0.5f) + Mathf.Pow(p.y, 0.5f) - 5.2f;
        }));

        public static Sprite Crumb => Get("crumb", () => Mask(32, (x, y) => RoundRect(x, y, 4, 6, 24, 20, 7)));

        public static Sprite Shard => Get("shard", () => Mask(32, (x, y) =>
            Mathf.Max(Mathf.Abs(x - 16f) * 1.8f + (y - 30f) * 0.9f, 4f - y)));

        // ------------------------------------------------------------ Capace speciale și obstacole

        /// <summary>Dungi pentru capacul „dungat” (orizontal; rotit 90° pentru vertical).</summary>
        public static Sprite Stripes => Get("stripes", () => Paint(128, (x, y) =>
        {
            float inside = Alpha(RoundRect(x, y, 10, 10, 108, 108, 26));
            float band = Mathf.Abs(Mathf.Repeat(y, 28f) - 14f) < 5f ? 1f : 0f;
            return new Color(1f, 1f, 1f, inside * band * 0.85f);
        }));

        /// <summary>Ramă pentru capacul „ambalat” (explodează în jur).</summary>
        public static Sprite WrapFrame => Get("wrapframe", () => Mask(128, (x, y) =>
        {
            float outer = RoundRect(x, y, 4, 4, 120, 120, 32);
            float inner = RoundRect(x, y, 18, 18, 92, 92, 22);
            return Mathf.Max(outer, -inner);
        }));

        /// <summary>Capacul curcubeu (colorat, cu spirală).</summary>
        public static Sprite Rainbow => Get("rainbow", () => Paint(128, (x, y) =>
        {
            Vector2 p = new(x - 64f, y - 64f);
            float d = p.magnitude;
            float a = Alpha(d - 58f);
            float angle = Mathf.Atan2(p.y, p.x) / (Mathf.PI * 2f) + 0.5f;
            float hue = Mathf.Repeat(angle + d / 90f, 1f);
            Color c = Color.HSVToRGB(hue, 0.55f, 1f);
            float centre = Alpha(d - 16f);
            c = Color.Lerp(c, Color.white, centre);
            c.a = a;
            return c;
        }));

        /// <summary>Gheață peste capac (semitransparentă, cu reflexii).</summary>
        public static Sprite Ice => Get("ice", () => Paint(128, (x, y) =>
        {
            float a = Alpha(RoundRect(x, y, 2, 2, 124, 124, 30)) * 0.6f;
            float streak = Mathf.Abs((x - y) * 0.7071f + 20f) < 6f || Mathf.Abs((x - y) * 0.7071f - 18f) < 3f ? 0.35f : 0f;
            float rim = Alpha(Mathf.Abs(RoundRect(x, y, 2, 2, 124, 124, 30) + 4f) - 3f) * 0.4f;
            return new Color(0.85f, 0.95f, 1f, Mathf.Clamp01(a + streak + rim));
        }));

        /// <summary>Biscuitul (obstacol): rotunjit, auriu, cu bucăți de ciocolată.</summary>
        public static Sprite Biscuit => Get("biscuit", () => Paint(128, (x, y) =>
        {
            float d = RoundRect(x, y, 6, 6, 116, 116, 34);
            float a = Alpha(d);
            var dough = new Color(0.93f, 0.72f, 0.42f);
            var edge = new Color(0.78f, 0.52f, 0.27f);
            Color c = Color.Lerp(dough, edge, Mathf.Clamp01((d + 14f) / 14f));
            Vector2[] chips = { new(38, 40), new(84, 52), new(52, 86), new(90, 92), new(30, 78) };
            foreach (Vector2 chip in chips)
                if (Vector2.Distance(new Vector2(x, y), chip) < 9f) c = new Color(0.36f, 0.2f, 0.12f);
            c.a = a;
            return c;
        }));

        public static Sprite BiscuitCrack => Get("biscuitcrack", () => Mask(128, (x, y) =>
        {
            float l1 = Mathf.Abs((y - 64f) - (x - 64f) * 0.6f) - 2.5f;
            float l2 = Mathf.Abs((x - 74f) + (y - 40f) * 0.4f) - 2f;
            float inBox = RoundRect(x, y, 16, 16, 96, 96, 20);
            return Mathf.Max(Mathf.Min(l1, y < 64f ? l2 : 99f), inBox);
        }));

        // ------------------------------------------------------------ Bomboane din grămadă

        public static Sprite CandyVariant(int index) => (index % 3) switch
        {
            0 => CandySwirl,
            1 => CandyWrapped,
            _ => CandyHeart,
        };

        /// <summary>Bomboană rotundă cu spirală (albă; se colorează).</summary>
        public static Sprite CandySwirl => Get("candyswirl", () => Paint(64, (x, y) =>
        {
            Vector2 p = new(x - 32f, y - 32f);
            float d = p.magnitude;
            float a = Alpha(d - 29f);
            float angle = Mathf.Atan2(p.y, p.x);
            float swirl = Mathf.Repeat(angle / (Mathf.PI * 2f) * 3f + d / 10f, 1f) < 0.5f ? 1f : 0.82f;
            float shine = Alpha(Vector2.Distance(new Vector2(x, y), new Vector2(22f, 42f)) - 6f) * 0.5f;
            float v = Mathf.Clamp01(swirl + shine);
            return new Color(v, v, v, a);
        }));

        /// <summary>Bomboană ambalată (oval cu „aripioare”).</summary>
        public static Sprite CandyWrapped => Get("candywrapped", () => Paint(64, (x, y) =>
        {
            Vector2 p = new(x - 32f, y - 32f);
            float body = new Vector2(p.x / 1.25f, p.y).magnitude - 16f;
            float wings = Mathf.Max(Mathf.Abs(p.y) - (Mathf.Abs(p.x) - 14f) * 0.8f, Mathf.Abs(p.x) - 30f);
            float d = Mathf.Min(body, wings);
            float v = body < 0f ? 1f : 0.85f;
            return new Color(v, v, v, Alpha(d));
        }));

        public static Sprite CandyHeart => Get("candyheart", () => Paint(64, (x, y) =>
        {
            float d = HeartDistance(x * 2f, y * 2f) * 0.5f;
            float shine = Alpha(Vector2.Distance(new Vector2(x, y), new Vector2(22f, 40f)) - 5f) * 0.45f;
            float v = Mathf.Clamp01(0.92f + shine);
            return new Color(v, v, v, Alpha(d));
        }));

        // ------------------------------------------------------------ Decor

        /// <summary>Fundal în degrade vertical (roz-piersică sus, lavandă jos).</summary>
        public static Sprite BackgroundGradient => Get("bggradient", () => Paint(8, 256, (x, y) =>
            Color.Lerp(new Color(0.84f, 0.76f, 1f), new Color(1f, 0.86f, 0.9f), y / 255f)));

        /// <summary>Cerul unui capitol: degrade din culorile temei (o singură textură pe temă).</summary>
        public static Sprite Sky(int chapter)
        {
            ChapterTheme theme = ChapterTheme.For(chapter);
            return Get("sky" + ChapterTheme.IndexOf(chapter), () => Paint(8, 256, (x, y) => Color.Lerp(theme.SkyBottom, theme.SkyTop, y / 255f)));
        }

        /// <summary>Baston de bomboană în culoarea temei (dungi diagonale colorat/alb), repetabil.</summary>
        public static Sprite Cane(int chapter)
        {
            ChapterTheme theme = ChapterTheme.For(chapter);
            // Soft Pop: cadru plin, în culoarea de accent a ținutului (fără dungi de baston).
            Color frame = Color.Lerp(theme.Stripe, Color.white, 0.2f);
            return Get("cane" + ChapterTheme.IndexOf(chapter), () => PaintRepeat(64, (x, y) => frame));
        }

        /// <summary>Cadrul implicit al tablei (lavandă plin), până se aplică tema ținutului.</summary>
        public static Sprite CandyCane => Get("candycane", () => PaintRepeat(64, (x, y) =>
            QuestStyle.Wood));

        public static Sprite Beam => Get("beam", () => Paint(128, 32, (x, y) =>
        {
            float v = 1f - Mathf.Abs(y - 16f) / 16f;
            float ends = Mathf.Clamp01(Mathf.Min(x, 127 - x) / 16f);
            return new Color(1f, 1f, 1f, Mathf.Clamp01(v * v * 1.5f) * ends);
        }));

        /// <summary>Partea de jos a cupcake-ului (folie cu caneluri).</summary>
        public static Sprite CupWrapper => Get("cupwrapper", () => Paint(128, (x, y) =>
        {
            float halfWidth = Mathf.Lerp(40f, 56f, y / 80f);
            float d = Mathf.Max(Mathf.Abs(x - 64f) - halfWidth, Mathf.Max(4f - y, y - 80f));
            float a = Alpha(d);
            float groove = Mathf.Abs(Mathf.Repeat(x - 64f + 6f, 14f) - 7f) < 2f ? 0.82f : 1f;
            Color c = new Color(1f, 0.55f, 0.68f) * groove;
            c.a = a;
            return c;
        }));

        /// <summary>Glazura cupcake-ului (nor din cercuri).</summary>
        public static Sprite Frosting => Get("frosting", () => Paint(128, (x, y) =>
        {
            Vector2 p = new(x, y);
            float d = Mathf.Min(Mathf.Min(Vector2.Distance(p, new Vector2(34, 46)) - 30f, Vector2.Distance(p, new Vector2(94, 46)) - 30f),
                Mathf.Min(Vector2.Distance(p, new Vector2(64, 66)) - 40f, Vector2.Distance(p, new Vector2(64, 40)) - 34f));
            Color c = Color.Lerp(new Color(1f, 0.96f, 0.98f), new Color(0.98f, 0.86f, 0.92f), Mathf.Clamp01((64f - y) / 64f));
            float sprinkle = Mathf.Repeat(x * 0.37f + y * 0.71f, 23f) < 1.4f && d < -6f ? 1f : 0f;
            if (sprinkle > 0f) c = Color.HSVToRGB(Mathf.Repeat(x * 0.013f, 1f), 0.6f, 1f);
            c.a = Alpha(d);
            return c;
        }));

        public static Sprite Smile => Get("smile", () => Mask(64, (x, y) =>
        {
            float ring = Mathf.Abs(Vector2.Distance(new Vector2(x, y), new Vector2(32f, 40f)) - 18f) - 3.5f;
            return Mathf.Max(ring, y - 34f);
        }));

        public static Sprite Frown => Get("frown", () => Mask(64, (x, y) =>
        {
            float ring = Mathf.Abs(Vector2.Distance(new Vector2(x, y), new Vector2(32f, 18f)) - 16f) - 3.5f;
            return Mathf.Max(ring, 24f - y);
        }));

        public static Sprite Bomb => Get("bomb", () => Paint(128, (x, y) =>
        {
            Vector2 p = new(x, y);
            float body = Vector2.Distance(p, new Vector2(60f, 54f)) - 42f;
            float cap = RoundRect(x, y, 76, 84, 26, 22, 6);
            float fuse = Mathf.Abs(Vector2.Distance(p, new Vector2(104f, 100f)) - 14f) - 3f;
            fuse = Mathf.Max(fuse, Mathf.Max(96f - x, 98f - y));
            float d = Mathf.Min(body, Mathf.Min(cap, fuse));
            Color c = body < 0f ? Color.Lerp(new Color(0.25f, 0.22f, 0.35f), new Color(0.5f, 0.45f, 0.65f), Alpha(Vector2.Distance(p, new Vector2(46f, 70f)) - 12f))
                : cap < 0f ? new Color(0.6f, 0.6f, 0.7f) : new Color(1f, 0.75f, 0.3f);
            c.a = Alpha(d);
            return c;
        }));

        public static Sprite ShuffleIcon => Get("shuffle", () => Mask(128, (x, y) =>
        {
            Vector2 p = new(x - 64f, y - 64f);
            float ring = Mathf.Abs(p.magnitude - 38f) - 9f;
            float angle = Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg;
            bool gap = (angle > 60f && angle < 100f) || (angle > -120f && angle < -80f);
            if (gap) ring = Mathf.Max(ring, 4f);
            Vector2 tipA = new(Mathf.Cos(60f * Mathf.Deg2Rad) * 38f, Mathf.Sin(60f * Mathf.Deg2Rad) * 38f);
            Vector2 tipB = -tipA;
            float headA = Mathf.Max(Mathf.Abs((p - tipA).x) * 0.9f + (p - tipA).y * 0.7f - 16f, -(p - tipA).y - 2f);
            float headB = Mathf.Max(Mathf.Abs((p - tipB).x) * 0.9f - (p - tipB).y * 0.7f - 16f, (p - tipB).y - 2f);
            return Mathf.Min(ring, Mathf.Min(headA, headB));
        }));

        public static Sprite Lock => Get("lock", () => Mask(128, (x, y) =>
        {
            float body = RoundRect(x, y, 30, 18, 68, 56, 12);
            float shackle = Mathf.Abs(Vector2.Distance(new Vector2(x, y), new Vector2(64f, 76f)) - 22f) - 7f;
            shackle = Mathf.Max(shackle, 70f - y);
            return Mathf.Min(body, shackle);
        }));

        /// <summary>Rachetă de bomboană (orizontală; verticala e aceeași rotită): corp roz-alb în dungi, vârf, aripioare.</summary>
        public static Sprite Rocket => Get("rocket", () => Paint(128, (x, y) =>
        {
            Vector2 p = new(x, y);
            // Corpul: capsulă orizontală; vârful: semicerc spre dreapta; aripioarele: două triunghiuri în stânga.
            float body = RoundRect(x, y, 22, 44, 78, 40, 20);
            float nose = Vector2.Distance(p, new Vector2(98f, 64f)) - 20f;
            float finTop = Mathf.Max(Mathf.Max(30f - x, x - 54f), Mathf.Max(84f - y, y - 104f + (x - 30f) * 0.8f));
            float finBottom = Mathf.Max(Mathf.Max(30f - x, x - 54f), Mathf.Max(y - 44f, 24f - y - (x - 30f) * 0.8f));
            float d = Mathf.Min(Mathf.Min(body, nose), Mathf.Min(finTop, finBottom));
            bool stripe = Mathf.Repeat(x + y * 0.4f, 20f) < 9f;
            Color c = stripe ? new Color(1f, 0.42f, 0.55f) : new Color(1f, 0.97f, 0.98f);
            if (nose < 0f && x > 92f) c = new Color(1f, 0.82f, 0.25f);
            if (finTop < 0f || finBottom < 0f) c = new Color(0.45f, 0.7f, 1f);
            float shine = Mathf.Clamp01(1f - Mathf.Abs(y - 74f) / 5f) * (body < -4f ? 0.45f : 0f);
            c = Color.Lerp(c, Color.white, shine);
            float shade = Mathf.Lerp(0.82f, 1f, Mathf.Clamp01((y - 30f) / 60f));
            c = new Color(c.r * shade, c.g * shade, c.b * shade, Alpha(d));
            return c;
        }));

        /// <summary>Ladă de lemn (obstacol): scânduri cu cuie și ramă.</summary>
        public static Sprite Crate => Get("crate", () => Paint(128, (x, y) =>
        {
            float d = RoundRect(x, y, 8, 8, 112, 112, 14);
            var wood = new Color(0.86f, 0.6f, 0.34f);
            var dark = new Color(0.6f, 0.38f, 0.2f);
            float frame = Mathf.Min(Mathf.Min(x - 8f, 120f - x), Mathf.Min(y - 8f, 120f - y));
            bool plankLine = Mathf.Repeat(y - 8f, 28f) < 3f;
            float diagonal = Mathf.Abs((x - 64f) - (y - 64f)) - 7f;
            Color c = frame < 12f || diagonal < 0f ? Color.Lerp(dark, wood, 0.35f) : plankLine ? dark : wood;
            float grain = Mathf.Sin(x * 0.35f + Mathf.Sin(y * 0.2f) * 2f) * 0.04f;
            c = new Color(c.r + grain, c.g + grain, c.b + grain);
            foreach (Vector2 nail in new[] { new Vector2(16, 16), new Vector2(112, 16), new Vector2(16, 112), new Vector2(112, 112) })
                if (Vector2.Distance(new Vector2(x, y), nail) < 4f) c = new Color(0.45f, 0.45f, 0.5f);
            c.a = Alpha(d);
            return c;
        }));

        /// <summary>Lanț în X peste o bomboană legată.</summary>
        public static Sprite Chain => Get("chain", () => Paint(128, (x, y) =>
        {
            Vector2 p = new(x - 64f, y - 64f);
            float best = 99f;
            foreach (float angle in new[] { 45f, -45f })
            {
                Vector2 q = Rotate(p, angle);
                if (Mathf.Abs(q.x) > 58f) continue;
                // Zale ovale alternând orientarea de-a lungul diagonalei.
                float cell = Mathf.Repeat(q.x + 9f, 18f) - 9f;
                bool flat = Mathf.Repeat(Mathf.Floor((q.x + 9f) / 18f), 2f) < 1f;
                float rx = flat ? 9f : 4.5f, ry = flat ? 5f : 8f;
                float ring = Mathf.Abs(new Vector2(cell / rx, q.y / ry).magnitude - 1f) * Mathf.Min(rx, ry) - 1.6f;
                best = Mathf.Min(best, ring);
            }
            float a = Alpha(best);
            float metal = 0.6f + 0.35f * Mathf.Clamp01(1f - Mathf.Abs(best + 1f) / 2f);
            return new Color(metal * 0.85f, metal * 0.88f, metal, a);
        }));

        /// <summary>
        /// Portretul unui Boss: 0 Regele Noroiului, 1 Regina Gheții, 2 Dragonul de Foc, 3 Monstrul Mlaștinii,
        /// 4–6 Mega-Boss-urile (Împăratul Furtunii, Regina Spinilor, Titanul de Piatră).
        /// </summary>
        public static Sprite BossFace(int look) => Get("boss" + look, () => Paint(128, (x, y) => BossPixel(look, x, y)));

        private static readonly Color[] BossBody =
        {
            new(0.5f, 0.29f, 0.16f), new(0.62f, 0.85f, 1f), new(0.38f, 0.8f, 0.42f), new(0.7f, 0.4f, 0.95f),
            new(1f, 0.55f, 0.75f), new(0.95f, 0.3f, 0.6f), new(1f, 0.62f, 0.2f),
        };

        private static Color BossPixel(int look, int px, int py)
        {
            look = Mathf.Clamp(look, 0, BossBody.Length - 1);
            Vector2 p = new(px + 0.5f, py + 0.5f);
            Color body = BossBody[look];
            Color gold = new(1f, 0.82f, 0.25f);
            Color color = new(0f, 0f, 0f, 0f);

            void Layer(float distance, Color c)
            {
                float a = Alpha(distance) * c.a;
                if (a <= 0f) return;
                float outA = a + color.a * (1f - a);
                Color mixed = (c * a + color * color.a * (1f - a)) / Mathf.Max(outA, 0.0001f);
                mixed.a = outA;
                color = mixed;
            }

            // Coarne (dragonul și titanul), în spatele capului.
            if (look == 2 || look == 6)
                foreach (float side in new[] { -1f, 1f })
                {
                    Vector2 a = new(64f + side * 26f, 84f), b = new(64f + side * 44f, 122f);
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, b - a) / (b - a).sqrMagnitude);
                    float horn = Vector2.Distance(p, Vector2.Lerp(a, b, t)) - Mathf.Lerp(9f, 1.5f, t);
                    Layer(horn, new Color(1f, 0.95f, 0.8f));
                }

            // Capul (jeleul e turtit și „curge”), cu umbră jos și lumină sus.
            bool jelly = look == 3;
            Vector2 center = new(64f, jelly ? 50f : 56f);
            Vector2 q = (p - center);
            q.x /= jelly ? 1.15f : 1f;
            q.y /= jelly ? 0.88f : 1f;
            float head = q.magnitude - 44f;
            if (jelly)
                for (int i = 0; i < 3; i++)
                {
                    float dx = 36f + i * 28f;
                    float drip = RoundRect(p.x - 0.5f, p.y - 0.5f, dx - 6f, 4f, 12f, 30f, 6f);
                    head = Mathf.Min(head, drip);
                }
            float shade = Mathf.Clamp01((p.y - 14f) / 90f);
            Color skin = Color.Lerp(body * 0.7f, Color.Lerp(body, Color.white, 0.25f), shade);
            skin.a = 1f;
            Layer(head, skin);
            Layer(Vector2.Distance(p, center + new Vector2(-18f, 22f)) - 10f, new Color(1f, 1f, 1f, 0.35f));

            // Ochi încruntați: albul, pupila și sprânceana oblică.
            foreach (float side in new[] { -1f, 1f })
            {
                Vector2 eye = center + new Vector2(side * 17f, 6f);
                Layer(Vector2.Distance(p, eye) - 10.5f, Color.white);
                Layer(Vector2.Distance(p, eye + new Vector2(-side * 2.5f, -2.5f)) - 5f, new Color(0.15f, 0.08f, 0.15f));
                Layer(Vector2.Distance(p, eye + new Vector2(-side * 4f, 0f)) - 1.6f, Color.white);
                Vector2 b0 = eye + new Vector2(side * 12f, 16f), b1 = eye + new Vector2(-side * 9f, 9f);
                float t = Mathf.Clamp01(Vector2.Dot(p - b0, b1 - b0) / (b1 - b0).sqrMagnitude);
                Layer(Vector2.Distance(p, Vector2.Lerp(b0, b1, t)) - 3.4f, new Color(0.2f, 0.1f, 0.12f));
            }

            // Gura: un zâmbet răutăcios, cu doi colți.
            Vector2 mouth = center + new Vector2(0f, -18f);
            Vector2 m = p - mouth;
            float mouthD = new Vector2(m.x / 17f, m.y / 8f).magnitude - 1f;
            mouthD = Mathf.Max(mouthD * 8f, m.y - 1f);
            Layer(mouthD, new Color(0.3f, 0.06f, 0.12f));
            foreach (float side in new[] { -1f, 1f })
            {
                Vector2 f = p - (mouth + new Vector2(side * 8f, 0f));
                float fang = Mathf.Max(Mathf.Abs(f.x) - 3.2f * Mathf.Clamp01((f.y + 6f) / 6f), Mathf.Max(f.y - 0.5f, -6f - f.y));
                Layer(fang, Color.white);
            }

            // Accesoriul: coroană (regi, împărat, titan) sau tiară (regine).
            bool crown = look == 0 || look == 4 || look == 6;
            bool tiara = look == 1 || look == 5;
            if (crown || tiara)
            {
                float width = crown ? 60f : 50f, baseY = crown ? 94f : 96f, spike = crown ? 24f : 14f;
                float left = 64f - width * 0.5f;
                float t = Mathf.Repeat((p.x - left) / (width / (crown ? 3f : 5f)), 1f);
                float top = baseY + 8f + spike * (1f - Mathf.Abs(2f * t - 1f));
                float band = Mathf.Max(Mathf.Max(left - p.x, p.x - (left + width)), Mathf.Max(baseY - p.y, p.y - top));
                Color metal = tiara ? new Color(0.85f, 0.95f, 1f) : gold;
                Layer(band, Color.Lerp(metal * 0.8f, metal, Mathf.Clamp01((p.y - baseY) / 14f)));
                Layer(Vector2.Distance(p, new Vector2(64f, baseY + 5f)) - (crown ? 5f : 4f), tiara ? new Color(0.4f, 0.75f, 1f) : new Color(0.95f, 0.2f, 0.35f));
            }
            return color;
        }

        /// <summary>Tabletă de ciocolată (obstacol care crește): pătrate în relief.</summary>
        public static Sprite Chocolate => Get("chocolate", () => Paint(128, (x, y) =>
        {
            float d = RoundRect(x, y, 6, 6, 116, 116, 22);
            float a = Alpha(d);
            float cellX = Mathf.Repeat(x - 6f, 58f), cellY = Mathf.Repeat(y - 6f, 58f);
            float groove = Mathf.Min(Mathf.Min(cellX, 58f - cellX), Mathf.Min(cellY, 58f - cellY));
            float light = Mathf.Clamp01(groove / 8f);
            var dark = new Color(0.33f, 0.18f, 0.1f);
            var milk = new Color(0.55f, 0.32f, 0.18f);
            Color c = Color.Lerp(dark, milk, light);
            float shine = Mathf.Clamp01(1f - Mathf.Abs(cellY - 44f) / 6f) * Mathf.Clamp01(groove / 10f) * 0.35f;
            c = Color.Lerp(c, new Color(0.85f, 0.6f, 0.45f), shine);
            c.a = a;
            return c;
        }));

        /// <summary>Glazura de pe o celulă (albă, cu scurgeri și presărături; se colorează după straturi).</summary>
        public static Sprite Glaze => Get("glaze", () => Paint(128, (x, y) =>
        {
            float d = RoundRect(x, y, 4, 4, 120, 120, 26);
            // Margine de jos „curgătoare”.
            float drip = Mathf.Sin(x * 0.12f) * 6f + Mathf.Sin(x * 0.31f + 1f) * 3f;
            d = Mathf.Max(d, 14f + drip - y);
            float a = Alpha(d) * 0.92f;
            float shade = Mathf.Lerp(0.9f, 1f, y / 128f);
            Color c = new(shade, shade, shade, a);
            float sprinkle = Mathf.Repeat(x * 0.41f + y * 0.73f, 29f) < 1.6f && d < -8f ? 1f : 0f;
            if (sprinkle > 0f) c = Color.HSVToRGB(Mathf.Repeat(x * 0.017f + y * 0.006f, 1f), 0.55f, 1f) * new Color(1f, 1f, 1f, a);
            return c;
        }));

        /// <summary>Săgeată în jos (ieșirea bomboanelor spre borcan).</summary>
        public static Sprite Chevron => Get("chevron", () => Mask(64, (x, y) =>
        {
            float px = Mathf.Abs(x - 32f), py = y - 30f;
            float arm = Mathf.Abs(py - px * 0.8f + 4f) - 5f;
            return Mathf.Max(arm, px - 22f);
        }));

        /// <summary>Bifă (obiectiv îndeplinit).</summary>
        public static Sprite Check => Get("check", () => Mask(128, (x, y) =>
        {
            Vector2 p = new(x, y);
            float a = SegmentDistance(p, new Vector2(26f, 66f), new Vector2(52f, 38f)) - 11f;
            float b = SegmentDistance(p, new Vector2(52f, 38f), new Vector2(104f, 92f)) - 11f;
            return Mathf.Min(a, b);
        }));

        private static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 pa = p - a, ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
            return (pa - ba * h).magnitude;
        }

        /// <summary>Rotiță (setări): cerc cu 8 dinți și o gaură în mijloc.</summary>
        public static Sprite Gear => Get("gear", () => Mask(128, (x, y) =>
        {
            Vector2 p = new(x - 64f, y - 64f);
            float angle = Mathf.Atan2(p.y, p.x);
            float tooth = Mathf.Clamp01((Mathf.Cos(angle * 8f) - 0.1f) * 3f);
            float outer = p.magnitude - (40f + tooth * 14f);
            float hole = 16f - p.magnitude;
            return Mathf.Max(outer, hole);
        }));

        // ------------------------------------------------------------ Roata norocului și costume

        /// <summary>Roata norocului: felii colorate (în sensul acelor de ceasornic, de sus), separatoare albe și margine aurie.</summary>
        public static Sprite Wheel(int slices) => Get("wheel" + slices, () => Paint(512, (x, y) =>
        {
            Vector2 p = new(x + 0.5f - 256f, y + 0.5f - 256f);
            float r = p.magnitude;
            if (r > 254f) return Color.clear;
            float edge = Mathf.Clamp01(254f - r);
            if (r > 236f)
            {
                // Margine aurie cu becuri.
                float bulbAngle = Mathf.Repeat(Mathf.Atan2(p.x, p.y) * Mathf.Rad2Deg, 360f);
                float bulb = Mathf.Abs(Mathf.Repeat(bulbAngle, 360f / (slices * 2)) - 180f / (slices * 2));
                bool on = bulb < 3.5f && r > 240f && r < 251f;
                Color rim = on ? new Color(1f, 0.98f, 0.8f) : new Color(1f, 0.78f, 0.25f);
                rim.a = edge;
                return rim;
            }
            float angle = Mathf.Repeat(Mathf.Atan2(p.x, p.y) * Mathf.Rad2Deg, 360f);
            float slice = 360f / slices;
            int index = Mathf.FloorToInt(angle / slice);
            float fromEdge = Mathf.Min(angle - index * slice, (index + 1) * slice - angle) * Mathf.Deg2Rad * r;
            Color c = index % 2 == 0 ? Palette.Tile(index / 2 % Palette.TileColorCount) : Palette.TileLight(index / 2 % Palette.TileColorCount);
            // Separatoare albe și o umbră ușoară spre centru.
            c = Color.Lerp(Color.white, c, Mathf.Clamp01((fromEdge - 3f) / 2f));
            c = Color.Lerp(c * 0.85f, c, Mathf.Clamp01(r / 120f));
            c.a = 1f;
            return c;
        }));

        private static Color Over(Color under, Color over, float alpha)
        {
            Color c = Color.Lerp(under, over, alpha);
            c.a = Mathf.Max(under.a, alpha);
            return c;
        }

        /// <summary>Costumele lui Bobo (desenate din cod, 128×128, cu baza în partea de jos a imaginii).</summary>
        public static Sprite CostumeSprite(Costume costume) => costume switch
        {
            Costume.PartyHat => Get("partyhat", () => Paint(128, (x, y) =>
            {
                Vector2 p = new(x + 0.5f, y + 0.5f);
                Color c = Color.clear;
                float cone = ConeDistance(p, new Vector2(64f, 112f), 34f, 14f);
                bool stripe = Mathf.Repeat(p.x * 0.6f + p.y, 28f) < 14f;
                c = Over(c, stripe ? new Color(1f, 0.42f, 0.62f) : new Color(1f, 0.82f, 0.3f), Alpha(cone));
                c = Over(c, new Color(0.45f, 0.75f, 1f), Alpha(RoundRect(x, y, 26, 8, 76, 12, 6)));
                c = Over(c, Color.white, Alpha(Vector2.Distance(p, new Vector2(64f, 114f)) - 11f));
                return c;
            })),
            Costume.Bow => Get("bow", () => Paint(128, (x, y) =>
            {
                Vector2 p = new(x + 0.5f, y + 0.5f);
                Color pink = new(1f, 0.38f, 0.6f);
                float left = Vector2.Distance(new Vector2((p.x - 38f) * 0.8f, p.y - 44f), Vector2.zero) - 26f;
                float right = Vector2.Distance(new Vector2((p.x - 90f) * 0.8f, p.y - 44f), Vector2.zero) - 26f;
                Color c = Over(Color.clear, pink, Alpha(Mathf.Min(left, right)));
                c = Over(c, new Color(0.85f, 0.2f, 0.45f), Alpha(Vector2.Distance(p, new Vector2(64f, 44f)) - 14f));
                c = Over(c, new Color(1f, 1f, 1f, 0.6f), Alpha(Vector2.Distance(p, new Vector2(30f, 54f)) - 6f));
                return c;
            })),
            Costume.Crown => Get("crown", () => Paint(128, (x, y) =>
            {
                Vector2 p = new(x + 0.5f, y + 0.5f);
                Color gold = new(1f, 0.8f, 0.25f);
                float band = RoundRect(x, y, 22, 10, 84, 30, 6);
                float spikes = Mathf.Min(ConeDistance(p, new Vector2(30f, 80f), 14f, 34f),
                    Mathf.Min(ConeDistance(p, new Vector2(64f, 92f), 15f, 34f), ConeDistance(p, new Vector2(98f, 80f), 14f, 34f)));
                Color c = Over(Color.clear, gold, Alpha(Mathf.Min(band, spikes)));
                c = Over(c, new Color(0.95f, 0.25f, 0.35f), Alpha(Vector2.Distance(p, new Vector2(64f, 25f)) - 8f));
                c = Over(c, new Color(0.3f, 0.6f, 1f), Alpha(Mathf.Min(Vector2.Distance(p, new Vector2(38f, 25f)), Vector2.Distance(p, new Vector2(90f, 25f))) - 6f));
                foreach (float tx in new[] { 30f, 64f, 98f })
                    c = Over(c, Color.white, Alpha(Vector2.Distance(p, new Vector2(tx, tx == 64f ? 94f : 82f)) - 6f));
                return c;
            })),
            Costume.ChefHat => Get("chefhat", () => Paint(128, (x, y) =>
            {
                Vector2 p = new(x + 0.5f, y + 0.5f);
                float puff = Mathf.Min(Vector2.Distance(p, new Vector2(40f, 66f)) - 24f,
                    Mathf.Min(Vector2.Distance(p, new Vector2(64f, 80f)) - 28f, Vector2.Distance(p, new Vector2(88f, 66f)) - 24f));
                Color c = Over(Color.clear, new Color(0.96f, 0.96f, 1f), Alpha(Mathf.Min(puff, RoundRect(x, y, 30, 34, 68, 30, 4))));
                c = Over(c, new Color(0.85f, 0.87f, 0.95f), Alpha(RoundRect(x, y, 28, 10, 72, 26, 6)));
                return c;
            })),
            Costume.Sunglasses => Get("sunglasses", () => Paint(128, (x, y) =>
            {
                Vector2 p = new(x + 0.5f, y + 0.5f);
                float lenses = Mathf.Min(RoundRect(x, y, 8, 44, 50, 36, 14), RoundRect(x, y, 70, 44, 50, 36, 14));
                float bridge = RoundRect(x, y, 54, 66, 20, 8, 4);
                Color c = Over(Color.clear, new Color(0.12f, 0.1f, 0.2f), Alpha(Mathf.Min(lenses, bridge)));
                c = Over(c, new Color(1f, 1f, 1f, 0.55f), Alpha(Mathf.Min(SegmentDistance(p, new Vector2(18f, 70f), new Vector2(30f, 58f)),
                    SegmentDistance(p, new Vector2(80f, 70f), new Vector2(92f, 58f))) - 3f));
                return c;
            })),
            Costume.WizardHat => Get("wizardhat", () => Paint(128, (x, y) =>
            {
                Vector2 p = new(x + 0.5f, y + 0.5f);
                Color c = Over(Color.clear, new Color(0.45f, 0.3f, 0.85f), Alpha(ConeDistance(p, new Vector2(70f, 122f), 30f, 18f)));
                c = Over(c, new Color(0.35f, 0.22f, 0.7f), Alpha(Vector2.Distance(new Vector2(p.x - 64f, (p.y - 16f) * 3f), Vector2.zero) - 54f));
                Vector2 s = p - new Vector2(64f, 56f);
                float a = Mathf.Atan2(s.y, s.x);
                float star = s.magnitude - (9f + 6f * Mathf.Cos(a * 5f));
                c = Over(c, new Color(1f, 0.88f, 0.3f), Alpha(star));
                return c;
            })),
            _ => null,
        };

        /// <summary>Con (triunghi isoscel) cu vârful sus: jumătatea bazei și înălțimea date; baza la y = top - height.</summary>
        private static float ConeDistance(Vector2 p, Vector2 apex, float halfBase, float bottomY)
        {
            float height = apex.y - bottomY;
            if (height <= 0f) return 1f;
            float t = Mathf.Clamp01((apex.y - p.y) / height);
            float halfWidth = halfBase * t;
            float horizontal = Mathf.Abs(p.x - apex.x) - halfWidth;
            float vertical = Mathf.Max(p.y - apex.y, bottomY - p.y);
            return Mathf.Max(horizontal, vertical);
        }

        // ------------------------------------------------------------ Mecanicile ținuturilor

        /// <summary>Ghiveci de lut (Pădurea de Mentă): buză lată și corp care se îngustează în jos.</summary>
        public static Sprite Pot => Get("pot", () => Paint(128, (x, y) =>
        {
            Vector2 p = new(x + 0.5f, y + 0.5f);
            float t = Mathf.Clamp01((p.y - 14f) / 54f);
            float halfWidth = Mathf.Lerp(30f, 40f, t);
            float body = Mathf.Max(Mathf.Abs(p.x - 64f) - halfWidth, Mathf.Max(14f - p.y, p.y - 68f));
            float rim = RoundRect(x, y, 18, 62, 92, 22, 8);
            Color clay = new(0.86f, 0.47f, 0.3f), rimColor = new(0.95f, 0.58f, 0.38f);
            Color c = Over(Color.clear, Color.Lerp(clay * 0.85f, clay, t), Alpha(body));
            c = Over(c, rimColor, Alpha(rim));
            c = Over(c, new Color(1f, 1f, 1f, 0.35f), Alpha(RoundRect(x, y, 30, 26, 8, 30, 4)));
            c.a = Mathf.Max(Alpha(body), Alpha(rim));
            return c;
        }));

        /// <summary>Planta din ghiveci, după câte udări a primit: 0 = mlădiță, 1 = tulpină cu frunze, 2 = boboc.</summary>
        public static Sprite Sprout(int stage) => Get("sprout" + stage, () => Paint(128, (x, y) =>
        {
            Vector2 p = new(x + 0.5f, y + 0.5f);
            float height = stage == 0 ? 88f : stage == 1 ? 104f : 116f;
            Color green = new(0.35f, 0.75f, 0.35f);
            float stem = Mathf.Max(Mathf.Abs(p.x - 64f) - 3.5f, Mathf.Max(80f - p.y, p.y - height));
            Color c = Over(Color.clear, new Color(0.3f, 0.6f, 0.3f), Alpha(stem));
            float leafY = stage == 0 ? 86f : 92f;
            float leafL = Vector2.Distance(new Vector2((p.x - 50f) * 0.7f, p.y - leafY), Vector2.zero) - 9f;
            float leafR = Vector2.Distance(new Vector2((p.x - 78f) * 0.7f, p.y - leafY - 4f), Vector2.zero) - 9f;
            c = Over(c, green, Alpha(Mathf.Min(leafL, leafR)));
            if (stage >= 1)
            {
                float leaf2 = Vector2.Distance(new Vector2((p.x - 52f) * 0.7f, p.y - 104f), Vector2.zero) - 8f;
                c = Over(c, Color.Lerp(green, Color.white, 0.15f), Alpha(leaf2));
            }
            if (stage >= 2)
                c = Over(c, new Color(1f, 0.45f, 0.65f), Alpha(Vector2.Distance(new Vector2(p.x - 64f, (p.y - 118f) * 0.8f), Vector2.zero) - 9f));
            return c;
        }));

        /// <summary>Floare înflorită (5 petale și mijloc galben).</summary>
        public static Sprite Flower => Get("flower", () => Paint(128, (x, y) =>
        {
            Vector2 p = new(x + 0.5f - 64f, y + 0.5f - 64f);
            float angle = Mathf.Atan2(p.y, p.x);
            float petals = p.magnitude - (34f + 18f * Mathf.Cos(angle * 5f));
            Color c = Over(Color.clear, new Color(1f, 0.55f, 0.75f), Alpha(petals));
            c = Over(c, new Color(1f, 0.85f, 0.3f), Alpha(p.magnitude - 16f));
            return c;
        }));

        /// <summary>Cifră (0–9) în stil „afișaj cu segmente” rotunjite, albă, 48×64.</summary>
        public static Sprite Digit(int digit) => Get("digit" + digit, () =>
        {
            // Segmentele: a (sus), b (dreapta sus), c (dreapta jos), d (jos), e (stânga jos), f (stânga sus), g (mijloc).
            string[] map = { "abcdef", "bc", "abged", "abgcd", "fgbc", "afgcd", "afgedc", "abc", "abcdefg", "abcdfg" };
            string on = map[Mathf.Clamp(digit, 0, 9)];
            return Build(48, 64, (x, y) =>
            {
                Vector2 p = new(x + 0.5f, y + 0.5f);
                float d = float.MaxValue;
                void Seg(char name, Vector2 a, Vector2 b)
                {
                    if (on.IndexOf(name) >= 0) d = Mathf.Min(d, SegmentDistance(p, a, b) - 5f);
                }
                Seg('a', new Vector2(14f, 54f), new Vector2(34f, 54f));
                Seg('b', new Vector2(36f, 50f), new Vector2(36f, 36f));
                Seg('c', new Vector2(36f, 28f), new Vector2(36f, 14f));
                Seg('d', new Vector2(14f, 10f), new Vector2(34f, 10f));
                Seg('e', new Vector2(12f, 28f), new Vector2(12f, 14f));
                Seg('f', new Vector2(12f, 50f), new Vector2(12f, 36f));
                Seg('g', new Vector2(14f, 32f), new Vector2(34f, 32f));
                return new Color(1f, 1f, 1f, Alpha(d));
            }, TextureWrapMode.Clamp);
        });

        /// <summary>Portal (Lacul de Afine): vârtej în spirală.</summary>
        public static Sprite Portal => Get("portal", () => Paint(128, (x, y) =>
        {
            Vector2 p = new(x + 0.5f - 64f, y + 0.5f - 64f);
            float r = p.magnitude;
            if (r > 60f) return Color.clear;
            float angle = Mathf.Atan2(p.y, p.x);
            float spiral = Mathf.Repeat(angle / (Mathf.PI * 2f) * 3f + r / 22f, 1f);
            Color deep = new(0.35f, 0.25f, 0.85f), light = new(0.55f, 0.85f, 1f);
            Color c = Color.Lerp(deep, light, spiral < 0.5f ? spiral * 2f : (1f - spiral) * 2f);
            c = Color.Lerp(c, Color.white, Mathf.Clamp01(1f - r / 14f));
            c.a = Alpha(r - 60f) * Mathf.Lerp(0.95f, 0.6f, r / 60f);
            return c;
        }));

        /// <summary>Siluetă de om (profil): cap și umeri.</summary>
        public static Sprite Person => Get("person", () => Mask(128, (x, y) =>
        {
            Vector2 p = new(x + 0.5f, y + 0.5f);
            float head = Vector2.Distance(p, new Vector2(64f, 84f)) - 24f;
            float body = Mathf.Max(Vector2.Distance(new Vector2(p.x - 64f, (p.y - 18f) * 1.25f), Vector2.zero) - 46f, 16f - p.y);
            return Mathf.Min(head, Mathf.Max(body, -(Vector2.Distance(p, new Vector2(64f, 84f)) - 30f)));
        }));

        /// <summary>Cadou: cutie cu capac, panglică și fundă.</summary>
        public static Sprite Gift => Get("gift", () => Mask(128, (x, y) =>
        {
            Vector2 p = new(x, y);
            float box = RoundRect(x, y, 26, 14, 76, 62, 8);
            float lid = RoundRect(x, y, 18, 72, 92, 22, 6);
            float bowL = Mathf.Abs(Vector2.Distance(p, new Vector2(50f, 104f)) - 11f) - 5f;
            float bowR = Mathf.Abs(Vector2.Distance(p, new Vector2(78f, 104f)) - 11f) - 5f;
            // Panglica: o fâșie goală pe mijlocul cutiei și o linie între capac și cutie.
            float ribbon = Mathf.Max(4f - Mathf.Abs(x + 0.5f - 64f), 3f - Mathf.Abs(y + 0.5f - 74f));
            float body = Mathf.Max(Mathf.Min(box, lid), ribbon);
            return Mathf.Min(body, Mathf.Min(bowL, bowR));
        }));

        /// <summary>Listă cu bife (misiuni).</summary>
        public static Sprite Checklist => Get("checklist", () => Mask(128, (x, y) =>
        {
            Vector2 p = new(x, y);
            float d = float.MaxValue;
            for (int row = 0; row < 3; row++)
            {
                float cy = 96f - row * 32f;
                float tick = Mathf.Min(SegmentDistance(p, new Vector2(16f, cy), new Vector2(26f, cy - 10f)),
                    SegmentDistance(p, new Vector2(26f, cy - 10f), new Vector2(44f, cy + 10f))) - 6f;
                float line = RoundRect(x, y, 54, cy - 7f, 60, 14, 7);
                d = Mathf.Min(d, Mathf.Min(tick, line));
            }
            return d;
        }));

        /// <summary>Flacără (nivel greu): limbă de foc portocalie cu miez galben.</summary>
        public static Sprite Flame => Get("flame", () => Paint(128, (x, y) =>
        {
            Vector2 p = new(x, y);
            // Formă de picătură întoarsă: un cerc jos care se subțiază spre un vârf ondulat sus.
            float Shape(Vector2 q, float radius, float tip)
            {
                float t = Mathf.Clamp01((q.y - 50f) / (tip - 50f));
                float half = Mathf.Lerp(radius, 0f, t * t) + Mathf.Sin(q.y * 0.12f) * 4f * t;
                float body = Vector2.Distance(q, new Vector2(64f, 50f)) - radius;
                float cone = Mathf.Max(Mathf.Abs(q.x - 64f - Mathf.Sin(q.y * 0.05f) * 8f * t) - half, Mathf.Max(50f - q.y, q.y - tip));
                return Mathf.Min(body, cone);
            }
            float outer = Shape(p, 40f, 124f);
            float a = Alpha(outer);
            if (a <= 0f) return Color.clear;
            Color c = Color.Lerp(new Color(0.95f, 0.25f, 0.15f), new Color(1f, 0.55f, 0.15f), Mathf.Clamp01(-outer / 14f));
            c = Over(c, new Color(1f, 0.88f, 0.3f), Alpha(Shape(new Vector2(p.x, p.y + 12f), 20f, 104f)));
            c.a = a;
            return c;
        }));

        /// <summary>Bobo ca piesă pe tablă: folie roz cu dungi, glazură, cireașă, ochi și zâmbet.</summary>
        public static Sprite BoboToken => Get("bobotoken", () => Paint(128, (x, y) => BoboPixel(x, y, false)));

        /// <summary>Bobo speriat (lângă foc): gura „O”, sprâncene ridicate și o picătură de sudoare.</summary>
        public static Sprite BoboTokenScared => Get("bobotokenscared", () => Paint(128, (x, y) => BoboPixel(x, y, true)));

        private static Color BoboPixel(int x, int y, bool scared)
        {
            Vector2 p = new(x, y);
            // Folia (trapez) jos și glazura (trei bule) sus.
            float half = Mathf.Lerp(34f, 44f, (y - 8f) / 50f);
            float cup = Mathf.Max(Mathf.Max(8f - y, y - 60f), Mathf.Abs(x - 64f) - half) - 2f;
            float dome = Mathf.Min(Mathf.Min(Vector2.Distance(p, new Vector2(64f, 74f)) - 40f, Vector2.Distance(p, new Vector2(38f, 64f)) - 24f),
                Vector2.Distance(p, new Vector2(90f, 64f)) - 24f);
            float cherry = Vector2.Distance(p, new Vector2(68f, 114f)) - 11f;
            float body = Mathf.Min(Mathf.Min(cup, dome), cherry);
            float a = Alpha(body);
            if (a <= 0f) return Color.clear;
            Color c;
            if (cherry <= 0.5f) c = Over(new Color(0.9f, 0.15f, 0.28f), Color.white, Alpha(Vector2.Distance(p, new Vector2(64f, 118f)) - 3f) * 0.8f);
            else if (dome <= 0.5f && y > 46f)
            {
                c = Color.Lerp(new Color(1f, 0.8f, 0.9f), Color.white, Mathf.Clamp01((y - 46f) / 60f));
                // Stropi colorați pe glazură.
                if ((x * 7 + y * 13) % 97 < 3) c = Color.HSVToRGB((x * 0.013f) % 1f, 0.6f, 1f);
            }
            else
            {
                c = new Color(1f, 0.45f, 0.62f);
                if (Mathf.Repeat(x - 64f, 14f) < 3f) c = new Color(0.92f, 0.32f, 0.5f);
            }
            // Fața, pe folie.
            Color ink = new(0.3f, 0.1f, 0.2f);
            for (int e = -1; e <= 1; e += 2)
            {
                Vector2 eye = new(64f + e * 17f, 40f);
                c = Over(c, ink, Alpha(Vector2.Distance(p, eye) - (scared ? 8f : 7f)));
                c = Over(c, Color.white, Alpha(Vector2.Distance(p, eye + new Vector2(-2f, 3f)) - 2.5f));
                if (scared) c = Over(c, ink, Alpha(SegmentDistance(p, eye + new Vector2(-7f * e, 16f), eye + new Vector2(6f * e, 12f)) - 2f));
            }
            if (scared)
            {
                c = Over(c, ink, Alpha(Vector2.Distance(p, new Vector2(64f, 22f)) - 6f));
                float drop = Mathf.Min(Vector2.Distance(p, new Vector2(100f, 62f)) - 6f,
                    Mathf.Max(Mathf.Abs(x - 100f) - (74f - y) * 0.45f, Mathf.Max(62f - y, y - 74f)));
                c = Over(c, new Color(0.55f, 0.85f, 1f), Alpha(drop));
            }
            else
            {
                float smile = Mathf.Max(Mathf.Abs(Vector2.Distance(p, new Vector2(64f, 32f)) - 11f) - 2.5f, y - 30f);
                c = Over(c, ink, Alpha(smile));
            }
            for (int e = -1; e <= 1; e += 2)
                c = Over(c, new Color(1f, 0.35f, 0.5f), Alpha(Vector2.Distance(p, new Vector2(64f + e * 30f, 28f)) - 6f) * 0.5f);
            c.a = a;
            return c;
        }

        /// <summary>Slime acru: pată verde din câteva bule unite, cu luciu.</summary>
        public static Sprite Slime => Get("slime", () => Paint(128, (x, y) =>
        {
            Vector2 p = new(x, y);
            float d = Mathf.Min(Mathf.Min(Vector2.Distance(p, new Vector2(64f, 58f)) - 44f, Vector2.Distance(p, new Vector2(36f, 80f)) - 26f),
                Mathf.Min(Vector2.Distance(p, new Vector2(90f, 84f)) - 24f, Vector2.Distance(p, new Vector2(70f, 100f)) - 18f));
            float a = Alpha(d);
            if (a <= 0f) return Color.clear;
            Color c = Color.Lerp(new Color(0.35f, 0.7f, 0.15f), new Color(0.65f, 0.92f, 0.3f), y / 127f);
            c = Over(c, Color.white, Alpha(Vector2.Distance(p, new Vector2(46f, 86f)) - 8f) * 0.7f);
            c = Over(c, new Color(0.25f, 0.5f, 0.1f), Alpha(Vector2.Distance(p, new Vector2(80f, 46f)) - 6f) * 0.6f);
            c.a = a;
            return c;
        }));

        // ------------------------------------------------------------ Iconițe pentru Setări

        /// <summary>Difuzor cu două unde (sunet).</summary>
        public static Sprite Speaker => Get("speaker", () => Mask(128, (x, y) =>
        {
            Vector2 p = new(x, y);
            float body = RoundRect(x, y, 14, 46, 28, 36, 5);
            // Pâlnia: se lărgește spre dreapta (de la ±18 la ±40 în jurul lui y = 64).
            float half = Mathf.Lerp(18f, 40f, (x - 40f) / 30f);
            float cone = Mathf.Max(Mathf.Max(40f - x, x - 70f), Mathf.Abs(y - 64f) - half);
            float d = Mathf.Min(body, cone);
            for (int i = 0; i < 2; i++)
            {
                float radius = 20f + i * 18f;
                float arc = Mathf.Max(Mathf.Abs(Vector2.Distance(p, new Vector2(72f, 64f)) - radius) - 4.5f, Mathf.Abs(y - 64f) - (x - 72f) * 1.1f);
                d = Mathf.Min(d, arc);
            }
            return d;
        }));

        /// <summary>Notă muzicală dublă (muzică).</summary>
        public static Sprite Note => Get("note", () => Mask(128, (x, y) =>
        {
            Vector2 p = new(x, y);
            float headA = Vector2.Distance(new Vector2(p.x * 0.85f, p.y), new Vector2(38f * 0.85f, 32f)) - 14f;
            float headB = Vector2.Distance(new Vector2(p.x * 0.85f, p.y), new Vector2(92f * 0.85f, 44f)) - 14f;
            float stemA = RoundRect(x, y, 46, 32, 9, 70, 3);
            float stemB = RoundRect(x, y, 100, 44, 9, 70, 3);
            float beam = SegmentDistance(p, new Vector2(50f, 98f), new Vector2(104f, 110f)) - 8f;
            return Mathf.Min(Mathf.Min(headA, headB), Mathf.Min(Mathf.Min(stemA, stemB), beam));
        }));

        /// <summary>Telefon care vibrează (vibrații).</summary>
        public static Sprite Vibrate => Get("vibrate", () => Mask(128, (x, y) =>
        {
            Vector2 p = new(x, y);
            float phone = Mathf.Max(RoundRect(x, y, 42, 18, 44, 92, 10), -RoundRect(x, y, 50, 32, 28, 64, 3));
            float d = phone;
            for (int side = -1; side <= 1; side += 2)
                for (int i = 0; i < 2; i++)
                {
                    float cx = 64f + side * (34f + i * 14f);
                    float wave = Mathf.Min(SegmentDistance(p, new Vector2(cx, 44f), new Vector2(cx + side * 5f, 56f)),
                        Mathf.Min(SegmentDistance(p, new Vector2(cx + side * 5f, 56f), new Vector2(cx, 70f)),
                            SegmentDistance(p, new Vector2(cx, 70f), new Vector2(cx + side * 5f, 84f)))) - 3.5f;
                    d = Mathf.Min(d, wave);
                }
            return d;
        }));

        /// <summary>Clopoțel (notificări).</summary>
        public static Sprite Bell => Get("bell", () => Mask(128, (x, y) =>
        {
            Vector2 p = new(x, y);
            float dome = Mathf.Max(Vector2.Distance(p, new Vector2(64f, 76f)) - 30f, 76f - y);
            float half = Mathf.Lerp(40f, 30f, (y - 38f) / 38f);
            float skirt = Mathf.Max(Mathf.Max(38f - y, y - 77f), Mathf.Abs(x - 64f) - half);
            float rim = RoundRect(x, y, 18, 32, 92, 12, 6);
            float clapper = Vector2.Distance(p, new Vector2(64f, 22f)) - 10f;
            float knob = Vector2.Distance(p, new Vector2(64f, 110f)) - 7f;
            return Mathf.Min(Mathf.Min(Mathf.Min(dome, skirt), rim), Mathf.Min(clapper, knob));
        }));

        /// <summary>Glob cu meridiane (limba).</summary>
        public static Sprite Globe => Get("globe", () => Mask(128, (x, y) =>
        {
            Vector2 p = new(x, y);
            float circle = Vector2.Distance(p, new Vector2(64f, 64f)) - 48f;
            float outline = Mathf.Abs(circle + 4f) - 4f;
            float inside = circle;
            float meridian = (Mathf.Sqrt(Mathf.Pow((x - 64f) / 22f, 2) + Mathf.Pow((y - 64f) / 46f, 2)) - 1f) * 22f;
            float lines = Mathf.Min(Mathf.Abs(meridian) - 3.5f, Mathf.Abs(x - 64f) - 3.5f);
            lines = Mathf.Min(lines, Mathf.Min(Mathf.Abs(y - 64f) - 3.5f, Mathf.Min(Mathf.Abs(y - 88f) - 3.5f, Mathf.Abs(y - 40f) - 3.5f)));
            return Mathf.Min(outline, Mathf.Max(lines, inside));
        }));

        /// <summary>„X” rotunjit (închide).</summary>
        public static Sprite Cross => Get("cross", () => Mask(128, (x, y) =>
        {
            Vector2 p = new(x, y);
            return Mathf.Min(SegmentDistance(p, new Vector2(32f, 32f), new Vector2(96f, 96f)),
                SegmentDistance(p, new Vector2(32f, 96f), new Vector2(96f, 32f))) - 11f;
        }));

        /// <summary>Căsuță (înapoi la hartă).</summary>
        public static Sprite Home => Get("home", () => Mask(128, (x, y) =>
        {
            Vector2 p = new(x, y);
            float roof = Mathf.Min(SegmentDistance(p, new Vector2(16f, 62f), new Vector2(64f, 106f)),
                SegmentDistance(p, new Vector2(64f, 106f), new Vector2(112f, 62f))) - 8f;
            float body = Mathf.Max(RoundRect(x, y, 30, 16, 68, 56, 6), -RoundRect(x, y, 54, 10, 20, 34, 4));
            return Mathf.Min(roof, body);
        }));

        /// <summary>Acul de hartă (butonul „Harta”): cap rotund cu gaură și vârf în jos.</summary>
        public static Sprite MapPin => Get("mappin", () => Mask(128, (x, y) =>
        {
            Vector2 p = new(x, y);
            float head = Vector2.Distance(p, new Vector2(64f, 82f)) - 34f;
            float t = Mathf.Clamp01((82f - y) / 66f);
            float tail = Mathf.Max(Mathf.Abs(x - 64f) - Mathf.Lerp(28f, 2f, t), Mathf.Max(y - 82f, 16f - y));
            float hole = Vector2.Distance(p, new Vector2(64f, 84f)) - 14f;
            return Mathf.Max(Mathf.Min(head, tail), -hole);
        }));

        /// <summary>Trofeu (clasament): cupă cu toarte, picior și soclu.</summary>
        public static Sprite Trophy => Get("trophy", () => Mask(128, (x, y) =>
        {
            Vector2 p = new(x, y);
            float bowl = Mathf.Max(Vector2.Distance(p, new Vector2(64f, 98f)) - 36f, y - 98f);
            float rim = RoundRect(x, y, 24, 94, 80, 12, 5);
            float handleL = Mathf.Max(Mathf.Abs(Vector2.Distance(p, new Vector2(30f, 84f)) - 13f) - 5f, x - 34f);
            float handleR = Mathf.Max(Mathf.Abs(Vector2.Distance(p, new Vector2(98f, 84f)) - 13f) - 5f, 94f - x);
            float stem = RoundRect(x, y, 56, 34, 16, 32, 4);
            float foot = RoundRect(x, y, 36, 16, 56, 18, 6);
            return Mathf.Min(Mathf.Min(Mathf.Min(bowl, rim), Mathf.Min(handleL, handleR)), Mathf.Min(stem, foot));
        }));

        /// <summary>Inimioară plină, colorată (vieți).</summary>
        public static Sprite LifeHeart => Get("lifeheart", () => Paint(128, (x, y) =>
        {
            float d = HeartDistance(x, y);
            float shine = Alpha(Vector2.Distance(new Vector2(x, y), new Vector2(42f, 78f)) - 10f) * 0.6f;
            Color c = Color.Lerp(new Color(1f, 0.33f, 0.45f), Color.white, shine);
            c.a = Alpha(d);
            return c;
        }));

        // ------------------------------------------------------------ Implementare

        private static Sprite Get(string key, Func<Sprite> create)
        {
            if (Cache.TryGetValue(key, out Sprite sprite) && sprite != null) return sprite;
            // O imagine PNG cu același nume (Resources/Art) are prioritate față de desenul din cod.
            sprite = ArtLibrary.Find(key) ?? create();
            sprite.name = key;
            Cache[key] = sprite;
            return sprite;
        }

        private static float Alpha(float distance) => Mathf.Clamp01(0.5f - distance);

        private static Vector2 Rotate(Vector2 p, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            float c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(p.x * c - p.y * s, p.x * s + p.y * c);
        }

        /// <summary>Distanța semnată până la un dreptunghi rotunjit (colțul stânga-jos x,y; lățime w, înălțime h).</summary>
        private static float RoundRect(float px, float py, float x, float y, float w, float h, float r)
        {
            float qx = Mathf.Abs(px + 0.5f - (x + w * 0.5f)) - (w * 0.5f - r);
            float qy = Mathf.Abs(py + 0.5f - (y + h * 0.5f)) - (h * 0.5f - r);
            return new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        private static Sprite Mask(int size, Func<int, int, float> distance) =>
            Paint(size, (x, y) => new Color(1f, 1f, 1f, Alpha(distance(x, y))));

        private static Sprite Paint(int size, Func<int, int, Color> pixel) => Paint(size, size, pixel);

        private static Sprite Paint(int width, int height, Func<int, int, Color> pixel) =>
            Build(width, height, pixel, TextureWrapMode.Clamp);

        private static Sprite PaintRepeat(int size, Func<int, int, Color> pixel) =>
            Build(size, size, pixel, TextureWrapMode.Repeat);

        private static Sprite Build(int width, int height, Func<int, int, Color> pixel, TextureWrapMode wrap)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                wrapMode = wrap,
                filterMode = FilterMode.Bilinear,
            };
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    pixels[y * width + x] = pixel(x, y);
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
        }
    }
}
