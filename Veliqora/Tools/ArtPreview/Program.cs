using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using Veliqora.Core.Economy;
using Veliqora.Core.Levels;
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
        Png(Path.Combine(outDir, "board_level_" + 45 + ".png"), Board(eco, levels.Levels[44], "bottle_classic", tiltBottle: 2, tiltDeg: 75f, light: false));
        Png(Path.Combine(outDir, "board_level_" + 170 + "_flask_light.png"), Board(eco, levels.Levels[169], "bottle_flask", -1, 0, light: true));
        Png(Path.Combine(outDir, "bottle_skins.png"), Skins(eco));
        Png(Path.Combine(outDir, "dark_gameplay_crystal_cave_L45.png"), Board(eco, levels.Levels[44], "bottle_classic", 2, 75f, false, "crystal_cave"));
        Png(Path.Combine(outDir, "dark_gameplay_aurora_L276_master.png"), Board(eco, levels.Levels[275], "bottle_classic", -1, 0, false, "aurora"));
        Png(Path.Combine(outDir, "dark_gameplay_neon_falls_L120.png"), Board(eco, levels.Levels[119], "bottle_slim", -1, 0, false, "neon_falls"));
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

    private static PixelPainter Board(EconomyConfig eco, LevelDefinition level, string bottleId, int tiltBottle, float tiltDeg, bool light, string scene = null)
    {
        int W = 1080 / 2, H = 1920 / 2;
        var th = ThemeVisuals.From(eco.FindCosmetic("theme_valley"));
        var p = new PixelPainter(W, H);
        if (scene != null)
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
    private static void DrawBottle(PixelPainter p, BottleShape shape, int capacity, int[] units, Vector2 pivot, float width, float tiltDeg, Color? glassTint = null)
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
            while (i < units.Length && units[i] == col) { run++; i++; }
            cum += run;
            float upper = LiquidGeometry.LevelForArea(interior, up, cum / capacity * total, minC, maxC, scratch);
            LiquidGeometry.Clip(interior, up, upper, true, a);
            LiquidGeometry.Clip(a, up, lower, false, b);
            if (b.Count >= 3)
            {
                var world = new Vector2[b.Count];
                for (int k = 0; k < b.Count; k++) world[k] = pivot + Rot(b[k]);
                var color = LiquidPalette.Get(col);
                p.Fill(q => PixelPainter.Polygon(q, world), color, q => Color.Lerp(color, Color.white, 0.12f * Mathf.Clamp01((Vector2.Dot(q - pivot, new Vector2(0, 1)) - lower) / Mathf.Max(1, upper - lower))));
            }
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
