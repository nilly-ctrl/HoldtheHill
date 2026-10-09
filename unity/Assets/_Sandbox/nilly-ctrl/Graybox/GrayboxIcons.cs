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

        private static readonly Dictionary<string, Sprite> SpriteCache = new Dictionary<string, Sprite>();

        /// <summary>
        /// The icon as a sprite, for a uGUI Image. Made once per icon and kept. Null if there is no such icon.
        /// </summary>
        public static Sprite GetSprite(string iconName)
        {
            Texture2D texture = Get(iconName);
            if (texture == null)
            {
                return null;
            }

            if (SpriteCache.TryGetValue(iconName, out Sprite cached) && cached != null && cached.texture == texture)
            {
                return cached;
            }

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = iconName;
            SpriteCache[iconName] = sprite;
            return sprite;
        }

        private static readonly string[] EditorFolders = { "Icons/PNG", "Ui/Hud", "Ui/Markers" };
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
