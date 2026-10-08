using HoldTheHill.Features.Enemies;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Big pixel-font announcements in the middle of the screen: WAVE 3 when a wave starts,
    /// WAVE CLEAR when its last enemy is gone, and VICTORY or DEFEAT when the run ends. Each one
    /// punches in, holds and fades. The styles come from the scene's <see cref="PixelFontTheme"/>
    /// (Banner, BannerVictory, BannerDefeat), so they follow the chosen art theme; without one in
    /// the scene this does nothing.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Graybox/Graybox Wave Banner")]
    public class GrayboxWaveBanner : MonoBehaviour
    {
        [Tooltip("Whole-number multiplier on font pixels.")]
        [SerializeField, Min(1)] private int _pixelScale = 1;
        [Tooltip("World offset from the centre of the camera.")]
        [SerializeField] private Vector2 _offset = new Vector2(0f, 1.5f);
        [SerializeField, Min(0.1f)] private float _waveSeconds = 1.8f;
        [SerializeField, Min(0.1f)] private float _endSeconds = 4f;
        [SerializeField, Min(0f)] private float _punchSeconds = 0.25f;
        [SerializeField, Min(1f)] private float _punchScale = 1.6f;
        [SerializeField, Range(0.05f, 1f)] private float _fadeShare = 0.25f;
        [SerializeField] private int _sortingOrder = 600;

        private const float ClearCheckSeconds = 0.5f;

        private EnemySpawner _spawner;
        private PixelFontTheme _theme;
        private PixelText _text;
        private float _age;
        private float _duration;
        private bool _showing;
        private bool _waitingForClear;
        private float _nextClearCheck;

        private void OnEnable()
        {
            _spawner = FindAnyObjectByType<EnemySpawner>();
            if (_spawner != null)
            {
                _spawner.onWaveStarted.AddListener(OnWaveStarted);
                _spawner.onWaveCompleted.AddListener(OnWaveCompleted);
            }

            GrayboxBaseHealth.OnGameFinished += OnGameFinished;
        }

        private void OnDisable()
        {
            if (_spawner != null)
            {
                _spawner.onWaveStarted.RemoveListener(OnWaveStarted);
                _spawner.onWaveCompleted.RemoveListener(OnWaveCompleted);
            }

            GrayboxBaseHealth.OnGameFinished -= OnGameFinished;
        }

        /// <summary>Shows a banner in the named theme style (Banner, BannerVictory, BannerDefeat, Heading...).</summary>
        public void Show(string message, string themeStyle, float seconds)
        {
            if (_theme == null)
            {
                _theme = FindAnyObjectByType<PixelFontTheme>();
            }

            PixelFontStyle style = _theme != null ? _theme.Find(themeStyle) : null;
            if (style == null)
            {
                return;
            }

            if (_text == null)
            {
                var go = new GameObject("Wave Banner Text");
                go.transform.SetParent(transform, false);
                _text = go.AddComponent<PixelText>();
                _text.SetRendering(_pixelScale, _sortingOrder);
            }

            _text.ThemeStyle = themeStyle;
            _text.Style = style;
            _text.Text = message;
            _text.gameObject.SetActive(true);
            _age = 0f;
            _duration = seconds;
            _showing = true;
            Apply();
        }

        private void OnWaveStarted(int waveNumber, int totalWaves)
        {
            _waitingForClear = false;
            Show($"WAVE {waveNumber}", "Banner", _waveSeconds);
        }

        // The spawner reports a wave complete when it has finished spawning, so wait for the field to empty.
        private void OnWaveCompleted(int waveNumber)
        {
            _waitingForClear = true;
            _nextClearCheck = Time.time + ClearCheckSeconds;
        }

        private void OnGameFinished(bool victory)
        {
            _waitingForClear = false;
            Show(victory ? "VICTORY!" : "DEFEAT", victory ? "BannerVictory" : "BannerDefeat", _endSeconds);
        }

        private void Update()
        {
            if (_waitingForClear && Time.time >= _nextClearCheck)
            {
                _nextClearCheck = Time.time + ClearCheckSeconds;
                if (FindAnyObjectByType<EnemyHealth>() == null)
                {
                    _waitingForClear = false;
                    Show("WAVE CLEAR", "Banner", _waveSeconds);
                }
            }

            if (!_showing)
            {
                return;
            }

            // Unscaled, so a banner still plays out while the end screen has the game paused.
            _age += Time.unscaledDeltaTime;
            if (_age >= _duration)
            {
                _showing = false;
                _text.gameObject.SetActive(false);
                return;
            }

            Apply();
        }

        private void Apply()
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 centre = cam.transform.position;
                _text.transform.position = new Vector3(centre.x + _offset.x, centre.y + _offset.y, 0f);
            }

            // Punch: start large and ease out to full size.
            float scale = 1f;
            if (_age < _punchSeconds)
            {
                float t = _age / _punchSeconds;
                float ease = 1f - (1f - t) * (1f - t) * (1f - t);
                scale = Mathf.Lerp(_punchScale, 1f, ease);
            }

            _text.transform.localScale = Vector3.one * scale;

            float fadeFrom = _duration * (1f - _fadeShare);
            float alpha = _age > fadeFrom ? 1f - Mathf.InverseLerp(fadeFrom, _duration, _age) : 1f;
            _text.Tint = new Color(1f, 1f, 1f, alpha);
        }
    }
}
