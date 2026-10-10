namespace HoldTheHill.Sandbox.NillyCtrl.Editor
{
    /// <summary>How serious a validator finding is. Errors make the batch entry exit non-zero.</summary>
    public enum ValidationSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2
    }
}
