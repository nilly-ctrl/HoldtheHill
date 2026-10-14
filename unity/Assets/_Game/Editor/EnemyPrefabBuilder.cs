using HoldTheHill.Features.Enemies;
using UnityEditor;
using UnityEngine;

namespace HoldTheHill.EditorTools
{
    /// <summary>
    /// Rebuilds the Triangle enemy prefab on the shared enemy components (EnemyHealth, EnemyMover,
    /// a collider and a kinematic body) in place, so Map_01 keeps pointing at it. Safe to run
    /// again: only what is missing is added, and nothing already tuned is overwritten.
    /// Menu: Tools > Hold the Hill > Rebuild Triangle Enemy. Batch:
    /// -executeMethod HoldTheHill.EditorTools.EnemyPrefabBuilder.Build
    /// </summary>
    internal static class EnemyPrefabBuilder
    {
        private const string TrianglePath = "Assets/_Game/Features/Enemies/Scripts/EnemySpawner/Triangle.prefab";

        // Triangle's old BasicEnemy numbers: 1 health, 1 damage on arrival, speed 2.
        private const float Health = 1f;
        private const float BreachDamage = 1f;
        private const float Speed = 2f;
        private const int Bounty = 10;

        [MenuItem("Tools/Hold the Hill/Rebuild Triangle Enemy")]
        public static void Build()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(TrianglePath);
            try
            {
                // The old BasicEnemy component, whether its script still exists or is already gone.
                foreach (MonoBehaviour behaviour in root.GetComponents<MonoBehaviour>())
                {
                    if (behaviour != null && behaviour.GetType().Name == "BasicEnemy")
                    {
                        Object.DestroyImmediate(behaviour);
                    }
                }

                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(root);

                if (root.GetComponent<Collider2D>() == null)
                {
                    root.AddComponent<CircleCollider2D>().radius = 0.3f;
                }

                if (root.GetComponent<Rigidbody2D>() == null)
                {
                    Rigidbody2D body = root.AddComponent<Rigidbody2D>();
                    body.bodyType = RigidbodyType2D.Kinematic;
                    body.gravityScale = 0f;
                }

                if (root.GetComponent<EnemyHealth>() == null)
                {
                    var health = new SerializedObject(root.AddComponent<EnemyHealth>());
                    health.FindProperty("_maxHealth").floatValue = Health;
                    health.FindProperty("_bountyValue").intValue = Bounty;
                    health.FindProperty("_breachDamage").floatValue = BreachDamage;
                    health.ApplyModifiedPropertiesWithoutUndo();
                }

                if (root.GetComponent<EnemyMover>() == null)
                {
                    var mover = new SerializedObject(root.AddComponent<EnemyMover>());
                    mover.FindProperty("_speed").floatValue = Speed;
                    mover.FindProperty("_onReachEnd").enumValueIndex = (int)EnemyMover.EndBehaviour.Despawn;
                    mover.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(root, TrianglePath);
                Debug.Log($"[HoldTheHill] Rebuilt {TrianglePath} on EnemyHealth and EnemyMover.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
