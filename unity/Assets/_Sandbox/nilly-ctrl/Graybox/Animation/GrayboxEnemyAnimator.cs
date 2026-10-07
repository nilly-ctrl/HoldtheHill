using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Enemies;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Drives an enemy's Walk / Hurt / Death clips, plus the shield clips on Shielded and
    /// the Heal clip on Healer.
    /// </summary>
    /// <remarks>
    /// EnemyHealth destroys the enemy the moment it dies, so Death plays on a separate
    /// corpse object left where it fell. Shield hits never reach EnemyHealth.Damaged (the
    /// shield swallows them first), so the shield clips come from watching CurrentShield.
    /// An enemy that reaches the end of the trail is removed at once too, so its Attack plays
    /// the same way, with a hit effect on the hill. Spawn plays on enemies that start at the
    /// burrow (not on hatchlings born mid-trail). Attack and Spawn come from the Enemy*Moves
    /// files, which the library builder folds into each enemy's own set.
    /// </remarks>
    [RequireComponent(typeof(SpriteClipPlayer))]
    public class GrayboxEnemyAnimator : MonoBehaviour
    {
        private const float HealRingPixels = 90f;
        private const float PixelsPerUnit = 32f;

        // The trail ends inside the hill, whose art is drawn over the enemies. These step back
        // along the enemy's facing to the hill's rim, where a bite can be seen.
        private const float AttackStandOff = 1.5f;
        private const float HillRim = 1.1f;

        private SpriteClipPlayer _player;
        private SpriteRenderer _renderer;
        private EnemyHealth _health;
        private EnemyShield _shield;
        private EnemyHealer _healer;
        private float _lastShield;

        /// <summary>
        /// Off while the enemy is in a pose the Hurt clip does not match (rolled up, on its back),
        /// so a hit does not flash it back into its walking shape.
        /// </summary>
        public bool PlayHurt { get; set; } = true;

        public static GrayboxEnemyAnimator Attach(GameObject enemy, SpriteAnimLibrary library, string key)
        {
            var renderer = enemy.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = enemy.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = 2;
            }

            renderer.drawMode = SpriteDrawMode.Simple;
            renderer.color = Color.white;
            // Leaving Sliced mode makes Unity rescale the transform to keep the old on-screen size
            // (4x for a 0.8 placeholder square). The art is drawn at its real size, so undo that.
            enemy.transform.localScale = Vector3.one;

            var player = enemy.GetComponent<SpriteClipPlayer>();
            if (player == null) player = enemy.AddComponent<SpriteClipPlayer>();
            player.Configure(library, key, "Walk");
            if (enemy.GetComponent<GrayboxStatusVisuals>() == null)
            {
                enemy.AddComponent<GrayboxStatusVisuals>();
            }

            var animator = enemy.GetComponent<GrayboxEnemyAnimator>();
            return animator != null ? animator : enemy.AddComponent<GrayboxEnemyAnimator>();
        }

        private void Awake()
        {
            _player = GetComponent<SpriteClipPlayer>();
            _renderer = GetComponent<SpriteRenderer>();
            _health = GetComponent<EnemyHealth>();
            _shield = GetComponent<EnemyShield>();
            _healer = GetComponent<EnemyHealer>();
        }

        private void Start()
        {
            var mover = GetComponent<EnemyMover>();
            if (_player.Has("Spawn") && (mover == null || mover.DistanceTravelled < 0.5f))
            {
                _player.Play("Spawn");
            }
        }

        private void OnEnable()
        {
            EnemyHealth.Damaged += OnDamaged;
            EnemyHealth.Defeated += OnDefeated;
            EnemyMover.ReachedEnd += OnReachedEnd;
            if (_healer != null) _healer.Pulsed += OnHealPulse;

            _lastShield = _shield != null ? _shield.MaxShield : 0f;
        }

        private void OnDisable()
        {
            EnemyHealth.Damaged -= OnDamaged;
            EnemyHealth.Defeated -= OnDefeated;
            EnemyMover.ReachedEnd -= OnReachedEnd;
            if (_healer != null) _healer.Pulsed -= OnHealPulse;
        }

        private void Update()
        {
            if (_shield == null)
            {
                return;
            }

            float shield = _shield.CurrentShield;
            if (_lastShield > 0f && shield <= 0f)
            {
                _player.DefaultClip = "WalkBare";
                _player.Play("ShieldBreak");
                GrayboxFeedback.RaiseShieldBroke(transform.position);
            }
            else if (_lastShield <= 0f && shield > 0f)
            {
                _player.DefaultClip = "Walk";   // regenerated
            }
            else if (shield > 0f && shield < _lastShield - 0.01f && _player.CurrentClip != "ShieldBreak")
            {
                _player.Play("ShieldHit");
            }

            _lastShield = shield;
        }

        private void OnDamaged(EnemyHealth health, DamageInfo info, float applied)
        {
            if (health != _health || health.IsDead)
            {
                return;
            }

            GrayboxFeedback.RaiseEnemyHurt(_player.Key, transform.position);
            if (PlayHurt && _player.CurrentClip != "ShieldBreak")
            {
                _player.Play("Hurt");
            }
        }

        private void OnHealPulse()
        {
            _player.Play("Heal");
            GrayboxFeedback.RaiseHealPulsed(transform.position);
            float scale = _healer.HealRadius * PixelsPerUnit / HealRingPixels;
            SpriteClipPlayer.SpawnOneShot(_player.Library, "FxHealPulse", "Pulse", transform.position, Quaternion.identity, scale, 1);
        }

        /// <summary>Which FxHillHit clip an enemy leaves on the hill.</summary>
        public static string HillHitFor(string enemyKey)
        {
            switch (enemyKey)
            {
                case "EnemyBrute": return "HitHeavy";
                case "EnemyRunner":
                case "EnemySwarm": return "Breach"; // small enough to slip inside
                default: return "Hit";
            }
        }

        private void OnReachedEnd(GameObject enemy)
        {
            if (enemy != gameObject)
            {
                return;
            }

            // Only where reaching the end removes the enemy; one that loops or stops is still here.
            var mover = GetComponent<EnemyMover>();
            if (mover != null && !mover.DespawnsAtEnd)
            {
                return;
            }

            SpriteAnimLibrary library = _player.Library;
            Vector3 facing = transform.right;
            // Both over the hill prop (4).
            SpriteClipPlayer.SpawnOneShot(library, _player.Key, "Attack", transform.position - facing * AttackStandOff,
                transform.rotation, transform.lossyScale.x, 5);
            SpriteClipPlayer.SpawnOneShot(library, "FxHillHit", HillHitFor(_player.Key), transform.position - facing * HillRim,
                Quaternion.identity, 1f, 6);
        }

        private void OnDefeated(GameObject enemy)
        {
            if (enemy != gameObject)
            {
                return;
            }

            GrayboxFeedback.RaiseEnemyDied(_player.Key, transform.position);

            // A corpse that keeps the enemy's facing and size, drawn under the living.
            SpriteClipPlayer.SpawnOneShot(_player.Library, _player.Key, "Death", transform.position, transform.rotation,
                transform.lossyScale.x, _renderer.sortingOrder - 1);
        }
    }
}
