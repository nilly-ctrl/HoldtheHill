using System.Collections;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Enemies;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Boss. A giant pill bug that walks for a while, then rolls up and bowls down the path:
    /// fast, unslowable and immune to everything. A Kicker shockwave or a mine pops it open and
    /// leaves it on its back for a few seconds, taking extra damage.
    /// </summary>
    public class GrayboxBoulderBug : GrayboxSpecialEnemy, IIncomingDamageModifier
    {
        [SerializeField, Min(0.5f)] private float _walkSeconds = 5f;
        [SerializeField, Min(0.5f)] private float _rollSeconds = 6f;
        [SerializeField, Min(1f)] private float _rollSpeedScale = 2.8f;
        [SerializeField, Min(0.5f)] private float _stunSeconds = 4f;
        [SerializeField, Min(1f)] private float _stunDamageScale = 1.5f;

        public enum State
        {
            Walking,
            Rolling,
            OnItsBack,
        }

        private bool _popped;
        private float _nextNotice;

        public State Current { get; private set; }

        protected override void OnEnable()
        {
            base.OnEnable();
            Current = State.Walking;
            _popped = false;
        }

        private void Start()
        {
            StartCoroutine(Cycle());
        }

        public float ModifyIncomingDamage(EnemyHealth health, DamageInfo info)
        {
            switch (Current)
            {
                case State.Rolling:
                    if (IsGroundShock(info))
                    {
                        // The hit that opens it lands too, and its knockback with it.
                        _popped = true;
                        Current = State.OnItsBack;
                        Mover.IgnoresKnockback = false;
                        return info.Amount * _stunDamageScale;
                    }

                    if (Time.time >= _nextNotice)
                    {
                        _nextNotice = Time.time + 1f;
                        Say("IMMUNE", DamageNumberKind.Physical, transform.position);
                    }

                    return 0f;

                case State.OnItsBack:
                    return info.Amount * _stunDamageScale;

                default:
                    return info.Amount;
            }
        }

        private IEnumerator Cycle()
        {
            yield return new WaitForSeconds(ClipLength("Spawn", 0.5f));

            while (Alive)
            {
                Current = State.Walking;
                SetLoop("Walk");
                Mover.SpeedScale = 1f;
                yield return new WaitForSeconds(_walkSeconds);
                if (!Alive)
                {
                    yield break;
                }

                Mover.SpeedScale = 0f;
                Play("RollUp");
                yield return new WaitForSeconds(ClipLength("RollUp", 0.4f));

                _popped = false;
                Current = State.Rolling;
                Mover.IgnoresKnockback = true;
                SetLoop("Roll");
                for (float t = 0f; t < _rollSeconds && Alive && !_popped; t += Time.deltaTime)
                {
                    Mover.SpeedScale = _rollSpeedScale / Mathf.Max(0.05f, Health.SpeedMultiplier);
                    yield return null;
                }

                Mover.IgnoresKnockback = false;
                if (!_popped)
                {
                    continue; // unrolls by itself and walks on
                }

                // Popped open.
                Mover.SpeedScale = 0f;
                if (Animator != null)
                {
                    Animator.PlayHurt = false; // the Hurt clip is drawn in the walking pose
                }

                Say("POPPED OPEN", DamageNumberKind.Critical, transform.position);
                SetLoop("Stunned");
                Play("Pop");
                yield return new WaitForSeconds(_stunSeconds);
                if (!Alive)
                {
                    yield break;
                }

                SetLoop("Walk");
                Play("Recover");
                yield return new WaitForSeconds(ClipLength("Recover", 0.45f));
                if (Animator != null)
                {
                    Animator.PlayHurt = true;
                }
            }
        }
    }
}
