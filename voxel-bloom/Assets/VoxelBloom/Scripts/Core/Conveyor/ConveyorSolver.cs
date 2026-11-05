using System.Collections.Generic;

namespace VoxelBloom.Core
{
    /// <summary>Greedy player, witness replay and hints for conveyor BUS levels (mirror of tools/conveyor/rules.mjs).</summary>
    public static class ConveyorSolver
    {
        public struct Result { public bool Won; public int Ticks; }

        static Dictionary<int, int> Demand(ConveyorSession s)
        {
            var d = new Dictionary<int, int>();
            foreach (var st in s.Stations)
                if (st.IsOpen)
                    for (int i = 0; i < st.Tray.Need.Length; i++)
                        if (st.Tray.Have[i] == ConveyorSession.Empty) d[st.Tray.Need[i]] = (d.TryGetValue(st.Tray.Need[i], out var n) ? n : 0) + 1;
            foreach (int c in s.Slot) if (c >= 0 && d.ContainsKey(c)) d[c]--;
            foreach (var st in s.Stations) foreach (int c in st.Out) if (d.ContainsKey(c)) d[c]--;
            return d;
        }

        static int Get(Dictionary<int, int> d, int c) => d.TryGetValue(c, out var n) ? n : 0;

        /// <summary>Heuristic action [tick, kind, index] or null (mirror of bestAction).</summary>
        public static int[] BestAction(ConveyorSession s)
        {
            var d = Demand(s);
            int[] best = null; int bestScore = 0;
            for (int k = 0; k < s.Stations.Length; k++)
            {
                if (!s.CanRelease(k)) continue;
                var b = s.Stations[k].Tray; int c = b.Have[b.WrongIndex()];
                int score = Get(d, c) > 0 ? 100 : s.Occupied < s.Space - 2 ? 10 : 0;
                if (score > bestScore) { bestScore = score; best = new[] { s.Tick, ConveyorSession.ActRelease, k }; }
            }
            for (int i = 0; i < s.VisibleCount; i++)
            {
                if (!s.CanPlace(i)) continue;
                var b = s.Queue[i]; int score = 50;
                for (int j = 0; j < b.Need.Length; j++) { if (b.Have[j] == b.Need[j]) score += 3; else if (Get(d, b.Have[j]) > 0) score += 5; }
                if (score > bestScore) { bestScore = score; best = new[] { s.Tick, ConveyorSession.ActPlace, i }; }
            }
            return best;
        }

        public static Result PlayGreedy(ConveyorSession s)
        {
            while (!s.IsWon && !s.IsStuck() && s.Tick < ConveyorSession.MaxTicks)
            {
                int[] a; int guard = 0;
                while ((a = BestAction(s)) != null && guard++ < 50) s.Act(a);
                s.Step();
            }
            return new Result { Won = s.IsWon, Ticks = s.Tick };
        }

        /// <summary>Replays a witness [[tick, kind, index], ...]; returns the final session.</summary>
        public static ConveyorSession Replay(ConveyorLevel level, int[][] witness)
        {
            var s = new ConveyorSession(level);
            int w = 0;
            while (!s.IsWon && s.Tick < ConveyorSession.MaxTicks)
            {
                while (w < witness.Length && witness[w][0] == s.Tick) { if (!s.Act(witness[w])) return s; w++; }
                if (s.IsStuck()) break;
                s.Step();
            }
            return s;
        }

        /// <summary>Hint for the tutorial: the action the greedy player would take now (null if none).</summary>
        public static int[] Hint(ConveyorSession s) => BestAction(s);
    }
}
