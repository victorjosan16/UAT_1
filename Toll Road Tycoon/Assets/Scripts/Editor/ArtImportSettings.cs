using UnityEditor;
using UnityEngine;

namespace TollRoadTycoon.EditorTools
{
    /// <summary>
    /// Import rules for authored art dropped into Assets/Resources/Art (see ASSETS.md):
    /// every image becomes a Sprite, UI panels/buttons and the canopy get 9-slice borders,
    /// and the grass tile repeats.
    /// </summary>
    public sealed class ArtImportSettings : AssetPostprocessor
    {
        private const string Root = "Assets/Resources/Art/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root)) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            // UI art matches the canvas reference (100 px per unit); world art is scaled in code.
            bool ui = assetPath.Contains("/UI/") || assetPath.Contains("/Icons/");
            importer.spritePixelsPerUnit = ui ? 100 : 256;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.maxTextureSize = 2048;

            string name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            if (assetPath.Contains("/Environment/grass"))
            {
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.spriteMeshType = SpriteMeshType.FullRect;
            }
            else
            {
                importer.wrapMode = TextureWrapMode.Clamp;
            }

            bool sliced = assetPath.Contains("/UI/panel") || assetPath.Contains("/UI/button") || name == "canopy";
            if (!sliced) return;
            importer.spriteMeshType = SpriteMeshType.FullRect;
            // Corners stay crisp; the middle 40% stretches.
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            float bx = width * 0.3f;
            float by = height * 0.3f;
            importer.spriteBorder = new Vector4(bx, by, bx, by);
        }
    }
}
