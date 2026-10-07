using System.Collections.Generic;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Enemies;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Leaves a small hit effect wherever a weapon damages an enemy: an acid splash, a spark,
    /// a mortar blast, a lightning flash and so on.
    /// </summary>
    /// <remarks>
    /// Works from <see cref="EnemyHealth.Damaged"/> alone. Every hit names its source object and
    /// hit point, and a projectile's source is the projectile itself, so the effect is chosen
    /// from what the source is. Splash weapons damage several enemies in one frame from one
    /// point; those are limited to one effect per source per frame.
    /// </remarks>
    [AddComponentMenu("Hold the Hill/Graybox/Graybox Impact Fx")]
    public class GrayboxImpactFx : MonoBehaviour
    {
        private const int SortingOrder = 4;

        // Projectile sprite key -> (effect file, clip, one per frame?)
        private static readonly Dictionary<string, (string Key, string Clip, bool Once)> ByProjectile =
            new Dictionary<string, (string, string, bool)>
            {
                { "ProjLinear", ("FxImpact", "Acid", false) },
                { "ProjHoming", ("FxImpact", "Pop", false) },
                { "ProjMortar", ("FxBlast", "Blast", true) },
                { "ProjRicochet", ("FxImpact", "Spark", false) },
                { "ProjFragment", ("FxImpact", "Spark", false) },
            };

        [SerializeField] private SpriteAnimLibrary _library;
        [Tooltip("Minimum seconds between steam wisps from one beam.")]
        [SerializeField] private float _beamInterval = 0.12f;

        private readonly Dictionary<int, int> _lastFrame = new Dictionary<int, int>();
        private readonly Dictionary<int, float> _lastTime = new Dictionary<int, float>();

        /// <summary>Effects spawned so far. For tests and smoke runs.</summary>
        public int Spawned { get; private set; }

        public void Configure(SpriteAnimLibrary library) => _library = library;

        private void OnEnable() => EnemyHealth.Damaged += OnDamaged;

        private void OnDisable() => EnemyHealth.Damaged -= OnDamaged;

        private void OnDamaged(EnemyHealth enemy, DamageInfo info, float applied)
        {
            GameObject source = info.Source;
            if (source == null || _library == null || source == enemy.gameObject)
            {
                return; // damage-over-time ticks come from the enemy itself: no impact
            }

            Vector3 at = info.HitPoint;
            int id = source.GetHashCode(); // stable per object for its lifetime

            var player = source.GetComponent<SpriteClipPlayer>();
            if (player != null && ByProjectile.TryGetValue(player.Key, out var fx))
            {
                if (fx.Once && !FirstThisFrame(id))
                {
                    return;
                }

                Spawn(fx.Key, fx.Clip, at);
            }
            else if (source.GetComponent<ChainLightning>() != null)
            {
                Spawn("FxImpact", "Zap", enemy.transform.position);
            }
            else if (source.GetComponent<ContinuousBeam>() != null)
            {
                if (!_lastTime.TryGetValue(id, out float last) || Time.time - last >= _beamInterval)
                {
                    _lastTime[id] = Time.time;
                    Spawn("FxImpact", "Sizzle", enemy.transform.position);
                }
            }
            else if (source.GetComponent<OrbitingDamageField>() != null)
            {
                Spawn("FxImpact", "Scratch", enemy.transform.position);
                // The orbiting ant's nip, where the art has one. Not counted: Scratch is the hit.
                SpriteClipPlayer.SpawnOneShot(_library, "FxTowerOrbit", "Bite", enemy.transform.position,
                    Quaternion.identity, 1f, SortingOrder + 1);
            }
        }

        private bool FirstThisFrame(int id)
        {
            if (_lastFrame.TryGetValue(id, out int frame) && frame == Time.frameCount)
            {
                return false;
            }

            _lastFrame[id] = Time.frameCount;
            return true;
        }

        private void Spawn(string key, string clip, Vector3 at)
        {
            if (SpriteClipPlayer.SpawnOneShot(_library, key, clip, at, Quaternion.identity, 1f, SortingOrder) != null)
            {
                Spawned++;
            }
        }
    }
}
