using System;
using System.Collections.Generic;
using HoldTheHill.Sandbox.UiKit;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Puts the saved settings (<see cref="SettingsSave"/>) into effect. The settings screen
    /// changes the save data and calls <see cref="Apply"/> after every change, so a setting is
    /// heard or seen at once; <see cref="Store"/> writes the file when the screen closes.
    /// </summary>
    public static class GrayboxSettings
    {
        /// <summary>Raised after <see cref="Apply"/>.</summary>
        public static event Action Applied;

        /// <summary>0 to 1. Nothing plays music yet; the music player should multiply by this.</summary>
        public static float MusicVolume { get; private set; } = 0.8f;

        /// <summary>0 to 1, for sound effects. Read by <see cref="GrayboxSfx"/> callers when audio returns.</summary>
        public static float SfxVolume { get; private set; } = 0.8f;

        public static bool AutoStartWaves { get; private set; }
        public static bool PauseOnFocusLoss { get; private set; } = true;

        private static SettingsSave Saved => GrayboxSave.Data.settings;

        public static void Apply()
        {
            SettingsSave s = Saved;

            AudioListener.volume = s.muted ? 0f : Mathf.Clamp01(s.masterVolume);
            MusicVolume = Mathf.Clamp01(s.musicVolume);
            SfxVolume = Mathf.Clamp01(s.sfxVolume);

            AutoStartWaves = s.autoStartWaves;
            PauseOnFocusLoss = s.pauseOnFocusLoss;
            DamageNumberSpawner.Enabled = s.showDamageNumbers;
            EnemyHealthBar.ShowHealthBars = s.showHealthBars;
            TowerTargetVisualizer.ShowRangeRings = s.showRangeRings;
            TowerTargetVisualizer.ShowTargetLines = s.showRangeRings;
            TowerTargetVisualizer.ShowTowerLabels = s.showRangeRings;

            foreach (UiPixelCanvasScaler scaler in UnityEngine.Object.FindObjectsByType<UiPixelCanvasScaler>(FindObjectsInactive.Include))
            {
                scaler.ScaleOffset = s.uiScaleOffset;
            }

            // The editor's Game view is not a window this can resize.
            if (!Application.isEditor) ApplyDisplay(s);

            Applied?.Invoke();
        }

        /// <summary>Writes the settings to the save file.</summary>
        public static void Store()
        {
            GrayboxSave.MarkDirty();
            GrayboxSave.Save();
        }

        /// <summary>Back to a new player's settings, applied but not yet written.</summary>
        public static void ResetToDefaults()
        {
            GrayboxSave.Data.settings = new SettingsSave();
            GrayboxSave.MarkDirty();
            Apply();
        }

        /// <summary>
        /// Window sizes to offer: what the monitor reports at 1280x720 and up, one entry per size,
        /// smallest first. Index 0 of the settings screen's list is "Native", which is not in here.
        /// </summary>
        public static List<Vector2Int> Resolutions()
        {
            var sizes = new List<Vector2Int>();
            foreach (Resolution resolution in Screen.resolutions)
            {
                var size = new Vector2Int(resolution.width, resolution.height);
                if (size.x >= 1280 && size.y >= 720 && !sizes.Contains(size)) sizes.Add(size);
            }

            sizes.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            return sizes;
        }

        private static void ApplyDisplay(SettingsSave s)
        {
            QualitySettings.vSyncCount = s.vsync ? 1 : 0;

            Resolution native = Screen.currentResolution;
            bool custom = s.resolutionWidth > 0 && s.resolutionHeight > 0;
            int width = custom ? s.resolutionWidth : native.width;
            int height = custom ? s.resolutionHeight : native.height;
            FullScreenMode mode = s.fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;

            // A native-size window would cover the taskbar and title bar; step down to fit.
            if (!s.fullscreen && !custom)
            {
                width = 1280;
                height = 720;
            }

            if (Screen.width != width || Screen.height != height || Screen.fullScreenMode != mode)
            {
                Screen.SetResolution(width, height, mode);
            }
        }
    }
}
