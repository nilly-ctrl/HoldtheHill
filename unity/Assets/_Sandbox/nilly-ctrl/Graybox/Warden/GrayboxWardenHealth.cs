using System;
using HoldTheHill.Features.Combat;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The Warden's health. Anything that can hurt the Warden calls <see cref="TakeDamage"/>;
    /// a hit interrupts holds in progress, and at zero health the Warden is down for a few
    /// seconds, then gets up at the hill with full health.
    /// </summary>
    /// <remarks>
    /// Nothing hurts the Warden yet: what should is an open design question (docs/level-up-plan, W3).
    /// This is the part that answers once that is decided. It has no collider on purpose, so
    /// towers and area hazards cannot find it by themselves.
    /// </remarks>
    [AddComponentMenu("Hold the Hill/Graybox/Warden/Health")]
    [RequireComponent(typeof(GrayboxWarden))]
    public class GrayboxWardenHealth : MonoBehaviour, IDamageable, IHealthReadable
    {
        [SerializeField, Min(1f)] private float _maxHealth = 20f;

        [Tooltip("Health restored per second once the Warden has gone this long without being hit.")]
        [SerializeField, Min(0f)] private float _regenPerSecond = 1f;

        [SerializeField, Min(0f)] private float _regenDelay = 3f;

        [Tooltip("Seconds the Warden is down before it gets up at the hill.")]
        [SerializeField, Min(0f)] private float _downedSeconds = 5f;

        private GrayboxWarden _warden;
        private SpriteRenderer _renderer;
        private float _health;
        private float _lastHitAt = float.NegativeInfinity;
        private float _getUpAt;

        /// <summary>Raised after the Warden takes damage, with the health actually lost.</summary>
        public event Action<float> Hurt;

        /// <summary>Raised when health runs out.</summary>
        public event Action Downed;

        /// <summary>Raised when the Warden gets up again.</summary>
        public event Action Revived;

        public float CurrentHealth => _health;

        public float MaxHealth => _maxHealth;

        public float HealthFraction => _maxHealth > 0f ? Mathf.Clamp01(_health / _maxHealth) : 0f;

        /// <summary>True while down. Targeting skips the Warden then, and it cannot move or act.</summary>
        public bool IsDead { get; private set; }

        public bool IsDowned => IsDead;

        /// <summary>Seconds until the Warden gets up, or 0 when it is not down.</summary>
        public float DownedLeft => IsDead ? Mathf.Max(0f, _getUpAt - Time.time) : 0f;

        public Transform Transform => transform;

        private void Awake()
        {
            _warden = GetComponent<GrayboxWarden>();
            _renderer = GetComponent<SpriteRenderer>();
            _health = _maxHealth;
        }

        private void Update()
        {
            if (!GrayboxGameFlow.GameplayActive)
            {
                return;
            }

            if (IsDead)
            {
                if (Time.time >= _getUpAt)
                {
                    GetUp();
                }

                return;
            }

            if (_health < _maxHealth && _regenPerSecond > 0f && Time.time - _lastHitAt >= _regenDelay)
            {
                _health = Mathf.Min(_maxHealth, _health + _regenPerSecond * Time.deltaTime);
            }
        }

        /// <summary>Takes a hit. Ignored while the Warden is down.</summary>
        public void TakeDamage(DamageInfo info)
        {
            if (IsDead || info.Amount <= 0f)
            {
                return;
            }

            float lost = Mathf.Min(info.Amount, _health);
            _health -= lost;
            _lastHitAt = Time.time;
            _warden.InterruptAbilities(downed: false);
            Hurt?.Invoke(lost);

            if (_health <= 0f)
            {
                GoDown();
            }
        }

        /// <summary>The Warden is not slowed, burned or poisoned; status effects do nothing yet.</summary>
        public void ApplyStatusEffect(StatusEffectData status)
        {
        }

        /// <summary>Restores health, up to the maximum. Does not get a downed Warden up.</summary>
        public void Heal(float amount)
        {
            if (!IsDead && amount > 0f)
            {
                _health = Mathf.Min(_maxHealth, _health + amount);
            }
        }

        private void GoDown()
        {
            IsDead = true;
            _getUpAt = Time.time + _downedSeconds;
            _warden.InterruptAbilities(downed: true);
            SetFade(0.35f);
            Downed?.Invoke();
        }

        private void GetUp()
        {
            IsDead = false;
            _health = _maxHealth;
            _warden.ReturnToHill();
            SetFade(1f);
            Revived?.Invoke();
        }

        // A downed Warden shows as a faded copy of itself.
        private void SetFade(float alpha)
        {
            if (_renderer != null)
            {
                Color colour = _renderer.color;
                colour.a = alpha;
                _renderer.color = colour;
            }
        }
    }
}
