using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Something lying on the map that is collected by passing the mouse over it, or by anything
    /// that calls <see cref="Collect"/> (the Warden walking into it). What collecting does is up
    /// to the <see cref="GrayboxPickupEffect"/> components on the same object.
    /// </summary>
    /// <remarks>
    /// Sits on the pickup base prefab. A new pickup is a variant of that prefab with one or more
    /// effects added, not a new subclass of this.
    /// </remarks>
    [AddComponentMenu("Hold the Hill/Graybox/Graybox Pickup")]
    public class GrayboxPickup : MonoBehaviour
    {
        [Tooltip("How close the mouse has to come, in world units.")]
        [SerializeField, Min(0.05f)] private float _reach = 0.45f;

        [Tooltip("Seconds before it disappears uncollected. 0 keeps it until collected.")]
        [SerializeField, Min(0f)] private float _lifetime = 12f;

        [Tooltip("How far it bobs up and down, in world units.")]
        [SerializeField, Min(0f)] private float _bob = 0.06f;

        private static readonly List<GrayboxPickup> s_active = new List<GrayboxPickup>();

        private Vector3 _home;
        private float _age;
        private Camera _camera;

        /// <summary>Raised when any pickup is collected, before it is removed.</summary>
        public static event Action<GrayboxPickup> Collected;

        public float Reach => _reach;

        public bool IsCollected { get; private set; }

        /// <summary>Fills <paramref name="results"/> with every pickup lying on the map.</summary>
        public static void GetActive(List<GrayboxPickup> results)
        {
            results.Clear();
            results.AddRange(s_active);
        }

        private void OnEnable()
        {
            s_active.Add(this);
        }

        private void OnDisable()
        {
            s_active.Remove(this);
        }

        private void Start()
        {
            _home = transform.position;
        }

        private void Update()
        {
            if (IsCollected || !GrayboxGameFlow.GameplayActive)
            {
                return;
            }

            _age += Time.deltaTime;
            transform.position = _home + Vector3.up * (Mathf.Sin(_age * 4f) * _bob);

            if (_lifetime > 0f && _age >= _lifetime)
            {
                Destroy(gameObject);
                return;
            }

            if (MouseIsOver())
            {
                Collect();
            }
        }

        private bool MouseIsOver()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return false;
            }

            if (_camera == null)
            {
                _camera = Camera.main;
                if (_camera == null)
                {
                    return false;
                }
            }

            Vector2 world = _camera.ScreenToWorldPoint(mouse.position.ReadValue());
            return (world - (Vector2)_home).sqrMagnitude <= _reach * _reach;
        }

        /// <summary>Applies every effect on this pickup, then removes it. Safe to call twice.</summary>
        public void Collect()
        {
            if (IsCollected)
            {
                return;
            }

            IsCollected = true;
            foreach (GrayboxPickupEffect effect in GetComponents<GrayboxPickupEffect>())
            {
                effect.Apply(this);
            }

            Collected?.Invoke(this);
            SpriteClipPlayer.SpawnOneShot(GrayboxSpecials.Library, "FxPickup", "Collect", transform.position, Quaternion.identity, 1f, 6);
            Destroy(gameObject);
        }
    }
}
