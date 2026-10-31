using System.Collections.Generic;
using UnityEngine;

namespace Veliqora.Presentation
{
    /// <summary>
    /// Pure geometry for liquid rendering (no engine calls): half-plane clipping of the convex bottle interior
    /// and area-preserving level search. Shared by LiquidGraphic and the offline art preview tool.
    /// </summary>
    public static class LiquidGeometry
    {
        /// <summary>Binary search the level c so that area(interior ∩ {dot(p,up) ≤ c}) == target.</summary>
        public static float LevelForArea(List<Vector2> interior, Vector2 up, float target, float minC, float maxC, List<Vector2> scratch)
        {
            if (target <= 0f) return minC;
            float lo = minC, hi = maxC;
            for (int it = 0; it < 22; it++)
            {
                float mid = (lo + hi) * 0.5f;
                Clip(interior, up, mid, true, scratch);
                if (Area(scratch) < target) lo = mid; else hi = mid;
            }
            return (lo + hi) * 0.5f;
        }

        /// <summary>Sutherland–Hodgman against the half-plane dot(p,n) ≤ c (keepBelow) or ≥ c.</summary>
        public static void Clip(List<Vector2> input, Vector2 n, float c, bool keepBelow, List<Vector2> output)
        {
            output.Clear();
            int count = input.Count;
            if (count == 0) return;
            for (int i = 0; i < count; i++)
            {
                var a = input[i];
                var b = input[(i + 1) % count];
                float da = Vector2.Dot(a, n) - c;
                float db = Vector2.Dot(b, n) - c;
                if (!keepBelow) { da = -da; db = -db; }
                bool ina = da <= 0f, inb = db <= 0f;
                if (ina) output.Add(a);
                if (ina != inb)
                {
                    float t = da / (da - db);
                    output.Add(Vector2.Lerp(a, b, t));
                }
            }
        }

        public static float Area(List<Vector2> p)
        {
            float a = 0f;
            for (int i = 0, j = p.Count - 1; i < p.Count; j = i, i++) a += (p[j].x * p[i].y) - (p[i].x * p[j].y);
            return Mathf.Abs(a) * 0.5f;
        }
    }
}
