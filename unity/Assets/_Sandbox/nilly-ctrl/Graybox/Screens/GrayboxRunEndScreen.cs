using HoldTheHill.Sandbox.UiKit;
using TMPro;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Shown when a run ends, in defeat or victory: the run's numbers, whether it set a record,
    /// and what to do next. Victory uses the same layout without "retry wave".
    /// </summary>
    [RequireComponent(typeof(UiScreen))]
    [AddComponentMenu("Hold the Hill/Graybox/Graybox Run End Screen")]
    public class GrayboxRunEndScreen : MonoBehaviour
    {
        private static readonly Color32 DefeatColour = new Color32(0xff, 0x6b, 0x5a, 0xff);
        private static readonly Color32 VictoryColour = new Color32(0x8f, 0xe0, 0x6a, 0xff);

        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _newBest;
        [SerializeField] private TMP_Text _wave;
        [SerializeField] private TMP_Text _kills;
        [SerializeField] private TMP_Text _food;
        [SerializeField] private TMP_Text _towers;
        [SerializeField] private TMP_Text _time;
        [SerializeField] private TMP_Text _retries;
        [SerializeField] private TMP_Text _best;
        [SerializeField] private TMP_Text _honeydew;
        [SerializeField] private UiButton _retryWave;
        [SerializeField] private UiButton _restart;
        [SerializeField] private UiButton _quit;

        private UiScreen _screen;
        private bool _hooked;

        public UiScreen Screen => _screen != null ? _screen : _screen = GetComponent<UiScreen>();
        public UiButton RetryWaveButton => _retryWave;
        public UiButton RestartButton => _restart;
        public UiButton QuitButton => _quit;
        public TMP_Text Title => _title;
        public TMP_Text NewBest => _newBest;
        public TMP_Text Wave => _wave;
        public TMP_Text Best => _best;
        public TMP_Text Honeydew => _honeydew;

        private static GrayboxGameFlow Flow => GrayboxGameFlow.Instance;

        /// <summary>Fills the screen from the run that just ended. Call before showing it.</summary>
        public void Populate(GrayboxGameFlow flow)
        {
            Hook();
            if (flow == null) return;

            GrayboxRunStats stats = flow.Stats;
            RunRecordResult record = flow.LastRunResult;
            bool victory = flow.LastRunWasVictory;

            _title.text = victory ? "Hill defended" : "Hill overrun";
            _title.color = victory ? VictoryColour : DefeatColour;

            _newBest.gameObject.SetActive(record.NewBestWave || record.NewFastestVictory);
            _newBest.text = record.NewFastestVictory && !record.NewBestWave ? "Fastest win!" : "New best!";

            // An endless run has no last wave to count towards.
            _wave.text = stats.TotalWaves > 0 ? $"{stats.WaveReached} of {stats.TotalWaves}" : stats.WaveReached.ToString();
            _honeydew.text = $"{PixelGlyphs.IconGem} {flow.LastRunHoneydew}";
            _kills.text = stats.Kills.ToString();
            _food.text = stats.GoldEarned.ToString();
            _towers.text = stats.TowersStanding.ToString();
            _time.text = FormatTime(stats.TimeSeconds);
            _retries.text = stats.WaveRetries.ToString();
            _best.text = record.BestWaveRetries > 0
                ? $"Wave {record.BestWave} ({record.BestWaveRetries} {(record.BestWaveRetries == 1 ? "retry" : "retries")})"
                : $"Wave {record.BestWave}";

            // Nothing to retry after a win, or before any wave has started.
            bool canRetry = !victory && flow.CanRetryWave;
            _retryWave.gameObject.SetActive(canRetry);
            if (canRetry) _retryWave.Label = $"Retry wave {flow.RetryWaveNumber}";

            Screen.FirstSelected = canRetry ? _retryWave.Button : _restart.Button;
        }

        private void Awake() => Hook();

        private void Hook()
        {
            if (_hooked) return;
            _hooked = true;

            // A finished run has nowhere to go "back" to; one of the three buttons must be chosen.
            Screen.CloseOnBack = false;

            _retryWave.Clicked.AddListener(() => Flow?.RetryWave());
            _restart.Clicked.AddListener(() => Flow?.StartNewRun());
            _quit.Clicked.AddListener(() => Flow?.QuitToHome());
        }

        private static string FormatTime(float seconds)
        {
            int total = Mathf.FloorToInt(seconds);
            return $"{total / 60}:{total % 60:00}";
        }
    }
}
