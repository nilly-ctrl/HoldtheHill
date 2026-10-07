using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Enemies;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Tints an enemy and floats an icon over it while it is slowed, burning or poisoned.
    /// </summary>
    /// <remarks>
    /// EnemyHealth keeps its active effects private, so this reads what it can see:
    /// Slow is <see cref="EnemyHealth.SpeedMultiplier"/> below 1; Burn is Fire damage taken
    /// recently (mines, the mortar's fire puddle); Poison is a damage-over-time tick
    /// (EnemyHealth reports every tick as Poison, sourced from the enemy itself) while the enemy
    /// is not slowed, because in the graybox the only slowing effect, Frost Chill, also ticks.
    /// Burn beats Poison beats Slow when more than one applies.
    ///
    /// The enemy also wears a looping overlay for the status: flames for Burn, bubbles for
    /// Poison, and for Slow the Frost Ant's ice crust, unless the slowing effect's name says
    /// what it is ("Web..." or "Honey..."), in which case it wears that instead.
    /// </remarks>
    // Deliberately no [RequireComponent(typeof(EnemyHealth))]: this can be attached before the
    // enemy's own EnemyHealth is added, and Unity would then auto-add a second, default one.
    // That once left every graybox enemy on the default 30 HP.
    public class GrayboxStatusVisuals : MonoBehaviour
    {
        private const float Linger = 0.6f;
        private const float IconHeight = 0.5f;
        private const string OverlayKey = "FxStatusOverlay";
        private const string CrustKey = "FxTowerFrostAura";
        private const string CrustClip = "Freeze";

        private static readonly Color SlowTint = new Color(0.65f, 0.85f, 1f);
        private static readonly Color BurnTint = new Color(1f, 0.75f, 0.55f);
        private static readonly Color PoisonTint = new Color(0.7f, 1f, 0.6f);
        private static readonly Color ShockTint = new Color(1f, 1f, 0.7f);

        private EnemyHealth _health;
        private SpriteRenderer _renderer;
        private SpriteClipPlayer _icon;
        private SpriteClipPlayer _overlay;
        private string _slowName = "";
        private EnemyShield _shield;
        private float _burnUntil;
        private float _poisonUntil;
        private float _shockUntil;
        private float _regenUntil;
        private string _shown;

        public string ActiveStatus => _shown;

        /// <summary>The overlay clip showing over this enemy ("Burn", "Freeze", "Web"...), or null.</summary>
        public string OverlayClip => _overlay != null && _overlay.gameObject.activeSelf ? _overlay.DefaultClip : null;

        /// <summary>True while the ice crust is showing over this enemy.</summary>
        public bool CrustShown => OverlayClip == CrustClip;

        private void Awake()
        {
            _health = GetComponent<EnemyHealth>();
            _renderer = GetComponent<SpriteRenderer>();
            if (_health == null)
            {
                enabled = false;
            }
        }

        private void OnEnable()
        {
            EnemyHealth.Damaged += OnDamaged;
            EnemyHealth.Healed += OnHealed;
            EnemyHealth.StatusApplied += OnStatusApplied;
            _shield = GetComponent<EnemyShield>();
            _burnUntil = _poisonUntil = _shockUntil = _regenUntil = 0f;
            _shown = null;
        }

        private void OnDisable()
        {
            EnemyHealth.Damaged -= OnDamaged;
            EnemyHealth.Healed -= OnHealed;
            EnemyHealth.StatusApplied -= OnStatusApplied;
        }

        private void OnStatusApplied(EnemyHealth health, StatusEffectData status)
        {
            if (health != _health || status.SlowMultiplier >= 1f)
            {
                return;
            }

            _slowName = status.EffectName ?? "";
            if (_shown == "Slow")
            {
                ShowOverlay(_shown); // already slowed by something else: change what it wears
            }
        }

        private void OnHealed(EnemyHealth health, float amount)
        {
            if (health == _health)
            {
                _regenUntil = Time.time + Linger;
            }
        }

        private void OnDestroy()
        {
            if (_icon != null)
            {
                Destroy(_icon.gameObject);
            }
        }

        private void OnDamaged(EnemyHealth health, DamageInfo info, float applied)
        {
            if (health != _health)
            {
                return;
            }

            if (info.Type == DamageType.Fire)
            {
                _burnUntil = Time.time + Linger;
            }
            else if (info.Type == DamageType.Poison && info.Source == gameObject && _health.SpeedMultiplier >= 1f)
            {
                _poisonUntil = Time.time + Linger;
            }
            else if (info.Type == DamageType.Lightning)
            {
                _shockUntil = Time.time + Linger;
            }
        }

        private void LateUpdate()
        {
            string status = Time.time < _burnUntil ? "Burn"
                : Time.time < _poisonUntil ? "Poison"
                : Time.time < _shockUntil ? "Shock"
                : _health.SpeedMultiplier < 1f ? "Slow"
                : Time.time < _regenUntil ? "Regen"
                : _shield != null && _shield.CurrentShield > 0f ? "Shield"
                : null;

            if (_renderer != null)
            {
                _renderer.color = status switch
                {
                    "Burn" => BurnTint,
                    "Poison" => PoisonTint,
                    "Slow" => SlowTint,
                    "Shock" => ShockTint,
                    _ => Color.white,
                };
            }

            if (status != _shown)
            {
                _shown = status;
                ShowIcon(status);
                ShowOverlay(status);
            }

            if (_icon != null && _icon.gameObject.activeSelf)
            {
                // A separate object, so it stays upright while the enemy turns along the path.
                _icon.transform.position = transform.position + Vector3.up * IconHeight;
            }
        }

        private (string Key, string Clip) OverlayFor(string status)
        {
            switch (status)
            {
                case "Burn": return (OverlayKey, "Burn");
                case "Poison": return (OverlayKey, "Poison");
                case "Slow":
                    if (_slowName.StartsWith("Web")) return (OverlayKey, "Web");
                    if (_slowName.StartsWith("Honey")) return (OverlayKey, "Honey");
                    return (CrustKey, CrustClip);
                default: return (null, null);
            }
        }

        // A child, so it is carried along and removed with the enemy.
        private void ShowOverlay(string status)
        {
            (string key, string clip) = OverlayFor(status);
            var player = GetComponent<SpriteClipPlayer>();
            SpriteAnimLibrary library = player != null ? player.Library : null;
            if (key == null || library == null || library.Find(key)?.Find(clip) == null)
            {
                if (_overlay != null) _overlay.gameObject.SetActive(false);
                return;
            }

            if (_overlay == null)
            {
                var go = new GameObject("Overlay", typeof(SpriteRenderer));
                go.transform.SetParent(transform, false);
                // The overlays are drawn for a one-unit enemy; fit them to this one.
                float width = _renderer != null && _renderer.sprite != null ? _renderer.sprite.bounds.size.x : 1f;
                go.transform.localScale = Vector3.one * Mathf.Clamp(width, 0.6f, 1.25f);
                go.GetComponent<SpriteRenderer>().sortingOrder = (_renderer != null ? _renderer.sortingOrder : 2) + 1;
                _overlay = go.AddComponent<SpriteClipPlayer>();
            }

            _overlay.gameObject.SetActive(true);
            if (_overlay.Key != key || _overlay.DefaultClip != clip)
            {
                _overlay.Configure(library, key, clip);
            }

            _overlay.Play(clip, restart: false);
        }

        private void ShowIcon(string status)
        {
            if (status == null)
            {
                if (_icon != null) _icon.gameObject.SetActive(false);
                return;
            }

            if (_icon == null)
            {
                var player = GetComponent<SpriteClipPlayer>();
                SpriteAnimLibrary library = player != null ? player.Library : null;
                if (library == null || library.Find("FxStatus") == null)
                {
                    return;
                }

                var go = new GameObject($"{name} Status", typeof(SpriteRenderer));
                go.GetComponent<SpriteRenderer>().sortingOrder = 6;
                _icon = go.AddComponent<SpriteClipPlayer>();
                _icon.Configure(library, "FxStatus", status);
            }

            _icon.gameObject.SetActive(true);
            _icon.DefaultClip = status;
            if (!_icon.Play(status, restart: false))
            {
                // The library predates this status (rebuild the graybox to add it): show nothing rather than a stale icon.
                _icon.gameObject.SetActive(false);
            }
        }
    }
}
