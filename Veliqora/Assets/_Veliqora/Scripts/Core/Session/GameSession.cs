using System;
using System.Collections.Generic;
using Veliqora.Core.Levels;
using Veliqora.Core.Puzzle;
using Veliqora.Core.Solver;

namespace Veliqora.Core.Session
{
    public enum TapKind { Ignored, Selected, Deselected, Reselected, Moved, Invalid }

    public sealed class TapResult
    {
        public TapKind Kind;
        public int Index;
        public int PreviousSelection = -1;
        public MoveResult Move;           // for Moved / Reselected / Invalid
        public bool CompletedBottle;      // destination became full + monochrome
        public bool Won;
        public override string ToString() => Kind + " #" + Index + (Move != null ? " " + Move : "");
    }

    public sealed class UndoResult
    {
        public Move UndoneMove;
        public GameState Before;
        public GameState After;
    }

    public enum HintSource { None, StoredSolution, Solver, AlreadySolved, NotFound }

    public sealed class HintResult
    {
        public HintSource Source;
        public Move Move;
        public bool HasMove => Source == HintSource.StoredSolution || Source == HintSource.Solver;
        public string Message;
    }

    /// <summary>
    /// Model-first gameplay session for one level attempt. Owns state transitions; the presentation layer only
    /// renders committed results. Input must be blocked by the controller (IsBusy) while animations play.
    /// </summary>
    public sealed class GameSession
    {
        public const int MaxUndoHistory = 200;
        /// <summary>Empty bottles that can be added per attempt (Add-bottle booster and/or rewarded ad).</summary>
        public const int MaxExtraBottles = 2;

        private readonly List<GameState> _history = new List<GameState>();   // states BEFORE each committed move
        private readonly List<Move> _moves = new List<Move>();

        public LevelDefinition Level { get; }
        public string AttemptId { get; private set; }
        public GameState Initial { get; private set; }
        public GameState Current { get; private set; }
        public int Selected { get; private set; } = -1;
        public bool IsBusy { get; set; }
        public bool IsComplete { get; private set; }
        public int ExtraBottlesAdded { get; private set; }
        public int ShuffleCount { get; private set; }
        /// <summary>Benchmark used for stars (level par, or the shuffled layout's verified solution length).</summary>
        public int ParMoves { get; private set; }
        public int MoveCount => _moves.Count;
        public int TotalPours { get; private set; }
        public int UndosUsed { get; private set; }
        public IReadOnlyList<Move> Moves => _moves;
        public bool CanUndo => _history.Count > 0 && !IsComplete;

        private int[][] _currentSolution; // verified solution from Initial (null after extra bottle changes nothing; still valid)

        public GameSession(LevelDefinition level, string attemptId)
        {
            Level = level ?? throw new ArgumentNullException(nameof(level));
            AttemptId = string.IsNullOrEmpty(attemptId) ? Guid.NewGuid().ToString("N") : attemptId;
            Initial = level.CreateInitialState();
            Current = Initial;
            ParMoves = level.ParMoves;
            _currentSolution = level.Solution;
        }

        // ---------------- input ----------------

        public TapResult Tap(int index)
        {
            var r = new TapResult { Index = index, PreviousSelection = Selected };
            if (IsBusy || IsComplete || index < 0 || index >= Current.BottleCount) { r.Kind = TapKind.Ignored; return r; }

            if (Current[index].IsLocked)
            {
                // Corked / ad-locked bottles can't be selected or poured into: subtle feedback, state unchanged.
                r.Kind = TapKind.Invalid;
                r.Move = MoveResult.Illegal(MoveRejection.Locked, Selected >= 0 ? Selected : index, index, Current);
                Selected = -1;
                return r;
            }

            if (Selected < 0)
            {
                if (Current[index].IsEmpty)
                {
                    r.Kind = TapKind.Invalid;
                    r.Move = MoveResult.Illegal(MoveRejection.SourceEmpty, index, index, Current);
                    return r;
                }
                Selected = index;
                r.Kind = TapKind.Selected;
                return r;
            }

            if (Selected == index)
            {
                Selected = -1;
                r.Kind = TapKind.Deselected;
                return r;
            }

            var mr = PuzzleRules.Evaluate(Current, Selected, index);
            if (mr.IsLegal)
            {
                Commit(mr);
                Selected = -1;
                r.Kind = TapKind.Moved;
                r.Move = mr;
                r.CompletedBottle = mr.After.IsBottleComplete(index);
                r.Won = IsComplete;
                return r;
            }

            r.Move = mr;
            // Predictable re-selection: tapping another non-empty bottle that can't receive the pour switches the
            // selection to it (with an "invalid" cue on the attempted pour). Tapping an empty/full target just
            // clears the selection with feedback.
            if (!Current[index].IsEmpty && mr.Rejection == MoveRejection.ColorMismatch)
            {
                Selected = index;
                r.Kind = TapKind.Reselected;
            }
            else
            {
                Selected = -1;
                r.Kind = TapKind.Invalid;
            }
            return r;
        }

        /// <summary>Direct move request (used by hints/tests). Applies only if legal.</summary>
        public MoveResult TryMove(int from, int to)
        {
            if (IsBusy || IsComplete) return MoveResult.Illegal(MoveRejection.OutOfRange, from, to, Current);
            var mr = PuzzleRules.Evaluate(Current, from, to);
            if (mr.IsLegal) { Commit(mr); Selected = -1; }
            return mr;
        }

        public void ClearSelection() => Selected = -1;

        private void Commit(MoveResult mr)
        {
            _history.Add(Current);
            _moves.Add(mr.Move);
            if (_history.Count > MaxUndoHistory) { _history.RemoveAt(0); }
            Current = mr.After;
            TotalPours++;
            IsComplete = PuzzleRules.IsWon(Current);
        }

        // ---------------- boosters ----------------

        /// <summary>Reverts the last committed move from its recorded snapshot (handles multi-unit pours exactly).</summary>
        public UndoResult Undo()
        {
            if (!CanUndo || IsBusy) return null;
            var before = Current;
            var prev = _history[_history.Count - 1];
            var move = _moves[_moves.Count - 1];
            _history.RemoveAt(_history.Count - 1);
            _moves.RemoveAt(_moves.Count - 1);
            Current = prev;
            Selected = -1;
            UndosUsed++;
            return new UndoResult { UndoneMove = move, Before = before, After = prev };
        }

        public bool CanAddExtraBottle => !IsComplete && ExtraBottlesAdded < MaxExtraBottles;

        /// <summary>Adds an empty temporary bottle for this attempt. Kept across restarts of the same attempt.</summary>
        public bool AddExtraBottle()
        {
            if (!CanAddExtraBottle || IsBusy) return false;
            var empty = BottleState.Empty(Level.ExtraCapacity);
            Current = Current.AddBottle(empty);
            Initial = Initial.AddBottle(empty);
            // History snapshots gain the bottle too, so undo never removes it.
            for (int i = 0; i < _history.Count; i++) _history[i] = _history[i].AddBottle(empty);
            ExtraBottlesAdded++;
            Selected = -1;
            return true;
        }

        /// <summary>Restarts the current puzzle (keeps extra bottles and any shuffled layout).</summary>
        public void Restart()
        {
            Current = Initial;
            _history.Clear();
            _moves.Clear();
            Selected = -1;
            IsComplete = false;
            IsBusy = false;
        }

        /// <summary>Replaces the layout with a verified-solvable shuffle (see LevelGenerator.ShuffleLayout) and restarts.</summary>
        public void ApplyShuffle(LevelGenerator.ShuffleResult shuffled)
        {
            if (shuffled == null) throw new ArgumentNullException(nameof(shuffled));
            if (shuffled.State.BottleCount != Initial.BottleCount) throw new ArgumentException("Shuffle changed the bottle count");
            Initial = shuffled.State;
            ShuffleCount++;
            ParMoves = shuffled.Solve.Length;
            _currentSolution = LevelDefinition.EncodeMoves(shuffled.Solve.Moves);
            Restart();
        }

        // ---------------- rewarded bottle ----------------

        public bool IsRewardBottle(int index) => index >= 0 && index < Current.BottleCount && Current[index].RewardLocked;

        /// <summary>
        /// Unlocks an ad-locked bottle after a CONFIRMED rewarded ad. The unlock is applied to the initial layout and
        /// every undo snapshot too, so it persists through undo, restart and save/load.
        /// </summary>
        public bool UnlockRewardBottle(int index)
        {
            if (!IsRewardBottle(index) || IsBusy) return false;
            Current = Current.WithBottle(index, Current[index].WithRewardLock(false));
            if (index < Initial.BottleCount && Initial[index].RewardLocked) Initial = Initial.WithBottle(index, Initial[index].WithRewardLock(false));
            for (int i = 0; i < _history.Count; i++)
                if (index < _history[i].BottleCount && _history[i][index].RewardLocked)
                    _history[i] = _history[i].WithBottle(index, _history[i][index].WithRewardLock(false));
            Selected = -1;
            return true;
        }

        // ---------------- remove top color ----------------

        private readonly List<int> _removedColors = new List<int>();
        public IReadOnlyList<int> RemovedColors => _removedColors;

        /// <summary>A color can be removed when it is the (visible) top color of an unlocked, incomplete bottle.</summary>
        public bool CanRemoveColorAt(int bottle)
        {
            if (IsComplete || IsBusy || bottle < 0 || bottle >= Current.BottleCount) return false;
            var b = Current[bottle];
            return !b.IsEmpty && !b.IsLocked && !Current.IsBottleComplete(bottle);
        }

        /// <summary>
        /// Computes (without committing) the boards after removing every unit of the top color of
        /// <paramref name="bottle"/> from all unlocked bottles. The caller verifies solvability before committing.
        /// </summary>
        public bool PreviewRemoveColor(int bottle, out int color, out GameState current, out GameState initial)
        {
            color = 0; current = null; initial = null;
            if (!CanRemoveColorAt(bottle)) return false;
            color = Current[bottle].TopColor;
            current = PuzzleRules.ResolveLocks(RemoveFrom(Current, color));
            initial = PuzzleRules.ResolveLocks(RemoveFrom(Initial, color).WithMoves(0));
            return true;
        }

        private static GameState RemoveFrom(GameState s, int color)
        {
            var bottles = new BottleState[s.BottleCount];
            for (int i = 0; i < s.BottleCount; i++) bottles[i] = s[i].IsLocked ? s[i] : s[i].RemoveColor(color);
            return s.WithAllBottles(bottles);
        }

        /// <summary>Commits a verified removal. Undo history is cleared (the booster's effect is permanent for this attempt).</summary>
        public void CommitRemoveColor(int color, GameState current, GameState initial)
        {
            Current = current;
            Initial = initial;
            _history.Clear();
            _prefixMoves = _moves.Count;
            _removedColors.Add(color);
            _currentSolution = null; // the stored solution no longer applies; hints use the live solver
            Selected = -1;
            IsComplete = PuzzleRules.IsWon(Current);
        }

        // ---------------- hint ----------------

        /// <summary>
        /// Recommends the next move. Uses the stored verified solution while the player follows it exactly;
        /// otherwise runs the bounded solver from the current state. Never fabricates a hint.
        /// </summary>
        public HintResult GetHint(SolverOptions options = null)
        {
            if (PuzzleRules.IsWon(Current)) return new HintResult { Source = HintSource.AlreadySolved, Message = "Already solved." };

            if (_currentSolution != null && _moves.Count < _currentSolution.Length && FollowsStoredSolution())
            {
                var s = _currentSolution[_moves.Count];
                var mr = PuzzleRules.Evaluate(Current, s[0], s[1]);
                if (mr.IsLegal) return new HintResult { Source = HintSource.StoredSolution, Move = mr.Move };
            }

            var result = PuzzleSolver.Solve(Current, options ?? SolverOptions.Hint);
            if (result.Status == SolveStatus.Solved && result.Moves.Count > 0)
                return new HintResult { Source = HintSource.Solver, Move = result.Moves[0] };
            if (result.Status == SolveStatus.Unsolvable)
                return new HintResult { Source = HintSource.NotFound, Message = "No solution from here. Try Undo or Restart." };
            return new HintResult { Source = HintSource.NotFound, Message = "Couldn't find a hint quickly. Try Undo, or Restart the level." };
        }

        private bool FollowsStoredSolution()
        {
            for (int i = 0; i < _moves.Count; i++)
            {
                var s = _currentSolution[i];
                if (_moves[i].From != s[0] || _moves[i].To != s[1]) return false;
            }
            return true;
        }

        public int CurrentStars => StarRating.Evaluate(MoveCount, ParMoves);

        // ---------------- persistence ----------------

        public SessionSnapshot ToSnapshot(int maxHistory = 30)
        {
            var snap = new SessionSnapshot
            {
                LevelNumber = Level.Number,
                AttemptId = AttemptId,
                Capacity = Level.Capacity,
                Initial = Initial,
                Current = Current,
                ExtraBottlesAdded = ExtraBottlesAdded,
                ShuffleCount = ShuffleCount,
                ParMoves = ParMoves,
                TotalPours = TotalPours,
                UndosUsed = UndosUsed,
                Solution = _currentSolution,
            };
            snap.RemovedColors.AddRange(_removedColors);
            int movesInWindow = _history.Count;
            int start = Math.Max(0, movesInWindow - maxHistory);
            int offset = _moves.Count - _history.Count; // moves made before the undo window (not undoable)
            for (int i = start; i < _history.Count; i++)
            {
                snap.History.Add(_history[i]);
                var m = _moves[offset + i];
                snap.Moves.Add(new[] { m.From, m.To, m.Amount, m.Color });
            }
            snap.MovesBeforeHistory = offset + start;
            return snap;
        }

        /// <summary>Restores an in-progress attempt. Returns null if the snapshot doesn't match the level (content changed / corrupt).</summary>
        public static GameSession FromSnapshot(LevelDefinition level, SessionSnapshot snap)
        {
            if (snap == null || snap.LevelNumber != level.Number || snap.Capacity != level.Capacity) return null;
            if (!snap.IsConsistentWith(level)) return null;
            try
            {
                var s = new GameSession(level, snap.AttemptId)
                {
                    Initial = snap.Initial,
                    Current = snap.Current,
                    ExtraBottlesAdded = snap.ExtraBottlesAdded,
                    ShuffleCount = snap.ShuffleCount,
                    ParMoves = snap.ParMoves > 0 ? snap.ParMoves : level.ParMoves,
                    TotalPours = snap.TotalPours,
                    UndosUsed = snap.UndosUsed,
                    _currentSolution = snap.RemovedColors.Count > 0 ? null : (snap.Solution ?? level.Solution),
                };
                s._removedColors.AddRange(snap.RemovedColors);
                // Moves made before the saved history window still count toward the move counter but can't be undone.
                for (int i = 0; i < snap.MovesBeforeHistory; i++) s._moves.Add(new Move(-1, -1, 0, 0));
                for (int i = 0; i < snap.History.Count; i++)
                {
                    s._history.Add(snap.History[i]);
                    var m = snap.Moves[i];
                    s._moves.Add(new Move(m[0], m[1], m[2], m[3]));
                }
                s._prefixMoves = snap.MovesBeforeHistory;
                s.IsComplete = PuzzleRules.IsWon(s.Current);
                return s;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        private int _prefixMoves;
        /// <summary>Moves that were made before the persisted undo window (counted, not undoable).</summary>
        public int NonUndoableMoves => _prefixMoves;
    }
}
