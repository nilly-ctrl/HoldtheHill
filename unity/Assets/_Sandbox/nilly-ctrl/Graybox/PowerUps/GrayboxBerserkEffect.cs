using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Berserk Berry: while it lasts the Warden's Shove hits three times as hard, reaches
    /// further and strips shields (docs/level-up-plan: "a triple damage wide shove that strips
    /// shields"). Does nothing without a Warden in the scene.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Graybox/Power-Ups/Berserk Effect")]
    public class GrayboxBerserkEffect : GrayboxPowerUpEffect
    {
        [SerializeField, Min(1f)] private float _damageMultiplier = 3f;
        [SerializeField, Min(1f)] private float _radiusMultiplier = 1.5f;

        private GrayboxWardenShove _shove;

        protected override void OnBegin()
        {
            _shove = GrayboxWarden.Instance != null ? GrayboxWarden.Instance.GetComponentInChildren<GrayboxWardenShove>() : null;
            if (_shove != null)
            {
                _shove.DamageMultiplier = _damageMultiplier;
                _shove.RadiusMultiplier = _radiusMultiplier;
                _shove.StripsShields = true;
            }
        }

        protected override void OnEnd()
        {
            if (_shove != null)
            {
                _shove.DamageMultiplier = 1f;
                _shove.RadiusMultiplier = 1f;
                _shove.StripsShields = false;
            }
        }
    }
}
