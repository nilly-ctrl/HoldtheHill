using System.Collections.Generic;
using HoldTheHill.Features.Towers;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Flies in a straight line from the burrow to the hill, and stings the first tower it
    /// passes close to, stunning it for a moment.
    /// </summary>
    public class GrayboxWasp : GrayboxFlyer
    {
        [SerializeField, Min(0.1f)] private float _stingRange = 2.3f;
        [SerializeField, Min(0.1f)] private float _stunSeconds = 2.5f;

        private bool _hasStung;

        protected override void Start()
        {
            base.Start();
            if (Mover != null)
            {
                Mover.SkipToLastWaypoint();
            }

            IReadOnlyList<Vector3> points = Waypoints;
            if (points != null && points.Count > 0)
            {
                FlipToward(points[points.Count - 1] - transform.position);
            }
        }

        private void Update()
        {
            if (_hasStung || !Alive)
            {
                return;
            }

            Tower tower = NearestTower(transform.position, _stingRange, t => !GrayboxTowerAffliction.Silenced(t));
            if (tower == null)
            {
                return;
            }

            _hasStung = true;
            Play("Sting");
            GrayboxTowerAffliction.Apply(tower, GrayboxTowerAffliction.Kind.Stun, _stunSeconds);
            SpriteClipPlayer.SpawnOneShot(Library, "FxImpact", "Zap", tower.transform.position, Quaternion.identity, 1f, 6);
        }
    }
}
