using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Towers;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The smaller effects around a tower's attack, from its FxTower file: a muzzle puff toward
    /// the target, a wind-up before the heavier weapons fire, a trail behind each shot, and a
    /// puff of dirt where a shot lands on bare ground.
    /// </summary>
    /// <remarks>
    /// <see cref="GrayboxTowerAnimator"/> adds this to every tower it animates, so nothing in
    /// the scene or prefabs has to change. A tower "TowerLinear" reads its effects from
    /// "FxTowerLinear"; with no such set in the library this switches itself off.
    ///
    /// Which clip plays when:
    ///   Muzzle  on each shot of an aimed tower (projectile, chain, beam), turned toward the
    ///           target; the Frost Ant plays its Muzzle glow on itself.
    ///   Charge  one clip-length before the next shot is due, if a target is still there.
    ///           The beam never stops firing, so it charges once each time it picks up a target.
    ///   Trail, Miss  through <see cref="GrayboxShotTrail"/> on each shot this tower launches.
    ///   Bounce  where a ricochet shot hops to its next target.
    ///   Crack   under the Kicker on each stomp.   Lay  where the Sapper digs a mine in.
    /// </remarks>
    public class GrayboxTowerExtras : MonoBehaviour
    {
        private const float MuzzleOffset = 0.45f; // world units from the tower's centre toward the target
        private const float CrackScale = 2f;
        private const int Above = 3;              // over the tower (1) and the enemies (2)
        private const int OnGround = 0;

        private GrayboxTowerAnimator _animator;
        private Tower _tower;
        private FrostAuraTower _frost;
        private KnockbackTower _knockback;
        private MineLayerTower _mineLayer;
        private SpriteAnimLibrary _library;
        private string _key;
        private bool _aimed;
        private bool _beam;
        private bool _hadTarget;
        private bool _ready;
        private bool _subscribed;
        private float _chargeLength;
        private float _chargeAt = -1f;
        private float _lastFire = -1f;

        /// <summary>The effect set in use, e.g. "FxTowerLinear". Null when the tower has none.</summary>
        public string Key => _subscribed ? _key : null;

        /// <summary>Effects this tower has spawned itself (not its shots' trails). For tests.</summary>
        public int Spawned { get; private set; }

        /// <summary>"TowerLinear" -> "FxTowerLinear". Null for anything not named Tower*.</summary>
        public static string KeyFor(string towerKey)
        {
            return towerKey != null && towerKey.StartsWith("Tower") ? "FxTower" + towerKey.Substring("Tower".Length) : null;
        }

        private void Start() => Setup();

        /// <summary>
        /// Finds the tower's weapon and effect set and starts listening. The animator calls this
        /// as soon as it adds the component, so the tower's very first shot is not missed.
        /// </summary>
        public void Setup()
        {
            if (_ready)
            {
                return;
            }

            _ready = true;
            _animator = GetComponent<GrayboxTowerAnimator>();
            _tower = GetComponent<Tower>();
            _frost = GetComponent<FrostAuraTower>();
            _knockback = GetComponent<KnockbackTower>();
            _mineLayer = GetComponent<MineLayerTower>();

            var player = GetComponent<SpriteClipPlayer>();
            _library = player != null ? player.Library : null;
            _key = player != null ? KeyFor(player.Key) : null;
            SpriteAnimSet set = _library != null && _key != null ? _library.Find(_key) : null;
            if (set == null || _animator == null)
            {
                enabled = false;
                return;
            }

            SpriteAnimClip charge = set.Find("Charge");
            _chargeLength = charge != null ? charge.Length : 0f;
            _beam = GetComponent<ContinuousBeam>() != null;
            _aimed = _frost == null && _knockback == null && _mineLayer == null
                     && GetComponent<OrbitingDamageField>() == null;

            _animator.Fired += OnFired;
            Projectile.Launched += OnLaunched;
            if (_mineLayer != null) _mineLayer.MineLaid += OnMineLaid;
            _subscribed = true;
        }

        private void OnDestroy()
        {
            if (!_subscribed)
            {
                return;
            }

            if (_animator != null) _animator.Fired -= OnFired;
            Projectile.Launched -= OnLaunched;
            if (_mineLayer != null) _mineLayer.MineLaid -= OnMineLaid;
        }

        private void Update()
        {
            if (_beam)
            {
                bool hasTarget = TryAim(out _);
                if (hasTarget && !_hadTarget)
                {
                    Spawn("Charge", transform.position, Quaternion.identity, 1f, Above);
                }

                _hadTarget = hasTarget;
                return;
            }

            if (_chargeAt < 0f || Time.time < _chargeAt)
            {
                return;
            }

            _chargeAt = -1f;
            bool pulses = _frost != null || _knockback != null;
            if (pulses || TryAim(out _))
            {
                Spawn("Charge", transform.position, Quaternion.identity, 1f, Above);
            }
        }

        private void OnFired()
        {
            float now = Time.time;
            float sinceLast = _lastFire >= 0f ? now - _lastFire : 0f;
            _lastFire = now;

            if (_knockback != null)
            {
                Spawn("Crack", transform.position, Quaternion.identity, CrackScale, OnGround);
            }
            else if (_frost != null)
            {
                Spawn("Muzzle", transform.position, Quaternion.identity, 1f, Above);
            }
            else if (_aimed && TryAim(out Vector2 direction))
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                Spawn("Muzzle", transform.position + (Vector3)(direction * MuzzleOffset),
                    Quaternion.Euler(0f, 0f, angle), 1f, Above);
            }

            if (_chargeLength <= 0f || _beam)
            {
                return;
            }

            // Pulse towers keep their own timer, so go by how long the last gap was.
            float interval = _frost != null || _knockback != null ? sinceLast
                : _tower != null ? _tower.EffectiveFireInterval
                : 0f;
            _chargeAt = interval > _chargeLength * 1.5f ? now + interval - _chargeLength : -1f;
        }

        private void OnMineLaid(ProximityMine mine)
        {
            if (mine != null)
            {
                Spawn("Lay", mine.transform.position, Quaternion.identity, 1f, Above);
            }
        }

        private void OnLaunched(Projectile shot)
        {
            if (shot == null || shot.Owner != gameObject)
            {
                return;
            }

            // The mortar's fragments are credited to this tower too; they stay bare.
            var visual = shot.GetComponentInChildren<SpriteClipPlayer>();
            if (visual != null && visual.Key == "ProjFragment")
            {
                return;
            }

            var trail = shot.GetComponent<GrayboxShotTrail>();
            if (trail == null)
            {
                trail = shot.gameObject.AddComponent<GrayboxShotTrail>();
            }
            else if (trail.InFlight)
            {
                // Launched again without being released: a ricochet hopping to its next target.
                Spawn("Bounce", shot.transform.position, Quaternion.identity, 1f, Above + 1);
            }

            trail.Begin(_library, _key);
        }

        private bool TryAim(out Vector2 direction)
        {
            direction = Vector2.right;
            IDamageable target = _tower != null ? _tower.CurrentTarget : null;
            if (!CombatUtil.IsAlive(target))
            {
                return false;
            }

            Vector2 delta = (Vector2)target.Transform.position - (Vector2)transform.position;
            if (delta.sqrMagnitude < 0.0001f)
            {
                return false;
            }

            direction = delta.normalized;
            return true;
        }

        private void Spawn(string clip, Vector3 at, Quaternion rotation, float scale, int order)
        {
            if (SpriteClipPlayer.SpawnOneShot(_library, _key, clip, at, rotation, scale, order) != null)
            {
                Spawned++;
            }
        }
    }
}
