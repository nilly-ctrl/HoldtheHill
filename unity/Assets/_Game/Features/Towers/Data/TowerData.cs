using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Economy;
using UnityEngine;
using UnityEngine.Serialization;

namespace HoldTheHill.Features.Towers
{
    /// <summary>
    /// One buildable tower: what the shop and the build bar show for it, what it costs, and the
    /// prefab it places.
    /// </summary>
    /// <remarks>
    /// The tower's combat numbers (range, fire interval, weapon) live on the prefab. This asset
    /// only holds what the build UI needs before a tower exists. Started as Adam Montgomery's
    /// shop card data (10.9.2026); the old field names still read through FormerlySerializedAs.
    /// </remarks>
    [CreateAssetMenu(fileName = "New Tower", menuName = "Hold The Hill/Tower Data")]
    public class TowerData : ScriptableObject
    {
        [Header("Shop Display")]
        [Tooltip("Name on the shop card, the build menu and the tower card.")]
        [SerializeField, FormerlySerializedAs("towerName")] private string _displayName;

        [Tooltip("Icon on the shop card.")]
        [SerializeField, FormerlySerializedAs("towerIcon")] private Sprite _icon;

        [Tooltip("Icon name, for a UI that looks its icons up by name (the graybox build bar).")]
        [SerializeField] private string _iconName;

        [Header("Cost")]
        [Tooltip("Cost to build, before any discount.")]
        [SerializeField, Min(0), FormerlySerializedAs("honeydewCost")] private int _baseCost = 100;

        [Tooltip("What the cost is paid in. Empty means the run's main resource.")]
        [SerializeField] private ResourceData _costResource;

        [Header("Placement")]
        [Tooltip("The role it plays. Unspecified takes it from the prefab's weapon, or Gunner if it has none.")]
        [SerializeField] private TowerArchetype _archetype;

        [Tooltip("The tower placed when this is built.")]
        [SerializeField] private Tower _prefab;

        [Tooltip("Sprite shown under the cursor while placing. Empty shows the range ring only.")]
        [SerializeField] private Sprite _ghostSprite;

        public string DisplayName => _displayName;

        public Sprite Icon => _icon;

        public string IconName => _iconName;

        public int BaseCost => _baseCost;

        /// <summary>What the cost is paid in, or null for the run's main resource.</summary>
        public ResourceData CostResource => _costResource;

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

        public Tower Prefab => _prefab;

        public Sprite GhostSprite => _ghostSprite;

        /// <summary>The prefab's reach in world units, or 0 with no prefab.</summary>
        public float Range => _prefab != null ? _prefab.Range : 0f;
    }
}
