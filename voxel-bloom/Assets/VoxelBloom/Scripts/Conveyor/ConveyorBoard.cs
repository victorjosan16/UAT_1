using UnityEngine;
using VoxelBloom.Core;

namespace VoxelBloom
{
    /// <summary>
    /// Geometry of the closed rounded-rectangle loop (visual only; gameplay uses integer path units).
    /// Board units: the loop centreline spans x in [0, W], y in [0, H] (y down), corner radius R; one unit = one cube pitch.
    /// Path start (s = 0) is the beginning of the top straight, direction clockwise (right along the top).
    /// </summary>
    public sealed class ConveyorBoard
    {
        public readonly float W, H, R, Lg;
        readonly float a, b, q;
        readonly int pathUnits;

        public ConveyorBoard(ConveyorLevel level)
        {
            W = level.BoardW; H = level.BoardH; R = level.BoardR;
            a = W - 2 * R; b = H - 2 * R; q = Mathf.PI * R / 2f;
            Lg = 2 * a + 2 * b + 4 * q;
            pathUnits = level.Slots * ConveyorSession.P;
        }

        /// <summary>Point on the centreline for a path position in path units (may be fractional for smooth motion).</summary>
        public Vector2 At(float s)
        {
            float g = Mathf.Repeat(s, pathUnits) * Lg / pathUnits;
            if (g < a) return new Vector2(R + g, 0);
            g -= a;
            if (g < q) { float f = g / R; return new Vector2(W - R + R * Mathf.Sin(f), R - R * Mathf.Cos(f)); }
            g -= q;
            if (g < b) return new Vector2(W, R + g);
            g -= b;
            if (g < q) { float f = g / R; return new Vector2(W - R + R * Mathf.Cos(f), H - R + R * Mathf.Sin(f)); }
            g -= q;
            if (g < a) return new Vector2(W - R - g, H);
            g -= a;
            if (g < q) { float f = g / R; return new Vector2(R - R * Mathf.Sin(f), H - R + R * Mathf.Cos(f)); }
            g -= q;
            if (g < b) return new Vector2(0, H - R - g);
            g -= b;
            { float f = Mathf.Min(g, q) / R; return new Vector2(R - R * Mathf.Cos(f), R - R * Mathf.Sin(f)); }
        }

        /// <summary>Travel direction (unit vector, y down) at a path position.</summary>
        public Vector2 Tangent(float s)
        {
            var d = At(s + 8f) - At(s - 8f);
            return d.sqrMagnitude > 1e-6f ? d.normalized : Vector2.right;
        }

        /// <summary>Centre of compartment <paramref name="i"/> of a station's tray (0 = next to the belt).</summary>
        public Vector2 Compartment(ConveyorStationDef st, int i)
        {
            const float first = 1.6f, pitch = Pitch;
            switch (st.Side)
            {
                case "top": return new Vector2(st.At, first + i * pitch);
                case "right": return new Vector2(W - first - i * pitch, st.At);
                case "bottom": return new Vector2(st.At, H - first - i * pitch);
                default: return new Vector2(first + i * pitch, st.At);
            }
        }

        /// <summary>Tray geometry in board units (mirrors TRAY in tools/conveyor/build-levels.mjs).</summary>
        public const float Pitch = 1.7f, Cell = 1.5f, TrayWidth = 1.84f;

        public static bool IsVertical(ConveyorStationDef st) => st.Side == "top" || st.Side == "bottom";
    }
}
