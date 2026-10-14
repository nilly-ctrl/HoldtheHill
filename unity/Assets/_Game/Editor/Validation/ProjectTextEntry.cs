using System.Collections.Generic;

namespace HoldTheHill.EditorTools.Validation
{
    /// <summary>One text-serialized asset file read for the validator: its references and its own object ids.</summary>
    public sealed class ProjectTextEntry
    {
        public ProjectTextEntry(string assetPath, List<YamlReference> references, HashSet<long> anchors)
        {
            AssetPath = assetPath;
            References = references;
            Anchors = anchors;
        }

        public string AssetPath { get; }

        public List<YamlReference> References { get; }

        public HashSet<long> Anchors { get; }
    }
}
