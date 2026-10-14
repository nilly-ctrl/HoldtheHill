using System.Collections.Generic;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Towers;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>Makes the towers around the Warden attack faster for a few seconds.</summary>
    [AddComponentMenu("Hold the Hill/Graybox/Warden/Rally")]
    public class GrayboxWardenRally : GrayboxWardenAbility
    {
        [SerializeField, Min(0.1f)] private float _radius = 3f;
        [SerializeField, Min(1f)] private float _rateMultiplier = 1.5f;
        [SerializeField, Min(0.1f)] private float _seconds = 6f;

        private readonly List<Tower> _towers = new List<Tower>();

        public override bool CanUse()
        {
            FindTowers();
            return _towers.Count > 0;
        }

        protected override void Perform()
        {
            FindTowers();
            foreach (Tower tower in _towers)
            {
                GrayboxTowerRate.Of(tower).Rally(_rateMultiplier, _seconds);
            }
        }

        private void FindTowers()
        {
            Tower.GetActive(_towers);
            Vector2 at = Warden.transform.position;
            _towers.RemoveAll(tower => ((Vector2)tower.transform.position - at).sqrMagnitude > _radius * _radius);
        }
    }
}
