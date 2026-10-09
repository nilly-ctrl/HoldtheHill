using System.Collections.Generic;
using HoldTheHill.Features.Enemies;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The ant the player walks around the map. It does no fighting of its own to speak of:
    /// what it does is its <see cref="GrayboxWardenAbility"/> children (shove, repair, rally,
    /// dig, carry). It also collects any pickup it walks into.
    /// </summary>
    /// <remarks>
    /// First pass for the graybox. It has no health and cannot be hurt; what hurts the Warden
    /// is still an open design question (docs/level-up-plan, W3).
    /// </remarks>
    [AddComponentMenu("Hold the Hill/Graybox/Graybox Warden")]
    public class GrayboxWarden : MonoBehaviour
    {
        [Tooltip("World units per second.")]
        [SerializeField, Min(0.1f)] private float _speed = 3.5f;

        [Tooltip("Pickups this close are collected.")]
        [SerializeField, Min(0.1f)] private float _pickupReach = 0.5f;

        [Tooltip("Where it starts, measured from the end of the enemy route (the hill).")]
        [SerializeField] private Vector2 _startOffset = new Vector2(-1.5f, 1.5f);

        private readonly List<GrayboxWardenAbility> _abilities = new List<GrayboxWardenAbility>();
        private readonly List<GrayboxPickup> _pickups = new List<GrayboxPickup>();
        private Camera _camera;

        public static GrayboxWarden Instance { get; private set; }

        public IReadOnlyList<GrayboxWardenAbility> Abilities => _abilities;

        /// <summary>Multiplier on walking speed. Carrying something lowers it.</summary>
        public float SpeedScale { get; set; } = 1f;

        /// <summary>The way it last walked.</summary>
        public Vector2 Facing { get; private set; } = Vector2.right;

        private void Awake()
        {
            Instance = this;
            GetComponentsInChildren(_abilities);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            var path = FindAnyObjectByType<EnemyPath>();
            if (path != null && path.Waypoints.Count > 0)
            {
                transform.position = path.PointAt(path.Length) + (Vector3)_startOffset;
            }
        }

        private void Update()
        {
            if (!GrayboxGameFlow.GameplayActive)
            {
                return;
            }

            Move();
            CollectPickups();

            foreach (GrayboxWardenAbility ability in _abilities)
            {
                string control = ability.Data != null ? ability.Data.ControlId : null;
                if (!string.IsNullOrEmpty(control))
                {
                    ability.Tick(GrayboxControls.Pressed(control), GrayboxControls.Held(control));
                }
            }
        }

        private void Move()
        {
            var direction = new Vector2(
                (GrayboxControls.Held(GrayboxControls.MoveRight) ? 1f : 0f) - (GrayboxControls.Held(GrayboxControls.MoveLeft) ? 1f : 0f),
                (GrayboxControls.Held(GrayboxControls.MoveUp) ? 1f : 0f) - (GrayboxControls.Held(GrayboxControls.MoveDown) ? 1f : 0f));
            if (direction.sqrMagnitude < 0.01f)
            {
                return;
            }

            direction.Normalize();
            Facing = direction;
            Vector3 next = transform.position + (Vector3)(direction * (_speed * SpeedScale * Time.deltaTime));
            transform.position = KeepOnScreen(next);
        }

        private Vector3 KeepOnScreen(Vector3 position)
        {
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            if (_camera == null || !_camera.orthographic)
            {
                return position;
            }

            float halfHeight = _camera.orthographicSize;
            float halfWidth = halfHeight * _camera.aspect;
            Vector3 centre = _camera.transform.position;
            position.x = Mathf.Clamp(position.x, centre.x - halfWidth, centre.x + halfWidth);
            position.y = Mathf.Clamp(position.y, centre.y - halfHeight, centre.y + halfHeight);
            return position;
        }

        private void CollectPickups()
        {
            GrayboxPickup.GetActive(_pickups);
            foreach (GrayboxPickup pickup in _pickups)
            {
                float reach = _pickupReach + pickup.Reach * 0.5f;
                if (((Vector2)pickup.transform.position - (Vector2)transform.position).sqrMagnitude <= reach * reach)
                {
                    pickup.Collect();
                }
            }
        }
    }
}
