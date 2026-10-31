using System;
using System.Collections.Generic;
using System.Diagnostics;
using Veliqora.Core.Puzzle;

namespace Veliqora.Core.Solver
{
    public sealed class SolverOptions
    {
        /// <summary>Hard cap on expanded nodes (memory/CPU bound).</summary>
        public int MaxNodes = 200_000;
        /// <summary>Wall-clock timeout in milliseconds (0 = none).</summary>
        public int TimeoutMs = 2_000;
        /// <summary>Weight on heuristic. 1 = A*, &gt;1 = faster, greedier, non-optimal.</summary>
        public double HeuristicWeight = 1.5;
        /// <summary>Cancellation hook polled periodically (e.g. scene unload). May be null.</summary>
        public Func<bool> IsCancelled;

        public static SolverOptions Hint => new SolverOptions { MaxNodes = 60_000, TimeoutMs = 800, HeuristicWeight = 2.0 };
        public static SolverOptions Validation => new SolverOptions { MaxNodes = 400_000, TimeoutMs = 10_000, HeuristicWeight = 1.25 };
        public static SolverOptions Generation => new SolverOptions { MaxNodes = 150_000, TimeoutMs = 2_500, HeuristicWeight = 1.5 };
    }

    public enum SolveStatus { Solved, AlreadySolved, Unsolvable, NodeLimit, Timeout, Cancelled }

    public sealed class SolverResult
    {
        public SolveStatus Status;
        public List<Move> Moves = new List<Move>();
        public int NodesExpanded;
        public int NodesGenerated;
        public long ElapsedMs;
        /// <summary>Average useful moves available along the solution path (practical branching).</summary>
        public double AverageBranching;

        public bool IsSolved => Status == SolveStatus.Solved || Status == SolveStatus.AlreadySolved;
        /// <summary>True when the search space was exhausted without a solution (proved unsolvable).</summary>
        public bool ProvedUnsolvable => Status == SolveStatus.Unsolvable;
        public int Length => Moves.Count;

        public override string ToString() => Status + " len=" + Length + " nodes=" + NodesExpanded + " ms=" + ElapsedMs;
    }

    /// <summary>
    /// Weighted A* over board states with canonicalization (bottle order ignored), symmetric-empty-bottle
    /// pruning and pointless-move pruning. Returns an explicit move sequence with real bottle indices.
    /// Not guaranteed optimal unless HeuristicWeight == 1 and the heuristic is admissible (it is not strictly),
    /// so lengths are reported as "solver benchmark", never as "optimal".
    /// </summary>
    public static class PuzzleSolver
    {
        private struct Node
        {
            public GameState State;
            public int Parent;
            public Move Move;
            public int G;
        }

        public static SolverResult Solve(GameState start, SolverOptions options = null)
        {
            options = options ?? new SolverOptions();
            var result = new SolverResult { Status = SolveStatus.Unsolvable };
            var sw = Stopwatch.StartNew();

            if (PuzzleRules.IsWon(start))
            {
                result.Status = SolveStatus.AlreadySolved;
                return result;
            }

            var nodes = new List<Node>(1024) { new Node { State = start, Parent = -1, G = 0 } };
            var bestG = new Dictionary<string, int> { { start.CanonicalKey(), 0 } };
            var open = new MinHeap();
            open.Push(Priority(0, Heuristic(start), options.HeuristicWeight), 0);

            while (open.Count > 0)
            {
                if ((result.NodesExpanded & 255) == 0)
                {
                    if (options.IsCancelled != null && options.IsCancelled()) { result.Status = SolveStatus.Cancelled; break; }
                    if (options.TimeoutMs > 0 && sw.ElapsedMilliseconds > options.TimeoutMs) { result.Status = SolveStatus.Timeout; break; }
                }
                if (result.NodesExpanded >= options.MaxNodes) { result.Status = SolveStatus.NodeLimit; break; }

                int idx = open.Pop();
                var node = nodes[idx];
                // Stale entry (a cheaper path to the same canonical state was found later).
                if (bestG.TryGetValue(node.State.CanonicalKey(), out int g0) && g0 < node.G) continue;
                result.NodesExpanded++;

                var state = node.State;
                int firstEmpty = -1;
                for (int f = 0; f < state.BottleCount; f++)
                {
                    var src = state[f];
                    if (src.IsEmpty || src.IsCompleted) continue;
                    firstEmpty = -1;
                    for (int t = 0; t < state.BottleCount; t++)
                    {
                        if (!PuzzleRules.IsUseful(state, f, t)) continue;
                        if (state[t].IsEmpty)
                        {
                            // All empty bottles are equivalent: only try the first one.
                            if (firstEmpty >= 0) continue;
                            firstEmpty = t;
                        }
                        var mr = PuzzleRules.Evaluate(state, f, t);
                        var next = mr.After;
                        int g = node.G + 1;
                        string key = next.CanonicalKey();
                        if (bestG.TryGetValue(key, out int seen) && seen <= g) continue;
                        bestG[key] = g;
                        nodes.Add(new Node { State = next, Parent = idx, Move = mr.Move, G = g });
                        result.NodesGenerated++;
                        int childIdx = nodes.Count - 1;
                        if (PuzzleRules.IsWon(next))
                        {
                            Reconstruct(nodes, childIdx, result);
                            result.Status = SolveStatus.Solved;
                            result.ElapsedMs = sw.ElapsedMilliseconds;
                            result.AverageBranching = MeasureBranching(start, result.Moves);
                            return result;
                        }
                        open.Push(Priority(g, Heuristic(next), options.HeuristicWeight), childIdx);
                    }
                }
            }

            // Reaching here without a solution: either a budget stopped us (status already set) or the
            // open list was exhausted, which proves the start state unsolvable (status stays Unsolvable).
            result.ElapsedMs = sw.ElapsedMilliseconds;
            return result;
        }

        /// <summary>Replays a move list from start; returns true when every move is legal and the end state is won.</summary>
        public static bool VerifySolution(GameState start, IList<Move> moves, out string error)
        {
            error = null;
            var s = start;
            for (int i = 0; i < moves.Count; i++)
            {
                var mr = PuzzleRules.Evaluate(s, moves[i].From, moves[i].To);
                if (!mr.IsLegal) { error = "Move " + i + " illegal: " + mr.Rejection; return false; }
                if (mr.Move.Amount != moves[i].Amount && moves[i].Amount != 0) { error = "Move " + i + " amount mismatch"; return false; }
                s = mr.After;
            }
            if (!PuzzleRules.IsWon(s)) { error = "Sequence does not end in a won state"; return false; }
            return true;
        }

        private static double Priority(int g, int h, double w) => g + w * h;

        /// <summary>
        /// Heuristic: color boundaries inside bottles + extra bottles each color is spread across.
        /// Each move can fix at most a small number of these, so it estimates remaining moves.
        /// </summary>
        public static int Heuristic(GameState s)
        {
            int boundaries = 0;
            // colorBottles[c] = number of bottles where color c appears
            var spread = new Dictionary<int, int>();
            for (int b = 0; b < s.BottleCount; b++)
            {
                var bottle = s[b];
                int prev = 0;
                for (int i = 0; i < bottle.Count; i++)
                {
                    int c = bottle[i];
                    if (c != prev)
                    {
                        if (prev != 0) boundaries++;
                        // Count each color once per bottle segment
                        spread.TryGetValue(c, out int n);
                        spread[c] = n + 1;
                        prev = c;
                    }
                }
            }
            int extra = 0;
            foreach (var kv in spread) extra += kv.Value - 1;
            return boundaries + extra;
        }

        private static void Reconstruct(List<Node> nodes, int idx, SolverResult result)
        {
            var stack = new List<Move>();
            while (idx > 0)
            {
                stack.Add(nodes[idx].Move);
                idx = nodes[idx].Parent;
            }
            stack.Reverse();
            result.Moves = stack;
        }

        private static double MeasureBranching(GameState start, List<Move> moves)
        {
            if (moves.Count == 0) return 0;
            var s = start;
            long total = 0;
            foreach (var m in moves)
            {
                total += PuzzleRules.LegalMoves(s, true).Count;
                s = PuzzleRules.Evaluate(s, m.From, m.To).After;
            }
            return (double)total / moves.Count;
        }

        /// <summary>Binary min-heap keyed by double priority; ties broken by insertion order for determinism.</summary>
        private sealed class MinHeap
        {
            private readonly List<(double p, long seq, int v)> _items = new List<(double, long, int)>();
            private long _seq;
            public int Count => _items.Count;

            public void Push(double priority, int value)
            {
                _items.Add((priority, _seq++, value));
                int i = _items.Count - 1;
                while (i > 0)
                {
                    int parent = (i - 1) / 2;
                    if (!Less(i, parent)) break;
                    Swap(i, parent);
                    i = parent;
                }
            }

            public int Pop()
            {
                var top = _items[0].v;
                int last = _items.Count - 1;
                _items[0] = _items[last];
                _items.RemoveAt(last);
                int i = 0;
                while (true)
                {
                    int l = i * 2 + 1, r = l + 1, m = i;
                    if (l < _items.Count && Less(l, m)) m = l;
                    if (r < _items.Count && Less(r, m)) m = r;
                    if (m == i) break;
                    Swap(i, m);
                    i = m;
                }
                return top;
            }

            private bool Less(int a, int b)
            {
                var x = _items[a];
                var y = _items[b];
                return x.p < y.p || (x.p == y.p && x.seq < y.seq);
            }

            private void Swap(int a, int b)
            {
                var t = _items[a];
                _items[a] = _items[b];
                _items[b] = t;
            }
        }
    }
}
