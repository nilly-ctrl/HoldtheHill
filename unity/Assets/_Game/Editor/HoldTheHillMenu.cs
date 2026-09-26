using HoldTheHill.Features.Enemies;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HoldTheHill.EditorTools
{
    /// <summary>Adds a Tools > Hold the Hill menu to the Unity editor.</summary>
    internal static class HoldTheHillMenu
    {
        // An S-shaped route in grid cells. It enters off the left edge of the default
        // 2D camera view and leaves off the right edge.
        private static readonly Vector2Int[] ExampleRoute =
        {
            new Vector2Int(-10, 2), new Vector2Int(-4, 2), new Vector2Int(-4, -3),
            new Vector2Int(2, -3), new Vector2Int(2, 3), new Vector2Int(9, 3),
        };

        [MenuItem("Tools/Hold the Hill/Create Example Path")]
        private static void CreateExamplePath()
        {
            EnemyPath existing = Object.FindAnyObjectByType<EnemyPath>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                EditorGUIUtility.PingObject(existing);
                Debug.LogWarning("This scene already has an Enemy Path. Move its waypoints to edit it, or delete it first.", existing);
                return;
            }

            var pathObject = new GameObject("Enemy Path");
            pathObject.AddComponent<EnemyPath>();

            for (int i = 0; i < ExampleRoute.Length; i++)
            {
                Transform waypoint = new GameObject($"Waypoint {i}").transform;
                waypoint.SetParent(pathObject.transform);
                // Waypoints sit in the middle of grid cells (cell size 1).
                waypoint.position = new Vector3(ExampleRoute[i].x + 0.5f, ExampleRoute[i].y + 0.5f, 0f);
            }

            Undo.RegisterCreatedObjectUndo(pathObject, "Create Example Path");
            Selection.activeGameObject = pathObject;
            EditorSceneManager.MarkSceneDirty(pathObject.scene);
        }
    }
}
