namespace HoldTheHill.Sandbox.NillyCtrl.Editor
{
    /// <summary>The knobs of the analytic balance model. Defaults mirror the gray-box scene.</summary>
    public sealed class BalanceSettings
    {
        /// <summary>
        /// Length of the enemy route in world units. 30 is the gray-box route (the Route array in
        /// GrayboxBuilder: 6 + 5 + 6 + 6 + 7 cells).
        /// </summary>
        public float PathLength = 30f;

        /// <summary>Gold at the start of the run. 500 is GrayboxEconomy's default.</summary>
        public int StartingGold = 500;

        /// <summary>The most towers the model builds. Real maps have a limited number of build cells.</summary>
        public int MaxTowers = 20;

        public BalanceBuyStrategy Strategy = BalanceBuyStrategy.RoundRobin;

        /// <summary>
        /// How far a tower sits from the middle of the road, in world units. A tower covers a chord
        /// of the route of half-length sqrt(range^2 - offset^2). 1 is one cell away from the road.
        /// </summary>
        public float PathOffset = 1f;

        /// <summary>Gold the player gets when a wave is cleared, on top of bounties. The gray-box pays none.</summary>
        public int WaveClearBonus = 0;

        /// <summary>Scales every bounty (skill tree and permanent upgrades raise this in the real game).</summary>
        public float BountyMultiplier = 1f;

        /// <summary>Scales every tower's damage (skill tree). 1 is unmodified.</summary>
        public float DamageMultiplier = 1f;

        /// <summary>Hill health. 100 is GrayboxBaseHealth's default; the model notes the wave it would fall in.</summary>
        public float HillHealth = 100f;

        /// <summary>Seconds per step of the wave simulation.</summary>
        public float TimeStep = 0.05f;

        /// <summary>A wave is called under-tuned when at least this fraction of its enemies leak.</summary>
        public float UnderTunedLeakFraction = 0.05f;

        /// <summary>A wave with no leaks is called over-tuned when enemies die, on average, within this fraction of the route.</summary>
        public float OverTunedKillDepth = 0.35f;

        /// <summary>Longest a wave may run, in simulated seconds, before stragglers are counted as leaks.</summary>
        public float MaxWaveSeconds = 600f;
    }
}
