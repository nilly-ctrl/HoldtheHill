using System.Collections.Generic;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Holds the 32x32 pixel icons so HUD scripts can look them up by file name, e.g.
    /// <c>GrayboxIcons.Get("TowerLinearIcon")</c>. GrayboxBuilder fills the list from Icons/PNG.
    /// </summary>
    /// <remarks>
    /// The icons are referenced from the scene, so they are included in a player build. With no
    /// GrayboxIcons in the scene, the Editor falls back to loading straight from the Icons folder,
    /// which keeps older scenes working until they are rebuilt.
    /// </remarks>
    [AddComponentMenu("Hold the Hill/Graybox/Graybox Icons")]
    public class GrayboxIcons : MonoBehaviour
    {
        /// <summary>Icons are drawn at this size (their native size) to stay pixel-sharp.</summary>
        public const float Size = 32f;

        [SerializeField] private Texture2D[] _icons = new Texture2D[0];

        private static GrayboxIcons s_instance;
        private readonly Dictionary<string, Texture2D> _lookup = new Dictionary<string, Texture2D>();

        /// <summary>The icon with this file name (no extension), or null if there is none.</summary>
        public static Texture2D Get(string iconName)
        {
            if (string.IsNullOrEmpty(iconName))
            {
                return null;
            }

            if (s_instance != null)
            {
                s_instance._lookup.TryGetValue(iconName, out Texture2D icon);
                return icon;
            }

#if UNITY_EDITOR
            foreach (string folder in EditorFolders)
            {
                var found = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(
                    $"Assets/_Sandbox/nilly-ctrl/{folder}/{iconName}.png");
                if (found != null)
                {
                    return found;
                }
            }
#endif
            return null;
        }

        private static readonly string[] EditorFolders = { "Icons/PNG", "Ui/Hud", "Ui/Markers" };
        private static readonly Dictionary<string, GUIStyle> SliceStyles = new Dictionary<string, GUIStyle>();

        /// <summary>
        /// Draws a HUD piece stretched to <paramref name="rect"/> with its corners kept square
        /// (9-slice). Call from OnGUI. Returns false if the sprite is missing, so callers can fall back.
        /// </summary>
        public static bool DrawSliced(Rect rect, string spriteName, int left, int right, int top, int bottom)
        {
            Texture2D texture = Get(spriteName);
            if (texture == null)
            {
                return false;
            }

            if (Event.current.type != EventType.Repaint)
            {
                return true;
            }

            if (!SliceStyles.TryGetValue(spriteName, out GUIStyle style) || style.normal.background != texture)
            {
                style = new GUIStyle { border = new RectOffset(left, right, top, bottom) };
                style.normal.background = texture;
                SliceStyles[spriteName] = style;
            }

            style.Draw(rect, false, false, false, false);
            return true;
        }

        /// <summary>9-slice with the same border on every side.</summary>
        public static bool DrawSliced(Rect rect, string spriteName, int border)
        {
            return DrawSliced(rect, spriteName, border, border, border, border);
        }

        private void Awake()
        {
            s_instance = this;
            _lookup.Clear();
            foreach (Texture2D icon in _icons)
            {
                if (icon != null)
                {
                    _lookup[icon.name] = icon;
                }
            }
        }

        private void OnDestroy()
        {
            if (s_instance == this)
            {
                s_instance = null;
            }
        }
    }
}
