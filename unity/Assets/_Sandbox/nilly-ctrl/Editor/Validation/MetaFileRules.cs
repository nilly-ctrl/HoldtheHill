using System;

namespace HoldTheHill.Sandbox.NillyCtrl.Editor
{
    /// <summary>Which files Unity gives a .meta file, and which need one most. Pure C#.</summary>
    public static class MetaFileRules
    {
        private static readonly string[] SpriteExtensions =
        {
            ".png", ".jpg", ".jpeg", ".tga", ".psd", ".gif", ".bmp", ".tif", ".tiff", ".svg", ".ase", ".aseprite"
        };

        /// <summary>
        /// True for paths Unity does not import and so never gives a .meta: any segment that starts
        /// with a dot or ends with a tilde, a "cvs" folder, and .tmp files.
        /// </summary>
        public static bool IsIgnoredByUnity(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                return true;
            }

            string[] segments = relativePath.Replace('\\', '/').Split('/');
            foreach (string segment in segments)
            {
                if (segment.Length == 0)
                {
                    continue;
                }

                if (segment.StartsWith(".", StringComparison.Ordinal) ||
                    segment.EndsWith("~", StringComparison.Ordinal) ||
                    string.Equals(segment, "cvs", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return relativePath.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsMetaFile(string path)
        {
            return path != null && path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>A sprite, texture or icon source file. A missing .meta here is an error, not a warning.</summary>
        public static bool IsSpriteLike(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            foreach (string ext in SpriteExtensions)
            {
                if (path.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>The asset a .meta file belongs to: the path without the trailing ".meta".</summary>
        public static string AssetPathForMeta(string metaPath)
        {
            return IsMetaFile(metaPath) ? metaPath.Substring(0, metaPath.Length - 5) : metaPath;
        }
    }
}
