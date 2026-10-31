using System;
using System.Collections.Generic;
using Veliqora.Core.Json;
using Veliqora.Core.Puzzle;

namespace Veliqora.Core.Levels
{
    public enum DifficultyTier { Tutorial = 0, Beginner = 1, Intermediate = 2, Advanced = 3, Expert = 4, Master = 5 }

    public static class DifficultyTiers
    {
        /// <summary>Tutorial 1–5, Beginner 6–25, Intermediate 26–75, Advanced 76–150, Expert 151–200, Master 201+.</summary>
        public static DifficultyTier ForLevel(int number)
        {
            if (number <= 5) return DifficultyTier.Tutorial;
            if (number <= 25) return DifficultyTier.Beginner;
            if (number <= 75) return DifficultyTier.Intermediate;
            if (number <= 150) return DifficultyTier.Advanced;
            if (number <= 200) return DifficultyTier.Expert;
            return DifficultyTier.Master;
        }
    }

    /// <summary>Recorded validation evidence for a level (from offline generation / batch validation).</summary>
    public sealed class LevelValidation
    {
        public string Status;          // SolveStatus name
        public int SolutionLength;
        public int NodesExpanded;
        public long ElapsedMs;
        public double AverageBranching;
        public int Attempts;           // generator attempts consumed
        public double StuckRate;       // naive-playout stuck fraction (0..1)
    }

    /// <summary>Immutable-by-convention level data. Bottles are bottom-to-top ColorIds; empty arrays are empty bottles.</summary>
    public sealed class LevelDefinition
    {
        public int Number;
        public ulong Seed;
        public int Capacity = BottleState.DefaultCapacity;
        public int ColorCount;
        public int[][] Bottles = new int[0][];
        public DifficultyTier Tier;
        public bool Handcrafted;
        /// <summary>Solver benchmark move count (not claimed optimal). Used for stars.</summary>
        public int ParMoves;
        /// <summary>Verified solution: each entry {from, to, amount}.</summary>
        public int[][] Solution = new int[0][];
        /// <summary>Optional contextual tutorial key consumed by the presentation layer.</summary>
        public string TutorialKey;
        public LevelValidation Validation = new LevelValidation();

        public string Id => "L" + Number.ToString("000");
        public int EmptyBottleCount
        {
            get
            {
                int n = 0;
                foreach (var b in Bottles) if (b.Length == 0) n++;
                return n;
            }
        }

        public GameState CreateInitialState() => GameState.FromArrays(Capacity, Bottles);

        public List<Move> SolutionMoves()
        {
            var list = new List<Move>(Solution.Length);
            foreach (var s in Solution) list.Add(new Move(s[0], s[1], s[2], 0));
            return list;
        }

        public static int[][] EncodeMoves(IList<Move> moves)
        {
            var arr = new int[moves.Count][];
            for (int i = 0; i < moves.Count; i++) arr[i] = new[] { moves[i].From, moves[i].To, moves[i].Amount };
            return arr;
        }

        // ---------------- JSON ----------------

        public Dictionary<string, object> ToJson()
        {
            var bottles = new List<object>();
            foreach (var b in Bottles) bottles.Add(MiniJson.FromIntArray(b));
            var sol = new List<object>();
            foreach (var m in Solution) sol.Add(MiniJson.FromIntArray(m));
            var d = new Dictionary<string, object>
            {
                ["number"] = (long)Number,
                ["seed"] = Seed.ToString(),
                ["capacity"] = (long)Capacity,
                ["colors"] = (long)ColorCount,
                ["tier"] = Tier.ToString(),
                ["handcrafted"] = Handcrafted,
                ["par"] = (long)ParMoves,
                ["bottles"] = bottles,
                ["solution"] = sol,
                ["validation"] = new Dictionary<string, object>
                {
                    ["status"] = Validation.Status ?? "",
                    ["length"] = (long)Validation.SolutionLength,
                    ["nodes"] = (long)Validation.NodesExpanded,
                    ["ms"] = Validation.ElapsedMs,
                    ["branching"] = Math.Round(Validation.AverageBranching, 2),
                    ["attempts"] = (long)Validation.Attempts,
                    ["stuck"] = Math.Round(Validation.StuckRate, 3),
                },
            };
            if (!string.IsNullOrEmpty(TutorialKey)) d["tutorial"] = TutorialKey;
            return d;
        }

        public static LevelDefinition FromJson(Dictionary<string, object> d)
        {
            var def = new LevelDefinition
            {
                Number = MiniJson.GetInt(d, "number"),
                Seed = ulong.Parse(MiniJson.GetString(d, "seed", "0")),
                Capacity = MiniJson.GetInt(d, "capacity", BottleState.DefaultCapacity),
                ColorCount = MiniJson.GetInt(d, "colors"),
                Handcrafted = MiniJson.GetBool(d, "handcrafted"),
                ParMoves = MiniJson.GetInt(d, "par"),
                TutorialKey = MiniJson.GetString(d, "tutorial"),
            };
            DifficultyTier tier;
            def.Tier = Enum.TryParse(MiniJson.GetString(d, "tier", ""), out tier) ? tier : DifficultyTiers.ForLevel(def.Number);
            var bottles = MiniJson.GetList(d, "bottles");
            def.Bottles = new int[bottles.Count][];
            for (int i = 0; i < bottles.Count; i++) def.Bottles[i] = MiniJson.ToIntArray(bottles[i]);
            var sol = MiniJson.GetList(d, "solution");
            def.Solution = new int[sol.Count][];
            for (int i = 0; i < sol.Count; i++) def.Solution[i] = MiniJson.ToIntArray(sol[i]);
            var v = MiniJson.GetObject(d, "validation");
            def.Validation = new LevelValidation
            {
                Status = MiniJson.GetString(v, "status", ""),
                SolutionLength = MiniJson.GetInt(v, "length"),
                NodesExpanded = MiniJson.GetInt(v, "nodes"),
                ElapsedMs = MiniJson.GetLong(v, "ms"),
                AverageBranching = MiniJson.GetDouble(v, "branching"),
                Attempts = MiniJson.GetInt(v, "attempts"),
                StuckRate = MiniJson.GetDouble(v, "stuck"),
            };
            return def;
        }
    }
}
