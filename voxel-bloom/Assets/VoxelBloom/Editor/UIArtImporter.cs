using UnityEditor;

namespace VoxelBloom.EditorTools
{
    /// <summary>Imports the premium UI artwork (Resources/VBUI) as high-quality, uncompressed-alpha sprites without NPOT rescaling.</summary>
    public sealed class UIArtImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/VBUI/")) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.alphaIsTransparency = true;
            ti.alphaSource = TextureImporterAlphaSource.FromInput;
            ti.mipmapEnabled = true; // art is shown smaller than the sheet on small phones
            ti.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            ti.filterMode = UnityEngine.FilterMode.Trilinear;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.maxTextureSize = 2048;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.sRGBTexture = true;
        }
    }
}
