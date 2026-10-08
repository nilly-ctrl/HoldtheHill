using System;
using System.Collections.Generic;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Every baked font style, by art theme and style name, so a theme can be chosen at run time
    /// without a Resources folder. Hold the Hill > Sandbox > Build Pixel Fonts fills it in.
    /// The base (meadow) set has an empty theme name.
    /// </summary>
    public class PixelFontThemeLibrary : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public string Theme;
            public string Style;
            [Tooltip("For a baked pairing such as BannerGothic: the style it is a version of (Banner). Empty otherwise.")]
            public string PairingOf;
            public PixelFontStyle Font;
        }

        [SerializeField] private List<Entry> _entries = new List<Entry>();

        private Dictionary<string, PixelFontStyle> _lookup;
        private Dictionary<string, PixelFontStyle> _pairings;

        /// <summary>The theme names that have a set, in the order they were built. The base set is not listed.</summary>
        public IEnumerable<string> Themes
        {
            get
            {
                var seen = new HashSet<string>();
                foreach (Entry entry in _entries)
                {
                    if (!string.IsNullOrEmpty(entry.Theme) && seen.Add(entry.Theme))
                    {
                        yield return entry.Theme;
                    }
                }
            }
        }

        /// <summary>The names of the styles in the base set, in the order they were built.</summary>
        public IEnumerable<string> BaseStyles
        {
            get
            {
                foreach (Entry entry in _entries)
                {
                    if (string.IsNullOrEmpty(entry.Theme))
                    {
                        yield return entry.Style;
                    }
                }
            }
        }

        /// <summary>
        /// The style in the given theme, or the base set's version when the theme has none.
        /// With preferPairings, a pairing baked for that style in that theme is returned instead.
        /// Null if the style does not exist at all.
        /// </summary>
        public PixelFontStyle Find(string theme, string style, bool preferPairings = false)
        {
            if (_lookup == null)
            {
                _lookup = new Dictionary<string, PixelFontStyle>();
                _pairings = new Dictionary<string, PixelFontStyle>();
                foreach (Entry entry in _entries)
                {
                    if (entry.Font == null)
                    {
                        continue;
                    }

                    _lookup[Key(entry.Theme, entry.Style)] = entry.Font;
                    // The first pairing built for a style in a theme is the one the theme uses.
                    if (!string.IsNullOrEmpty(entry.PairingOf) && !_pairings.ContainsKey(Key(entry.Theme, entry.PairingOf)))
                    {
                        _pairings[Key(entry.Theme, entry.PairingOf)] = entry.Font;
                    }
                }
            }

            // A theme's own pairing (Spooky's Banner in Gothic, say) wins over its body-font version.
            if (preferPairings && _pairings.TryGetValue(Key(theme, style), out PixelFontStyle pairing))
            {
                return pairing;
            }

            if (_lookup.TryGetValue(Key(theme, style), out PixelFontStyle themed))
            {
                return themed;
            }

            return _lookup.TryGetValue(Key(null, style), out PixelFontStyle plain) ? plain : null;
        }

        /// <summary>Replaces the contents. Used by the editor builder.</summary>
        public void Set(List<Entry> entries)
        {
            _entries = entries;
            _lookup = null;
            _pairings = null;
        }

        private void OnEnable()
        {
            _lookup = null;
            _pairings = null;
        }

        private static string Key(string theme, string style)
        {
            return (IsBase(theme) ? string.Empty : theme) + "/" + style;
        }

        private static bool IsBase(string theme)
        {
            return string.IsNullOrEmpty(theme) || theme == "Meadow";
        }
    }
}
