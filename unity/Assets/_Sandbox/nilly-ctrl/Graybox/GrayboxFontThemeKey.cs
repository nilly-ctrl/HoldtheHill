using UnityEngine;
using UnityEngine.InputSystem;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Steps the scene's font theme while playing, to compare the themed number, banner and label
    /// sets live: F6 for the next theme, Shift+F6 for the one before. The new theme's name shows
    /// for a moment in its own title style if the scene has a <see cref="GrayboxWaveBanner"/>.
    /// Fonts only; sprites do not change. Needs a <see cref="PixelFontTheme"/> in the scene.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Graybox/Graybox Font Theme Key")]
    public class GrayboxFontThemeKey : MonoBehaviour
    {
        [SerializeField, Min(0.2f)] private float _nameSeconds = 1.4f;

        private PixelFontTheme _theme;
        private GrayboxWaveBanner _banner;

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !GrayboxControls.Pressed(GrayboxControls.FontTheme))
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

            string themeName = _theme.StepTheme(keyboard.shiftKey.isPressed ? -1 : 1);
            if (_banner == null)
            {
                _banner = FindAnyObjectByType<GrayboxWaveBanner>();
            }

            if (_banner != null)
            {
                _banner.Show(themeName, "Title", _nameSeconds);
            }
        }
    }
}
