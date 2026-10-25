using UnityEngine;

namespace SugarDrop
{
    /// <summary>Ce cere un nivel.</summary>
    public enum LevelType { Score, Collect, Ingredients, Frosting }

    /// <summary>
    /// Parametrii unui nivel, generați determinist din numărul lui (același nivel arată la fel la fiecare încercare):
    /// tipul (scor, strânge culori, bomboane în borcan, glazură), forma tablei, mutările, obiectivele și obstacolele.
    /// Fiecare al 10-lea nivel e „Boss”: două obiective, mai multe mutări, recompensă dublă.
    /// </summary>
    public readonly struct LevelConfig
    {
        public const int BossEvery = 10;
        public const int BoardColumns = 9;
        public const int BoardRows = 9;

        private static readonly LevelType[] Cycle =
        {
            LevelType.Collect, LevelType.Ingredients, LevelType.Frosting, LevelType.Ingredients, LevelType.Collect,
            LevelType.Frosting, LevelType.Score, LevelType.Ingredients, LevelType.Frosting,
        };

        public readonly int Level;
        public readonly int Columns;
        public readonly int Rows;
        public readonly int Colors;
        public readonly int Moves;
        public readonly bool IsBoss;
        public readonly LevelType Type;
        /// <summary>Sămânța pentru așezarea obstacolelor și a glazurii (aceeași la fiecare încercare).</summary>
        public readonly int Seed;

        /// <summary>Forma tablei: pe fiecare coloană, celulele valide sunt de la ColumnBottom la ColumnTop (inclusiv).</summary>
        public readonly int[] ColumnBottom;
        public readonly int[] ColumnTop;

        // Obiective (0 = nefolosit).
        public readonly int Ingredients;
        public readonly int[] CollectColors;
        public readonly int[] CollectCounts;
        public readonly int FrostingCells;
        public readonly int DoubleFrosting;
        public readonly int ScoreTarget;

        // Obstacole.
        public readonly int Biscuits;
        public readonly int BiscuitHits;
        public readonly int Frozen;
        public readonly int Chocolate;

        private LevelConfig(int level, int colors, int moves, bool boss, LevelType type, int[] bottom, int[] top,
            int ingredients, int[] collectColors, int[] collectCounts, int frosting, int doubleFrosting, int scoreTarget,
            int biscuits, int biscuitHits, int frozen, int chocolate)
        {
            Level = level;
            Columns = BoardColumns;
            Rows = BoardRows;
            Colors = colors;
            Moves = moves;
            IsBoss = boss;
            Type = type;
            Seed = level * 7919 + 17;
            ColumnBottom = bottom;
            ColumnTop = top;
            Ingredients = ingredients;
            CollectColors = collectColors;
            CollectCounts = collectCounts;
            FrostingCells = frosting;
            DoubleFrosting = doubleFrosting;
            ScoreTarget = scoreTarget;
            Biscuits = biscuits;
            BiscuitHits = biscuitHits;
            Frozen = frozen;
            Chocolate = chocolate;
        }

        public static bool IsBossLevel(int level) => level > 0 && level % BossEvery == 0;

        public bool IsValidCell(int x, int y) => x >= 0 && x < Columns && y >= ColumnBottom[x] && y <= ColumnTop[x];

        public int ValidCells
        {
            get
            {
                int count = 0;
                for (int x = 0; x < Columns; x++) count += ColumnTop[x] - ColumnBottom[x] + 1;
                return count;
            }
        }

        public static LevelType TypeFor(int level)
        {
            level = Mathf.Max(1, level);
            if (level == 1) return LevelType.Score;
            if (IsBossLevel(level)) return LevelType.Ingredients;
            return Cycle[(level - 2) % Cycle.Length];
        }

        public static LevelConfig For(int level)
        {
            level = Mathf.Max(1, level);
            var random = new System.Random(level * 104729 + 3);
            bool boss = IsBossLevel(level);
            LevelType type = TypeFor(level);

            int colors = level < 6 ? 4 : 5;

            Shape(level, random, out int[] bottom, out int[] top);
            int valid = 0;
            for (int x = 0; x < BoardColumns; x++) valid += top[x] - bottom[x] + 1;

            // Obiectivele, apoi mutările pe măsura lor. Valorile sunt verificate cu simulatorul din teste:
            // un jucător automat care alege mereu mutarea sugerată de indiciu trebuie să câștige des.
            int ingredients = 0, frosting = 0, doubleFrosting = 0, scoreTarget = 0;
            int[] collectColors = System.Array.Empty<int>(), collectCounts = System.Array.Empty<int>();
            int moves;
            int harder = level / 40; // mutările scad foarte încet cu nivelul
            switch (type)
            {
                case LevelType.Ingredients:
                    ingredients = Mathf.Min(3, 2 + level / 25);
                    moves = 22 + ingredients * 4 - harder;
                    break;
                case LevelType.Frosting:
                    frosting = Mathf.Min(valid / 2, 8 + level / 4);
                    doubleFrosting = level >= 20 ? frosting / 5 : 0;
                    moves = 20 + (frosting + doubleFrosting) / 3 - harder;
                    break;
                case LevelType.Score:
                    moves = 20 - harder;
                    break;
                default:
                    moves = 0;
                    break;
            }
            if (type == LevelType.Collect || boss)
            {
                int kinds = boss ? 2 : 1 + (level >= 8 ? 1 : 0) + (level >= 20 ? 1 : 0);
                kinds = Mathf.Min(kinds, colors - 1);
                collectColors = new int[kinds];
                collectCounts = new int[kinds];
                int first = random.Next(colors);
                int total = 0;
                for (int i = 0; i < kinds; i++)
                {
                    collectColors[i] = (first + i * 2) % colors;
                    int count = Mathf.Min(30, 12 + level / 4 + random.Next(4));
                    collectCounts[i] = boss ? count * 7 / 10 : count;
                    total += collectCounts[i];
                }
                if (type == LevelType.Collect) moves = 10 + total / 6 - harder;
            }
            if (boss)
            {
                ingredients += 1;
                moves += 8;
            }
            moves = Mathf.Max(16, moves);
            if (type == LevelType.Score) scoreTarget = moves * 130 + level * 40;

            // Obstacole (cresc treptat). Pe nivelurile cu bomboane și glazură sunt mai puține: ar bloca obiectivul.
            // Valorile sunt verificate cu simulatorul din teste (un jucător automat trebuie să poată câștiga des).
            int biscuits = level < 5 ? 0 : Mathf.Min(6, 1 + (level - 5) / 5);
            int biscuitHits = level >= 15 ? 2 : 1;
            int frozen = level < 7 ? 0 : Mathf.Min(6, 1 + (level - 7) / 5);
            int chocolate = level < 25 ? 0 : Mathf.Min(3, 1 + (level - 25) / 15);
            if (type == LevelType.Ingredients || type == LevelType.Frosting)
            {
                biscuits /= 2;
                frozen /= 2;
                chocolate = 0;
            }

            return new LevelConfig(level, colors, moves, boss, type, bottom, top, ingredients, collectColors, collectCounts,
                frosting, doubleFrosting, scoreTarget, biscuits, biscuitHits, frozen, chocolate);
        }

        /// <summary>
        /// Forma tablei: primele niveluri sunt pătrate; apoi colțuri tăiate, arc, pâlnie, romb sau trepte.
        /// Coloanele rămân continue (fără goluri în interior), ca piesele să poată cădea pe fiecare coloană.
        /// </summary>
        private static void Shape(int level, System.Random random, out int[] bottom, out int[] top)
        {
            bottom = new int[BoardColumns];
            top = new int[BoardColumns];
            for (int x = 0; x < BoardColumns; x++) top[x] = BoardRows - 1;
            if (level < 4 || random.Next(10) < 4) return;

            int center = BoardColumns / 2;
            switch (random.Next(5))
            {
                case 0: // colțuri tăiate
                    bottom[0] = bottom[BoardColumns - 1] = 1;
                    top[0] = top[BoardColumns - 1] = BoardRows - 2;
                    break;
                case 1: // arc: mijlocul e mai înalt
                    for (int x = 0; x < BoardColumns; x++) top[x] = BoardRows - 1 - Mathf.Min(2, Mathf.Abs(x - center) / 2);
                    break;
                case 2: // pâlnie: marginile coboară mai puțin
                    for (int x = 0; x < BoardColumns; x++) bottom[x] = Mathf.Min(2, Mathf.Abs(x - center) / 2);
                    break;
                case 3: // romb
                    for (int x = 0; x < BoardColumns; x++)
                    {
                        int cut = Mathf.Min(2, Mathf.Abs(x - center) / 2);
                        bottom[x] = cut;
                        top[x] = BoardRows - 1 - cut;
                    }
                    break;
                default: // trepte
                    for (int x = 0; x < BoardColumns; x++) top[x] = BoardRows - 1 - (x < 3 ? 1 : 0);
                    for (int x = 0; x < BoardColumns; x++) bottom[x] = x > 5 ? 1 : 0;
                    break;
            }
        }
    }
}
