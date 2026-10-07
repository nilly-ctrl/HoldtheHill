using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The graybox's pixel-art look for IMGUI: the Hold the Hill pixel font, a wooden panel
    /// behind every box and wooden buttons, instead of Unity's default grey skin.
    /// </summary>
    /// <remarks>
    /// IMGUI resets to the default skin for each script's OnGUI, so every HUD script calls
    /// <see cref="Apply"/> first thing in its OnGUI. With no GrayboxUi in the scene the call does
    /// nothing and the HUD draws exactly as before. The font is pixel-exact at sizes 10, 20, 30…
    /// (one font pixel per 1/10 em), so HUD styles should stick to those sizes.
    /// </remarks>
    [AddComponentMenu("Hold the Hill/Graybox/Graybox UI Skin")]
    public class GrayboxUi : MonoBehaviour
    {
        public const int BodySize = 10;
        public const int TitleSize = 20;

        private static readonly Color Cream = new Color32(0xff, 0xf1, 0xd6, 0xff);
        private static readonly Color Dim = new Color32(0xc4, 0xbf, 0xb2, 0xff);

        [SerializeField] private Font _font;
        [SerializeField] private Texture2D _panel;
        [SerializeField] private Texture2D _button;
        [SerializeField] private Texture2D _buttonHover;
        [SerializeField] private Texture2D _buttonPressed;

        private static GrayboxUi s_instance;
        private GUISkin _skin;

        /// <summary>Switches the current OnGUI call to the pixel skin. Only valid inside OnGUI.</summary>
        public static void Apply()
        {
            if (s_instance != null)
            {
                s_instance.ApplySkin();
            }
        }

        private void Awake()
        {
            s_instance = this;
        }

        private void OnDestroy()
        {
            if (s_instance == this)
            {
                s_instance = null;
            }

            if (_skin != null)
            {
                Destroy(_skin);
            }
        }

        private void ApplySkin()
        {
            if (_skin == null)
            {
                _skin = Build();
            }

            GUI.skin = _skin;
        }

        private GUISkin Build()
        {
            GUISkin skin = Instantiate(GUI.skin);
            skin.name = "GrayboxPixelSkin";
            skin.font = _font;

            foreach (GUIStyle style in new[] { skin.label, skin.button, skin.box, skin.window, skin.toggle, skin.textField })
            {
                style.font = _font;
                style.fontSize = BodySize;
                style.normal.textColor = Cream;
            }

            skin.box.normal.background = _panel;
            skin.box.border = new RectOffset(6, 6, 6, 6);
            skin.box.padding = new RectOffset(8, 8, 8, 8);

            GUIStyle button = skin.button;
            button.border = new RectOffset(5, 5, 5, 5);
            button.padding = new RectOffset(6, 6, 4, 5);
            button.margin = new RectOffset(2, 2, 2, 2);
            button.alignment = TextAnchor.MiddleCenter;
            button.normal.background = button.focused.background = _button;
            button.hover.background = _buttonHover;
            button.active.background = _buttonPressed;
            button.onNormal.background = button.onHover.background = button.onActive.background = _buttonPressed;
            button.normal.textColor = button.focused.textColor = Cream;
            button.hover.textColor = Color.white;
            button.active.textColor = Dim;
            button.onNormal.textColor = button.onHover.textColor = button.onActive.textColor = Dim;

            return skin;
        }
    }
}
