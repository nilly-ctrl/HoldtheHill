using HoldTheHill.Features.Economy;
using HoldTheHill.Features.DamageNumbers;
using UnityEngine;

namespace HoldTheHill.Features.PowerUps
{
    /// <summary>A pickup effect that starts a power-up when the pickup is collected.</summary>
    [AddComponentMenu("Hold the Hill/Graybox Power-Up Gain")]
    public class PowerUpGain : PickupEffect
    {
        [SerializeField] private PowerUpData _powerUp;

        public PowerUpData PowerUp => _powerUp;

        public override void Apply(Pickup pickup)
        {
            if (PowerUpEffect.Activate(_powerUp) != null)
            {
                DamageNumberSpawner.ShowText(_powerUp.DisplayName.ToUpperInvariant(), DamageNumberKind.Critical, pickup.transform.position);
            }
        }
    }
}
