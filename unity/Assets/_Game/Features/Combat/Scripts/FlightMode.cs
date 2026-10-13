namespace HoldTheHill.Features.Combat
{
    /// <summary>How a projectile travels once it leaves the tower.</summary>
    public enum FlightMode
    {
        /// <summary>Straight line in the direction it was fired.</summary>
        Linear,

        /// <summary>Steers toward its target. Overshoots and loops back if the target dodges.</summary>
        Homing,

        /// <summary>Parabolic arc to a landing point, ignoring whatever is in between.</summary>
        Lobbed,

        /// <summary>Resolves the moment it is fired, with no travel time.</summary>
        Instant,

        /// <summary>Hovers in place, picks a target, then dives into it at speed.</summary>
        SeekAndDestroy
    }
}
