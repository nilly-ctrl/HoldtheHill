using System;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>How one kind of popup moves, scales and fades over its life.</summary>
    [Serializable]
    public struct DamageNumberMotion
    {
        [Tooltip("Seconds on screen.")]
        [Min(0.05f)] public float Lifetime;

        [Tooltip("Starting upward speed, world units per second.")]
        public float RiseSpeed;

        [Tooltip("Downward pull, world units per second squared. Higher = the number arcs and settles.")]
        [Min(0f)] public float Gravity;

        [Tooltip("Random sideways speed, plus or minus, world units per second.")]
        [Min(0f)] public float SidewaysSpeed;

        [Tooltip("Fraction of the lifetime after which the number fades out.")]
        [Range(0f, 1f)] public float FadeStart;

        [Tooltip("Scale on spawn; it eases down to 1. Above 1 gives a slam-in punch.")]
        [Min(0.1f)] public float PunchScale;

        [Tooltip("Seconds the punch takes to settle.")]
        [Min(0.01f)] public float PunchDuration;

        public static DamageNumberMotion Hit => new DamageNumberMotion
        {
            Lifetime = 0.75f, RiseSpeed = 3.2f, Gravity = 6f, SidewaysSpeed = 0.8f,
            FadeStart = 0.55f, PunchScale = 1.4f, PunchDuration = 0.12f,
        };

        public static DamageNumberMotion Crit => new DamageNumberMotion
        {
            Lifetime = 1.1f, RiseSpeed = 3.6f, Gravity = 6.5f, SidewaysSpeed = 0.4f,
            FadeStart = 0.7f, PunchScale = 1.8f, PunchDuration = 0.2f,
        };

        public static DamageNumberMotion Tick => new DamageNumberMotion
        {
            Lifetime = 0.55f, RiseSpeed = 1.6f, Gravity = 1.5f, SidewaysSpeed = 0.5f,
            FadeStart = 0.45f, PunchScale = 1f, PunchDuration = 0.05f,
        };

        public static DamageNumberMotion Heal => new DamageNumberMotion
        {
            Lifetime = 0.9f, RiseSpeed = 1.4f, Gravity = 0f, SidewaysSpeed = 0f,
            FadeStart = 0.6f, PunchScale = 1.2f, PunchDuration = 0.15f,
        };

        public static DamageNumberMotion Reward => new DamageNumberMotion
        {
            Lifetime = 1f, RiseSpeed = 1.8f, Gravity = 0.8f, SidewaysSpeed = 0f,
            FadeStart = 0.65f, PunchScale = 1.5f, PunchDuration = 0.18f,
        };
    }
}
