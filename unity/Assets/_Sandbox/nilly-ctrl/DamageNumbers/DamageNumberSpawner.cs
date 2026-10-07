using System.Collections.Generic;
using System.Globalization;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Enemies;
using UnityEngine;
using UnityEngine.Pool;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Floating pixel-art numbers for hits, crits, damage-over-time ticks, heals and bounties.
    /// Put one in the scene (Hold the Hill > Sandbox > Add Damage Numbers To Scene does it and
    /// assigns the styles). It listens to <see cref="EnemyHealth"/> on its own; anything else can call
    /// <see cref="Show(float, DamageNumberKind, Vector3)"/> or <see cref="ShowText"/>.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Sandbox/Damage Number Spawner")]
    public class DamageNumberSpawner : MonoBehaviour
    {
        public static DamageNumberSpawner Instance { get; private set; }

        [Header("Font styles (Fonts/Generated)")]
        [SerializeField] private PixelFontStyle _physical;
        [SerializeField] private PixelFontStyle _magic;
        [SerializeField] private PixelFontStyle _true;
        [SerializeField] private PixelFontStyle _fire;
        [SerializeField] private PixelFontStyle _poison;
        [SerializeField] private PixelFontStyle _lightning;
        [SerializeField] private PixelFontStyle _critical;
        [SerializeField] private PixelFontStyle _heal;
        [SerializeField] private PixelFontStyle _resource;

        [Header("Motion")]
        [SerializeField] private DamageNumberMotion _hitMotion = DamageNumberMotion.Hit;
        [SerializeField] private DamageNumberMotion _critMotion = DamageNumberMotion.Crit;
        [SerializeField] private DamageNumberMotion _tickMotion = DamageNumberMotion.Tick;
        [SerializeField] private DamageNumberMotion _healMotion = DamageNumberMotion.Heal;
        [SerializeField] private DamageNumberMotion _rewardMotion = DamageNumberMotion.Reward;

        [Header("Rendering")]
        [Tooltip("Unlit sprite shader that multiplies texture by vertex colour.")]
        [SerializeField] private Shader _shader;
        [Tooltip("World pixels per unit. Match the game's sprites so a font pixel is the same size as an art pixel.")]
        [SerializeField, Min(1)] private int _pixelsPerUnit = 32;
        [Tooltip("Whole-number multiplier on font pixels. 1 = one font pixel per art pixel.")]
        [SerializeField, Min(1)] private int _pixelScale = 1;
        [Tooltip("Snap popups to the pixel grid so they don't shimmer while moving.")]
        [SerializeField] private bool _snapToPixels = true;
        [SerializeField] private string _sortingLayer = "Default";
        [SerializeField] private int _sortingOrder = 500;

        [Header("Behaviour")]
        [Tooltip("Show numbers automatically for every EnemyHealth hit, heal and death.")]
        [SerializeField] private bool _listenToEnemies = true;
        [Tooltip("Show the bounty as a +N resource popup when an enemy dies.")]
        [SerializeField] private bool _showBounty = true;
        [Tooltip("Put a minus sign in front of damage.")]
        [SerializeField] private bool _showMinusOnDamage = false;
        [Tooltip("World offset from the enemy's pivot to where numbers spawn.")]
        [SerializeField] private Vector2 _spawnOffset = new Vector2(0f, 0.45f);
        [Tooltip("Random jitter on the spawn point, so hits on one enemy don't stack exactly.")]
        [SerializeField, Min(0f)] private float _spawnJitter = 0.12f;
        [Tooltip("Sideways distance between damage kinds, world units. Fire and poison sit furthest out.")]
        [SerializeField, Min(0f)] private float _laneSpacing = 0.45f;
        [Tooltip("How far above the hit numbers the bounty popup starts, world units.")]
        [SerializeField, Min(0f)] private float _bountyLift = 0.7f;
        [Tooltip("Hits of the same kind on one enemy within this many seconds add onto the popup already showing, " +
                 "instead of stacking a new one on top. 0 turns merging off.")]
        [SerializeField, Min(0f)] private float _mergeWindow = 0.3f;
        [Tooltip("Most popups alive at once; the oldest is recycled when a new one needs room.")]
        [SerializeField, Min(1)] private int _maxActive = 80;

        private readonly List<DamageNumber> _active = new List<DamageNumber>();
        private readonly Dictionary<PixelFontStyle, Material> _materials = new Dictionary<PixelFontStyle, Material>();
        private ObjectPool<DamageNumber> _pool;

        // The popup each enemy's recent hits are being added onto.
        private struct Merge
        {
            public DamageNumber Number;
            public int PlayId;
            public DamageNumberKind Kind;
            public float Amount;
            public float Started;
            public float Last;
        }

        // One popup stops absorbing hits after this long, so a constant beam still produces fresh numbers.
        private const float MaxMergeSpan = 1f;
        private readonly Dictionary<EnemyHealth, Merge> _merges = new Dictionary<EnemyHealth, Merge>();

        // ------------------------------------------------------------ static API

        /// <summary>Shows a number, e.g. Show(42, DamageNumberKind.Fire, enemy.position).</summary>
        public static void Show(float amount, DamageNumberKind kind, Vector3 worldPosition)
        {
            if (Instance != null)
            {
                Instance.Spawn(Instance.FormatAmount(amount, kind), kind, worldPosition, Instance.MotionFor(kind, false));
            }
        }

        /// <summary>Shows any text in a style, e.g. ShowText("MISS", DamageNumberKind.Physical, pos).</summary>
        public static void ShowText(string text, DamageNumberKind kind, Vector3 worldPosition)
        {
            if (Instance != null)
            {
                Instance.Spawn(text, kind, worldPosition, Instance.MotionFor(kind, false));
            }
        }

        // ------------------------------------------------------------ lifecycle

        private void Reset()
        {
            _shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (_shader == null)
            {
                _shader = Shader.Find("Sprites/Default");
            }
        }

        private void Awake()
        {
            _pool = new ObjectPool<DamageNumber>(CreateNumber, OnTake, OnRelease, OnDestroyNumber,
                collectionCheck: false, defaultCapacity: 32, maxSize: Mathf.Max(_maxActive, 32));
        }

        private void OnEnable()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("More than one DamageNumberSpawner is active; the newest one wins.", this);
            }

            Instance = this;
            if (_listenToEnemies)
            {
                EnemyHealth.Damaged += OnEnemyDamaged;
                EnemyHealth.Healed += OnEnemyHealed;
                EnemyHealth.Defeated += OnEnemyDefeated;
            }
        }

        private void OnDisable()
        {
            EnemyHealth.Damaged -= OnEnemyDamaged;
            EnemyHealth.Healed -= OnEnemyHealed;
            EnemyHealth.Defeated -= OnEnemyDefeated;
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnDestroy()
        {
            foreach (Material material in _materials.Values)
            {
                Destroy(material);
            }

            _materials.Clear();
            _pool?.Clear();
        }

        // ------------------------------------------------------------ enemy events

        private void OnEnemyDamaged(EnemyHealth enemy, DamageInfo info, float applied)
        {
            if (info.Amount <= 0f)
            {
                return;
            }

            // EnemyHealth routes damage-over-time ticks through TakeDamage with itself as the source.
            bool isTick = info.Source == enemy.gameObject;
            DamageNumberKind kind = info.IsCritical ? DamageNumberKind.Critical : KindFor(info.Type);
            float now = Time.time;

            if (_mergeWindow > 0f && _merges.TryGetValue(enemy, out Merge merge)
                && merge.Number != null && merge.Number.IsPlaying && merge.Number.PlayId == merge.PlayId
                && merge.Kind == kind && now - merge.Last <= _mergeWindow && now - merge.Started <= MaxMergeSpan)
            {
                merge.Amount += info.Amount;
                merge.Last = now;
                merge.Number.Retext(FormatAmount(merge.Amount, kind), StyleFor(kind));
                _merges[enemy] = merge;
                return;
            }

            Vector3 point = SpawnPoint(enemy.transform) + LaneOffset(kind);
            DamageNumber number = Spawn(FormatAmount(info.Amount, kind), kind, point, MotionFor(kind, isTick), small: isTick);
            if (number == null || _mergeWindow <= 0f)
            {
                return;
            }

            if (_merges.Count > 256)
            {
                _merges.Clear(); // dead enemies leave entries behind; this keeps the table small
            }

            _merges[enemy] = new Merge
            {
                Number = number, PlayId = number.PlayId, Kind = kind, Amount = info.Amount, Started = now, Last = now,
            };
        }

        private void OnEnemyHealed(EnemyHealth enemy, float amount)
        {
            Spawn(FormatAmount(amount, DamageNumberKind.Heal), DamageNumberKind.Heal, SpawnPoint(enemy.transform), _healMotion);
        }

        private void OnEnemyDefeated(GameObject enemy)
        {
            if (!_showBounty || enemy == null)
            {
                return;
            }

            var health = enemy.GetComponent<EnemyHealth>();
            if (health != null && health.BountyValue > 0)
            {
                Vector3 point = SpawnPoint(enemy.transform) + new Vector3(0f, _bountyLift, 0f);
                Spawn(FormatAmount(health.BountyValue, DamageNumberKind.Resource), DamageNumberKind.Resource, point, _rewardMotion);
            }
        }

        // ------------------------------------------------------------ spawning

        // Each kind gets its own lane beside the enemy, so a poison tick doesn't land on top of a hit.
        private Vector3 LaneOffset(DamageNumberKind kind)
        {
            float lane;
            switch (kind)
            {
                case DamageNumberKind.Magic: lane = 0.6f; break;
                case DamageNumberKind.True: lane = -0.6f; break;
                case DamageNumberKind.Fire: lane = 1f; break;
                case DamageNumberKind.Poison: lane = -1f; break;
                case DamageNumberKind.Lightning: lane = 0.3f; break;
                default: lane = 0f; break;
            }

            return new Vector3(lane * _laneSpacing, 0f, 0f);
        }

        private DamageNumber Spawn(string text, DamageNumberKind kind, Vector3 position, DamageNumberMotion motion,
            bool small = false)
        {
            PixelFontStyle style = StyleFor(kind);
            if (style == null || style.Atlas == null || string.IsNullOrEmpty(text))
            {
                return null;
            }

            if (_active.Count >= _maxActive)
            {
                Release(_active[0]);
            }

            DamageNumber number = _pool.Get();
            _active.Add(number);
            // Damage-over-time ticks draw one step smaller, so they read as background chatter.
            int scale = small ? Mathf.Max(1, _pixelScale - 1) : _pixelScale;
            float unitsPerPixel = (float)scale / _pixelsPerUnit;
            number.Play(text.ToUpperInvariant(), style, MaterialFor(style), position, motion, unitsPerPixel,
                _snapToPixels, _sortingLayer, _sortingOrder);
            return number;
        }

        internal void Release(DamageNumber number)
        {
            if (!number.IsPlaying)
            {
                return;
            }

            number.Stop();
            _active.Remove(number);
            _pool.Release(number);
        }

        private Vector3 SpawnPoint(Transform target)
        {
            Vector2 jitter = Random.insideUnitCircle * _spawnJitter;
            return target.position + new Vector3(_spawnOffset.x + jitter.x, _spawnOffset.y + jitter.y, 0f);
        }

        // ------------------------------------------------------------ lookups

        private string FormatAmount(float amount, DamageNumberKind kind)
        {
            string number = Abbreviate(amount);
            switch (kind)
            {
                case DamageNumberKind.Heal:
                case DamageNumberKind.Resource:
                    return amount >= 0f ? "+" + number : number;
                case DamageNumberKind.Critical:
                    return (_showMinusOnDamage ? "-" : "") + number + "!";
                default:
                    return (_showMinusOnDamage ? "-" : "") + number;
            }
        }

        // 7 -> "7", 0.4 -> "1", 1234 -> "1.2K", 15600 -> "16K", 2500000 -> "2.5M"
        private static string Abbreviate(float amount)
        {
            float value = Mathf.Abs(amount);
            string sign = amount < 0f ? "-" : "";
            if (value >= 1_000_000f)
            {
                return sign + Short(value / 1_000_000f) + "M";
            }

            if (value >= 1000f)
            {
                return sign + Short(value / 1000f) + "K";
            }

            return sign + Mathf.Max(1, Mathf.RoundToInt(value)).ToString(CultureInfo.InvariantCulture);
        }

        private static string Short(float value)
        {
            return value < 10f
                ? (Mathf.Floor(value * 10f) / 10f).ToString("0.#", CultureInfo.InvariantCulture)
                : Mathf.FloorToInt(value).ToString(CultureInfo.InvariantCulture);
        }

        private static DamageNumberKind KindFor(DamageType type)
        {
            switch (type)
            {
                case DamageType.Magic: return DamageNumberKind.Magic;
                case DamageType.True: return DamageNumberKind.True;
                case DamageType.Fire: return DamageNumberKind.Fire;
                case DamageType.Poison: return DamageNumberKind.Poison;
                case DamageType.Lightning: return DamageNumberKind.Lightning;
                default: return DamageNumberKind.Physical;
            }
        }

        private DamageNumberMotion MotionFor(DamageNumberKind kind, bool isTick)
        {
            switch (kind)
            {
                case DamageNumberKind.Critical: return _critMotion;
                case DamageNumberKind.Heal: return _healMotion;
                case DamageNumberKind.Resource: return _rewardMotion;
                default: return isTick ? _tickMotion : _hitMotion;
            }
        }

        private PixelFontStyle StyleFor(DamageNumberKind kind)
        {
            PixelFontStyle style;
            switch (kind)
            {
                case DamageNumberKind.Magic: style = _magic; break;
                case DamageNumberKind.True: style = _true; break;
                case DamageNumberKind.Fire: style = _fire; break;
                case DamageNumberKind.Poison: style = _poison; break;
                case DamageNumberKind.Lightning: style = _lightning; break;
                case DamageNumberKind.Critical: style = _critical; break;
                case DamageNumberKind.Heal: style = _heal; break;
                case DamageNumberKind.Resource: style = _resource; break;
                default: style = _physical; break;
            }

            // Any style left empty falls back to the physical one rather than showing nothing.
            return style != null ? style : _physical;
        }

        private Material MaterialFor(PixelFontStyle style)
        {
            if (_materials.TryGetValue(style, out Material material))
            {
                return material;
            }

            Shader shader = _shader != null ? _shader : Shader.Find("Sprites/Default");
            material = new Material(shader) { name = "DamageNumbers_" + style.name, mainTexture = style.Atlas };
            _materials[style] = material;
            return material;
        }

        // ------------------------------------------------------------ pool callbacks

        private DamageNumber CreateNumber()
        {
            var go = new GameObject("DamageNumber", typeof(MeshFilter), typeof(MeshRenderer), typeof(DamageNumber));
            go.transform.SetParent(transform, false);
            var number = go.GetComponent<DamageNumber>();
            number.Init(this);
            return number;
        }

        private static void OnTake(DamageNumber number) => number.gameObject.SetActive(true);

        private static void OnRelease(DamageNumber number) => number.gameObject.SetActive(false);

        private static void OnDestroyNumber(DamageNumber number)
        {
            if (number != null)
            {
                Destroy(number.gameObject);
            }
        }
    }
}
