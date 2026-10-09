using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>A pickup effect that pays a resource into the wallet: a crumb of Food.</summary>
    [AddComponentMenu("Hold the Hill/Graybox/Graybox Resource Gain")]
    public class GrayboxResourceGain : GrayboxPickupEffect
    {
        [Tooltip("What is gained. Empty means the run's main resource.")]
        [SerializeField] private GrayboxResourceData _resource;

        [SerializeField, Min(1)] private int _amount = 5;

        public GrayboxResourceData Resource => _resource;

        public int Amount => _amount;

        public override void Apply(GrayboxPickup pickup)
        {
            if (GrayboxEconomy.Instance != null)
            {
                GrayboxEconomy.Instance.Earn(_resource, _amount);
            }

            DamageNumberSpawner.ShowText("+" + _amount, DamageNumberKind.Resource, pickup.transform.position);
        }
    }
}
