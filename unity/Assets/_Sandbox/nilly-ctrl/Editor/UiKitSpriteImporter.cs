using System.IO;
using UnityEditor;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Imports UiKit/Sprites as crisp 9-slice UI sprites: point filtering, no compression, and
    /// the slice border for each file. Borders live here rather than being set by hand in the
    /// Sprite Editor so regenerating the PNGs never loses them.
    /// </summary>
    public class UiKitSpriteImporter : AssetPostprocessor
    {
        public const string SpriteFolder = "Assets/_Sandbox/nilly-ctrl/UiKit/Sprites/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(SpriteFolder))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100; // matches the canvas, so one sprite pixel is one canvas unit
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; // sliced sprites need the full rectangle
            settings.spriteBorder = BorderFor(Path.GetFileNameWithoutExtension(assetPath));
            importer.SetTextureSettings(settings);
        }

        // Left, bottom, right, top, in sprite pixels.
        private static Vector4 BorderFor(string name)
        {
            if (name == "TabActive") return new Vector4(5, 2, 5, 5); // open at the bottom
            if (name.StartsWith("Button") || name.StartsWith("Tab")) return new Vector4(5, 5, 5, 5);
            if (name == "Panel" || name == "FocusRing") return new Vector4(6, 6, 6, 6);
            if (name == "Well" || name.StartsWith("ToggleBox") || name.StartsWith("SliderHandle")) return new Vector4(3, 3, 3, 3);
            if (name == "SliderFill") return new Vector4(1, 1, 1, 1);
            return Vector4.zero; // Arrow, ToggleCheck, Divider: drawn whole
        }
    }
}
