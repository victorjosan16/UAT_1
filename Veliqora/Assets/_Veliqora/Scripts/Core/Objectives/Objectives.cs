using System;
using System.Collections.Generic;
using Veliqora.Core.Json;
using Veliqora.Core.Puzzle;

namespace Veliqora.Core.Objectives
{
    public enum ObjectiveType
    {
        /// <summary>Complete <c>Target</c> bottle(s) of color <c>Color</c>.</summary>
        CollectColor = 0,
        /// <summary>Complete <c>Target</c> bottles of any color.</summary>
        CompleteBottles = 1,
        /// <summary>Uncork <c>Target</c> bottles.</summary>
        UnlockBottles = 2,
        /// <summary>Reveal <c>Target</c> mystery layers.</summary>
        RevealMystery = 3,
        /// <summary>Finish the level (used for rewarded bonus objectives).</summary>
        FinishLevel = 4,
    }

    /// <summary>
    /// Data for one top-panel objective. Progress is DERIVED from the board state (never stored separately), so
    /// undo/restore keep it consistent automatically. Rewards are granted once per attempt when the level is won.
    /// </summary>
    public sealed class ObjectiveDef
    {
        public string Id;
        public ObjectiveType Type;
        public int Color;
        public int Target = 1;
        public int RewardCoins;
        /// <summary>Locked objective: inactive until the objective with this id is complete (null = always active).</summary>
        public string RequiresId;
        /// <summary>Rewarded objective: its reward is claimable only through a completed rewarded ad.</summary>
        public bool Rewarded;

        public Dictionary<string, object> ToJson()
        {
            var d = new Dictionary<string, object>
            {
                ["id"] = Id,
                ["type"] = Type.ToString(),
                ["target"] = (long)Target,
                ["reward"] = (long)RewardCoins,
            };
            if (Type == ObjectiveType.CollectColor) d["color"] = (long)Color;
            if (!string.IsNullOrEmpty(RequiresId)) d["requires"] = RequiresId;
            if (Rewarded) d["rewarded"] = true;
            return d;
        }

        public static ObjectiveDef FromJson(Dictionary<string, object> d)
        {
            ObjectiveType t;
            if (!Enum.TryParse(MiniJson.GetString(d, "type", ""), out t)) throw new FormatException("Unknown objective type");
            var o = new ObjectiveDef
            {
                Id = MiniJson.GetString(d, "id", ""),
                Type = t,
                Color = MiniJson.GetInt(d, "color"),
                Target = Math.Max(1, MiniJson.GetInt(d, "target", 1)),
                RewardCoins = Math.Max(0, MiniJson.GetInt(d, "reward")),
                RequiresId = MiniJson.GetString(d, "requires"),
                Rewarded = MiniJson.GetBool(d, "rewarded"),
            };
            if (string.IsNullOrEmpty(o.Id)) throw new FormatException("Objective id required");
            return o;
        }
    }

    public struct ObjectiveStatus
    {
        public ObjectiveDef Def;
        public int Progress;
        public bool Unlocked;
        public bool Complete => Unlocked && Progress >= Def.Target;
    }

    /// <summary>Computes objective progress from the initial and current board states.</summary>
    public static class ObjectiveTracker
    {
        public static int RawProgress(ObjectiveDef def, GameState initial, GameState current, bool won)
        {
            switch (def.Type)
            {
                case ObjectiveType.CollectColor:
                {
                    int n = 0;
                    for (int i = 0; i < current.BottleCount; i++)
                        if (current[i].TopColor == def.Color && current.IsBottleComplete(i)) n++;
                    return n;
                }
                case ObjectiveType.CompleteBottles: return current.CompletedBottleCount();
                case ObjectiveType.UnlockBottles:
                {
                    int n = 0;
                    for (int i = 0; i < initial.BottleCount && i < current.BottleCount; i++)
                        if (initial[i].IsCorked && !current[i].IsCorked) n++;
                    return n;
                }
                case ObjectiveType.RevealMystery: return Math.Max(0, initial.HiddenTotal() - current.HiddenTotal());
                case ObjectiveType.FinishLevel: return won ? 1 : 0;
                default: return 0;
            }
        }

        public static List<ObjectiveStatus> Evaluate(IList<ObjectiveDef> defs, GameState initial, GameState current, bool won)
        {
            var list = new List<ObjectiveStatus>(defs?.Count ?? 0);
            if (defs == null) return list;
            var done = new HashSet<string>();
            // Two passes so "requires" chains resolve regardless of declaration order.
            for (int pass = 0; pass < 2; pass++)
            {
                list.Clear();
                foreach (var d in defs)
                {
                    bool unlocked = string.IsNullOrEmpty(d.RequiresId) || done.Contains(d.RequiresId);
                    int p = Math.Min(d.Target, RawProgress(d, initial, current, won));
                    var st = new ObjectiveStatus { Def = d, Progress = unlocked ? p : 0, Unlocked = unlocked };
                    if (st.Complete) done.Add(d.Id);
                    list.Add(st);
                }
            }
            return list;
        }

        /// <summary>Checks a level's objectives are achievable by the time the level is won.</summary>
        public static bool AllAchievableAtWin(IList<ObjectiveDef> defs, GameState initial, GameState wonState, out string error)
        {
            error = null;
            foreach (var st in Evaluate(defs, initial, wonState, true))
                if (!st.Complete) { error = "Objective " + st.Def.Id + " not complete in the solved state"; return false; }
            return true;
        }
    }
}
