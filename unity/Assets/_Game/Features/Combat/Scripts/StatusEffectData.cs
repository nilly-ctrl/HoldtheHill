using System;
using UnityEngine;

namespace HoldTheHill.Features.Combat
{
    /// <summary>
    /// A damage-over-time and/or slow that a hit can leave behind.
    /// Serializable so designers set it up on the projectile prefab.
    /// </summary>
    [Serializable]
    public struct StatusEffectData
    {
        [Tooltip("Identifies the effect. Re-applying the same name refreshes it instead of stacking.")]
        public string EffectName;

        [Tooltip("Damage dealt each tick. Zero for a pure slow.")]
        [Min(0f)] public float DamagePerTick;

        [Tooltip("Seconds between ticks. Values at or below zero are treated as one tick per second.")]
        [Min(0f)] public float TickInterval;

        [Tooltip("Total seconds the effect lasts. Zero or less means this effect does nothing.")]
        [Min(0f)] public float Duration;

        [Tooltip("Movement speed multiplier while active. 1 = no slow, 0.5 = half speed.")]
        [Range(0f, 1f)] public float SlowMultiplier;

        /// <summary>True if this effect would actually do something.</summary>
        public bool IsValid => Duration > 0f && (DamagePerTick > 0f || SlowMultiplier < 1f);

        /// <summary>Tick interval clamped to something safe to divide by.</summary>
        public float SafeTickInterval => TickInterval > 0.01f ? TickInterval : 1f;

        /// <summary>A no-op effect. Use for "this weapon applies nothing".</summary>
        public static StatusEffectData None => new StatusEffectData { SlowMultiplier = 1f };
    }
}
