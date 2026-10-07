using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Enemies;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// An enemy in the air: drawn over everything on the ground, and untouched by what happens
    /// down there (fire puddles, mines, the Kicker shockwave and its knockback).
    /// </summary>
    /// <remarks>
    /// The shadow of a flyer is drawn into its sprite below the body, so it never rotates to
    /// face its heading; it flips left and right instead.
    /// </remarks>
    public class GrayboxFlyer : GrayboxSpecialEnemy, IIncomingDamageModifier
    {
        private const int AirOrder = 5; // over the hill prop (4)

        private SpriteRenderer _renderer;

        protected override void Awake()
        {
            base.Awake();
            _renderer = GetComponent<SpriteRenderer>();
        }

        protected virtual void Start()
        {
            if (Mover != null)
            {
                Mover.IgnoresKnockback = true;
            }

            if (_renderer != null)
            {
                _renderer.sortingOrder = AirOrder;
            }
        }

        public virtual float ModifyIncomingDamage(EnemyHealth health, DamageInfo info)
        {
            bool fromGround = IsGroundShock(info) || (info.Source != null && info.Source.GetComponent<GroundHazard>() != null);
            return fromGround ? 0f : info.Amount;
        }

        protected void FlipToward(Vector2 direction)
        {
            if (_renderer != null && Mathf.Abs(direction.x) > 0.01f)
            {
                _renderer.flipX = direction.x < 0f;
            }
        }

        /// <summary>Flies straight at a point. Returns true once it is there.</summary>
        protected bool FlyToward(Vector2 target, float speed, float arrive = 0.05f)
        {
            Vector2 position = transform.position;
            Vector2 to = target - position;
            if (to.magnitude <= arrive)
            {
                return true;
            }

            FlipToward(to);
            Vector2 next = Vector2.MoveTowards(position, target, speed * Health.SpeedMultiplier * Time.deltaTime);
            transform.position = new Vector3(next.x, next.y, transform.position.z);
            return false;
        }
    }
}
