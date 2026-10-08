using System.Collections.Generic;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// A line of text drawn with a baked <see cref="PixelFontStyle"/>: banners, titles, labels,
    /// counters. Put it on an empty GameObject, pick a style and type the text. It builds one quad
    /// per glyph, the same way the damage numbers do, and updates in the editor as you type.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [AddComponentMenu("Hold the Hill/Sandbox/Pixel Text")]
    public class PixelText : MonoBehaviour
    {
        public enum Anchor
        {
            Left,
            Centre,
            Right,
        }

        [SerializeField] private PixelFontStyle _style;
        [Tooltip("Style name for the theme switch (Title, Label, Banner, Hud...). Leave empty to keep the style above whatever the theme.")]
        [SerializeField] private string _themeStyle;
        [SerializeField, TextArea(1, 3)] private string _text = "HOLD THE HILL";
        [SerializeField] private Anchor _anchor = Anchor.Centre;
        [Tooltip("Give every digit the same width, so a counter does not shift as it changes.")]
        [SerializeField] private bool _fixedWidthDigits;
        [SerializeField] private Color _tint = Color.white;
        [Tooltip("Play the frames of an animated style (gold that shimmers, neon that flickers, blood that drips).")]
        [SerializeField] private bool _animate = true;

        [Header("Rendering")]
        [Tooltip("World pixels per unit. Match the game's sprites so a font pixel is the same size as an art pixel.")]
        [SerializeField, Min(1)] private int _pixelsPerUnit = 32;
        [Tooltip("Whole-number multiplier on font pixels.")]
        [SerializeField, Min(1)] private int _pixelScale = 1;
        [Tooltip("Unlit sprite shader that multiplies texture by vertex colour.")]
        [SerializeField] private Shader _shader;
        [SerializeField] private string _sortingLayer = "Default";
        [SerializeField] private int _sortingOrder = 400;

        // One material per atlas and shader, shared by every PixelText that uses them.
        private static readonly Dictionary<(Texture2D, Shader), Material> Materials =
            new Dictionary<(Texture2D, Shader), Material>();

        private static readonly int MainTex = Shader.PropertyToID("_MainTex");

        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Vector2> _uvs = new List<Vector2>();
        private readonly List<Color32> _colors = new List<Color32>();
        private readonly List<int> _triangles = new List<int>();
        private Mesh _mesh;
        private MaterialPropertyBlock _block;
        private int _shownFrame = -1;

        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value;
                    Rebuild();
                }
            }
        }

        public PixelFontStyle Style
        {
            get => _style;
            set
            {
                if (_style != value)
                {
                    _style = value;
                    Rebuild();
                }
            }
        }

        public Anchor Alignment
        {
            get => _anchor;
            set
            {
                if (_anchor != value)
                {
                    _anchor = value;
                    Rebuild();
                }
            }
        }

        /// <summary>Give every digit the same width, so a counter does not shift as it changes.</summary>
        public bool FixedWidthDigits
        {
            get => _fixedWidthDigits;
            set
            {
                if (_fixedWidthDigits != value)
                {
                    _fixedWidthDigits = value;
                    Rebuild();
                }
            }
        }

        /// <summary>The style name the theme switch looks up for this text, or empty to leave it alone.</summary>
        public string ThemeStyle
        {
            get => _themeStyle;
            set => _themeStyle = value;
        }

        /// <summary>Sets how the text is drawn, for text created from code.</summary>
        public void SetRendering(int pixelScale, int sortingOrder, string sortingLayer = "Default")
        {
            _pixelScale = Mathf.Max(1, pixelScale);
            _sortingOrder = sortingOrder;
            _sortingLayer = sortingLayer;
            Rebuild();
        }

        public Color Tint
        {
            get => _tint;
            set
            {
                _tint = value;
                Rebuild();
            }
        }

        private void Reset()
        {
            _shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (_shader == null)
            {
                _shader = Shader.Find("Sprites/Default");
            }
        }

        private void OnEnable()
        {
            Rebuild();
        }

        // An animated style swaps its atlas for the next frame; the mesh and material stay as they are.
        private void Update()
        {
            if (_style == null || _style.FrameCount < 2)
            {
                return;
            }

            int frame = _animate && Application.isPlaying
                ? (int)(Time.unscaledTime * _style.FramesPerSecond) % _style.FrameCount
                : 0;
            if (frame == _shownFrame)
            {
                return;
            }

            _shownFrame = frame;
            if (_block == null)
            {
                _block = new MaterialPropertyBlock();
            }

            var meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.GetPropertyBlock(_block);
            _block.SetTexture(MainTex, _style.Frame(frame));
            meshRenderer.SetPropertyBlock(_block);
        }

        private void OnValidate()
        {
            if (isActiveAndEnabled)
            {
                Rebuild();
            }
        }

        private void OnDestroy()
        {
            if (_mesh != null)
            {
                DestroyMesh(_mesh);
            }
        }

        /// <summary>Lays the text out again. Called for you when the text, style or tint changes.</summary>
        public void Rebuild()
        {
            if (_mesh == null)
            {
                _mesh = new Mesh { name = "PixelText", hideFlags = HideFlags.HideAndDontSave };
                GetComponent<MeshFilter>().sharedMesh = _mesh;
            }

            var meshRenderer = GetComponent<MeshRenderer>();
            meshRenderer.sortingLayerName = _sortingLayer;
            meshRenderer.sortingOrder = _sortingOrder;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            _vertices.Clear();
            _uvs.Clear();
            _colors.Clear();
            _triangles.Clear();
            _mesh.Clear();
            if (_style == null || _style.Atlas == null || string.IsNullOrEmpty(_text))
            {
                return;
            }

            meshRenderer.sharedMaterial = MaterialFor(_style.Atlas);
            meshRenderer.SetPropertyBlock(null);        // back to frame 0; Update picks the frame up again
            _shownFrame = -1;

            // Mesh space is in world units: the baseline sits at y = 0.
            float unit = (float)_pixelScale / _pixelsPerUnit;
            int width = Measure();
            int pen = _anchor == Anchor.Left ? 0 : _anchor == Anchor.Right ? -width : -width / 2;
            int baseline = _style.Baseline;
            Color32 colour = _tint;
            int previous = -1;

            foreach (char raw in _text)
            {
                if (!Resolve(raw, out char c, out PixelFontStyle.Glyph g))
                {
                    continue;
                }

                if (previous >= 0)
                {
                    pen += _style.GetKerning((char)previous, c);
                }

                previous = c;
                int advance = Advance(c, g);
                // A digit narrower than the shared width sits in the middle of its column.
                int x = pen + (advance - g.Advance) / 2;
                float x0 = x * unit;
                float x1 = (x + g.Width) * unit;
                float y1 = baseline * unit;
                float y0 = (baseline - g.Height) * unit;
                int start = _vertices.Count;
                _vertices.Add(new Vector3(x0, y0));
                _vertices.Add(new Vector3(x0, y1));
                _vertices.Add(new Vector3(x1, y1));
                _vertices.Add(new Vector3(x1, y0));
                _uvs.Add(new Vector2(g.Uv.xMin, g.Uv.yMin));
                _uvs.Add(new Vector2(g.Uv.xMin, g.Uv.yMax));
                _uvs.Add(new Vector2(g.Uv.xMax, g.Uv.yMax));
                _uvs.Add(new Vector2(g.Uv.xMax, g.Uv.yMin));
                for (int i = 0; i < 4; i++)
                {
                    _colors.Add(colour);
                }

                _triangles.Add(start);
                _triangles.Add(start + 1);
                _triangles.Add(start + 2);
                _triangles.Add(start);
                _triangles.Add(start + 2);
                _triangles.Add(start + 3);
                pen += advance;
            }

            _mesh.SetVertices(_vertices);
            _mesh.SetUVs(0, _uvs);
            _mesh.SetColors(_colors);
            _mesh.SetTriangles(_triangles, 0);
            _mesh.RecalculateBounds();
        }

        // Most styles hold capitals only, so a missing lowercase letter falls back to its capital.
        private bool Resolve(char raw, out char c, out PixelFontStyle.Glyph glyph)
        {
            c = raw;
            if (_style.TryGetGlyph(c, out glyph))
            {
                return true;
            }

            c = char.ToUpperInvariant(raw);
            return _style.TryGetGlyph(c, out glyph);
        }

        private int Advance(char c, PixelFontStyle.Glyph g)
        {
            return _fixedWidthDigits && c >= '0' && c <= '9' && _style.DigitAdvance > 0 ? _style.DigitAdvance : g.Advance;
        }

        private int Measure()
        {
            int width = 0;
            int lastExtra = 0;
            int previous = -1;
            foreach (char raw in _text)
            {
                if (!Resolve(raw, out char c, out PixelFontStyle.Glyph g))
                {
                    continue;
                }

                if (previous >= 0)
                {
                    width += _style.GetKerning((char)previous, c);
                }

                width += Advance(c, g);
                lastExtra = g.Width - g.Advance;
                previous = c;
            }

            return width + Mathf.Max(0, lastExtra);
        }

        private Material MaterialFor(Texture2D atlas)
        {
            // Reset() only runs in the editor, so text added from code finds its shader here.
            if (_shader == null)
            {
                _shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            }

            Shader shader = _shader != null ? _shader : Shader.Find("Sprites/Default");
            if (Materials.TryGetValue((atlas, shader), out Material material) && material != null)
            {
                return material;
            }

            material = new Material(shader)
            {
                name = "PixelText_" + atlas.name,
                mainTexture = atlas,
                hideFlags = HideFlags.HideAndDontSave,
            };
            Materials[(atlas, shader)] = material;
            return material;
        }

        private static void DestroyMesh(Mesh mesh)
        {
            if (Application.isPlaying)
            {
                Destroy(mesh);
            }
            else
            {
                DestroyImmediate(mesh);
            }
        }
    }
}
