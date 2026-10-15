namespace HoldTheHill.Features.Towers
{
    /// <summary>
    /// Which enemy a tower shoots when several are in range. Players expect to set this
    /// per tower: a slow, heavy tower wants Strongest, a chip-damage tower wants Weakest
    /// so it finishes things off rather than wasting shots.
    /// </summary>
    public enum TargetingPriority
    {
        /// <summary>Furthest along the path, so closest to the Queen. The usual default.</summary>
        First,

        /// <summary>Least far along the path, hitting new arrivals first.</summary>
        Last,

        /// <summary>Nearest to the tower in a straight line.</summary>
        Closest,

        /// <summary>Most health remaining.</summary>
        Strongest,

        /// <summary>Least health remaining, for finishing off wounded enemies.</summary>
        Weakest
    }
}
