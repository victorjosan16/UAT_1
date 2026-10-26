using System;
using System.Collections.Generic;
using System.Numerics;

namespace UndergroundMetroTycoon.Simulation
{
    public enum DigOutcome
    {
        Ok,
        Damaged,
        Excavated,
        NoSite,
        NotFound,
        AlreadyExcavated,
        NoActiveContract,
        OutsideContract,
        NotAccessible,
        InvalidTool,
        ToolTooWeak,
        ReservedByOther
    }

    public readonly struct DigResult
    {
        public readonly DigOutcome Outcome;
        public readonly int CellIndex;
        public readonly int Damage;

        public DigResult(DigOutcome outcome, int cellIndex, int damage)
        {
            Outcome = outcome;
            CellIndex = cellIndex;
            Damage = damage;
        }

        public bool Succeeded => Outcome == DigOutcome.Damaged || Outcome == DigOutcome.Excavated;
    }

    /// <summary>
    /// Validates and applies excavation. Holds per-cell reservations so several diggers
    /// (player now, workers later) never work the same cell.
    /// </summary>
    public sealed class ExcavationService
    {
        public const float MaxReachUp = 0.6f;
        public const float MaxReachDown = 1.1f;

        private readonly Dictionary<int, string> _reservations = new Dictionary<int, string>();

        public ExcavationGrid Grid { get; private set; }
        public string ActiveContractId { get; set; }

        public event Action<TerrainCell, int> CellDamaged;
        public event Action<TerrainCell> CellExcavated;

        public void SetGrid(ExcavationGrid grid)
        {
            Grid = grid;
            _reservations.Clear();
        }

        public DigOutcome Validate(int cellIndex, ToolDefinition tool, string actorId)
        {
            if (Grid == null) return DigOutcome.NoSite;
            TerrainCell cell = Grid.GetCell(cellIndex);
            if (cell == null) return DigOutcome.NotFound;
            if (cell.IsExcavated) return DigOutcome.AlreadyExcavated;
            if (!cell.IsContractCell) return DigOutcome.OutsideContract;
            if (ActiveContractId == null) return DigOutcome.NoActiveContract;
            if (cell.ContractId != ActiveContractId) return DigOutcome.OutsideContract;
            if (!Grid.IsAccessible(cell)) return DigOutcome.NotAccessible;
            if (tool == null || tool.Power <= 0) return DigOutcome.InvalidTool;
            if (tool.Level < cell.RequiredToolLevel) return DigOutcome.ToolTooWeak;
            if (IsReservedByOther(cellIndex, actorId)) return DigOutcome.ReservedByOther;
            return DigOutcome.Ok;
        }

        public DigResult Dig(int cellIndex, ToolDefinition tool, string actorId)
        {
            DigOutcome validation = Validate(cellIndex, tool, actorId);
            if (validation != DigOutcome.Ok) return new DigResult(validation, cellIndex, 0);

            TerrainCell cell = Grid.GetCell(cellIndex);
            int dealt = cell.ApplyDamage(tool.Power);
            CellDamaged?.Invoke(cell, dealt);
            if (!cell.IsExcavated) return new DigResult(DigOutcome.Damaged, cellIndex, dealt);

            Grid.NotifyExcavated(cell);
            _reservations.Remove(cellIndex);
            CellExcavated?.Invoke(cell);
            return new DigResult(DigOutcome.Excavated, cellIndex, dealt);
        }

        public bool TryReserve(int cellIndex, string actorId)
        {
            if (Grid?.GetCell(cellIndex) == null || string.IsNullOrEmpty(actorId)) return false;
            if (IsReservedByOther(cellIndex, actorId)) return false;
            _reservations[cellIndex] = actorId;
            return true;
        }

        public void Release(int cellIndex, string actorId)
        {
            if (_reservations.TryGetValue(cellIndex, out string owner) && owner == actorId) _reservations.Remove(cellIndex);
        }

        public string GetReservation(int cellIndex) => _reservations.TryGetValue(cellIndex, out string owner) ? owner : null;

        private bool IsReservedByOther(int cellIndex, string actorId) =>
            _reservations.TryGetValue(cellIndex, out string owner) && owner != actorId;

        /// <summary>Picks the best diggable cell within reach, preferring cells in front of the actor and not under its feet.</summary>
        public int FindBestTarget(Vector3 feet, Vector3 forward, float reach, ToolDefinition tool, string actorId)
        {
            if (Grid == null || ActiveContractId == null || tool == null) return -1;
            int best = -1;
            float bestScore = float.MaxValue;
            IReadOnlyList<TerrainCell> cells = Grid.Cells;
            for (int i = 0; i < cells.Count; i++)
            {
                TerrainCell c = cells[i];
                if (c.IsExcavated || c.ContractId != ActiveContractId) continue;
                if (!Grid.IsAccessible(c) || tool.Level < c.RequiredToolLevel || IsReservedByOther(i, actorId)) continue;
                if (!IsWithinReach(c, feet, reach, out float distance, out float dx, out float dz)) continue;

                float score = distance;
                if (distance > 0.001f) score -= 0.25f * (dx * forward.X + dz * forward.Z) / distance;
                if (distance < 0.45f * Grid.Layout.CellSize) score += 0.8f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = i;
                }
            }
            return best;
        }

        /// <summary>Nearest exposed contract cell in reach, regardless of whether it can be dug right now (used for hints).</summary>
        public int FindNearestExposedCell(Vector3 feet, float reach)
        {
            if (Grid == null) return -1;
            int best = -1;
            float bestDistance = float.MaxValue;
            IReadOnlyList<TerrainCell> cells = Grid.Cells;
            for (int i = 0; i < cells.Count; i++)
            {
                TerrainCell c = cells[i];
                if (!c.IsContractCell || !Grid.IsAccessible(c)) continue;
                if (!IsWithinReach(c, feet, reach, out float distance, out _, out _)) continue;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }
            return best;
        }

        private bool IsWithinReach(TerrainCell c, Vector3 feet, float reach, out float distance, out float dx, out float dz)
        {
            Vector3 top = Grid.Layout.CellTopCenter(c.Coord);
            dx = top.X - feet.X;
            dz = top.Z - feet.Z;
            float dy = top.Y - feet.Y;
            distance = (float)Math.Sqrt(dx * dx + dz * dz);
            return dy <= MaxReachUp && dy >= -MaxReachDown && distance <= reach;
        }
    }
}
