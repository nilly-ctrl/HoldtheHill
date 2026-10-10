using System.Collections.Generic;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Towers;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The three close-range castes. Each sits on an ordinary <see cref="Tower"/>, which does the
    /// targeting and the timing; this strikes whenever that tower fires.
    /// </summary>
    /// <remarks>
    ///   Worker   bites the one enemy the tower is targeting. Cheap and quick.
    ///   Soldier  slashes: hits the target and anything standing right beside it.
    ///   Major    slams the ground: hits everything in range and shoves it back down the trail.
    ///
    /// The Nurse, who does not fight, is <see cref="GrayboxNurseTower"/>.
    /// Damage names this tower as its source, so kills and damage are credited to it.
    /// Numbers are first guesses for the graybox, not balanced.
    /// </remarks>
    [AddComponentMenu("Hold the Hill/Graybox/Graybox Melee Tower")]
    public class GrayboxMeleeTower : MeleeTower
    {
        public enum Caste
        {
            Worker,
            Soldier,
            Major,
        }

        /// <summary>What the placer needs to build one.</summary>
        public readonly struct Stats
        {
            public readonly float Range;
            public readonly float Interval;
            public readonly float Damage;

            public Stats(float range, float interval, float damage)
            {
                Range = range;
                Interval = interval;
                Damage = damage;
            }
        }

        private const float SoldierSweep = 0.9f; // radius around the target a slash also hits
        private const float MajorShove = 0.5f;   // world units back along the trail
        private const int FxOrder = 4;

        [SerializeField] private Caste _caste;
        [SerializeField, Min(0f)] private float _damage = 9f;

        private readonly List<IDamageable> _hits = new List<IDamageable>();
        private Tower _tower;
        private SpriteAnimLibrary _library;

        public Caste Kind => _caste;

        /// <summary>Strikes landed so far.</summary>
        public int Strikes { get; private set; }

        public static Stats StatsFor(Caste caste)
        {
            switch (caste)
            {
                case Caste.Soldier: return new Stats(2.0f, 0.9f, 16f);
                case Caste.Major: return new Stats(2.0f, 1.8f, 28f);
                default: return new Stats(1.8f, 0.5f, 9f);
            }
        }

        /// <summary>Sets the caste and its damage. Range and interval go on the Tower.</summary>
        public void Configure(Caste caste)
        {
            _caste = caste;
            _damage = StatsFor(caste).Damage;
        }

        private void Start()
        {
            _tower = GetComponent<Tower>();
            var player = GetComponent<SpriteClipPlayer>();
            _library = player != null ? player.Library : null;
        }

        public override void Shoot(IDamageable target)
        {
            if (CombatUtil.IsAlive(target))
            {
                Strike(target);
            }
        }

        private void Strike(IDamageable target)
        {
            Vector2 at = target.Transform.position;
            Vector2 toTarget = at - (Vector2)transform.position;
            Strikes++;

            switch (_caste)
            {
                case Caste.Worker:
                    Hurt(target);
                    Fx("Bite", at, 0f, 1f);
                    break;

                case Caste.Soldier:
                    CombatUtil.OverlapDamageables(at, SoldierSweep, ~0, _hits);
                    foreach (IDamageable hit in _hits)
                    {
                        Hurt(hit);
                    }

                    Fx("Slash", at, Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg - 90f, 1f);
                    break;

                case Caste.Major:
                    CombatUtil.OverlapDamageables(transform.position, _tower.Range, ~0, _hits);
                    foreach (IDamageable hit in _hits)
                    {
                        Hurt(hit);
                        if (CombatUtil.IsAlive(hit) && hit.Transform.TryGetComponent(out EnemyMover mover))
                        {
                            mover.Knockback(MajorShove);
                        }
                    }

                    Fx("Slam", transform.position, 0f, 2f);
                    Fx("Block", at, Mathf.Atan2(-toTarget.y, -toTarget.x) * Mathf.Rad2Deg, 1f);
                    break;
            }
        }

        private void Hurt(IDamageable target)
        {
            if (CombatUtil.IsAlive(target))
            {
                target.TakeDamage(new DamageInfo(_damage, gameObject, target.Transform.position, DamageType.Physical));
            }
        }

        private void Fx(string clip, Vector2 at, float angle, float scale)
        {
            SpriteClipPlayer.SpawnOneShot(_library, "FxMelee", clip, at, Quaternion.Euler(0f, 0f, angle), scale, FxOrder);
        }
    }
}
