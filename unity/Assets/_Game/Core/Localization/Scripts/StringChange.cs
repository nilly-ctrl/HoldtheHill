namespace HoldTheHill.Core.Localization
{
    /// <summary>One cell-level difference between a string table and an incoming CSV.</summary>
    public readonly struct StringChange
    {
        public enum ChangeKind
        {
            /// <summary>The key or that language's text is not in the table yet.</summary>
            Added,

            /// <summary>The table has different text for this key and language.</summary>
            Changed,
        }

        public readonly ChangeKind Kind;
        public readonly string Key;
        public readonly string Language;
        public readonly string OldText;
        public readonly string NewText;

        public StringChange(ChangeKind kind, string key, string language, string oldText, string newText)
        {
            Kind = kind;
            Key = key;
            Language = language;
            OldText = oldText;
            NewText = newText;
        }

        public override string ToString()
        {
            return Kind == ChangeKind.Added
                ? $"+ {Key} [{Language}] \"{NewText}\""
                : $"~ {Key} [{Language}] \"{OldText}\" -> \"{NewText}\"";
        }
    }
}
