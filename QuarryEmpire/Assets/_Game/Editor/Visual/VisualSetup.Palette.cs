using System.Collections.Generic;
using UnityEngine;
using static QuarryEmpire.EditorTools.VisualAssetFactory;

namespace QuarryEmpire.EditorTools
{
    public static partial class VisualSetup
    {
        /// <summary>Toate asset-urile vizuale generate, într-un singur loc. Culorile direcției artistice sunt aici.</summary>
        private sealed class Palette
        {
            // Mesh-uri
            public Mesh BlockRounded, Nugget;
            public Mesh[] Rocks = new Mesh[0];
            public Mesh ZoneRing;
            public readonly Dictionary<string, Mesh> Boxes = new();

            // Materiale personaj
            public Material Helmet, Shirt, Overalls, OverallsDark, Skin, SkinShade, Leather, Boots, Backpack,
                BackpackDark, Dark, Metal, Wood, Lamp, Gold;

            // Materiale mediu
            public Material Grass, GrassOuter, PitFloor, Road, RockA, RockB, Bush, BushDark, Fence, Crate,
                Rail, CartBody, SellPad, SellAccent, UpgradePad, UpgradeAccent, LanternGlow, SignBoard;

            // Particule
            public Material FxDust, FxSparkle, FxChunk, FxCoin;

            // UI
            public Sprite RoundRect, Circle, Ring, IconCoin, IconStar, IconPause, IconTarget, IconBolt,
                IconMagnet, IconChevron;

            /// <summary>Cutie rotunjită cu dimensiuni „coapte” în mesh (fără scară neuniformă care ar deforma colțurile).</summary>
            public Mesh Box(float x, float y, float z, float radius)
            {
                string key = $"RBox_{x:0.###}x{y:0.###}x{z:0.###}_r{radius:0.###}".Replace(',', '.');
                if (!Boxes.TryGetValue(key, out Mesh mesh))
                {
                    mesh = SaveMesh(key, RoundedBox(new Vector3(x, y, z), radius));
                    Boxes[key] = mesh;
                }
                return mesh;
            }
        }

        private static Palette BuildPalette()
        {
            var p = new Palette
            {
                BlockRounded = SaveMesh("Block_Rounded", RoundedBox(Vector3.one, 0.12f)),
                Nugget = SaveMesh("Pickup_Nugget", Rock(11, 0.35f, 0.85f)),
                Rocks = new[]
                {
                    SaveMesh("Rock_A", Rock(3, 0.28f, 0.7f)),
                    SaveMesh("Rock_B", Rock(7, 0.22f, 0.55f)),
                    SaveMesh("Rock_C", Rock(19, 0.32f, 0.8f)),
                },
                ZoneRing = SaveMesh("Zone_Ring", Ring(1.05f, 1.25f)),
            };

            // ---- Personaj (paletă caldă: cască galbenă, cămașă portocalie, salopetă albastră)
            p.Helmet = Lit("M_V_Miner_Helmet", Hex("#FFC23D"), 0.35f);
            p.Shirt = Lit("M_V_Miner_Shirt", Hex("#EE7F4A"));
            p.Overalls = Lit("M_V_Miner_Overalls", Hex("#3C6CCB"));
            p.OverallsDark = Lit("M_V_Miner_OverallsDark", Hex("#2C519E"));
            p.Skin = Lit("M_V_Miner_Skin", Hex("#F4C7A1"));
            p.SkinShade = Lit("M_V_Miner_SkinShade", Hex("#E3A57F"));
            p.Leather = Lit("M_V_Miner_Leather", Hex("#7B4E33"));
            p.Boots = Lit("M_V_Miner_Boots", Hex("#3B3340"));
            p.Backpack = Lit("M_V_Miner_Backpack", Hex("#B57D4B"));
            p.BackpackDark = Lit("M_V_Miner_BackpackDark", Hex("#8E5E36"));
            p.Dark = Lit("M_V_Miner_Dark", Hex("#2A2530"));
            p.Metal = Lit("M_V_Metal", Hex("#A3AFBE"), 0.45f, 0.6f);
            p.Wood = Lit("M_V_Wood", Hex("#8B5A36"));
            p.Lamp = Lit("M_V_LampGlow", Hex("#FFF3C4"), 0.2f, 0f, Hex("#FFE38A") * 1.6f);
            p.Gold = Lit("M_V_Gold", Hex("#F2C230"), 0.45f, 0.5f);

            // ---- Mediu (verde cald, pământ prăfuit, drum nisipos)
            Texture2D groundNoise = SaveTexture("T_GroundNoise", GroundNoise(256), TextureWrapMode.Repeat);
            p.Grass = Lit("M_V_Grass", Hex("#8FC46A"), 0.05f, 0f, null, groundNoise, new Vector2(6f, 6f));
            p.GrassOuter = Lit("M_V_GrassOuter", Hex("#8FC46A"), 0.05f, 0f, null, groundNoise, new Vector2(28f, 28f));
            p.PitFloor = Lit("M_V_PitFloor", Hex("#B79C7F"), 0.05f, 0f, null, groundNoise, new Vector2(2f, 2f));
            p.Road = Lit("M_V_Road", Hex("#DCC093"), 0.05f, 0f, null, groundNoise, new Vector2(1.5f, 1.5f));
            p.RockA = Lit("M_V_RockA", Hex("#A59C94"));
            p.RockB = Lit("M_V_RockB", Hex("#8D857E"));
            p.Bush = Lit("M_V_Bush", Hex("#5FA855"));
            p.BushDark = Lit("M_V_BushDark", Hex("#4A8C47"));
            p.Fence = Lit("M_V_Fence", Hex("#A0704A"));
            p.Crate = Lit("M_V_Crate", Hex("#C4925E"));
            p.Rail = Lit("M_V_Rail", Hex("#6E7684"), 0.35f, 0.5f);
            p.CartBody = Lit("M_V_CartBody", Hex("#C8553D"));
            p.SellPad = Lit("M_V_SellPad", Hex("#46B86A"));
            p.SellAccent = Lit("M_V_SellAccent", Hex("#7EE89A"), 0.2f, 0f, Hex("#3DCB6A") * 0.8f);
            p.UpgradePad = Lit("M_V_UpgradePad", Hex("#4F7BE0"));
            p.UpgradeAccent = Lit("M_V_UpgradeAccent", Hex("#8FB6FF"), 0.2f, 0f, Hex("#4F86FF") * 0.8f);
            p.LanternGlow = Lit("M_V_LanternGlow", Hex("#FFE7A3"), 0.2f, 0f, Hex("#FFD36B") * 1.4f);
            p.SignBoard = Lit("M_V_SignBoard", Hex("#F6EAD7"));

            // ---- Particule
            Texture2D softCircle = SaveTexture("T_FX_SoftCircle", SoftCircle(64), TextureWrapMode.Clamp, false);
            Texture2D sparkle = SaveTexture("T_FX_Sparkle", Sparkle(64), TextureWrapMode.Clamp, false);
            Texture2D coin = SaveTexture("T_FX_Coin", CoinTexture(64), TextureWrapMode.Clamp, false);
            p.FxDust = ParticleUnlit("M_V_FX_Dust", softCircle, ParticleBlend.Alpha);
            p.FxSparkle = ParticleUnlit("M_V_FX_Sparkle", sparkle, ParticleBlend.Additive);
            p.FxCoin = ParticleUnlit("M_V_FX_Coin", coin, ParticleBlend.Alpha);
            p.FxChunk = ParticleLit("M_V_FX_Chunk");

            BuildUiSprites(p);
            return p;
        }

        // ---------------------------------------------------------------- texturi

        private static Texture2D GroundNoise(int size)
        {
            // Zgomot discret aproape alb: rupe monotonia suprafețelor mari fără să pară „murdar”.
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size, v = y / (float)size;
                float large = TileableNoise(u, v, 4f);
                float small = TileableNoise(u, v, 16f);
                float value = Mathf.Lerp(0.86f, 1f, large * 0.65f + small * 0.35f);
                pixels[y * size + x] = new Color(value, value, value, 1f);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        // Zgomot Perlin care se repetă fără cusături (amestec pe cele 4 colțuri).
        private static float TileableNoise(float u, float v, float scale)
        {
            float a = Mathf.PerlinNoise(u * scale, v * scale);
            float b = Mathf.PerlinNoise((u - 1f) * scale, v * scale);
            float c = Mathf.PerlinNoise(u * scale, (v - 1f) * scale);
            float d = Mathf.PerlinNoise((u - 1f) * scale, (v - 1f) * scale);
            return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
        }

        private static Texture2D SoftCircle(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = new Vector2(x + 0.5f - r, y + 0.5f - r).magnitude / r;
                float a = Mathf.Clamp01(1f - d);
                pixels[y * size + x] = new Color(1f, 1f, 1f, a * a);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D Sparkle(int size)
        {
            Vector2 c = Vector2.one * size * 0.5f;
            return RenderSdf(size, q => Polygon(q, Star(c, size * 0.48f, size * 0.12f, 4)), Color.white, 2f);
        }

        private static Texture2D CoinTexture(int size)
        {
            Vector2 c = Vector2.one * size * 0.5f;
            float r = size * 0.46f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var q = new Vector2(x + 0.5f, y + 0.5f);
                float outer = Circle(q, c, r);
                float inner = Circle(q, c, r * 0.72f);
                float alpha = Mathf.Clamp01(0.5f - outer);
                // Inel exterior mai închis, centru luminos — se citește ca monedă și la 20 px.
                Color color = inner < 0f ? Hex("#FFE27A") : Hex("#E8A923");
                color.a = alpha;
                pixels[y * size + x] = color;
            }
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        // ---------------------------------------------------------------- sprite-uri UI

        private static void BuildUiSprites(Palette p)
        {
            const int s = 128;
            Vector2 c = Vector2.one * s * 0.5f;

            // Dreptunghi rotunjit 9-slice: raza vizibilă se reglează din Image.pixelsPerUnitMultiplier.
            p.RoundRect = SaveSprite("UI_RoundRect",
                RenderSdf(s, q => RoundBox(q, c, c - Vector2.one, 40f), Color.white), new Vector4(44, 44, 44, 44));
            p.Circle = SaveSprite("UI_Circle", RenderSdf(s, q => Circle(q, c, s * 0.5f - 1f), Color.white));
            p.Ring = SaveSprite("UI_Ring", RenderSdf(256, q => Mathf.Abs(Circle(q, Vector2.one * 128f, 112f)) - 9f, Color.white));

            // Monedă: disc cu inel gravat (se citește ca monedă și la 40 px).
            p.IconCoin = SaveSprite("Icon_Coin", RenderSdf(s, q =>
                Mathf.Max(Circle(q, c, 58f), -(Mathf.Abs(Circle(q, c, 40f)) - 5f)), Color.white));

            p.IconStar = SaveSprite("Icon_Star", RenderSdf(s, q => Polygon(q, Star(c, 60f, 27f, 5)) - 3f, Color.white));

            p.IconPause = SaveSprite("Icon_Pause", RenderSdf(s, q => Mathf.Min(
                RoundBox(q, c + new Vector2(-20f, 0f), new Vector2(12f, 38f), 6f),
                RoundBox(q, c + new Vector2(20f, 0f), new Vector2(12f, 38f), 6f)), Color.white));

            p.IconTarget = SaveSprite("Icon_Target", RenderSdf(s, q => Mathf.Min(
                Mathf.Abs(Circle(q, c, 46f)) - 8f,
                Circle(q, c, 16f)), Color.white));

            p.IconBolt = SaveSprite("Icon_Bolt", RenderSdf(s, q => Polygon(q, new[]
            {
                new Vector2(72, 122), new Vector2(30, 58), new Vector2(60, 58), new Vector2(50, 6),
                new Vector2(98, 74), new Vector2(66, 74),
            }) - 2f, Color.white));

            p.IconMagnet = SaveSprite("Icon_Magnet", RenderSdf(s, q =>
            {
                // Potcoavă: semicerc gros jos + două brațe în sus.
                float arc = Mathf.Abs(Circle(q, new Vector2(64f, 56f), 32f)) - 14f;
                arc = Mathf.Max(arc, q.y - 56f);
                // Brațele coboară puțin sub arc (fără cusătură la îmbinare).
                float left = RoundBox(q, new Vector2(32f, 82f), new Vector2(14f, 30f), 3f);
                float right = RoundBox(q, new Vector2(96f, 82f), new Vector2(14f, 30f), 3f);
                return Mathf.Min(arc, Mathf.Min(left, right));
            }, Color.white));

            p.IconChevron = SaveSprite("Icon_Chevron", RenderSdf(s, q => Mathf.Min(
                Segment(q, new Vector2(30f, 52f), new Vector2(64f, 86f), 11f),
                Segment(q, new Vector2(98f, 52f), new Vector2(64f, 86f), 11f)), Color.white));
        }
    }
}
