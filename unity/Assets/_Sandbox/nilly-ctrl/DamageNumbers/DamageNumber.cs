using System.Collections.Generic;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// One floating popup. Builds a quad per glyph from a <see cref="PixelFontStyle"/> atlas into a
    /// single mesh, then rises, punches and fades. Owned and recycled by <see cref="DamageNumberSpawner"/>.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class DamageNumber : MonoBehaviour
    {
        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Vector2> _uvs = new List<Vector2>();
        private readonly List<Color32> _colors = new List<Color32>();
        private readonly List<int> _triangles = new List<int>();

        private DamageNumberSpawner _owner;
        private Mesh _mesh;
        private MeshRenderer _renderer;

        private DamageNumberMotion _motion;
        private Vector3 _origin;
        private Vector2 _offset;
        private Vector2 _velocity;
        private float _age;
        private float _unitsPerPixel;
        private bool _snap;
        private byte _alpha;

        /// <summary>True between Play and the moment it is handed back to the pool.</summary>
        public bool IsPlaying { get; private set; }

        /// <summary>Goes up by one every Play, so a caller can tell a recycled popup from the one it started.</summary>
        public int PlayId { get; private set; }

        internal void Init(DamageNumberSpawner owner)
        {
            _owner = owner;
            _mesh = new Mesh { name = "DamageNumber" };
            _mesh.MarkDynamic();
            GetComponent<MeshFilter>().sharedMesh = _mesh;
            _renderer = GetComponent<MeshRenderer>();
            _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _renderer.receiveShadows = false;
        }

        internal void Play(string text, PixelFontStyle style, Material material, Vector3 position,
            DamageNumberMotion motion, float unitsPerPixel, bool snapToPixels, string sortingLayer, int sortingOrder)
        {
            _motion = motion;
            _origin = position;
            _offset = Vector2.zero;
            _velocity = new Vector2(Random.Range(-motion.SidewaysSpeed, motion.SidewaysSpeed), motion.RiseSpeed);
            _age = 0f;
            _unitsPerPixel = unitsPerPixel;
            _snap = snapToPixels;
            _alpha = 255;

            _renderer.sharedMaterial = material;
            _renderer.sortingLayerName = sortingLayer;
            _renderer.sortingOrder = sortingOrder;

            BuildMesh(text, style);
            IsPlaying = true;
            PlayId++;
            Apply();
        }

        /// <summary>
        /// Changes the text of a popup that is already on screen and gives it a fresh punch and
        /// lifetime. It keeps its place, so merged hits read as one number counting up.
        /// </summary>
        internal void Retext(string text, PixelFontStyle style)
        {
            BuildMesh(text, style);
            _age = 0f;
            _alpha = 255;
            _velocity = new Vector2(0f, _motion.RiseSpeed * 0.35f);
            Apply();
        }

        internal void Stop()
        {
            IsPlaying = false;
        }

        private void Update()
        {
            if (!IsPlaying)
            {
                return;
            }

            float dt = Time.deltaTime;
            _age += dt;
            if (_age >= _motion.Lifetime)
            {
                _owner.Release(this);
                return;
            }

            _velocity.y -= _motion.Gravity * dt;
            _offset += _velocity * dt;
            Apply();
        }

        private void Apply()
        {
            Vector3 position = _origin + (Vector3)_offset;
            if (_snap && _unitsPerPixel > 0f)
            {
                position.x = Mathf.Round(position.x / _unitsPerPixel) * _unitsPerPixel;
                position.y = Mathf.Round(position.y / _unitsPerPixel) * _unitsPerPixel;
            }

            transform.position = position;

            // Punch: start at PunchScale and ease out to 1.
            float punch = 1f;
            if (_age < _motion.PunchDuration && _motion.PunchScale != 1f)
            {
                float t = _age / _motion.PunchDuration;
                float ease = 1f - (1f - t) * (1f - t) * (1f - t);
                punch = Mathf.Lerp(_motion.PunchScale, 1f, ease);
            }

            transform.localScale = Vector3.one * (_unitsPerPixel * punch);

            float fadeFrom = _motion.Lifetime * _motion.FadeStart;
            byte alpha = 255;
            if (_age > fadeFrom)
            {
                float t = Mathf.InverseLerp(fadeFrom, _motion.Lifetime, _age);
                alpha = (byte)Mathf.RoundToInt(255f * (1f - t));
            }

            if (alpha != _alpha)
            {
                _alpha = alpha;
                for (int i = 0; i < _colors.Count; i++)
                {
                    _colors[i] = new Color32(255, 255, 255, alpha);
                }

                _mesh.SetColors(_colors);
            }
        }

        // Mesh space is in font pixels: the baseline sits at y = 0 and the text is centred on the origin.
        private void BuildMesh(string text, PixelFontStyle style)
        {
            _vertices.Clear();
            _uvs.Clear();
            _colors.Clear();
            _triangles.Clear();

            int pen = -style.Measure(text) / 2;
            int previous = -1;
            int baseline = style.Baseline;
            int yShift = -baseline / 2;
            var white = new Color32(255, 255, 255, 255);

            foreach (char c in text)
            {
                if (!style.TryGetGlyph(c, out PixelFontStyle.Glyph g))
                {
                    continue;
                }

                if (previous >= 0)
                {
                    pen += style.GetKerning((char)previous, c);
                }

                previous = c;
                float x0 = pen;
                float x1 = pen + g.Width;
                float y1 = baseline + yShift;
                float y0 = y1 - g.Height;
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
                    _colors.Add(white);
                }

                // Later glyphs draw over earlier ones, so outlines overlap the way the preview shows.
                _triangles.Add(start);
                _triangles.Add(start + 1);
                _triangles.Add(start + 2);
                _triangles.Add(start);
                _triangles.Add(start + 2);
                _triangles.Add(start + 3);
                pen += g.Advance;
            }

            _mesh.Clear();
            _mesh.SetVertices(_vertices);
            _mesh.SetUVs(0, _uvs);
            _mesh.SetColors(_colors);
            _mesh.SetTriangles(_triangles, 0);
            _mesh.RecalculateBounds();
        }

        private void OnDestroy()
        {
            if (_mesh != null)
            {
                Destroy(_mesh);
            }
        }
    }
}
