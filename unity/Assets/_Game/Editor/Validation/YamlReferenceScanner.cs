using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace HoldTheHill.EditorTools.Validation
{
    /// <summary>
    /// Finds object references in Unity text-serialized YAML (prefabs, scenes, assets) without
    /// opening them in the Editor. Pure C#.
    /// </summary>
    public static class YamlReferenceScanner
    {
        // Unity's two built-in resource GUIDs. They always resolve and never name a project file.
        private const string BuiltInExtra = "0000000000000000e000000000000000";
        private const string BuiltInDefault = "0000000000000000f000000000000000";

        private static readonly Regex ReferencePattern = new Regex(
            @"(?:(?<prop>[A-Za-z_][\w\.]*):\s*)?\{fileID:\s*(?<fid>-?\d+)(?:,\s*guid:\s*(?<guid>[0-9a-fA-F]{32}))?(?:,\s*type:\s*\d+)?\}",
            RegexOptions.Compiled);

        private static readonly Regex AnchorPattern = new Regex(
            @"^--- !u!\d+ &(?<id>-?\d+)", RegexOptions.Compiled);

        /// <summary>True for a Unity text YAML file (it starts with the %YAML header).</summary>
        public static bool IsUnityYaml(string text)
        {
            return text != null && text.StartsWith("%YAML");
        }

        /// <summary>Every non-null reference in the file. A reference with fileID 0 is "none" and is skipped.</summary>
        public static List<YamlReference> FindReferences(string yaml)
        {
            var found = new List<YamlReference>();
            if (string.IsNullOrEmpty(yaml))
            {
                return found;
            }

            string[] lines = yaml.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.IndexOf("{fileID:", System.StringComparison.Ordinal) < 0)
                {
                    continue;
                }

                foreach (Match m in ReferencePattern.Matches(line))
                {
                    if (!long.TryParse(m.Groups["fid"].Value, out long fileId) || fileId == 0)
                    {
                        continue;
                    }

                    string guid = m.Groups["guid"].Success ? m.Groups["guid"].Value.ToLowerInvariant() : null;
                    found.Add(new YamlReference(m.Groups["prop"].Value, fileId, guid, i + 1));
                }
            }

            return found;
        }

        /// <summary>The fileIDs of the objects the file itself defines (the numbers after '&amp;' on each document header).</summary>
        public static HashSet<long> FindAnchors(string yaml)
        {
            var anchors = new HashSet<long>();
            if (string.IsNullOrEmpty(yaml))
            {
                return anchors;
            }

            foreach (string raw in yaml.Split('\n'))
            {
                Match m = AnchorPattern.Match(raw);
                if (m.Success && long.TryParse(m.Groups["id"].Value, out long id))
                {
                    anchors.Add(id);
                }
            }

            return anchors;
        }

        /// <summary>True for a GUID that names Unity's built-in resources, which are never project files.</summary>
        public static bool IsBuiltInGuid(string guid)
        {
            return guid == BuiltInExtra || guid == BuiltInDefault;
        }
    }
}
