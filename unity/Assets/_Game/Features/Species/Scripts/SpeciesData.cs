using HoldTheHill.Features.Towers;
using HoldTheHill.Features.Hero;
using UnityEngine;

namespace HoldTheHill.Features.Species
{
    /// <summary>
    /// One species of ant the player can play as: which towers it can build, which Warden it
    /// fields and what it starts a run with. A new species is a new one of these plus its
    /// prefab variants; no code changes.
    /// </summary>
    /// <remarks>
    /// A species' castes are prefab variants of the shared tower prefabs (a Fire Ant Soldier is
    /// a variant of TowerSoldier), so they override only what is different about them.
    /// </remarks>
    [CreateAssetMenu(menuName = "Hold the Hill/Species Data", fileName = "SpeciesData")]
    public class SpeciesData : ScriptableObject
    {
        [Tooltip("Short id used in code and saves, e.g. GardenAnt.")]
        [SerializeField] private string _id;

        [Tooltip("Name shown to the player.")]
        [SerializeField] private string _displayName;

        [Tooltip("One or two lines for a species picker.")]
        [SerializeField, TextArea] private string _description;

        [Tooltip("Colour that stands for the species in menus.")]
        [SerializeField] private Color _color = Color.white;

        [Tooltip("The towers this species can build, in build bar order.")]
        [SerializeField] private TowerCatalog _towerCatalog;

        [Tooltip("The Warden this species fields. Empty fields none.")]
        [SerializeField] private Warden _wardenPrefab;

        [Tooltip("Food a run starts with. 0 leaves the usual starting amount.")]
        [SerializeField, Min(0)] private int _startingFood;

        public string Id => _id;

        public string DisplayName => _displayName;

        public string Description => _description;

        public Color Color => _color;

        public TowerCatalog TowerCatalog => _towerCatalog;

        public Warden WardenPrefab => _wardenPrefab;

        public int StartingFood => _startingFood;
    }
}
