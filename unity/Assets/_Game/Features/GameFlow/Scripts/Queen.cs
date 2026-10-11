using System;
using UnityEngine;
using UnityEngine.Events;
using HoldTheHill.Features.Enemies;

namespace HoldTheHill.Features.GameFlow
{
    /// <summary>
    /// Represents the Queen object (Ant Queen), which acts as the core health pool for the player / hill.
    /// Reduces health by enemy damage when a BasicEnemy reaches its destination.
    /// Fires a Game Over event when health drops to 0 or below.
    /// </summary>
    [DisallowMultipleComponent]
    public class Queen : MonoBehaviour
    {
        [Header("Health Settings")]
        [Tooltip("Maximum health pool of the Queen as a double.")]
        [SerializeField] private double _maxHealth = 100.0;

        [Tooltip("If true, automatically listens to all BasicEnemy instances reaching their destination via global static event.")]
        [SerializeField] private bool _autoListenToEnemyEvents = true;

        [Header("Events")]
        [Tooltip("UnityEvent invoked when the Queen's health drops to 0 or below (Game Over).")]
        [SerializeField] private UnityEvent _onGameOver = new UnityEvent();

        [Tooltip("UnityEvent invoked whenever the Queen's health changes. Passes (currentHealth, maxHealth).")]
        [SerializeField] private UnityEvent<double, double> _onHealthChanged = new UnityEvent<double, double>();

        private double _currentHealth = 100.0;
        //never be game over - jason
        private bool _isGameOver;

        // C# Events for decoupled code listeners
        public event Action OnGameOver;
        public static event Action OnAnyGameOver;
        public event Action<double, double> OnHealthChanged;

        /// <summary>Maximum health pool value as a double.</summary>
        public double MaxHealth => _maxHealth;

        /// <summary>Current health value as a double.</summary>
        public double CurrentHealth => _currentHealth;

        /// <summary>True if health has depleted to 0 or below and the Game Over event has fired.</summary>
        public bool IsGameOver => _isGameOver;

        /// <summary>UnityEvent for Game Over (for inspector wiring).</summary>
        public UnityEvent OnGameOverEvent => _onGameOver;

        /// <summary>UnityEvent for health change (for inspector wiring).</summary>
        public UnityEvent<double, double> OnHealthChangedEvent => _onHealthChanged;

        protected virtual void Awake()
        {
            _currentHealth = _maxHealth;
        }

        protected virtual void OnEnable()
        {
            if (_autoListenToEnemyEvents)
            {
                BasicEnemy.OnAnyDestinationReached += HandleEnemyReachedDestination;
            }
        }

        protected virtual void OnDisable()
        {
            if (_autoListenToEnemyEvents)
            {
                BasicEnemy.OnAnyDestinationReached -= HandleEnemyReachedDestination;
            }
        }

        /// <summary>
        /// Handler for when an enemy reaches the Queen's location (the end of the path).
        /// Subtracts the enemy's damage value from the Queen's health pool.
        /// </summary>
        /// <param name="enemy">The enemy that completed its path.</param>
        public virtual void HandleEnemyReachedDestination(BasicEnemy enemy)
        {
            if (enemy == null || _isGameOver)
            {
                return;
            }

            TakeDamage(enemy.Damage);
        }

        /// <summary>
        /// Subtracts damage from the Queen's health pool.
        /// If health reaches or drops below 0, triggers the Game Over event.
        /// </summary>
        /// <param name="damageAmount">Damage to inflict on the Queen.</param>
        public virtual void TakeDamage(double damageAmount)
        {
            if (damageAmount <= 0.0 || _isGameOver)
            {
                return;
            }

            _currentHealth -= damageAmount;
            if (_currentHealth <= 0.0)
            {
                _currentHealth = 0.0;
            }

            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            _onHealthChanged?.Invoke(_currentHealth, _maxHealth);

            if (_currentHealth <= 0.0 && !_isGameOver)
            {
                TriggerGameOver();
            }
        }

        /// <summary>
        /// Restores health up to the maximum health limit.
        /// </summary>
        /// <param name="healAmount">Amount of health to restore.</param>
        public virtual void Heal(double healAmount)
        {
            if (healAmount <= 0.0 || _isGameOver)
            {
                return;
            }

            _currentHealth = Math.Min(_maxHealth, _currentHealth + healAmount);
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            _onHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        /// <summary>
        /// Resets the Queen's health back to maximum and clears the Game Over state.
        /// </summary>
        public virtual void ResetHealth()
        {
            _currentHealth = _maxHealth;
            _isGameOver = false;
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            _onHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        /// <summary>
        /// Updates the maximum health pool and optionally clamps/resets current health.
        /// </summary>
        /// <param name="newMaxHealth">The new max health value.</param>
        /// <param name="resetCurrentHealth">If true, sets current health equal to new max health.</param>
        public void SetMaxHealth(double newMaxHealth, bool resetCurrentHealth = false)
        {
            _maxHealth = Math.Max(0.0, newMaxHealth);
            if (resetCurrentHealth || _currentHealth > _maxHealth)
            {
                _currentHealth = _maxHealth;
            }
            OnHealthChanged?.Invoke(_currentHealth, _maxHealth);
            _onHealthChanged?.Invoke(_currentHealth, _maxHealth);
        }

        /// <summary>
        /// Explicitly subscribes to a specific enemy's destination reached event.
        /// Useful when auto-listening is disabled or for manual spawner wiring.
        /// </summary>
        /// <param name="enemy">The enemy to monitor.</param>
        public void RegisterEnemy(BasicEnemy enemy)
        {
            if (enemy != null)
            {
                enemy.OnDestinationReached += HandleEnemyReachedDestination;
            }
        }

        /// <summary>
        /// Explicitly unsubscribes from a specific enemy's destination reached event.
        /// </summary>
        /// <param name="enemy">The enemy to stop monitoring.</param>
        public void UnregisterEnemy(BasicEnemy enemy)
        {
            if (enemy != null)
            {
                enemy.OnDestinationReached -= HandleEnemyReachedDestination;
            }
        }

        /// <summary>
        /// Executes the Game Over sequence once when health is depleted.
        /// </summary>
        protected virtual void TriggerGameOver()
        {
            if (_isGameOver)
            {
                return;
            }

            _isGameOver = true;
            Debug.Log("[Queen] Queen health depleted to 0 or below. Game Over triggered!");
            OnGameOver?.Invoke();
            OnAnyGameOver?.Invoke();
            _onGameOver?.Invoke();
        }
    }
}
