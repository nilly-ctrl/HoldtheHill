using System.Collections.Generic;
using UnityEngine;

namespace HoldTheHill.Features.Combat
{
    /// <summary>
    /// Shared target queries. Every weapon needs "find the damageable things near
    /// this point", so it lives here once rather than in each of them.
    /// </summary>
    public static class CombatUtil
    {
        // Reused so per-frame queries do not allocate during heavy waves.
        private static readonly List<Collider2D> OverlapBuffer = new List<Collider2D>(64);
        private static ContactFilter2D _filter = CreateFilter();

        private static ContactFilter2D CreateFilter()
        {
            var filter = new ContactFilter2D { useTriggers = true, useLayerMask = true };
            filter.SetLayerMask(Physics2D.AllLayers);
            return filter;
        }

        /// <summary>
        /// Fills <paramref name="results"/> with live damageables whose collider overlaps the circle.
        /// Clears the list first. Each target appears once even with several colliders.
        /// </summary>
        public static void OverlapDamageables(Vector2 center, float radius, LayerMask mask, List<IDamageable> results)
        {
            results.Clear();
            if (radius <= 0f)
            {
                return;
            }

            _filter.SetLayerMask(mask);
            Physics2D.OverlapCircle(center, radius, _filter, OverlapBuffer);

            for (int i = 0; i < OverlapBuffer.Count; i++)
            {
                if (TryGetDamageable(OverlapBuffer[i], out IDamageable damageable) && !results.Contains(damageable))
                {
                    results.Add(damageable);
                }
            }
        }

        /// <summary>
        /// True if a target still exists and has health left. Use this rather than a
        /// plain null check anywhere an <see cref="IDamageable"/> is held across frames.
        /// </summary>
        /// <remarks>
        /// The cast is the whole point. Unity overloads <c>==</c> on
        /// <see cref="UnityEngine.Object"/> so a destroyed component reports as null,
        /// but that overload does not apply through an interface reference: a plain
        /// <c>target != null</c> on an <see cref="IDamageable"/> is an ordinary C#
        /// reference comparison, so a destroyed enemy sails through it and the next
        /// member access throws MissingReferenceException. Casting back to Object
        /// restores the real check.
        /// </remarks>
        public static bool IsAlive(IDamageable target)
        {
            return target is Object obj && obj != null && !target.IsDead && target.Transform != null;
        }

        /// <summary>Finds the live <see cref="IDamageable"/> owning a collider, if any.</summary>
        public static bool TryGetDamageable(Collider2D collider, out IDamageable damageable)
        {
            damageable = null;
            if (collider == null)
            {
                return false;
            }

            // Colliders often sit on a child of the object holding the health component.
            damageable = collider.GetComponentInParent<IDamageable>();
            return IsAlive(damageable);
        }

        /// <summary>
        /// Damages everything in a circle. <paramref name="ignore"/> is skipped so a direct
        /// hit is not also counted as splash on the same target.
        /// </summary>
        public static void ApplySplash(
            Vector2 center,
            float radius,
            LayerMask mask,
            float damage,
            DamageType type,
            GameObject source,
            StatusEffectData status,
            IDamageable ignore = null)
        {
            if (radius <= 0f || damage <= 0f)
            {
                return;
            }

            var hits = new List<IDamageable>();
            OverlapDamageables(center, radius, mask, hits);

            for (int i = 0; i < hits.Count; i++)
            {
                IDamageable target = hits[i];
                if (target == ignore)
                {
                    continue;
                }

                target.TakeDamage(new DamageInfo(damage, source, center, type));
                if (status.IsValid)
                {
                    target.ApplyStatusEffect(status);
                }
            }
        }

        /// <summary>Squared distance between two transforms, for cheap comparisons.</summary>
        public static float SqrDistance(Transform a, Transform b)
        {
            return a == null || b == null
                ? float.PositiveInfinity
                : ((Vector2)a.position - (Vector2)b.position).sqrMagnitude;
        }
    }
}
