using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Keeps the sandbox pixel art crisp on import: point filtering, no compression, no mipmaps.
    /// Icons/PNG, Ui/Hud and Ui/Markers become 32 PPU sprites (HUD pieces also get their 9-slice
    /// borders); Fonts/Atlases stay plain textures for the damage-number materials.
    /// </summary>
    public class PixelIconImporter : AssetPostprocessor
    {
        private const string IconFolder = "Assets/_Sandbox/nilly-ctrl/Icons/PNG/";
        private const string FontAtlasFolder = "Assets/_Sandbox/nilly-ctrl/Fonts/Atlases/";
        private const string HudFolder = "Assets/_Sandbox/nilly-ctrl/Ui/Hud/";
        private const string MarkerFolder = "Assets/_Sandbox/nilly-ctrl/Ui/Markers/";

        // Slice borders as (left, bottom, right, top). Keep in step with Ui/Hud/Source~/borders.json.
        private static readonly Dictionary<string, Vector4> HudBorders = new Dictionary<string, Vector4>
        {
            { "BarFrame", new Vector4(3, 3, 3, 3) },
            { "BarFillHealth", new Vector4(1, 1, 1, 1) },
            { "BarFillShield", new Vector4(1, 1, 1, 1) },
            { "BarFillDanger", new Vector4(1, 1, 1, 1) },
            { "BarFillFood", new Vector4(1, 1, 1, 1) },
            { "BarFillSkill", new Vector4(1, 1, 1, 1) },
            { "WaveTrack", new Vector4(2, 2, 2, 2) },
            { "WaveFill", new Vector4(1, 0, 1, 0) },
            { "Tooltip", new Vector4(4, 4, 4, 4) },
            { "CostTag", new Vector4(8, 3, 3, 3) },
            { "CostTagCant", new Vector4(8, 3, 3, 3) },
            { "Keycap", new Vector4(4, 5, 4, 4) },
            { "Slot", new Vector4(4, 4, 4, 4) },
            { "SlotSelected", new Vector4(4, 4, 4, 4) },
            { "SlotDisabled", new Vector4(4, 4, 4, 4) },
        };

        // Raise this when the rules below change, so Unity re-imports textures that were
        // already imported under the old rules (Ui/Hud and Ui/Markers were, with blurry defaults).
        public override uint GetVersion() => 2;

        private void OnPreprocessTexture()
        {
            bool isHud = assetPath.StartsWith(HudFolder);
            bool isSprite = isHud || assetPath.StartsWith(IconFolder) || assetPath.StartsWith(MarkerFolder);
            bool isFontAtlas = assetPath.StartsWith(FontAtlasFolder);
            if (!isSprite && !isFontAtlas)
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            if (isSprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 32;
                if (isHud && HudBorders.TryGetValue(Path.GetFileNameWithoutExtension(assetPath), out Vector4 border))
                {
                    importer.spriteBorder = border;
                }
            }
            else
            {
                importer.textureType = TextureImporterType.Default;
            }

            // Many of these are not power-of-two; scaling them would smear the pixels.
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = assetPath.EndsWith("/RangeDash.png") ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        }
    }
}
