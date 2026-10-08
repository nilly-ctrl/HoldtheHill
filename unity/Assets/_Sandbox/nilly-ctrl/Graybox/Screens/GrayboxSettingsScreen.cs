using System.Collections.Generic;
using HoldTheHill.Sandbox.UiKit;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The settings screen: audio, display, gameplay and accessibility tabs. Opens over the title
    /// or pause menu, applies every change at once and writes the save file when it closes.
    /// </summary>
    [RequireComponent(typeof(UiScreen))]
    [AddComponentMenu("Hold the Hill/Graybox/Graybox Settings Screen")]
    public class GrayboxSettingsScreen : MonoBehaviour
    {
        private static readonly string[] UiSizes = { "Small", "Normal", "Large" };

        [Header("Audio")]
        [SerializeField] private UiSliderField _master;
        [SerializeField] private UiSliderField _music;
        [SerializeField] private UiSliderField _sfx;
        [SerializeField] private UiToggleField _mute;

        [Header("Display")]
        [SerializeField] private UiToggleField _fullscreen;
        [SerializeField] private UiStepper _resolution;
        [SerializeField] private UiToggleField _vsync;

        [Header("Gameplay")]
        [SerializeField] private UiToggleField _autoStart;
        [SerializeField] private UiToggleField _damageNumbers;
        [SerializeField] private UiToggleField _healthBars;
        [SerializeField] private UiToggleField _rangeRings;

        [Header("Accessibility")]
        [SerializeField] private UiStepper _uiSize;
        [SerializeField] private UiToggleField _pauseOnFocusLoss;

        [SerializeField] private UiButton _defaults;
        [SerializeField] private UiButton _done;

        private readonly List<Vector2Int> _sizes = new List<Vector2Int>();
        private UiScreen _screen;
        private bool _hooked;

        public UiScreen Screen => _screen != null ? _screen : _screen = GetComponent<UiScreen>();
        public UiSliderField Master => _master;
        public UiToggleField Mute => _mute;
        public UiToggleField AutoStart => _autoStart;
        public UiToggleField HealthBars => _healthBars;
        public UiStepper UiSize => _uiSize;
        public UiButton DefaultsButton => _defaults;
        public UiButton DoneButton => _done;

        private static SettingsSave Saved => GrayboxSave.Data.settings;

        private void OnEnable()
        {
            Hook();
            Show();
        }

        // Closing by Done, by back, or because the game state changed all end here.
        private void OnDisable() => GrayboxSettings.Store();

        /// <summary>Puts the saved settings on the controls.</summary>
        public void Show()
        {
            SettingsSave s = Saved;
            _master.Value = s.masterVolume;
            _music.Value = s.musicVolume;
            _sfx.Value = s.sfxVolume;
            _mute.IsOn = s.muted;

            _fullscreen.IsOn = s.fullscreen;
            _vsync.IsOn = s.vsync;
            ShowResolutions(s);

            _autoStart.IsOn = s.autoStartWaves;
            _damageNumbers.IsOn = s.showDamageNumbers;
            _healthBars.IsOn = s.showHealthBars;
            _rangeRings.IsOn = s.showRangeRings;

            _uiSize.SetOptions(UiSizes, Mathf.Clamp(s.uiScaleOffset + 1, 0, UiSizes.Length - 1));
            _pauseOnFocusLoss.IsOn = s.pauseOnFocusLoss;
        }

        private void ShowResolutions(SettingsSave s)
        {
            _sizes.Clear();
            _sizes.AddRange(GrayboxSettings.Resolutions());

            var labels = new List<string> { "Native" };
            int index = 0;
            for (int i = 0; i < _sizes.Count; i++)
            {
                labels.Add($"{_sizes[i].x}x{_sizes[i].y}");
                if (_sizes[i].x == s.resolutionWidth && _sizes[i].y == s.resolutionHeight) index = i + 1;
            }

            _resolution.SetOptions(labels, index);
        }

        private void Hook()
        {
            if (_hooked) return;
            _hooked = true;

            _master.ValueChanged.AddListener(v => Change(s => s.masterVolume = v));
            _music.ValueChanged.AddListener(v => Change(s => s.musicVolume = v));
            _sfx.ValueChanged.AddListener(v => Change(s => s.sfxVolume = v));
            _mute.ValueChanged.AddListener(v => Change(s => s.muted = v));

            _fullscreen.ValueChanged.AddListener(v => Change(s => s.fullscreen = v));
            _vsync.ValueChanged.AddListener(v => Change(s => s.vsync = v));
            _resolution.IndexChanged.AddListener(i => Change(s =>
            {
                // Entry 0 is "Native"; the rest follow _sizes.
                bool native = i <= 0 || i > _sizes.Count;
                s.resolutionWidth = native ? 0 : _sizes[i - 1].x;
                s.resolutionHeight = native ? 0 : _sizes[i - 1].y;
            }));

            _autoStart.ValueChanged.AddListener(v => Change(s => s.autoStartWaves = v));
            _damageNumbers.ValueChanged.AddListener(v => Change(s => s.showDamageNumbers = v));
            _healthBars.ValueChanged.AddListener(v => Change(s => s.showHealthBars = v));
            _rangeRings.ValueChanged.AddListener(v => Change(s => s.showRangeRings = v));

            _uiSize.IndexChanged.AddListener(i => Change(s => s.uiScaleOffset = i - 1));
            _pauseOnFocusLoss.ValueChanged.AddListener(v => Change(s => s.pauseOnFocusLoss = v));

            _defaults.Clicked.AddListener(() =>
            {
                GrayboxSettings.ResetToDefaults();
                Show();
            });
            _done.Clicked.AddListener(Screen.Hide);
        }

        private static void Change(System.Action<SettingsSave> edit)
        {
            edit(Saved);
            GrayboxSave.MarkDirty();
            GrayboxSettings.Apply();
        }
    }
}
