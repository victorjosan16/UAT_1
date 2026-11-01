using System;
using System.Collections.Generic;

namespace VoxelBloom.Core
{
    public enum FailReason { None, QueueFull, OutOfMoves, OutOfBlocks }

    public sealed class AttackResult
    {
        public AttackBlock Attack;
        public int QueueIndex;
        public readonly List<int> Destroyed = new List<int>();
        /// <summary>Voxels of detached pieces that fell after this attack (count as destroyed).</summary>
        public readonly List<int> Dropped = new List<int>();
        public bool Wasted;
        public bool ColorCleared;
        public int Combo;
        public int ScoreGained;
        /// <summary>Board slots refilled because the colour was cleared.</summary>
        public readonly List<int> RefilledSlots = new List<int>();
        public bool IsFullPower => !Wasted && Destroyed.Count == Attack.Power;
    }

    /// <summary>
    /// Authoritative, engine-free game rules for one level. Mirrors tools/rules.mjs exactly so that
    /// build-time solver results (level witness) replay identically at runtime.
    /// Rules:
    ///  * Tap a board block: it moves into the FIFO attack queue (max QueueSize) and the slot refills from the deck.
    ///  * ResolveNext fires the first queued attack whose colour has exposed voxels and destroys up to Power of them
    ///    (exposure is evaluated when firing and recomputed afterwards). Attacks never hit other colours.
    ///  * Attacks for a colour that is fully destroyed are discarded. Board blocks of a cleared colour are replaced.
    ///  * Consecutive full-power attacks build a combo (x2..x5 score multiplier).
    ///  * Win: all voxels destroyed. Fail: queue full and stuck, move limit reached, or no blocks left.
    /// </summary>
    public sealed class GameSession
    {
        public const int BoardCols = 5, BoardRows = 4, BoardSize = 20, MaxComboMultiplier = 5;

        public readonly LevelDefinition Level;
        public readonly int X, Y, Z, N;
        public int QueueSize { get; }
        public int MoveLimit { get; private set; }

        readonly sbyte[] cell;
        readonly bool[] exposed;
        readonly int[] remaining;
        readonly int[] exposedCount;
        readonly int[] rank;
        readonly List<AttackBlock> deck;
        readonly AttackBlock?[] board = new AttackBlock?[BoardSize];
        readonly List<AttackBlock> queue = new List<AttackBlock>();
        readonly List<int> scratch = new List<int>(256);

        public int DeckIndex { get; private set; }
        public int Moves { get; private set; }
        public int Combo { get; private set; }
        public int MaxCombo { get; private set; }
        public int Score { get; private set; }
        public int TotalVoxels { get; }
        public int TotalRemaining { get; private set; }
        public int ColorCount => remaining.Length;

        public event Action<int> VoxelDestroyed;

        public GameSession(LevelDefinition level)
        {
            Level = level ?? throw new ArgumentNullException(nameof(level));
            var g = level.Grid;
            X = g.X; Y = g.Y; Z = g.Z; N = X * Y * Z;
            QueueSize = level.QueueSize;
            MoveLimit = level.MoveLimit > 0 ? level.MoveLimit : 1000000;
            cell = g.CopyCells();
            remaining = new int[g.PaletteHex.Length];
            exposedCount = new int[remaining.Length];
            for (int i = 0; i < N; i++) if (cell[i] >= 0) remaining[cell[i]]++;
            foreach (int r in remaining) TotalRemaining += r;
            TotalVoxels = TotalRemaining;
            deck = new List<AttackBlock>(level.Deck);
            for (int s = 0; s < BoardSize; s++) board[s] = DrawFromDeck();
            rank = BuildPriority();
            exposed = new bool[N];
            for (int i = 0; i < N; i++)
                if (cell[i] >= 0 && VoxelTopology.IsExposed(cell, X, Y, Z, i)) { exposed[i] = true; exposedCount[cell[i]]++; }
        }

        // ------------------------------------------------------------------ queries
        public int CellColor(int i) => cell[i];
        public bool IsSolid(int i) => cell[i] >= 0;
        public bool IsExposed(int i) => exposed[i];
        public int Remaining(int color) => remaining[color];
        public int ExposedCount(int color) => exposedCount[color];
        public AttackBlock? BoardAt(int slot) => board[slot];
        public IReadOnlyList<AttackBlock> Queue => queue;
        public int DeckRemaining => deck.Count - DeckIndex;
        public bool IsWon => TotalRemaining == 0;
        public float Progress => TotalVoxels == 0 ? 1f : 1f - (float)TotalRemaining / TotalVoxels;
        public int MovesLeft => Math.Max(0, MoveLimit - Moves);
        public sbyte[] CellsSnapshot() => (sbyte[])cell.Clone();

        public bool BoardEmpty
        {
            get { for (int s = 0; s < BoardSize; s++) if (board[s].HasValue) return false; return true; }
        }

        /// <summary>Voxels an attack of this colour/power would remove right now (same order as ResolveNext).</summary>
        public List<int> PreviewTargets(int color, int power)
        {
            var list = new List<int>();
            if (color < 0 || color >= remaining.Length) return list;
            for (int i = 0; i < N; i++) if (exposed[i] && cell[i] == color) list.Add(i);
            list.Sort((u, v) => rank[u].CompareTo(rank[v]));
            if (list.Count > power) list.RemoveRange(power, list.Count - power);
            return list;
        }

        public bool CanTap(int slot)
            => slot >= 0 && slot < BoardSize && !IsWon && Moves < MoveLimit && board[slot].HasValue && queue.Count < QueueSize;

        /// <summary>True when at least one board block could fire right now.</summary>
        public bool HasPlayableBlock()
        {
            for (int s = 0; s < BoardSize; s++)
                if (board[s].HasValue && exposedCount[board[s].Value.Color] > 0) return true;
            return false;
        }

        /// <summary>Only meaningful once ResolveNext() returns null (queue settled).</summary>
        public FailReason GetFailReason()
        {
            if (IsWon) return FailReason.None;
            if (queue.Count >= QueueSize) return FailReason.QueueFull;
            if (Moves >= MoveLimit) return FailReason.OutOfMoves;
            if (BoardEmpty) return FailReason.OutOfBlocks;
            return FailReason.None;
        }

        // ------------------------------------------------------------------ actions
        public bool Tap(int slot)
        {
            if (!CanTap(slot)) return false;
            queue.Add(board[slot].Value);
            board[slot] = DrawFromDeck();
            Moves++;
            return true;
        }

        /// <summary>Fires the next attack that has a target, or returns null when nothing can fire.</summary>
        public AttackResult ResolveNext()
        {
            for (int q = 0; q < queue.Count; q++)
            {
                if (remaining[queue[q].Color] != 0) continue;
                var wasted = new AttackResult { Attack = queue[q], QueueIndex = q, Wasted = true, Combo = 0 };
                queue.RemoveAt(q);
                Combo = 0;
                return wasted;
            }
            int qi = -1;
            for (int q = 0; q < queue.Count; q++) if (exposedCount[queue[q].Color] > 0) { qi = q; break; }
            if (qi < 0) return null;

            var a = queue[qi];
            queue.RemoveAt(qi);
            var res = new AttackResult { Attack = a, QueueIndex = qi };

            // single wave: voxels accessible at the moment of firing, by removal priority
            scratch.Clear();
            for (int i = 0; i < N; i++) if (exposed[i] && cell[i] == a.Color) scratch.Add(i);
            scratch.Sort((u, v) => rank[u].CompareTo(rank[v]));
            int take = Math.Min(a.Power, scratch.Count);
            for (int k = 0; k < take; k++) { Destroy(scratch[k]); res.Destroyed.Add(scratch[k]); }

            bool full = res.Destroyed.Count == a.Power;
            Combo = full ? Combo + 1 : 0;
            if (Combo > MaxCombo) MaxCombo = Combo;
            res.Combo = Combo;
            res.ScoreGained = res.Destroyed.Count * 10 * ComboMultiplier(Combo);
            Score += res.ScoreGained;

            Collapse(res.Dropped);
            res.ColorCleared = remaining[a.Color] == 0;
            PurgeClearedColors(res.RefilledSlots);
            return res;
        }

        public List<AttackResult> ResolveAll()
        {
            var list = new List<AttackResult>();
            AttackResult r;
            while ((r = ResolveNext()) != null)
            {
                list.Add(r);
                if (IsWon) break;
            }
            return list;
        }

        public static int ComboMultiplier(int combo) => Math.Min(Math.Max(combo, 1), MaxComboMultiplier);

        // ------------------------------------------------------------------ boosters
        /// <summary>Hammer: destroys the tapped voxel and up to count-1 connected voxels, any colour, exposed first.</summary>
        public List<int> ApplyHammer(int voxelIndex, int count)
        {
            var destroyed = new List<int>();
            if (voxelIndex < 0 || voxelIndex >= N || cell[voxelIndex] < 0 || count <= 0) return destroyed;
            var frontier = new List<int> { voxelIndex };
            var seen = new HashSet<int> { voxelIndex };
            while (frontier.Count > 0 && destroyed.Count < count)
            {
                // exposed voxels first, then by distance order of discovery
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
            PurgeClearedColors(null);
            return destroyed;
        }

        /// <summary>Colour Bomb: destroys up to count voxels of one colour, peeling layer after layer.</summary>
        public List<int> ApplyColorBomb(int color, int count)
        {
            var destroyed = new List<int>();
            if (color < 0 || color >= remaining.Length) return destroyed;
            while (destroyed.Count < count && exposedCount[color] > 0)
            {
                scratch.Clear();
                for (int i = 0; i < N; i++) if (exposed[i] && cell[i] == color) scratch.Add(i);
                scratch.Sort((u, v) => rank[u].CompareTo(rank[v]));
                int take = Math.Min(count - destroyed.Count, scratch.Count);
                for (int k = 0; k < take; k++) { Destroy(scratch[k]); destroyed.Add(scratch[k]); }
            }
            Collapse(destroyed);
            PurgeClearedColors(null);
            return destroyed;
        }

        /// <summary>Shuffle: mixes board blocks with the remaining deck; keeps a playable block on the board if possible.</summary>
        public bool ApplyShuffle(DeterministicRandom rng)
        {
            var pool = new List<AttackBlock>();
            for (int s = 0; s < BoardSize; s++) if (board[s].HasValue) pool.Add(board[s].Value);
            int boardCount = pool.Count;
            for (int i = DeckIndex; i < deck.Count; i++) if (remaining[deck[i].Color] > 0) pool.Add(deck[i]);
            if (pool.Count == 0) return false;

            bool anyPlayable = false;
            foreach (var b in pool) if (exposedCount[b.Color] > 0) { anyPlayable = true; break; }

            for (int attempt = 0; attempt < 12; attempt++)
            {
                for (int i = pool.Count - 1; i > 0; i--)
                {
                    int j = rng.Next(i + 1);
                    var t = pool[i]; pool[i] = pool[j]; pool[j] = t;
                }
                bool ok = !anyPlayable;
                for (int k = 0; k < boardCount && !ok; k++) if (exposedCount[pool[k].Color] > 0) ok = true;
                if (ok) break;
            }
            deck.RemoveRange(DeckIndex, deck.Count - DeckIndex);
            int p = 0;
            for (int s = 0; s < BoardSize; s++) board[s] = p < boardCount ? pool[p++] : (AttackBlock?)null;
            for (; p < pool.Count; p++) deck.Add(pool[p]);
            return true;
        }

        /// <summary>Eliminare: removes a queued attack (e.g. one that is stuck) and frees its slot.</summary>
        public bool ApplyEliminate(int queueIndex)
        {
            if (queueIndex < 0 || queueIndex >= queue.Count) return false;
            queue.RemoveAt(queueIndex);
            return true;
        }

        /// <summary>Index of the first queued attack that currently has no target, or -1.</summary>
        public int FirstStuckQueueIndex()
        {
            for (int q = 0; q < queue.Count; q++) if (exposedCount[queue[q].Color] == 0) return q;
            return -1;
        }

        // ------------------------------------------------------------------ internals
        AttackBlock? DrawFromDeck()
        {
            while (DeckIndex < deck.Count)
            {
                var b = deck[DeckIndex++];
                if (remaining == null || remaining[b.Color] > 0) return b;
            }
            return null;
        }

        void ClearColorFromBoard(int color, List<int> refilled)
        {
            for (int s = 0; s < BoardSize; s++)
                if (board[s].HasValue && board[s].Value.Color == color) { board[s] = DrawFromDeck(); refilled?.Add(s); }
        }

        /// <summary>Board blocks of finished colours are replaced (colours ascending, slots ascending), as in rules.mjs.</summary>
        void PurgeClearedColors(List<int> refilled)
        {
            for (int c = 0; c < remaining.Length; c++) if (remaining[c] == 0) ClearColorFromBoard(c, refilled);
        }

        int[] compBuf;
        readonly List<int> bfs = new List<int>(1024);

        /// <summary>
        /// Structural rule (mirrors rules.mjs collapse): the largest connected piece stays, plus every piece touching
        /// y = 0; any other detached piece falls and counts as destroyed. Ties: piece containing the lowest index.
        /// Appends fallen voxel indices (ascending per piece) to <paramref name="dropped"/>.
        /// </summary>
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
                if (cell[j] >= 0 && !exposed[j]) { exposed[j] = true; exposedCount[cell[j]]++; }
            }
            VoxelDestroyed?.Invoke(i);
        }

        /// <summary>Removal priority: highest y first, then farthest from the vertical axis, then lowest index.</summary>
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
                keyY[i] = y;
                keyD[i] = dx * dx + dz * dz; // == round(((x-cx)^2 + (z-cz)^2) * 4) in rules.mjs
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
            for (int i = 0; i < N; i++) r[i] = -1;
            for (int k = 0; k < ids.Count; k++) r[ids[k]] = k;
            return r;
        }

        // ------------------------------------------------------------------ persistence of an in-progress level
        public Dictionary<string, object> ToSnapshot()
        {
            var cells = new char[N];
            for (int i = 0; i < N; i++) cells[i] = cell[i] < 0 ? '.' : (cell[i] < 10 ? (char)('0' + cell[i]) : (char)('a' + cell[i] - 10));
            var b = new List<object>();
            for (int s = 0; s < BoardSize; s++) b.Add(board[s].HasValue ? (object)new List<object> { (double)board[s].Value.Color, (double)board[s].Value.Power } : null);
            var q = new List<object>();
            foreach (var a in queue) q.Add(new List<object> { (double)a.Color, (double)a.Power });
            var dk = new List<object>();
            foreach (var a in deck) dk.Add(new List<object> { (double)a.Color, (double)a.Power });
            return new Dictionary<string, object>
            {
                { "level", (double)Level.Id }, { "cells", new string(cells) }, { "board", b }, { "queue", q }, { "deck", dk },
                { "deckIndex", (double)DeckIndex }, { "moves", (double)Moves }, { "combo", (double)Combo },
                { "maxCombo", (double)MaxCombo }, { "score", (double)Score },
            };
        }

        public static GameSession FromSnapshot(LevelDefinition level, Dictionary<string, object> snap)
        {
            if (snap == null || MiniJson.GetInt(snap, "level") != level.Id) return null;
            var s = new GameSession(level);
            var cells = VoxelGrid.Decode(MiniJson.GetString(snap, "cells", ""));
            if (cells.Length != s.N) return null;
            for (int i = 0; i < s.N; i++) if (s.cell[i] >= 0 && cells[i] < 0) s.Destroy(i);
            s.deck.Clear();
            foreach (var o in MiniJson.GetArr(snap, "deck") ?? new List<object>()) s.deck.Add(ReadBlock(o).Value);
            var b = MiniJson.GetArr(snap, "board");
            for (int k = 0; k < BoardSize; k++) s.board[k] = b != null && k < b.Count ? ReadBlock(b[k]) : null;
            s.queue.Clear();
            foreach (var o in MiniJson.GetArr(snap, "queue") ?? new List<object>()) s.queue.Add(ReadBlock(o).Value);
            s.DeckIndex = MiniJson.GetInt(snap, "deckIndex");
            s.Moves = MiniJson.GetInt(snap, "moves");
            s.Combo = MiniJson.GetInt(snap, "combo");
            s.MaxCombo = MiniJson.GetInt(snap, "maxCombo");
            s.Score = MiniJson.GetInt(snap, "score");
            return s;
        }

        static AttackBlock? ReadBlock(object o)
        {
            var l = MiniJson.Arr(o);
            if (l == null || l.Count != 2) return null;
            return new AttackBlock((int)(double)l[0], (int)(double)l[1]);
        }
    }
}
