using System.Collections.Generic;
using System.IO;

namespace HoldTheHill.Sandbox.NillyCtrl.Editor
{
    /// <summary>
    /// Files and folders under the sandbox without a .meta (a sprite without one gets a new GUID on
    /// every machine, so prefab references to it break), and .meta files whose asset is gone.
    /// </summary>
    public static class MetaFileCheck
    {
        private const string Name = "MetaFile";

        public static void Run(string rootAssetFolder, List<ValidationFinding> findings)
        {
            string absoluteRoot = ProjectTextIndex.AbsolutePath(rootAssetFolder);
            if (!Directory.Exists(absoluteRoot))
            {
                return;
            }

            foreach (string entry in Directory.EnumerateFileSystemEntries(absoluteRoot, "*", SearchOption.AllDirectories))
            {
                string relative = ToAssetPath(entry, absoluteRoot, rootAssetFolder);
                if (MetaFileRules.IsIgnoredByUnity(relative))
                {
                    continue;
                }

                if (MetaFileRules.IsMetaFile(entry))
                {
                    string owner = MetaFileRules.AssetPathForMeta(entry);
                    if (!File.Exists(owner) && !Directory.Exists(owner))
                    {
                        GrayboxProjectValidator.Add(findings, ValidationSeverity.Warning, Name,
                            "Orphan .meta: the asset it belongs to is gone.", relative);
                    }

                    continue;
                }

                if (File.Exists(entry + ".meta"))
                {
                    continue;
                }

                bool isFolder = Directory.Exists(entry);
                ValidationSeverity severity = !isFolder && MetaFileRules.IsSpriteLike(entry)
                    ? ValidationSeverity.Error
                    : ValidationSeverity.Warning;

                string what = isFolder ? "Folder" : MetaFileRules.IsSpriteLike(entry) ? "Sprite or icon file" : "File";
                GrayboxProjectValidator.Add(findings, severity, Name,
                    $"{what} has no .meta file, so Unity will give it a new GUID on import.", relative);
            }
        }

        private static string ToAssetPath(string absolute, string absoluteRoot, string rootAssetFolder)
        {
            string tail = absolute.Substring(absoluteRoot.Length).Replace('\\', '/').TrimStart('/');
            return rootAssetFolder.TrimEnd('/') + "/" + tail;
        }
    }
}
