using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HoldTheHill.Sandbox.UiKit
{
    /// <summary>A labelled on/off row. The whole row is the click target, not just the box.</summary>
    [AddComponentMenu("Hold the Hill/UI Kit/Toggle Field")]
    public class UiToggleField : MonoBehaviour
    {
        [SerializeField] private Toggle _toggle;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private UnityEvent<bool> _valueChanged = new UnityEvent<bool>();

        private bool _ready;

        /// <summary>Raised when the player flips it, not when code sets it.</summary>
        public UnityEvent<bool> ValueChanged => _valueChanged;

        public Toggle Toggle => _toggle;

        public string Label
        {
            get => _label != null ? _label.text : string.Empty;
            set
            {
                if (_label != null) _label.text = value;
            }
        }

        /// <summary>Setting it does not raise <see cref="ValueChanged"/>.</summary>
        public bool IsOn
        {
            get => _toggle.isOn;
            set
            {
                Prepare();
                _toggle.SetIsOnWithoutNotify(value);
            }
        }

        private void Awake() => Prepare();

        private void Prepare()
        {
            if (_ready || _toggle == null) return;

            _ready = true;
            _toggle.onValueChanged.AddListener(OnToggled);
        }

        private void OnToggled(bool isOn)
        {
            UiFeedback.Raise(UiCue.Change);
            _valueChanged.Invoke(isOn);
        }
    }
}
