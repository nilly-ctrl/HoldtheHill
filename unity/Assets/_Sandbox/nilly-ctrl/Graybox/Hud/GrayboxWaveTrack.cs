using System.Collections.Generic;
using HoldTheHill.Features.Enemies;
using UnityEngine;
using UnityEngine.UI;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// A progress line under the hill bar: a flag for each wave (changed once cleared), a skull on
    /// every fifth wave, a gold fill up to the wave being played and a marker beneath it.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Graybox/HUD/Wave Track")]
    public class GrayboxWaveTrack : GrayboxHudPanel
    {
        [Tooltip("The line itself. The markers are spread along its width.")]
        [SerializeField] private RectTransform _track;

        [SerializeField] private RectTransform _fill;
        [SerializeField] private RectTransform _now;

        [Tooltip("An Image made once per wave.")]
        [SerializeField] private Image _markerPrefab;

        [Header("Marker art")]
        [SerializeField] private Sprite _upcoming;
        [SerializeField] private Sprite _cleared;
        [SerializeField] private Sprite _boss;

        [Tooltip("Every Nth wave gets the boss marker.")]
        [SerializeField, Min(2)] private int _bossEvery = 5;

        [Tooltip("Canvas units the track is inset at each end, so the end markers sit inside the frame.")]
        [SerializeField] private float _inset = 2f;

        private readonly List<Image> _markers = new List<Image>();
        private EnemySpawner _spawner;
        private int _builtFor;
        private int _shownCurrent = -1;

        public override bool WantsToShow
        {
            get
            {
                if (_spawner == null)
                {
                    _spawner = FindAnyObjectByType<EnemySpawner>();
                }

                return _spawner != null && _spawner.TotalWaves >= 2;
            }
        }

        public override void Refresh()
        {
            int total = _spawner.TotalWaves;
            if (total != _builtFor)
            {
                Rebuild(total);
            }

            int current = Mathf.Clamp(_spawner.CurrentWaveIndex, 0, total - 1);
            if (current == _shownCurrent)
            {
                return;
            }

            _shownCurrent = current;
            float inner = _track.rect.width - _inset * 2f;
            float done = inner * current / (total - 1);

            // The fill and the "now" marker end where the current wave's flag is.
            _fill.sizeDelta = new Vector2(Mathf.Round(done), _fill.sizeDelta.y);
            _fill.gameObject.SetActive(done >= 2f);
            _now.anchoredPosition = new Vector2(_inset + Mathf.Round(done), _now.anchoredPosition.y);

            for (int i = 0; i < _markers.Count; i++)
            {
                bool boss = IsBoss(i);
                Image marker = _markers[i];
                marker.sprite = boss ? _boss : (i < current ? _cleared : _upcoming);
                marker.SetNativeSize();
                marker.color = boss && i < current ? new Color(1f, 1f, 1f, 0.5f) : Color.white;
            }
        }

        private bool IsBoss(int index) => (index + 1) % _bossEvery == 0;

        private void Rebuild(int total)
        {
            foreach (Image marker in _markers)
            {
                Destroy(marker.gameObject);
            }

            _markers.Clear();
            _builtFor = total;
            _shownCurrent = -1;

            float inner = _track.rect.width - _inset * 2f;
            for (int i = 0; i < total; i++)
            {
                Image marker = Instantiate(_markerPrefab, _track);
                var rect = (RectTransform)marker.transform;
                rect.anchoredPosition = new Vector2(_inset + Mathf.Round(inner * i / (total - 1)), rect.anchoredPosition.y);
                _markers.Add(marker);
            }
        }
    }
}
