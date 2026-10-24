using System;
using System.Collections.Generic;
using UnityEngine;

namespace SugarDrop
{
    /// <summary>Un șir de 3+ capace de aceeași culoare, pe un rând sau o coloană.</summary>
    public struct MatchRun
    {
        public List<Vector2Int> Cells;
        public bool Horizontal;
    }

    /// <summary>Șirurile care se ating (L, T, +) formează un singur grup.</summary>
    public class MatchGroup
    {
        public readonly HashSet<Vector2Int> Cells = new();
        public readonly List<MatchRun> Runs = new();
        public int Color;
    }

    /// <summary>
    /// Regulile pure ale alinierilor (fără obiecte Unity), ca să poată fi testate separat.
    /// „colorAt” întoarce culoarea unei celule sau -1 dacă acolo nu e un capac care se poate alinia
    /// (gaură, biscuit, curcubeu).
    /// </summary>
    public static class BoardRules
    {
        public static List<MatchGroup> FindGroups(int columns, int rows, Func<Vector2Int, int> colorAt)
        {
            var runs = new List<MatchRun>();
            for (int y = 0; y < rows; y++) ScanLine(new Vector2Int(0, y), Vector2Int.right, columns, true, colorAt, runs);
            for (int x = 0; x < columns; x++) ScanLine(new Vector2Int(x, 0), Vector2Int.up, rows, false, colorAt, runs);

            var groups = new List<MatchGroup>();
            foreach (MatchRun run in runs)
            {
                int color = colorAt(run.Cells[0]);
                MatchGroup target = null;
                foreach (MatchGroup group in groups)
                {
                    if (group.Color != color) continue;
                    foreach (Vector2Int cell in run.Cells)
                        if (group.Cells.Contains(cell)) { target = group; break; }
                    if (target != null) break;
                }
                if (target == null)
                {
                    target = new MatchGroup { Color = color };
                    groups.Add(target);
                }
                target.Runs.Add(run);
                foreach (Vector2Int cell in run.Cells) target.Cells.Add(cell);
            }
            return groups;
        }

        private static void ScanLine(Vector2Int start, Vector2Int step, int length, bool horizontal,
            Func<Vector2Int, int> colorAt, List<MatchRun> runs)
        {
            int i = 0;
            while (i < length)
            {
                int color = colorAt(start + step * i);
                if (color < 0)
                {
                    i++;
                    continue;
                }
                int run = 1;
                while (i + run < length && colorAt(start + step * (i + run)) == color) run++;
                if (run >= 3)
                {
                    var cells = new List<Vector2Int>(run);
                    for (int k = 0; k < run; k++) cells.Add(start + step * (i + k));
                    runs.Add(new MatchRun { Cells = cells, Horizontal = horizontal });
                }
                i += run;
            }
        }

        /// <summary>Ce capac special iese dintr-un grup: 5 în linie → curcubeu, L/T → ambalat, 4 → dungat.</summary>
        public static TileKind SpecialFor(MatchGroup group)
        {
            int longest = 0;
            bool horizontal = false, vertical = false;
            foreach (MatchRun run in group.Runs)
            {
                if (run.Cells.Count > longest) longest = run.Cells.Count;
                if (run.Horizontal) horizontal = true;
                else vertical = true;
            }
            if (longest >= 5) return TileKind.Rainbow;
            if (horizontal && vertical) return TileKind.Wrapped;
            if (longest == 4) return horizontal ? TileKind.StripedRow : TileKind.StripedColumn;
            return TileKind.Normal;
        }

        /// <summary>Celula face parte dintr-o aliniere de 3+ (pe rând sau pe coloană)?</summary>
        public static bool IsMatchAt(Vector2Int cell, Func<Vector2Int, int> colorAt)
        {
            int color = colorAt(cell);
            if (color < 0) return false;
            return Count(cell, Vector2Int.left, color, colorAt) + Count(cell, Vector2Int.right, color, colorAt) >= 2
                   || Count(cell, Vector2Int.down, color, colorAt) + Count(cell, Vector2Int.up, color, colorAt) >= 2;
        }

        private static int Count(Vector2Int from, Vector2Int direction, int color, Func<Vector2Int, int> colorAt)
        {
            int count = 0;
            Vector2Int p = from + direction;
            while (colorAt(p) == color)
            {
                count++;
                p += direction;
            }
            return count;
        }
    }
}
