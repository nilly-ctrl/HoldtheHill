using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl.Editor
{
    /// <summary>
    /// Prefab variants whose base prefab is gone, nested prefab instances that point at a deleted
    /// prefab, and variants that add a second copy of a script their base already has.
    /// </summary>
    public static class VariantPrefabCheck
    {
        private const string Name = "PrefabVariant";

        public static void Run(string root, List<ValidationFinding> findings)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { root }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                {
                    GrayboxProjectValidator.Add(findings, ValidationSeverity.Error, Name,
                        "The prefab could not be loaded.", path);
                    continue;
                }

                PrefabAssetType type = PrefabUtility.GetPrefabAssetType(prefab);
                if (type == PrefabAssetType.MissingAsset)
                {
                    GrayboxProjectValidator.Add(findings, ValidationSeverity.Error, Name,
                        "The prefab is a variant of a prefab that no longer exists.", path);
                    continue;
                }

                if (type == PrefabAssetType.Variant)
                {
                    CheckVariant(prefab, path, findings);
                }

                CheckNestedInstances(prefab, path, findings);
            }
        }

        private static void CheckVariant(GameObject variant, string path, List<ValidationFinding> findings)
        {
            GameObject baseRoot = PrefabUtility.GetCorrespondingObjectFromSource(variant);
            if (baseRoot == null)
            {
                GrayboxProjectValidator.Add(findings, ValidationSeverity.Error, Name,
                    "Variant whose base prefab is missing.", path);
                return;
            }

            // A component the variant adds, of a script its base object already carries, is a
            // second copy: the base's values are shadowed instead of overridden.
            List<AddedComponent> added = PrefabUtility.GetAddedComponents(variant);
            foreach (AddedComponent addition in added)
            {
                Component copy = addition.instanceComponent;
                if (copy == null || !(copy is MonoBehaviour))
                {
                    continue;
                }

                GameObject baseObject = PrefabUtility.GetCorrespondingObjectFromSource(copy.gameObject);
                if (baseObject != null && baseObject.GetComponent(copy.GetType()) != null)
                {
                    GrayboxProjectValidator.Add(findings, ValidationSeverity.Warning, Name,
                        $"Variant adds its own '{copy.GetType().Name}' on '{copy.gameObject.name}', " +
                        $"but its base '{AssetDatabase.GetAssetPath(baseRoot)}' already has one. " +
                        "Override the base's component instead of copying it.", path);
                }
            }
        }

        private static void CheckNestedInstances(GameObject prefab, string path, List<ValidationFinding> findings)
        {
            foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
            {
                if (child.gameObject != prefab && PrefabUtility.IsPrefabAssetMissing(child.gameObject))
                {
                    GrayboxProjectValidator.Add(findings, ValidationSeverity.Error, Name,
                        $"'{child.name}' is an instance of a prefab that no longer exists.", path);
                }
            }
        }
    }
}
