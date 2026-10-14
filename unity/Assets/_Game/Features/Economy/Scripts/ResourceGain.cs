using HoldTheHill.Features.DamageNumbers;
using HoldTheHill.Features.Economy;
using UnityEngine;

namespace HoldTheHill.Features.Economy
{
    /// <summary>A pickup effect that pays a resource into the wallet: a crumb of Food.</summary>
    [AddComponentMenu("Hold the Hill/Graybox Resource Gain")]
    public class ResourceGain : PickupEffect
    {
        [Tooltip("What is gained. Empty means the run's main resource.")]
        [SerializeField] private ResourceData _resource;

        [SerializeField, Min(1)] private int _amount = 5;

        public ResourceData Resource => _resource;

        public int Amount => _amount;

        public override void Apply(Pickup pickup)
        {
            WalletBehaviour economy = UnityEngine.Object.FindAnyObjectByType<WalletBehaviour>();
            if (economy != null)
            {
                economy.Earn(_resource, _amount);
            }

            DamageNumberSpawner.ShowText("+" + _amount, DamageNumberKind.Resource, pickup.transform.position);
        }
    }
}
