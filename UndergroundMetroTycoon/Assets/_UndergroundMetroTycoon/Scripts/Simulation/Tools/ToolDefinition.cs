using System;

namespace UndergroundMetroTycoon.Simulation
{
    [Serializable]
    public sealed class ToolDefinition
    {
        public string Id;
        public string DisplayName;
        public int Level = 1;
        public int Power = 5;
        public float SwingsPerSecond = 1.4f;
        public float Reach = 1.6f;
        public long PurchaseCost;
    }
}
