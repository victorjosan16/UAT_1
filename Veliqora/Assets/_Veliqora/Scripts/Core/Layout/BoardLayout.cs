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
    /// Adaptive multi-row layout for 1–20 bottles (pure math, testable without Unity). Tries 1–4 rows and keeps the
    /// arrangement with the largest bottles that fits the board area, leaving room under every bottle for its
    /// obstacle tag, so nothing overlaps.
    /// </summary>
    public static class BoardLayout
    {
        public const int MaxRows = 4;
        public const float GapRatio = 0.5f;      // horizontal gap = 0.5 × bottle width
        public const float RowRatio = 1.32f;     // row height = 1.32 × bottle height (room for tags + lift)
        public const float TagSpace = 48f;       // obstacle / condition tag below a bottle (reference pixels)
        public const float MinWidth = 48f;
        public const float MaxWidth = 190f;

        public static BoardLayoutResult Choose(int count, float areaWidth, float areaHeight, float aspect)
        {
            if (count <= 0) count = 1;
            var best = new BoardLayoutResult { Rows = 1, PerRow = count, BottleWidth = 0f };
            for (int rows = 1; rows <= MaxRows; rows++)
            {
                int perRow = (count + rows - 1) / rows;
                if (rows > 1 && (rows - 1) * perRow >= count) continue; // an empty row
                float byWidth = areaWidth / (perRow * (1f + GapRatio) - GapRatio + 0.4f);
                // each row needs the bottle, the tag space under it and the lift room
                float byHeight = (areaHeight / rows - TagSpace) / (aspect * RowRatio);
                float w = Math.Min(MaxWidth, Math.Min(byWidth, byHeight));
                if (w > best.BottleWidth + 0.5f)
                    best = new BoardLayoutResult { Rows = rows, PerRow = perRow, BottleWidth = w };
            }
            best.BottleWidth = Math.Max(MinWidth, best.BottleWidth);
            best.BottleHeight = best.BottleWidth * aspect;
            best.GapX = best.BottleWidth * GapRatio;
            best.RowHeight = best.BottleHeight * RowRatio + TagSpace;
            return best;
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
