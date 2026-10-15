using System.Collections.Generic;
using UnityEngine;

namespace HoldTheHill.Features.Combat
{
    /// <summary>
    /// A shot that skips from one enemy to the next instead of stopping at the first.
    /// Each bounce looks for the closest target it has not already struck, so it works
    /// its way through a group rather than pinging between the same two enemies.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Combat/Ricochet Projectile")]
    public class RicochetProjectile : Projectile
    {
        [Header("Ricochet")]
        [Tooltip("How many extra targets the shot can bounce to after the first hit.")]
        [SerializeField, Min(1)] private int _maxBounces = 3;

        [Tooltip("How far it will look for the next target, in world units.")]
        [SerializeField, Min(0.1f)] private float _bounceRange = 3f;

        [Tooltip("Damage multiplier applied per bounce. 0.8 means each hop hits for 80% of the last.")]
        [SerializeField, Range(0.1f, 1f)] private float _damageFalloff = 0.8f;

        [Tooltip("If on, the shot may bounce back to a target it has already hit once it runs out of fresh ones.")]
        [SerializeField] private bool _allowRepeatTargets = false;

        private readonly List<IDamageable> _alreadyHit = new List<IDamageable>();
        private int _bouncesUsed;
        private float _currentDamage;

        public override void Launch(Vector2 origin, IDamageable target, Vector2 direction)
        {
            // A fresh shot: clear the bounce history. Redirects go through Redirect instead.
            _alreadyHit.Clear();
            _bouncesUsed = 0;
            _currentDamage = Damage;

            base.Launch(origin, target, direction);
        }

        protected override void ApplyHit(IDamageable target, Vector2 point)
        {
            if (!_alreadyHit.Contains(target))
            {
                _alreadyHit.Add(target);
            }

            // Damage is applied from our own falling-off value rather than the base field,
            // so later hops hit softer without permanently changing the prefab's damage.
            target.TakeDamage(new DamageInfo(_currentDamage, gameObject, point, _damageType));
            if (_status.IsValid)
            {
                target.ApplyStatusEffect(_status);
            }
        }

        protected override void Detonate(Vector2 point, IDamageable directHit)
        {
            if (_bouncesUsed >= _maxBounces)
            {
                base.Detonate(point, directHit);
                return;
            }

            IDamageable next = FindBounceTarget(point);
            if (next == null)
            {
                base.Detonate(point, directHit);
                return;
            }

            _bouncesUsed++;
            _currentDamage *= _damageFalloff;

            // Splash still happens at each bounce point, then the shot carries on
            // rather than being released.
            CombatUtil.ApplySplash(
                point, _splashRadius, _targetMask, _splashDamage, _damageType, gameObject, _status, directHit);

            Vector2 toNext = ((Vector2)next.Transform.position - point).normalized;
            base.Launch(point, next, toNext);
        }

        private IDamageable FindBounceTarget(Vector2 from)
        {
            var candidates = new List<IDamageable>();
            CombatUtil.OverlapDamageables(from, _bounceRange, _targetMask, candidates);

            IDamageable best = null;
            float bestSqr = float.PositiveInfinity;

            for (int i = 0; i < candidates.Count; i++)
            {
                IDamageable candidate = candidates[i];
                if (!_allowRepeatTargets && _alreadyHit.Contains(candidate))
                {
                    continue;
                }

                float sqr = ((Vector2)candidate.Transform.position - from).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = candidate;
                }
            }

            return best;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, _bounceRange);
        }
    }
}
