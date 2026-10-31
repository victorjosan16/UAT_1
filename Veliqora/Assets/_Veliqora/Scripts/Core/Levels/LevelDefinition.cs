using System;
using System.Collections.Generic;
using Veliqora.Core.Json;
using Veliqora.Core.Objectives;
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
        /// <summary>Pacing role from StagePlanner (Normal / random Hard / stage-finale Boss).</summary>
        public LevelKind Kind = LevelKind.Normal;

        // ---- special mechanics (all optional; absent = classic level) ----
        /// <summary>Per-bottle capacities (2–6). Null = every bottle uses <see cref="Capacity"/>.</summary>
        public int[] Capacities;
        /// <summary>Mystery units at the bottom of each bottle. Null = none.</summary>
        public int[] Hidden;
        /// <summary>Corked bottles and their unlock conditions.</summary>
        public List<LockDef> Locks = new List<LockDef>();
        /// <summary>Indices of bottles that are unlocked only through a completed rewarded ad (never required).</summary>
        public int[] RewardBottles = new int[0];
        /// <summary>Top-panel objectives (may be empty).</summary>
        public List<ObjectiveDef> Objectives = new List<ObjectiveDef>();
        /// <summary>Boosters disabled on this level (BoosterType names).</summary>
        public List<string> DisabledBoosters = new List<string>();
        /// <summary>First-clear base coins override (0 = economy default).</summary>
        public int RewardCoins;
        /// <summary>1 (easiest) … 10 (hardest), from measured solution length, branching, trap density and mechanics.</summary>
        public int Difficulty = 1;
        /// <summary>Capacity of an Extra Bottle on this level (0 = <see cref="Capacity"/>).</summary>
        public int ExtraBottleCapacity;

        public sealed class LockDef
        {
            public int Bottle;
            public LockKind Kind;
            public int Param;
        }

        public bool HasMystery { get { if (Hidden == null) return false; foreach (var h in Hidden) if (h > 0) return true; return false; } }
        public bool HasLocks => Locks.Count > 0;
        public bool HasMixedCapacities { get { if (Capacities == null) return false; foreach (var c in Capacities) if (c != Capacity) return true; return false; } }
        public int CapacityOf(int bottle) => Capacities != null && bottle < Capacities.Length ? Capacities[bottle] : Capacity;
        public int ExtraCapacity => ExtraBottleCapacity > 0 ? ExtraBottleCapacity : Capacity;
        public bool IsBoosterDisabled(string booster) => DisabledBoosters.Contains(booster);

        public int Stage => StagePlanner.StageOf(Number);
        public bool IsHard => Kind != LevelKind.Normal;

        public string Id => "L" + Number.ToString("000");
        public int EmptyBottleCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < Bottles.Length; i++)
                    if (Bottles[i].Length == 0 && Array.IndexOf(RewardBottles, i) < 0) n++;
                return n;
            }
        }

        /// <summary>Builds the full starting board (capacities, mystery layers, corks, ad locks) and resolves corks whose
        /// condition already holds.</summary>
        public GameState CreateInitialState()
        {
            var list = new BottleState[Bottles.Length];
            for (int i = 0; i < Bottles.Length; i++)
            {
                BottleLock lk = null;
                foreach (var l in Locks) if (l.Bottle == i) lk = new BottleLock(l.Kind, l.Param);
                int hidden = Hidden != null && i < Hidden.Length ? Hidden[i] : 0;
                list[i] = new BottleState(CapacityOf(i), Bottles[i], hidden, lk, Array.IndexOf(RewardBottles, i) >= 0);
            }
            return PuzzleRules.ResolveLocks(new GameState(list));
        }

        /// <summary>Copies the special-bottle layout of <paramref name="s"/> into this definition.</summary>
        public void ApplyLayout(GameState s)
        {
            Bottles = s.ToArrays();
            Capacities = null; Hidden = null;
            Locks = new List<LockDef>();
            var reward = new List<int>();
            bool mixed = false, mystery = false;
            for (int i = 0; i < s.BottleCount; i++)
            {
                if (s[i].Capacity != Capacity) mixed = true;
                if (s[i].Hidden > 0) mystery = true;
                if (s[i].Lock != null) Locks.Add(new LockDef { Bottle = i, Kind = s[i].Lock.Kind, Param = s[i].Lock.Param });
                if (s[i].RewardLocked) reward.Add(i);
            }
            if (mixed) { Capacities = new int[s.BottleCount]; for (int i = 0; i < s.BottleCount; i++) Capacities[i] = s[i].Capacity; }
            if (mystery) { Hidden = new int[s.BottleCount]; for (int i = 0; i < s.BottleCount; i++) Hidden[i] = s[i].Hidden; }
            RewardBottles = reward.ToArray();
        }

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
            if (Kind != LevelKind.Normal) d["kind"] = Kind.ToString().ToLowerInvariant();
            d["difficulty"] = (long)Difficulty;
            if (Capacities != null) d["capacities"] = MiniJson.FromIntArray(Capacities);
            if (Hidden != null) d["hidden"] = MiniJson.FromIntArray(Hidden);
            if (Locks.Count > 0)
            {
                var locks = new List<object>();
                foreach (var l in Locks) locks.Add(new Dictionary<string, object> { ["bottle"] = (long)l.Bottle, ["kind"] = l.Kind.ToString(), ["param"] = (long)l.Param });
                d["locks"] = locks;
            }
            if (RewardBottles.Length > 0) d["rewardBottles"] = MiniJson.FromIntArray(RewardBottles);
            if (Objectives.Count > 0)
            {
                var objs = new List<object>();
                foreach (var o in Objectives) objs.Add(o.ToJson());
                d["objectives"] = objs;
            }
            if (DisabledBoosters.Count > 0) { var db = new List<object>(); foreach (var b in DisabledBoosters) db.Add(b); d["disabledBoosters"] = db; }
            if (RewardCoins > 0) d["rewardCoins"] = (long)RewardCoins;
            if (ExtraBottleCapacity > 0) d["extraBottleCapacity"] = (long)ExtraBottleCapacity;
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
            def.Difficulty = MiniJson.GetInt(d, "difficulty", 1);
            if (MiniJson.TryGet(d, "capacities", out var caps)) def.Capacities = MiniJson.ToIntArray(caps);
            if (MiniJson.TryGet(d, "hidden", out var hid)) def.Hidden = MiniJson.ToIntArray(hid);
            foreach (var lo in MiniJson.GetList(d, "locks"))
            {
                var l = MiniJson.AsObject(lo);
                LockKind lk;
                if (!Enum.TryParse(MiniJson.GetString(l, "kind", ""), out lk)) throw new FormatException("Bad lock kind in " + def.Id);
                def.Locks.Add(new LockDef { Bottle = MiniJson.GetInt(l, "bottle"), Kind = lk, Param = MiniJson.GetInt(l, "param") });
            }
            if (MiniJson.TryGet(d, "rewardBottles", out var rb)) def.RewardBottles = MiniJson.ToIntArray(rb);
            foreach (var oo in MiniJson.GetList(d, "objectives")) def.Objectives.Add(ObjectiveDef.FromJson(MiniJson.AsObject(oo)));
            foreach (var bo in MiniJson.GetList(d, "disabledBoosters")) if (bo is string bs) def.DisabledBoosters.Add(bs);
            def.RewardCoins = MiniJson.GetInt(d, "rewardCoins");
            def.ExtraBottleCapacity = MiniJson.GetInt(d, "extraBottleCapacity");
            LevelKind kind;
            def.Kind = Enum.TryParse(MiniJson.GetString(d, "kind", "normal"), true, out kind) ? kind : LevelKind.Normal;
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
