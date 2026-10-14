#if UNITY_EDITOR || DEVELOPMENT_BUILD
using HoldTheHill.Features.Towers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HoldTheHill.Features.DevTools
{
    /// <summary>
    /// Shared IMGUI styles for the dev tools: dark panels and text that scales with the screen.
    /// Call <see cref="Ensure"/> at the top of every OnGUI that draws with them.
    /// </summary>
    internal static class DevGui
    {
        private static Texture2D s_panel;
        private static Texture2D s_field;
        private static int s_size;

        public static GUIStyle Panel { get; private set; }
        public static GUIStyle Label { get; private set; }
        public static GUIStyle Heading { get; private set; }
        public static GUIStyle Button { get; private set; }
        public static GUIStyle Field { get; private set; }
        public static GUIStyle Toggle { get; private set; }

        /// <summary>Font size in points for the current screen height.</summary>
        public static int FontSize => Mathf.Clamp(Mathf.RoundToInt(Screen.height / 55f), 11, 24);

        /// <summary>A comfortable height for one line of text or a button.</summary>
        public static float Line => FontSize * 1.7f;

        public static void Ensure()
        {
            int size = FontSize;
            if (Panel != null && s_size == size && s_panel != null) return;

            s_size = size;
            if (s_panel == null) s_panel = Solid(new Color(0.05f, 0.06f, 0.08f, 0.88f));
            if (s_field == null) s_field = Solid(new Color(0.16f, 0.18f, 0.22f, 1f));

            Panel = new GUIStyle(GUI.skin.box)
            {
                normal = { background = s_panel },
                padding = new RectOffset(8, 8, 6, 6),
            };

            Label = new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                richText = true,
                wordWrap = true,
                normal = { textColor = new Color(0.9f, 0.93f, 0.95f) },
            };

            Heading = new GUIStyle(Label)
            {
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.85f, 0.4f) },
            };

            Button = new GUIStyle(GUI.skin.button) { fontSize = size, richText = true };

            Field = new GUIStyle(GUI.skin.textField)
            {
                fontSize = size,
                normal = { background = s_field, textColor = Color.white },
                focused = { background = s_field, textColor = Color.white },
            };

            Toggle = new GUIStyle(GUI.skin.toggle) { fontSize = size, normal = { textColor = Color.white } };
        }

        // ---------- keeping clicks on a panel from reaching the field ----------

        private static readonly List<Rect> s_rects = new List<Rect>();
        private static TowerPlacer s_placer;
        private static bool s_disabledPlacer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_panel = null;
            s_field = null;
            s_size = 0; // forces Ensure to rebuild the styles
            Panel = Label = Heading = Button = Field = Toggle = null;
            s_rects.Clear();
            s_placer = null;
            s_disabledPlacer = false;
        }

        /// <summary>Call from OnGUI with the screen rectangle (GUI space) a panel covers this frame.</summary>
        public static void Occupy(Rect rect)
        {
            if (Event.current != null && Event.current.type == EventType.Repaint) s_rects.Add(rect);
        }

        /// <summary>
        /// Switches the tower placer off while the mouse is over a dev panel, so a click on the
        /// panel does not also open the build menu underneath. Call once per frame from Update.
        /// </summary>
        public static void UpdatePointerBlock()
        {
            bool over = false;
            Mouse mouse = Mouse.current;
            if (mouse != null && s_rects.Count > 0)
            {
                Vector2 p = mouse.position.ReadValue();
                p.y = Screen.height - p.y;
                for (int i = 0; i < s_rects.Count && !over; i++) over = s_rects[i].Contains(p);
            }

            s_rects.Clear();

            if (over)
            {
                if (s_placer == null) s_placer = Object.FindAnyObjectByType<TowerPlacer>();
                if (s_placer != null && s_placer.enabled)
                {
                    s_placer.enabled = false;
                    s_disabledPlacer = true;
                }
            }
            else
            {
                ReleasePointerBlock();
            }
        }

        public static void ReleasePointerBlock()
        {
            if (s_disabledPlacer && s_placer != null) s_placer.enabled = true;
            s_disabledPlacer = false;
        }

        private static Texture2D Solid(Color color)
        {
            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}
#endif
