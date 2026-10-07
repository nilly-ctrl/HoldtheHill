using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Slides a tiled texture along a LineRenderer so the beam flows and the lightning crackles.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class GrayboxLineScroll : MonoBehaviour
    {
        [Tooltip("Texture widths per second. Negative runs from the tower towards the target.")]
        [SerializeField] private float _speed = -3f;
        [Tooltip("Random jump added every frame, in texture widths. Use for lightning.")]
        [SerializeField] private float _jitter;

        private LineRenderer _line;
        private Material _material;
        private float _offset;

        /// <summary>
        /// Gives a weapon's LineRenderer a tiled, scrolling texture. The line is drawn white so
        /// the texture's own colours show.
        /// </summary>
        public static void Dress(LineRenderer line, Material textured, float width, float speed, float jitter)
        {
            if (line == null || textured == null)
            {
                return;
            }

            line.sharedMaterial = textured;
            line.textureMode = LineTextureMode.Tile;
            line.startColor = line.endColor = Color.white;
            line.startWidth = line.endWidth = width;
            line.numCapVertices = 0;

            var scroll = line.GetComponent<GrayboxLineScroll>();
            if (scroll == null)
            {
                scroll = line.gameObject.AddComponent<GrayboxLineScroll>();
            }

            scroll._speed = speed;
            scroll._jitter = jitter;
        }

        private void Awake()
        {
            _line = GetComponent<LineRenderer>();
        }

        private void Update()
        {
            if (!_line.enabled)
            {
                return;
            }

            if (_material == null)
            {
                _material = _line.material; // instanced per line, so towers don't scroll each other
            }

            _offset += _speed * Time.deltaTime;
            float jump = _jitter > 0f ? Random.Range(0f, _jitter) : 0f;
            _material.mainTextureOffset = new Vector2(Mathf.Repeat(_offset + jump, 1f), 0f);
        }

        private void OnDestroy()
        {
            if (_material != null)
            {
                Destroy(_material);
            }
        }
    }
}
