using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>A pickup effect that starts a power-up when the pickup is collected.</summary>
    [AddComponentMenu("Hold the Hill/Graybox/Graybox Power-Up Gain")]
    public class GrayboxPowerUpGain : GrayboxPickupEffect
    {
        [SerializeField] private GrayboxPowerUpData _powerUp;

        public GrayboxPowerUpData PowerUp => _powerUp;

        public override void Apply(GrayboxPickup pickup)
        {
            if (GrayboxPowerUpEffect.Activate(_powerUp) != null)
            {
                DamageNumberSpawner.ShowText(_powerUp.DisplayName.ToUpperInvariant(), DamageNumberKind.Critical, pickup.transform.position);
            }
        }
    }
}
