using System.Collections;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Towers;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Boss. Armoured from the front: hits from towers ahead of it are cut down, hits from
    /// behind land in full. At two health marks it roars (stunning towers near it), charges a
    /// stretch of path that slows cannot hold back, and loses a wing-case plate.
    /// </summary>
    /// <remarks>
    /// Needs a mover that faces its direction of travel, or "ahead" means nothing. A mine or a
    /// Kicker shockwave landing during the charge ends it early. Damage over time has the enemy
    /// itself as its source, so poison and burn always go round the armour.
    /// </remarks>
    public class GrayboxTitanBeetle : GrayboxSpecialEnemy, IIncomingDamageModifier
    {
        [Header("Front armour: share of a frontal hit that gets through")]
        [SerializeField, Range(0f, 1f)] private float _frontIntact = 0.35f;
        [SerializeField, Range(0f, 1f)] private float _frontCracked = 0.55f;
        [SerializeField, Range(0f, 1f)] private float _frontBare = 0.8f;

        [Header("Roar and charge")]
        [SerializeField, Range(0f, 1f)] private float _firstMark = 0.66f;
        [SerializeField, Range(0f, 1f)] private float _secondMark = 0.33f;
        [SerializeField, Min(0.5f)] private float _roarRadius = 3.5f;
        [SerializeField, Min(0.1f)] private float _roarStunSeconds = 2.5f;
        [SerializeField, Min(1f)] private float _chargeSpeedScale = 3.2f;
        [SerializeField, Min(0.5f)] private float _chargeDistance = 5f;
        [SerializeField, Min(0.5f)] private float _chargeMaxSeconds = 3.5f;

        private int _platesLost;
        private bool _inPhase;
        private bool _charging;
        private bool _interrupted;

        /// <summary>0 with both plates, 1 after the first charge, 2 after the second.</summary>
        public int PlatesLost => _platesLost;

        public bool IsCharging => _charging;

        protected override void OnEnable()
        {
            base.OnEnable();
            _platesLost = 0;
            _inPhase = _charging = _interrupted = false;
        }

        public float ModifyIncomingDamage(EnemyHealth health, DamageInfo info)
        {
            if (_charging && IsGroundShock(info))
            {
                _interrupted = true;
            }

            GameObject owner = CombatUtil.OwnerOf(info.Source);
            if (owner == null || owner == gameObject)
            {
                return info.Amount;
            }

            Vector2 toAttacker = owner.transform.position - transform.position;
            bool fromAhead = Vector2.Dot(transform.right, toAttacker) > 0f;
            if (!fromAhead)
            {
                return info.Amount;
            }

            float through = _platesLost == 0 ? _frontIntact : _platesLost == 1 ? _frontCracked : _frontBare;
            return info.Amount * through;
        }

        private void Update()
        {
            if (_inPhase || !Alive)
            {
                return;
            }

            float mark = _platesLost == 0 ? _firstMark : _platesLost == 1 ? _secondMark : -1f;
            if (Health.HealthFraction <= mark)
            {
                StartCoroutine(RoarAndCharge());
            }
        }

        private IEnumerator RoarAndCharge()
        {
            _inPhase = true;
            Mover.SpeedScale = 0f;

            Play("Roar");
            foreach (Tower tower in ActiveTowers())
            {
                if (Vector2.Distance(tower.transform.position, transform.position) <= _roarRadius)
                {
                    GrayboxTowerAffliction.Apply(tower, GrayboxTowerAffliction.Kind.Stun, _roarStunSeconds);
                }
            }

            yield return new WaitForSeconds(ClipLength("Roar", 0.63f));
            Play("ChargeWindup");
            yield return new WaitForSeconds(ClipLength("ChargeWindup", 0.44f));

            _charging = true;
            _interrupted = false;
            Mover.IgnoresKnockback = true;
            SetLoop("Charge");
            float start = Mover.DistanceTravelled;
            float elapsed = 0f;
            while (Alive && !_interrupted && elapsed < _chargeMaxSeconds && Mover.DistanceTravelled - start < _chargeDistance)
            {
                // Dividing the slow back out is what makes the charge ignore frost, web and honey.
                Mover.SpeedScale = _chargeSpeedScale / Mathf.Max(0.05f, Health.SpeedMultiplier);
                elapsed += Time.deltaTime;
                yield return null;
            }

            _charging = false;
            Mover.IgnoresKnockback = false;
            if (_interrupted)
            {
                Say("CHARGE BROKEN", DamageNumberKind.Critical, transform.position);
            }

            _platesLost++;
            SetLoop(_platesLost == 1 ? "WalkCracked" : "WalkBare");
            Play(_platesLost == 1 ? "PlateBreak" : "PlateBreak2");
            Mover.SpeedScale = 0f;
            yield return new WaitForSeconds(ClipLength("PlateBreak", 0.56f));

            // With nothing left to carry it walks a little faster.
            Mover.SpeedScale = _platesLost >= 2 ? 1.15f : 1f;
            _inPhase = false;
        }
    }
}
