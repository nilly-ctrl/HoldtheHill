namespace HoldTheHill.Sandbox.NillyCtrl.Editor
{
    /// <summary>One problem the project validator found.</summary>
    public sealed class ValidationFinding
    {
        public ValidationFinding(ValidationSeverity severity, string check, string message, string assetPath)
        {
            Severity = severity;
            Check = check ?? string.Empty;
            Message = message ?? string.Empty;
            AssetPath = assetPath ?? string.Empty;
        }

        public ValidationSeverity Severity { get; }

        /// <summary>Short name of the check that raised it, e.g. "MissingReference".</summary>
        public string Check { get; }

        public string Message { get; }

        /// <summary>Project-relative asset path ("Assets/..."), or empty when there is none.</summary>
        public string AssetPath { get; }

        /// <summary>The object to ping in the Project window. Null when the finding is not about a loadable asset.</summary>
        public UnityEngine.Object Target { get; set; }

        public override string ToString()
        {
            string where = AssetPath.Length > 0 ? $" ({AssetPath})" : string.Empty;
            return $"[{Severity}] {Check}: {Message}{where}";
        }
    }
}
