using System.Collections.Generic;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Towers;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Plays the graybox's placeholder sound effects from a <see cref="GrayboxSfxBank"/>.
    /// </summary>
    /// <remarks>
    /// One-shots go through a small pool of AudioSources, each play picking a random variant
    /// with about ±5% pitch so repeats don't sound mechanical (as Audio/Sfx/README.md asks).
    /// Sounds are panned by x position rather than played in 3D, since the camera is top-down.
    /// Loops (beam, orbit, fire puddle) get their own AudioSource on the object that makes them.
    /// </remarks>
    [AddComponentMenu("Hold the Hill/Graybox/Graybox Sfx")]
    [DefaultExecutionOrder(-100)] // subscribe before EnemySpawner auto-starts wave 1 in its Start
    public class GrayboxSfx : MonoBehaviour
    {
        [SerializeField] private GrayboxSfxBank _bank;
        [SerializeField, Range(0f, 1f)] private float _masterVolume = 0.8f;
        [SerializeField, Min(1)] private int _voices = 16;
        [Tooltip("World x at which a sound is panned fully to one side.")]
        [SerializeField] private float _panWidth = 9f;

        private readonly List<AudioSource> _pool = new List<AudioSource>();
        private readonly Dictionary<string, float> _lastPlayed = new Dictionary<string, float>();
        private readonly Dictionary<Component, AudioSource> _loops = new Dictionary<Component, AudioSource>();
        private int _next;
        private float _nextScan;

        public static GrayboxSfx Instance { get; private set; }

        public GrayboxSfxBank Bank => _bank;

        /// <summary>Plays a cue by id if a GrayboxSfx is in the scene; does nothing otherwise.</summary>
        public static void PlayCue(string id, Vector3 at, int variant = -1)
        {
            if (Instance != null)
            {
                Instance.Play(id, at, variant);
            }
        }

        public bool Play(string id, Vector3 at, int variant = -1)
        {
            GrayboxSfxCue cue = _bank != null ? _bank.Find(id) : null;
            if (cue == null || cue.Clips.Length == 0)
            {
                return false;
            }

            float now = Time.unscaledTime;
            if (cue.Cooldown > 0f && _lastPlayed.TryGetValue(id, out float last) && now - last < cue.Cooldown)
            {
                return false;
            }

            _lastPlayed[id] = now;

            AudioSource source = _pool[_next];
            _next = (_next + 1) % _pool.Count;

            int index = variant >= 0 ? Mathf.Min(variant, cue.Clips.Length - 1) : Random.Range(0, cue.Clips.Length);
            source.clip = cue.Clips[index];
            source.volume = cue.Volume * _masterVolume;
            source.pitch = variant >= 0 ? 1f : Random.Range(0.95f, 1.05f);
            source.panStereo = Pan(at);
            source.Play();
            return true;
        }

        private void Awake()
        {
            Instance = this;
            for (int i = 0; i < _voices; i++)
            {
                var voice = new GameObject($"Voice {i}").AddComponent<AudioSource>();
                voice.transform.SetParent(transform, false);
                voice.playOnAwake = false;
                voice.spatialBlend = 0f;
                _pool.Add(voice);
            }
        }

        private void OnEnable()
        {
            GrayboxFeedback.TowerAttacked += OnTowerAttacked;
            GrayboxFeedback.TowerUpgraded += OnTowerUpgraded;
            GrayboxFeedback.EnemyHurt += OnEnemyHurt;
            GrayboxFeedback.EnemyDied += OnEnemyDied;
            GrayboxFeedback.ShieldBroke += OnShieldBroke;
            GrayboxFeedback.HealPulsed += OnHealPulsed;
            GrayboxFeedback.MineExploded += OnMineExploded;
            EnemyHealth.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            GrayboxFeedback.TowerAttacked -= OnTowerAttacked;
            GrayboxFeedback.TowerUpgraded -= OnTowerUpgraded;
            GrayboxFeedback.EnemyHurt -= OnEnemyHurt;
            GrayboxFeedback.EnemyDied -= OnEnemyDied;
            GrayboxFeedback.ShieldBroke -= OnShieldBroke;
            GrayboxFeedback.HealPulsed -= OnHealPulsed;
            GrayboxFeedback.MineExploded -= OnMineExploded;
            EnemyHealth.Damaged -= OnDamaged;
        }

        private void Start()
        {
            var spawner = FindAnyObjectByType<EnemySpawner>();
            if (spawner != null)
            {
                spawner.onWaveStarted?.AddListener((wave, total) => Play("WaveStart", Vector3.zero));
                spawner.onWaveCompleted?.AddListener(wave => Play("WaveClear", Vector3.zero));
                spawner.onAllWavesCompleted?.AddListener(() => Play("Victory", Vector3.zero));
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnTowerAttacked(string key, Vector3 at) => Play("Fire_" + key, at);

        // Upgrade variants rise in pitch, so tier 2 plays the first, tier 3 the second.
        private void OnTowerUpgraded(string key, Vector3 at, int level) => Play("Upgrade", at, Mathf.Max(0, level - 2));

        private void OnEnemyHurt(string key, Vector3 at) => Play("Hit", at);

        private void OnEnemyDied(string key, Vector3 at) => Play("Death_" + key, at);

        private void OnShieldBroke(Vector3 at) => Play("ShieldBreak", at);

        private void OnHealPulsed(Vector3 at) => Play("Heal", at);

        private void OnMineExploded(Vector3 at) => Play("MineExplode", at);

        private void OnDamaged(EnemyHealth enemy, DamageInfo info, float applied)
        {
            Vector3 at = enemy.transform.position;
            if (info.Type == DamageType.Fire)
            {
                Play("Burn", at);
            }
            else if (info.Type == DamageType.Poison && info.Source == enemy.gameObject && enemy.SpeedMultiplier >= 1f)
            {
                Play("Poison", at);   // a slowed enemy's tick is Frost Chill, not poison
            }
        }

        // ---------- loops ----------

        private void Update()
        {
            if (Time.unscaledTime >= _nextScan)
            {
                _nextScan = Time.unscaledTime + 0.25f;
                ScanForLoops();
            }

            // The beam only hums while it has a target.
            foreach (KeyValuePair<Component, AudioSource> pair in _loops)
            {
                if (pair.Key is ContinuousBeam beam && pair.Value != null)
                {
                    var tower = beam.GetComponent<Tower>();
                    IDamageable target = tower != null ? tower.CurrentTarget : null;
                    bool firing = target != null && !(target is Object o && o == null) && !target.IsDead;
                    if (firing && !pair.Value.isPlaying) pair.Value.Play();
                    else if (!firing && pair.Value.isPlaying) pair.Value.Stop();
                }
            }
        }

        private void ScanForLoops()
        {
            foreach (ContinuousBeam beam in FindObjectsByType<ContinuousBeam>())
            {
                AddLoop(beam, "BeamLoop", playNow: false);
            }

            foreach (OrbitingDamageField field in FindObjectsByType<OrbitingDamageField>())
            {
                AddLoop(field, "OrbitLoop", playNow: true);
            }

            foreach (GroundHazard hazard in FindObjectsByType<GroundHazard>())
            {
                AddLoop(hazard, "HazardLoop", playNow: true);
            }

            // Forget loops whose owner is gone (their AudioSource went with it).
            var dead = new List<Component>();
            foreach (Component owner in _loops.Keys)
            {
                if (owner == null) dead.Add(owner);
            }

            foreach (Component owner in dead)
            {
                _loops.Remove(owner);
            }
        }

        private void AddLoop(Component owner, string id, bool playNow)
        {
            if (_loops.ContainsKey(owner))
            {
                return;
            }

            GrayboxSfxCue cue = _bank != null ? _bank.Find(id) : null;
            if (cue == null || cue.Clips.Length == 0)
            {
                _loops[owner] = null;
                return;
            }

            var source = owner.gameObject.AddComponent<AudioSource>();
            source.clip = cue.Clips[0];
            source.loop = true;
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.volume = cue.Volume * _masterVolume;
            source.panStereo = Pan(owner.transform.position);
            if (playNow)
            {
                source.Play();
            }

            _loops[owner] = source;
        }

        private float Pan(Vector3 at) => Mathf.Clamp(at.x / _panWidth, -0.8f, 0.8f);
    }
}
