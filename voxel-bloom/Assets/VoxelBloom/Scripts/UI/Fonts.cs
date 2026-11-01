using UnityEngine;

namespace VoxelBloom
{
    /// <summary>Bundled Fredoka SemiBold (SIL OFL) as a dynamic font, with Unity's built-in font as fallback.</summary>
    public static class Fonts
    {
        static Font main;

        public static Font Main
        {
            get
            {
                if (main != null) return main;
                main = Resources.Load<Font>("VoxelBloom/Fonts/Fredoka-SemiBold");
                if (main == null) main = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return main;
            }
        }
    }
}
