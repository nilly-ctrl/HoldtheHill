using System.Collections.Generic;
using HoldTheHill.Core;
using UnityEngine;

namespace HoldTheHill.Features.Combat
{
    /// <summary>
    /// A ring of wisps, blades or embers circling the tower, hurting anything they
    /// touch. Unlike a projectile this is always on: it defends the tower's own tile
    /// rather than reaching out, which makes it the answer to enemies that get close.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Combat/Orbiting Damage Field")]
    public class OrbitingDamageField : TowerWeapon
    {
        /// <summary>The shape the wisps trace.</summary>
        public enum OrbitPath
        {
            /// <summary>A plain circle at a fixed radius.</summary>
            Circular,

            /// <summary>A squashed circle, wider than it is tall.</summary>
            Elliptical,

            /// <summary>Breathes in and out between a minimum and maximum radius.</summary>
            Expanding
        }

        [Header("Orbit")]
        [Tooltip("How many wisps circle the tower. They are spaced evenly.")]
        [SerializeField, Min(1)] private int _orbiterCount = 3;

        [Tooltip("Distance from the tower, in world units.")]
        [SerializeField, Min(0.1f)] private float _radius = 1.5f;

        [Tooltip("Degrees per second. Negative spins the other way.")]
        [SerializeField] private float _angularSpeed = 120f;

        [SerializeField] private OrbitPath _path = OrbitPath.Circular;

        [Tooltip("Elliptical only: how squashed the orbit is. 0.5 is half as tall as it is wide.")]
        [SerializeField, Range(0.1f, 1f)] private float _verticalRatio = 0.6f;

        [Tooltip("Expanding only: smallest radius as a fraction of the full radius.")]
        [SerializeField, Range(0.1f, 1f)] private float _minRadiusRatio = 0.5f;

        [Tooltip("Expanding only: how many in-and-out cycles per second.")]
        [SerializeField, Min(0.01f)] private float _pulseSpeed = 0.5f;

        [Header("Damage")]
        [Tooltip("Damage dealt each time a wisp touches an enemy.")]
        [SerializeField, Min(0f)] private float _contactDamage = 4f;

        [Tooltip("Seconds before the same enemy can be hit again by the same wisp.")]
        [SerializeField, Min(0.05f)] private float _hitCooldown = 0.5f;

        [SerializeField] private DamageType _damageType = DamageType.Magic;

        [Tooltip("Status effect applied on contact. Duration 0 means none.")]
        [SerializeField] private StatusEffectData _status;

        [Tooltip("How large each wisp's hit area is.")]
        [SerializeField, Min(0.05f)] private float _orbiterRadius = 0.3f;

        [Tooltip("Which layers the wisps can hurt.")]
        [SerializeField] private LayerMask _targetMask = ~0;

        [Header("Look")]
        [Tooltip("Optional sprite for each wisp. A placeholder square is used if empty.")]
        [SerializeField] private Sprite _orbiterSprite;

        [SerializeField] private Color _orbiterColor = new Color(0.6f, 0.9f, 1f);

        private readonly List<Transform> _orbiters = new List<Transform>();
        private readonly List<IDamageable> _touching = new List<IDamageable>();
        private readonly Dictionary<IDamageable, float> _lastHit = new Dictionary<IDamageable, float>();

        private float _angle;

        /// <summary>Distance the wisps orbit at.</summary>
        public float Radius => _radius;

        private void Start()
        {
            BuildOrbiters();
        }

        private void OnDestroy()
        {
            _lastHit.Clear();
        }

        private void Update()
        {
            _angle += _angularSpeed * Time.deltaTime;
            if (_angle >= 360f || _angle <= -360f)
            {
                _angle %= 360f;
            }

            float spacing = 360f / _orbiters.Count;

            for (int i = 0; i < _orbiters.Count; i++)
            {
                Transform orbiter = _orbiters[i];
                if (orbiter == null)
                {
                    continue;
                }

                float theta = (_angle + spacing * i) * Mathf.Deg2Rad;
                orbiter.localPosition = OffsetFor(theta);
                DamageAround(orbiter.position);
            }
        }

        private Vector3 OffsetFor(float theta)
        {
            switch (_path)
            {
                case OrbitPath.Elliptical:
                    return new Vector3(Mathf.Cos(theta) * _radius, Mathf.Sin(theta) * _radius * _verticalRatio, 0f);

                case OrbitPath.Expanding:
                {
                    // Ping-pongs between the minimum and full radius.
                    float t = (Mathf.Sin(Time.time * _pulseSpeed * Mathf.PI * 2f) + 1f) * 0.5f;
                    float radius = Mathf.Lerp(_radius * _minRadiusRatio, _radius, t);
                    return new Vector3(Mathf.Cos(theta) * radius, Mathf.Sin(theta) * radius, 0f);
                }

                default:
                    return new Vector3(Mathf.Cos(theta) * _radius, Mathf.Sin(theta) * _radius, 0f);
            }
        }

        private void DamageAround(Vector3 worldPoint)
        {
            CombatUtil.OverlapDamageables(worldPoint, _orbiterRadius, _targetMask, _touching);

            for (int i = 0; i < _touching.Count; i++)
            {
                IDamageable target = _touching[i];

                // Without a cooldown a wisp would hit every frame it overlaps, which at
                // 60fps is 60 hits a second rather than the intended handful.
                if (_lastHit.TryGetValue(target, out float last) && Time.time - last < _hitCooldown)
                {
                    continue;
                }

                _lastHit[target] = Time.time;
                target.TakeDamage(new DamageInfo(_contactDamage, gameObject, worldPoint, _damageType));

                if (_status.IsValid)
                {
                    target.ApplyStatusEffect(_status);
                }
            }
        }

        private void BuildOrbiters()
        {
            for (int i = 0; i < _orbiterCount; i++)
            {
                var orbiter = new GameObject($"Orbiter {i}");
                orbiter.transform.SetParent(transform, false);
                orbiter.transform.localScale = Vector3.one * (_orbiterRadius * 2f);

                var renderer = orbiter.AddComponent<SpriteRenderer>();
                renderer.sprite = _orbiterSprite != null ? _orbiterSprite : SpriteUtil.Square;
                renderer.color = _orbiterColor;
                renderer.sortingOrder = 5;

                _orbiters.Add(orbiter.transform);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.6f, 0.9f, 1f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, _radius);
        }
    }
}
