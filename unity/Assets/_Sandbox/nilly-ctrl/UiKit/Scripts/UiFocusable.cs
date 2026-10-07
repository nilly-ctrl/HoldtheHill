using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HoldTheHill.Sandbox.UiKit
{
    /// <summary>
    /// Goes on every selectable kit control. Shows the focus ring while the control is selected,
    /// and makes the mouse move the same focus a keyboard or gamepad does, so there is only ever
    /// one highlighted control whichever device is in use.
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    [AddComponentMenu("Hold the Hill/UI Kit/Focusable")]
    public class UiFocusable : MonoBehaviour, ISelectHandler, IDeselectHandler, IPointerEnterHandler, ISubmitHandler, IPointerClickHandler
    {
        [Tooltip("Shown while this control has focus. Optional.")]
        [SerializeField] private GameObject _focusRing;

        [Tooltip("Raise the Press cue when clicked or submitted. Off for sliders, which raise Change instead.")]
        [SerializeField] private bool _pressCue = true;

        private Selectable _selectable;

        public bool HasFocus { get; private set; }

        private void Awake()
        {
            _selectable = GetComponent<Selectable>();
            if (_focusRing != null) _focusRing.SetActive(false);
        }

        private void OnDisable()
        {
            HasFocus = false;
            if (_focusRing != null) _focusRing.SetActive(false);
        }

        public void OnSelect(BaseEventData eventData)
        {
            HasFocus = true;
            if (_focusRing != null) _focusRing.SetActive(true);
            UiFeedback.Raise(UiCue.Focus);
        }

        public void OnDeselect(BaseEventData eventData)
        {
            HasFocus = false;
            if (_focusRing != null) _focusRing.SetActive(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_selectable == null || !_selectable.IsInteractable() || EventSystem.current == null) return;
            if (EventSystem.current.currentSelectedGameObject == gameObject) return;

            EventSystem.current.SetSelectedGameObject(gameObject);
        }

        public void OnSubmit(BaseEventData eventData) => RaisePress();

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left) RaisePress();
        }

        private void RaisePress()
        {
            if (!_pressCue || _selectable == null) return;

            UiFeedback.Raise(_selectable.IsInteractable() ? UiCue.Press : UiCue.Denied);
        }
    }
}
