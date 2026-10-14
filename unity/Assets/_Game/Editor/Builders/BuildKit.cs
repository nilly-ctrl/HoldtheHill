using HoldTheHill.Features.Specials;
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

namespace HoldTheHill.EditorTools.Builders
{
    /// <summary>
    /// Shared helpers for the graybox builders: folders, serialized edits, prefab saving,
    /// placeholder sprites and the animation library every generated object animates with.
    /// </summary>
    internal static class BuildKit
    {
        internal const string GameRoot = "Assets/_Game";

        internal const string FeaturesRoot = GameRoot + "/Features";

        internal const string DevScenesRoot = GameRoot + "/Scenes/Dev";

        /// <summary>
        /// Where a generated prefab lives: the first folder of its name picks the feature
        /// (Towers, PowerUps, Warden, Hud, Combat, Enemies; Pickups belong to Economy), the rest is
        /// the path inside that feature's Prefabs folder. A bare name is an enemy.
        /// </summary>
        internal static string PrefabPath(string fileName)
        {
            int slash = fileName.IndexOf('/');
            string kind = slash < 0 ? "Enemies" : fileName.Substring(0, slash);
            string rest = slash < 0 ? fileName : fileName.Substring(slash + 1);
            string feature = kind == "Pickups" ? "Economy" : kind;
            string folder = $"{FeaturesRoot}/{feature}/Prefabs";
            int sub = rest.LastIndexOf('/');
            EnsureFolder(sub < 0 ? folder : $"{folder}/{rest.Substring(0, sub)}");
            return $"{folder}/{rest}.prefab";
        }

        /// <summary>Creates a folder and its parents under Assets if they are missing.</summary>
        internal static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            int slash = path.LastIndexOf('/');
            EnsureFolder(path.Substring(0, slash));
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }

        internal static void CreateFolderIfMissing(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{child}"))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        internal static void Apply(Object target, System.Action<SerializedObject> edit)
        {
            var so = new SerializedObject(target);
            edit(so);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static GameObject SavePrefab(GameObject instance, string fileName)
        {
            // Two of the same script on one prefab is always a mistake here, and a quiet one: a
            // [RequireComponent] auto-added a default EnemyHealth ahead of the configured one, and
            // every enemy ran on 30 HP. Refuse to save rather than build a broken graybox.
            foreach (IGrouping<System.Type, MonoBehaviour> group in instance.GetComponents<MonoBehaviour>().GroupBy(c => c.GetType()))
            {
                if (group.Count() > 1)
                {
                    throw new System.InvalidOperationException(
                        $"[Build] {fileName} has {group.Count()} {group.Key.Name} components. One was probably auto-added by a [RequireComponent].");
                }
            }

            // Changes made straight to a component (not through a SerializedObject) only become
            // overrides on a variant once they are recorded.
            if (PrefabUtility.IsPartOfPrefabInstance(instance))
            {
                foreach (Component component in instance.GetComponentsInChildren<Component>(true))
                {
                    PrefabUtility.RecordPrefabInstancePropertyModifications(component);
                }
            }

            string path = PrefabPath(fileName);
            GameObject asset = PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
            return asset;
        }

        internal static void AddSprite(GameObject go, Sprite sprite, Color color, float size, int sortingOrder)
        {
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = new Vector2(size, size);
        }

        // Which .aseprite each generated object animates with. Anything missing here (or a
        // missing library) keeps its flat placeholder shape, so the scene still builds.
        internal static readonly Dictionary<string, string> AnimKeys = new Dictionary<string, string>
        {
            { "Runner", "EnemyRunner" },
            { "Grunt", "EnemyGrunt" },
            { "Brute", "EnemyBrute" },
            { "Shielded", "EnemyShielded" },
            { "Swarm", "EnemySwarm" },
            { "Splitter", "EnemySplitter" },
            { "Healer", "EnemyHealer" },
            { "TitanBeetle", "EnemyBoss" },
            { "MantisQueen", "EnemyMantisQueen" },
            { "Hornet", "EnemyHornet" },
            { "OrbWeaver", "EnemyOrbWeaver" },
            { "BoulderBug", "EnemyBoulderBug" },
            { "RivalQueen", "EnemyRivalQueen" },
            { "Wasp", "EnemyWasp" },
            { "Spider", "EnemySpider" },
            { "Silverfish", "EnemySilverfish" },
            { "ThiefAnt", "EnemyThiefAnt" },
            { "Bombardier", "EnemyBombardier" },
        };

        /// <summary>The animation library of the build in progress; set by <see cref="CombatTestSceneBuilder.Build"/>.</summary>
        internal static SpriteAnimLibrary Anim { get; set; }

        /// <summary>
        /// Swaps a prefab's placeholder circle for an animated sprite. With a visual scale the
        /// sprite goes on a child, so scaling it never touches the root's collider.
        /// </summary>
        internal static void AnimateVisual(GameObject go, string key, string clip, float visualScale = 1f)
        {
            if (Anim == null || Anim.Find(key) == null)
            {
                return;
            }

            var rootRenderer = go.GetComponent<SpriteRenderer>();
            GameObject target = go;
            if (!Mathf.Approximately(visualScale, 1f))
            {
                target = new GameObject("Visual");
                target.transform.SetParent(go.transform, false);
                target.transform.localScale = Vector3.one * visualScale;
                var childRenderer = target.AddComponent<SpriteRenderer>();
                childRenderer.sortingOrder = rootRenderer != null ? rootRenderer.sortingOrder : 0;
                if (rootRenderer != null)
                {
                    Object.DestroyImmediate(rootRenderer);
                }
            }
            else if (rootRenderer != null)
            {
                rootRenderer.drawMode = SpriteDrawMode.Simple;
                rootRenderer.color = Color.white;
                go.transform.localScale = Vector3.one; // undo the rescale Unity applies when leaving Sliced
            }

            target.AddComponent<SpriteClipPlayer>().Configure(Anim, key, clip);
        }

        internal static void Animate(GameObject go)
        {
            if (Anim == null || !AnimKeys.TryGetValue(go.name, out string key))
            {
                return;
            }

            EnemyAnimator.Attach(go, Anim, key);
        }
    }
}
