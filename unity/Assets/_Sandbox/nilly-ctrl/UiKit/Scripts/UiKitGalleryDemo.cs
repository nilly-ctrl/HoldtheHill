using TMPro;
using UnityEngine;

namespace HoldTheHill.Sandbox.UiKit
{
    /// <summary>
    /// Wires up the UI kit gallery scene: opens the confirm dialog from a button and prints the
    /// last feedback cue, so every widget can be tried with mouse, keyboard and gamepad.
    /// Not used by the game.
    /// </summary>
    [AddComponentMenu("Hold the Hill/UI Kit/Gallery Demo")]
    public class UiKitGalleryDemo : MonoBehaviour
    {
        [SerializeField] private UiButton _openDialog;
        [SerializeField] private UiConfirmDialog _dialog;
        [SerializeField] private TMP_Text _status;

        private void OnEnable()
        {
            UiFeedback.Cue += OnCue;
            if (_openDialog != null) _openDialog.Clicked.AddListener(OpenDialog);
        }

        private void OnDisable()
        {
            UiFeedback.Cue -= OnCue;
            if (_openDialog != null) _openDialog.Clicked.RemoveListener(OpenDialog);
        }

        private void OpenDialog()
        {
            _dialog.Open("Quit to Home?", "This run will be lost.", "Quit", "Keep playing",
                () => Say("Confirmed"),
                () => Say("Cancelled"));
        }

        private void OnCue(UiCue cue) => Say($"Last cue: {cue}");

        private void Say(string text)
        {
            if (_status != null) _status.text = text;
        }
    }
}
