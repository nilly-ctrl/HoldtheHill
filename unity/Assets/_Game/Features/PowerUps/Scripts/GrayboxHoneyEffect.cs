using System.Collections.Generic;
using HoldTheHill.Features.Combat;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Honey Drop: while it lasts every raider on the map is stuck in honey and walks slower.
    /// </summary>
    /// <remarks>
    /// A first guess. The design documents name the Honey Drop and say the Warden carries it,
    /// but not what it does; swap this component on the effect prefab when that is decided.
    /// </remarks>
    [AddComponentMenu("Hold the Hill/Graybox/Power-Ups/Honey Effect")]
    public class GrayboxHoneyEffect : GrayboxPowerUpEffect
    {
        private const float Reapply = 0.5f;
        private const float WholeMap = 200f;

        [Tooltip("Enemy speed while honeyed. 0.5 is half speed.")]
        [SerializeField, Range(0.1f, 0.95f)] private float _slowMultiplier = 0.5f;

        private readonly List<IDamageable> _enemies = new List<IDamageable>();
        private float _next;

        protected override void OnBegin()
        {
            Slow();
        }

        protected override void OnEnd()
        {
            // Nothing to undo: the slow is a one-second status that is no longer renewed.
        }

        protected override void OnTick()
        {
            _next -= Time.deltaTime;
            if (_next <= 0f)
            {
                Slow();
            }
        }

        // Renewed twice a second, so raiders that arrive later are caught too.
        private void Slow()
        {
            _next = Reapply;
            var honeyed = new StatusEffectData
            {
                EffectName = "Honeyed",
                TickInterval = Reapply,
                Duration = Reapply * 2f,
                SlowMultiplier = _slowMultiplier,
            };

            CombatUtil.OverlapDamageables(Vector2.zero, WholeMap, ~0, _enemies);
            foreach (IDamageable enemy in _enemies)
            {
                if (CombatUtil.IsAlive(enemy))
                {
                    enemy.ApplyStatusEffect(honeyed);
                }
            }
        }
    }
}
