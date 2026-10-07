using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Enemies;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Rides on a projectile: leaves the tower's Trail effect behind it as it flies, and its
    /// Miss effect where it ends if it never hurt anything.
    /// </summary>
    /// <remarks>
    /// <see cref="GrayboxTowerExtras"/> adds this to a shot the first time its tower launches it.
    /// Shots are pooled, so the component stays on and is started again by each launch.
    /// A shot ends when its pool switches it off (or it is destroyed), which is OnDisable here.
    /// </remarks>
    public class GrayboxShotTrail : MonoBehaviour
    {
        private const float Interval = 0.07f;
        private const int SortingOrder = 2; // under the shot itself (3)

        private static bool s_quitting;

        private SpriteAnimLibrary _library;
        private string _key;
        private float _timer;
        private bool _dealtDamage;

        /// <summary>True from a launch until the shot is released.</summary>
        public bool InFlight { get; private set; }

        /// <summary>Trail puffs spawned since play began. For tests.</summary>
        public static int TrailsSpawned { get; private set; }

        /// <summary>Miss effects spawned since play began. For tests.</summary>
        public static int MissesSpawned { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_quitting = false;
            TrailsSpawned = 0;
            MissesSpawned = 0;
            Application.quitting -= OnQuitting;
            Application.quitting += OnQuitting;
        }

        private static void OnQuitting() => s_quitting = true;

        /// <summary>Called on every launch, including a ricochet's hop to its next target.</summary>
        public void Begin(SpriteAnimLibrary library, string key)
        {
            _library = library;
            _key = key;
            _timer = Interval;
            _dealtDamage = false;
            InFlight = true;
        }

        private void OnEnable() => EnemyHealth.Damaged += OnDamaged;

        private void OnDisable()
        {
            EnemyHealth.Damaged -= OnDamaged;

            bool ended = InFlight;
            InFlight = false;

            // Not while the scene is being torn down: spawning then leaves objects behind.
            if (ended && !_dealtDamage && !s_quitting && gameObject.scene.isLoaded
                && Spawn("Miss", Quaternion.identity))
            {
                MissesSpawned++;
            }
        }

        private void OnDamaged(EnemyHealth enemy, DamageInfo info, float applied)
        {
            if (info.Source == gameObject)
            {
                _dealtDamage = true;
            }
        }

        private void Update()
        {
            if (!InFlight)
            {
                return;
            }

            _timer -= Time.deltaTime;
            if (_timer > 0f)
            {
                return;
            }

            _timer = Interval;
            if (Spawn("Trail", transform.rotation))
            {
                TrailsSpawned++;
            }
        }

        private bool Spawn(string clip, Quaternion rotation)
        {
            return SpriteClipPlayer.SpawnOneShot(_library, _key, clip, transform.position, rotation, 1f, SortingOrder) != null;
        }
    }
}
