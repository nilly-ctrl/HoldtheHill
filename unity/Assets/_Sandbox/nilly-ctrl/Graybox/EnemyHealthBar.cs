using HoldTheHill.Features.Enemies;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// A small health bar above an enemy, and a thinner shield bar above that when the enemy has
    /// an <see cref="EnemyShield"/>. Drawn with sprites in the world, not with IMGUI.
    /// </summary>
    /// <remarks>
    /// Slow, burn and poison are shown by <see cref="GrayboxStatusVisuals"/> (a tint and an icon),
    /// so the bar carries no status badge of its own.
    /// </remarks>
    [AddComponentMenu("Hold the Hill/Graybox/Enemy Health Bar")]
    public class EnemyHealthBar : MonoBehaviour
    {
        private const int Order = 20; // above the enemy (2) and its status icon, below popups
        private const float Pad = 0.02f;
        private const float ShieldHeight = 0.035f;
        private static readonly Color Back = new Color(0.1f, 0.1f, 0.12f, 0.85f);
        private static readonly Color ShieldColour = new Color(0.3f, 0.75f, 1f, 0.9f);

        [SerializeField] private Vector3 _offset = new Vector3(0f, 0.55f, 0f);
        [Tooltip("Bar size in world units.")]
        [SerializeField] private Vector2 _size = new Vector2(0.5f, 0.06f);
        [SerializeField] private bool _showOnlyWhenDamaged = false;

        private EnemyHealth _health;
        private EnemyShield _shield;
        private GameObject _root;
        private SpriteRenderer _fill;
        private SpriteRenderer _shieldBack;
        private SpriteRenderer _shieldFill;
        private float _fractionShown = -1f;
        private float _shieldShown = -1f;

        private static Sprite s_pixel;

        public static bool ShowHealthBars { get; set; } = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ShowHealthBars = true;
            s_pixel = null;
        }

        // One white unit square with its pivot on the left edge, so scale.x is a bar's length.
        private static Sprite Pixel
        {
            get
            {
                if (s_pixel == null)
                {
                    s_pixel = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 4f, 4f), new Vector2(0f, 0.5f), 4f);
                }

                return s_pixel;
            }
        }

        private void Awake()
        {
            _health = GetComponent<EnemyHealth>();
            _shield = GetComponent<EnemyShield>();
            Build();
        }

        private void Build()
        {
            _root = new GameObject("Health Bar");
            _root.transform.SetParent(transform, false);
            _root.transform.localPosition = _offset;

            float left = -_size.x * 0.5f;
            Bar("Back", left, 0f, _size, Back, Order);
            _fill = Bar("Fill", left + Pad, 0f, new Vector2(_size.x - Pad * 2f, _size.y - Pad * 2f), Color.green, Order + 1);

            if (_shield != null)
            {
                float y = _size.y * 0.5f + ShieldHeight;
                _shieldBack = Bar("Shield Back", left, y, new Vector2(_size.x, ShieldHeight), Back, Order);
                _shieldFill = Bar("Shield Fill", left + Pad, y, new Vector2(_size.x - Pad * 2f, ShieldHeight - Pad), ShieldColour, Order + 1);
            }
        }

        private SpriteRenderer Bar(string name, float left, float y, Vector2 size, Color colour, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root.transform, false);
            go.transform.localPosition = new Vector3(left, y, 0f);
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = Pixel;
            renderer.color = colour;
            renderer.sortingOrder = order;
            return renderer;
        }

        private void Update()
        {
            bool show = ShowHealthBars && GrayboxGameFlow.GameplayActive && _health != null && !_health.IsDead
                        && (!_showOnlyWhenDamaged || _health.HealthFraction < 0.999f);
            if (_root.activeSelf != show)
            {
                _root.SetActive(show);
            }

            if (!show)
            {
                return;
            }

            float fraction = _health.HealthFraction;
            if (fraction != _fractionShown)
            {
                _fractionShown = fraction;
                Vector3 scale = _fill.transform.localScale;
                scale.x = (_size.x - Pad * 2f) * Mathf.Clamp01(fraction);
                _fill.transform.localScale = scale;
                _fill.color = HealthColour(fraction);
            }

            if (_shieldFill != null)
            {
                float shield = _shield.ShieldFraction;
                if (shield != _shieldShown)
                {
                    _shieldShown = shield;
                    bool has = shield > 0f;
                    _shieldBack.enabled = has;
                    _shieldFill.enabled = has;
                    Vector3 scale = _shieldFill.transform.localScale;
                    scale.x = (_size.x - Pad * 2f) * Mathf.Clamp01(shield);
                    _shieldFill.transform.localScale = scale;
                }
            }
        }

        private static Color HealthColour(float fraction)
        {
            if (fraction > 0.5f)
            {
                return Color.Lerp(new Color(0.9f, 0.8f, 0.2f), new Color(0.2f, 0.85f, 0.3f), (fraction - 0.5f) * 2f);
            }

            return Color.Lerp(new Color(0.9f, 0.2f, 0.2f), new Color(0.9f, 0.8f, 0.2f), fraction * 2f);
        }
    }
}
