using HoldTheHill.Sandbox.UiKit;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Opens and closes the menu screens to match <see cref="GrayboxGameFlow"/>: title, home, the
    /// pause screen while paused, the run-end screen when a run finishes. The flow controller
    /// decides the state; this only shows it. Settings and Controls open over whichever screen
    /// asked for them and close with it.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Graybox/Graybox Menus")]
    public class GrayboxMenus : MonoBehaviour
    {
        [SerializeField] private GrayboxTitleScreen _title;
        [SerializeField] private GrayboxHomeScreen _home;
        [SerializeField] private GrayboxPauseScreen _pause;
        [SerializeField] private GrayboxRunEndScreen _runEnd;
        [SerializeField] private GrayboxSettingsScreen _settings;
        [SerializeField] private GrayboxControlsScreen _controls;
        [SerializeField] private UiConfirmDialog _dialog;

        public GrayboxTitleScreen Title => _title;
        public GrayboxHomeScreen Home => _home;
        public GrayboxSettingsScreen Settings => _settings;
        public GrayboxControlsScreen Controls => _controls;
        public GrayboxPauseScreen Pause => _pause;
        public GrayboxRunEndScreen RunEnd => _runEnd;
        public UiConfirmDialog Dialog => _dialog;

        // The longest the run-end panel waits for the VICTORY or DEFEAT banner to play first.
        private const float MaxBannerSeconds = 1.6f;

        private GrayboxWaveBanner _banner;
        private Coroutine _runEndWait;

        private void OnEnable()
        {
            GrayboxGameFlow.StateChanged += OnStateChanged;
        }

        private void OnDisable()
        {
            GrayboxGameFlow.StateChanged -= OnStateChanged;
        }

        private void Start()
        {
            _banner = FindAnyObjectByType<GrayboxWaveBanner>();
            if (GrayboxGameFlow.Instance != null) Sync(GrayboxGameFlow.Instance.State);
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
            _runEnd.Populate(GrayboxGameFlow.Instance);
            SetOpen(_runEnd.Screen, true);
        }

        private static void SetOpen(UiScreen screen, bool open)
        {
            if (screen.gameObject.activeSelf != open) screen.gameObject.SetActive(open);
        }
    }
}
