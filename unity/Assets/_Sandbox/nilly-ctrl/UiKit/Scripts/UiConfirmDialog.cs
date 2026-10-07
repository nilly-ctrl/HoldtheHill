using System;
using TMPro;
using UnityEngine;

namespace HoldTheHill.Sandbox.UiKit
{
    /// <summary>
    /// A yes/no question over the current screen ("Quit to Home? This run will be lost.").
    /// Focus starts on the cancel button and back/cancel answers no, so a stray press never
    /// confirms something destructive.
    /// </summary>
    [RequireComponent(typeof(UiScreen))]
    [AddComponentMenu("Hold the Hill/UI Kit/Confirm Dialog")]
    public class UiConfirmDialog : MonoBehaviour
    {
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _message;
        [SerializeField] private UiButton _confirm;
        [SerializeField] private UiButton _cancel;

        private UiScreen _screen;
        private Action _onConfirm;
        private Action _onCancel;
        private bool _hooked;

        public bool IsOpen => gameObject.activeSelf;

        public void Open(string title, string message, string confirmLabel, string cancelLabel, Action onConfirm, Action onCancel = null)
        {
            Hook();
            if (_title != null) _title.text = title;
            if (_message != null) _message.text = message;
            _confirm.Label = confirmLabel;
            _cancel.Label = cancelLabel;
            _onConfirm = onConfirm;
            _onCancel = onCancel;

            _screen.FirstSelected = _cancel.Button;
            _screen.Show();
        }

        private void Hook()
        {
            if (_hooked) return;

            _hooked = true;
            _screen = GetComponent<UiScreen>();
            // The dialog closes itself, so that it is already shut when a callback runs
            // (a callback may open it again with the next question).
            _screen.CloseOnBack = false;
            _screen.BackPressed += () => Answer(false);
            _confirm.Clicked.AddListener(() => Answer(true));
            _cancel.Clicked.AddListener(() => Answer(false));
        }

        private void Answer(bool confirmed)
        {
            Action callback = confirmed ? _onConfirm : _onCancel;
            _onConfirm = null;
            _onCancel = null;

            _screen.Hide();
            callback?.Invoke();
        }
    }
}
