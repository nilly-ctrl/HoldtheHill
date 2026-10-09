using System;
using System.Collections.Generic;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// A power-up while it is running. One of these is made from the effect prefab when the
    /// power-up is collected, counts down, and removes itself. Collecting the same power-up
    /// again restarts the clock instead of stacking a second one.
    /// </summary>
    /// <remarks>
    /// A subclass says what starts in <see cref="OnBegin"/> and what is undone in
    /// <see cref="OnEnd"/>. A HUD shows timers by reading <see cref="Active"/>.
    /// </remarks>
    public abstract class GrayboxPowerUpEffect : MonoBehaviour
    {
        private static readonly List<GrayboxPowerUpEffect> s_active = new List<GrayboxPowerUpEffect>();

        private bool _ended;

        /// <summary>Raised when a power-up starts or is restarted.</summary>
        public static event Action<GrayboxPowerUpEffect> Started;

        /// <summary>Every power-up running now.</summary>
        public static IReadOnlyList<GrayboxPowerUpEffect> Active => s_active;

        public GrayboxPowerUpData Data { get; private set; }

        public float Remaining { get; private set; }

        /// <summary>Starts a power-up, or restarts its clock if it is already running.</summary>
        public static GrayboxPowerUpEffect Activate(GrayboxPowerUpData data)
        {
            if (data == null || data.EffectPrefab == null)
            {
                return null;
            }

            foreach (GrayboxPowerUpEffect running in s_active)
            {
                if (running.Data == data)
                {
                    running.Remaining = data.Duration;
                    Started?.Invoke(running);
                    return running;
                }
            }

            GrayboxPowerUpEffect effect = Instantiate(data.EffectPrefab);
            effect.name = data.DisplayName;
            effect.Data = data;
            effect.Remaining = data.Duration;
            s_active.Add(effect);
            effect.OnBegin();
            Started?.Invoke(effect);
            return effect;
        }

        /// <summary>Ends every running power-up, for a restart.</summary>
        public static void EndAll()
        {
            for (int i = s_active.Count - 1; i >= 0; i--)
            {
                s_active[i].End();
            }
        }

        protected abstract void OnBegin();

        protected abstract void OnEnd();

        /// <summary>Called every frame while running, after the clock has moved.</summary>
        protected virtual void OnTick()
        {
        }

        private void Update()
        {
            if (_ended || !GrayboxGameFlow.GameplayActive)
            {
                return;
            }

            Remaining -= Time.deltaTime;
            if (Remaining <= 0f)
            {
                End();
                return;
            }

            OnTick();
        }

        /// <summary>Stops the power-up now and removes it.</summary>
        public void End()
        {
            if (_ended)
            {
                return;
            }

            _ended = true;
            s_active.Remove(this);
            OnEnd();
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            // Destroyed from outside (the scene closing): still undo what it did.
            if (!_ended)
            {
                _ended = true;
                s_active.Remove(this);
                OnEnd();
            }
        }
    }
}
