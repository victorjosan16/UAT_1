using UndergroundMetroTycoon.Simulation;
using UnityEngine;

namespace UndergroundMetroTycoon.Game
{
    /// <summary>Shared colour palette: navy, construction yellow, orange, cyan, emerald and warm soil browns.</summary>
    public static class Palette
    {
        public static readonly Color Navy = Hex(0x0E1A2E);
        public static readonly Color NavyDeep = Hex(0x081120);
        public static readonly Color NavyLight = Hex(0x1C2E4A);
        public static readonly Color Yellow = Hex(0xFFC21A);
        public static readonly Color Orange = Hex(0xFF8A1F);
        public static readonly Color Cyan = Hex(0x22C7E8);
        public static readonly Color Emerald = Hex(0x2ECC71);
        public static readonly Color Red = Hex(0xE5484D);
        public static readonly Color White = Hex(0xF4F7FB);
        public static readonly Color Muted = Hex(0x9FB3C8);

        public static readonly Color Grass = Hex(0x6DBE45);
        public static readonly Color GrassDark = Hex(0x55A23A);
        public static readonly Color Asphalt = Hex(0x3A4250);
        public static readonly Color Concrete = Hex(0xC9CED6);
        public static readonly Color ConcreteDark = Hex(0x9AA3AF);
        public static readonly Color Bedrock = Hex(0x5B4636);
        public static readonly Color DirtPlot = Hex(0x9C6B42);

        public static readonly Color Skin = Hex(0xF1C27D);
        public static readonly Color Vest = Hex(0xFF7A1A);
        public static readonly Color Reflective = Hex(0xE8EEF2);
        public static readonly Color Shirt = Hex(0x2F6FB5);
        public static readonly Color Pants = Hex(0x23324D);
        public static readonly Color Boots = Hex(0x4A3222);
        public static readonly Color Steel = Hex(0xA9B4C2);
        public static readonly Color Wood = Hex(0xB07A45);
        public static readonly Color Black = Hex(0x1A1A1A);

        public static Color Terrain(TerrainMaterial material)
        {
            switch (material)
            {
                case TerrainMaterial.SoftSoil: return Hex(0xB9814F);
                case TerrainMaterial.CompactSoil: return Hex(0x8F5E37);
                case TerrainMaterial.Clay: return Hex(0xC0673E);
                case TerrainMaterial.Stone: return Hex(0x8C8F94);
                case TerrainMaterial.HardRock: return Hex(0x5F646B);
                default: return Hex(0x3E4148);
            }
        }

        public static Color Hex(int rgb) => new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);

        public static Color Shade(Color c, float factor) => new Color(c.r * factor, c.g * factor, c.b * factor, c.a);
    }
}
