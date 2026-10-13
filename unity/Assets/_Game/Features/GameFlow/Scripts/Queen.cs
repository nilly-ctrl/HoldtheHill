using System;
using HoldTheHill.Features.Enemies;
using UnityEngine;
using UnityEngine.Events;

namespace HoldTheHill.Features.GameFlow
{
    /// <summary>
    /// The Queen (the hill's heart): the health pool enemies drain by walking in, and where a
    /// run ends in victory or defeat.
    /// </summary>
    /// <remarks>
    /// Listens to <see cref="EnemyMover.ReachedEnd"/> and takes each breaching enemy's
    /// <see cref="EnemyHealth.BreachDamage"/>; an enemy whose mover stops at the hill instead
    /// of going in does its own harm and is skipped. Nothing here is a singleton: whoever needs
    /// the Queen holds a reference or listens to its events.
    /// </remarks>
    [DisallowMultipleComponent]
    public class Queen : MonoBehaviour
    {
        [Header("Health Settings")]
        [Tooltip("Full health of the Queen. Permanent upgrades can raise it at the start of a run.")]
        [SerializeField, Min(1f)] private float _maxHealth = 100f;

        [Tooltip("If on, every enemy that walks off the end of its path damages the Queen. Off for a scene that reports breaches itself.")]
        [SerializeField] private bool _autoListenToEnemyEvents = true;

        [Header("Events")]
        [Tooltip("Invoked once when the Queen's health reaches 0 (defeat).")]
        [SerializeField] private UnityEvent _onGameOver = new UnityEvent();

        [Tooltip("Invoked whenever the Queen's health changes, with (current, max).")]
        [SerializeField] private UnityEvent<float, float> _onHealthChanged = new UnityEvent<float, float>();

        // NaN until Awake, so a Queen made in edit mode (a test, an editor tool) reads as full health.
        private float _currentHealth = float.NaN;
        private float _builtMaxHealth = float.NaN;

        /// <summary>Raised when this Queen's health changes, with (current, max).</summary>
        public event Action<float, float> OnHealthChanged;

        /// <summary>Raised once when this Queen's health reaches 0.</summary>
        public event Action OnGameOver;

        /// <summary>Raised once when the run ends: true for victory, false for defeat.</summary>
        public event Action<bool> Finished;

        /// <summary>Raised when an enemy breaches, with the damage and where it happened. For the breach text.</summary>
        public event Action<float, Vector3> Breached;

        /// <summary>Raised when any Queen's health reaches 0.</summary>
        public static event Action OnAnyGameOver;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            OnAnyGameOver = null;
        }

        /// <summary>Full health.</summary>
        public float MaxHealth => _maxHealth;

        /// <summary>Health right now.</summary>
        public float CurrentHealth
        {
            get => float.IsNaN(_currentHealth) ? _maxHealth : _currentHealth;
            private set => _currentHealth = value;
        }

        /// <summary>True once the run has ended either way.</summary>
        public bool IsFinished { get; private set; }

        /// <summary>True once the run has ended in victory.</summary>
        public bool IsVictory { get; private set; }

        /// <summary>True once the run has ended in defeat.</summary>
        public bool IsGameOver => IsFinished && !IsVictory;

        /// <summary>UnityEvent for game over (for Inspector wiring).</summary>
        public UnityEvent OnGameOverEvent => _onGameOver;

        /// <summary>UnityEvent for health change (for Inspector wiring).</summary>
        public UnityEvent<float, float> OnHealthChangedEvent => _onHealthChanged;

        protected virtual void Awake()
        {
            _builtMaxHealth = _maxHealth;
            CurrentHealth = _maxHealth;
        }

        protected virtual void OnEnable()
        {
            if (_autoListenToEnemyEvents)
            {
                EnemyMover.ReachedEnd += HandleEnemyReachedEnd;
            }
        }

        protected virtual void OnDisable()
        {
            if (_autoListenToEnemyEvents)
            {
                EnemyMover.ReachedEnd -= HandleEnemyReachedEnd;
            }
        }

        /// <summary>
        /// An enemy walked off the end of its path. Takes its breach damage, unless its mover
        /// stops at the hill rather than going in, or it carries no breach damage.
        /// </summary>
        public void HandleEnemyReachedEnd(GameObject enemy)
        {
            if (enemy == null || IsFinished)
            {
                return;
            }

            var mover = enemy.GetComponent<EnemyMover>();
            if (mover != null && !mover.DespawnsAtEnd)
            {
                return;
            }

            var health = enemy.GetComponent<EnemyHealth>();
            float damage = health != null ? health.BreachDamage : 0f;
            if (damage <= 0f)
            {
                return;
            }

            TakeDamage(damage, enemy.transform.position);
        }

        /// <summary>Takes damage at the Queen's own position.</summary>
        public void TakeDamage(float amount)
        {
            TakeDamage(amount, transform.position);
        }

        /// <summary>
        /// Takes damage from a breach at a position. Health stops at 0 and the defeat events fire
        /// once; nothing happens after the run has ended.
        /// </summary>
        public virtual void TakeDamage(float amount, Vector3 position)
        {
            if (amount <= 0f || IsFinished)
            {
                return;
            }

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            Breached?.Invoke(amount, position);
            RaiseHealthChanged();

            if (CurrentHealth <= 0f)
            {
                TriggerDefeat();
            }
        }

        /// <summary>Restores health, never above the maximum. Does nothing once the run has ended.</summary>
        public virtual void Heal(float amount)
        {
            if (amount <= 0f || IsFinished)
            {
                return;
            }

            CurrentHealth = Mathf.Min(_maxHealth, CurrentHealth + amount);
            RaiseHealthChanged();
        }

        /// <summary>Ends the run in victory, once.</summary>
        public void TriggerVictory()
        {
            if (IsFinished)
            {
                return;
            }

            IsFinished = true;
            IsVictory = true;
            Finished?.Invoke(true);
        }

        /// <summary>Ends the run in defeat, once.</summary>
        public void TriggerDefeat()
        {
            if (IsFinished)
            {
                return;
            }

            IsFinished = true;
            IsVictory = false;
            OnGameOver?.Invoke();
            OnAnyGameOver?.Invoke();
            _onGameOver?.Invoke();
            Finished?.Invoke(false);
        }

        /// <summary>Puts health back to full and clears a finished run.</summary>
        public virtual void ResetHealth()
        {
            RestoreHealth(_maxHealth);
        }

        /// <summary>Puts health to a saved value (clamped to the maximum) and clears a finished run.</summary>
        public void RestoreHealth(float health)
        {
            CurrentHealth = Mathf.Clamp(health, 0f, _maxHealth);
            IsFinished = false;
            IsVictory = false;
            RaiseHealthChanged();
        }

        /// <summary>
        /// Raises full health above what the scene was built with and refills it. For a permanent
        /// "tougher hill" upgrade at the start of a run; 0 puts it back.
        /// </summary>
        public void SetBonusHealth(float bonus)
        {
            if (float.IsNaN(_builtMaxHealth))
            {
                _builtMaxHealth = _maxHealth;
            }

            _maxHealth = _builtMaxHealth + Mathf.Max(0f, bonus);
            CurrentHealth = _maxHealth;
            RaiseHealthChanged();
        }

        /// <summary>Changes full health; current health is clamped to it, or refilled when asked.</summary>
        public void SetMaxHealth(float newMaxHealth, bool resetCurrentHealth = false)
        {
            _maxHealth = Mathf.Max(1f, newMaxHealth);
            if (resetCurrentHealth || CurrentHealth > _maxHealth)
            {
                CurrentHealth = _maxHealth;
            }

            RaiseHealthChanged();
        }

        private void RaiseHealthChanged()
        {
            OnHealthChanged?.Invoke(CurrentHealth, _maxHealth);
            _onHealthChanged?.Invoke(CurrentHealth, _maxHealth);
        }
    }
}
