using HoldTheHill.Features.Enemies;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Food, wave and run time in the pixel font, pinned to the top right of the camera. Each is
    /// a <see cref="PixelText"/> with an icon glyph and fixed-width digits, in the Resource and
    /// Hud styles of the scene's <see cref="PixelFontTheme"/>, so it follows the art theme.
    /// While it is showing, <see cref="GrayboxHud"/> leaves those three readouts out of its panel.
    /// Without a font theme in the scene it shows nothing and the old readouts stay.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Graybox/Graybox Pixel HUD")]
    public class GrayboxPixelHud : MonoBehaviour
    {
        [Tooltip("Whole-number multiplier on font pixels.")]
        [SerializeField, Min(1)] private int _pixelScale = 2;
        [Tooltip("World units in from the top right corner of the camera.")]
        [SerializeField] private Vector2 _margin = new Vector2(0.35f, 0.9f);
        [Tooltip("World units between one readout and the next.")]
        [SerializeField, Min(0f)] private float _lineSpacing = 0.6f;
        [SerializeField] private int _sortingOrder = 550;

        private EnemySpawner _spawner;
        private GrayboxHud _hud;
        private PixelFontTheme _theme;
        private PixelText _food;
        private PixelText _wave;
        private PixelText _time;

        /// <summary>True while a pixel HUD is drawing the food, wave and time readouts.</summary>
        public static bool Showing { get; private set; }

        private void OnDisable()
        {
            Showing = false;
        }

        private void LateUpdate()
        {
            if (!EnsureTexts() || !GrayboxGameFlow.GameplayActive)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);
            if (_spawner == null)
            {
                _spawner = FindAnyObjectByType<EnemySpawner>();
            }

            if (_hud == null)
            {
                _hud = FindAnyObjectByType<GrayboxHud>();
            }

            int gold = GrayboxEconomy.Instance != null ? GrayboxEconomy.Instance.CurrentGold : 0;
            _food.Text = $"{PixelGlyphs.IconFood} {gold:N0}";
            _wave.Text = _spawner != null
                ? $"{PixelGlyphs.IconWave} {_spawner.CurrentWaveNumber}/{_spawner.TotalWaves}"
                : string.Empty;
            int seconds = _hud != null ? Mathf.FloorToInt(_hud.ElapsedSeconds) : 0;
            _time.Text = $"{PixelGlyphs.IconClock} {seconds / 60:00}:{seconds % 60:00}";

            Camera cam = Camera.main;
            if (cam == null || !cam.orthographic)
            {
                return;
            }

            Vector3 centre = cam.transform.position;
            float right = centre.x + cam.orthographicSize * cam.aspect - _margin.x;
            float top = centre.y + cam.orthographicSize - _margin.y;
            _food.transform.position = new Vector3(right, top, 0f);
            _wave.transform.position = new Vector3(right, top - _lineSpacing, 0f);
            _time.transform.position = new Vector3(right, top - _lineSpacing * 2f, 0f);
        }

        // The texts are made once a font theme is there to style them.
        private bool EnsureTexts()
        {
            if (_food != null)
            {
                return true;
            }

            if (_theme == null)
            {
                _theme = FindAnyObjectByType<PixelFontTheme>();
            }

            if (_theme == null || _theme.Find("Hud") == null || _theme.Find("Resource") == null)
            {
                return false;
            }

            _food = Create("Food", "Resource");
            _wave = Create("Wave", "Hud");
            _time = Create("Time", "Hud");
            return true;
        }

        private PixelText Create(string label, string themeStyle)
        {
            var go = new GameObject("Pixel HUD " + label);
            go.transform.SetParent(transform, false);
            var text = go.AddComponent<PixelText>();
            text.ThemeStyle = themeStyle;
            text.Style = _theme.Find(themeStyle);
            text.Alignment = PixelText.Anchor.Right;
            text.FixedWidthDigits = true;
            text.SetRendering(_pixelScale, _sortingOrder);
            return text;
        }

        private void SetVisible(bool visible)
        {
            Showing = visible;
            if (_food == null || _food.gameObject.activeSelf == visible)
            {
                return;
            }

            _food.gameObject.SetActive(visible);
            _wave.gameObject.SetActive(visible);
            _time.gameObject.SetActive(visible);
        }
    }
}
