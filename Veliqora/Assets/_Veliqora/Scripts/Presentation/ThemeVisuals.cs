using System.Collections.Generic;
using UnityEngine;
using Veliqora.Core.Economy;
using Veliqora.Core.Json;
using Veliqora.UI;

namespace Veliqora.Presentation
{
    /// <summary>Resolved color set for a theme cosmetic (from economy.json "visual").</summary>
    public sealed class ThemeVisuals
    {
        public string Id = "theme_valley";
        public Color SkyTop = UIStyle.Hex("#7EC8F2");
        public Color SkyBottom = UIStyle.Hex("#D9F1FB");
        public Color Mountain = UIStyle.Hex("#9DB7D9");
        public Color Hill = UIStyle.Hex("#7CCB8B");
        public Color HillDark = UIStyle.Hex("#4FA86A");
        public Color River = UIStyle.Hex("#5BC0EB");
        public Color Accent = UIStyle.Hex("#FF8FB1");
        public Color GameTop = UIStyle.Hex("#1D2A55");
        public Color GameBottom = UIStyle.Hex("#2E5DA8");

        public bool IsDarkSky => SkyTop.grayscale < 0.35f;

        public static ThemeVisuals From(CosmeticItem item)
        {
            var t = new ThemeVisuals();
            if (item == null) return t;
            t.Id = item.Id;
            var v = item.Visual;
            t.SkyTop = Col(v, "skyTop", t.SkyTop);
            t.SkyBottom = Col(v, "skyBottom", t.SkyBottom);
            t.Mountain = Col(v, "mountain", t.Mountain);
            t.Hill = Col(v, "hill", t.Hill);
            t.HillDark = Col(v, "hillDark", t.HillDark);
            t.River = Col(v, "river", t.River);
            t.Accent = Col(v, "accent", t.Accent);
            t.GameTop = Col(v, "gameTop", t.GameTop);
            t.GameBottom = Col(v, "gameBottom", t.GameBottom);
            return t;
        }

        internal static Color Col(Dictionary<string, object> v, string key, Color fallback)
        {
            var s = MiniJson.GetString(v, key, null);
            if (string.IsNullOrEmpty(s)) return fallback;
            Color c;
            return UIStyle.TryParseHex(s, out c) ? c : fallback;
        }
    }

    public enum BottleShapeKind { Classic, Slim, Flask, Crystal }

    /// <summary>
    /// Bottle geometry in normalized units (bottle width = 1). Provides the glass sprite and the convex liquid
    /// interior polygon used by LiquidGraphic, so liquid always sits exactly inside the drawn glass.
    /// </summary>
    public sealed class BottleShape
    {
        public string Id = "bottle_classic";
        public BottleShapeKind Kind;
        public Color Tint = Color.white;
        public float BodyWidth = 0.92f;
        public float NeckWidth = 0.62f;
        public float BottomRadius = 0.36f;
        public float Glass = 0.06f;      // wall thickness
        public float Aspect;             // height / width
        public float BodyTop;            // y where shoulders start (normalized)
        public float FillTop;            // max liquid height (capacity line)

        public static BottleShape From(CosmeticItem item, int capacity)
        {
            var s = new BottleShape();
            string shape = item != null ? MiniJson.GetString(item.Visual, "shape", "classic") : "classic";
            if (item != null)
            {
                s.Id = item.Id;
                s.Tint = ThemeVisuals.Col(item.Visual, "tint", Color.white);
            }
            float aspect = 1.0f + 0.55f * capacity;
            switch (shape)
            {
                case "slim": s.Kind = BottleShapeKind.Slim; s.BodyWidth = 0.8f; s.NeckWidth = 0.6f; s.BottomRadius = 0.4f; aspect *= 1.06f; break;
                case "flask": s.Kind = BottleShapeKind.Flask; s.BodyWidth = 0.96f; s.NeckWidth = 0.46f; s.BottomRadius = 0.48f; aspect *= 1.02f; break;
                case "crystal": s.Kind = BottleShapeKind.Crystal; s.BodyWidth = 0.94f; s.NeckWidth = 0.64f; s.BottomRadius = 0.22f; break;
                default: s.Kind = BottleShapeKind.Classic; break;
            }
            s.Aspect = aspect;
            s.BodyTop = aspect * 0.87f;
            s.FillTop = s.BodyTop - s.BottomRadius * 0.35f;
            return s;
        }

        /// <summary>Convex interior polygon (normalized, origin at bottom-center), counter-clockwise.</summary>
        public List<Vector2> InteriorPolygon()
        {
            var pts = new List<Vector2>();
            float hw = BodyWidth * 0.5f - Glass;
            float r = Mathf.Max(0.02f, BottomRadius - Glass);
            float bottom = Glass;
            if (Kind == BottleShapeKind.Crystal)
            {
                pts.Add(new Vector2(-hw + r, bottom));
                pts.Add(new Vector2(hw - r, bottom));
                pts.Add(new Vector2(hw, bottom + r));
            }
            else
            {
                // bottom-left arc (180..270deg), bottom-right arc (270..360deg)
                const int seg = 6;
                for (int i = 0; i <= seg; i++)
                {
                    float a = Mathf.PI + i * (Mathf.PI * 0.5f) / seg;
                    pts.Add(new Vector2(-hw + r + Mathf.Cos(a) * r, bottom + r + Mathf.Sin(a) * r));
                }
                for (int i = 0; i <= seg; i++)
                {
                    float a = 1.5f * Mathf.PI + i * (Mathf.PI * 0.5f) / seg;
                    pts.Add(new Vector2(hw - r + Mathf.Cos(a) * r, bottom + r + Mathf.Sin(a) * r));
                }
            }
            pts.Add(new Vector2(hw, FillTop));
            pts.Add(new Vector2(-hw, FillTop));
            if (Kind == BottleShapeKind.Crystal) pts.Add(new Vector2(-hw, bottom + r));
            return pts;
        }

        /// <summary>Normalized mouth position (top center) for pour geometry.</summary>
        public Vector2 Mouth => new Vector2(0, Aspect);

        private static readonly Dictionary<string, Sprite> GlassCache = new Dictionary<string, Sprite>();

        /// <summary>Procedural glass sprite (PLACEHOLDER ART): faint body, bright rim, highlights.</summary>
        public Sprite GlassSprite()
        {
            string key = Id + "_" + Aspect.ToString("0.00");
            Sprite cached;
            if (GlassCache.TryGetValue(key, out cached) && cached != null) return cached;

            var p = PaintGlass(160);
            var tex = p.ToTexture();
            var sprite = Sprite.Create(tex, new Rect(0, 0, p.Width, p.Height), new Vector2(0.5f, 0f), 100f);
            sprite.name = "proc_glass_" + key;
            GlassCache[key] = sprite;
            return sprite;
        }

        /// <summary>Rasterizes the glass (pure managed; used by GlassSprite and the art preview tool).</summary>
        public PixelPainter PaintGlass(int W)
        {
            int H = Mathf.RoundToInt(W * Aspect);
            float k = W; // px per normalized unit
            var p = new PixelPainter(W, H);
            return PaintGlassInto(p, W, H, k);
        }

        private PixelPainter PaintGlassInto(PixelPainter p, int W, int H, float k)
        {
            float cx = W * 0.5f;
            float bodyHalf = BodyWidth * 0.5f * k;
            float neckHalf = NeckWidth * 0.5f * k;
            float rad = BottomRadius * k;
            float bodyTop = BodyTop * k;
            float top = H - 2;
            float t = Glass * k;

            System.Func<Vector2, float> outer = q =>
            {
                float body = Kind == BottleShapeKind.Crystal
                    ? PixelPainter.Polygon(q, new[] { new Vector2(cx - bodyHalf + rad, 2), new Vector2(cx + bodyHalf - rad, 2), new Vector2(cx + bodyHalf, 2 + rad), new Vector2(cx + bodyHalf, bodyTop), new Vector2(cx - bodyHalf, bodyTop), new Vector2(cx - bodyHalf, 2 + rad) })
                    : PixelPainter.RoundedRect(q, new Rect(cx - bodyHalf, 2, bodyHalf * 2, bodyTop - 2 + rad * 0.6f), rad);
                float neck = PixelPainter.RoundedRect(q, new Rect(cx - neckHalf, bodyTop - rad * 0.4f, neckHalf * 2, top - bodyTop + rad * 0.4f), t * 1.2f);
                return Mathf.Min(body, neck);
            };
            System.Func<Vector2, float> inner = q => outer(q) + t;

            var tint = Tint;
            // body glass
            p.Fill(outer, new Color(tint.r, tint.g, tint.b, 0.16f));
            // wall stroke
            p.Fill(q => PixelPainter.Subtract(outer(q), inner(q)), new Color(tint.r, tint.g, tint.b, 0.85f));
            // lip ring
            p.Fill(q => PixelPainter.RoundedRect(q, new Rect(cx - neckHalf - t * 0.6f, top - t * 2.2f, neckHalf * 2 + t * 1.2f, t * 2.2f), t), new Color(1, 1, 1, 0.95f));
            // left highlight streak
            p.Fill(q => PixelPainter.Segment(q, new Vector2(cx - bodyHalf + t * 2.6f, rad * 0.9f), new Vector2(cx - bodyHalf + t * 2.6f, bodyTop - rad * 0.2f), t * 0.75f), new Color(1, 1, 1, 0.55f));
            p.Fill(q => PixelPainter.Segment(q, new Vector2(cx - neckHalf + t * 2.2f, bodyTop + t), new Vector2(cx - neckHalf + t * 2.2f, top - t * 3.5f), t * 0.45f), new Color(1, 1, 1, 0.4f));
            // right soft reflection
            p.Fill(q => PixelPainter.Segment(q, new Vector2(cx + bodyHalf - t * 2.2f, bodyTop * 0.45f), new Vector2(cx + bodyHalf - t * 2.2f, bodyTop * 0.75f), t * 0.4f), new Color(1, 1, 1, 0.28f));
            if (Kind == BottleShapeKind.Crystal)
            {
                p.Fill(q => PixelPainter.Segment(q, new Vector2(cx, 4 + t), new Vector2(cx, bodyTop - t), 1.2f), new Color(1, 1, 1, 0.22f));
            }

            return p;
        }
    }
}
