using UnityEngine;

namespace HoldTheHill.Features.Combat
{
    /// <summary>
    /// A shell that breaks apart into a spray of smaller shots. Splits either where it
    /// lands or at the top of its arc, so it can be used as a mortar or as a shotgun burst.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Combat/Cluster Projectile")]
    public class ClusterProjectile : Projectile
    {
        /// <summary>When the shell breaks apart.</summary>
        public enum SplitTrigger
        {
            /// <summary>Splits where it lands or hits something.</summary>
            OnImpact,

            /// <summary>Splits at the peak of a lobbed arc, raining fragments down.</summary>
            AtApex
        }

        [Header("Cluster")]
        [Tooltip("The smaller shot scattered on detonation. Must not be this same prefab.")]
        [SerializeField] private Projectile _fragmentPrefab;

        [Tooltip("How many fragments are produced.")]
        [SerializeField, Min(1)] private int _fragmentCount = 6;

        [Tooltip("Total spread of the fragment fan, in degrees. 360 scatters in all directions.")]
        [SerializeField, Range(0f, 360f)] private float _spreadAngle = 120f;

        [Tooltip("When the shell breaks apart.")]
        [SerializeField] private SplitTrigger _splitOn = SplitTrigger.OnImpact;

        [Tooltip("Fragments spawn this far out from the burst point, so they do not all overlap.")]
        [SerializeField, Min(0f)] private float _fragmentSpawnOffset = 0.2f;

        private ProjectilePool _fragmentPool;
        private bool _hasSplit;

        public override void Launch(Vector2 origin, IDamageable target, Vector2 direction)
        {
            _hasSplit = false;
            base.Launch(origin, target, direction);
        }

        protected override void Update()
        {
            base.Update();

            // base.Update may have detonated and recycled this shot; an apex split after
            // that would scatter fragments from a projectile that no longer exists.
            if (IsReleased)
            {
                return;
            }

            if (_splitOn == SplitTrigger.AtApex && !_hasSplit && _flightMode == FlightMode.Lobbed && IsPastApex())
            {
                Scatter(transform.position);
            }
        }

        // Apex of the arc is the halfway point of a symmetric parabola.
        private bool IsPastApex()
        {
            return ArcProgress >= 0.5f;
        }

        protected override void Detonate(Vector2 point, IDamageable directHit)
        {
            if (!_hasSplit)
            {
                Scatter(point);
            }

            base.Detonate(point, directHit);
        }

        private void Scatter(Vector2 point)
        {
            _hasSplit = true;

            if (_fragmentPrefab == null)
            {
                Debug.LogWarning($"[ClusterProjectile] '{name}' has no fragment prefab; nothing to scatter.", this);
                return;
            }

            // Fragments are as numerous as the shells are rare, so they get their own pool.
            if (_fragmentPool == null)
            {
                _fragmentPool = ProjectilePool.For(_fragmentPrefab, _fragmentCount * 2);
            }

            float start = _spreadAngle >= 360f ? 0f : -_spreadAngle * 0.5f;
            float step = _fragmentCount > 1 ? _spreadAngle / (_fragmentCount - 1) : 0f;
            if (_spreadAngle >= 360f)
            {
                step = 360f / _fragmentCount;
            }

            float baseAngle = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg;

            for (int i = 0; i < _fragmentCount; i++)
            {
                float angle = (baseAngle + start + step * i) * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                Projectile fragment = _fragmentPool.Get();
                if (fragment == null)
                {
                    continue;
                }

                fragment.TargetMask = TargetMask;
                fragment.Launch(point + dir * _fragmentSpawnOffset, null, dir);
            }
        }
    }
}
