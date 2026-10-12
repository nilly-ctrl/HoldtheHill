using UnityEngine;

namespace HoldTheHill.Features.Enemies
{
    /// <summary>
    /// Energy barrier / chitin shield component for enemies that absorbs damage before health is reduced.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Enemies/Enemy Shield")]
    public class EnemyShield : MonoBehaviour
    {
        [Header("Shield Parameters")]
        [SerializeField, Min(1f)] private float _maxShield = 100f;
        [SerializeField, Min(0f)] private float _regenRate = 10f;
        [SerializeField, Min(0f)] private float _regenDelay = 4.0f;

        private float _currentShield;
        private float _lastHitTimer;

        public float CurrentShield => _currentShield;
        public float MaxShield => _maxShield;
        public float ShieldFraction => _maxShield > 0f ? Mathf.Clamp01(_currentShield / _maxShield) : 0f;

        private void OnEnable()
        {
            _currentShield = _maxShield;
            _lastHitTimer = 0f;
        }

        private void Update()
        {
            if (_regenRate <= 0f || Mathf.Approximately(_currentShield, _maxShield))
            {
                return;
            }

            _lastHitTimer += Time.deltaTime;
            if (_lastHitTimer >= _regenDelay)
            {
                _currentShield = Mathf.Min(_currentShield + _regenRate * Time.deltaTime, _maxShield);
            }
        }

        /// <summary>
        /// Absorbs incoming damage using shield points. Returns any unabsorbed excess damage.
        /// </summary>
        public float AbsorbDamage(float incomingDamage)
        {
            if (incomingDamage <= 0f || _currentShield <= 0f)
            {
                return incomingDamage;
            }

            _lastHitTimer = 0f;

            if (_currentShield >= incomingDamage)
            {
                _currentShield -= incomingDamage;
                return 0f;
            }
            else
            {
                float excess = incomingDamage - _currentShield;
                _currentShield = 0f;
                return excess;
            }
        }
    }
}
