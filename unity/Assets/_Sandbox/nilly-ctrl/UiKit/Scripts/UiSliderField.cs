using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HoldTheHill.Sandbox.UiKit
{
    /// <summary>
    /// A labelled slider row with its value written beside it ("Music   [====  ]  60%").
    /// </summary>
    /// <remarks>
    /// The slider moves in whole steps rather than freely. That gives a keyboard or gamepad a
    /// sensible amount per press (Unity's own step is a tenth of the range) and stops a mouse
    /// drag from landing on values like 63.7%.
    /// </remarks>
    [AddComponentMenu("Hold the Hill/UI Kit/Slider Field")]
    public class UiSliderField : MonoBehaviour
    {
        public enum ValueFormat
        {
            /// <summary>0..1 shown as 0%..100%.</summary>
            Percent,

            /// <summary>The value itself, rounded.</summary>
            Number,
        }

        [SerializeField] private Slider _slider;
        [SerializeField] private TMP_Text _label;
        [SerializeField] private TMP_Text _valueText;

        [SerializeField] private float _min;
        [SerializeField] private float _max = 1f;

        [Tooltip("How many steps the range is cut into. 20 over 0..1 is 5% per press.")]
        [SerializeField, Min(1)] private int _steps = 20;

        [SerializeField] private ValueFormat _format = ValueFormat.Percent;
        [SerializeField] private UnityEvent<float> _valueChanged = new UnityEvent<float>();

        private bool _ready;

        /// <summary>Raised when the player changes the value, not when code sets it.</summary>
        public UnityEvent<float> ValueChanged => _valueChanged;

        public Slider Slider => _slider;

        public string Label
        {
            get => _label != null ? _label.text : string.Empty;
            set
            {
                if (_label != null) _label.text = value;
            }
        }

        /// <summary>The value in <c>min..max</c>. Setting it does not raise <see cref="ValueChanged"/>.</summary>
        public float Value
        {
            get
            {
                Prepare();
                return Mathf.Lerp(_min, _max, _slider.value / _steps);
            }
            set
            {
                Prepare();
                float t = Mathf.Approximately(_min, _max) ? 0f : Mathf.InverseLerp(_min, _max, value);
                _slider.SetValueWithoutNotify(Mathf.Round(t * _steps));
                RefreshText();
            }
        }

        /// <summary>Changes the range and step count, keeping the current value where it fits.</summary>
        public void Configure(float min, float max, int steps, ValueFormat format)
        {
            float current = _ready ? Value : min;
            _min = min;
            _max = max;
            _steps = Mathf.Max(1, steps);
            _format = format;
            _ready = false;
            Prepare();
            Value = current;
        }

        private void Awake() => Prepare();

        private void Prepare()
        {
            if (_ready || _slider == null) return;

            _ready = true;
            float t = _slider.maxValue > _slider.minValue ? Mathf.InverseLerp(_slider.minValue, _slider.maxValue, _slider.value) : 0f;
            _slider.wholeNumbers = true;
            _slider.minValue = 0f;
            _slider.maxValue = _steps;
            _slider.SetValueWithoutNotify(Mathf.Round(t * _steps));
            _slider.onValueChanged.RemoveListener(OnSliderChanged);
            _slider.onValueChanged.AddListener(OnSliderChanged);
            RefreshText();
        }

        private void OnSliderChanged(float raw)
        {
            RefreshText();
            UiFeedback.Raise(UiCue.Change);
            _valueChanged.Invoke(Value);
        }

        private void RefreshText()
        {
            if (_valueText == null) return;

            float value = Mathf.Lerp(_min, _max, _slider.value / _steps);
            _valueText.text = _format == ValueFormat.Percent
                ? $"{Mathf.RoundToInt(value * 100f)}%"
                : Mathf.RoundToInt(value).ToString();
        }
    }
}
