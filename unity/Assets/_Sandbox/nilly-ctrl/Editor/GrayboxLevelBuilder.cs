using System;
using System.Collections.Generic;
using System.Linq;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Towers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Builds a playable scene for a hand-drawn level: Tools > Hold the Hill > Levels.
    /// </summary>
    /// <remarks>
    /// A level starts as a text map in <c>Levels/Source~/maps/</c>. <c>build_level.py --install</c>
    /// draws its floor (<c>Levels/&lt;Name&gt;/&lt;Name&gt;_Ground.png</c>) and writes the route and
    /// the blocked cells (<c>&lt;Name&gt;.json</c>). This takes the graybox combat scene, which
    /// already has the towers, enemies, waves, economy and HUD wired up, and swaps the map
    /// under them: new waypoints, the drawn floor in place of the grass and trail, the spawner,
    /// hill and burrow moved to the new ends, the camera fitted to the map, and a
    /// <see cref="GrayboxBuildMask"/> for the cells nothing can stand on. It rebuilds the graybox
    /// scene first (the same as Build Graybox Combat Test) and saves the level as its own scene.
    /// </remarks>
    internal static class GrayboxLevelBuilder
    {
        private const string LevelRoot = "Assets/_Sandbox/nilly-ctrl/Levels";
        private const string GrayboxScene = "Assets/_Sandbox/nilly-ctrl/Graybox/GrayboxCombatTest.unity";
        private static readonly string[] Required =
        {
            "Enemy Path", "Enemy Spawner", "Main Camera", "Tower Placer & Inspector", "GrayboxGameFlow",
        };

        // Above EnemyPath's own flat road (-10), below everything that moves.
        private const int GroundOrder = -9;

        [Serializable]
        private class Point
        {
            public float x;
            public float y;
        }

        [Serializable]
        private class LevelData
        {
            public string name;
            public int width;
            public int height;
            public Point centre;
            public Point[] waypoints;
            public Point[] hill;
            public Point[] blocked;
            public Point[] crumbs;
            public Point[] spills;
        }

        [MenuItem("Tools/Hold the Hill/Levels/Build Kitchen Floor")]
        public static void BuildKitchenFloor() => Build("KitchenFloor");

        public static string ScenePath(string level) => $"{LevelRoot}/{level}/{level}.unity";

        public static void Build(string level)
        {
            string folder = $"{LevelRoot}/{level}";
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>($"{folder}/{level}.json");
            var ground = AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/{level}_Ground.png");
            if (json == null || ground == null)
            {
                throw new InvalidOperationException(
                    $"[Level] {folder} has no {level}.json or {level}_Ground.png. " +
                    "Run Levels/Source~/build_level.py with --install first.");
            }

            LevelData data = JsonUtility.FromJson<LevelData>(json.text);
            if (data.waypoints == null || data.waypoints.Length < 2)
            {
                throw new InvalidOperationException($"[Level] {level}.json has no route.");
            }

            // Always from a fresh graybox, so the level has whatever the graybox has today. A saved
            // graybox scene can be older than the scripts (it once lacked the game flow object).
            GrayboxBuilder.Build();

            Scene scene = EditorSceneManager.OpenScene(GrayboxScene, OpenSceneMode.Single);
            Dictionary<string, GameObject> roots = scene.GetRootGameObjects()
                .GroupBy(go => go.name).ToDictionary(g => g.Key, g => g.First());

            GameObject Root(string name)
            {
                if (!roots.TryGetValue(name, out GameObject go))
                {
                    throw new InvalidOperationException(
                        $"[Level] The graybox scene has no '{name}' object. Has GrayboxBuilder changed?");
                }

                return go;
            }

            foreach (string name in Required)
            {
                Root(name);
            }

            Vector3 first = ToWorld(data.waypoints[0]);
            var centre = new Vector3(data.centre.x, data.centre.y, 0f);

            // Route.
            Transform path = Root("Enemy Path").transform;
            for (int i = path.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(path.GetChild(i).gameObject);
            }

            for (int i = 0; i < data.waypoints.Length; i++)
            {
                var waypoint = new GameObject($"Waypoint {i}");
                waypoint.transform.SetParent(path, false);
                waypoint.transform.position = ToWorld(data.waypoints[i]);
            }

            // Floor: the drawn map replaces the grass, the dirt trail and the meadow decor.
            foreach (string name in new[] { "Ground", "Decor" })
            {
                if (roots.TryGetValue(name, out GameObject old))
                {
                    UnityEngine.Object.DestroyImmediate(old);
                }
            }

            var floor = new GameObject("Ground");
            floor.transform.position = centre;
            var renderer = floor.AddComponent<SpriteRenderer>();
            renderer.sprite = ground;
            renderer.sortingOrder = GroundOrder;

            // The graybox's showcase towers stand where its own trail left room. Here the player builds.
            foreach (GameObject go in scene.GetRootGameObjects())
            {
                if (go.GetComponent<Tower>() != null)
                {
                    UnityEngine.Object.DestroyImmediate(go);
                }
            }

            // Ends of the route.
            GameObject spawner = Root("Enemy Spawner");
            spawner.transform.position = first;
            GrayboxBuilder.Apply(spawner.GetComponent<EnemySpawner>(), so =>
            {
                so.FindProperty("activeMapId").stringValue = level;
            });

            if (roots.TryGetValue("Burrow", out GameObject burrow))
            {
                burrow.transform.position = first;
            }

            if (roots.TryGetValue("Hill", out GameObject hill) && data.hill.Length > 0)
            {
                hill.transform.position = new Vector3(
                    data.hill.Average(p => p.x), data.hill.Average(p => p.y), 0f);
            }

            // Camera: the whole map on a 16:9 screen, with a thin margin.
            var camera = Root("Main Camera").GetComponent<Camera>();
            camera.transform.position = new Vector3(centre.x, centre.y, -10f);
            camera.orthographicSize = Mathf.Max(data.height * 0.5f, data.width * 0.5f * 9f / 16f) + 0.2f;

            // Where nothing can be built.
            IEnumerable<Vector2Int> blocked = data.blocked.Concat(data.crumbs).Concat(data.spills).Concat(data.hill)
                .Select(p => new Vector2Int(Mathf.RoundToInt(p.x), Mathf.RoundToInt(p.y)));
            var bounds = new RectInt(-(data.width / 2), data.height / 2 - (data.height - 1), data.width, data.height);
            new GameObject("Build Mask").AddComponent<GrayboxBuildMask>().Configure(blocked, bounds);

            GameObject label = scene.GetRootGameObjects().FirstOrDefault(go => go.name.StartsWith("--- GENERATED"));
            if (label != null)
            {
                label.name = $"--- GENERATED BY Tools > Hold the Hill > Levels ({data.name}) ---";
            }

            string scenePath = ScenePath(level);
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Level] Built {scenePath} from {level}.json: {data.waypoints.Length} waypoints, " +
                      $"{data.width} x {data.height} cells. Open it and press Play.");
        }

        private static Vector3 ToWorld(Point p) => new Vector3(p.x, p.y, 0f);
    }
}
