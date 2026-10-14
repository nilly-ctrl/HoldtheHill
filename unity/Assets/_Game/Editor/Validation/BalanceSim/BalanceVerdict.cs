namespace HoldTheHill.EditorTools.Validation
{
    /// <summary>The model's call on one wave.</summary>
    public enum BalanceVerdict
    {
        /// <summary>Enemies die, mostly in the middle of the route, with no or few leaks.</summary>
        OnTarget,

        /// <summary>Too many enemies leak: the defence is too weak for this wave.</summary>
        UnderTuned,

        /// <summary>Nothing leaks and enemies die early: the defence is more than this wave needs.</summary>
        OverTuned
    }
}
