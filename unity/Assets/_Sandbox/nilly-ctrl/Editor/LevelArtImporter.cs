using UnityEditor;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// First-import defaults for the level ground images in Levels/ (drawn by
    /// Levels/Source~/build_level.py): one crisp sprite at 32 pixels per cell, so a cell is one
    /// world unit. Only applied when the asset has no .meta yet, so later Inspector changes stick.
    /// </summary>
    public class LevelArtImporter : AssetPostprocessor
    {
        private const string LevelFolder = "Assets/_Sandbox/nilly-ctrl/Levels/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(LevelFolder) || !assetImporter.importSettingsMissing)
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            // A 40 x 24 cell map is 1280 px wide; leave room for larger ones.
            importer.maxTextureSize = 4096;
        }
    }
}
