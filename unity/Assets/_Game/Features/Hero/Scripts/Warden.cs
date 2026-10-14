using HoldTheHill.Core.Input;
using HoldTheHill.Features.Economy;
using HoldTheHill.Features.GameFlow;
using System.Collections.Generic;
using HoldTheHill.Features.Enemies;
using UnityEngine;

namespace HoldTheHill.Features.Hero
{
    /// <summary>
    /// The ant the player walks around the map. It does no fighting of its own to speak of:
    /// what it does is its <see cref="WardenAbility"/> children (shove, repair, rally,
    /// dig, carry). It also collects any pickup it walks into.
    /// </summary>
    /// <remarks>
    /// Its health is a <see cref="WardenHealth"/> on the same prefab. What hurts the Warden
    /// is still an open design question (docs/level-up-plan, W3), so nothing does yet.
    /// </remarks>
    [AddComponentMenu("Hold the Hill/Warden")]
    public class Warden : MonoBehaviour
    {
        [Tooltip("World units per second.")]
        [SerializeField, Min(0.1f)] private float _speed = 3.5f;

        [Tooltip("Pickups this close are collected.")]
        [SerializeField, Min(0.1f)] private float _pickupReach = 0.5f;

        [Tooltip("Where it starts, measured from the end of the enemy route (the hill).")]
        [SerializeField] private Vector2 _startOffset = new Vector2(-1.5f, 1.5f);

        private readonly List<WardenAbility> _abilities = new List<WardenAbility>();
        private readonly List<Pickup> _pickups = new List<Pickup>();
        private Camera _camera;

        public static Warden Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        public IReadOnlyList<WardenAbility> Abilities => _abilities;

        /// <summary>The Warden's health, or null on a Warden prefab that has none.</summary>
        public WardenHealth Health { get; private set; }

        /// <summary>Multiplier on walking speed. Carrying something lowers it.</summary>
        public float SpeedScale { get; set; } = 1f;

        /// <summary>The way it last walked.</summary>
        public Vector2 Facing { get; private set; } = Vector2.right;

        private RunFlow _flow;

        private void Awake()
        {
            Instance = this;
            _flow = FindAnyObjectByType<RunFlow>();
            GetComponentsInChildren(_abilities);
            Health = GetComponent<WardenHealth>();
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
            ReturnToHill();
        }

        /// <summary>Puts the Warden back where it starts, beside the hill at the end of the route.</summary>
        public void ReturnToHill()
        {
            var path = FindAnyObjectByType<EnemyPath>();
            if (path != null && path.Waypoints.Count > 0)
            {
                transform.position = path.PointAt(path.Length) + (Vector3)_startOffset;
            }
        }

        /// <summary>Breaks off whatever the abilities are part way through: a hit resets a hold, being downed drops what is carried.</summary>
        public void InterruptAbilities(bool downed)
        {
            foreach (WardenAbility ability in _abilities)
            {
                ability.Interrupt(downed);
            }
        }

        private void Update()
        {
            if ((_flow != null && !_flow.IsPlaying) || (Health != null && Health.IsDowned))
            {
                return;
            }

            Move();
            CollectPickups();

            foreach (WardenAbility ability in _abilities)
            {
                string control = ability.Data != null ? ability.Data.ControlId : null;
                if (!string.IsNullOrEmpty(control))
                {
                    ability.Tick(GameControls.Pressed(control), GameControls.Held(control));
                }
            }
        }

        private void Move()
        {
            var direction = new Vector2(
                (GameControls.Held(GameControls.MoveRight) ? 1f : 0f) - (GameControls.Held(GameControls.MoveLeft) ? 1f : 0f),
                (GameControls.Held(GameControls.MoveUp) ? 1f : 0f) - (GameControls.Held(GameControls.MoveDown) ? 1f : 0f));
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
            Pickup.GetActive(_pickups);
            foreach (Pickup pickup in _pickups)
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
