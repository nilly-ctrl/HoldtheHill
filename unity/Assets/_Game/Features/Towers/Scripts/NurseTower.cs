using HoldTheHill.Core.Animation;
using HoldTheHill.Features.GameFlow;
using HoldTheHill.Features.Combat;
using UnityEngine;

namespace HoldTheHill.Features.Towers
{
    /// <summary>
    /// The Nurse caste. She does not fight: every few seconds she mends the hill a little,
    /// while it is hurt. Her <see cref="HoldTheHill.Features.Towers.Tower"/> has no reach, so it
    /// never targets anything.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Graybox Nurse Tower")]
    public class NurseTower : SupportTower
    {
        private const int FxOrder = 4;

        [Tooltip("Hill health restored per pulse.")]
        [SerializeField, Min(0f)] private float _healAmount = 2f;
        [Tooltip("Seconds between pulses.")]
        [SerializeField, Min(0.1f)] private float _healInterval = 3f;

        private TowerAnimator _animator;
        private SpriteAnimLibrary _library;
        private Queen _hill;

        /// <summary>Pulses that mended the hill so far.</summary>
        public int Heals { get; private set; }

        protected override float Interval => _healInterval;

        private void Start()
        {
            _animator = GetComponent<TowerAnimator>();
            _hill = FindAnyObjectByType<Queen>();
            var player = GetComponent<SpriteClipPlayer>();
            _library = player != null ? player.Library : null;
            ResetCooldown(); // she has just arrived: the first pulse comes a full interval later
        }

        protected override void Trigger()
        {
            Queen hill = _hill;
            if (hill == null || hill.IsFinished || hill.CurrentHealth >= hill.MaxHealth)
            {
                return;
            }

            hill.Heal(_healAmount);
            Heals++;
            SpriteClipPlayer.SpawnOneShot(_library, "FxMelee", "Heal", transform.position, Quaternion.identity, 1f, FxOrder);
            if (_animator != null)
            {
                _animator.PlayAttack();
            }
        }
    }
}
