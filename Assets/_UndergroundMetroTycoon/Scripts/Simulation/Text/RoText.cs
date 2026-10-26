using System.Collections.Generic;

namespace UndergroundMetroTycoon.Simulation
{
    /// <summary>Romanian text helpers for player-facing strings built from game data.</summary>
    public static class RoText
    {
        /// <summary>
        /// Romanian counted nouns: "1 bloc", "7 blocuri", "20 de blocuri", "101 blocuri", "120 de blocuri".
        /// Numbers whose last two digits are 00 or 20-99 take "de" (except 1).
        /// </summary>
        public static string Count(long n, string singular, string plural)
        {
            if (n == 1) return "1 " + singular;
            long lastTwo = System.Math.Abs(n) % 100;
            bool needsDe = n != 0 && (lastTwo == 0 || lastTwo >= 20);
            return n + (needsDe ? " de " : " ") + plural;
        }

        /// <summary>Objective line for a contract, e.g. "Sapă 20 de blocuri de pământ".</summary>
        public static string ContractObjective(ContractDefinition contract)
        {
            int cells = contract.CountContractCells();
            string text = "Sapă " + Count(cells, "bloc", "blocuri");
            return AllSoil(contract.Layers) ? text + " de pământ" : text;
        }

        public static string Difficulty(ContractDifficulty difficulty)
        {
            switch (difficulty)
            {
                case ContractDifficulty.Beginner: return "UȘOR";
                case ContractDifficulty.Intermediate: return "MEDIU";
                default: return "DIFICIL";
            }
        }

        private static bool AllSoil(List<ContractLayerSpec> layers)
        {
            if (layers == null || layers.Count == 0) return false;
            foreach (ContractLayerSpec layer in layers)
            {
                if (layer.Material != TerrainMaterial.SoftSoil && layer.Material != TerrainMaterial.CompactSoil) return false;
            }
            return true;
        }
    }
}
