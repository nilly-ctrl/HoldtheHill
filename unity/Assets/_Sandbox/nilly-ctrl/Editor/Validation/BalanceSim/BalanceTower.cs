using System;

namespace HoldTheHill.Sandbox.NillyCtrl.Editor
{
    /// <summary>
    /// A tower reduced to the numbers the analytic balance model uses. Plain data, no Unity types.
    /// </summary>
    public sealed class BalanceTower
    {
        public BalanceTower(string name, int cost, float range, float dpsPerTarget,
            int maxTargets = 1, float falloffPerTarget = 1f, string note = null)
        {
            Name = name ?? string.Empty;
            Cost = cost;
            Range = range;
            DpsPerTarget = dpsPerTarget;
            MaxTargets = Math.Max(1, maxTargets);
            FalloffPerTarget = falloffPerTarget;
            Note = note ?? string.Empty;
        }

        public string Name { get; }

        public int Cost { get; }

        /// <summary>Reach in world units, measured from the tower.</summary>
        public float Range { get; }

        /// <summary>Damage per second against the first target it hits.</summary>
        public float DpsPerTarget { get; }

        /// <summary>How many enemies one tower can hit at once (splash, chain, pierce, pulse).</summary>
        public int MaxTargets { get; }

        /// <summary>Each further target takes this fraction of the previous one's damage (1 = no falloff).</summary>
        public float FalloffPerTarget { get; }

        /// <summary>How the numbers were derived, or what was left out.</summary>
        public string Note { get; }

        /// <summary>True when the tower is modelled as doing damage at all.</summary>
        public bool DealsDamage => DpsPerTarget > 0f;

        /// <summary>Damage per second against a crowd thick enough to fill every target slot.</summary>
        public float CrowdDps
        {
            get
            {
                float total = 0f;
                float factor = 1f;
                for (int i = 0; i < MaxTargets; i++)
                {
                    total += DpsPerTarget * factor;
                    factor *= FalloffPerTarget;
                }

                return total;
            }
        }
    }
}
