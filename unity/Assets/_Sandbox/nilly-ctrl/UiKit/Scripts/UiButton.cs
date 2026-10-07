using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HoldTheHill.Sandbox.UiKit
{
    /// <summary>A kit button: Unity's Button plus its label, kept in step when it is disabled.</summary>
    [RequireComponent(typeof(Button))]
    [AddComponentMenu("Hold the Hill/UI Kit/Button")]
    public class UiButton : MonoBehaviour
    {
        [SerializeField] private TMP_Text _label;

        private Button _button;
        private Color _labelColour = UiKitStyle.Cream;

        public Button Button => _button != null ? _button : _button = GetComponent<Button>();

        /// <summary>Shortcut for <c>Button.onClick</c>.</summary>
        public Button.ButtonClickedEvent Clicked => Button.onClick;

        public string Label
        {
            get => _label != null ? _label.text : string.Empty;
            set
            {
                if (_label != null) _label.text = value;
            }
        }

        /// <summary>Label colour while the button is usable. A disabled button is always greyed.</summary>
        public Color LabelColour
        {
            get => _labelColour;
            set
            {
                _labelColour = value;
                RefreshLabel();
            }
        }

        public bool Interactable
        {
            get => Button.interactable;
            set
            {
                Button.interactable = value;
                RefreshLabel();
            }
        }

        private void OnEnable() => RefreshLabel();

        private void RefreshLabel()
        {
            if (_label != null) _label.color = Button.interactable ? _labelColour : (Color)UiKitStyle.Disabled;
        }
    }
}
