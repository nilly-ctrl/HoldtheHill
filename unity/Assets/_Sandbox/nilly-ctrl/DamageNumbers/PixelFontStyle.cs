using System;
using System.Collections.Generic;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// One baked pixel-font style: the atlas PNG and the glyph JSON written by
    /// Fonts/Source~/build_pixel_fonts.py. Hold the Hill > Sandbox > Build Pixel Fonts creates these.
    /// </summary>
    [CreateAssetMenu(menuName = "Hold the Hill/Sandbox/Pixel Font Style", fileName = "PixelFontStyle")]
    public class PixelFontStyle : ScriptableObject
    {
        [SerializeField] private Texture2D _atlas;
        [SerializeField] private TextAsset _glyphData;
        [Tooltip("Further frames of an animated style, laid out exactly like the atlas. Empty for a still style.")]
        [SerializeField] private Texture2D[] _frames = new Texture2D[0];
        [SerializeField, Min(1f)] private float _framesPerSecond = 8f;

        /// <summary>A glyph's place in the atlas and its size in font pixels.</summary>
        public struct Glyph
        {
            public Rect Uv;
            public int Width;
            public int Height;
            public int Advance;
        }

        // Filled by JsonUtility, so the compiler never sees these assigned.
#pragma warning disable 0649
        [Serializable]
        private class GlyphJson
        {
            public int code;
            public int x;
            public int y;
            public int w;
            public int h;
            public int advance;
        }

        [Serializable]
        private class KerningJson
        {
            public int first;
            public int second;
            public int amount;
        }

        [Serializable]
        private class FontJson
        {
            public int lineHeight;
            public int baseline;
            public int atlasWidth;
            public int atlasHeight;
            public GlyphJson[] glyphs;
            public int digitAdvance;
            public KerningJson[] kerning;
        }
#pragma warning restore 0649

        private Dictionary<char, Glyph> _glyphs;
        private Dictionary<int, int> _kerning;
        private int _lineHeight;
        private int _baseline;
        private int _digitAdvance;

        public Texture2D Atlas => _atlas;

        /// <summary>How many frames the style has: 1 for a still style, more for one that shimmers, flickers or drips.</summary>
        public int FrameCount => 1 + (_frames != null ? _frames.Length : 0);

        public float FramesPerSecond => _framesPerSecond;

        /// <summary>The atlas for a frame; frame 0 is the still atlas. Frames wrap round.</summary>
        public Texture2D Frame(int index)
        {
            int count = FrameCount;
            index = ((index % count) + count) % count;
            return index == 0 || _frames[index - 1] == null ? _atlas : _frames[index - 1];
        }

        /// <summary>Height of every glyph image, in font pixels.</summary>
        public int LineHeight
        {
            get
            {
                EnsureLoaded();
                return _lineHeight;
            }
        }

        /// <summary>Font pixels from the top of a glyph image down to the baseline (the foot of the letters).</summary>
        public int Baseline
        {
            get
            {
                EnsureLoaded();
                return _baseline;
            }
        }

        public bool TryGetGlyph(char c, out Glyph glyph)
        {
            EnsureLoaded();
            return _glyphs.TryGetValue(c, out glyph);
        }

        /// <summary>The widest digit's advance. Use it for every digit to keep a counter from jittering.</summary>
        public int DigitAdvance
        {
            get
            {
                EnsureLoaded();
                return _digitAdvance;
            }
        }

        /// <summary>Font pixels to add between two neighbouring characters (zero or negative).</summary>
        public int GetKerning(char first, char second)
        {
            EnsureLoaded();
            return _kerning.TryGetValue((first << 16) | second, out int amount) ? amount : 0;
        }

        /// <summary>Width of a line of text in font pixels, kerning included.</summary>
        public int Measure(string text)
        {
            int width = 0;
            int lastExtra = 0;
            int previous = -1;
            foreach (char c in text)
            {
                if (!TryGetGlyph(c, out Glyph g))
                {
                    continue;
                }

                if (previous >= 0)
                {
                    width += GetKerning((char)previous, c);
                }

                width += g.Advance;
                lastExtra = g.Width - g.Advance;
                previous = c;
            }

            return width + Mathf.Max(0, lastExtra);
        }

        private void OnEnable()
        {
            // Re-read the JSON whenever the asset reloads, so regenerated atlases are picked up.
            _glyphs = null;
        }

        private void EnsureLoaded()
        {
            if (_glyphs != null)
            {
                return;
            }

            _glyphs = new Dictionary<char, Glyph>();
            _kerning = new Dictionary<int, int>();
            if (_glyphData == null)
            {
                Debug.LogWarning($"{name}: no glyph data assigned.", this);
                return;
            }

            FontJson data = JsonUtility.FromJson<FontJson>(_glyphData.text);
            if (data?.glyphs == null || data.atlasWidth <= 0 || data.atlasHeight <= 0)
            {
                Debug.LogWarning($"{name}: glyph data could not be read.", this);
                return;
            }

            _lineHeight = data.lineHeight;
            _baseline = data.baseline;
            _digitAdvance = data.digitAdvance;
            if (data.kerning != null)
            {
                foreach (KerningJson k in data.kerning)
                {
                    _kerning[(k.first << 16) | k.second] = k.amount;
                }
            }

            float w = data.atlasWidth;
            float h = data.atlasHeight;
            foreach (GlyphJson g in data.glyphs)
            {
                _glyphs[(char)g.code] = new Glyph
                {
                    // JSON rows count down from the top; UVs count up from the bottom.
                    Uv = new Rect(g.x / w, 1f - (g.y + g.h) / h, g.w / w, g.h / h),
                    Width = g.w,
                    Height = g.h,
                    Advance = g.advance,
                };
            }
        }
    }
}
