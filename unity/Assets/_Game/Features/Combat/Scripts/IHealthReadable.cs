namespace HoldTheHill.Features.Combat
{
    /// <summary>
    /// An <see cref="IDamageable"/> that will also tell you how much health it has left.
    /// </summary>
    /// <remarks>
    /// Kept separate from <see cref="IDamageable"/> on purpose. Dealing damage to something
    /// does not require knowing its health, and plenty of damageable things (scenery, shields)
    /// have no meaningful "strongest" reading. Towers targeting by Strongest or Weakest check
    /// for this interface and fall back to distance when a target does not implement it.
    /// </remarks>
    public interface IHealthReadable
    {
        /// <summary>Health remaining right now.</summary>
        float CurrentHealth { get; }

        /// <summary>Health when at full.</summary>
        float MaxHealth { get; }
    }
}
