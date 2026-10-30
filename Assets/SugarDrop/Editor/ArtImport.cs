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
            // Piesele pictate (256×256, 5 imagini) rămân necomprimate și citibile: dungile pieselor speciale se
            // decupează după transparența lor (PieceSprites.Stripes).
            string name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            if (name.Length == 6 && name.StartsWith("piece") && char.IsDigit(name[5]))
            {
                importer.isReadable = true;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
            }
        }
    }
}
