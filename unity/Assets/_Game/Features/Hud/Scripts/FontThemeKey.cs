using HoldTheHill.Core.Input;
using HoldTheHill.Features.DamageNumbers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HoldTheHill.Features.Hud
{
    /// <summary>
    /// Steps the scene's font theme while playing, to compare the themed number, banner and label
    /// sets live: F6 for the next theme, F5 for the one before (both rebindable). The new theme's name shows
    /// for a moment in its own title style if the scene has a <see cref="WaveBanner"/>.
    /// Fonts only; sprites do not change. Needs a <see cref="PixelFontTheme"/> in the scene.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Graybox Font Theme Key")]
    public class FontThemeKey : MonoBehaviour
    {
        [SerializeField, Min(0.2f)] private float _nameSeconds = 1.4f;

        private PixelFontTheme _theme;
        private WaveBanner _banner;

        private void Update()
        {
            int step = GameControls.Pressed(GameControls.FontTheme) ? 1
                : GameControls.Pressed(GameControls.FontThemeBack) ? -1 : 0;
            if (step == 0)
            {
                return;
            }

            if (_theme == null)
            {
                _theme = FindAnyObjectByType<PixelFontTheme>();
            }

            if (_theme == null)
            {
                return;
            }

            string themeName = _theme.StepTheme(step);
            if (_banner == null)
            {
                _banner = FindAnyObjectByType<WaveBanner>();
            }

            if (_banner != null)
            {
                _banner.Show(themeName, "Title", _nameSeconds);
            }
        }
    }
}
