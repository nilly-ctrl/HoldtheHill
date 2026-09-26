using System;
using System.Collections;
using System.Collections.Generic;
using HoldTheHill.Features.Combat;
using UnityEngine;

namespace HoldTheHill.Features.Enemies
{
    /// <summary>
    /// Health, damage-over-time and death for one enemy. This is the component towers,
    /// projectiles and hazards actually talk to, through <see cref="IDamageable"/>.
    /// </summary>
    /// <remarks>
    /// Death is announced through the static <see cref="Defeated"/> event rather than by
    /// calling the spawner directly. <c>EnemySpawner</c> lives outside this assembly, so a
    /// direct call would not compile; see <c>EnemySpawnerBridge</c> for the adapter that
    /// forwards this event to <c>EnemySpawner.NotifyEnemyDefeated</c>.
    /// </remarks>
    [AddComponentMenu("Hold the Hill/Enemies/Enemy Health")]
    public class EnemyHealth : MonoBehaviour, IDamageable, IHealthReadable
    {
        /// <summary>
        /// Raised whenever any enemy dies, with the enemy's GameObject. Static so systems
        /// in other assemblies can listen without this component knowing about them.
        /// </summary>
        public static event Action<GameObject> Defeated;

        [Header("Health")]
        [SerializeField, Min(1f)] private float _maxHealth = 30f;

        [Header("Death")]
        [Tooltip("Seconds to wait before the body disappears, leaving room for a death animation.")]
        [SerializeField, Min(0f)] private float _despawnDelay = 0f;

        [Tooltip("If on, the enemy is deactivated instead of destroyed, ready for pooling.")]
        [SerializeField] private bool _deactivateInsteadOfDestroy = false;

        // One running coroutine per effect name, so re-applying refreshes rather than stacks.
        private readonly Dictionary<string, Coroutine> _activeEffects = new Dictionary<string, Coroutine>();
        private readonly Dictionary<string, float> _activeSlows = new Dictionary<string, float>();

        private float _currentHealth;

        /// <summary>Health remaining right now.</summary>
        public float CurrentHealth => _currentHealth;

        /// <summary>Health when at full.</summary>
        public float MaxHealth => _maxHealth;

        /// <summary>True once health has run out.</summary>
        public bool IsDead { get; private set; }

        /// <summary>This enemy's transform, for aiming and distance checks.</summary>
        public Transform Transform => transform;

        /// <summary>
        /// Movement speed multiplier from active slows, 1 when unslowed. A mover script
        /// reads this each frame; nothing here moves the enemy itself.
        /// </summary>
        public float SpeedMultiplier { get; private set; } = 1f;

        /// <summary>Fraction of health remaining, 0 to 1.</summary>
        public float HealthFraction => _maxHealth > 0f ? Mathf.Clamp01(_currentHealth / _maxHealth) : 0f;

        private void OnEnable()
        {
            // Reset here rather than in Awake so pooled enemies come back at full health.
            _currentHealth = _maxHealth;
            IsDead = false;
            SpeedMultiplier = 1f;
            _activeEffects.Clear();
            _activeSlows.Clear();
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            _activeEffects.Clear();
            _activeSlows.Clear();
        }

        /// <summary>Applies one hit. Ignored once already dead.</summary>
        public void TakeDamage(DamageInfo info)
        {
            if (IsDead || info.Amount <= 0f)
            {
                return;
            }

            _currentHealth -= info.Amount;

            if (_currentHealth <= 0f)
            {
                _currentHealth = 0f;
                Die();
            }
        }

        /// <summary>
        /// Applies a damage-over-time and/or slow. Re-applying an effect with the same
        /// name restarts it rather than stacking a second copy.
        /// </summary>
        public void ApplyStatusEffect(StatusEffectData status)
        {
            if (IsDead || !status.IsValid)
            {
                return;
            }

            string key = string.IsNullOrEmpty(status.EffectName) ? "Unnamed" : status.EffectName;

            if (_activeEffects.TryGetValue(key, out Coroutine running) && running != null)
            {
                StopCoroutine(running);
            }

            _activeEffects[key] = StartCoroutine(RunStatusEffect(key, status));
        }

        /// <summary>Heals the enemy, never above its maximum. Does nothing once dead.</summary>
        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f)
            {
                return;
            }

            _currentHealth = Mathf.Min(_currentHealth + amount, _maxHealth);
        }

        private IEnumerator RunStatusEffect(string key, StatusEffectData status)
        {
            if (status.SlowMultiplier < 1f)
            {
                _activeSlows[key] = status.SlowMultiplier;
                RecalculateSpeed();
            }

            float elapsed = 0f;
            float tick = status.SafeTickInterval;

            while (elapsed < status.Duration && !IsDead)
            {
                yield return new WaitForSeconds(tick);
                elapsed += tick;

                if (IsDead)
                {
                    break;
                }

                if (status.DamagePerTick > 0f)
                {
                    // Routed through TakeDamage so a DoT can land the killing blow
                    // and trigger death exactly like a direct hit.
                    TakeDamage(new DamageInfo(status.DamagePerTick, gameObject, transform.position, DamageType.Poison));
                }
            }

            _activeSlows.Remove(key);
            _activeEffects.Remove(key);
            RecalculateSpeed();
        }

        // Slows do not stack; the strongest one active wins. Stacking multiplicatively
        // makes a handful of weak slows freeze an enemy solid, which plays badly.
        private void RecalculateSpeed()
        {
            float slowest = 1f;
            foreach (float multiplier in _activeSlows.Values)
            {
                slowest = Mathf.Min(slowest, multiplier);
            }

            SpeedMultiplier = slowest;
        }

        private void Die()
        {
            if (IsDead)
            {
                return;
            }

            IsDead = true;
            SpeedMultiplier = 1f;

            StopAllCoroutines();
            _activeEffects.Clear();
            _activeSlows.Clear();

            Defeated?.Invoke(gameObject);

            if (_despawnDelay > 0f)
            {
                StartCoroutine(DespawnAfterDelay());
            }
            else
            {
                Despawn();
            }
        }

        private IEnumerator DespawnAfterDelay()
        {
            yield return new WaitForSeconds(_despawnDelay);
            Despawn();
        }

        private void Despawn()
        {
            if (_deactivateInsteadOfDestroy)
            {
                gameObject.SetActive(false);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}
