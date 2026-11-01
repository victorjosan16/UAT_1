using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using Veliqora.Core.Economy;
using Veliqora.Core.Levels;
using Veliqora.Core.Puzzle;
using Veliqora.Presentation;
using Veliqora.UI;

internal static class Program
{
    private static string _root;

    private static void Main(string[] args)
    {
        string outDir = args.Length > 0 ? args[0] : "Docs/ArtPreview";
        Directory.CreateDirectory(outDir);
        _root = FindRoot();
        var eco = EconomyConfig.FromJson(File.ReadAllText(Path.Combine(_root, "Assets/_Veliqora/Resources/Veliqora/Config/economy.json")));
        var levels = LevelCatalog.FromJson(File.ReadAllText(Path.Combine(_root, "Assets/_Veliqora/Resources/Veliqora/Levels/levels.json")));

        foreach (var id in new[] { "theme_valley", "theme_sunset", "theme_night" })
            Png(Path.Combine(outDir, "valley_" + id + ".png"), BackgroundArt.PaintValley(ThemeVisuals.From(eco.FindCosmetic(id)), 540, 960));
        Png(Path.Combine(outDir, "map_tile_valley.png"), BackgroundArt.PaintMapTile(ThemeVisuals.From(eco.FindCosmetic("theme_valley"))));
        Png(Path.Combine(outDir, "icons.png"), IconSheet());

        // QA renders of levels 1, 5, 11, 25, 50, 100 (offline, real art code + real layout; not Unity screenshots)
        foreach (int n in new[] { 1, 5, 11, 25, 50, 100 })
            Png(Path.Combine(outDir, $"qa_level_{n:000}.png"), LevelShot(eco, levels.Levels[n - 1]));
        Png(Path.Combine(outDir, "clean_level_001.png"), Board(eco, levels.Levels[0], "bottle_classic", -1, 0, light: false, themeId: "theme_clean"));
        Png(Path.Combine(outDir, "clean_level_060.png"), Board(eco, levels.Levels[59], "bottle_classic", tiltBottle: 1, tiltDeg: 60f, light: false, themeId: "theme_clean"));
        Png(Path.Combine(outDir, "board_level_" + 45 + ".png"), Board(eco, levels.Levels[44], "bottle_classic", tiltBottle: 2, tiltDeg: 75f, light: false));
        Png(Path.Combine(outDir, "board_level_" + 170 + "_flask_light.png"), Board(eco, levels.Levels[169], "bottle_flask", -1, 0, light: true));
        Png(Path.Combine(outDir, "bottle_skins.png"), Skins(eco));
        Png(Path.Combine(outDir, "dark_gameplay_crystal_cave_L45.png"), Board(eco, levels.Levels[44], "bottle_classic", 2, 75f, false, "crystal_cave"));
        Png(Path.Combine(outDir, "dark_gameplay_aurora_L276_master.png"), Board(eco, levels.Levels[275], "bottle_classic", -1, 0, false, "aurora_night"));
        Png(Path.Combine(outDir, "dark_gameplay_neon_falls_L120.png"), Board(eco, levels.Levels[119], "bottle_slim", -1, 0, false, "moonlight_falls"));
        Png(Path.Combine(outDir, "tilt_sequence.png"), TiltSequence(eco));
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var sheet = new PixelPainter(6 * 270, 2 * 480);
        for (int i = 0; i < DarkScenes.Ids.Length; i++)
        {
            var scene = DarkScenes.Paint(DarkScenes.Ids[i], 540, 960);
            if (i == 0) Console.WriteLine("one scene: " + sw.ElapsedMilliseconds + " ms");
            // 2x downsample into the contact sheet
            int ox = (i % 6) * 270, oy = (1 - i / 6) * 480;
            for (int y = 0; y < 480; y++)
                for (int x = 0; x < 270; x++)
                {
                    var c = scene.Get(x * 2, y * 2);
                    sheet.Pixels[(oy + y) * sheet.Width + ox + x] = new Color(c.r, c.g, c.b, 1);
                }
        }
        Console.WriteLine("12 scenes: " + sw.ElapsedMilliseconds + " ms");
        Png(Path.Combine(outDir, "dark_backgrounds_12.png"), sheet);

        // chapter backgrounds (painted at the in-game 720x1280, shown at 1/2)
        var chapters = new PixelPainter(360 * 5, 640);
        for (int i = 0; i < DarkScenes.ChapterScenes.Length; i++)
        {
            var sw2 = System.Diagnostics.Stopwatch.StartNew();
            var scene = DarkScenes.Paint(DarkScenes.ChapterScenes[i], 720, 1280);
            Console.WriteLine(DarkScenes.ChapterScenes[i] + ": " + sw2.ElapsedMilliseconds + " ms");
            for (int y = 0; y < 640; y++)
                for (int x = 0; x < 360; x++)
                {
                    var c = scene.Get(x * 2, y * 2);
                    chapters.Pixels[y * chapters.Width + i * 360 + x] = new Color(c.r, c.g, c.b, 1);
                }
        }
        Png(Path.Combine(outDir, "chapter_backgrounds_5.png"), chapters);
        Console.WriteLine("Wrote previews to " + Path.GetFullPath(outDir));
    }

    private static string FindRoot()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d != null && !Directory.Exists(Path.Combine(d.FullName, "Assets", "_Veliqora"))) d = d.Parent;
        return d.FullName;
    }

    private static PixelPainter IconSheet()
    {
        var icons = (Icon[])Enum.GetValues(typeof(Icon));
        int cols = 6, cell = 150;
        int rows = (icons.Length + cols - 1) / cols;
        var sheet = new PixelPainter(cols * cell, rows * cell, UIStyle.Navy);
        for (int i = 0; i < icons.Length; i++)
        {
            var ip = SpriteLibrary.PaintIcon(icons[i]);
            int cx = (i % cols) * cell + 11, cy = (rows - 1 - i / cols) * cell + 11;
            Blit(sheet, ip, cx, cy);
        }
        return sheet;
    }

    private static PixelPainter Skins(EconomyConfig eco)
    {
        var ids = new[] { "bottle_classic", "bottle_slim", "bottle_flask", "bottle_crystal" };
        var p = new PixelPainter(4 * 220, 520);
        var th = ThemeVisuals.From(eco.FindCosmetic("theme_valley"));
        p.VerticalGradient(th.GameBottom, th.GameTop);
        for (int i = 0; i < ids.Length; i++)
        {
            var shape = BottleShape.From(eco.FindCosmetic(ids[i]), 4);
            DrawBottle(p, shape, 4, new[] { 1, 5, 2, 4 }, new Vector2(110 + i * 220, 40), 120, 0f);
        }
        return p;
    }

    private static PixelPainter TiltSequence(EconomyConfig eco)
    {
        var p = new PixelPainter(1000, 520);
        var th = ThemeVisuals.From(eco.FindCosmetic("theme_valley"));
        p.VerticalGradient(th.GameBottom, th.GameTop);
        var shape = BottleShape.From(eco.FindCosmetic("bottle_classic"), 4);
        float[] angles = { 0f, 30f, 60f, 90f };
        for (int i = 0; i < angles.Length; i++)
            DrawBottle(p, shape, 4, new[] { 3, 4, 1 }, new Vector2(130 + i * 240, 60 + i * 25), 110, angles[i]);
        return p;
    }

    private static PixelPainter Board(EconomyConfig eco, LevelDefinition level, string bottleId, int tiltBottle, float tiltDeg, bool light, string scene = null, string themeId = "theme_valley")
    {
        int W = 1080 / 2, H = 1920 / 2;
        var th = ThemeVisuals.From(eco.FindCosmetic(themeId));
        var p = new PixelPainter(W, H);
        if (th.Flat)
        {
            var bg = BackgroundArt.PaintClean(th, W, H);
            System.Array.Copy(bg.Pixels, p.Pixels, bg.Pixels.Length);
        }
        else if (scene != null)
        {
            var bg = DarkScenes.Paint(scene, W, H);
            System.Array.Copy(bg.Pixels, p.Pixels, bg.Pixels.Length);
            // veil + edge gradients like ReadabilityScrim(0.45, 0.5, 0.12)
            for (int y = 0; y < H; y++)
            {
                float v = y / (float)(H - 1);
                float a = Mathf.Max(0.5f * Mathf.Pow(Mathf.Clamp01(1f - v / 0.32f), 1.6f), 0.45f * Mathf.Pow(Mathf.Clamp01((v - 0.72f) / 0.28f), 1.6f));
                a = 1 - (1 - a) * (1 - 0.12f);
                for (int x = 0; x < W; x++) p.Blend(x, y, new Color(0.04f, 0.05f, 0.15f, a));
            }
        }
        else if (light) p.VerticalGradient(UIStyle.Hex("#F3EBDD"), UIStyle.Hex("#FFFBF3"));
        else p.VerticalGradient(th.GameBottom, th.GameTop);
        var shape = BottleShape.From(eco.FindCosmetic(bottleId), level.Capacity);
        int n = level.Bottles.Length;
        int rows = n <= 5 ? 1 : (n <= 12 ? 2 : 3);
        int perRow = (n + rows - 1) / rows;
        float bw = Mathf.Min(W / (perRow * 1.5f + 0.4f), (H * 0.6f) / (rows * shape.Aspect * 1.32f));
        float rowH = bw * shape.Aspect * 1.32f;
        int idx = 0;
        for (int r = 0; r < rows; r++)
        {
            int inRow = Math.Min(perRow, n - idx);
            float rowW = inRow * bw + (inRow - 1) * bw * 0.5f;
            float y = H * 0.55f - (r + 1) * rowH + rowH * 0.5f;
            for (int c = 0; c < inRow; c++, idx++)
            {
                float x = W * 0.5f - rowW * 0.5f + c * bw * 1.5f + bw * 0.5f;
                bool tilt = idx == tiltBottle;
                var units = level.Bottles[idx];
                if (scene != null && units.Length > 0)
                {
                    var top = LiquidPalette.Get(units[units.Length - 1]);
                    bool done = units.Length == level.Capacity && System.Array.TrueForAll(units, u => u == units[0]);
                    p.Glow(new Vector2(x, y + bw * shape.Aspect * 0.45f), bw * 1.3f, new Color(top.r, top.g, top.b, done ? 0.34f : 0.16f), 1.6f);
                }
                DrawBottle(p, shape, level.Capacity, level.Bottles[idx], new Vector2(x, y + (tilt ? bw * 0.6f : 0)), bw, tilt ? tiltDeg : 0f, light ? UIStyle.Hex("#7F8DB0") : Color.white);
            }
        }
        return p;
    }

    /// <summary>Same math as LiquidGraphic: world-level bands by area, clipped to the convex interior; then glass on top.</summary>
    /// <summary>
    /// Offline approximation of the gameplay screen at half of 1080×1920: chapter background, modern header/moves
    /// pills, booster tray, bottles placed by Core.Layout.BoardLayout with the in-game glass and liquid shading,
    /// and simplified obstacle overlays (mystery smoke, ice slab + snowflake, chains + padlock). No text rendering.
    /// </summary>
    private static PixelPainter LevelShot(EconomyConfig eco, LevelDefinition level)
    {
        const int W = 540, H = 960;
        var p = new PixelPainter(W, H);
        var bg = DarkScenes.Paint(DarkScenes.ChapterScene(Veliqora.Core.Progression.Chapters.IndexOf(level.Number)), W, H);
        System.Array.Copy(bg.Pixels, p.Pixels, bg.Pixels.Length);
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) p.Blend(x, y, new Color(0.03f, 0.05f, 0.12f, 0.1f)); // readability veil
        var ivory = UIStyle.Ivory;
        System.Action<Rect, float, Color, Color?> Pill = (r, rad, c, outline) =>
        {
            if (outline.HasValue) p.Fill(q => PixelPainter.RoundedRect(q, new Rect(r.x - 1.5f, r.y - 1.5f, r.width + 3, r.height + 3), rad + 1.5f), outline.Value);
            p.Fill(q => PixelPainter.RoundedRect(q, r, rad), c);
        };
        // header (y up: top of screen = H)
        Pill(new Rect(W / 2f - 20 - 95, H - 85 - 29 + 19, 190, 58), 22, UIStyle.WithAlpha(ivory, 0.8f), UIStyle.GlassOutline);
        Pill(new Rect(W / 2f - 20 - 105, H - 85 - 13 - 22, 210, 26), 13, UIStyle.WithAlpha(UIStyle.Card, 0.88f), UIStyle.WithAlpha(UIStyle.ThemeAccent, 0.6f));
        Pill(new Rect(12, H - 85 - 29, 58, 58), 17, UIStyle.Violet, new Color(1, 1, 1, 0.18f));                    // pause
        Pill(new Rect(W - 12 - 165, H - 85 - 28, 165, 56), 24, new Color(0.08f, 0.1f, 0.25f, 0.6f), null);         // coins
        DrawIcon(p, Icon.Pause, new Vector2(41, H - 85), 30);
        DrawIcon(p, Icon.Coin, new Vector2(W - 12 - 140, H - 85), 40);
        bool objectives = level.Objectives.Count > 0;
        if (objectives)
            for (int i = 0; i < level.Objectives.Count; i++)
            {
                float w = Mathf.Min(160f, 490f / level.Objectives.Count);
                float x0 = W / 2f + (i - (level.Objectives.Count - 1) * 0.5f) * (w + 6) - w / 2;
                Pill(new Rect(x0, H - 85 - 49 - 21 - 28, w, 42), 13, new Color(0.07f, 0.06f, 0.18f, 0.75f), UIStyle.WithAlpha(UIStyle.ThemeAccent, 0.5f));
            }
        // booster tray + five squircle buttons
        Pill(new Rect(15, 23, W - 30, 144), 32, UIStyle.WithAlpha(ivory, 0.75f), UIStyle.GlassOutline);
        Icon[] icons = { Icon.Undo, Icon.BottlePlus, Icon.Shuffle, Icon.Droplet, Icon.Bulb };
        string[] fills = { "#3D8BFF", "#36B0C9", "#8E5CF7", "#E2557A", "#F5A623" };
        for (int i = 0; i < 5; i++)
        {
            float cx = 20 + (W - 40) * (0.1f + i * 0.2f);
            var f = UIStyle.Hex(fills[i]);
            Pill(new Rect(cx - 37, 72, 75, 75), 22, UIStyle.Darken(f, 0.32f), null);
            Pill(new Rect(cx - 37, 74.5f, 75, 72.5f), 22, f, new Color(1, 1, 1, 0.18f));
            DrawIcon(p, icons[i], new Vector2(cx, 112), 40);
            p.Fill(q => PixelPainter.Circle(q, new Vector2(cx + 33, 141), 15), UIStyle.Coral);
        }
        // board area (same insets as GameplayScreen: bottom 360, top bar 170 + 110 + objectives 96, sides 30) at half scale
        float top = H - (170 + 110 + (objectives ? 96 : 0)) / 2f, bottom = 180f;
        float areaW = W - 30, areaH = top - bottom;
        var start = level.CreateInitialState();
        var shapes = new BottleShape[start.BottleCount];
        float aspect = 0f;
        for (int i = 0; i < start.BottleCount; i++)
        {
            int cap = start[i].Capacity;
            string skin = cap == 4 ? "bottle_classic" : (cap < 4 ? "bottle_slim" : "bottle_flask");
            shapes[i] = BottleShape.From(eco.FindCosmetic(skin), cap);
            aspect = Mathf.Max(aspect, shapes[i].Aspect);
        }
        var lay = Veliqora.Core.Layout.BoardLayout.Choose(start.BottleCount, areaW * 2, areaH * 2, aspect); // layout works in reference px
        var xs = new float[start.BottleCount]; var ys = new float[start.BottleCount];
        Veliqora.Core.Layout.BoardLayout.Positions(start.BottleCount, lay, xs, ys);
        float cxBoard = W / 2f, cyBoard = (top + bottom) / 2f;
        for (int i = 0; i < start.BottleCount; i++)
        {
            var b = start[i];
            float bw = lay.BottleWidth / 2f;
            var pivot = new Vector2(cxBoard + xs[i] / 2f, cyBoard + ys[i] / 2f);
            var units = new int[b.Count];
            for (int u = 0; u < b.Count; u++) units[u] = b[u];
            DrawBottle(p, shapes[i], b.Capacity, units, pivot, bw, 0f, null, b.Hidden);
            float bh = bw * shapes[i].Aspect;
            if (b.RewardLocked) DrawIcon(p, Icon.Ad, pivot + new Vector2(0, bh * 0.5f), bw * 0.8f);
            if (b.Lock != null && b.Lock.IsFrozen)
            {
                p.Fill(q => PixelPainter.RoundedRect(q, new Rect(pivot.x - bw * 0.54f, pivot.y - 1, bw * 1.08f, bh * 1.02f), bw * 0.25f), new Color(0.72f, 0.9f, 1f, 0.42f));
                DrawIcon(p, Icon.Snowflake, pivot + new Vector2(0, bh * 0.62f), bw * 0.62f);
            }
            else if (b.Lock != null)
            {
                for (int k = 0; k < 2; k++)
                {
                    float sgn = k == 0 ? 1 : -1;
                    var c0 = pivot + new Vector2(-bw * 0.62f, bh * 0.5f - sgn * bw * 0.33f);
                    var c1 = pivot + new Vector2(bw * 0.62f, bh * 0.5f + sgn * bw * 0.33f);
                    p.Fill(q => PixelPainter.Segment(q, c0, c1, Mathf.Max(2f, bw * 0.05f)), new Color(0.78f, 0.8f, 0.86f, 0.9f));
                }
                var keyColor = b.Lock.Kind == LockKind.CompleteColor ? LiquidPalette.Get(b.Lock.Param) : UIStyle.Hex("#E7C46A");
                DrawIcon(p, Icon.Lock, pivot + new Vector2(0, bh * 0.47f), bw * 0.66f, keyColor);
            }
        }
        return p;
    }

    private static void DrawIcon(PixelPainter p, Icon icon, Vector2 center, float size, Color? tint = null)
    {
        var src = SpriteLibrary.PaintIcon(icon);
        for (int y = 0; y < (int)size; y++)
            for (int x = 0; x < (int)size; x++)
            {
                var c = src.Get((int)(x / size * src.Width), (int)(y / size * src.Height));
                if (c.a < 0.01f) continue;
                if (tint.HasValue) c = new Color(c.r * tint.Value.r, c.g * tint.Value.g, c.b * tint.Value.b, c.a);
                int px = (int)(center.x - size / 2 + x), py = (int)(center.y - size / 2 + y);
                if (px >= 0 && py >= 0 && px < p.Width && py < p.Height) p.Blend(px, py, c);
            }
    }

    private static void DrawBottle(PixelPainter p, BottleShape shape, int capacity, int[] units, Vector2 pivot, float width, float tiltDeg, Color? glassTint = null, int hidden = 0)
    {
        float th = tiltDeg * Mathf.Deg2Rad;
        Vector2 Rot(Vector2 v) => new Vector2(Mathf.Cos(th) * v.x - Mathf.Sin(th) * v.y, Mathf.Sin(th) * v.x + Mathf.Cos(th) * v.y);
        var interior = new List<Vector2>();
        foreach (var v in shape.InteriorPolygon()) interior.Add(v * width);
        var up = new Vector2(Mathf.Sin(th), Mathf.Cos(th)); // world up in bottle-local space
        float total = LiquidGeometry.Area(interior);
        float minC = float.MaxValue, maxC = float.MinValue;
        foreach (var v in interior) { float c = Vector2.Dot(v, up); minC = Mathf.Min(minC, c); maxC = Mathf.Max(maxC, c); }
        var scratch = new List<Vector2>();
        var a = new List<Vector2>();
        var b = new List<Vector2>();
        float lower = minC - 1, cum = 0;
        int i = 0;
        while (i < units.Length)
        {
            int col = units[i], run = 0;
            bool smoke = i < hidden;
            while (i < units.Length && (smoke ? i < hidden : units[i] == col)) { run++; i++; }
            cum += run;
            float upper = LiquidGeometry.LevelForArea(interior, up, cum / capacity * total, minC, maxC, scratch);
            var color = smoke ? new Color(0.24f, 0.21f, 0.34f, 1f) : LiquidPalette.Get(col);
            bool topLayer = i >= units.Length;
            float seam = Mathf.Max(1f, 0.014f * width), surfaceBand = 0.05f * width;
            // same shading as LiquidGraphic: light vertical gradient, hairline seam, meniscus on the top surface
            void Band(float lo, float hi, Color c0, Color c1)
            {
                LiquidGeometry.Clip(interior, up, hi, true, a);
                LiquidGeometry.Clip(a, up, lo, false, b);
                if (b.Count < 3) return;
                var world = new Vector2[b.Count];
                for (int k = 0; k < b.Count; k++) world[k] = pivot + Rot(b[k]);
                p.Fill(q => PixelPainter.Polygon(q, world), c0, q =>
                {
                    var local = q - pivot;
                    var bl = new Vector2(Mathf.Cos(-th) * local.x - Mathf.Sin(-th) * local.y, Mathf.Sin(-th) * local.x + Mathf.Cos(-th) * local.y);
                    float t = Mathf.Clamp01((Vector2.Dot(bl, up) - lo) / Mathf.Max(1f, hi - lo));
                    return Color.Lerp(c0, c1, t);
                });
            }
            var bottomC = Color.Lerp(color, Color.black, 0.07f);
            var topC = Color.Lerp(color, Color.white, 0.06f);
            if (topLayer && upper - lower > surfaceBand * 1.5f)
            {
                Band(lower, upper - surfaceBand, bottomC, topC);
                var surf = Color.Lerp(color, Color.white, 0.22f);
                Band(upper - surfaceBand, upper - seam, surf, surf);
                var rim = Color.Lerp(color, Color.white, 0.55f);
                Band(upper - seam, upper, rim, rim);
            }
            else if (!topLayer && upper - lower > seam * 3f)
            {
                Band(lower, upper - seam, bottomC, topC);
                var line = Color.Lerp(color, Color.black, 0.22f);
                Band(upper - seam, upper, line, line);
            }
            else Band(lower, upper, bottomC, topC);
            lower = upper;
        }
        // glass: rasterize at bottle resolution, then inverse-map into the rotated frame (bilinear-free nearest)
        var glass = shape.PaintGlass(Mathf.RoundToInt(width));
        float r = (float)Math.Sqrt(glass.Width * glass.Width + glass.Height * glass.Height);
        for (int py = (int)(pivot.y - r); py <= (int)(pivot.y + r); py++)
            for (int px = (int)(pivot.x - r); px <= (int)(pivot.x + r); px++)
            {
                if (px < 0 || py < 0 || px >= p.Width || py >= p.Height) continue;
                var d = new Vector2(px + 0.5f - pivot.x, py + 0.5f - pivot.y);
                // rotate by -th back into bottle space
                var l = new Vector2(Mathf.Cos(-th) * d.x - Mathf.Sin(-th) * d.y, Mathf.Sin(-th) * d.x + Mathf.Cos(-th) * d.y);
                int gx = (int)(l.x + glass.Width * 0.5f), gy = (int)l.y;
                if (gx < 0 || gy < 0 || gx >= glass.Width || gy >= glass.Height) continue;
                var c = glass.Get(gx, gy);
                if (c.a > 0.01f) p.Blend(px, py, glassTint.HasValue ? c * glassTint.Value : c);
            }
    }

    private static void Blit(PixelPainter dst, PixelPainter src, int ox, int oy)
    {
        for (int y = 0; y < src.Height; y++)
            for (int x = 0; x < src.Width; x++)
            {
                int dx = ox + x, dy = oy + y;
                if (dx < dst.Width && dy < dst.Height) dst.Blend(dx, dy, src.Get(x, y));
            }
    }

    // ---- minimal PNG writer (RGBA8, flips Unity's bottom-up rows) ----
    private static void Png(string path, PixelPainter p)
    {
        int w = p.Width, h = p.Height;
        var raw = new byte[(w * 4 + 1) * h];
        int o = 0;
        for (int y = h - 1; y >= 0; y--)
        {
            raw[o++] = 0;
            for (int x = 0; x < w; x++)
            {
                var c = p.Get(x, y);
                raw[o++] = (byte)(Mathf.Clamp01(c.r) * 255); raw[o++] = (byte)(Mathf.Clamp01(c.g) * 255);
                raw[o++] = (byte)(Mathf.Clamp01(c.b) * 255); raw[o++] = (byte)(Mathf.Clamp01(c.a) * 255);
            }
        }
        using var fs = File.Create(path);
        fs.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var ihdr = new byte[13];
        BE(ihdr, 0, (uint)w); BE(ihdr, 4, (uint)h); ihdr[8] = 8; ihdr[9] = 6;
        Chunk(fs, "IHDR", ihdr);
        using (var ms = new MemoryStream())
        {
            using (var z = new ZLibStream(ms, System.IO.Compression.CompressionLevel.Optimal, true)) z.Write(raw);
            Chunk(fs, "IDAT", ms.ToArray());
        }
        Chunk(fs, "IEND", Array.Empty<byte>());
    }

    private static void BE(byte[] b, int i, uint v) { b[i] = (byte)(v >> 24); b[i + 1] = (byte)(v >> 16); b[i + 2] = (byte)(v >> 8); b[i + 3] = (byte)v; }

    private static void Chunk(Stream s, string type, byte[] data)
    {
        var len = new byte[4]; BE(len, 0, (uint)data.Length); s.Write(len);
        var td = new byte[4 + data.Length];
        System.Text.Encoding.ASCII.GetBytes(type).CopyTo(td, 0); data.CopyTo(td, 4);
        s.Write(td);
        var crc = new byte[4]; BE(crc, 0, Crc(td)); s.Write(crc);
    }

    private static uint Crc(byte[] d)
    {
        uint c = 0xFFFFFFFF;
        foreach (var b in d) { c ^= b; for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320 ^ (c >> 1) : c >> 1; }
        return ~c;
    }
}
