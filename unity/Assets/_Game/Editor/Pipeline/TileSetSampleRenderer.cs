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

using Manifest = HoldTheHill.EditorTools.Pipeline.TileSetBuilder.Manifest;
using SetInfo = HoldTheHill.EditorTools.Pipeline.TileSetBuilder.SetInfo;
using SheetInfo = HoldTheHill.EditorTools.Pipeline.TileSetBuilder.SheetInfo;

namespace HoldTheHill.EditorTools.Pipeline
{
    /// <summary>
    /// Pictures of the tile sets for review: a painted sample per set rendered to a PNG, and the demo scenes. Reads the same manifest as <see cref="TileSetBuilder"/>.
    /// </summary>
    internal static class TileSetSampleRenderer
    {
        /// <summary>
        /// Paints each set's sample map (the one in TileSets.json) with the real tile assets and
        /// saves a picture of it, to compare with Source~/Preview. For batch runs:
        /// -executeMethod HoldTheHill.EditorTools.Pipeline.TileSetSampleRenderer.RenderSamples -tileShots &lt;folder&gt;
        /// </summary>
        public static void RenderSamples()
        {
            string[] args = Environment.GetCommandLineArgs();
            int at = Array.IndexOf(args, "-tileShots");
            string outFolder = at >= 0 && at + 1 < args.Length ? args[at + 1] : "Temp/TileShots";
            Directory.CreateDirectory(outFolder);
            Manifest manifest = TileSetBuilder.ReadManifest();
            int wrong = 0;

            foreach (SetInfo set in manifest.sets)
            {
                string folder = $"{TileSetBuilder.Root}/{set.name}";
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    continue;
                }

                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                int height = set.map.Length;
                int width = set.map[0].Length;
                Tilemap[] layers = PaintSample(set, folder, 1);

                for (int t = 0; t < set.terrains.Length; t++)
                {
                    char letter = (char)('A' + t);
                    string terrain = set.terrains[t].name;
                    Tilemap layer = layers[t];
                    bool Is(int x, int row) => set.map[Mathf.Clamp(row, 0, height - 1)][Mathf.Clamp(x, 0, width - 1)] == letter;
                    for (int row = 0; row < height; row++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            if (!Is(x, row))
                            {
                                continue;
                            }

                            int mask = 0;
                            foreach ((int bit, Vector3Int offset) in TileSetBuilder.Neighbours)
                            {
                                if (Is(x + offset.x, row - offset.y))
                                {
                                    mask |= bit;
                                }
                            }

                            for (int i = 1; i < TileSetBuilder.Neighbours.Length; i += 2)
                            {
                                int both = TileSetBuilder.Neighbours[i - 1].bit | TileSetBuilder.Neighbours[(i + 1) % TileSetBuilder.Neighbours.Length].bit;
                                if ((mask & both) != both)
                                {
                                    mask &= ~TileSetBuilder.Neighbours[i].bit;
                                }
                            }

                            Sprite shown = layer.GetSprite(new Vector3Int(x, height - 1 - row, 0));
                            string expected = $"{terrain}_{mask:000}";
                            bool ok = shown != null && (shown.name == expected || (mask == 255 && shown.name.StartsWith(terrain + "_Fill")));
                            if (!ok)
                            {
                                wrong++;
                                Debug.LogError($"[TileSetBuilder] {terrain} at ({x},{row}): expected {expected}, got {(shown ? shown.name : "nothing")}");
                            }
                        }
                    }
                }

                Camera camera = SampleCamera(width, height);
                camera.backgroundColor = Color.magenta;
                var target = new RenderTexture(width * manifest.cell, height * manifest.cell, 24, RenderTextureFormat.ARGB32);
                target.filterMode = FilterMode.Point;
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                var shot = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
                shot.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                shot.Apply();
                RenderTexture.active = null;
                File.WriteAllBytes(Path.Combine(outFolder, set.name + ".png"), shot.EncodeToPNG());
            }

            Debug.Log($"[TileSetBuilder] Sample maps rendered to {outFolder}; cells showing the wrong tile: {wrong}");
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(wrong == 0 ? 0 : 3);
            }
        }

        /// <summary>
        /// One scene per set, Sets/&lt;Set&gt;/&lt;Set&gt;Demo.unity, with the sample map painted on layered
        /// Tilemaps: somewhere to open, look and keep painting. Rebuilding replaces the scene.
        /// </summary>
        [MenuItem("Tools/Hold the Hill/Build Tile Set Demo Scenes")]
        public static void BuildDemoScenes()
        {
            Manifest manifest = TileSetBuilder.ReadManifest();
            foreach (SetInfo set in manifest.sets)
            {
                string folder = $"{TileSetBuilder.Root}/{set.name}";
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    continue;
                }

                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                PaintSample(set, folder, 0);
                Camera camera = SampleCamera(set.map[0].Length, set.map.Length);
                camera.tag = "MainCamera";
                camera.backgroundColor = new Color(0.1f, 0.1f, 0.12f);
                EditorSceneManager.SaveScene(scene, $"{folder}/{set.name}Demo.unity");
            }

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Debug.Log("[TileSetBuilder] Demo scenes written.");
        }

        /// <summary>
        /// Paints the set's sample map: a Ground Tilemap, then one Tilemap per terrain in the order
        /// of the manifest (returned). <paramref name="margin"/> repeats the map's outer cells that
        /// many cells past its edge, so shapes touching the edge run off it.
        /// </summary>
        internal static Tilemap[] PaintSample(SetInfo set, string folder, int margin)
        {
            var grid = new GameObject("Grid").AddComponent<Grid>();
            int height = set.map.Length;
            int width = set.map[0].Length;

            string groundName = set.checker ? set.name + "Floor" : set.grounds[0].name;
            Tilemap ground = Layer(grid, "Ground", 0);
            var groundTile = AssetDatabase.LoadAssetAtPath<TileBase>($"{folder}/{groundName}Tile.asset");
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    ground.SetTile(new Vector3Int(x, y, 0), groundTile);
                }
            }

            var layers = new Tilemap[set.terrains.Length];
            for (int t = 0; t < set.terrains.Length; t++)
            {
                char letter = (char)('A' + t);
                string terrain = set.terrains[t].name;
                layers[t] = Layer(grid, terrain, t + 1);
                var tile = AssetDatabase.LoadAssetAtPath<TileBase>($"{folder}/{terrain}Tile.asset");
                for (int row = -margin; row < height + margin; row++)
                {
                    for (int x = -margin; x < width + margin; x++)
                    {
                        if (set.map[Mathf.Clamp(row, 0, height - 1)][Mathf.Clamp(x, 0, width - 1)] == letter)
                        {
                            layers[t].SetTile(new Vector3Int(x, height - 1 - row, 0), tile);
                        }
                    }
                }

                layers[t].RefreshAllTiles();
            }

            return layers;
        }

        internal static Camera SampleCamera(int width, int height)
        {
            var camera = new GameObject("Camera").AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = height / 2f;
            camera.transform.position = new Vector3(width / 2f, height / 2f, -10f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            return camera;
        }

        internal static Tilemap Layer(Grid grid, string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(grid.transform, false);
            var tilemap = go.AddComponent<Tilemap>();
            go.AddComponent<TilemapRenderer>().sortingOrder = order;
            return tilemap;
        }
    }
}
