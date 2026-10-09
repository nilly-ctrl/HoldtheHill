using System;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Something the Warden can do. Each ability is a child prefab of the Warden prefab, so
    /// giving the Warden a new one, or a species a different set, is adding or removing a child.
    /// </summary>
    /// <remarks>
    /// The base class handles the key, the hold and the cooldown from <see cref="GrayboxAbilityData"/>.
    /// A subclass says when it can be used (<see cref="CanUse"/>) and what it does (<see cref="Perform"/>).
    /// </remarks>
    public abstract class GrayboxWardenAbility : MonoBehaviour
    {
        [SerializeField] private GrayboxAbilityData _data;

        private float _readyAt;
        private float _held;

        /// <summary>Raised after any ability is used.</summary>
        public static event Action<GrayboxWardenAbility> Used;

        public GrayboxAbilityData Data => _data;

        /// <summary>Times this ability has been used.</summary>
        public int Uses { get; private set; }

        public float CooldownLeft => Mathf.Max(0f, _readyAt - Time.time);

        public bool IsReady => CooldownLeft <= 0f;

        /// <summary>How far through its hold the key is, 0 to 1. Always 0 for a press ability.</summary>
        public float HoldProgress => HoldSeconds > 0f ? Mathf.Clamp01(_held / HoldSeconds) : 0f;

        protected GrayboxWarden Warden { get; private set; }

        private float HoldSeconds => _data != null ? _data.HoldSeconds : 0f;

        protected virtual void Awake()
        {
            Warden = GetComponentInParent<GrayboxWarden>();
        }

        /// <summary>True when using the ability now would do something: a target is in reach, and so on.</summary>
        public virtual bool CanUse() => true;

        protected abstract void Perform();

        /// <summary>
        /// The Warden was hit, or went down. A hold in progress starts over; a subclass can do more,
        /// such as drop what it carries when <paramref name="downed"/>.
        /// </summary>
        public virtual void Interrupt(bool downed)
        {
            _held = 0f;
        }

        /// <summary>Called by the Warden every frame with the state of the key for this ability.</summary>
        public void Tick(bool pressedThisFrame, bool held)
        {
            if (HoldSeconds <= 0f)
            {
                if (pressedThisFrame)
                {
                    TryUse();
                }

                return;
            }

            if (!held || !IsReady || !CanUse())
            {
                _held = 0f;
                return;
            }

            _held += Time.deltaTime;
            if (_held >= HoldSeconds)
            {
                _held = 0f;
                TryUse();
            }
        }

        /// <summary>Uses the ability now, skipping the hold, if it is off cooldown and has something to do.</summary>
        public bool TryUse()
        {
            if (!IsReady || !CanUse())
            {
                return false;
            }

            Perform();
            Uses++;
            _readyAt = Time.time + (_data != null ? _data.Cooldown : 0f);
            Used?.Invoke(this);
            return true;
        }
    }
}
