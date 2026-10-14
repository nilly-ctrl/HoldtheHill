using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HoldTheHill.EditorTools.Validation
{
    /// <summary>
    /// C# files that define more than one MonoBehaviour, or one whose file name does not match its
    /// class. Either way Unity can only attach the class named like the file, so prefabs lose the rest.
    /// </summary>
    public static class ScriptLayoutCheck
    {
        private const string Name = "ScriptLayout";

        public static void Run(string root, List<ValidationFinding> findings)
        {
            var monoNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (Type type in TypeCache.GetTypesDerivedFrom<MonoBehaviour>())
            {
                if (!type.IsAbstract)
                {
                    monoNames.Add(type.Name);
                }
            }

            foreach (string guid in AssetDatabase.FindAssets("t:MonoScript", new[] { root }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                string absolute = ProjectTextIndex.AbsolutePath(path);
                if (!File.Exists(absolute))
                {
                    continue;
                }

                List<string> behaviours =
                    SourceClassScanner.MonoBehaviourNames(File.ReadAllText(absolute), monoNames.Contains);

                if (behaviours.Count == 0)
                {
                    continue;
                }

                string fileName = Path.GetFileNameWithoutExtension(path);

                if (behaviours.Count > 1)
                {
                    ProjectValidator.Add(findings, ValidationSeverity.Error, Name,
                        $"Defines {behaviours.Count} MonoBehaviours ({string.Join(", ", behaviours)}). " +
                        "Unity attaches only the one named like the file; give each its own file.", path);
                }
                else if (behaviours[0] != fileName)
                {
                    ProjectValidator.Add(findings, ValidationSeverity.Error, Name,
                        $"MonoBehaviour '{behaviours[0]}' is in a file named '{fileName}.cs'. " +
                        "The file name must match the class name or prefabs lose the script.", path);
                }
            }
        }
    }
}
