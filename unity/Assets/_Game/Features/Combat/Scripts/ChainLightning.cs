using System.Collections.Generic;
using UnityEngine;

namespace HoldTheHill.Features.Combat
{
    /// <summary>
    /// An instant electric arc that leaps from one enemy to the next. There is no
    /// travel time: the whole chain resolves in the frame it is fired, and the
    /// <see cref="LineRenderer"/> just shows where it went for a moment afterwards.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Combat/Chain Lightning")]
    [RequireComponent(typeof(LineRenderer))]
    public class ChainLightning : TowerWeapon
    {
        [Header("Chain")]
        [Tooltip("How many enemies the arc can strike in total, including the first.")]
        [SerializeField, Min(1)] private int _maxTargets = 4;

        [Tooltip("How far the arc can leap between enemies, in world units.")]
        [SerializeField, Min(0.1f)] private float _jumpRange = 3f;

        [Tooltip("Which layers the arc can strike.")]
        [SerializeField] private LayerMask _targetMask = ~0;

        [Header("Damage")]
        [Tooltip("Damage dealt to the first enemy struck.")]
        [SerializeField, Min(0f)] private float _damage = 12f;

        [Tooltip("Multiplier applied per jump. 0.7 means each link hits for 70% of the one before.")]
        [SerializeField, Range(0.1f, 1f)] private float _damageFalloff = 0.7f;

        [Tooltip("Status effect applied at every link. Duration 0 means none.")]
        [SerializeField] private StatusEffectData _status;

        [Header("Look")]
        [Tooltip("Seconds the arc stays visible after firing.")]
        [SerializeField, Min(0.01f)] private float _arcDisplayTime = 0.12f;

        [Tooltip("How far each segment wobbles sideways, to stop the arc looking like a ruler.")]
        [SerializeField, Min(0f)] private float _jitter = 0.15f;

        [Tooltip("Extra points per link. More points means a more ragged arc.")]
        [SerializeField, Range(1, 8)] private int _segmentsPerLink = 3;

        private readonly List<IDamageable> _struck = new List<IDamageable>();
        private readonly List<Vector3> _points = new List<Vector3>();
        private LineRenderer _line;
        private float _visibleUntil;

        /// <summary>How far the first link can reach from this component.</summary>
        public float JumpRange => _jumpRange;

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
            _line.useWorldSpace = true;
            _line.positionCount = 0;
            _line.enabled = false;
        }

        private void Update()
        {
            if (_line.enabled && Time.time >= _visibleUntil)
            {
                _line.enabled = false;
            }
        }

        public override void Shoot(IDamageable target) => Fire(target);

        /// <summary>
        /// Strikes <paramref name="first"/> and chains onward. Does nothing if the
        /// first target is already gone.
        /// </summary>
        public void Fire(IDamageable first)
        {
            if (!CombatUtil.IsAlive(first))
            {
                return;
            }

            _struck.Clear();
            _points.Clear();
            _points.Add(transform.position);

            IDamageable current = first;
            Vector2 from = transform.position;
            float damage = _damage;

            while (current != null && _struck.Count < _maxTargets)
            {
                Vector2 to = current.Transform.position;

                _struck.Add(current);
                AddJaggedLink(from, to);

                current.TakeDamage(new DamageInfo(damage, gameObject, to, DamageType.Lightning));
                if (_status.IsValid)
                {
                    current.ApplyStatusEffect(_status);
                }

                damage *= _damageFalloff;
                from = to;
                current = FindNextLink(to);
            }

            ShowArc();
        }

        private IDamageable FindNextLink(Vector2 from)
        {
            var candidates = new List<IDamageable>();
            CombatUtil.OverlapDamageables(from, _jumpRange, _targetMask, candidates);

            IDamageable best = null;
            float bestSqr = float.PositiveInfinity;

            for (int i = 0; i < candidates.Count; i++)
            {
                IDamageable candidate = candidates[i];
                if (_struck.Contains(candidate))
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

        // Breaks each straight link into a few offset points so the bolt looks electrical.
        private void AddJaggedLink(Vector2 from, Vector2 to)
        {
            Vector2 delta = to - from;
            Vector2 perpendicular = new Vector2(-delta.y, delta.x).normalized;

            for (int i = 1; i <= _segmentsPerLink; i++)
            {
                float t = i / (float)_segmentsPerLink;
                Vector2 point = Vector2.Lerp(from, to, t);

                // The final point of a link must sit exactly on the target.
                if (i < _segmentsPerLink)
                {
                    point += perpendicular * Random.Range(-_jitter, _jitter);
                }

                _points.Add(point);
            }
        }

        private void ShowArc()
        {
            if (_points.Count < 2)
            {
                return;
            }

            _line.positionCount = _points.Count;
            _line.SetPositions(_points.ToArray());
            _line.enabled = true;
            _visibleUntil = Time.time + _arcDisplayTime;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.4f, 0.8f, 1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, _jumpRange);
        }
    }
}
