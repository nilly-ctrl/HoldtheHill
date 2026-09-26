using UnityEngine;

namespace HoldTheHill.Features.Combat
{
    /// <summary>How a hit should be treated. Resistances key off this.</summary>
    public enum DamageType
    {
        Physical,
        Magic,
        True,
        Fire,
        Poison,
        Lightning
    }

    /// <summary>
    /// One instance of damage travelling from a source to a target.
    /// A struct so hits cost no allocation during heavy waves.
    /// </summary>
    public struct DamageInfo
    {
        /// <summary>Raw damage before the target's own resistances.</summary>
        public float Amount;

        /// <summary>What caused the hit. May be null if the source was already recycled.</summary>
        public GameObject Source;

        /// <summary>Where the hit landed, for impact effects and knockback direction.</summary>
        public Vector2 HitPoint;

        /// <summary>Which resistance applies.</summary>
        public DamageType Type;

        public DamageInfo(float amount, GameObject source, Vector2 hitPoint, DamageType type = DamageType.Physical)
        {
            Amount = amount;
            Source = source;
            HitPoint = hitPoint;
            Type = type;
        }
    }
}
