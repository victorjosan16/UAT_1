using System;
using System.Collections.Generic;

namespace VoxelBloom.Core
{
    public enum GameState
    {
        Boot, Home, LevelLoading, Ready, BlockSelected, AttackQueued, Attacking, Resolving, Paused, LevelComplete, LevelFailed,
    }

    /// <summary>Validated state machine; illegal transitions are rejected (and reported) instead of silently applied.</summary>
    public sealed class GameStateMachine
    {
        static readonly Dictionary<GameState, GameState[]> Allowed = new Dictionary<GameState, GameState[]>
        {
            { GameState.Boot, new[] { GameState.Home } },
            { GameState.Home, new[] { GameState.LevelLoading } },
            { GameState.LevelLoading, new[] { GameState.Ready, GameState.Home } },
            { GameState.Ready, new[] { GameState.BlockSelected, GameState.Paused, GameState.Attacking, GameState.Resolving, GameState.Home, GameState.LevelLoading } },
            { GameState.BlockSelected, new[] { GameState.AttackQueued, GameState.Ready } },
            { GameState.AttackQueued, new[] { GameState.Attacking, GameState.Resolving, GameState.Ready, GameState.Paused } },
            { GameState.Attacking, new[] { GameState.Attacking, GameState.Resolving, GameState.Paused, GameState.BlockSelected } },
            { GameState.Resolving, new[] { GameState.Ready, GameState.LevelComplete, GameState.LevelFailed, GameState.Attacking, GameState.Paused } },
            { GameState.Paused, new[] { GameState.Ready, GameState.Attacking, GameState.Resolving, GameState.AttackQueued, GameState.LevelLoading, GameState.Home } },
            { GameState.LevelComplete, new[] { GameState.LevelLoading, GameState.Home } },
            { GameState.LevelFailed, new[] { GameState.LevelLoading, GameState.Home } },
        };

        public GameState Current { get; private set; } = GameState.Boot;
        public GameState BeforePause { get; private set; } = GameState.Ready;
        public event Action<GameState, GameState> Changed;
        public event Action<GameState, GameState> Rejected;

        public bool CanTransition(GameState to) => Allowed.TryGetValue(Current, out var list) && Array.IndexOf(list, to) >= 0;

        public bool TryTransition(GameState to)
        {
            if (!CanTransition(to)) { Rejected?.Invoke(Current, to); return false; }
            var from = Current;
            if (to == GameState.Paused) BeforePause = from;
            Current = to;
            Changed?.Invoke(from, to);
            return true;
        }

        /// <summary>Gameplay input (tapping blocks) is accepted only in these states.</summary>
        public bool AcceptsBoardInput => Current == GameState.Ready || Current == GameState.AttackQueued || Current == GameState.Attacking;

        public bool Resume() => Current == GameState.Paused && TryTransition(BeforePause);

        /// <summary>Hard reset used on restart/scene unload; bypasses validation by design.</summary>
        public void ForceReset(GameState to)
        {
            var from = Current;
            Current = to;
            Changed?.Invoke(from, to);
        }
    }
}
