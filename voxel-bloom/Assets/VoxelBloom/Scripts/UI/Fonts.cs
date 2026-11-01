using UnityEngine;

namespace VoxelBloom
{
    /// <summary>Bundled Baloo 2 ExtraBold (SIL OFL, covers Romanian) as a dynamic font, with Unity's built-in font as fallback.</summary>
    public static class Fonts
    {
        static Font main;

        public static Font Main
        {
            get
            {
                if (main != null) return main;
                main = Resources.Load<Font>("VoxelBloom/Fonts/Baloo2-ExtraBold");
                if (main == null) main = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return main;
            }
        }
    }
}
