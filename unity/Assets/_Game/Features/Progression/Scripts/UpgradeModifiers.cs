namespace HoldTheHill.Features.Progression
{
    /// <summary>
    /// Where game code looks up upgrade bonuses. Whatever upgrade system is running registers itself
    /// here; with nothing registered, <see cref="Current"/> returns neutral values (no bonuses).
    /// </summary>
    /// <remarks>
    /// This keeps Features code from depending on any one upgrade implementation, including
    /// sandbox prototypes, which live in a different assembly and can't be referenced from here.
    /// </remarks>
    public static class UpgradeModifiers
    {
        private static IUpgradeModifiers _source;

        /// <summary>The active modifiers, or neutral values if no upgrade system is registered.</summary>
        public static IUpgradeModifiers Current => _source ?? Neutral.Instance;

        /// <summary>Makes <paramref name="source"/> the active modifier provider.</summary>
        public static void Register(IUpgradeModifiers source)
        {
            _source = source;
        }

        /// <summary>Removes <paramref name="source"/> if it is the active provider.</summary>
        public static void Unregister(IUpgradeModifiers source)
        {
            if (_source == source)
            {
                _source = null;
            }
        }

        private sealed class Neutral : IUpgradeModifiers
        {
            public static readonly Neutral Instance = new Neutral();

            public float DamageMultiplier => 1f;
            public float FrostDebuffDamageMultiplier => 1f;
            public float FireRateMultiplier => 1f;
            public float FrostSlowBonus => 0f;
            public float KnockbackBonus => 0f;
            public float RefundPercentage => 0.75f;
        }
    }
}
