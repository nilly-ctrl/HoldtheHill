using UnityEngine;

namespace HoldTheHill.Features.Economy
{
    /// <summary>
    /// One thing the player collects and spends during a run, such as Food. Costs, pickups and
    /// the <see cref="Wallet"/> all name a resource with one of these.
    /// </summary>
    /// <remarks>
    /// Honeydew, which is kept between runs, lives in the save file and is not one of these.
    /// </remarks>
    [CreateAssetMenu(menuName = "Hold the Hill/Economy/Resource Data", fileName = "ResourceData")]
    public class ResourceData : ScriptableObject
    {
        [Tooltip("Short id used in code and saves, e.g. Food.")]
        [SerializeField] private string _id;

        [Tooltip("Name shown to the player.")]
        [SerializeField] private string _displayName;

        [Tooltip("Icon name, for whatever draws it.")]
        [SerializeField] private string _iconName;

        [Tooltip("Colour for its numbers and pickups.")]
        [SerializeField] private Color _color = Color.white;

        [Tooltip("How much a run starts with.")]
        [SerializeField, Min(0)] private int _startingAmount;

        public string Id => _id;

        public string DisplayName => _displayName;

        public string IconName => _iconName;

        public Color Color => _color;

        public int StartingAmount => _startingAmount;
    }
}
