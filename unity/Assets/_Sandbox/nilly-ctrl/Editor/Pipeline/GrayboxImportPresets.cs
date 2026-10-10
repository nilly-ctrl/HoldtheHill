using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The one documented table of how each sandbox art folder should import. It mirrors what the
    /// existing importers (<c>PixelIconImporter</c>, <c>PixelAnimImporter</c>, <c>UiKitSpriteImporter</c>,
    /// <c>LevelArtImporter</c>) apply and is read only by the audit window; it does not change them.
    /// </summary>
    /// <remarks>
    /// Keep in step with those importers. A rule here that disagrees with an importer shows up in the
    /// audit as a deviation on every texture, which is the cue to fix whichever side is wrong.
    /// Tiles/ sets are sliced (Sprite Mode Multiple), so that rule accepts Single or Multiple.
    /// Common to every rule: point filter, uncompressed, no mipmaps, alpha is transparency, no NPOT
    /// scaling. The first matching rule wins, so more specific folders come first.
    /// </remarks>
    public static class GrayboxImportPresets
    {
        private const string Root = "Assets/_Sandbox/nilly-ctrl/";

        public enum Kind
        {
            Sprite,
            Gui,
            Default,
        }

        /// <summary>Expected import settings for every texture under <see cref="Folder"/>.</summary>
        public sealed class Rule
        {
            public string Folder;
            public string Note;
            public Kind TextureKind;
            public float PixelsPerUnit;            // sprites only; 0 = not checked
            public TextureWrapMode? Wrap;          // null = not checked
            public SpriteAlignment? Pivot;         // null = not checked (sprites only)
            public bool AllowMultiple;             // sliced sheets (Sprite Mode Multiple) are fine
            public SpriteMeshType? MeshType;       // null = not checked (sprites only)
        }

        // Folder -> settings. PPU 32 = one tower fills one world unit; UiKit is 100 to match its canvas.
        public static readonly IReadOnlyList<Rule> Rules = new List<Rule>
        {
            new Rule { Folder = Root + "Icons/PNG/",          Note = "32x32 icons",           TextureKind = Kind.Sprite,  PixelsPerUnit = 32, Wrap = TextureWrapMode.Clamp, Pivot = SpriteAlignment.Center },
            new Rule { Folder = Root + "Ui/Hud/",             Note = "9-slice HUD pieces",    TextureKind = Kind.Sprite,  PixelsPerUnit = 32, Wrap = TextureWrapMode.Clamp, Pivot = SpriteAlignment.Center },
            new Rule { Folder = Root + "Ui/Markers/",         Note = "cursors, tile markers", TextureKind = Kind.Sprite,  PixelsPerUnit = 32, Pivot = SpriteAlignment.Center },
            new Rule { Folder = Root + "Fonts/Atlases/",      Note = "font atlases (plain textures)", TextureKind = Kind.Default },
            new Rule { Folder = Root + "UiKit/Sprites/",      Note = "uGUI 9-slice sprites",  TextureKind = Kind.Sprite,  PixelsPerUnit = 100, Wrap = TextureWrapMode.Clamp, MeshType = SpriteMeshType.FullRect, Pivot = SpriteAlignment.Center },
            new Rule { Folder = Root + "Animations/Sheets/",  Note = "animation sheets",      TextureKind = Kind.Sprite,  PixelsPerUnit = 32, Wrap = TextureWrapMode.Clamp, Pivot = SpriteAlignment.Center },
            new Rule { Folder = Root + "Tiles/",              Note = "ground and tile sets",  TextureKind = Kind.Sprite,  PixelsPerUnit = 32, Pivot = SpriteAlignment.Center, AllowMultiple = true },
            new Rule { Folder = Root + "Levels/",             Note = "hand-drawn level floors", TextureKind = Kind.Sprite, PixelsPerUnit = 32, Wrap = TextureWrapMode.Clamp, Pivot = SpriteAlignment.Center },
            new Rule { Folder = Root + "Ui/",                 Note = "IMGUI skin pieces",     TextureKind = Kind.Gui,     Wrap = TextureWrapMode.Clamp },
        };

        /// <summary>Folders whose textures are not audited (drawing sources, app icon sizes, themed copies the builders own).</summary>
        public static readonly string[] Excluded =
        {
            Root + "Icons/AppIcon/",
            Root + "Ui/Title/",
        };

        /// <summary>The rule for an asset path, or null when the path is outside every audited folder.</summary>
        public static Rule Find(string assetPath)
        {
            foreach (string excluded in Excluded)
            {
                if (assetPath.StartsWith(excluded, StringComparison.Ordinal)) return null;
            }

            foreach (Rule rule in Rules)
            {
                if (assetPath.StartsWith(rule.Folder, StringComparison.Ordinal)) return rule;
            }

            return null;
        }

        /// <summary>One setting that differs from its rule.</summary>
        public readonly struct Deviation
        {
            public readonly string Setting;
            public readonly string Expected;
            public readonly string Actual;

            public Deviation(string setting, string expected, string actual)
            {
                Setting = setting;
                Expected = expected;
                Actual = actual;
            }

            public override string ToString() => $"{Setting}: {Actual} (want {Expected})";
        }

        /// <summary>Settings of the importer that differ from the rule. Empty when it conforms.</summary>
        public static List<Deviation> Check(Rule rule, TextureImporter importer)
        {
            var result = new List<Deviation>();
            void Expect<T>(string name, T expected, T actual)
            {
                if (!EqualityComparer<T>.Default.Equals(expected, actual))
                {
                    result.Add(new Deviation(name, expected.ToString(), actual.ToString()));
                }
            }

            TextureImporterType wantType = rule.TextureKind == Kind.Sprite ? TextureImporterType.Sprite
                : rule.TextureKind == Kind.Gui ? TextureImporterType.GUI
                : TextureImporterType.Default;

            Expect("Texture Type", wantType, importer.textureType);
            Expect("Filter Mode", FilterMode.Point, importer.filterMode);
            Expect("Compression", TextureImporterCompression.Uncompressed, importer.textureCompression);
            Expect("Generate Mip Maps", false, importer.mipmapEnabled);
            Expect("Alpha Is Transparency", true, importer.alphaIsTransparency);
            Expect("Non-Power of 2", TextureImporterNPOTScale.None, importer.npotScale);

            if (rule.Wrap.HasValue)
            {
                Expect("Wrap Mode", rule.Wrap.Value, importer.wrapMode);
            }

            if (rule.TextureKind == Kind.Sprite)
            {
                bool modeOk = importer.spriteImportMode == SpriteImportMode.Single
                    || (rule.AllowMultiple && importer.spriteImportMode == SpriteImportMode.Multiple);
                if (!modeOk)
                {
                    result.Add(new Deviation("Sprite Mode", SpriteImportMode.Single.ToString(), importer.spriteImportMode.ToString()));
                }

                if (rule.PixelsPerUnit > 0f)
                {
                    Expect("Pixels Per Unit", rule.PixelsPerUnit, importer.spritePixelsPerUnit);
                }

                if (rule.Pivot.HasValue || rule.MeshType.HasValue)
                {
                    var settings = new TextureImporterSettings();
                    importer.ReadTextureSettings(settings);
                    if (rule.Pivot.HasValue) Expect("Pivot", rule.Pivot.Value, (SpriteAlignment)settings.spriteAlignment);
                    if (rule.MeshType.HasValue) Expect("Mesh Type", rule.MeshType.Value, settings.spriteMeshType);
                }
            }

            return result;
        }

        /// <summary>Writes the rule's settings to the importer. Caller records undo and saves.</summary>
        public static void Apply(Rule rule, TextureImporter importer)
        {
            importer.textureType = rule.TextureKind == Kind.Sprite ? TextureImporterType.Sprite
                : rule.TextureKind == Kind.Gui ? TextureImporterType.GUI
                : TextureImporterType.Default;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.npotScale = TextureImporterNPOTScale.None;
            if (rule.Wrap.HasValue) importer.wrapMode = rule.Wrap.Value;

            if (rule.TextureKind != Kind.Sprite)
            {
                return;
            }

            if (!(rule.AllowMultiple && importer.spriteImportMode == SpriteImportMode.Multiple))
            {
                importer.spriteImportMode = SpriteImportMode.Single;
            }

            if (rule.PixelsPerUnit > 0f) importer.spritePixelsPerUnit = rule.PixelsPerUnit;
            if (rule.Pivot.HasValue || rule.MeshType.HasValue)
            {
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                if (rule.Pivot.HasValue) settings.spriteAlignment = (int)rule.Pivot.Value;
                if (rule.MeshType.HasValue) settings.spriteMeshType = rule.MeshType.Value;
                importer.SetTextureSettings(settings);
            }
        }
    }
}
