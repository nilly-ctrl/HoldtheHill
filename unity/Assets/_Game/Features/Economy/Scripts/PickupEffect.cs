using UnityEngine;

namespace HoldTheHill.Features.Economy
{
    /// <summary>
    /// What a <see cref="Pickup"/> does when it is collected. A pickup can carry several.
    /// </summary>
    public abstract class PickupEffect : MonoBehaviour
    {
        /// <summary>Called once, when the pickup this sits on is collected.</summary>
        public abstract void Apply(Pickup pickup);
    }
}
