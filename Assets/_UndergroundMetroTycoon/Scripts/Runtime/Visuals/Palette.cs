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
        public static readonly Color Pants = Hex(0x2F5FA8);
        public static readonly Color Boots = Hex(0x4A3222);
        public static readonly Color Steel = Hex(0xA9B4C2);
        public static readonly Color Wood = Hex(0xB07A45);
        public static readonly Color Black = Hex(0x1A1A1A);

        // Environment
        public static readonly Color Sand = Hex(0xE8C88F);
        public static readonly Color SandDark = Hex(0xD3AE72);
        public static readonly Color Terracotta = Hex(0xC8693C);
        public static readonly Color GrassFresh = Hex(0x8BCB52);
        public static readonly Color GrassDeep = Hex(0x6DB443);
        public static readonly Color Sky = Hex(0x8FD2FA);
        public static readonly Color WorkBlue = Hex(0x2F5FA8);

        /// <summary>Main (side) colour of a terrain layer.</summary>
        public static Color Terrain(TerrainMaterial material) => TerrainSide(material);

        /// <summary>Lighter colour of a block's exposed top surface.</summary>
        public static Color TerrainTop(TerrainMaterial material)
        {
            switch (material)
            {
                case TerrainMaterial.SoftSoil: return Hex(0xDDA468);
                case TerrainMaterial.CompactSoil: return Hex(0xBF7E49);
                case TerrainMaterial.Clay: return Hex(0xD9804F);
                case TerrainMaterial.Stone: return Hex(0xAEB3BB);
                case TerrainMaterial.HardRock: return Hex(0x838A94);
                default: return Hex(0x5B616C);
            }
        }

        /// <summary>Darker colour of a block's sides, also used for the strata in pit walls.</summary>
        public static Color TerrainSide(TerrainMaterial material)
        {
            switch (material)
            {
                case TerrainMaterial.SoftSoil: return Hex(0xBA7640);
                case TerrainMaterial.CompactSoil: return Hex(0x965A30);
                case TerrainMaterial.Clay: return Hex(0xAD5830);
                case TerrainMaterial.Stone: return Hex(0x81878F);
                case TerrainMaterial.HardRock: return Hex(0x5E646D);
                default: return Hex(0x40454E);
            }
        }

        public static Color Hex(int rgb) => new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);

        public static Color Shade(Color c, float factor) => new Color(c.r * factor, c.g * factor, c.b * factor, c.a);
    }
}
