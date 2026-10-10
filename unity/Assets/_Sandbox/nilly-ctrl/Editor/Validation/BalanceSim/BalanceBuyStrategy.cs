namespace HoldTheHill.Sandbox.NillyCtrl.Editor
{
    /// <summary>How the model spends gold between waves.</summary>
    public enum BalanceBuyStrategy
    {
        /// <summary>
        /// Cycles through the damaging towers from cheapest to dearest, one each, like a player who
        /// builds a mix. Saves gold when the next one in the cycle is too dear.
        /// </summary>
        RoundRobin,

        /// <summary>Always buys the tower with the most damage per gold. Optimistic: it stacks one type.</summary>
        BestValue
    }
}
