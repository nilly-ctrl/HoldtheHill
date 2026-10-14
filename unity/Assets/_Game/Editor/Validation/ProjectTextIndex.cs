using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HoldTheHill.EditorTools.Validation
{
    /// <summary>
    /// Reads every text-serialized prefab, scene and asset under a folder once, so the checks can
    /// share what they found: which GUIDs each file points at, and which files point at an asset.
    /// </summary>
    public sealed class ProjectTextIndex
    {
        private static readonly string[] Extensions =
        {
            ".prefab", ".unity", ".asset", ".mat", ".controller", ".overrideController", ".anim", ".playable"
        };

        private const long MaxBytes = 64L * 1024L * 1024L;

        private readonly Dictionary<string, ProjectTextEntry> _byPath =
            new Dictionary<string, ProjectTextEntry>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, List<string>> _referencedBy =
            new Dictionary<string, List<string>>(StringComparer.Ordinal);

        public IEnumerable<ProjectTextEntry> Entries => _byPath.Values;

        /// <summary>Files that were skipped because they are binary-serialized or too large.</summary>
        public List<string> Skipped { get; } = new List<string>();

        public ProjectTextEntry Get(string assetPath)
        {
            return assetPath != null && _byPath.TryGetValue(assetPath, out ProjectTextEntry entry) ? entry : null;
        }

        /// <summary>Paths of the files (other than the asset itself) that reference the GUID.</summary>
        public IReadOnlyList<string> ReferencedBy(string guid)
        {
            return guid != null && _referencedBy.TryGetValue(guid, out List<string> list)
                ? list
                : (IReadOnlyList<string>)Array.Empty<string>();
        }

        /// <summary>The asset's raw reference in the named field, or null when it holds none.</summary>
        public YamlReference FieldReference(string assetPath, string property)
        {
            ProjectTextEntry entry = Get(assetPath);
            if (entry == null)
            {
                return null;
            }

            foreach (YamlReference reference in entry.References)
            {
                if (reference.Property == property)
                {
                    return reference;
                }
            }

            return null;
        }

        public static string AbsolutePath(string assetPath)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath) ?? string.Empty;
            return Path.Combine(projectRoot, assetPath);
        }

        public static ProjectTextIndex Build(string rootAssetFolder)
        {
            var index = new ProjectTextIndex();
            string[] guids = AssetDatabase.FindAssets(string.Empty, new[] { rootAssetFolder });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path) || !HasTextExtension(path))
                {
                    continue;
                }

                string absolute = AbsolutePath(path);
                if (!File.Exists(absolute))
                {
                    continue;
                }

                if (new FileInfo(absolute).Length > MaxBytes)
                {
                    index.Skipped.Add(path + " (too large)");
                    continue;
                }

                string text;
                try
                {
                    text = File.ReadAllText(absolute);
                }
                catch (IOException)
                {
                    index.Skipped.Add(path + " (unreadable)");
                    continue;
                }

                if (!YamlReferenceScanner.IsUnityYaml(text))
                {
                    index.Skipped.Add(path + " (not text YAML)");
                    continue;
                }

                var entry = new ProjectTextEntry(
                    path, YamlReferenceScanner.FindReferences(text), YamlReferenceScanner.FindAnchors(text));
                index._byPath[path] = entry;
            }

            // Second pass, once every file's own GUID is known, so self references can be skipped.
            foreach (ProjectTextEntry entry in index._byPath.Values)
            {
                string ownGuid = AssetDatabase.AssetPathToGUID(entry.AssetPath);
                var seen = new HashSet<string>(StringComparer.Ordinal);

                foreach (YamlReference reference in entry.References)
                {
                    if (reference.Guid == null || reference.Guid == ownGuid || !seen.Add(reference.Guid))
                    {
                        continue;
                    }

                    if (!index._referencedBy.TryGetValue(reference.Guid, out List<string> list))
                    {
                        list = new List<string>();
                        index._referencedBy[reference.Guid] = list;
                    }

                    list.Add(entry.AssetPath);
                }
            }

            return index;
        }

        private static bool HasTextExtension(string path)
        {
            foreach (string ext in Extensions)
            {
                if (path.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
