using UnityEditor;
using UnityEditor.U2D.Aseprite;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// First-import defaults for the generated animation sheets in Animations/ and the ground
    /// tiles in Tiles/: 32 PPU (one tower fills one world unit) and centred pivots, because
    /// towers and enemies are drawn top-down around the canvas centre. Only applied when the asset has no .meta yet, so later
    /// Inspector changes stick.
    /// </summary>
    public class PixelAnimImporter : AssetPostprocessor
    {
        private const string AsepriteFolder = "Assets/_Sandbox/nilly-ctrl/Animations/Aseprite/";
        private const string SheetFolder = "Assets/_Sandbox/nilly-ctrl/Animations/Sheets/";
        private const string TileFolder = "Assets/_Sandbox/nilly-ctrl/Tiles/";
        private const string UiFolder = "Assets/_Sandbox/nilly-ctrl/Ui/";
        private const float PixelsPerUnit = 32f;

        private void OnPreprocessAsset()
        {
            if (!assetPath.StartsWith(AsepriteFolder) || !assetImporter.importSettingsMissing)
            {
                return;
            }

            if (assetImporter is AsepriteImporter importer)
            {
                importer.spritePixelsPerUnit = PixelsPerUnit;
                importer.pivotSpace = PivotSpaces.Canvas;
                importer.pivotAlignment = SpriteAlignment.Center;
            }
        }

        private void OnPreprocessTexture()
        {
            bool isTile = assetPath.StartsWith(TileFolder);
            bool isUi = assetPath.StartsWith(UiFolder);
            if (!(isTile || isUi || assetPath.StartsWith(SheetFolder)) || !assetImporter.importSettingsMissing)
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            if (isUi)
            {
                // IMGUI skin pieces: plain crisp textures, drawn nine-sliced by GUIStyle borders.
                importer.textureType = TextureImporterType.GUI;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = isTile ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;

            if (isTile)
            {
                // Tiled SpriteRenderers need a full-rect mesh (set last, after the texture type).
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(settings);
            }
        }
    }
}
