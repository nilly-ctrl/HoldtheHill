using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Turns an Input System control path into what the menus show for it: the key or button
    /// prompt drawn in the pixel font (<see cref="PixelGlyphs"/>) when there is one, and the
    /// control's plain name otherwise.
    /// </summary>
    public static class GrayboxBindingText
    {
        public const string None = "-";

        private static readonly Dictionary<string, string> s_glyphs = new Dictionary<string, string>
        {
            { "<Keyboard>/escape", PixelGlyphs.KeyEsc },
            { "<Keyboard>/tab", PixelGlyphs.KeyTab },
            { "<Keyboard>/space", PixelGlyphs.KeySpace },
            { "<Keyboard>/enter", PixelGlyphs.KeyEnter },
            { "<Keyboard>/leftShift", PixelGlyphs.KeyShift },
            { "<Keyboard>/rightShift", PixelGlyphs.KeyShift },
            { "<Keyboard>/leftCtrl", PixelGlyphs.KeyCtrl },
            { "<Keyboard>/rightCtrl", PixelGlyphs.KeyCtrl },
            { "<Keyboard>/leftAlt", PixelGlyphs.KeyAlt },
            { "<Keyboard>/rightAlt", PixelGlyphs.KeyAlt },
            { "<Keyboard>/delete", PixelGlyphs.KeyDel },
            { "<Keyboard>/upArrow", PixelGlyphs.KeyUp },
            { "<Keyboard>/downArrow", PixelGlyphs.KeyDown },
            { "<Keyboard>/leftArrow", PixelGlyphs.KeyLeft },
            { "<Keyboard>/rightArrow", PixelGlyphs.KeyRight },
            { "<Gamepad>/buttonSouth", PixelGlyphs.PadA },
            { "<Gamepad>/buttonEast", PixelGlyphs.PadB },
            { "<Gamepad>/buttonWest", PixelGlyphs.PadX },
            { "<Gamepad>/buttonNorth", PixelGlyphs.PadY },
            { "<Gamepad>/leftShoulder", PixelGlyphs.PadLb },
            { "<Gamepad>/rightShoulder", PixelGlyphs.PadRb },
            { "<Gamepad>/leftTrigger", PixelGlyphs.PadLt },
            { "<Gamepad>/rightTrigger", PixelGlyphs.PadRt },
            { "<Gamepad>/leftStickPress", PixelGlyphs.PadLeftStick },
            { "<Gamepad>/rightStickPress", PixelGlyphs.PadRightStick },
            { "<Gamepad>/dpad/up", PixelGlyphs.PadDpadUp },
            { "<Gamepad>/dpad/down", PixelGlyphs.PadDpadDown },
            { "<Gamepad>/dpad/left", PixelGlyphs.PadDpadLeft },
            { "<Gamepad>/dpad/right", PixelGlyphs.PadDpadRight },
            { "<Gamepad>/start", PixelGlyphs.PadStart },
            { "<Gamepad>/select", PixelGlyphs.PadSelect },
        };

        public static string For(string path)
        {
            if (string.IsNullOrEmpty(path)) return None;
            if (s_glyphs.TryGetValue(path, out string glyph)) return glyph;

            const string keyboard = "<Keyboard>/";
            if (path.StartsWith(keyboard))
            {
                string key = path.Substring(keyboard.Length);
                if (key.Length == 1) return Glyph(PixelGlyphs.Key(key[0]), key);

                // Function keys have prompts F1 to F12, in order from KeyF1.
                if (key.Length <= 3 && key[0] == 'f' && int.TryParse(key.Substring(1), out int number) && number >= 1 && number <= 12)
                {
                    return ((char)(PixelGlyphs.KeyF1[0] + number - 1)).ToString();
                }
            }

            return InputControlPath.ToHumanReadableString(path, InputControlPath.HumanReadableStringOptions.OmitDevice);
        }

        private static string Glyph(string glyph, string fallback) => string.IsNullOrEmpty(glyph) ? fallback.ToUpperInvariant() : glyph;
    }
}
