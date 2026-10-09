using System.Collections.Generic;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Towers;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Shared plumbing for the bosses and special enemies: the components they all read, the
    /// three events they all listen for, and the questions they all ask about towers and the path.
    /// </summary>
    /// <remarks>
    /// Movement stays with <see cref="EnemyMover"/>. A special enemy steers it through
    /// <see cref="EnemyMover.SpeedScale"/>, or sets that to zero and moves its own transform when
    /// it has to leave the path (the hornet, the orb-weaver on a thread, the thief running home).
    /// </remarks>
    [RequireComponent(typeof(EnemyHealth))]
    public abstract class GrayboxSpecialEnemy : MonoBehaviour
    {
        [Tooltip("Hill health lost if this enemy gets in.")]
        [SerializeField, Min(0f)] private float _breachDamage = 10f;

        protected EnemyHealth Health { get; private set; }

        protected EnemyMover Mover { get; private set; }

        protected SpriteClipPlayer Player { get; private set; }

        protected GrayboxEnemyAnimator Animator { get; private set; }

        protected SpriteAnimLibrary Library => Player != null ? Player.Library : GrayboxSpecials.Library;

        /// <summary>Hill health lost if this enemy gets in. Read by <see cref="GrayboxBaseHealth"/>.</summary>
        public float BreachDamage => _breachDamage;

        protected bool Alive => Health != null && !Health.IsDead;

        protected virtual void Awake()
        {
            Health = GetComponent<EnemyHealth>();
            Mover = GetComponent<EnemyMover>();
            Player = GetComponent<SpriteClipPlayer>();
            Animator = GetComponent<GrayboxEnemyAnimator>();
        }

        protected virtual void OnEnable()
        {
            EnemyHealth.Damaged += HandleDamaged;
            EnemyHealth.Defeated += HandleDefeated;
            EnemyMover.ReachedEnd += HandleReachedEnd;
        }

        protected virtual void OnDisable()
        {
            EnemyHealth.Damaged -= HandleDamaged;
            EnemyHealth.Defeated -= HandleDefeated;
            EnemyMover.ReachedEnd -= HandleReachedEnd;
        }

        private void HandleDamaged(EnemyHealth health, DamageInfo info, float applied)
        {
            if (health == Health)
            {
                OnHurt(info, applied);
            }
        }

        private void HandleDefeated(GameObject enemy)
        {
            if (enemy == gameObject)
            {
                OnDied();
            }
        }

        private void HandleReachedEnd(GameObject enemy)
        {
            if (enemy == gameObject)
            {
                OnReachedHill();
            }
        }

        protected virtual void OnHurt(DamageInfo info, float applied)
        {
        }

        protected virtual void OnDied()
        {
        }

        protected virtual void OnReachedHill()
        {
        }

        // ---------- clips ----------

        protected void Play(string clip)
        {
            if (Player != null)
            {
                Player.Play(clip);
            }
        }

        /// <summary>The loop this enemy returns to after any one-shot clip.</summary>
        protected void SetLoop(string clip)
        {
            if (Player != null && Player.Has(clip))
            {
                Player.DefaultClip = clip;
            }
        }

        protected float ClipLength(string clip, float fallback)
        {
            SpriteAnimClip found = Library != null ? Library.Find(Player != null ? Player.Key : null)?.Find(clip) : null;
            return found != null ? found.Length : fallback;
        }

        protected static void Say(string text, DamageNumberKind kind, Vector3 position)
        {
            if (DamageNumberSpawner.Instance != null)
            {
                DamageNumberSpawner.ShowText(text, kind, position);
            }
        }

        // ---------- towers ----------

        private static readonly List<Tower> TowerBuffer = new List<Tower>();

        /// <summary>Every tower standing on the map right now. The list is reused: do not keep it.</summary>
        protected static List<Tower> ActiveTowers()
        {
            Tower.GetActive(TowerBuffer);
            return TowerBuffer;
        }

        protected static Tower NearestTower(Vector2 from, float maxDistance, System.Predicate<Tower> accept = null)
        {
            Tower best = null;
            float bestSqr = maxDistance * maxDistance;
            foreach (Tower tower in ActiveTowers())
            {
                float sqr = ((Vector2)tower.transform.position - from).sqrMagnitude;
                if (sqr <= bestSqr && (accept == null || accept(tower)))
                {
                    bestSqr = sqr;
                    best = tower;
                }
            }

            return best;
        }

        /// <summary>True if the hit came from something on the ground that shakes or blasts: a Kicker or a mine.</summary>
        protected static bool IsGroundShock(DamageInfo info)
        {
            GameObject source = info.Source;
            if (source == null)
            {
                return false;
            }

            if (source.GetComponent<ProximityMine>() != null)
            {
                return true;
            }

            GameObject owner = CombatUtil.OwnerOf(source);
            return owner != null && (owner.GetComponent<KnockbackTower>() != null || owner.GetComponent<MineLayerTower>() != null);
        }

        // ---------- path ----------

        private static EnemyPath s_path;

        private static EnemyPath Route
        {
            get
            {
                if (s_path == null)
                {
                    s_path = FindAnyObjectByType<EnemyPath>();
                }

                return s_path;
            }
        }

        protected static IReadOnlyList<Vector3> Waypoints => Route != null ? Route.Waypoints : null;

        public static float PathLength() => Route != null ? Route.Length : 0f;

        /// <summary>The point a given distance along the route, clamped to its ends.</summary>
        public static Vector3 PointAlongPath(float distance) => Route != null ? Route.PointAt(distance) : Vector3.zero;

        /// <summary>Puts a freshly made enemy on the route at a given distance from its start.</summary>
        public static void PlaceOnPath(GameObject enemy, float distance)
        {
            var mover = enemy.GetComponent<EnemyMover>();
            if (mover == null)
            {
                return;
            }

            mover.SnapToStart();
            if (distance > 0.01f)
            {
                mover.UpdatePositionToMatchDistance(distance);
            }
        }

        /// <summary>
        /// Takes the enemy off the mover entirely, so this component can turn and move it. A mover
        /// that is merely slowed to zero still turns the enemy toward its next waypoint each frame.
        /// Returns the distance to hand back to <see cref="ReturnToPath"/>.
        /// </summary>
        protected float LeavePath()
        {
            float distance = Mover.DistanceTravelled;
            Mover.IgnoresKnockback = true; // a knockback would drag it back to the path from wherever it is
            Mover.enabled = false;
            return distance;
        }

        /// <summary>Puts the enemy back on the route at a distance from its start, walking again.</summary>
        protected void ReturnToPath(float distance)
        {
            Mover.enabled = true; // re-enabling resets it to the start of the route
            Mover.SnapToStart();
            if (distance > 0.01f)
            {
                Mover.UpdatePositionToMatchDistance(distance);
            }
        }

        /// <summary>Faces +x along a direction of travel. Flyers flip instead (see GrayboxFlyer).</summary>
        protected void Face(Vector2 direction)
        {
            if (direction.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            }
        }
    }
}
