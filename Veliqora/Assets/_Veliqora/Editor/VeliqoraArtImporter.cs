using UnityEditor;

namespace Veliqora.EditorTools
{
    /// <summary>
    /// Import settings for authored art under Resources/Veliqora/Art (loaded at runtime as Texture2D and turned
    /// into sprites/9-slices in code): keep exact pixel dimensions (no power-of-two rescale), no mipmaps,
    /// clamp wrap, proper alpha, high-quality compression.
    /// </summary>
    public sealed class VeliqoraArtImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            string path = assetPath.Replace('\\', '/');
            if (!path.Contains("/_Veliqora/Resources/Veliqora/Art/") && !path.Contains("/_Veliqora/Art/Source/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            importer.filterMode = UnityEngine.FilterMode.Bilinear;
            importer.isReadable = false;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
        }
    }
}
