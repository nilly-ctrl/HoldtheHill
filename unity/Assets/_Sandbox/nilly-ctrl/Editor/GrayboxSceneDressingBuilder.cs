using HoldTheHill.Core.Animation;
using System.Collections.Generic;
using System.Linq;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Towers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The scene itself: camera, route, ground, grass, decor, and the hill and burrow props.
    /// </summary>
    internal static class GrayboxSceneDressingBuilder
    {
        // The same S-shaped route the Tools menu builds, in grid cells.
        internal static readonly Vector2Int[] Route =
        {
            new Vector2Int(-10, 2), new Vector2Int(-4, 2), new Vector2Int(-4, -3),
            new Vector2Int(2, -3), new Vector2Int(2, 3), new Vector2Int(9, 3),
        };

        internal static void BuildCamera()
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";

            // Framed on the path rather than the world origin. The route spans y -2.5
            // to 3.5, so its centre is 0.5; at size 8 more than half the view was empty
            // background and the projectiles were too small to follow.
            go.transform.position = new Vector3(0f, 0.5f, -10f);

            Camera camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.16f, 0.17f, 0.19f);

            go.AddComponent<AudioListener>();
        }

        internal const string TileRoot = GrayboxBuildKit.SandboxRoot + "/Tiles";

        /// <summary>
        /// Grass under everything and a dirt trail along the route. EnemyPath still draws its
        /// own flat road (sorting -10); this sits just above it with the same geometry.
        /// </summary>
        internal static void BuildGround(EnemyPath path)
        {
            var grass = AssetDatabase.LoadAssetAtPath<Sprite>(TileRoot + "/GroundGrass.png");
            var road = AssetDatabase.LoadAssetAtPath<Sprite>(TileRoot + "/RoadDirt.png");
            var fill = AssetDatabase.LoadAssetAtPath<Sprite>(TileRoot + "/RoadDirtFill.png");
            if (grass == null || road == null || fill == null)
            {
                Debug.LogWarning($"[Graybox] Ground tiles missing in {TileRoot}; keeping the plain background.");
                return;
            }

            var root = new GameObject("Ground");
            Tiled(root.transform, "Grass", grass, new Vector3(0f, 0.5f, 0f), 0f, new Vector2(40f, 24f), -20);

            float width = new SerializedObject(path).FindProperty("_roadWidth").floatValue;
            for (int i = 0; i < Route.Length - 1; i++)
            {
                Vector3 a = new Vector3(Route[i].x + 0.5f, Route[i].y + 0.5f, 0f);
                Vector3 b = new Vector3(Route[i + 1].x + 0.5f, Route[i + 1].y + 0.5f, 0f);
                Vector3 delta = b - a;
                float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                Tiled(root.transform, $"Road {i}", road, (a + b) * 0.5f, angle, new Vector2(delta.magnitude + width, width), -9);
            }

            // Plain dirt over each bend, so one segment's edge line doesn't run across the next.
            for (int i = 1; i < Route.Length - 1; i++)
            {
                Vector3 corner = new Vector3(Route[i].x + 0.5f, Route[i].y + 0.5f, 0f);
                Tiled(root.transform, $"Bend {i}", fill, corner, 0f, new Vector2(width, width), -8);
            }
        }

        /// <summary>Just the grass, for scenes with no trail (the weapon picker).</summary>
        internal static void BuildGrass(Vector3 centre, Vector2 size)
        {
            var grass = AssetDatabase.LoadAssetAtPath<Sprite>(TileRoot + "/GroundGrass.png");
            if (grass != null)
            {
                Tiled(new GameObject("Ground").transform, "Grass", grass, centre, 0f, size, -20);
            }
        }

        // How many of each piece to scatter, and which clips (variants) it has.
        private static readonly (string Key, string[] Clips, int Count)[] DecorTable =
        {
            ("DecorGrass", new[] { "A", "B", "C" }, 46),
            ("DecorFlower", new[] { "A", "B", "C" }, 12),
            ("DecorPebble", new[] { "A", "B", "C" }, 14),
            ("DecorLeaf", new[] { "A", "B", "C" }, 9),
            ("DecorTwig", new[] { "A", "B" }, 6),
            ("DecorMushroom", new[] { "A", "B" }, 5),
            ("DecorRoot", new[] { "A", "B" }, 3),
        };

        /// <summary>
        /// Scatters grass tufts, flowers, pebbles, leaves, twigs, mushrooms and roots over the
        /// grass, clear of the trail, and breaks up the trail's edges. Everything sits below the
        /// towers and enemies, so anything placed later simply draws over it. Seeded, so a
        /// rebuild lays the same map.
        /// </summary>
        internal static void BuildDecor(EnemyPath path)
        {
            if (GrayboxBuildKit.Anim == null || GrayboxBuildKit.Anim.Find("DecorGrass") == null)
            {
                return;
            }

            var rng = new System.Random(7);
            float Range(float min, float max) => min + (float)rng.NextDouble() * (max - min);

            float width = new SerializedObject(path).FindProperty("_roadWidth").floatValue;
            var points = new Vector2[Route.Length];
            for (int i = 0; i < Route.Length; i++)
            {
                points[i] = new Vector2(Route[i].x + 0.5f, Route[i].y + 0.5f);
            }

            float DistanceToTrail(Vector2 p)
            {
                float best = float.PositiveInfinity;
                for (int i = 0; i < points.Length - 1; i++)
                {
                    Vector2 a = points[i], ab = points[i + 1] - points[i];
                    float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
                    best = Mathf.Min(best, Vector2.Distance(p, a + ab * t));
                }

                return best;
            }

            var root = new GameObject("Decor");
            foreach ((string key, string[] clips, int count) in DecorTable)
            {
                SpriteAnimSet set = GrayboxBuildKit.Anim.Find(key);
                if (set == null)
                {
                    continue;
                }

                int placed = 0;
                for (int attempt = 0; attempt < count * 20 && placed < count; attempt++)
                {
                    var at = new Vector2(Range(-10.5f, 10.5f), Range(-4.8f, 5.8f));
                    if (DistanceToTrail(at) < width * 0.5f + 0.6f)
                    {
                        continue;
                    }

                    string clip = clips[rng.Next(clips.Length)];
                    PlaceDecor(root.transform, set, key, clip, at, 0f, rng.Next(2) == 0, -6);
                    placed++;
                }
            }

            // Trail edges: a piece every unit or so along both sides of every stretch.
            SpriteAnimSet edges = GrayboxBuildKit.Anim.Find("DecorTrailEdge");
            if (edges == null)
            {
                return;
            }

            string[] edgeClips = { "A", "A", "B", "C" }; // grass overhang twice as often
            for (int i = 0; i < points.Length - 1; i++)
            {
                Vector2 delta = points[i + 1] - points[i];
                Vector2 along = delta.normalized;
                var normal = new Vector2(-along.y, along.x);
                float angle = Mathf.Atan2(along.y, along.x) * Mathf.Rad2Deg;

                for (float d = 1f; d < delta.magnitude - 0.5f; d += 1f)
                {
                    foreach (int side in new[] { 1, -1 })
                    {
                        if (rng.NextDouble() < 0.35)
                        {
                            continue;
                        }

                        // The art has the trail under it; on the far side turn it round.
                        Vector2 at = points[i] + along * (d + Range(-0.2f, 0.2f)) + normal * (side * width * 0.5f);
                        PlaceDecor(root.transform, edges, "DecorTrailEdge", edgeClips[rng.Next(edgeClips.Length)],
                            at, side > 0 ? angle : angle + 180f, false, -7);
                    }
                }
            }
        }

        internal static void PlaceDecor(
            Transform parent, SpriteAnimSet set, string key, string clipName, Vector2 at, float angle, bool flip, int order)
        {
            SpriteAnimClip clip = set.Find(clipName);
            if (clip == null || clip.Frames.Length == 0)
            {
                return;
            }

            var go = new GameObject($"{key} {clipName}");
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, 0f, angle));
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = clip.Frames[0];
            renderer.flipX = flip;
            renderer.sortingOrder = order;

            // Only the pieces that move (grass, flowers) need a player.
            if (clip.Frames.Length > 1)
            {
                go.AddComponent<SpriteClipPlayer>().Configure(GrayboxBuildKit.Anim, key, clipName);
            }
        }

        internal static void Tiled(Transform parent, string name, Sprite sprite, Vector3 position, float angle, Vector2 size, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = size;
            renderer.sortingOrder = order;
        }

        /// <summary>The hill at the end of the trail and the burrow at the start.</summary>
        internal static void BuildProps()
        {
            if (GrayboxBuildKit.Anim == null)
            {
                return;
            }

            Vector2Int first = Route[0];
            Vector2Int last = Route[Route.Length - 1];

            // Hill: drawn above the enemies so they vanish into it; nudged in from the screen edge.
            MakeProp("Hill", "PropHill", "Healthy", new Vector3(last.x - 0.3f, last.y + 0.5f, 0f), 4, GrayboxPropVisuals.Kind.Hill);
            // Burrow: under the enemies, so they climb out of it.
            MakeProp("Burrow", "PropBurrow", "Idle", new Vector3(first.x + 0.5f, first.y + 0.5f, 0f), 1, GrayboxPropVisuals.Kind.Burrow);
        }

        internal static void MakeProp(string name, string key, string clip, Vector3 position, int order, GrayboxPropVisuals.Kind kind)
        {
            if (GrayboxBuildKit.Anim.Find(key) == null)
            {
                return;
            }

            var go = new GameObject(name);
            go.transform.position = position;
            go.AddComponent<SpriteRenderer>().sortingOrder = order;
            go.AddComponent<SpriteClipPlayer>().Configure(GrayboxBuildKit.Anim, key, clip);
            go.AddComponent<GrayboxPropVisuals>().Configure(kind);
        }

        internal static EnemyPath BuildPath()
        {
            var go = new GameObject("Enemy Path");
            EnemyPath path = go.AddComponent<EnemyPath>();
            go.AddComponent<PathClearanceBlocker>();

            for (int i = 0; i < Route.Length; i++)
            {
                var waypoint = new GameObject($"Waypoint {i}");
                waypoint.transform.SetParent(go.transform);
                waypoint.transform.position = new Vector3(Route[i].x + 0.5f, Route[i].y + 0.5f, 0f);
            }

            return path;
        }
    }
}
