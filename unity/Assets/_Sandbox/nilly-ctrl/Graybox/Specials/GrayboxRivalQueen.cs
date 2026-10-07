using System.Collections;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Boss. The queen of a red colony. She walks part of the way, stops, and digs a burrow of
    /// her own beside the path. If she finishes it, red ants pour out of it; kill her before
    /// then and the half-dug burrow falls in. Either way she then walks on to the hill.
    /// </summary>
    /// <remarks>
    /// A race against her digging clock, not a health check: nothing interrupts the dig but her
    /// death.
    /// </remarks>
    public class GrayboxRivalQueen : GrayboxSpecialEnemy
    {
        [Tooltip("What comes out of her burrow.")]
        [SerializeField] private GameObject _antPrefab;
        [Tooltip("Where on the route she stops to dig, as a share of its length.")]
        [SerializeField, Range(0.1f, 0.9f)] private float _digAt = 0.4f;
        [SerializeField, Min(1f)] private float _digSeconds = 14f;
        [SerializeField, Min(1)] private int _antCount = 8;
        [SerializeField, Min(0.5f)] private float _antInterval = 4f;

        private GrayboxRivalBurrow _burrow;
        private bool _started;

        public GrayboxRivalBurrow Burrow => _burrow;

        /// <summary>True from the moment she stops until the burrow is finished.</summary>
        public bool IsDigging { get; private set; }

        protected override void OnEnable()
        {
            base.OnEnable();
            _started = false;
            IsDigging = false;
        }

        private void Update()
        {
            if (!_started && Alive && Mover.DistanceTravelled >= PathLength() * _digAt)
            {
                _started = true;
                StartCoroutine(Dig());
            }
        }

        private IEnumerator Dig()
        {
            IsDigging = true;
            Mover.SpeedScale = 0f;
            SetLoop("Dig");

            // The hole opens under her head.
            Vector3 site = transform.position + transform.right * 0.7f;
            _burrow = GrayboxRivalBurrow.Begin(Library, site, Mover.DistanceTravelled, _antPrefab, transform.parent);

            for (float t = 0f; t < _digSeconds && Alive; t += Time.deltaTime)
            {
                _burrow.SetProgress(t / _digSeconds);
                yield return null;
            }

            if (!Alive)
            {
                yield break; // OnDied has already brought it down
            }

            _burrow.Complete(_antCount, _antInterval);
            IsDigging = false;
            Say("BURROW DUG", DamageNumberKind.Fire, site);
            SetLoop("Walk");
            Play("Call");
            yield return new WaitForSeconds(ClipLength("Call", 0.7f));
            Mover.SpeedScale = 1f;
        }

        protected override void OnDied()
        {
            if (IsDigging && _burrow != null)
            {
                Say("BURROW COLLAPSED", DamageNumberKind.Heal, _burrow.transform.position);
                _burrow.Collapse();
            }
        }
    }
}
