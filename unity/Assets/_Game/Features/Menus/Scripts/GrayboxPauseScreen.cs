using HoldTheHill.Core.UiKit;
using TMPro;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The pause menu: Resume, Restart, Settings, Controls, Quit to Home. Restart and Quit ask
    /// first, because both throw the run away.
    /// </summary>
    [RequireComponent(typeof(UiScreen))]
    [AddComponentMenu("Hold the Hill/Graybox/Graybox Pause Screen")]
    public class GrayboxPauseScreen : MonoBehaviour
    {
        [SerializeField] private TMP_Text _subtitle;
        [SerializeField] private UiButton _resume;
        [SerializeField] private UiButton _restart;
        [SerializeField] private UiButton _settings;
        [SerializeField] private UiButton _controls;
        [SerializeField] private UiButton _quit;
        [SerializeField] private UiConfirmDialog _dialog;
        [SerializeField] private UiScreen _settingsScreen;
        [SerializeField] private UiScreen _controlsScreen;

        private UiScreen _screen;
        private bool _hooked;

        public UiScreen Screen => _screen != null ? _screen : _screen = GetComponent<UiScreen>();
        public UiButton ResumeButton => _resume;
        public UiButton RestartButton => _restart;
        public UiButton SettingsButton => _settings;
        public UiButton ControlsButton => _controls;
        public UiButton QuitButton => _quit;

        private GrayboxGameFlow _flow;

        private GrayboxGameFlow Flow
        {
            get
            {
                if (_flow == null) _flow = FindAnyObjectByType<GrayboxGameFlow>();
                return _flow;
            }
        }

        private void Awake() => Hook();

        private void OnEnable()
        {
            Hook();
            if (_subtitle == null || Flow == null) return;

            GrayboxRunStats stats = Flow.Stats;
            _subtitle.text = stats.WaveReached > 0
                ? (stats.TotalWaves > 0
                    ? GrayboxLoc.Get("Wave {0} of {1}", stats.WaveReached, stats.TotalWaves)
                    : GrayboxLoc.Get("Endless, wave {0}", stats.WaveReached))
                : GrayboxLoc.Get("Before wave 1");
        }

        private void Hook()
        {
            if (_hooked) return;
            _hooked = true;

            // Back on the pause menu means "carry on", not "close this panel and stay frozen".
            Screen.CloseOnBack = false;
            Screen.BackPressed += Resume;

            _resume.Clicked.AddListener(Resume);
            _settings.Clicked.AddListener(_settingsScreen.Show);
            _controls.Clicked.AddListener(_controlsScreen.Show);
            _restart.Clicked.AddListener(() => _dialog.Open(
                GrayboxLoc.Get("Restart run?"), GrayboxLoc.Get("You go back to wave 1 and lose this run."),
                GrayboxLoc.Get("Restart"), GrayboxLoc.Get("Cancel"),
                () => { if (Flow != null) Flow.StartNewRun(); }));
            _quit.Clicked.AddListener(() => _dialog.Open(
                GrayboxLoc.Get("Quit to Home?"), GrayboxLoc.Get("This run will be lost."),
                GrayboxLoc.Get("Quit"), GrayboxLoc.Get("Keep playing"),
                () => { if (Flow != null) Flow.QuitToHome(); }));
        }

        private void Resume()
        {
            if (Flow != null) Flow.Resume();
        }
    }
}
