using System.Collections;
using System.Collections.Generic;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Enemies;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Boss. Lays egg sacs on the path behind her. While enough of them are alive she is warded
    /// and takes no damage at all, so the sacs have to be cleared first; left alone, each one
    /// hatches into another enemy.
    /// </summary>
    public class GrayboxMantisQueen : GrayboxSpecialEnemy, IIncomingDamageModifier
    {
        [SerializeField] private GameObject _eggPrefab;
        [SerializeField, Min(1f)] private float _layInterval = 5f;
        [SerializeField, Min(1)] private int _maxEggs = 5;
        [Tooltip("She takes no damage while at least this many of her egg sacs are alive.")]
        [SerializeField, Min(1)] private int _wardAt = 3;
        [SerializeField, Min(0f)] private float _layBehind = 1.1f;

        private readonly List<GrayboxEggSac> _eggs = new List<GrayboxEggSac>();
        private float _cooldown;
        private bool _laying;
        private bool _warded;
        private float _nextWardNotice;

        public bool IsWarded => _warded;

        public int LivingEggs
        {
            get
            {
                _eggs.RemoveAll(e => e == null || !e.Alive);
                return _eggs.Count;
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _eggs.Clear();
            _cooldown = _layInterval * 0.5f;
            _laying = _warded = false;
        }

        public float ModifyIncomingDamage(EnemyHealth health, DamageInfo info)
        {
            if (!_warded)
            {
                return info.Amount;
            }

            if (Time.time >= _nextWardNotice)
            {
                _nextWardNotice = Time.time + 1.2f;
                Say("WARDED", DamageNumberKind.Magic, transform.position);
            }

            return 0f;
        }

        private void Update()
        {
            if (!Alive)
            {
                return;
            }

            bool warded = LivingEggs >= _wardAt;
            if (warded != _warded)
            {
                _warded = warded;
                SetLoop(warded ? "Warded" : "Walk");
                if (!warded)
                {
                    Play("WardBreak");
                    Say("WARD DOWN", DamageNumberKind.Critical, transform.position);
                }
            }

            if (_laying || _eggPrefab == null)
            {
                return;
            }

            _cooldown -= Time.deltaTime;
            if (_cooldown <= 0f && _eggs.Count < _maxEggs)
            {
                StartCoroutine(Lay());
            }
        }

        private IEnumerator Lay()
        {
            _laying = true;
            Mover.SpeedScale = 0f;
            Play("LayEgg");
            yield return new WaitForSeconds(ClipLength("LayEgg", 0.7f));

            if (Alive)
            {
                LayEggNow();
            }

            Mover.SpeedScale = 1f;
            _cooldown = _layInterval;
            _laying = false;
        }

        /// <summary>Puts an egg sac on the path just behind her. Public so a test can skip the animation.</summary>
        public GrayboxEggSac LayEggNow()
        {
            float distance = Mathf.Max(0f, Mover != null ? Mover.DistanceTravelled - _layBehind : 0f);
            Vector3 position = Mover != null && Waypoints != null ? PointAlongPath(distance) : transform.position - transform.right * _layBehind;
            GameObject go = Instantiate(_eggPrefab, position, Quaternion.identity, transform.parent);
            var egg = go.GetComponent<GrayboxEggSac>();
            if (egg != null)
            {
                egg.PathDistance = distance;
                _eggs.Add(egg);
            }

            return egg;
        }
    }
}
