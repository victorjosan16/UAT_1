using TMPro;
using UnityEngine;

namespace VoxelBloom
{
    /// <summary>Creates a dynamic TMP font asset from the bundled Fredoka (OFL) font, with TMP default as fallback.</summary>
    public static class Fonts
    {
        static TMP_FontAsset main;

        public static TMP_FontAsset Main
        {
            get
            {
                if (main != null) return main;
                var font = Resources.Load<Font>("VoxelBloom/Fonts/Fredoka-SemiBold");
                if (font != null)
                {
                    try { main = TMP_FontAsset.CreateFontAsset(font); }
                    catch (System.Exception e) { Debug.LogWarning("[VoxelBloom] Dynamic font asset failed: " + e.Message); }
                }
                if (main == null) main = TMP_Settings.defaultFontAsset;
                if (main != null && TMP_Settings.defaultFontAsset != null && main != TMP_Settings.defaultFontAsset)
                {
                    if (main.fallbackFontAssetTable == null) main.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
                    if (!main.fallbackFontAssetTable.Contains(TMP_Settings.defaultFontAsset)) main.fallbackFontAssetTable.Add(TMP_Settings.defaultFontAsset);
                }
                return main;
            }
        }
    }
}
