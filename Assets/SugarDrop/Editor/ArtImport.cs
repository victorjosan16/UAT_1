using UnityEditor;

namespace SugarDrop.EditorTools
{
    /// <summary>
    /// Setările de import pentru grafica din Resources/Art: transparență corectă, fără mipmap-uri, compresie de
    /// calitate (pentru telefoane) și mărime maximă 2048 (fundalurile).
    /// </summary>
    public class ArtImport : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("/SugarDrop/Resources/Art/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
        }
    }
}
