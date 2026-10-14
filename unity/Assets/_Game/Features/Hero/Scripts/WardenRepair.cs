using HoldTheHill.Features.Specials;
using System.Collections.Generic;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Towers;
using UnityEngine;

namespace HoldTheHill.Features.Hero
{
    /// <summary>Held next to a stunned, webbed, cocooned or scalded tower, sets it right.</summary>
    [AddComponentMenu("Hold the Hill/Warden/Repair")]
    public class WardenRepair : WardenAbility
    {
        private static readonly TowerAffliction.Kind[] Kinds =
            (TowerAffliction.Kind[])System.Enum.GetValues(typeof(TowerAffliction.Kind));

        [SerializeField, Min(0.1f)] private float _reach = 1.5f;

        private readonly List<Tower> _towers = new List<Tower>();

        public override bool CanUse() => FindHurtTower() != null;

        protected override void Perform()
        {
            Tower tower = FindHurtTower();
            foreach (TowerAffliction.Kind kind in Kinds)
            {
                TowerAffliction.Clear(tower, kind);
            }
        }

        private Tower FindHurtTower()
        {
            Tower.GetActive(_towers);
            Vector2 at = Warden.transform.position;
            Tower best = null;
            float bestSqr = _reach * _reach;
            foreach (Tower tower in _towers)
            {
                float sqr = ((Vector2)tower.transform.position - at).sqrMagnitude;
                if (sqr <= bestSqr && IsHurt(tower))
                {
                    bestSqr = sqr;
                    best = tower;
                }
            }

            return best;
        }

        private static bool IsHurt(Tower tower)
        {
            foreach (TowerAffliction.Kind kind in Kinds)
            {
                if (TowerAffliction.IsAfflicted(tower, kind))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
