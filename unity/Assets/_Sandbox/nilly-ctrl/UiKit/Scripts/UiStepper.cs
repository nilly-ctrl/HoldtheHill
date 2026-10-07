using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HoldTheHill.Sandbox.UiKit
{
    /// <summary>
    /// A labelled choice from a short list ("Resolution   &lt; 1920x1080 &gt;"). Left and right
    /// change the choice; up and down move on to the next control.
    /// </summary>
    /// <remarks>
    /// Used instead of a dropdown, which needs a pointer to be comfortable. The arrows are
    /// clickable for the mouse; a keyboard or gamepad never needs to focus them.
    /// </remarks>
    [AddComponentMenu("Hold the Hill/UI Kit/Stepper")]
    public class UiStepper : Selectable, ISubmitHandler
    {
        [SerializeField] private TMP_Text _label;
        [SerializeField] private TMP_Text _valueText;
        [SerializeField] private Button _previous;
        [SerializeField] private Button _next;

        [SerializeField] private List<string> _options = new List<string>();
        [SerializeField] private int _index;

        [Tooltip("Going past either end comes round to the other.")]
        [SerializeField] private bool _wrap = true;

        [SerializeField] private UnityEvent<int> _indexChanged = new UnityEvent<int>();

        /// <summary>Raised when the player changes the choice, not when code sets it.</summary>
        public UnityEvent<int> IndexChanged => _indexChanged;

        public IReadOnlyList<string> Options => _options;

        public string Label
        {
            get => _label != null ? _label.text : string.Empty;
            set
            {
                if (_label != null) _label.text = value;
            }
        }

        /// <summary>Setting it does not raise <see cref="IndexChanged"/>.</summary>
        public int Index
        {
            get => _index;
            set
            {
                _index = _options.Count == 0 ? 0 : Mathf.Clamp(value, 0, _options.Count - 1);
                Refresh();
            }
        }

        public string Current => _index >= 0 && _index < _options.Count ? _options[_index] : string.Empty;

        public void SetOptions(IEnumerable<string> options, int index = 0)
        {
            _options.Clear();
            _options.AddRange(options);
            Index = index;
        }

        /// <summary>Moves the choice by one, as the player would. Returns false at an end with wrap off.</summary>
        public bool Step(int direction)
        {
            if (_options.Count < 2 || !IsInteractable()) return false;

            int next = _index + (direction < 0 ? -1 : 1);
            if (_wrap)
            {
                next = (next + _options.Count) % _options.Count;
            }
            else if (next < 0 || next >= _options.Count)
            {
                UiFeedback.Raise(UiCue.Denied);
                return false;
            }

            _index = next;
            Refresh();
            UiFeedback.Raise(UiCue.Change);
            _indexChanged.Invoke(_index);
            return true;
        }

        protected override void Awake()
        {
            base.Awake();
            if (!Application.isPlaying) return;

            if (_previous != null) _previous.onClick.AddListener(() => Step(-1));
            if (_next != null) _next.onClick.AddListener(() => Step(1));
            Refresh();
        }

        public override void OnMove(AxisEventData eventData)
        {
            if (!IsActive() || !IsInteractable())
            {
                base.OnMove(eventData);
                return;
            }

            switch (eventData.moveDir)
            {
                case MoveDirection.Left:
                    Step(-1);
                    eventData.Use();
                    break;
                case MoveDirection.Right:
                    Step(1);
                    eventData.Use();
                    break;
                default:
                    base.OnMove(eventData);
                    break;
            }
        }

        public void OnSubmit(BaseEventData eventData) => Step(1);

        private void Refresh()
        {
            if (_valueText != null) _valueText.text = Current;
        }
    }
}
