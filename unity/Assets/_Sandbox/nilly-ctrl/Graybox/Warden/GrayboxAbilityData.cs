using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// One Warden ability as the HUD and the controls screen see it: its name, its key and
    /// its timing. What it does is the <see cref="GrayboxWardenAbility"/> on its prefab.
    /// </summary>
    [CreateAssetMenu(menuName = "Hold the Hill/Sandbox/Graybox Ability Data", fileName = "AbilityData")]
    public class GrayboxAbilityData : ScriptableObject
    {
        [Tooltip("Short id, e.g. Shove.")]
        [SerializeField] private string _id;

        [Tooltip("Name shown to the player.")]
        [SerializeField] private string _displayName;

        [Tooltip("One line on what it does, for a tooltip or help page.")]
        [SerializeField] private string _description;

        [Tooltip("The GrayboxControls action that triggers it.")]
        [SerializeField] private string _controlId;

        [Tooltip("Seconds after a use before it can be used again.")]
        [SerializeField, Min(0f)] private float _cooldown;

        [Tooltip("Seconds the key has to be held. 0 fires on the press.")]
        [SerializeField, Min(0f)] private float _holdSeconds;

        public string Id => _id;

        public string DisplayName => _displayName;

        public string Description => _description;

        public string ControlId => _controlId;

        public float Cooldown => _cooldown;

        public float HoldSeconds => _holdSeconds;
    }
}
