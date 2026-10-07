using HoldTheHill.Features.Combat;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// A bombardier beetle. Where it dies it leaves a boiling puddle, and towers standing in
    /// reach of the puddle cannot attack until it cools.
    /// </summary>
    public class GrayboxBombardier : GrayboxSpecialEnemy
    {
        [SerializeField, Min(0.5f)] private float _scaldSeconds = 5f;
        [SerializeField, Min(0.5f)] private float _scaldRadius = 2.2f;
        [SerializeField, Range(0.05f, 0.9f)] private float _boilBelow = 0.35f;

        private bool _boiled;

        protected override void OnEnable()
        {
            base.OnEnable();
            _boiled = false;
        }

        protected override void OnHurt(DamageInfo info, float applied)
        {
            // The tell: it swells and glows before it goes.
            if (!_boiled && Alive && Health.HealthFraction < _boilBelow)
            {
                _boiled = true;
                Play("Boil");
            }
        }

        protected override void OnDied()
        {
            GrayboxScald.Spawn(Library, transform.position, _scaldSeconds, _scaldRadius);
        }
    }
}
