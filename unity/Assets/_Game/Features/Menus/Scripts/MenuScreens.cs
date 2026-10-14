using HoldTheHill.Features.Hud;
using HoldTheHill.Features.GameFlow;
using HoldTheHill.Core.UiKit;
using UnityEngine;

namespace HoldTheHill.Features.Menus
{
    /// <summary>
    /// Opens and closes the menu screens to match <see cref="RunFlow"/>: title, home, the
    /// pause screen while paused, the run-end screen when a run finishes. The flow controller
    /// decides the state; this only shows it. Settings and Controls open over whichever screen
    /// asked for them and close with it.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Menus")]
    public class MenuScreens : MonoBehaviour
    {
        [SerializeField] private TitleScreen _title;
        [SerializeField] private HomeScreen _home;
        [SerializeField] private PauseScreen _pause;
        [SerializeField] private RunEndScreen _runEnd;
        [SerializeField] private SettingsScreen _settings;
        [SerializeField] private ControlsScreen _controls;
        [SerializeField] private UiConfirmDialog _dialog;

        public TitleScreen Title => _title;
        public HomeScreen Home => _home;
        public SettingsScreen Settings => _settings;
        public ControlsScreen Controls => _controls;
        public PauseScreen Pause => _pause;
        public RunEndScreen RunEnd => _runEnd;
        public UiConfirmDialog Dialog => _dialog;

        // The longest the run-end panel waits for the VICTORY or DEFEAT banner to play first.
        private const float MaxBannerSeconds = 1.6f;

        private WaveBanner _banner;
        private Coroutine _runEndWait;

        private RunFlow _flow;
        private bool _listening;

        private void OnEnable() => Listen();

        private void OnDisable()
        {
            if (_listening) _flow.StateChanged -= OnStateChanged;
            _listening = false;
        }

        // The flow may be built after this object in a test; Start tries again.
        private void Listen()
        {
            if (_listening) return;
            if (_flow == null) _flow = FindAnyObjectByType<RunFlow>();
            if (_flow == null) return;
            _flow.StateChanged += OnStateChanged;
            _listening = true;
        }

        private void Start()
        {
            _banner = FindAnyObjectByType<WaveBanner>();
            Listen();
            if (_flow != null) Sync(_flow.State);
        }

        private void OnStateChanged(GameFlowState previous, GameFlowState next) => Sync(next);

        private void Sync(GameFlowState state)
        {
            // A question left open belongs to the state that asked it.
            if (_dialog != null && _dialog.IsOpen) _dialog.Dismiss();
            if (_settings != null) SetOpen(_settings.Screen, false);
            if (_controls != null) SetOpen(_controls.Screen, false);

            if (_title != null) SetOpen(_title.Screen, state == GameFlowState.Title);
            if (_home != null) SetOpen(_home.Screen, state == GameFlowState.Home);
            if (_pause != null) SetOpen(_pause.Screen, state == GameFlowState.Paused);

            if (_runEndWait != null)
            {
                StopCoroutine(_runEndWait);
                _runEndWait = null;
            }

            if (_runEnd == null) return;

            if (state != GameFlowState.RunEnd)
            {
                SetOpen(_runEnd.Screen, false);
            }
            else if (_banner != null && _banner.isActiveAndEnabled)
            {
                // The banner and the panel would sit on top of each other; the banner goes first.
                _runEndWait = StartCoroutine(OpenRunEndAfterBanner());
            }
            else
            {
                OpenRunEnd();
            }
        }

        private System.Collections.IEnumerator OpenRunEndAfterBanner()
        {
            // Real time: the game is frozen at run end.
            yield return new WaitForSecondsRealtime(Mathf.Min(_banner.EndSeconds, MaxBannerSeconds));
            _runEndWait = null;
            _banner.Hide();
            OpenRunEnd();
        }

        private void OpenRunEnd()
        {
            _runEnd.Populate(_flow);
            SetOpen(_runEnd.Screen, true);
        }

        private static void SetOpen(UiScreen screen, bool open)
        {
            if (screen.gameObject.activeSelf != open) screen.gameObject.SetActive(open);
        }
    }
}
