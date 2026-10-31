using System;
using System.Collections.Generic;
using Veliqora.Core.Json;

namespace Veliqora.Core.Puzzle
{
    /// <summary>Lossless JSON for a full board: capacities, units, mystery depth, corks, ad locks and move count.</summary>
    public static class GameStateJson
    {
        public static Dictionary<string, object> ToJson(GameState s)
        {
            var bottles = new List<object>(s.BottleCount);
            for (int i = 0; i < s.BottleCount; i++)
            {
                var b = s[i];
                var d = new Dictionary<string, object> { ["c"] = (long)b.Capacity, ["u"] = MiniJson.FromIntArray(b.ToArray()) };
                if (b.Hidden > 0) d["h"] = (long)b.Hidden;
                if (b.Lock != null) d["lock"] = new Dictionary<string, object> { ["k"] = b.Lock.Kind.ToString(), ["p"] = (long)b.Lock.Param };
                if (b.RewardLocked) d["ad"] = true;
                bottles.Add(d);
            }
            return new Dictionary<string, object> { ["moves"] = (long)s.Moves, ["bottles"] = bottles };
        }

        /// <summary>Parses a full board; also accepts the legacy format (array of unit arrays) with a default capacity.</summary>
        public static GameState FromJson(object o, int legacyCapacity)
        {
            if (o is List<object> legacy)
            {
                var arr = new int[legacy.Count][];
                for (int i = 0; i < legacy.Count; i++) arr[i] = MiniJson.ToIntArray(legacy[i]);
                return GameState.FromArrays(legacyCapacity, arr);
            }
            var d = MiniJson.AsObject(o);
            var list = new List<BottleState>();
            foreach (var bo in MiniJson.GetList(d, "bottles"))
            {
                var b = MiniJson.AsObject(bo);
                BottleLock lk = null;
                if (MiniJson.TryGet(b, "lock", out var lo))
                {
                    var l = MiniJson.AsObject(lo);
                    LockKind kind;
                    if (!Enum.TryParse(MiniJson.GetString(l, "k", ""), out kind)) throw new FormatException("Bad lock kind");
                    lk = new BottleLock(kind, MiniJson.GetInt(l, "p"));
                }
                list.Add(new BottleState(MiniJson.GetInt(b, "c", legacyCapacity), MiniJson.ToIntArray(MiniJson.GetList(b, "u")),
                    MiniJson.GetInt(b, "h"), lk, MiniJson.GetBool(b, "ad")));
            }
            return new GameState(list, MiniJson.GetInt(d, "moves"));
        }
    }
}
