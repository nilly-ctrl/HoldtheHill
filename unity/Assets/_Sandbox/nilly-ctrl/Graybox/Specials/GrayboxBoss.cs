using System;
using HoldTheHill.Features.Enemies;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Marks an enemy as a boss and says which one. Sits on the boss base prefab, so every
    /// boss variant has it; <see cref="GrayboxBossBanner"/> listens for <see cref="Appeared"/>.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Graybox/Graybox Boss")]
    public class GrayboxBoss : MonoBehaviour
    {
        [SerializeField] private GrayboxBossData _data;

        [Tooltip("Left where the boss dies, e.g. a golden crumb. Empty drops nothing.")]
        [SerializeField] private GameObject _dropPrefab;

        private EnemyHealth _health;

        /// <summary>Raised once for each boss, on its first frame in the scene.</summary>
        public static event Action<GrayboxBoss> Appeared;

        public GrayboxBossData Data => _data;

        private void Awake()
        {
            _health = GetComponent<EnemyHealth>();
        }

        private void OnEnable()
        {
            if (_health != null) _health.Died += OnDied;
        }

        private void OnDisable()
        {
            if (_health != null) _health.Died -= OnDied;
        }

        private void Start()
        {
            Appeared?.Invoke(this);
        }

        private void OnDied()
        {
            if (_dropPrefab != null)
            {
                Instantiate(_dropPrefab, transform.position, Quaternion.identity);
            }
        }
    }
}
