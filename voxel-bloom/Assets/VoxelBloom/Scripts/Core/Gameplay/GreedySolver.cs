using System;
using System.Collections.Generic;

namespace VoxelBloom.Core
{
    /// <summary>
    /// Heuristic witness solver mirroring tools/rules.mjs (bestColumn / solveGreedy). Not exhaustive: a win proves
    /// the level solvable (the column list is a witness); a loss only means the heuristic found no solution.
    /// </summary>
    public static class GreedySolver
    {
        public sealed class Result
        {
            public bool Won;
            public int Moves, Score, MaxCombo;
            public FailReason Fail;
            public readonly List<int> Taps = new List<int>();
        }

        public static int BestColumn(GameSession s)
        {
            int best = -1;
            double bestScore = double.NegativeInfinity;
            for (int col = 0; col < GameSession.Columns; col++)
            {
                var ob = s.Top(col);
                if (!ob.HasValue) continue;
                var b = ob.Value;
                int exp = s.ExposedCount(b.Color);
                int pending = 0;
                for (int li = 0; li < GameSession.Launchers; li++) { var l = s.LauncherAt(li); if (l.HasValue && l.Value.Color == b.Color) pending += l.Value.Power; }
                int need = Math.Max(0, s.Remaining(b.Color) - pending);
                var next = s.BlockAt(col, 1);
                double sc = (exp > 0 ? 1000 : 0) + Math.Min(exp, b.Power) * 2 + (need > 0 ? 50 : -500)
                            - (s.ColumnLength(col) > 1 && next.HasValue && s.ExposedCount(next.Value.Color) > 0 ? 0 : 5);
                if (sc > bestScore) { bestScore = sc; best = col; }
            }
            return best;
        }

        public static Result Solve(LevelDefinition level, int maxMoves = 1000)
        {
            var s = new GameSession(level);
            var r = new Result();
            while (!s.IsWon && r.Taps.Count < maxMoves)
            {
                s.Settle();
                if (s.IsWon || s.GetFailReason() != FailReason.None) break;
                if (s.FreeLauncher() < 0) break;
                int col = BestColumn(s);
                if (col < 0) { s.ResolveAll(); break; }
                s.Tap(col);
                r.Taps.Add(col);
            }
            if (!s.IsWon) s.ResolveAll();
            r.Won = s.IsWon; r.Moves = s.Moves; r.Score = s.Score; r.MaxCombo = s.MaxCombo; r.Fail = s.GetFailReason();
            return r;
        }

        /// <summary>Replays a witness exactly like tools/rules.mjs replay(): settle, tap, ..., then resolve.</summary>
        public static GameSession Replay(LevelDefinition level, IReadOnlyList<int> taps)
        {
            var s = new GameSession(level);
            foreach (int col in taps) { s.Settle(); if (s.Tap(col) < 0) return null; }
            s.ResolveAll();
            return s;
        }
    }
}
