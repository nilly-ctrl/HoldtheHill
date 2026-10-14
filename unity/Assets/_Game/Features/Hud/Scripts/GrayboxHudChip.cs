using TMPro;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>A small framed label for the HUD strips: an ability, a power-up timer, the Warden's health.</summary>
    [AddComponentMenu("Hold the Hill/Graybox/HUD/Chip")]
    public class GrayboxHudChip : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;

        /// <summary>The chip's text; rich text colours work.</summary>
        public string Text
        {
            get => _text.text;
            set => _text.text = value;
        }

        /// <summary>The last value the owner built <see cref="Text"/> from, so it can skip rebuilding an unchanged one.</summary>
        public int StateKey { get; set; } = int.MinValue;
    }
}
