using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Enemies;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// An egg sac laid by the Mantis Queen. It sits on the path, can be shot like any enemy,
    /// and hatches into another enemy if it is left alone long enough.
    /// </summary>
    /// <remarks>
    /// It has health and a collider but no mover, so nothing walks it anywhere. It plays its own
    /// clips (Idle, Hurt, Hatch, Burst) because the enemy animator expects a Walk and a Death.
    /// </remarks>
    [RequireComponent(typeof(EnemyHealth))]
    public class GrayboxEggSac : MonoBehaviour
    {
        [SerializeField] private GameObject _hatchPrefab;
        [SerializeField, Min(0.5f)] private float _hatchSeconds = 8f;

        private EnemyHealth _health;
        private SpriteClipPlayer _player;
        private float _left;
        private bool _hatching;

        /// <summary>How far along the route it was laid, so what hatches starts from here.</summary>
        public float PathDistance { get; set; }

        public bool Alive => _health != null && !_health.IsDead && !_hatching;

        private void Awake()
        {
            _health = GetComponent<EnemyHealth>();
            _player = GetComponent<SpriteClipPlayer>();
        }

        private void OnEnable()
        {
            _left = _hatchSeconds;
            _hatching = false;
            EnemyHealth.Damaged += OnDamaged;
            EnemyHealth.Defeated += OnDefeated;
        }

        private void OnDisable()
        {
            EnemyHealth.Damaged -= OnDamaged;
            EnemyHealth.Defeated -= OnDefeated;
        }

        private void Update()
        {
            if (_hatching || _health.IsDead)
            {
                return;
            }

            _left -= Time.deltaTime;
            if (_left <= 0f)
            {
                Hatch();
            }
        }

        private void OnDamaged(EnemyHealth health, DamageInfo info, float applied)
        {
            if (health == _health && !health.IsDead && !_hatching && _player != null)
            {
                _player.Play("Hurt");
            }
        }

        private void OnDefeated(GameObject enemy)
        {
            if (enemy == gameObject && _player != null)
            {
                SpriteClipPlayer.SpawnOneShot(_player.Library, _player.Key, "Burst", transform.position, Quaternion.identity, 1f, 1);
            }
        }

        /// <summary>Hatches now: the husk plays out and the hatchling takes the path from here.</summary>
        public GameObject Hatch()
        {
            if (_hatching)
            {
                return null;
            }

            _hatching = true;
            if (_player != null)
            {
                SpriteClipPlayer.SpawnOneShot(_player.Library, _player.Key, "Hatch", transform.position, Quaternion.identity, 1f, 1);
            }

            GameObject hatchling = null;
            if (_hatchPrefab != null)
            {
                hatchling = Instantiate(_hatchPrefab, transform.position, Quaternion.identity, transform.parent);
                GrayboxSpecialEnemy.PlaceOnPath(hatchling, PathDistance);
            }

            // Not a kill: nothing is paid for an egg that got to hatch.
            Destroy(gameObject);
            return hatchling;
        }
    }
}
