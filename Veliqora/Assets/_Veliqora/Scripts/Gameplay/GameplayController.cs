using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Veliqora.Core.Economy;
using Veliqora.Core.Levels;
using Veliqora.Core.Puzzle;
using Veliqora.Core.Save;
using Veliqora.Core.Session;
using Veliqora.Core.Solver;
using Veliqora.Infrastructure;
using Veliqora.Presentation;
using Veliqora.UI;

namespace Veliqora.Gameplay
{
    /// <summary>Outcome handed to the LevelComplete screen.</summary>
    public sealed class LevelResult
    {
        public int Level;
        public int Stars;
        public int Moves;
        public int Par;
        public Core.Levels.LevelKind Kind;
        public LevelReward Reward;
        public string AttemptId;
        public bool IsLastLevel;
    }

    /// <summary>
    /// Mediates input → model → presentation for one level. Pure rules live in GameSession; this class wires
    /// boosters/economy/saving and drives BoardView animations. Input is locked while anything is in flight.
    /// </summary>
    public sealed class GameplayController
    {
        private readonly AppServices _s;
        private readonly BoardView _board;
        private readonly MonoBehaviour _host;
        private CancellationTokenSource _cts = new CancellationTokenSource();

        public GameSession Session { get; private set; }
        public LevelDefinition Level { get; }
        public bool Paused { get; set; }

        public event Action StateChanged;               // moves, inventory, stars changed
        public event Action<LevelResult> Won;
        public event Action Stuck;
        public event Action<TapResult> Tapped;
        public event Action<string> Notice;              // toast-worthy messages

        public GameplayController(AppServices services, LevelDefinition level, BoardView board, MonoBehaviour host)
        {
            _s = services;
            Level = level;
            _board = board;
            _host = host;

            var snap = services.Save.Session;
            if (snap != null && snap.LevelNumber == level.Number) Session = GameSession.FromSnapshot(level, snap);
            if (Session == null) Session = new GameSession(level, Guid.NewGuid().ToString("N"));
            board.BottleTapped = OnBottleTapped;
            board.PourStarted = _ => { _s.Audio.PlayPour(1); _s.Haptics.Light(); };
            PersistSession();
        }

        public void Dispose()
        {
            _cts.Cancel();
        }

        public bool InputLocked => Paused || Session.IsBusy || Session.IsComplete || _board.IsAnimating;

        // ---------------- taps ----------------

        private void OnBottleTapped(int index)
        {
            if (InputLocked) return;
            _board.ClearHints();
            var r = Session.Tap(index);
            Tapped?.Invoke(r);
            switch (r.Kind)
            {
                case TapKind.Selected:
                    _board.SetSelected(index);
                    _s.Audio.PlaySelect();
                    _s.Haptics.Light();
                    break;
                case TapKind.Deselected:
                    _board.SetSelected(-1);
                    _s.Audio.PlayTap();
                    break;
                case TapKind.Reselected:
                    _board.Shake(r.PreviousSelection);
                    _board.SetSelected(index);
                    _s.Audio.PlaySelect();
                    break;
                case TapKind.Invalid:
                    _board.SetSelected(-1);
                    _board.Shake(index);
                    _s.Audio.PlayInvalid();
                    _s.Haptics.Medium();
                    break;
                case TapKind.Moved:
                    AnimateCommittedMove(r);
                    break;
            }
        }

        private void AnimateCommittedMove(TapResult r)
        {
            // The model already committed the move; persist immediately, then render it.
            PersistSession();
            StateChanged?.Invoke();
            Session.IsBusy = true;
            _board.SetSelected(-1);
            var mr = r.Move;
            _board.AnimatePour(mr.Move, mr.Before, mr.After, () =>
            {
                Session.IsBusy = false;
                _board.SyncTo(Session.Current);
                if (r.CompletedBottle)
                {
                    _board.PlayCompleted(mr.Move.To);
                    _s.Audio.PlayComplete();
                    _s.Haptics.Light();
                }
                if (Session.IsComplete) OnWin();
                else if (PuzzleRules.IsStuck(Session.Current)) Stuck?.Invoke();
            });
        }

        // ---------------- boosters ----------------

        private string Tx(string kind) => kind + ":" + Session.AttemptId + ":" + Session.TotalPours + ":" + Session.UndosUsed + ":" + Session.ShuffleCount + ":" + Session.ExtraBottlesAdded + ":" + Guid.NewGuid().ToString("N").Substring(0, 6);

        public int Count(BoosterType t) => _s.Economy.Count(t);

        /// <summary>Returns false (and does nothing) when the booster can't apply right now.</summary>
        public bool CanUse(BoosterType t)
        {
            if (InputLocked) return false;
            switch (t)
            {
                case BoosterType.Undo: return Session.CanUndo;
                case BoosterType.ExtraBottle: return Session.CanAddExtraBottle;
                case BoosterType.Shuffle: return !Session.IsComplete;
                case BoosterType.Hint: return !Session.IsComplete;
            }
            return false;
        }

        public void UseUndo()
        {
            if (!CanUse(BoosterType.Undo)) { Notice?.Invoke(Session.CanUndo ? "Wait for the pour to finish." : "Nothing to undo yet."); return; }
            string tx = Tx("undo");
            if (!_s.Economy.ConsumeBooster(BoosterType.Undo, tx).Success) return;
            var u = Session.Undo();
            if (u == null) { _s.Economy.RefundBooster(BoosterType.Undo, tx); return; }
            Session.IsBusy = true;
            _board.SetSelected(-1);
            PersistSession();
            StateChanged?.Invoke();
            _s.Audio.PlayTap();
            _board.AnimateUndo(u.UndoneMove, u.Before, u.After, () => { Session.IsBusy = false; });
        }

        public void UseExtraBottle()
        {
            if (!CanUse(BoosterType.ExtraBottle)) { Notice?.Invoke("You already added " + GameSession.MaxExtraBottles + " extra bottles on this level."); return; }
            string tx = Tx("extra");
            if (!_s.Economy.ConsumeBooster(BoosterType.ExtraBottle, tx).Success) return;
            if (!Session.AddExtraBottle()) { _s.Economy.RefundBooster(BoosterType.ExtraBottle, tx); return; }
            _board.Build(Session.Current, false);
            var added = _board.Views[_board.Views.Count - 1];
            if (!MotionPrefs.Reduced) Tween.Scale(_host, added.Rect, new Vector3(0.2f, 0.2f, 1), Vector3.one, 0.35f, Ease.OutBack);
            _s.Audio.PlayComplete();
            PersistSession();
            StateChanged?.Invoke();
        }

        /// <summary>
        /// Rewarded ad → an empty bottle is added to the board right away (no inventory involved).
        /// The reward is applied only on a completed ad, at most once per request (RewardedAdGate).
        /// </summary>
        public bool CanAddBottleFromAd => !InputLocked && Session.CanAddExtraBottle && _s.AdGate.CanRequest(Core.Ads.AdPlacements.EmptyBottle);

        public void AddBottleFromAd(Action<bool> finished)
        {
            if (!CanAddBottleFromAd) { finished?.Invoke(false); return; }
            bool rewarded = false;
            Paused = true;
            _s.AdGate.Request(Core.Ads.AdPlacements.EmptyBottle, () =>
            {
                if (!Session.AddExtraBottle()) return;
                rewarded = true;
                _board.Build(Session.Current, false);
                var added = _board.Views[_board.Views.Count - 1];
                if (!MotionPrefs.Reduced) Tween.Scale(_host, added.Rect, new Vector3(0.2f, 0.2f, 1), Vector3.one, 0.35f, Ease.OutBack);
                if (_board.Fx != null) _board.Fx.Sparkle(added.HomePosition + new Vector2(0, added.Height * 0.6f), Color.white, 12, added.Width / 100f);
                _s.Audio.PlayComplete();
                PersistSession();
                StateChanged?.Invoke();
            }, outcome =>
            {
                Paused = false;
                finished?.Invoke(rewarded);
            });
        }

        /// <summary>Generates a verified-solvable layout off the main thread; refunds if none found.</summary>
        public void UseShuffle(Action<bool> finished)
        {
            if (!CanUse(BoosterType.Shuffle)) { finished?.Invoke(false); return; }
            string tx = Tx("shuffle");
            if (!_s.Economy.ConsumeBooster(BoosterType.Shuffle, tx).Success) { finished?.Invoke(false); return; }
            Session.IsBusy = true;
            int index = Session.ShuffleCount;
            var level = Level;
            var task = Task.Run(() => LevelGenerator.GenerateShuffle(level, index), _cts.Token);
            _host.StartCoroutine(Await(task, result =>
            {
                Session.IsBusy = false;
                if (result == null)
                {
                    _s.Economy.RefundBooster(BoosterType.Shuffle, tx);
                    Notice?.Invoke("Couldn't find a new solvable layout quickly. Your puzzle is unchanged and the booster was refunded.");
                    finished?.Invoke(false);
                    return;
                }
                Session.ApplyShuffle(result);
                _board.Build(Session.Current, true);
                PersistSession();
                StateChanged?.Invoke();
                finished?.Invoke(true);
            }));
        }

        /// <summary>Solver-backed hint. The booster is only consumed when a real hint is found.</summary>
        public void UseHint()
        {
            if (!CanUse(BoosterType.Hint)) return;
            if (Count(BoosterType.Hint) <= 0) return;
            Session.IsBusy = true;
            _board.SetSelected(-1);
            Session.ClearSelection();
            var token = _cts.Token;
            var session = Session;
            var opts = SolverOptions.Hint;
            opts.IsCancelled = () => token.IsCancellationRequested;
            var task = Task.Run(() => session.GetHint(opts), token);
            _host.StartCoroutine(Await(task, hint =>
            {
                Session.IsBusy = false;
                if (hint == null) return;
                if (!hint.HasMove)
                {
                    Notice?.Invoke(hint.Message ?? "No hint available right now.");
                    return;
                }
                if (!_s.Economy.ConsumeBooster(BoosterType.Hint, Tx("hint")).Success) return;
                _board.ShowHint(hint.Move.From, hint.Move.To);
                StateChanged?.Invoke();
            }));
        }

        private System.Collections.IEnumerator Await<T>(Task<T> task, Action<T> done)
        {
            while (!task.IsCompleted) yield return null;
            if (task.IsCanceled) yield break;
            if (task.IsFaulted)
            {
                Debug.LogError("[Veliqora] Background task failed: " + task.Exception);
                done(default(T));
                yield break;
            }
            done(task.Result);
        }

        public void Restart()
        {
            if (Session.IsBusy || _board.IsAnimating) return;
            Session.Restart();
            _board.Build(Session.Current, true);
            PersistSession();
            StateChanged?.Invoke();
        }

        // ---------------- win / persistence ----------------

        private void OnWin()
        {
            var prog = _s.Save.Progress;
            int stars = Session.CurrentStars;
            int prev = prog.Stars(Level.Number);
            var reward = _s.Economy.GrantLevelReward(Level.Number, stars, prev, Session.AttemptId, Level.Kind);
            ProgressRules.RecordCompletion(prog, Level.Number, stars, _s.Levels.Count);
            _s.Save.Session = null;
            _s.SaveNow();
            _s.Audio.PlayWin();
            _s.Haptics.Success();
            _board.Celebrate();
            Won?.Invoke(new LevelResult
            {
                Level = Level.Number,
                Stars = stars,
                Moves = Session.MoveCount,
                Par = Session.ParMoves,
                Kind = Level.Kind,
                Reward = reward,
                AttemptId = Session.AttemptId,
                IsLastLevel = Level.Number >= _s.Levels.Count,
            });
        }

        public void PersistSession()
        {
            if (Session.IsComplete) return;
            _s.Save.Session = Session.ToSnapshot();
            _s.MarkDirty();
        }
    }
}
