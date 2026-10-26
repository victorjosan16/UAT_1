using System;

namespace UndergroundMetroTycoon.Simulation
{
    /// <summary>
    /// Authoritative state of one excavation cell. Views only read this; all mutation goes
    /// through <see cref="ExcavationService"/>.
    /// </summary>
    public sealed class TerrainCell
    {
        public TerrainCell(int index, string id, GridCoord coord, TerrainMaterialDefinition material, string contractId)
        {
            if (material == null) throw new ArgumentNullException(nameof(material));
            Index = index;
            Id = id;
            Coord = coord;
            Material = material.Material;
            MaxHp = Math.Max(1, material.MaxHp);
            CurrentHp = MaxHp;
            RequiredToolLevel = material.RequiredToolLevel;
            CoinValue = Math.Max(0, material.CoinValue);
            XpValue = Math.Max(0, material.XpValue);
            MaterialYield = Math.Max(0, material.MaterialYield);
            ContractId = string.IsNullOrEmpty(contractId) ? null : contractId;
        }

        public int Index { get; }
        public string Id { get; }
        public GridCoord Coord { get; }
        public TerrainMaterial Material { get; }
        public int MaxHp { get; }
        public int CurrentHp { get; private set; }
        public bool IsExcavated { get; private set; }
        public int RequiredToolLevel { get; }
        public int CoinValue { get; }
        public int XpValue { get; }
        public int MaterialYield { get; }

        /// <summary>Contract this cell belongs to; null for solid terrain that may never be dug.</summary>
        public string ContractId { get; }

        public bool IsContractCell => ContractId != null;
        public float Health01 => (float)CurrentHp / MaxHp;

        /// <summary>Applies damage, clamped so HP never goes negative. Returns the damage actually dealt.</summary>
        internal int ApplyDamage(int amount)
        {
            if (IsExcavated || amount <= 0) return 0;
            int dealt = Math.Min(amount, CurrentHp);
            CurrentHp -= dealt;
            if (CurrentHp == 0) IsExcavated = true;
            return dealt;
        }

        internal void RestoreHp(int hp)
        {
            if (!IsContractCell) hp = MaxHp;
            CurrentHp = Math.Max(0, Math.Min(MaxHp, hp));
            IsExcavated = CurrentHp == 0;
        }
    }
}
