using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Towers;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Drives a tower's Idle / Attack / Upgrade clips from what the tower is doing.
    /// </summary>
    /// <remarks>
    /// Projectile, chain, beam and orbit towers attack whenever <see cref="Tower.ShotsFired"/>
    /// goes up. Frost and Knockback towers run their own pulse timers, so they attack on their
    /// Pulsed events and also drop a range-sized ring effect; the Mine Layer attacks on MineLaid.
    /// Upgrade comes from the <see cref="Tower.Upgraded"/> event.
    /// </remarks>
    [RequireComponent(typeof(SpriteClipPlayer))]
    public class GrayboxTowerAnimator : MonoBehaviour
    {
        // Outer ring radius, in pixels, of the last frame of each ring effect (see build_anim_sheets.py).
        private const float FrostRingPixels = 112f;
        private const float ShockwaveRingPixels = 96f;
        private const float PixelsPerUnit = 32f;

        private SpriteClipPlayer _player;
        private Tower _tower;
        private FrostAuraTower _frost;
        private KnockbackTower _knockback;
        private MineLayerTower _mineLayer;
        private int _lastShots;
        private SpriteClipPlayer _tier;

        /// <summary>Raised on every attack: each shot, frost pulse, stomp and mine laid.</summary>
        public event System.Action Fired;

        /// <summary>The tier overlay clip showing now ("Tier2", "Tier3"), or null at tier 1.</summary>
        public string TierClip => _tier != null ? _tier.CurrentClip : null;

        /// <summary>
        /// Swaps a tower's placeholder square for the animated sprite. Works on scene objects
        /// in the editor and on towers placed at runtime.
        /// </summary>
        public static GrayboxTowerAnimator Attach(GameObject tower, SpriteAnimLibrary library, string key)
        {
            if (library == null || library.Find(key) == null)
            {
                Debug.LogWarning($"[Graybox] No animation set '{key}' in the library; {tower.name} keeps its placeholder.");
                return null;
            }

            var renderer = tower.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = tower.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = 1;
            }

            renderer.drawMode = SpriteDrawMode.Simple;
            renderer.color = Color.white;
            // Leaving Sliced mode makes Unity rescale the transform to keep the old on-screen size
            // (4x for a 0.8 placeholder square). The art is drawn at its real size, so undo that.
            tower.transform.localScale = Vector3.one;

            var player = tower.GetComponent<SpriteClipPlayer>();
            if (player == null) player = tower.AddComponent<SpriteClipPlayer>();
            player.Configure(library, key, "Idle");
            var animator = tower.GetComponent<GrayboxTowerAnimator>();
            return animator != null ? animator : tower.AddComponent<GrayboxTowerAnimator>();
        }

        private void Awake()
        {
            _player = GetComponent<SpriteClipPlayer>();
            _tower = GetComponent<Tower>();
        }

        // Start rather than Awake: GrayboxTowerPlacer adds the weapon component after this one.
        private void Start()
        {
            _frost = GetComponent<FrostAuraTower>();
            _knockback = GetComponent<KnockbackTower>();
            _mineLayer = GetComponent<MineLayerTower>();
            _lastShots = _tower != null ? _tower.ShotsFired : 0;

            if (GetComponent<OrbitingDamageField>() != null && GetComponent<GrayboxOrbiterVisuals>() == null)
            {
                gameObject.AddComponent<GrayboxOrbiterVisuals>();
            }

            // Muzzle puffs, wind-ups and shot trails, for towers that have an FxTower file.
            var extras = GetComponent<GrayboxTowerExtras>();
            if (extras == null) extras = gameObject.AddComponent<GrayboxTowerExtras>();
            extras.Setup();

            // A tower copied by the run checkpoint arrives already upgraded: pick up its overlay
            // (or make one) instead of waiting for the next upgrade.
            Transform tier = transform.Find("Tier");
            if (tier != null) _tier = tier.GetComponent<SpriteClipPlayer>();
            if (_tower != null && _tower.Level >= 2) ShowTier(_tower.Level);

            if (_tower != null) _tower.Upgraded += OnTowerUpgraded;
            if (_frost != null) _frost.Pulsed += OnFrostPulse;
            if (_knockback != null) _knockback.Pulsed += OnShockwave;
            if (_mineLayer != null) _mineLayer.MineLaid += OnMineLaid;
        }

        private void OnDestroy()
        {
            if (_tower != null) _tower.Upgraded -= OnTowerUpgraded;
            if (_frost != null) _frost.Pulsed -= OnFrostPulse;
            if (_knockback != null) _knockback.Pulsed -= OnShockwave;
            if (_mineLayer != null) _mineLayer.MineLaid -= OnMineLaid;
        }

        private void Update()
        {
            if (_tower == null || _tower.ShotsFired == _lastShots)
            {
                return;
            }

            _lastShots = _tower.ShotsFired;

            // Pulse towers also tick ShotsFired on the Tower's own cooldown; their events are the real attack.
            if (_frost == null && _knockback == null && _mineLayer == null)
            {
                OnFired();
            }
        }

        // Sound and other feedback fire every shot; the Attack clip just doesn't cut into Upgrade.
        private void OnFired()
        {
            GrayboxFeedback.RaiseTowerAttacked(_player.Key, transform.position);
            Fired?.Invoke();
            PlayAttack();
        }

        public void PlayAttack()
        {
            if (_player.CurrentClip == "Upgrade")
            {
                return;
            }

            _player.Play("Attack");
        }

        private void OnFrostPulse()
        {
            OnFired();
            SpawnRing("FxFrostPulse", "Pulse", _frost.Radius, FrostRingPixels);
        }

        private void OnShockwave()
        {
            OnFired();
            SpawnRing("FxShockwave", "Blast", _knockback.Radius, ShockwaveRingPixels);
        }

        private void OnMineLaid(ProximityMine mine) => OnFired();

        private void SpawnRing(string key, string clip, float radius, float ringPixels)
        {
            float scale = radius * PixelsPerUnit / ringPixels;
            SpriteClipPlayer.SpawnOneShot(_player.Library, key, clip, transform.position, Quaternion.identity, scale, 3);
        }

        private void OnTowerUpgraded(int level)
        {
            GrayboxFeedback.RaiseTowerUpgraded(_player.Key, transform.position, level);
            _player.Play("Upgrade");
            ShowTier(level);
        }

        /// <summary>
        /// Tier 2 adds a stone ring and banner over the tower, tier 3 a gold ring and red banner.
        /// The overlay is a child, so it grows with the tower when Tower.Upgrade scales it.
        /// </summary>
        private void ShowTier(int level)
        {
            string clip = $"Tier{Mathf.Min(level, 3)}";
            SpriteAnimLibrary library = _player.Library;
            if (level < 2 || library == null || library.Find("FxTier")?.Find(clip) == null)
            {
                return;
            }

            if (_tier == null)
            {
                var go = new GameObject("Tier", typeof(SpriteRenderer));
                go.transform.SetParent(transform, false);
                go.GetComponent<SpriteRenderer>().sortingOrder = GetComponent<SpriteRenderer>().sortingOrder + 1;
                _tier = go.AddComponent<SpriteClipPlayer>();
                _tier.Configure(library, "FxTier", clip);
            }

            _tier.DefaultClip = clip;
            _tier.Play(clip);
        }
    }
}
