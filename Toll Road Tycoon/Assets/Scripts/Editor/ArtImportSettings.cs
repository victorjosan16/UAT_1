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
            bool fullRect = false;
            if (assetPath.Contains("/Environment/grass"))
            {
                importer.wrapMode = TextureWrapMode.Repeat;
                fullRect = true;
            }
            else
            {
                importer.wrapMode = TextureWrapMode.Clamp;
            }

            bool sliced = assetPath.Contains("/UI/panel") || assetPath.Contains("/UI/button") || name == "canopy";
            if (fullRect || sliced)
            {
                // Tiled and 9-sliced sprites need a full-rect mesh (set through TextureImporterSettings).
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
            }
            if (!sliced) return;
            // Corners stay crisp while the middle stretches. Buttons are pills, so their border is
            // almost half the height; panels and the canopy keep 30% of their shorter side.
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            float corner = Mathf.Min(width, height) * (assetPath.Contains("/UI/button") ? 0.48f : 0.3f);
            importer.spriteBorder = new Vector4(corner, corner, corner, corner);
        }
    }
}
