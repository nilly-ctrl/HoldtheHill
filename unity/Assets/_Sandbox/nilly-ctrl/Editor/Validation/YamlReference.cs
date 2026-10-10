namespace HoldTheHill.Sandbox.NillyCtrl.Editor
{
    /// <summary>One <c>{fileID: ..., guid: ...}</c> reference found in a Unity YAML file.</summary>
    public sealed class YamlReference
    {
        public YamlReference(string property, long fileId, string guid, int line)
        {
            Property = property ?? string.Empty;
            FileId = fileId;
            Guid = guid;
            Line = line;
        }

        /// <summary>The field the reference sits in, e.g. m_Script. Empty for a bare list item.</summary>
        public string Property { get; }

        public long FileId { get; }

        /// <summary>The target asset's GUID, or null for a reference inside the same file.</summary>
        public string Guid { get; }

        /// <summary>1-based line number in the file.</summary>
        public int Line { get; }

        public bool IsLocal => Guid == null;
    }
}
