using System.Collections.Generic;
using UnityEngine;

namespace SugarDrop
{
    public enum StepKind { Clear, Fall, Exit, Shuffle, Grow, BossAttack, Swap }

    /// <summary>Starea unei piese într-un pas (pentru animații).</summary>
    public struct PieceInfo
    {
        public int Id;
        public Vector2Int Cell;
        public TileKind Kind;
        public int Color;
        /// <summary>Straturi de gheață peste bomboană (0–3): nu se poate atinge și nu cade cât are gheață.</summary>
        public int Ice;
        /// <summary>Bomboană legată cu lanț: nu se poate atinge și nu cade până nu se rupe lanțul.</summary>
        public bool Chained;
        /// <summary>Loviturile rămase ale unei lăzi / ciocolate.</summary>
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
    /// Un pas din rezolvarea unei mutări (eliminare, cădere, ieșirea bomboanelor, amestecare, ciocolata crește).
    /// Motorul calculează instant toți pașii; tabla din scenă îi animă pe rând.
    /// </summary>
    public sealed class BoardStep
    {
        public StepKind Kind;
        /// <summary>Păstrat pentru compatibilitate cu scorul (0 = eliminarea făcută direct de mutare).</summary>
        public int Cascade;

        /// <summary>Celula atinsă de jucător (pentru efecte).</summary>
        public Vector2Int Origin;
        /// <summary>Mărimea grupului eliminat la atingere (0 dacă pasul vine dintr-o piesă specială).</summary>
        public int GroupSize;
        public int GroupColor = -1;
        /// <summary>Două piese speciale combinate (pentru efecte mai mari).</summary>
        public bool Combo;

        public readonly List<PieceInfo> Cleared = new();
        public readonly List<Activation> Activations = new();
        /// <summary>Piese schimbate pe loc (capac special nou, culoare nouă la amestecare, ciocolată nouă). Un Id nou = piesă nouă.</summary>
        public readonly List<PieceInfo> Changed = new();
        /// <summary>Obstacole lovite, dar încă întregi: gheață subțiată, lanț rupt, ladă/ciocolată crăpată (starea nouă).</summary>
        public readonly List<PieceInfo> ObstacleHits = new();
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
