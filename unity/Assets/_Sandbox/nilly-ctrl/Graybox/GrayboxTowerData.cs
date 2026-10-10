using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Towers;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// One buildable tower: what the build bar shows for it and the prefab it places.
    /// </summary>
    /// <remarks>
    /// The tower's combat numbers (range, fire interval, weapon) live on the prefab. This asset
    /// only holds what the build UI needs before a tower exists.
    /// </remarks>
    [CreateAssetMenu(menuName = "Hold the Hill/Sandbox/Graybox Tower Data", fileName = "TowerData")]
    public class GrayboxTowerData : ScriptableObject
    {
        [Tooltip("Name on the build menu and the tower card.")]
        [SerializeField] private string _displayName;

        [Tooltip("The role it plays. Unspecified takes it from the prefab's weapon, or Gunner if it has none.")]
        [SerializeField] private TowerArchetype _archetype;

        [Tooltip("Icon to look up in GrayboxIcons, e.g. TowerLinearIcon.")]
        [SerializeField] private string _iconName;

        [Tooltip("Cost to build, before Skill Tree discounts.")]
        [SerializeField, Min(0)] private int _baseCost = 100;

        [Tooltip("What the cost is paid in. Empty means the run's main resource (Food).")]
        [SerializeField] private GrayboxResourceData _costResource;

        [Tooltip("The tower placed when this is built.")]
        [SerializeField] private Tower _prefab;

        [Tooltip("Sprite shown under the cursor while placing. Empty shows the range ring only.")]
        [SerializeField] private Sprite _ghostSprite;

        public string DisplayName => _displayName;

        public string IconName => _iconName;

        /// <summary>The tower's role: set on the asset, else its weapon's, else Gunner (a tower with only a projectile).</summary>
        public TowerArchetype Archetype
        {
            get
            {
                if (_archetype != TowerArchetype.Unspecified)
                {
                    return _archetype;
                }

                TowerWeapon weapon = _prefab != null ? _prefab.GetComponent<TowerWeapon>() : null;
                return weapon != null && weapon.Archetype != TowerArchetype.Unspecified ? weapon.Archetype : TowerArchetype.Gunner;
            }
        }

        public int BaseCost => _baseCost;

        /// <summary>What the cost is paid in, or null for the run's main resource.</summary>
        public GrayboxResourceData CostResource => _costResource;

        public Tower Prefab => _prefab;

        public Sprite GhostSprite => _ghostSprite;

        /// <summary>The prefab's reach in world units, or 0 with no prefab.</summary>
        public float Range => _prefab != null ? _prefab.Range : 0f;
    }
}
