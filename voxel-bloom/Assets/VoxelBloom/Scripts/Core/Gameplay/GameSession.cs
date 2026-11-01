using System;
using System.Collections.Generic;

namespace VoxelBloom.Core
{
    public enum FailReason { None, Jammed, OutOfBlocks }

    /// <summary>Result of one firing round.</summary>
    public sealed class RoundResult
    {
        /// <summary>(launcher index, voxel index) for every projectile fired this round.</summary>
        public readonly List<(int launcher, int voxel)> Shots = new List<(int, int)>();
        public readonly List<int> Freed = new List<int>();
        public readonly List<int> Dropped = new List<int>();
        public int Wasted;
        public int Combo;
        public int ScoreGained;
    }

    /// <summary>
    /// Authoritative, engine-free rules v2 ("launchers"); mirrors tools/rules.mjs exactly so build-time witnesses
    /// replay identically. 4 columns: only the top block of each column is active. Tapping a column moves its top
    /// block into the first free launcher (4). Each round every occupied launcher (slot order) fires one projectile
    /// at the highest-priority exposed voxel of its colour and decrements its count; empty launchers free up; a
    /// launcher whose colour is finished is freed; one with no exposed voxel waits. After each round detached
    /// pieces fall and finished colours leave the columns. Win: all voxels destroyed. Fail: all launchers occupied
    /// and none can fire, or no blocks left and no launcher can fire.
    /// </summary>
    public sealed class GameSession
    {
        public const int Columns = 4, Launchers = 4, MaxComboMultiplier = 4;

        public readonly LevelDefinition Level;
        public readonly int X, Y, Z, N;

        readonly sbyte[] cell;
        readonly bool[] exposed;
        readonly int[] remaining, exposedCount, rank;
        readonly MinHeap[] heaps;
        readonly List<AttackBlock>[] columns = new List<AttackBlock>[Columns];
        readonly AttackBlock?[] launchers = new AttackBlock?[Launchers];

        public int Moves { get; private set; }
        public int Score { get; private set; }
        public int MaxCombo { get; private set; }
        public int Rounds { get; private set; }
        public int TotalVoxels { get; }
        public int TotalRemaining { get; private set; }
        public int ColorCount => remaining.Length;

        public GameSession(LevelDefinition level)
        {
            Level = level ?? throw new ArgumentNullException(nameof(level));
            var g = level.Grid;
            X = g.X; Y = g.Y; Z = g.Z; N = X * Y * Z;
            cell = g.CopyCells();
            remaining = new int[g.PaletteHex.Length];
            exposedCount = new int[remaining.Length];
            for (int i = 0; i < N; i++) if (cell[i] >= 0) remaining[cell[i]]++;
            foreach (int r in remaining) TotalRemaining += r;
            TotalVoxels = TotalRemaining;
            for (int c = 0; c < Columns; c++) columns[c] = new List<AttackBlock>(c < level.Columns.Length ? level.Columns[c] : Array.Empty<AttackBlock>());
            rank = BuildPriority();
            heaps = new MinHeap[remaining.Length];
            for (int c = 0; c < heaps.Length; c++) heaps[c] = new MinHeap(rank);
            exposed = new bool[N];
            for (int i = 0; i < N; i++) if (cell[i] >= 0 && VoxelTopology.IsExposed(cell, X, Y, Z, i)) Expose(i);
        }

        // ------------------------------------------------------------------ queries
        public int CellColor(int i) => cell[i];
        public bool IsExposed(int i) => exposed[i];
        public int Remaining(int color) => remaining[color];
        public int ExposedCount(int color) => exposedCount[color];
        public AttackBlock? LauncherAt(int i) => launchers[i];
        public int ColumnLength(int col) => columns[col].Count;
        public AttackBlock? BlockAt(int col, int depth) => depth < columns[col].Count ? columns[col][depth] : (AttackBlock?)null;
        public AttackBlock? Top(int col) => BlockAt(col, 0);
        public bool IsWon => TotalRemaining == 0;
        public float Progress => TotalVoxels == 0 ? 1f : 1f - (float)TotalRemaining / TotalVoxels;
        public sbyte[] CellsSnapshot() => (sbyte[])cell.Clone();
        public int BlocksLeft { get { int n = 0; foreach (var c in columns) n += c.Count; return n; } }
        public bool BoardEmpty => BlocksLeft == 0;

        public int FreeLauncher()
        {
            for (int i = 0; i < Launchers; i++) if (!launchers[i].HasValue) return i;
            return -1;
        }

        public bool CanTap(int col) => !IsWon && col >= 0 && col < Columns && columns[col].Count > 0 && FreeLauncher() >= 0;

        public bool CanFire(int li)
        {
            var l = launchers[li];
            return l.HasValue && (remaining[l.Value.Color] == 0 || exposedCount[l.Value.Color] > 0);
        }

        public bool AnyCanFire()
        {
            for (int i = 0; i < Launchers; i++) if (CanFire(i)) return true;
            return false;
        }

        /// <summary>True when the launcher holds a block whose colour exists but is not reachable yet (it waits).</summary>
        public bool IsWaiting(int li) => launchers[li].HasValue && !CanFire(li);

        public FailReason GetFailReason()
        {
            if (IsWon) return FailReason.None;
            if (FreeLauncher() < 0 && !AnyCanFire()) return FailReason.Jammed;
            if (BoardEmpty && !AnyCanFire()) return FailReason.OutOfBlocks;
            return FailReason.None;
        }

        // ------------------------------------------------------------------ actions
        /// <summary>Moves the column's top block into the first free launcher; returns the launcher index or -1.</summary>
        public int Tap(int col)
        {
            if (!CanTap(col)) return -1;
            int li = FreeLauncher();
            launchers[li] = columns[col][0];
            columns[col].RemoveAt(0);
            Moves++;
            return li;
        }

        /// <summary>One firing round; null when nothing happened (all launchers empty or waiting).</summary>
        public RoundResult Step()
        {
            var res = new RoundResult();
            for (int li = 0; li < Launchers; li++)
            {
                if (!launchers[li].HasValue) continue;
                var l = launchers[li].Value;
                if (remaining[l.Color] == 0) { res.Wasted += l.Power; launchers[li] = null; res.Freed.Add(li); continue; }
                int v = Target(l.Color);
                if (v < 0) continue;
                Destroy(v);
                res.Shots.Add((li, v));
                l = new AttackBlock(l.Color, l.Power - 1);
                if (l.Power == 0) { launchers[li] = null; res.Freed.Add(li); }
                else launchers[li] = l;
            }
            if (res.Shots.Count == 0 && res.Freed.Count == 0) return null;
            Rounds++;
            res.Combo = res.Shots.Count;
            if (res.Combo > MaxCombo) MaxCombo = res.Combo;
            res.ScoreGained = res.Combo * 10 * Math.Max(1, res.Combo);
            Score += res.ScoreGained;
            if (res.Shots.Count > 0) Collapse(res.Dropped);
            PurgeFinished();
            return res;
        }

        /// <summary>Runs rounds until a launcher is free or nothing can happen (witness replay granularity).</summary>
        public void Settle()
        {
            while (FreeLauncher() < 0 && !IsWon && Step() != null) { }
        }

        public void ResolveAll()
        {
            while (!IsWon && Step() != null) { }
        }

        /// <summary>Voxels the next projectiles of this colour would hit (current exposure, in firing order).</summary>
        public List<int> PreviewTargets(int color, int count)
        {
            var list = new List<int>();
            if (color < 0 || color >= remaining.Length) return list;
            for (int i = 0; i < N; i++) if (exposed[i] && cell[i] == color) list.Add(i);
            list.Sort((u, v) => rank[u].CompareTo(rank[v]));
            if (list.Count > count) list.RemoveRange(count, list.Count - count);
            return list;
        }

        // ------------------------------------------------------------------ boosters
        public List<int> ApplyHammer(int voxelIndex, int count)
        {
            var destroyed = new List<int>();
            if (voxelIndex < 0 || voxelIndex >= N || cell[voxelIndex] < 0 || count <= 0) return destroyed;
            var frontier = new List<int> { voxelIndex };
            var seen = new HashSet<int> { voxelIndex };
            while (frontier.Count > 0 && destroyed.Count < count)
            {
                int pick = 0;
                for (int k = 0; k < frontier.Count; k++) if (exposed[frontier[k]]) { pick = k; break; }
                int i = frontier[pick];
                frontier.RemoveAt(pick);
                if (cell[i] < 0) continue;
                int x = i % X, y = (i / X) % Y, z = i / (X * Y);
                Destroy(i);
                destroyed.Add(i);
                for (int d = 0; d < 6; d++)
                {
                    int a = x + VoxelTopology.DX[d], b = y + VoxelTopology.DY[d], c = z + VoxelTopology.DZ[d];
                    if (a < 0 || b < 0 || c < 0 || a >= X || b >= Y || c >= Z) continue;
                    int j = a + X * (b + Y * c);
                    if (cell[j] >= 0 && seen.Add(j)) frontier.Add(j);
                }
            }
            Collapse(destroyed);
            PurgeFinished();
            return destroyed;
        }

        public List<int> ApplyColorBomb(int color, int count)
        {
            var destroyed = new List<int>();
            if (color < 0 || color >= remaining.Length) return destroyed;
            while (destroyed.Count < count)
            {
                int v = Target(color);
                if (v < 0) break;
                Destroy(v);
                destroyed.Add(v);
            }
            Collapse(destroyed);
            PurgeFinished();
            return destroyed;
        }

        /// <summary>Shuffle: redistributes all column blocks; tries to put a reachable colour on top of a column.</summary>
        public bool ApplyShuffle(DeterministicRandom rng)
        {
            var pool = new List<AttackBlock>();
            foreach (var c in columns) pool.AddRange(c);
            if (pool.Count == 0) return false;
            for (int attempt = 0; attempt < 12; attempt++)
            {
                for (int i = pool.Count - 1; i > 0; i--) { int j = rng.Next(i + 1); var t = pool[i]; pool[i] = pool[j]; pool[j] = t; }
                bool ok = false;
                for (int k = 0; k < Math.Min(Columns, pool.Count) && !ok; k++) if (exposedCount[pool[k].Color] > 0) ok = true;
                if (ok) break;
            }
            foreach (var c in columns) c.Clear();
            for (int i = 0; i < pool.Count; i++) columns[i % Columns].Add(pool[i]);
            return true;
        }

        /// <summary>Eliminare: discards the block in a launcher (e.g. one that is waiting) and frees it.</summary>
        public bool ApplyEliminate(int launcher)
        {
            if (launcher < 0 || launcher >= Launchers || !launchers[launcher].HasValue) return false;
            launchers[launcher] = null;
            return true;
        }

        public int FirstWaitingLauncher()
        {
            for (int i = 0; i < Launchers; i++) if (IsWaiting(i)) return i;
            return -1;
        }

        // ------------------------------------------------------------------ internals
        void Expose(int i)
        {
            if (exposed[i]) return;
            exposed[i] = true;
            exposedCount[cell[i]]++;
            heaps[cell[i]].Push(i);
        }

        int Target(int color)
        {
            var h = heaps[color];
            while (h.Count > 0 && (cell[h.Peek()] != color || !exposed[h.Peek()])) h.Pop();
            return h.Count > 0 ? h.Peek() : -1;
        }

        void PurgeFinished()
        {
            foreach (var c in columns) c.RemoveAll(b => remaining[b.Color] == 0);
        }

        void Destroy(int i)
        {
            int c = cell[i];
            cell[i] = -1;
            if (exposed[i]) { exposed[i] = false; exposedCount[c]--; }
            remaining[c]--;
            TotalRemaining--;
            int x = i % X, y = (i / X) % Y, z = i / (X * Y);
            for (int d = 0; d < 6; d++)
            {
                int a = x + VoxelTopology.DX[d], b = y + VoxelTopology.DY[d], e = z + VoxelTopology.DZ[d];
                if (a < 0 || b < 0 || e < 0 || a >= X || b >= Y || e >= Z) continue;
                int j = a + X * (b + Y * e);
                if (cell[j] >= 0) Expose(j);
            }
        }

        int[] BuildPriority()
        {
            var ids = new List<int>();
            var keyY = new int[N];
            var keyD = new int[N];
            for (int i = 0; i < N; i++)
            {
                if (cell[i] < 0) continue;
                int x = i % X, y = (i / X) % Y, z = i / (X * Y);
                int dx = 2 * x - (X - 1), dz = 2 * z - (Z - 1);
                keyY[i] = y; keyD[i] = dx * dx + dz * dz;
                ids.Add(i);
            }
            ids.Sort((a, b) =>
            {
                int c = keyY[b].CompareTo(keyY[a]);
                if (c != 0) return c;
                c = keyD[b].CompareTo(keyD[a]);
                return c != 0 ? c : a.CompareTo(b);
            });
            var r = new int[N];
            for (int i = 0; i < N; i++) r[i] = int.MaxValue;
            for (int k = 0; k < ids.Count; k++) r[ids[k]] = k;
            return r;
        }

        int[] compBuf;
        readonly List<int> bfs = new List<int>(1024);

        /// <summary>Structural rule (mirrors rules.mjs collapse). Appends fallen voxels to <paramref name="dropped"/>.</summary>
        public int Collapse(List<int> dropped)
        {
            if (compBuf == null) compBuf = new int[N];
            for (int i = 0; i < N; i++) compBuf[i] = -1;
            var sizes = new List<int>(); var grounded = new List<bool>(); var members = new List<int[]>();
            int XY = X * Y;
            for (int i = 0; i < N; i++)
            {
                if (cell[i] < 0 || compBuf[i] >= 0) continue;
                int id = sizes.Count; bool g = false;
                bfs.Clear(); bfs.Add(i); compBuf[i] = id;
                for (int h = 0; h < bfs.Count; h++)
                {
                    int j = bfs[h], x = j % X, y = (j / X) % Y, z = j / XY;
                    if (y == 0) g = true;
                    if (x > 0) Visit(j - 1, id);
                    if (x < X - 1) Visit(j + 1, id);
                    if (y > 0) Visit(j - X, id);
                    if (y < Y - 1) Visit(j + X, id);
                    if (z > 0) Visit(j - XY, id);
                    if (z < Z - 1) Visit(j + XY, id);
                }
                sizes.Add(bfs.Count); grounded.Add(g); members.Add(bfs.ToArray());
            }
            if (sizes.Count <= 1) return 0;
            int main = 0;
            for (int k = 1; k < sizes.Count; k++) if (sizes[k] > sizes[main]) main = k;
            int n = 0;
            for (int k = 0; k < sizes.Count; k++)
            {
                if (k == main || grounded[k]) continue;
                var list = members[k];
                Array.Sort(list);
                foreach (int i in list) { Destroy(i); dropped?.Add(i); n++; }
            }
            return n;
        }

        void Visit(int j, int id)
        {
            if (cell[j] >= 0 && compBuf[j] < 0) { compBuf[j] = id; bfs.Add(j); }
        }

        // ------------------------------------------------------------------ persistence
        public Dictionary<string, object> ToSnapshot()
        {
            var cells = new char[N];
            for (int i = 0; i < N; i++) cells[i] = cell[i] < 0 ? '.' : (cell[i] < 10 ? (char)('0' + cell[i]) : (char)('a' + cell[i] - 10));
            var cols = new List<object>();
            foreach (var c in columns) { var l = new List<object>(); foreach (var b in c) l.Add(Pair(b)); cols.Add(l); }
            var ls = new List<object>();
            foreach (var l in launchers) ls.Add(l.HasValue ? Pair(l.Value) : null);
            return new Dictionary<string, object>
            {
                { "level", (double)Level.Id }, { "rules", 2.0 }, { "cells", new string(cells) }, { "columns", cols }, { "launchers", ls },
                { "moves", (double)Moves }, { "score", (double)Score }, { "maxCombo", (double)MaxCombo },
            };
        }

        static List<object> Pair(AttackBlock b) => new List<object> { (double)b.Color, (double)b.Power };

        public static GameSession FromSnapshot(LevelDefinition level, Dictionary<string, object> snap)
        {
            if (snap == null || MiniJson.GetInt(snap, "level") != level.Id || MiniJson.GetInt(snap, "rules") != 2) return null;
            var s = new GameSession(level);
            var cells = VoxelGrid.Decode(MiniJson.GetString(snap, "cells", ""));
            if (cells.Length != s.N) return null;
            for (int i = 0; i < s.N; i++) if (s.cell[i] >= 0 && cells[i] < 0) s.Destroy(i);
            var cols = MiniJson.GetArr(snap, "columns");
            for (int c = 0; c < Columns; c++)
            {
                s.columns[c].Clear();
                var l = cols != null && c < cols.Count ? MiniJson.Arr(cols[c]) : null;
                if (l != null) foreach (var o in l) { var b = Read(o); if (b.HasValue) s.columns[c].Add(b.Value); }
            }
            var ls = MiniJson.GetArr(snap, "launchers");
            for (int i = 0; i < Launchers; i++) s.launchers[i] = ls != null && i < ls.Count ? Read(ls[i]) : null;
            s.Moves = MiniJson.GetInt(snap, "moves");
            s.Score = MiniJson.GetInt(snap, "score");
            s.MaxCombo = MiniJson.GetInt(snap, "maxCombo");
            return s;
        }

        static AttackBlock? Read(object o)
        {
            var l = MiniJson.Arr(o);
            if (l == null || l.Count != 2) return null;
            return new AttackBlock((int)(double)l[0], (int)(double)l[1]);
        }

        /// <summary>Binary min-heap of voxel indices ordered by removal priority.</summary>
        sealed class MinHeap
        {
            readonly List<int> a = new List<int>();
            readonly int[] r;
            public MinHeap(int[] rank) { r = rank; }
            public int Count => a.Count;
            public int Peek() => a[0];

            public void Push(int v)
            {
                a.Add(v);
                int i = a.Count - 1;
                while (i > 0)
                {
                    int p = (i - 1) >> 1;
                    if (r[a[p]] <= r[a[i]]) break;
                    (a[p], a[i]) = (a[i], a[p]);
                    i = p;
                }
            }

            public int Pop()
            {
                int top = a[0], last = a[a.Count - 1];
                a.RemoveAt(a.Count - 1);
                if (a.Count > 0)
                {
                    a[0] = last;
                    int i = 0;
                    while (true)
                    {
                        int l = 2 * i + 1, rr = l + 1, m = i;
                        if (l < a.Count && r[a[l]] < r[a[m]]) m = l;
                        if (rr < a.Count && r[a[rr]] < r[a[m]]) m = rr;
                        if (m == i) break;
                        (a[m], a[i]) = (a[i], a[m]);
                        i = m;
                    }
                }
                return top;
            }
        }
    }
}
