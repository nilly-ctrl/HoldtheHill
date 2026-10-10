using System;
using System.Collections.Generic;
using UnityEditor;

namespace HoldTheHill.Sandbox.NillyCtrl.Editor
{
    /// <summary>
    /// Missing scripts and missing prefab or asset references in sandbox prefabs, scenes and
    /// assets. Reads the YAML text rather than opening scenes, so it never disturbs the Editor.
    /// </summary>
    public static class MissingReferenceCheck
    {
        private const string Name = "MissingReference";

        public static void Run(ProjectTextIndex index, List<ValidationFinding> findings)
        {
            var resolves = new Dictionary<string, bool>(StringComparer.Ordinal);
            var scriptProblem = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (ProjectTextEntry entry in index.Entries)
            {
                var missing = new Dictionary<string, int>(StringComparer.Ordinal);
                int danglingLocal = 0;

                foreach (YamlReference reference in entry.References)
                {
                    if (reference.IsLocal)
                    {
                        if (!entry.Anchors.Contains(reference.FileId))
                        {
                            danglingLocal++;
                        }

                        continue;
                    }

                    if (YamlReferenceScanner.IsBuiltInGuid(reference.Guid))
                    {
                        continue;
                    }

                    if (!Resolves(reference.Guid, resolves))
                    {
                        string key = reference.Property + "|" + reference.Guid;
                        missing.TryGetValue(key, out int count);
                        missing[key] = count + 1;
                        continue;
                    }

                    if (reference.Property == "m_Script")
                    {
                        string problem = ScriptProblem(reference.Guid, scriptProblem);
                        if (problem != null)
                        {
                            string key = "script|" + reference.Guid;
                            if (!missing.ContainsKey(key))
                            {
                                missing[key] = 1;
                                GrayboxProjectValidator.Add(findings, ValidationSeverity.Error, Name, problem, entry.AssetPath);
                            }
                        }
                    }
                }

                foreach (KeyValuePair<string, int> pair in missing)
                {
                    if (pair.Key.StartsWith("script|", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    int bar = pair.Key.IndexOf('|');
                    string property = pair.Key.Substring(0, bar);
                    string guid = pair.Key.Substring(bar + 1);
                    GrayboxProjectValidator.Add(findings, ValidationSeverity.Error, Name,
                        Describe(property, guid, pair.Value), entry.AssetPath);
                }

                if (danglingLocal > 0)
                {
                    GrayboxProjectValidator.Add(findings, ValidationSeverity.Warning, Name,
                        $"{danglingLocal} reference(s) inside the file point at an object that is not in it " +
                        "(a deleted component or game object)", entry.AssetPath);
                }
            }
        }

        private static string Describe(string property, string guid, int count)
        {
            string times = count > 1 ? $" ({count} times)" : string.Empty;
            switch (property)
            {
                case "m_Script":
                    return $"Missing script: the script with GUID {guid} no longer exists{times}. " +
                           "Its file was deleted, or its .meta was regenerated with a new GUID.";
                case "m_SourcePrefab":
                    return $"Missing prefab: this is an instance or variant of a prefab with GUID {guid} that no longer exists{times}.";
                case "m_CorrespondingSourceObject":
                    return $"Missing prefab: its source prefab (GUID {guid}) no longer exists{times}.";
                default:
                    string field = property.Length > 0 ? $"'{property}'" : "a list entry";
                    return $"Missing reference: {field} points at an asset (GUID {guid}) that no longer exists{times}.";
            }
        }

        private static bool Resolves(string guid, Dictionary<string, bool> cache)
        {
            if (!cache.TryGetValue(guid, out bool ok))
            {
                ok = !string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid));
                cache[guid] = ok;
            }

            return ok;
        }

        // Null when the script resolves to a loadable class, else the message to report.
        private static string ScriptProblem(string guid, Dictionary<string, string> cache)
        {
            if (cache.TryGetValue(guid, out string known))
            {
                return known;
            }

            string problem = null;
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                var script = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                if (script == null || script.GetClass() == null)
                {
                    problem = $"Script '{path}' is used as a component but has no class Unity can load. " +
                              "Check that the file name matches the class name and that the project compiles.";
                }
            }

            cache[guid] = problem;
            return problem;
        }
    }
}
