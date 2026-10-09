using System.Collections.Generic;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Towers;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>Held, digs up a crumb of food where the Warden stands.</summary>
    [AddComponentMenu("Hold the Hill/Graybox/Warden/Dig")]
    public class GrayboxWardenDig : GrayboxWardenAbility
    {
        [Tooltip("What is dug up.")]
        [SerializeField] private GameObject _findPrefab;

        public override bool CanUse() => _findPrefab != null;

        protected override void Perform()
        {
            // A step ahead, so it is seen before it is walked into and collected.
            Vector3 at = Warden.transform.position + (Vector3)(Warden.Facing * 0.9f);
            Instantiate(_findPrefab, at, Quaternion.identity);
        }
    }
}
