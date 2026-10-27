using UnityEngine;

namespace SugarDrop
{
    /// <summary>Ce cere un nivel.</summary>
    public enum LevelType { Score, Collect, Ingredients, Frosting, Boss, Crates, Ice, Chains, Chocolate, Specials, Garden, Defuse, Bonus, Descent, Frozen, Blaze }

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

        /// <summary>
        /// Ordinea tipurilor de niveluri (se repetă). Tipurile cu obstacole apar abia după ce obstacolul lor a fost
        /// introdus (vezi <see cref="Unlocked"/>); până atunci locul lor îl ia un tip clasic.
        /// </summary>
        private static readonly LevelType[] Cycle =
        {
            LevelType.Collect, LevelType.Ingredients, LevelType.Crates, LevelType.Frosting, LevelType.Specials,
            LevelType.Ice, LevelType.Score, LevelType.Ingredients, LevelType.Chains, LevelType.Frosting,
            LevelType.Chocolate, LevelType.Collect, LevelType.Specials, LevelType.Crates, LevelType.Garden, LevelType.Defuse,
        };

        /// <summary>Nivelul bonus „Sugar Rush” (pe timp, fără pierdere): al 5-lea din fiecare capitol, de la nivelul 15.</summary>
        public static bool IsBonusLevel(int level) => level >= 15 && level % ChapterSize == 5;

        /// <summary>De la ce nivel apar portalurile (Lacul de Afine).</summary>
        public const int PortalsFrom = 21;

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
        /// <summary>Câte piese speciale trebuie activate (nivelurile „Piese speciale”).</summary>
        public readonly int SpecialsTarget;
        /// <summary>Ghivece cu flori pe tablă (Pădurea de Mentă).</summary>
        public readonly int Pots;
        /// <summary>Bombe cu ceas: câte sunt pe tablă deodată, de la ce număr pornesc și câte trebuie dezamorsate.</summary>
        public readonly int TimeBombs;
        public readonly int TimeBombStart;
        public readonly int TimeBombGoal;
        /// <summary>Portaluri (Lacul de Afine): piesa de jos din coloana PortalIn[i] iese sus în coloana PortalOut[i].</summary>
        public readonly int[] PortalIn;
        public readonly int[] PortalOut;
        /// <summary>Secundele nivelului bonus (0 = nivel obișnuit, cu mutări).</summary>
        public readonly int BonusSeconds;
        public bool IsBonus => Type == LevelType.Bonus;
        /// <summary>Straturile de gheață din jurul lui Bobo (nivelurile „Bobo înghețat”).</summary>
        public readonly int BoboIce;
        /// <summary>Focul de la start și la câte mutări se întinde (nivelurile „Focul și apa”).</summary>
        public readonly int Fires;
        public readonly int FireEvery;
        /// <summary>Bobo e o piesă pe tablă care trebuie dusă jos, la ieșire.</summary>
        public bool HasBobo => Type is LevelType.Descent or LevelType.Frozen;

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
            int ingredients, int[] collectColors, int[] collectCounts, int frosting, int doubleFrosting, int scoreTarget, int specialsTarget,
            int crates, int crateHits, int ice, int iceLayers, int chains, int chocolate,
            int pots, int timeBombs, int timeBombStart, int timeBombGoal, int[] portalIn, int[] portalOut, int bonusSeconds,
            int boboIce, int fires, int fireEvery)
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
            SpecialsTarget = specialsTarget;
            Crates = crates;
            CrateHits = crateHits;
            Ice = ice;
            IceLayers = iceLayers;
            Chains = chains;
            Chocolate = chocolate;
            Pots = pots;
            TimeBombs = timeBombs;
            TimeBombStart = timeBombStart;
            TimeBombGoal = timeBombGoal;
            PortalIn = portalIn;
            PortalOut = portalOut;
            BonusSeconds = bonusSeconds;
            BoboIce = boboIce;
            Fires = fires;
            FireEvery = fireEvery;
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
            if (IsBonusLevel(level)) return LevelType.Bonus;
            if (BoboTypeFor(level) is LevelType bobo) return bobo;
            // Mecanica ținutului e „vedeta” lui: în Pădurea de Mentă (11–20) grădinile, în Deșertul de Lămâie (31–40)
            // bombele cu ceas apar la fiecare al treilea nivel; după aceea intră în rotația obișnuită.
            int chapterIndex = (level - 1) % ChapterSize;
            int chapterOfLevel = (level - 1) / ChapterSize;
            if (chapterOfLevel == 1 && chapterIndex % 3 == 1) return LevelType.Garden;
            if (chapterOfLevel == 3 && chapterIndex % 3 == 1) return LevelType.Defuse;
            LevelType type = Cycle[(level - 2) % Cycle.Length];
            return Unlocked(type, level) ? type : type switch
            {
                LevelType.Chocolate => LevelType.Ingredients,
                LevelType.Ice => LevelType.Frosting,
                LevelType.Specials => LevelType.Score,
                LevelType.Defuse => LevelType.Frosting,
                _ => LevelType.Collect,
            };
        }

        /// <summary>
        /// Aventurile lui Bobo: în fiecare ținut, 4 niveluri cu Bobo pe tablă (pozițiile 3, 6, 8 și 9 din capitol).
        /// Câmpia de Căpșuni învață coborârea, Pădurea de Mentă aduce focul, Lacul de Afine gheața; apoi se amestecă.
        /// </summary>
        public static LevelType? BoboTypeFor(int level)
        {
            int index = (Mathf.Max(1, level) - 1) % ChapterSize;
            int chapter = (Mathf.Max(1, level) - 1) / ChapterSize;
            if (index is not (2 or 5 or 7 or 8)) return null;
            bool first = index is 2 or 7;
            LevelType[] rotation = { LevelType.Descent, LevelType.Blaze, LevelType.Frozen };
            (LevelType a, LevelType b) = chapter switch
            {
                0 => (LevelType.Descent, LevelType.Descent),
                1 => (LevelType.Blaze, LevelType.Descent),
                2 => (LevelType.Frozen, LevelType.Blaze),
                3 => (LevelType.Blaze, LevelType.Frozen),
                _ => (rotation[chapter % 3], rotation[(chapter + 1) % 3]),
            };
            return first ? a : b;
        }

        /// <summary>Un tip de nivel cu obstacole apare abia de la nivelul în care e introdus obstacolul lui.</summary>
        private static bool Unlocked(LevelType type, int level) => type switch
        {
            LevelType.Crates => level >= 4,
            LevelType.Specials => level >= 5,
            LevelType.Ice => level >= 7,
            LevelType.Chains => level >= 12,
            LevelType.Chocolate => level >= 25,
            LevelType.Garden => level >= 11,
            LevelType.Defuse => level >= 31,
            _ => true,
        };

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
            // Primele două capitole se termină cu un nivel greu, nu foarte greu (jucătorul abia învață mecanicile).
            bool hardEnding = chapter <= 1 ? false : random.Next(4) != 0;
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
                // ușor ≈ 1,5×, normal ≈ 1,2×, greu ≈ 0,98×, foarte greu ≈ 0,95×.
                Difficulty.Easy => 1.5f,
                Difficulty.Normal => 1.2f,
                Difficulty.Hard => 0.98f,
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

            // Focul: cu 4 culori, apa (albastrul) apare mai des și se fac mai multe linii.
            int colors = level <= 3 || type == LevelType.Blaze ? 4 : 5;

            Shape(level, difficulty, random, out int[] bottom, out int[] top);
            int valid = 0;
            for (int x = 0; x < BoardColumns; x++) valid += top[x] - bottom[x] + 1;

            int ingredients = 0, frosting = 0, doubleFrosting = 0, scoreTarget = 0, specialsTarget = 0;
            int pots = 0, timeBombs = 0, timeBombStart = 0, timeBombGoal = 0, bonusSeconds = 0;
            int boboIce = 0, fires = 0, fireEvery = 0;
            int crateHits = level >= 15 && difficulty != Difficulty.Easy ? 2 : 1;
            int iceLayers = level < 15 ? 1 : level < 30 ? 2 : 3;
            // Câte obstacole are nivelul care le are ca obiectiv (lăzi, gheață, lanțuri, ciocolată).
            int Focus(float baseCount, int min, int max) => Mathf.Clamp(Mathf.RoundToInt(baseCount * goalFactor), min, max);
            int focus = 0;
            int[] collectColors = System.Array.Empty<int>(), collectCounts = System.Array.Empty<int>();
            float moves;
            switch (type)
            {
                // Mutările de bază = cât îi trebuie de obicei jucătorului automat (măsurat), apoi × dificultatea.
                case LevelType.Ingredients:
                    ingredients = level <= 4 ? 1 : level <= 30 ? 2 : 3;
                    moves = 4 + ingredients * 7;
                    // De la capitolul 2 apar obstacole care țin ingredientele pe loc: două mutări în plus.
                    if (chapter >= 1) moves += 2;
                    break;
                case LevelType.Frosting:
                    frosting = Mathf.Min(valid / 2, Mathf.RoundToInt(Mathf.Min(24f, 10 + level / 3f) * goalFactor));
                    doubleFrosting = level >= 25 ? frosting / 5 : 0;
                    moves = 6 + (frosting + doubleFrosting) * 0.8f;
                    // În primul capitol (învățare), glazura de pe marginile formelor neregulate cere mutări în plus.
                    if (chapter == 0) moves *= 1.3f;
                    break;
                case LevelType.Score:
                    moves = 18;
                    break;
                case LevelType.Crates:
                    focus = Focus(4 + chapter * 0.8f, 3, 10);
                    moves = 5 + focus * crateHits * 1.6f;
                    break;
                case LevelType.Ice:
                    focus = Focus(4 + chapter * 0.8f, 3, 10);
                    moves = 4 + focus * (1 + iceLayers) * 0.5f * 1.2f;
                    break;
                case LevelType.Chains:
                    focus = Focus(4 + chapter * 0.7f, 3, 9);
                    moves = 4 + focus * 1.1f;
                    break;
                case LevelType.Chocolate:
                    focus = Focus(2 + chapter * 0.3f, 2, 5);
                    moves = 5 + focus * 1.6f;
                    break;
                case LevelType.Specials:
                    specialsTarget = Focus(3 + level / 20f, 3, 8);
                    moves = 3 + specialsTarget * 2f;
                    break;
                case LevelType.Garden:
                    pots = Focus(3 + chapter * 0.4f, 3, 7);
                    focus = pots;
                    moves = 5 + pots * BlastRules.PotHits * 1.4f;
                    break;
                case LevelType.Defuse:
                    timeBombGoal = Focus(4 + chapter * 0.3f, 4, 10);
                    // În deșert (unde apar prima dată) e o singură bombă pe tablă la nivelurile ușoare și normale.
                    timeBombs = level >= 60 ? 3 : chapter <= 3 && difficulty is Difficulty.Easy or Difficulty.Normal ? 1 : 2;
                    timeBombStart = difficulty switch
                    {
                        Difficulty.Easy => 14,
                        Difficulty.Normal => 12,
                        Difficulty.Hard => 10,
                        _ => 9,
                    };
                    // Cu o singură bombă pe tablă, următoarea trebuie întâi să intre: mai multe mutări pe bombă.
                    moves = 4 + timeBombGoal * (timeBombs == 1 ? 3.4f : 2.6f);
                    break;
                case LevelType.Bonus:
                    // Sugar Rush: pe timp; „mutările” sunt doar pentru pragurile de stele (TwoStars/ThreeStars).
                    bonusSeconds = 40;
                    moves = bonusSeconds / 1.2f;
                    break;
                case LevelType.Descent:
                    // Bobo pornește din rândul de sus și trebuie dus până jos.
                    moves = 9f + chapter * 0.4f;
                    break;
                case LevelType.Frozen:
                    // Întâi se sparge gheața din jurul lui (liniile de lângă el), apoi coboară de la mijloc.
                    boboIce = level < 40 ? 2 : 3;
                    moves = 8f + boboIce * 2.5f + chapter * 0.3f;
                    break;
                case LevelType.Blaze:
                    // Focul coboară spre Bobo; la început se întinde mai rar.
                    fires = Focus(3 + chapter * 0.45f, 3, 8);
                    fireEvery = chapter >= 3 && difficulty is Difficulty.Hard or Difficulty.VeryHard ? 2 : 3;
                    moves = 4f + fires * 2.2f;
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
                int kinds = 1 + (level >= 11 ? 1 : 0) + (level >= 20 ? 1 : 0);
                kinds = Mathf.Min(kinds, colors - 1);
                collectColors = new int[kinds];
                collectCounts = new int[kinds];
                int first = random.Next(colors);
                int total = 0;
                for (int i = 0; i < kinds; i++)
                {
                    collectColors[i] = (first + i * 2) % colors;
                    int count = Mathf.RoundToInt(Mathf.Min(60, 20 + level / 3f + random.Next(6)) * goalFactor);
                    collectCounts[i] = count;
                    total += collectCounts[i];
                }
                moves = 3 + total / 5.5f;
            }
            int finalMoves = type == LevelType.Bonus ? 33 : Mathf.Clamp(Mathf.RoundToInt(moves * movesFactor), 12, 65);
            // Scorul țintă: cam cât adună jucătorul automat în ~16 mutări, ajustat după dificultate.
            if (type == LevelType.Score) scoreTarget = Mathf.RoundToInt((16 * 400 + level * 30) * goalFactor);

            // Obstacolele apar treptat (ladă de la 4, gheață de la 7, lanțuri de la 12, ciocolată de la 25) și
            // urmează valul de dificultate. Pe nivelurile cu bomboane sunt doar lăzi (gheața și lanțurile ar ține
            // bomboanele pe loc), iar pe cele cu glazură obstacolele sunt mai puține.
            // Pe nivelurile cu un obstacol ca obiectiv, celelalte obstacole sunt mai puține (nu acoperă ținta).
            bool ingredientLevel = type is LevelType.Ingredients or LevelType.Descent or LevelType.Frozen;
            bool focusLevel = focus > 0;
            float blockerShare = type == LevelType.Frosting || type == LevelType.Blaze || ingredientLevel || focusLevel ? 0.5f : 1f;
            int Scaled(float baseCount) => Mathf.RoundToInt(baseCount * obstacleFactor * blockerShare);
            int crates = level < 4 ? 0 : Mathf.Min(8, Scaled(2 + chapter * 0.6f));
            int ice = level < 7 || ingredientLevel ? 0 : Mathf.Min(8, Scaled(2 + chapter * 0.6f));
            int chains = level < 12 || ingredientLevel ? 0 : Mathf.Min(6, Scaled(1 + chapter * 0.4f));
            int chocolate = level < 25 || blockerShare < 1f ? 0 : Mathf.Min(3, Scaled(1 + chapter * 0.2f));
            switch (type)
            {
                case LevelType.Crates: crates = focus; break;
                case LevelType.Ice: ice = focus; break;
                case LevelType.Chains: chains = focus; break;
                case LevelType.Chocolate: chocolate = focus; break;
                case LevelType.Garden: crates = Mathf.Min(crates, 2); break;
            }
            // Cel mult un sfert din tablă acoperit de obstacole (formele mici nu se umplu); obstacolul-țintă se taie ultimul.
            int obstacleRoom = valid / 4;
            obstacleRoom = Mathf.Max(0, obstacleRoom - pots);
            while (crates + ice + chains + chocolate > obstacleRoom)
            {
                if (type != LevelType.Ice && ice > 0 && ice >= crates && ice >= chains) ice--;
                else if (type != LevelType.Crates && crates > 0 && crates >= chains) crates--;
                else if (type != LevelType.Chains && chains > 0) chains--;
                else if (type != LevelType.Chocolate && chocolate > 0) chocolate--;
                else if (type != LevelType.Ice && ice > 0) ice--;
                else if (type != LevelType.Crates && crates > 0) crates--;
                else if (crates + ice + chains + chocolate > 0)
                {
                    // Doar obstacolul-țintă a mai rămas: îl reducem pe el.
                    if (crates > 0) crates--;
                    else if (ice > 0) ice--;
                    else if (chains > 0) chains--;
                    else chocolate--;
                }
            }

            // Portalurile (de la Lacul de Afine): pe toate nivelurile din lac, apoi pe unele niveluri. Nu pe cele cu
            // bomboane-ingredient (ar ieși prin portal în loc să ajungă jos), nici la Boss sau în bonus.
            int[] portalIn = System.Array.Empty<int>(), portalOut = System.Array.Empty<int>();
            bool portals = level >= PortalsFrom && type != LevelType.Ingredients && type != LevelType.Boss && type != LevelType.Bonus
                && type != LevelType.Descent && type != LevelType.Frozen
                && (chapter == 2 || random.Next(10) < 3);
            if (portals)
            {
                int pairs = level >= 60 ? 2 : 1;
                portalIn = new int[pairs];
                portalOut = new int[pairs];
                // Intrările în stânga, ieșirile în dreapta (fără cicluri): piesele trec de jos-stânga sus-dreapta.
                for (int i = 0; i < pairs; i++)
                {
                    portalIn[i] = 1 + i * 2 + random.Next(2);
                    portalOut[i] = BoardColumns - 2 - i * 2 - random.Next(2);
                }
            }

            return new LevelConfig(level, difficulty, colors, finalMoves, type, bottom, top, ingredients, collectColors, collectCounts,
                frosting, doubleFrosting, scoreTarget, specialsTarget, crates, crateHits, ice, iceLayers, chains, chocolate,
                pots, timeBombs, timeBombStart, timeBombGoal, portalIn, portalOut, bonusSeconds, boboIce, fires, fireEvery);
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
