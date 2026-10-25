using UnityEngine;

namespace SugarDrop
{
    /// <summary>Ce cere un nivel.</summary>
    public enum LevelType { Score, Collect, Ingredients, Frosting, Boss }

    /// <summary>Dificultatea unui nivel în „valul” capitolului său.</summary>
    public enum Difficulty { Easy, Normal, Hard, VeryHard, Boss, MegaBoss }

    /// <summary>
    /// Parametrii unui nivel, generați determinist din numărul lui (același nivel arată la fel la fiecare încercare).
    /// Dificultatea nu crește liniar: fiecare capitol de 10 niveluri e un „val” — câteva ușoare, câteva normale,
    /// unul-două grele, apoi Boss la final (Mega-Boss la fiecare 50). Ordinea variază ușor de la capitol la capitol.
    /// Tot echilibrul (mutări, obiective, obstacole) e aici și e verificat cu simulatorul din teste.
    /// </summary>
    public readonly struct LevelConfig
    {
        public const int ChapterSize = 10;
        public const int MegaBossEvery = 50;
        public const int BoardColumns = 9;
        public const int BoardRows = 9;

        private static readonly LevelType[] Cycle =
        {
            LevelType.Collect, LevelType.Ingredients, LevelType.Frosting, LevelType.Ingredients, LevelType.Collect,
            LevelType.Frosting, LevelType.Score, LevelType.Ingredients, LevelType.Frosting,
        };

        public readonly int Level;
        public readonly int Chapter;
        public readonly Difficulty Difficulty;
        public readonly int Columns;
        public readonly int Rows;
        public readonly int Colors;
        public readonly int Moves;
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
        public readonly int Crates;
        public readonly int CrateHits;
        public readonly int Ice;
        public readonly int IceLayers;
        public readonly int Chains;
        public readonly int Chocolate;

        public bool IsBoss => Difficulty == Difficulty.Boss || Difficulty == Difficulty.MegaBoss;
        public bool IsMegaBoss => Difficulty == Difficulty.MegaBoss;

        private LevelConfig(int level, Difficulty difficulty, int colors, int moves, LevelType type, int[] bottom, int[] top,
            int ingredients, int[] collectColors, int[] collectCounts, int frosting, int doubleFrosting, int scoreTarget,
            int crates, int crateHits, int ice, int iceLayers, int chains, int chocolate)
        {
            Level = level;
            Chapter = (level - 1) / ChapterSize;
            Difficulty = difficulty;
            Columns = BoardColumns;
            Rows = BoardRows;
            Colors = colors;
            Moves = moves;
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
            Crates = crates;
            CrateHits = crateHits;
            Ice = ice;
            IceLayers = iceLayers;
            Chains = chains;
            Chocolate = chocolate;
        }

        public static bool IsBossLevel(int level) => level > 0 && level % ChapterSize == 0;
        public static bool IsMegaBossLevel(int level) => level > 0 && level % MegaBossEvery == 0;

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
            if (IsBossLevel(level)) return LevelType.Boss;
            return Cycle[(level - 2) % Cycle.Length];
        }

        /// <summary>
        /// Dificultatea nivelului în valul capitolului: ușor, ușor, normal… greu, recuperare ușoară… foarte greu, Boss.
        /// Poziția nivelului greu și a câtorva niveluri normale variază de la un capitol la altul.
        /// </summary>
        public static Difficulty DifficultyFor(int level)
        {
            level = Mathf.Max(1, level);
            if (IsMegaBossLevel(level)) return Difficulty.MegaBoss;
            if (IsBossLevel(level)) return Difficulty.Boss;
            int chapter = (level - 1) / ChapterSize;
            int index = (level - 1) % ChapterSize;
            var random = new System.Random(chapter * 31337 + 7);
            int hard = 3 + random.Next(3); // 3, 4 sau 5
            bool hardEnding = chapter == 0 ? false : random.Next(4) != 0;
            bool secondEasyIsNormal = chapter >= 2 && random.Next(2) == 0;
            if (index == 0) return Difficulty.Easy;
            if (index == 1) return secondEasyIsNormal ? Difficulty.Normal : Difficulty.Easy;
            // În primul capitol (învățarea jocului) valul de la mijloc e doar normal; greul vine abia la nivelul 9.
            if (index == hard) return chapter == 0 ? Difficulty.Normal : Difficulty.Hard;
            if (index == hard + 1) return Difficulty.Easy; // recuperare după nivelul greu
            if (index == 8) return hardEnding ? Difficulty.VeryHard : Difficulty.Hard;
            return Difficulty.Normal;
        }

        public static LevelConfig For(int level)
        {
            level = Mathf.Max(1, level);
            var random = new System.Random(level * 104729 + 3);
            Difficulty difficulty = DifficultyFor(level);
            LevelType type = TypeFor(level);
            int chapter = (level - 1) / ChapterSize;

            // Cât de generos e nivelul (mutări), cât cere (obiective) și câte obstacole are, după dificultate.
            float movesFactor = difficulty switch
            {
                // Raportat la câte mutări îi trebuie în mod obișnuit jucătorului automat din teste (mediana):
                // ușor ≈ 1,5×, normal ≈ 1,2×, greu ≈ 1,05×, foarte greu ≈ 0,95×.
                Difficulty.Easy => 1.5f,
                Difficulty.Normal => 1.2f,
                Difficulty.Hard => 1.05f,
                Difficulty.VeryHard => 0.95f,
                _ => 1.15f,
            };
            float goalFactor = difficulty switch
            {
                Difficulty.Easy => 0.85f,
                Difficulty.Hard => 1.1f,
                Difficulty.VeryHard => 1.2f,
                _ => 1f,
            };
            float obstacleFactor = difficulty switch
            {
                Difficulty.Easy => 0.5f,
                Difficulty.Hard => 1.4f,
                Difficulty.VeryHard => 1.7f,
                // Boss-ul își aduce singur obstacolele (atacurile), deci tabla de start e mai liberă.
                Difficulty.Boss or Difficulty.MegaBoss => 0.7f,
                _ => 1f,
            };

            int colors = level <= 5 || (difficulty == Difficulty.Easy && level < 30) ? 4 : 5;

            Shape(level, difficulty, random, out int[] bottom, out int[] top);
            int valid = 0;
            for (int x = 0; x < BoardColumns; x++) valid += top[x] - bottom[x] + 1;

            int ingredients = 0, frosting = 0, doubleFrosting = 0, scoreTarget = 0;
            int[] collectColors = System.Array.Empty<int>(), collectCounts = System.Array.Empty<int>();
            float moves;
            switch (type)
            {
                // Mutările de bază = cât îi trebuie de obicei jucătorului automat (măsurat), apoi × dificultatea.
                case LevelType.Ingredients:
                    ingredients = level <= 4 ? 1 : level <= 30 ? 2 : 3;
                    moves = 4 + ingredients * 10;
                    break;
                case LevelType.Frosting:
                    frosting = Mathf.Min(valid / 3, Mathf.RoundToInt((6 + level / 5f) * goalFactor));
                    doubleFrosting = level >= 25 ? frosting / 5 : 0;
                    moves = 4 + (frosting + doubleFrosting) * 2.2f;
                    break;
                case LevelType.Score:
                    moves = 16;
                    break;
                case LevelType.Boss:
                    // Lupta: viața Boss-ului se calculează din mutări (BossDefinition), deci mutările decid durata.
                    moves = difficulty == Difficulty.MegaBoss ? 28 : 22;
                    break;
                default:
                    moves = 0;
                    break;
            }
            if (type == LevelType.Collect)
            {
                int kinds = 1 + (level >= 8 ? 1 : 0) + (level >= 20 ? 1 : 0);
                kinds = Mathf.Min(kinds, colors - 1);
                collectColors = new int[kinds];
                collectCounts = new int[kinds];
                int first = random.Next(colors);
                int total = 0;
                for (int i = 0; i < kinds; i++)
                {
                    collectColors[i] = (first + i * 2) % colors;
                    int count = Mathf.RoundToInt(Mathf.Min(30, 15 + level / 4f + random.Next(4)) * goalFactor);
                    collectCounts[i] = count;
                    total += collectCounts[i];
                }
                moves = 2 + total / 2.3f;
            }
            int finalMoves = Mathf.Clamp(Mathf.RoundToInt(moves * movesFactor), 12, 65);
            // Scorul țintă: cam cât adună jucătorul automat în 16 mutări, ajustat după dificultate.
            if (type == LevelType.Score) scoreTarget = Mathf.RoundToInt((16 * 150 + level * 30) * goalFactor);

            // Obstacolele apar treptat (ladă de la 4, gheață de la 7, lanțuri de la 12, ciocolată de la 25) și
            // urmează valul de dificultate. Pe nivelurile cu bomboane sunt doar lăzi (gheața și lanțurile ar ține
            // bomboanele pe loc), iar pe cele cu glazură obstacolele sunt mai puține.
            bool ingredientLevel = type == LevelType.Ingredients;
            float blockerShare = type == LevelType.Frosting || ingredientLevel ? 0.5f : 1f;
            int Scaled(float baseCount) => Mathf.RoundToInt(baseCount * obstacleFactor * blockerShare);
            int crates = level < 4 ? 0 : Mathf.Min(8, Scaled(2 + chapter * 0.6f));
            int crateHits = level >= 15 && difficulty != Difficulty.Easy ? 2 : 1;
            int ice = level < 7 || ingredientLevel ? 0 : Mathf.Min(8, Scaled(2 + chapter * 0.6f));
            int iceLayers = level < 15 ? 1 : level < 30 ? 2 : 3;
            int chains = level < 12 || ingredientLevel ? 0 : Mathf.Min(6, Scaled(1 + chapter * 0.4f));
            int chocolate = level < 25 || blockerShare < 1f ? 0 : Mathf.Min(3, Scaled(1 + chapter * 0.2f));
            // Cel mult un sfert din tablă acoperit de obstacole (formele mici nu se umplu).
            int obstacleRoom = valid / 4;
            while (crates + ice + chains + chocolate > obstacleRoom)
            {
                if (ice >= crates && ice >= chains && ice > 0) ice--;
                else if (crates >= chains && crates > 0) crates--;
                else if (chains > 0) chains--;
                else chocolate--;
            }

            return new LevelConfig(level, difficulty, colors, finalMoves, type, bottom, top, ingredients, collectColors, collectCounts,
                frosting, doubleFrosting, scoreTarget, crates, crateHits, ice, iceLayers, chains, chocolate);
        }

        /// <summary>
        /// Forma tablei: primele niveluri și cele ușoare sunt pătrate; apoi colțuri tăiate, arc, pâlnie, romb sau trepte.
        /// Coloanele rămân continue (fără goluri în interior), ca piesele să poată cădea pe fiecare coloană.
        /// </summary>
        private static void Shape(int level, Difficulty difficulty, System.Random random, out int[] bottom, out int[] top)
        {
            bottom = new int[BoardColumns];
            top = new int[BoardColumns];
            for (int x = 0; x < BoardColumns; x++) top[x] = BoardRows - 1;
            if (level < 4 || difficulty == Difficulty.Easy || random.Next(10) < 3) return;

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
                case 2: // pâlnie
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
