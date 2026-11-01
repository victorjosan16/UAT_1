using System;
using System.Collections.Generic;

namespace VoxelBloom.Core
{
    /// <summary>
    /// Immutable voxel model data: dimensions, palette and one colour index per cell (-1 = empty).
    /// Cell index = x + X * (y + Y * z). The model front faces -Z.
    /// </summary>
    public sealed class VoxelGrid
    {
        public readonly int X, Y, Z;
        public readonly string[] PaletteHex;
        public readonly Rgb[] Palette;
        readonly sbyte[] cells;

        public int Length => cells.Length;

        public VoxelGrid(int x, int y, int z, string[] paletteHex, sbyte[] cells)
        {
            if (x <= 0 || y <= 0 || z <= 0) throw new ArgumentException("Invalid voxel grid size");
            if (cells == null || cells.Length != x * y * z) throw new ArgumentException("Cell count does not match size");
            X = x; Y = y; Z = z;
            PaletteHex = paletteHex ?? Array.Empty<string>();
            Palette = new Rgb[PaletteHex.Length];
            for (int i = 0; i < PaletteHex.Length; i++) Palette[i] = Rgb.FromHex(PaletteHex[i]);
            for (int i = 0; i < cells.Length; i++)
                if (cells[i] >= PaletteHex.Length) throw new ArgumentException("Colour index out of palette at cell " + i);
            this.cells = cells;
        }

        /// <summary>Decodes the compact base-36 string used in the JSON data ('.' = empty).</summary>
        public static sbyte[] Decode(string voxels)
        {
            var r = new sbyte[voxels.Length];
            for (int i = 0; i < voxels.Length; i++)
            {
                char c = voxels[i];
                if (c == '.') r[i] = -1;
                else if (c >= '0' && c <= '9') r[i] = (sbyte)(c - '0');
                else if (c >= 'a' && c <= 'z') r[i] = (sbyte)(c - 'a' + 10);
                else throw new FormatException("Invalid voxel character '" + c + "'");
            }
            return r;
        }

        public static VoxelGrid FromJson(Dictionary<string, object> d)
        {
            int[] size = MiniJson.GetIntArray(d, "size");
            if (size.Length != 3) throw new FormatException("Voxel data needs size[3]");
            var pal = MiniJson.GetArr(d, "palette");
            var hex = new string[pal?.Count ?? 0];
            for (int i = 0; i < hex.Length; i++) hex[i] = (string)pal[i];
            return new VoxelGrid(size[0], size[1], size[2], hex, Decode(MiniJson.GetString(d, "voxels", "")));
        }

        public int Index(int x, int y, int z) => x + X * (y + Y * z);
        public bool InBounds(int x, int y, int z) => x >= 0 && y >= 0 && z >= 0 && x < X && y < Y && z < Z;
        public int Get(int i) => cells[i];
        public int Get(int x, int y, int z) => InBounds(x, y, z) ? cells[Index(x, y, z)] : -1;

        public void Coord(int i, out int x, out int y, out int z)
        {
            x = i % X;
            y = (i / X) % Y;
            z = i / (X * Y);
        }

        public sbyte[] CopyCells() => (sbyte[])cells.Clone();

        public int CountSolid()
        {
            int n = 0;
            for (int i = 0; i < cells.Length; i++) if (cells[i] >= 0) n++;
            return n;
        }
    }

    /// <summary>Static helpers shared by gameplay, rendering and tests.</summary>
    public static class VoxelTopology
    {
        public static readonly int[] DX = { -1, 1, 0, 0, 0, 0 };
        public static readonly int[] DY = { 0, 0, -1, 1, 0, 0 };
        public static readonly int[] DZ = { 0, 0, 0, 0, -1, 1 };

        /// <summary>A voxel is exposed when any of its 6 orthogonal neighbours is empty or outside the model.</summary>
        public static bool IsExposed(sbyte[] cells, int X, int Y, int Z, int i)
        {
            int x = i % X, y = (i / X) % Y, z = i / (X * Y);
            for (int k = 0; k < 6; k++)
            {
                int a = x + DX[k], b = y + DY[k], c = z + DZ[k];
                if (a < 0 || b < 0 || c < 0 || a >= X || b >= Y || c >= Z) return true;
                if (cells[a + X * (b + Y * c)] < 0) return true;
            }
            return false;
        }
    }
}
