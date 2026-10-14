using HoldTheHill.Features.DamageNumbers;
using HoldTheHill.Features.GameFlow;
using UnityEngine;

namespace HoldTheHill.Features.GameFlow
{
    /// <summary>
    /// The graybox hill: a <see cref="Queen"/> that shows the breach text. No statics: the sandbox
    /// finds the hill once and listens to the Queen events themselves (step 3 of
    /// <c>docs/CODEBASE_CONVERGENCE.md</c>).
    /// </summary>
    public class GrayboxBaseHealth : Queen
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            Breached += ShowBreach;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            Breached -= ShowBreach;
        }

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
