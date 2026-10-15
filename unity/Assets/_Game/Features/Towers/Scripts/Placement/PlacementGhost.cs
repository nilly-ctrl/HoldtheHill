using UnityEngine;

namespace HoldTheHill.Features.Towers
{
    /// <summary>
    /// The tower drawn under the cursor while placing, snapped to its cell, with its range ring:
    /// green where it can be built, red where it can't. Made on first show, hidden when nothing
    /// is being placed.
    /// </summary>
    public sealed class PlacementGhost
    {
        private const int RingSegments = 48;
        private static readonly Color Good = new Color(0.6f, 1f, 0.6f, 0.75f);
        private static readonly Color Bad = new Color(1f, 0.4f, 0.4f, 0.65f);

        private readonly Material _lineMaterial;
        private SpriteRenderer _sprite;
        private LineRenderer _ring;

        /// <summary><paramref name="lineMaterial"/> draws the range ring; null draws it with the default line material.</summary>
        public PlacementGhost(Material lineMaterial)
        {
            _lineMaterial = lineMaterial;
        }

        /// <summary>Shows a tower's ghost at a cell, tinted green when it may be built there and red when not.</summary>
        public void Show(TowerData tower, Vector3 cell, bool allowed)
        {
            if (_sprite == null)
            {
                Build();
            }

            Color tint = allowed ? Good : Bad;
            _sprite.sprite = tower.GhostSprite;
            _sprite.color = tint;
            _sprite.transform.position = cell;
            _sprite.gameObject.SetActive(true);

            // A tower with almost no reach still gets a ring big enough to see.
            float radius = Mathf.Max(tower.Range, 0.5f);
            for (int i = 0; i < RingSegments; i++)
            {
                float angle = i * Mathf.PI * 2f / RingSegments;
                _ring.SetPosition(i, new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius);
            }

            _ring.startColor = _ring.endColor = tint;
        }

        public void Hide()
        {
            if (_sprite != null)
            {
                _sprite.gameObject.SetActive(false);
            }
        }

        private void Build()
        {
            _sprite = new GameObject("Placement Ghost").AddComponent<SpriteRenderer>();
            _sprite.sortingOrder = 6;

            _ring = new GameObject("Range").AddComponent<LineRenderer>();
            _ring.transform.SetParent(_sprite.transform, false);
            _ring.useWorldSpace = false;
            _ring.loop = true;
            _ring.positionCount = RingSegments;
            _ring.startWidth = _ring.endWidth = 0.05f;
            _ring.sharedMaterial = _lineMaterial;
            _ring.sortingOrder = 6;
        }
    }
}
