namespace SugarDrop
{
    /// <summary>
    /// Pe unde pot curge bomboanele: ele intră în tablă doar prin celulele goale din rândul de sus și trec
    /// doar prin celule goale vecine (sus/jos/stânga/dreapta). Dacă drumul ajunge în rândul de jos,
    /// bomboanele ajung în pâlnie și în borcan. Pur (fără Unity), ca să poată fi testat.
    /// </summary>
    public static class ChannelRules
    {
        /// <summary>
        /// Marchează în „reached” celulele goale legate de sus. empty[x, y]: y = 0 e rândul de jos.
        /// Întoarce câte celule sunt atinse; „opensBottom” = drumul ajunge jos.
        /// </summary>
        public static int Reach(bool[,] empty, bool[,] reached, out bool opensBottom)
        {
            int columns = empty.GetLength(0), rows = empty.GetLength(1);
            System.Array.Clear(reached, 0, reached.Length);
            var stack = new System.Collections.Generic.Stack<(int, int)>();
            for (int x = 0; x < columns; x++)
                if (empty[x, rows - 1])
                {
                    reached[x, rows - 1] = true;
                    stack.Push((x, rows - 1));
                }

            int count = 0;
            opensBottom = false;
            while (stack.Count > 0)
            {
                (int x, int y) = stack.Pop();
                count++;
                if (y == 0) opensBottom = true;
                Visit(x + 1, y);
                Visit(x - 1, y);
                Visit(x, y + 1);
                Visit(x, y - 1);
            }
            return count;

            void Visit(int x, int y)
            {
                if (x < 0 || y < 0 || x >= columns || y >= rows || reached[x, y] || !empty[x, y]) return;
                reached[x, y] = true;
                stack.Push((x, y));
            }
        }

        /// <summary>
        /// Pentru o bomboană aflată în celula (column, row) care stă pe capacul de dedesubt: coloana celei mai
        /// apropiate găuri de pe aceeași podea (rândul row - 1) la care poate aluneca fără capace în cale.
        /// row ≥ numărul de rânduri = deasupra tablei (acolo e mereu liber). -1 = nicio gaură accesibilă
        /// (sau bomboana e deja deasupra unei găuri / sub tablă).
        /// </summary>
        public static int FindDropColumn(bool[,] empty, int column, int row)
        {
            int columns = empty.GetLength(0), rows = empty.GetLength(1);
            if (row <= 0 || column < 0 || column >= columns) return -1;
            if (row > rows) row = rows;
            int floor = row - 1;
            if (empty[column, floor]) return -1;
            for (int distance = 1; distance < columns; distance++)
            for (int side = -1; side <= 1; side += 2)
            {
                int x = column + side * distance;
                if (x < 0 || x >= columns || !empty[x, floor] || !RowFree(empty, column, x, row)) continue;
                return x;
            }
            return -1;
        }

        private static bool RowFree(bool[,] empty, int from, int to, int row)
        {
            if (row >= empty.GetLength(1)) return true;
            int step = to > from ? 1 : -1;
            for (int x = from + step; x != to + step; x += step)
                if (!empty[x, row]) return false;
            return true;
        }

        /// <summary>Cât de bună e o tablă pentru bomboane: un drum până jos valorează cel mai mult, apoi cât de adânc intră.</summary>
        public static int Score(bool[,] empty)
        {
            var reached = new bool[empty.GetLength(0), empty.GetLength(1)];
            int count = Reach(empty, reached, out bool bottom);
            int deepest = 0;
            for (int x = 0; x < empty.GetLength(0); x++)
                for (int y = 0; y < empty.GetLength(1); y++)
                    if (reached[x, y]) deepest = System.Math.Max(deepest, empty.GetLength(1) - y);
            return (bottom ? 1000 : 0) + deepest * 20 + count * 5;
        }
    }
}
