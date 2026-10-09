using System.Collections.Generic;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Towers;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>Pushes every enemy close by back along the trail and staggers it. Barely hurts.</summary>
    [AddComponentMenu("Hold the Hill/Graybox/Warden/Shove")]
    public class GrayboxWardenShove : GrayboxWardenAbility
    {
        [SerializeField, Min(0.1f)] private float _radius = 1.3f;
        [SerializeField, Min(0f)] private float _damage = 3f;
        [Tooltip("World units back along the trail.")]
        [SerializeField, Min(0f)] private float _knockback = 1.2f;
        [SerializeField, Min(0f)] private float _staggerSeconds = 0.5f;

        private readonly List<IDamageable> _hits = new List<IDamageable>();

        protected override void Perform()
        {
            Vector3 at = Warden.transform.position;
            CombatUtil.OverlapDamageables(at, _radius, ~0, _hits);

            var stagger = new StatusEffectData
            {
                EffectName = "Stagger",
                TickInterval = 0.5f,
                Duration = _staggerSeconds,
                SlowMultiplier = 0.05f,
            };

            foreach (IDamageable hit in _hits)
            {
                if (!CombatUtil.IsAlive(hit))
                {
                    continue;
                }

                hit.TakeDamage(new DamageInfo(_damage, Warden.gameObject, hit.Transform.position, DamageType.Physical));
                if (!CombatUtil.IsAlive(hit))
                {
                    continue;
                }

                hit.ApplyStatusEffect(stagger);
                if (hit.Transform.TryGetComponent(out EnemyMover mover))
                {
                    mover.Knockback(_knockback);
                }
            }

            SpriteClipPlayer.SpawnOneShot(GrayboxSpecials.Library, "FxShockwave", "Blast", at, Quaternion.identity, 0.4f, 3);
        }
    }
}
