using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl.Editor
{
    /// <summary>
    /// Checks the sandbox for the mistakes that cost time later: missing scripts and prefabs, data
    /// assets with empty required fields, assets no catalog lists, broken prefab variants, duplicate
    /// ids, impossible skill prerequisites, sprites without a .meta and C# files holding several
    /// MonoBehaviours. Read-only: it never changes an asset.
    /// </summary>
    public static class GrayboxProjectValidator
    {
        /// <summary>The folder every check is limited to.</summary>
        public const string SandboxRoot = "Assets/_Sandbox/nilly-ctrl";

        /// <summary>Runs every check and returns the findings, worst first within each check.</summary>
        public static List<ValidationFinding> Run()
        {
            return Run(SandboxRoot);
        }

        public static List<ValidationFinding> Run(string root)
        {
            var findings = new List<ValidationFinding>();
            bool showProgress = !Application.isBatchMode;

            try
            {
                Progress(showProgress, 0.05f, "Reading prefabs, scenes and assets");
                ProjectTextIndex index = ProjectTextIndex.Build(root);
                foreach (string skipped in index.Skipped)
                {
                    Add(findings, ValidationSeverity.Info, "Index", "Not scanned for references: " + skipped, string.Empty);
                }

                Progress(showProgress, 0.25f, "Missing scripts and references");
                MissingReferenceCheck.Run(index, findings);

                Progress(showProgress, 0.40f, "Prefab variants");
                VariantPrefabCheck.Run(root, findings);

                Progress(showProgress, 0.55f, "Data assets");
                DataAssetCheck.Run(root, findings);

                Progress(showProgress, 0.70f, "Catalogs");
                CatalogMembershipCheck.Run(root, index, findings);

                Progress(showProgress, 0.80f, "Ids and prerequisites");
                IdAndPrerequisiteCheck.Run(root, index, findings);

                Progress(showProgress, 0.90f, ".meta files");
                MetaFileCheck.Run(root, findings);

                Progress(showProgress, 0.95f, "Script layout");
                ScriptLayoutCheck.Run(root, findings);
            }
            finally
            {
                if (showProgress)
                {
                    EditorUtility.ClearProgressBar();
                }
            }

            findings.Sort((a, b) => b.Severity.CompareTo(a.Severity));
            return findings;
        }

        /// <summary>
        /// Batch entry: <c>-executeMethod HoldTheHill.Sandbox.NillyCtrl.Editor.GrayboxProjectValidator.RunBatch</c>.
        /// Logs every finding and exits with 1 if there is any error, else 0. Automated runs must go
        /// through the Unity session lock, and not while someone has the project open:
        /// <c>unity-session-lock.sh &lt;unity-root&gt; run &lt;session-id&gt; "validator" -- -batchmode -nographics -executeMethod ...RunBatch</c>.
        /// </summary>
        public static void RunBatch()
        {
            int exitCode = 1;
            try
            {
                List<ValidationFinding> findings = Run();
                foreach (ValidationFinding finding in findings)
                {
                    switch (finding.Severity)
                    {
                        case ValidationSeverity.Error:
                            Debug.LogError("[Validator] " + finding);
                            break;
                        case ValidationSeverity.Warning:
                            Debug.LogWarning("[Validator] " + finding);
                            break;
                        default:
                            Debug.Log("[Validator] " + finding);
                            break;
                    }
                }

                Debug.Log("[Validator] " + ValidationReport.Summary(findings));
                exitCode = ValidationReport.ExitCode(findings);
            }
            catch (Exception exception)
            {
                Debug.LogError("[Validator] failed: " + exception);
            }

            EditorApplication.Exit(exitCode);
        }

        // ---------- helpers shared by the checks ----------

        internal static void Add(List<ValidationFinding> findings, ValidationSeverity severity,
            string check, string message, string assetPath)
        {
            var finding = new ValidationFinding(severity, check, message, assetPath);
            if (!string.IsNullOrEmpty(assetPath) && assetPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                finding.Target = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            }

            findings.Add(finding);
        }

        /// <summary>Every asset of type <typeparamref name="T"/> under the folder, with its path.</summary>
        internal static List<(string path, T asset)> FindAll<T>(string root) where T : UnityEngine.Object
        {
            var found = new List<(string, T)>();
            foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { root }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                {
                    found.Add((path, asset));
                }
            }

            return found;
        }

        /// <summary>The serialized string in a private field, without the property's fallbacks.</summary>
        internal static string RawString(UnityEngine.Object asset, string field)
        {
            SerializedProperty property = new SerializedObject(asset).FindProperty(field);
            return property != null ? property.stringValue : string.Empty;
        }

        private static void Progress(bool show, float amount, string label)
        {
            if (show)
            {
                EditorUtility.DisplayProgressBar("Hold the Hill validator", label, amount);
            }
        }
    }
}
