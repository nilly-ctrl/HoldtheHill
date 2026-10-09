using System.Collections.Generic;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Towers;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Picks up the nearest tower, and on the next press puts it down on the grid cell the
    /// Warden stands on. A carried tower does not attack, and the Warden walks slower.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Graybox/Warden/Carry")]
    public class GrayboxWardenCarry : GrayboxWardenAbility
    {
        [SerializeField, Min(0.1f)] private float _reach = 1.2f;
        [SerializeField, Range(0.1f, 1f)] private float _carrySpeedScale = 0.6f;
        [SerializeField] private Vector2 _carryOffset = new Vector2(0f, 0.5f);

        private readonly List<Tower> _towers = new List<Tower>();
        private readonly List<Behaviour> _switchedOff = new List<Behaviour>();
        private GrayboxTowerPlacer _placer;

        /// <summary>The tower being carried, or null.</summary>
        public Tower Carried { get; private set; }

        public override bool CanUse()
        {
            if (Carried == null)
            {
                return FindTower() != null;
            }

            return Placer() == null || Placer().CanBuildAt(Warden.transform.position, Carried);
        }

        protected override void Perform()
        {
            if (Carried == null)
            {
                PickUp(FindTower());
            }
            else
            {
                PutDown(GrayboxTowerPlacer.Snap(Warden.transform.position));
            }
        }

        private void LateUpdate()
        {
            if (Carried != null)
            {
                Carried.transform.position = Warden.transform.position + (Vector3)_carryOffset;
            }
            else if (_switchedOff.Count > 0)
            {
                // The tower was sold or destroyed while it was being carried.
                _switchedOff.Clear();
                Warden.SpeedScale = 1f;
            }
        }

        private void OnDisable()
        {
            if (Carried != null)
            {
                PutDown(Carried.transform.position);
            }
        }

        private void PickUp(Tower tower)
        {
            Carried = tower;
            _switchedOff.Clear();
            Switch(tower);
            foreach (TowerWeapon weapon in tower.GetComponents<TowerWeapon>())
            {
                Switch(weapon);
            }

            Warden.SpeedScale = _carrySpeedScale;
        }

        private void Switch(Behaviour behaviour)
        {
            if (behaviour.enabled)
            {
                behaviour.enabled = false;
                _switchedOff.Add(behaviour);
            }
        }

        private void PutDown(Vector3 at)
        {
            Carried.transform.position = at;
            foreach (Behaviour behaviour in _switchedOff)
            {
                if (behaviour != null)
                {
                    behaviour.enabled = true;
                }
            }

            _switchedOff.Clear();
            Carried = null;
            if (Warden != null)
            {
                Warden.SpeedScale = 1f;
            }
        }

        private Tower FindTower()
        {
            Tower.GetActive(_towers);
            Vector2 at = Warden.transform.position;
            Tower best = null;
            float bestSqr = _reach * _reach;
            foreach (Tower tower in _towers)
            {
                float sqr = ((Vector2)tower.transform.position - at).sqrMagnitude;
                if (sqr <= bestSqr && tower.enabled)
                {
                    bestSqr = sqr;
                    best = tower;
                }
            }

            return best;
        }

        private GrayboxTowerPlacer Placer()
        {
            if (_placer == null)
            {
                _placer = FindAnyObjectByType<GrayboxTowerPlacer>();
            }

            return _placer;
        }
    }
}
