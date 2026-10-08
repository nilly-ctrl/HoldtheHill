using HoldTheHill.Sandbox.UiKit;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>The title screen: Play (to Home), Settings, Controls, Quit.</summary>
    [RequireComponent(typeof(UiScreen))]
    [AddComponentMenu("Hold the Hill/Graybox/Graybox Title Screen")]
    public class GrayboxTitleScreen : MonoBehaviour
    {
        [SerializeField] private UiButton _play;
        [SerializeField] private UiButton _settings;
        [SerializeField] private UiButton _controls;
        [SerializeField] private UiButton _quit;
        [SerializeField] private UiScreen _settingsScreen;
        [SerializeField] private UiScreen _controlsScreen;
        [SerializeField] private UiConfirmDialog _dialog;

        private UiScreen _screen;
        private bool _hooked;

        public UiScreen Screen => _screen != null ? _screen : _screen = GetComponent<UiScreen>();
        public UiButton PlayButton => _play;
        public UiButton SettingsButton => _settings;
        public UiButton ControlsButton => _controls;
        public UiButton QuitButton => _quit;

        private void Awake() => Hook();

        private void OnEnable() => Hook();

        private void Hook()
        {
            if (_hooked) return;
            _hooked = true;

            // The first screen of the game: back has nowhere to go.
            Screen.CloseOnBack = false;

            _play.Clicked.AddListener(() => GrayboxGameFlow.Instance?.QuitToHome());
            _settings.Clicked.AddListener(_settingsScreen.Show);
            _controls.Clicked.AddListener(_controlsScreen.Show);
            _quit.Clicked.AddListener(() => _dialog.Open(
                "Quit the game?", "Your progress is saved.", "Quit", "Stay", Quit));
        }

        private static void Quit()
        {
            GrayboxSave.SaveIfDirty();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
