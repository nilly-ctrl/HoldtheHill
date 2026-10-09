using HoldTheHill.Features.Towers;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Combines everything that changes how fast one tower fires (a web slows it, a rally speeds
    /// it up) into the single <see cref="Tower.FireRateScale"/>, so one does not overwrite the
    /// other. Added to a tower the first time something changes its rate.
    /// </summary>
    public class GrayboxTowerRate : MonoBehaviour
    {
        private Tower _tower;
        private float _web = 1f;
        private float _rally = 1f;
        private float _rallyLeft;

        public bool IsRallied => _rallyLeft > 0f;

        public static GrayboxTowerRate Of(Tower tower)
        {
            var rate = tower.GetComponent<GrayboxTowerRate>();
            if (rate == null)
            {
                rate = tower.gameObject.AddComponent<GrayboxTowerRate>();
            }

            rate._tower = tower;
            return rate;
        }

        /// <summary>Sets the slow from being webbed. 1 is none.</summary>
        public void SetWeb(float multiplier)
        {
            _web = multiplier;
            Apply();
        }

        /// <summary>Speeds the tower up for a while. A second rally restarts the timer.</summary>
        public void Rally(float multiplier, float seconds)
        {
            _rally = multiplier;
            _rallyLeft = seconds;
            Apply();
        }

        private void Update()
        {
            if (_rallyLeft <= 0f)
            {
                return;
            }

            _rallyLeft -= Time.deltaTime;
            if (_rallyLeft <= 0f)
            {
                _rally = 1f;
                Apply();
            }
        }

        private void Apply()
        {
            if (_tower != null)
            {
                _tower.FireRateScale = _web * _rally;
            }
        }
    }
}
