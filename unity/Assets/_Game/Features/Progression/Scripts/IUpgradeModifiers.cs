namespace HoldTheHill.Features.Progression
{
    /// <summary>
    /// Run-wide stat modifiers from the upgrade / skill tree. Game code reads these through
    /// <see cref="UpgradeModifiers.Current"/> and never needs to know which system supplies them.
    /// </summary>
    public interface IUpgradeModifiers
    {
        /// <summary>Multiplier on all tower damage. 1 = no bonus.</summary>
        float DamageMultiplier { get; }

        /// <summary>Extra damage multiplier against slowed enemies. 1 = no bonus.</summary>
        float FrostDebuffDamageMultiplier { get; }

        /// <summary>Multiplier on tower fire rate. 1 = no bonus.</summary>
        float FireRateMultiplier { get; }

        /// <summary>Added to Frost Aura slow strength (subtracted from its speed multiplier). 0 = no bonus.</summary>
        float FrostSlowBonus { get; }

        /// <summary>Added fraction of knockback distance. 0 = no bonus.</summary>
        float KnockbackBonus { get; }

        /// <summary>Fraction of invested cost returned when a tower is dismantled.</summary>
        float RefundPercentage { get; }
    }
}
