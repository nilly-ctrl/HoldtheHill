using System;
using HoldTheHill.Features.Enemies;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Base Health tracking system monitoring ant breaches, applying breach damage to the Hill,
    /// and driving Game Over (Defeat) / Hill Defended (Victory) UI states.
    /// </summary>
    public class GrayboxBaseHealth : MonoBehaviour
    {
        public static GrayboxBaseHealth Instance { get; private set; }

        public static event Action<float, float> OnBaseHealthChanged;
        public static event Action<bool> OnGameFinished; // true for Victory, false for Defeat

        [Header("Hill Base Health")]
        [SerializeField] private float _maxHealth = 100f;

        public float CurrentHealth { get; private set; }
        public float MaxHealth => _maxHealth;
        public bool IsFinished { get; private set; }
        public bool IsVictory { get; private set; }

        private float _builtMaxHealth;

        private void Awake()
        {
            Instance = this;
            _builtMaxHealth = _maxHealth;
            CurrentHealth = _maxHealth;
        }

        /// <summary>
        /// Raises the hill's full health above what the scene was built with and refills it. Called
        /// at the start of a run with the permanent "tougher hill" upgrade; 0 puts it back.
        /// </summary>
        public void SetBonusHealth(float bonus)
        {
            _maxHealth = _builtMaxHealth + Mathf.Max(0f, bonus);
            CurrentHealth = _maxHealth;
            OnBaseHealthChanged?.Invoke(CurrentHealth, _maxHealth);
        }

        private void OnEnable()
        {
            EnemyMover.ReachedEnd += OnAntBreachedBase;
        }

        private void OnDisable()
        {
            EnemyMover.ReachedEnd -= OnAntBreachedBase;
        }

        private void OnAntBreachedBase(GameObject antObj)
        {
            if (IsFinished || antObj == null) return;

            // One that stops at the hill instead of going in (the thief ant) does its own harm.
            var mover = antObj.GetComponent<EnemyMover>();
            if (mover != null && !mover.DespawnsAtEnd) return;

            float breachDamage = 5f;
            string antName = antObj.name;

            if (antName.Contains("Brute")) breachDamage = 25f;
            else if (antName.Contains("Shielded")) breachDamage = 12f;
            else if (antName.Contains("Healer")) breachDamage = 15f;
            else if (antName.Contains("Grunt")) breachDamage = 10f;
            else if (antName.Contains("Splitter")) breachDamage = 8f;
            else if (antName.Contains("Runner")) breachDamage = 5f;
            else if (antName.Contains("Swarm")) breachDamage = 2f;
            var special = antObj.GetComponent<GrayboxSpecialEnemy>();
            if (special != null) breachDamage = special.BreachDamage;

            TakeBaseDamage(breachDamage, antObj.transform.position);
        }

        public void TakeBaseDamage(float damage, Vector3 position)
        {
            if (IsFinished) return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
            OnBaseHealthChanged?.Invoke(CurrentHealth, _maxHealth);

            DamageNumberSpawner.ShowText($"BREACH! -{Mathf.RoundToInt(damage)} HP", DamageNumberKind.Fire, position);

            if (CurrentHealth <= 0f)
            {
                TriggerDefeat();
            }
        }

        public void TriggerVictory()
        {
            if (IsFinished) return;
            IsFinished = true;
            IsVictory = true;
            OnGameFinished?.Invoke(true);
        }

        public void TriggerDefeat()
        {
            if (IsFinished) return;
            IsFinished = true;
            IsVictory = false;
            OnGameFinished?.Invoke(false);
        }

        /// <summary>Puts the hill back to a saved health value and clears a finished game.</summary>
        public void RestoreHealth(float health)
        {
            CurrentHealth = Mathf.Clamp(health, 0f, _maxHealth);
            IsFinished = false;
            IsVictory = false;
            OnBaseHealthChanged?.Invoke(CurrentHealth, _maxHealth);
        }

        public void ResetBaseHealth()
        {
            CurrentHealth = _maxHealth;
            IsFinished = false;
            IsVictory = false;
            OnBaseHealthChanged?.Invoke(CurrentHealth, _maxHealth);
        }
    }
}
