using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// One power-up: what it is called, how long it lasts, the pickup that lies on the map and
    /// the effect prefab that runs while it is active.
    /// </summary>
    [CreateAssetMenu(menuName = "Hold the Hill/Sandbox/Graybox Power-Up Data", fileName = "PowerUpData")]
    public class GrayboxPowerUpData : ScriptableObject
    {
        [Tooltip("Short id, e.g. BerserkBerry.")]
        [SerializeField] private string _id;

        [Tooltip("Name shown to the player.")]
        [SerializeField] private string _displayName;

        [Tooltip("One line on what it does.")]
        [SerializeField] private string _description;

        [Tooltip("Colour for its timer and pickup.")]
        [SerializeField] private Color _color = Color.white;

        [Tooltip("Seconds it stays active once collected.")]
        [SerializeField, Min(0.1f)] private float _duration = 8f;

        [Tooltip("The pickup placed on the map.")]
        [SerializeField] private GameObject _pickupPrefab;

        [Tooltip("Made when it is collected and removed when it runs out. Needs a GrayboxPowerUpEffect.")]
        [SerializeField] private GrayboxPowerUpEffect _effectPrefab;

        public string Id => _id;

        public string DisplayName => _displayName;

        public string Description => _description;

        public Color Color => _color;

        public float Duration => _duration;

        public GameObject PickupPrefab => _pickupPrefab;

        public GrayboxPowerUpEffect EffectPrefab => _effectPrefab;
    }
}
