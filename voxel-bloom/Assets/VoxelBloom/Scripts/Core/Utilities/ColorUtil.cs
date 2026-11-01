using System;
using System.Globalization;

namespace VoxelBloom.Core
{
    /// <summary>Engine-free RGB colour (0..1 floats) used by data and tests.</summary>
    public readonly struct Rgb
    {
        public readonly float R, G, B;
        public Rgb(float r, float g, float b) { R = r; G = g; B = b; }

        public static Rgb FromHex(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return new Rgb(1, 0, 1);
            string h = hex[0] == '#' ? hex.Substring(1) : hex;
            if (h.Length != 6) throw new FormatException("Colour must be #RRGGBB: " + hex);
            int v = int.Parse(h, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return new Rgb(((v >> 16) & 255) / 255f, ((v >> 8) & 255) / 255f, (v & 255) / 255f);
        }

        public float Luminance => 0.2126f * R + 0.7152f * G + 0.0722f * B;
    }
}
