using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The two set pieces at the ends of the trail: the hill being defended, which looks more
    /// battered as <see cref="GrayboxBaseHealth"/> drops, and the burrow the enemy comes out of,
    /// which kicks up dirt on each spawn.
    /// </summary>
    [RequireComponent(typeof(SpriteClipPlayer))]
    public class GrayboxPropVisuals : MonoBehaviour
    {
        public enum Kind { Hill, Burrow }

        [SerializeField] private Kind _kind;
        [Tooltip("Seconds the hill shakes after taking a hit.")]
        [SerializeField] private float _shakeTime = 0.2f;

        private SpriteClipPlayer _player;
        private EnemySpawner _spawner;
        private Vector3 _home;
        private float _shakeUntil;

        public void Configure(Kind kind) => _kind = kind;

        /// <summary>Which hill clip matches a health fraction.</summary>
        public static string HillClipFor(float fraction)
        {
            if (fraction <= 0f) return "Destroyed";
            if (fraction <= 0.34f) return "Critical";
            return fraction <= 0.67f ? "Damaged" : "Healthy";
        }

        private void Awake()
        {
            _player = GetComponent<SpriteClipPlayer>();
            _home = transform.position;
        }

        private void OnEnable()
        {
            if (_kind == Kind.Hill)
            {
                GrayboxBaseHealth.OnBaseHealthChanged += OnHillHealthChanged;
            }
        }

        private void Start()
        {
            if (_kind == Kind.Burrow)
            {
                _spawner = FindAnyObjectByType<EnemySpawner>();
                if (_spawner != null)
                {
                    _spawner.onEnemySpawned?.AddListener(OnEnemySpawned);
                }
            }
        }

        private void OnDisable()
        {
            GrayboxBaseHealth.OnBaseHealthChanged -= OnHillHealthChanged;
            if (_spawner != null)
            {
                _spawner.onEnemySpawned?.RemoveListener(OnEnemySpawned);
            }
        }

        private void OnHillHealthChanged(float current, float max)
        {
            string clip = HillClipFor(max > 0f ? current / max : 0f);
            if (_player.DefaultClip != clip)
            {
                _player.DefaultClip = clip;
                _player.Play(clip);
            }

            _shakeUntil = Time.time + _shakeTime;
        }

        private void OnEnemySpawned(GameObject enemy) => _player.Play("Spawn");

        private void Update()
        {
            if (_kind != Kind.Hill)
            {
                return;
            }

            // One pixel of jitter while shaking, then back exactly where it was.
            transform.position = Time.time < _shakeUntil
                ? _home + new Vector3(Random.Range(-1, 2), Random.Range(-1, 2), 0f) / 32f
                : _home;
        }
    }
}
