using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// What a <see cref="GrayboxPickup"/> does when it is collected. A pickup can carry several.
    /// </summary>
    public abstract class GrayboxPickupEffect : MonoBehaviour
    {
        /// <summary>Called once, when the pickup this sits on is collected.</summary>
        public abstract void Apply(GrayboxPickup pickup);
    }
}
