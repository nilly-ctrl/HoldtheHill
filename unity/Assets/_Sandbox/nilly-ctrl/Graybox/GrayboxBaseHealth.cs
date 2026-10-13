using System;
using HoldTheHill.Features.GameFlow;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The graybox's hill: a <see cref="Queen"/> with the static <c>Instance</c> and events the
    /// sandbox still reaches for, and the breach text. A shim until the scenes hold a reference to
    /// the Queen themselves (step 3 of <c>docs/CODEBASE_CONVERGENCE.md</c>).
    /// </summary>
    public class GrayboxBaseHealth : Queen
    {
        public static GrayboxBaseHealth Instance { get; private set; }

        public static event Action<float, float> OnBaseHealthChanged;
        public static event Action<bool> OnGameFinished; // true for Victory, false for Defeat

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            OnBaseHealthChanged = null;
            OnGameFinished = null;
        }

        protected override void Awake()
        {
            base.Awake();
            Instance = this;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            OnHealthChanged += ForwardHealthChanged;
            Finished += ForwardFinished;
            Breached += ShowBreach;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            OnHealthChanged -= ForwardHealthChanged;
            Finished -= ForwardFinished;
            Breached -= ShowBreach;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private static void ForwardHealthChanged(float current, float max) => OnBaseHealthChanged?.Invoke(current, max);

        private static void ForwardFinished(bool victory) => OnGameFinished?.Invoke(victory);

        private static void ShowBreach(float damage, Vector3 position)
        {
            DamageNumberSpawner.ShowText($"BREACH! -{Mathf.RoundToInt(damage)} HP", DamageNumberKind.Fire, position);
        }

        /// <summary>The old name for <see cref="Queen.TakeDamage(float, Vector3)"/>.</summary>
        public void TakeBaseDamage(float damage, Vector3 position) => TakeDamage(damage, position);

        /// <summary>The old name for <see cref="Queen.ResetHealth"/>.</summary>
        public void ResetBaseHealth() => ResetHealth();
    }
}
