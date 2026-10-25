using System.Collections.Generic;
using UnityEngine;

namespace SugarDrop
{
    public enum StepKind { Swap, Clear, Fall, Exit, Shuffle, Grow }

    /// <summary>Starea unei piese într-un pas (pentru animații).</summary>
    public struct PieceInfo
    {
        public int Id;
        public Vector2Int Cell;
        public TileKind Kind;
        public int Color;
        public bool Frozen;
        public int Hits;
    }

    /// <summary>O piesă care cade (sau una nouă care intră de sus: From e deasupra tablei).</summary>
    public struct PieceMove
    {
        public PieceInfo Piece;
        public Vector2Int From;
        public Vector2Int To;
        public bool Spawned;
    }

    /// <summary>Un capac special care s-a activat și celulele lovite.</summary>
    public struct Activation
    {
        public TileKind Kind;
        public Vector2Int Cell;
        public int Color;
        public List<Vector2Int> Targets;
    }

    /// <summary>
    /// Un pas din rezolvarea unei mutări (schimb, eliminare, cădere, ieșirea bomboanelor, amestecare, ciocolata crește).
    /// Motorul calculează instant toți pașii; tabla din scenă îi animă pe rând.
    /// </summary>
    public sealed class BoardStep
    {
        public StepKind Kind;
        /// <summary>A câta cascadă (0 = eliminarea făcută direct de mutare).</summary>
        public int Cascade;

        // Schimb: piesa ajunsă în A și cea ajunsă în B.
        public Vector2Int A, B;
        public int IdAtA, IdAtB;

        public readonly List<PieceInfo> Cleared = new();
        public readonly List<Activation> Activations = new();
        /// <summary>Piese schimbate pe loc (capac special nou, culoare nouă la amestecare, ciocolată nouă). Un Id nou = piesă nouă.</summary>
        public readonly List<PieceInfo> Changed = new();
        public readonly List<PieceInfo> Unfrozen = new();
        /// <summary>Biscuiți loviți, dar încă întregi (Hits = loviturile rămase).</summary>
        public readonly List<PieceInfo> BlockerHits = new();
        /// <summary>Celule unde s-a topit un strat de glazură (și câte straturi au rămas).</summary>
        public readonly List<Vector2Int> FrostingCells = new();
        public readonly List<int> FrostingLeft = new();
        public readonly List<PieceMove> Moves = new();
        /// <summary>Bomboanele care au ieșit jos, spre borcan.</summary>
        public readonly List<PieceInfo> Exits = new();

        public BoardStep(StepKind kind, int cascade = 0)
        {
            Kind = kind;
            Cascade = cascade;
        }
    }
}
