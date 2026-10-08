using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The one setting that picks which art theme's fonts the scene uses. It points the damage
    /// numbers at that theme's set and restyles every <see cref="PixelText"/> that names a theme
    /// style. Leave the theme empty for the base (meadow) set.
    /// </summary>
    [ExecuteAlways]
    [AddComponentMenu("Hold the Hill/Sandbox/Pixel Font Theme")]
    public class PixelFontTheme : MonoBehaviour
    {
        [SerializeField] private PixelFontThemeLibrary _library;
        [Tooltip("Neon, Space, Spooky, Steampunk, Medieval, Samurai, Candy, Jungle, Pirate, Robot, NeonSpace, " +
                 "Vaporwave or Military. Empty for the base set.")]
        [SerializeField] private string _theme;

        public string Theme => _theme;

        private void OnEnable()
        {
            Apply();
        }

        private void OnValidate()
        {
            if (isActiveAndEnabled)
            {
                Apply();
            }
        }

        /// <summary>The named style in the current theme (the base set's if the theme has none), or null.</summary>
        public PixelFontStyle Find(string style)
        {
            return _library != null ? _library.Find(_theme, style) : null;
        }

        /// <summary>Switches the scene's fonts to another theme. Empty or "Meadow" is the base set.</summary>
        public void SetTheme(string theme)
        {
            _theme = theme;
            Apply();
        }

        /// <summary>
        /// Steps to the next theme in the library (or back, with -1), going round through the base
        /// set, and applies it. Returns the new theme's name; the base set is "Meadow".
        /// </summary>
        public string StepTheme(int direction = 1)
        {
            var names = new System.Collections.Generic.List<string> { string.Empty };
            if (_library != null)
            {
                names.AddRange(_library.Themes);
            }

            int current = Mathf.Max(0, names.IndexOf(_theme ?? string.Empty));
            int next = ((current + direction) % names.Count + names.Count) % names.Count;
            SetTheme(names[next]);
            return string.IsNullOrEmpty(_theme) ? "Meadow" : _theme;
        }

        /// <summary>Applies the current theme again, for example after new text has been created.</summary>
        [ContextMenu("Apply Theme")]
        public void Apply()
        {
            if (_library == null)
            {
                return;
            }

            foreach (DamageNumberSpawner spawner in FindObjectsByType<DamageNumberSpawner>())
            {
                spawner.ApplyTheme(_library, _theme);
            }

            foreach (PixelText text in FindObjectsByType<PixelText>())
            {
                if (string.IsNullOrEmpty(text.ThemeStyle))
                {
                    continue;
                }

                PixelFontStyle style = _library.Find(_theme, text.ThemeStyle);
                if (style != null)
                {
                    text.Style = style;
                }
            }
        }
    }
}
