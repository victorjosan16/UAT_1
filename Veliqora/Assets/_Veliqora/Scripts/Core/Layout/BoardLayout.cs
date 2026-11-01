using System;

namespace Veliqora.Core.Layout
{
    /// <summary>Result of the adaptive board layout: how many rows, bottles per row and the bottle width.</summary>
    public struct BoardLayoutResult
    {
        public int Rows;
        public int PerRow;
        public float BottleWidth;
        public float BottleHeight;
        /// <summary>Horizontal gap between bottles and vertical distance between row baselines (reference pixels).</summary>
        public float GapX;
        public float RowHeight;
    }

    /// <summary>
    /// Adaptive multi-row layout for 1–20 bottles (pure math, testable without Unity): 1 row up to 5 bottles, 2 rows
    /// for 6–10, 3 rows for 11–15 and a compact 4-row grid for 16–20, always centered and balanced, with room under
    /// every bottle for its obstacle tag so nothing overlaps. Falls back to a better-fitting row count on unusual screens.
    /// </summary>
    public static class BoardLayout
    {
        public const int MaxRows = 4;
        public const float GapRatio = 0.5f;      // horizontal gap = 0.5 × bottle width
        public const float RowRatio = 1.32f;     // row height = 1.32 × bottle height (room for tags + lift)
        public const float TagSpace = 48f;       // obstacle / condition tag below a bottle (reference pixels)
        public const float MinWidth = 48f;
        public const float MaxWidth = 190f;

        /// <summary>Design rule: 1–5 bottles → 1 row, 6–10 → 2 rows, 11–15 → 3 rows, 16–20 → 4-row compact grid.</summary>
        public static int PreferredRows(int count) => count <= 5 ? 1 : count <= 10 ? 2 : count <= 15 ? 3 : 4;

        /// <summary>Smallest comfortable bottle width; below it the layout may use another row count that fits better.</summary>
        public const float ComfortWidth = 70f;

        public static BoardLayoutResult Choose(int count, float areaWidth, float areaHeight, float aspect)
        {
            if (count <= 0) count = 1;
            int preferred = Math.Min(MaxRows, PreferredRows(count));
            var pick = Evaluate(count, preferred, areaWidth, areaHeight, aspect);
            // "according to the available space": another row count wins only if bottles get clearly bigger
            // (≥ 25 %), or if the preferred one would make them uncomfortably small.
            var preferredPick = pick;
            for (int rows = 1; rows <= MaxRows; rows++)
            {
                if (rows == preferred) continue;
                var alt = Evaluate(count, rows, areaWidth, areaHeight, aspect);
                bool clearlyBigger = alt.BottleWidth >= preferredPick.BottleWidth * 1.25f;
                bool rescue = preferredPick.BottleWidth < ComfortWidth;
                if ((clearlyBigger || rescue) && alt.BottleWidth > pick.BottleWidth + 0.5f) pick = alt;
            }
            pick.BottleWidth = Math.Max(MinWidth, pick.BottleWidth);
            pick.BottleHeight = pick.BottleWidth * aspect;
            pick.GapX = pick.BottleWidth * GapRatio;
            pick.RowHeight = pick.BottleHeight * RowRatio + TagSpace;
            return pick;
        }

        private static BoardLayoutResult Evaluate(int count, int rows, float areaWidth, float areaHeight, float aspect)
        {
            rows = Math.Max(1, Math.Min(rows, count));
            int perRow = (count + rows - 1) / rows;
            float byWidth = areaWidth / (perRow * (1f + GapRatio) - GapRatio + 0.4f);
            float byHeight = (areaHeight / rows - TagSpace) / (aspect * RowRatio);
            float w = Math.Min(MaxWidth, Math.Min(byWidth, byHeight));
            return new BoardLayoutResult { Rows = rows, PerRow = perRow, BottleWidth = w };
        }

        /// <summary>Bottle centers (x) and bottoms (y), origin at the board center, rows centered and balanced.</summary>
        public static void Positions(int count, BoardLayoutResult l, float[] x, float[] yBottom)
        {
            float totalH = l.Rows * l.RowHeight;
            int idx = 0;
            for (int r = 0; r < l.Rows; r++)
            {
                int remainingRows = l.Rows - r;
                int inRow = Math.Min(l.PerRow, (count - idx + remainingRows - 1) / remainingRows);
                if (r == l.Rows - 1) inRow = count - idx;
                float rowW = inRow * l.BottleWidth + (inRow - 1) * l.GapX;
                float bottom = totalH * 0.5f - (r + 1) * l.RowHeight + l.TagSpaceOffset();
                for (int c = 0; c < inRow && idx < count; c++, idx++)
                {
                    x[idx] = -rowW * 0.5f + c * (l.BottleWidth + l.GapX) + l.BottleWidth * 0.5f;
                    yBottom[idx] = bottom;
                }
            }
        }

        private static float TagSpaceOffset(this BoardLayoutResult l) => TagSpace + (l.RowHeight - TagSpace - l.BottleHeight) * 0.35f;
    }
}
