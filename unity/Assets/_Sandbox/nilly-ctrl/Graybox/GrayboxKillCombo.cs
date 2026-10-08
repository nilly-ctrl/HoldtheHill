using HoldTheHill.Features.Enemies;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// A combo counter for kills that come quickly one after another. From the third kill of a
    /// chain it shows "x3", "x4"... in the Combo style of the scene's <see cref="PixelFontTheme"/>,
    /// punching on each kill and fading when the chain runs out. Cosmetic only: it changes no numbers.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Graybox/Graybox Kill Combo")]
    public class GrayboxKillCombo : MonoBehaviour
    {
        [Tooltip("Seconds a chain survives without another kill.")]
        [SerializeField, Min(0.1f)] private float _window = 2.5f;
        [Tooltip("Kills in a chain before the counter shows.")]
        [SerializeField, Min(2)] private int _showFrom = 3;
        [Tooltip("Whole-number multiplier on font pixels.")]
        [SerializeField, Min(1)] private int _pixelScale = 2;
        [Tooltip("World units in from the right edge of the camera, and up from its centre.")]
        [SerializeField] private Vector2 _offset = new Vector2(1.6f, 0.5f);
        [SerializeField, Min(0f)] private float _punchSeconds = 0.18f;
        [SerializeField, Min(1f)] private float _punchScale = 1.5f;
        [SerializeField] private int _sortingOrder = 560;

        private PixelFontTheme _theme;
        private PixelText _text;
        private int _count;
        private float _sinceKill;
        private float _sincePunch;

        /// <summary>Kills in the chain that is running, or 0 between chains.</summary>
        public int Count => _count;

        private void OnEnable()
        {
            EnemyHealth.Defeated += OnEnemyDefeated;
        }

        private void OnDisable()
        {
            EnemyHealth.Defeated -= OnEnemyDefeated;
        }

        private void OnEnemyDefeated(GameObject enemy)
        {
            _count++;
            _sinceKill = 0f;
            if (_count < _showFrom || !EnsureText())
            {
                return;
            }

            _text.Text = $"x{_count}";
            _text.gameObject.SetActive(true);
            _sincePunch = 0f;
        }

        private void Update()
        {
            if (_count == 0)
            {
                return;
            }

            _sinceKill += Time.deltaTime;
            if (_sinceKill >= _window)
            {
                _count = 0;
                if (_text != null)
                {
                    _text.gameObject.SetActive(false);
                }

                return;
            }

            if (_text == null || !_text.gameObject.activeSelf)
            {
                return;
            }

            Camera cam = Camera.main;
            if (cam != null && cam.orthographic)
            {
                Vector3 centre = cam.transform.position;
                _text.transform.position = new Vector3(
                    centre.x + cam.orthographicSize * cam.aspect - _offset.x, centre.y + _offset.y, 0f);
            }

            // Punch on each kill, then fade over the last third of the window.
            _sincePunch += Time.deltaTime;
            float scale = 1f;
            if (_sincePunch < _punchSeconds)
            {
                float t = _sincePunch / _punchSeconds;
                scale = Mathf.Lerp(_punchScale, 1f, 1f - (1f - t) * (1f - t));
            }

            _text.transform.localScale = Vector3.one * scale;
            float fadeFrom = _window * 0.67f;
            float alpha = _sinceKill > fadeFrom ? 1f - Mathf.InverseLerp(fadeFrom, _window, _sinceKill) : 1f;
            _text.Tint = new Color(1f, 1f, 1f, alpha);
        }

        private bool EnsureText()
        {
            if (_text != null)
            {
                return true;
            }

            if (_theme == null)
            {
                _theme = FindAnyObjectByType<PixelFontTheme>();
            }

            PixelFontStyle style = _theme != null ? _theme.Find("Combo") : null;
            if (style == null)
            {
                return false;
            }

            var go = new GameObject("Kill Combo Text");
            go.transform.SetParent(transform, false);
            _text = go.AddComponent<PixelText>();
            _text.ThemeStyle = "Combo";
            _text.Style = style;
            _text.Alignment = PixelText.Anchor.Right;
            _text.SetRendering(_pixelScale, _sortingOrder);
            return true;
        }
    }
}
