using HoldTheHill.Features.Levels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Tilemaps;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace HoldTheHill.EditorTools.Pipeline
{
    /// <summary>
    /// Turns the sheets written by Tiles/Source~/build_tile_sets.py into things you can paint
    /// with: sliced sprites, one ground tile and one Rule Tile per terrain, and a Tile Palette
    /// per set. Reads Tiles/Sets/TileSets.json. Safe to run again after regenerating the art:
    /// sprite ids come from the sprite names, and existing tile assets are updated in place.
    /// </summary>
    public static class TileSetBuilder
    {
        internal const string Root = "Assets/_Sandbox/nilly-ctrl/Tiles/Sets";
        internal const float PixelsPerUnit = 32f;

        // Bit of the neighbour mask for each direction (y up), matching build_tile_sets.py.
        internal static readonly (int bit, Vector3Int offset)[] Neighbours =
        {
            (1, new Vector3Int(0, 1, 0)), (2, new Vector3Int(1, 1, 0)), (4, new Vector3Int(1, 0, 0)),
            (8, new Vector3Int(1, -1, 0)), (16, new Vector3Int(0, -1, 0)), (32, new Vector3Int(-1, -1, 0)),
            (64, new Vector3Int(-1, 0, 0)), (128, new Vector3Int(-1, 1, 0)),
        };

        [Serializable]
        internal class Manifest
        {
            public int cell;
            public int columns;
            public int[] blob;
            public int extraFills;
            public SetInfo[] sets;
        }

        [Serializable]
        internal class SetInfo
        {
            public string name;
            public bool checker;
            public bool mirror;
            public SheetInfo[] grounds;
            public SheetInfo[] terrains;
            public string[] map;
        }

        [Serializable]
        internal class SheetInfo
        {
            public string name;
            public int count;
        }

        [MenuItem("Tools/Hold the Hill/Build Tile Sets")]
        public static void BuildAll()
        {
            Manifest manifest = ReadManifest();
            foreach (SetInfo set in manifest.sets)
            {
                if (AssetDatabase.IsValidFolder($"{Root}/{set.name}"))
                {
                    BuildSet(manifest, set);
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[TileSetBuilder] Built {manifest.sets.Length} tile sets under {Root}.");
        }

        internal static Manifest ReadManifest()
        {
            return JsonUtility.FromJson<Manifest>(File.ReadAllText($"{Root}/TileSets.json"));
        }

        private static void BuildSet(Manifest manifest, SetInfo set)
        {
            string folder = $"{Root}/{set.name}";
            var paletteTiles = new List<TileBase>();

            var groundSprites = new List<Sprite[]>();
            foreach (SheetInfo ground in set.grounds)
            {
                var names = Enumerable.Range(0, ground.count).Select(i => $"{ground.name}_{i}").ToList();
                Dictionary<string, Sprite> sprites = Slice($"{folder}/{ground.name}.png", names, manifest);
                groundSprites.Add(names.Select(n => sprites[n]).ToArray());
            }

            if (set.checker)
            {
                var tile = LoadOrCreate<GroundVariantTile>($"{folder}/{set.name}FloorTile.asset");
                tile.even = groundSprites[0];
                tile.odd = groundSprites[1];
                tile.plainCount = 1;
                tile.plainShare = 0.9f;
                tile.mirror = false;
                EditorUtility.SetDirty(tile);
                paletteTiles.Add(tile);
            }
            else
            {
                for (int i = 0; i < set.grounds.Length; i++)
                {
                    var tile = LoadOrCreate<GroundVariantTile>($"{folder}/{set.grounds[i].name}Tile.asset");
                    tile.even = groundSprites[i];
                    tile.odd = new Sprite[0];
                    tile.mirror = set.mirror;
                    EditorUtility.SetDirty(tile);
                    paletteTiles.Add(tile);
                }
            }

            foreach (SheetInfo terrain in set.terrains)
            {
                var names = manifest.blob.Select(m => $"{terrain.name}_{m:000}").ToList();
                names.AddRange(Enumerable.Range(1, manifest.extraFills).Select(i => $"{terrain.name}_Fill{i}"));
                Dictionary<string, Sprite> sprites = Slice($"{folder}/{terrain.name}.png", names, manifest);

                var tile = LoadOrCreate<RuleTile>($"{folder}/{terrain.name}Tile.asset");
                tile.m_DefaultSprite = sprites[$"{terrain.name}_255"];
                tile.m_DefaultColliderType = Tile.ColliderType.None;
                tile.m_TilingRules = manifest.blob.Select((mask, index) => Rule(mask, index, terrain.name, sprites, manifest)).ToList();
                EditorUtility.SetDirty(tile);
                paletteTiles.Add(tile);
            }

            BuildPalette(folder, set.name, paletteTiles);
        }

        /// <summary>One rule per blob tile. A corner is only tested when both sides next to it match.</summary>
        private static RuleTile.TilingRule Rule(int mask, int index, string terrain, Dictionary<string, Sprite> sprites, Manifest manifest)
        {
            var rule = new RuleTile.TilingRule
            {
                m_Id = index,
                m_ColliderType = Tile.ColliderType.None,
                m_Neighbors = new List<int>(),
                m_NeighborPositions = new List<Vector3Int>(),
            };
            for (int i = 0; i < Neighbours.Length; i++)
            {
                bool corner = i % 2 == 1;
                if (corner)
                {
                    int before = Neighbours[i - 1].bit;
                    int after = Neighbours[(i + 1) % Neighbours.Length].bit;
                    if ((mask & before) == 0 || (mask & after) == 0)
                    {
                        continue;
                    }
                }

                rule.m_NeighborPositions.Add(Neighbours[i].offset);
                rule.m_Neighbors.Add((mask & Neighbours[i].bit) != 0
                    ? RuleTile.TilingRuleOutput.Neighbor.This
                    : RuleTile.TilingRuleOutput.Neighbor.NotThis);
            }

            Sprite plain = sprites[$"{terrain}_{mask:000}"];
            if (mask == 255 && manifest.extraFills > 0)
            {
                // Open water: mostly the plain tile, now and then one with a detail on it.
                var options = new List<Sprite> { plain, plain, plain };
                options.AddRange(Enumerable.Range(1, manifest.extraFills).Select(i => sprites[$"{terrain}_Fill{i}"]));
                rule.m_Output = RuleTile.TilingRuleOutput.OutputSprite.Random;
                rule.m_Sprites = options.ToArray();
                rule.m_PerlinScale = 0.9f;
            }
            else
            {
                rule.m_Output = RuleTile.TilingRuleOutput.OutputSprite.Single;
                rule.m_Sprites = new[] { plain };
            }

            return rule;
        }

        private static Dictionary<string, Sprite> Slice(string path, List<string> names, Manifest manifest)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null)
            {
                throw new FileNotFoundException("Tile sheet missing. Run Tiles/Source~/build_tile_sets.py first.", path);
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteExtrude = 0;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);

            importer.GetSourceTextureWidthAndHeight(out int _, out int height);
            int cell = manifest.cell;
            var rects = new List<SpriteRect>();
            for (int i = 0; i < names.Count; i++)
            {
                int column = i % manifest.columns;
                int row = i / manifest.columns;
                rects.Add(new SpriteRect
                {
                    name = names[i],
                    spriteID = StableId(names[i]),
                    rect = new Rect(column * cell, height - (row + 1) * cell, cell, cell),
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                });
            }

            var factory = new SpriteDataProviderFactories();
            factory.Init();
            ISpriteEditorDataProvider data = factory.GetSpriteEditorDataProviderFromObject(importer);
            data.InitSpriteEditorDataProvider();
            data.SetSpriteRects(rects.ToArray());
            data.GetDataProvider<ISpriteNameFileIdDataProvider>()
                .SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)).ToList());
            data.Apply();
            importer.SaveAndReimport();

            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(s => s.name);
        }

        /// <summary>The same id for the same sprite name on every machine, so references survive a rebuild.</summary>
        private static GUID StableId(string name)
        {
            using MD5 md5 = MD5.Create();
            byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes("HoldTheHill.TileSet." + name));
            return new GUID(string.Concat(hash.Select(b => b.ToString("x2"))));
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }

            return asset;
        }

        private static void BuildPalette(string folder, string setName, List<TileBase> tiles)
        {
            string path = $"{folder}/{setName}Palette.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                GridPaletteUtility.CreateNewPalette(folder, setName + "Palette", GridLayout.CellLayout.Rectangle,
                    GridPalette.CellSizing.Manual, Vector3.one, GridLayout.CellSwizzle.XYZ);
            }

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var tilemap = root.GetComponentInChildren<Tilemap>();
                tilemap.ClearAllTiles();
                for (int i = 0; i < tiles.Count; i++)
                {
                    // Leave a gap between tiles so each Rule Tile shows as a single pond, not a joined strip.
                    tilemap.SetTile(new Vector3Int(i * 2, 0, 0), tiles[i]);
                }

                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

    }
}
