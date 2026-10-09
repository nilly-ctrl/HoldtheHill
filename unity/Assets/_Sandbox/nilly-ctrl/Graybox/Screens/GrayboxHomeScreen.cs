using System;
using System.Collections.Generic;
using HoldTheHill.Sandbox.UiKit;
using TMPro;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The hub between runs. Four tabs: Play (campaign or endless), Upgrades (the permanent shop,
    /// paid in Honeydew), Awards (achievements) and Records.
    /// </summary>
    [RequireComponent(typeof(UiScreen))]
    [AddComponentMenu("Hold the Hill/Graybox/Graybox Home Screen")]
    public class GrayboxHomeScreen : MonoBehaviour
    {
        private static readonly string[] Modes = { "Campaign", "Endless" };

        [Serializable]
        public class UpgradeRow
        {
            public string Id;
            public TMP_Text Name;
            public TMP_Text Effect;
            public UiButton Buy;
        }

        [Serializable]
        public class TextRow
        {
            public GameObject Root;
            public TMP_Text Label;
            public TMP_Text Value;
        }

        [SerializeField] private TMP_Text _honeydew;

        [Header("Play")]
        [SerializeField] private UiStepper _level;
        [SerializeField] private UiStepper _mode;
        [SerializeField] private TMP_Text _best;
        [SerializeField] private UiButton _start;
        [SerializeField] private UiButton _toTitle;

        [Header("Upgrades")]
        [SerializeField] private List<UpgradeRow> _upgrades = new List<UpgradeRow>();

        [Header("Awards")]
        [SerializeField] private TMP_Text _awardCount;
        [SerializeField] private List<TextRow> _awards = new List<TextRow>();

        [Header("Records")]
        [SerializeField] private TMP_Text _runs;
        [SerializeField] private TMP_Text _wins;
        [SerializeField] private TMP_Text _kills;
        [SerializeField] private TMP_Text _food;
        [SerializeField] private TMP_Text _time;
        [SerializeField] private TMP_Text _bestCampaign;
        [SerializeField] private TMP_Text _fastestWin;
        [SerializeField] private TMP_Text _bestEndless;

        private readonly List<GrayboxLevels.Level> _levels = new List<GrayboxLevels.Level>();
        private UiScreen _screen;
        private bool _hooked;

        public UiScreen Screen => _screen != null ? _screen : _screen = GetComponent<UiScreen>();
        public UiStepper Mode => _mode;
        public UiStepper LevelStepper => _level;
        public UiButton StartButton => _start;
        public UiButton TitleButton => _toTitle;
        public TMP_Text Honeydew => _honeydew;
        public TMP_Text Best => _best;
        public IReadOnlyList<UpgradeRow> Upgrades => _upgrades;
        public IReadOnlyList<TextRow> Awards => _awards;
        public TMP_Text BestCampaign => _bestCampaign;

        private static GrayboxGameFlow Flow => GrayboxGameFlow.Instance;

        private GrayboxRunMode SelectedMode => _mode.Index == 1 ? GrayboxRunMode.Endless : GrayboxRunMode.Campaign;

        /// <summary>The id of the level chosen on the Play tab; the scene's own when there is no choice.</summary>
        public string SelectedLevelId
        {
            get
            {
                if (_level.Index >= 0 && _level.Index < _levels.Count) return _levels[_level.Index].Id;
                return Flow != null ? Flow.MapId : "Unknown";
            }
        }

        private void OnEnable()
        {
            Hook();
            GrayboxMetaProgress.CurrencyChanged += OnCurrencyChanged;
            if (Flow != null) _mode.SetOptions(Modes, Flow.Mode == GrayboxRunMode.Endless ? 1 : 0);
            ShowLevels();
            Refresh();
        }

        private void OnDisable() => GrayboxMetaProgress.CurrencyChanged -= OnCurrencyChanged;

        private void OnCurrencyChanged(int balance) => Refresh();

        /// <summary>Fills every tab from the save data.</summary>
        public void Refresh()
        {
            _honeydew.text = $"{PixelGlyphs.IconGem} {GrayboxMetaProgress.Currency} {GrayboxUpgrades.CurrencyName}";
            RefreshPlay();
            RefreshUpgrades();
            RefreshAwards();
            RefreshRecords();
        }

        // The levels that can be loaded, with the one this scene is chosen. A scene that is not in
        // the catalogue (a test scene) is listed under its map id so the row is never empty.
        private void ShowLevels()
        {
            string here = Flow != null ? Flow.MapId : null;
            _levels.Clear();
            _levels.AddRange(GrayboxLevels.Available());
            if (here != null && _levels.FindIndex(l => l.Id == here) < 0)
            {
                _levels.Insert(0, new GrayboxLevels.Level { Id = here, Name = here });
            }

            var names = new List<string>();
            foreach (GrayboxLevels.Level level in _levels) names.Add(level.Name);
            _level.SetOptions(names, Mathf.Max(0, _levels.FindIndex(l => l.Id == here)));
        }

        private void StartRun()
        {
            if (Flow == null) return;

            string chosen = SelectedLevelId;
            if (chosen == Flow.MapId)
            {
                Flow.StartNewRun(SelectedMode);
            }
            else
            {
                GrayboxLevels.LoadAndPlay(_levels[_level.Index], SelectedMode);
            }
        }

        private void RefreshPlay()
        {
            LevelRecord record = Level(SelectedMode);
            if (record == null || record.bestWave <= 0)
            {
                _best.text = SelectedMode == GrayboxRunMode.Endless ? "Waves keep coming until the hill falls." : "Hold the hill through every wave.";
            }
            else
            {
                _best.text = "Best: " + BestWave(record);
            }
        }

        private void RefreshUpgrades()
        {
            foreach (UpgradeRow row in _upgrades)
            {
                GrayboxUpgrades.Upgrade upgrade = GrayboxUpgrades.Find(row.Id);
                if (upgrade == null) continue;

                int level = GrayboxUpgrades.Level(upgrade);
                int cost = GrayboxUpgrades.NextCost(upgrade);
                bool maxed = cost < 0;

                row.Name.text = $"{upgrade.Name}  {level}/{upgrade.MaxLevel}";
                row.Effect.text = maxed
                    ? GrayboxUpgrades.Describe(upgrade, level)
                    : "Next: " + GrayboxUpgrades.Describe(upgrade, level + 1);
                row.Buy.Label = maxed ? "Owned" : $"Buy {PixelGlyphs.IconGem}{cost}";
                row.Buy.Interactable = !maxed && GrayboxMetaProgress.Currency >= cost;
            }
        }

        private void RefreshAwards()
        {
            IReadOnlyList<Achievement> all = GrayboxAchievements.Instance != null ? GrayboxAchievements.Instance.Achievements : null;
            int unlocked = 0;

            for (int i = 0; i < _awards.Count; i++)
            {
                TextRow row = _awards[i];
                bool used = all != null && i < all.Count;
                row.Root.SetActive(used);
                if (!used) continue;

                Achievement a = all[i];
                if (a.IsUnlocked) unlocked++;
                row.Label.text = a.Title;
                row.Label.color = a.IsUnlocked ? (Color)UiKitStyle.Gold : (Color)UiKitStyle.Cream;
                row.Value.text = a.IsUnlocked ? PixelGlyphs.Check : $"{Mathf.Min(a.CurrentProgress, a.RequiredProgress)}/{a.RequiredProgress}";
                row.Value.color = row.Label.color;
            }

            _awardCount.text = all == null ? "No achievements here." : $"{unlocked} of {all.Count} earned";
        }

        private void RefreshRecords()
        {
            RecordsSave records = GrayboxSave.Data.records;
            _runs.text = records.runsFinished.ToString();
            _wins.text = records.victories.ToString();
            _kills.text = records.totalKills.ToString();
            _food.text = records.totalFoodEarned.ToString();
            _time.text = FormatTime(records.totalPlaySeconds);

            LevelRecord campaign = Level(GrayboxRunMode.Campaign);
            LevelRecord endless = Level(GrayboxRunMode.Endless);
            _bestCampaign.text = campaign != null && campaign.bestWave > 0 ? BestWave(campaign) : GrayboxBindingText.None;
            _fastestWin.text = campaign != null && campaign.fastestVictorySeconds > 0f ? FormatTime(campaign.fastestVictorySeconds) : GrayboxBindingText.None;
            _bestEndless.text = endless != null && endless.bestWave > 0 ? BestWave(endless) : GrayboxBindingText.None;
        }

        private void Hook()
        {
            if (_hooked) return;
            _hooked = true;

            // Back from the hub is the title screen.
            Screen.CloseOnBack = false;
            Screen.BackPressed += () => Flow?.GoToTitle();

            _mode.IndexChanged.AddListener(_ => RefreshPlay());
            _level.IndexChanged.AddListener(_ =>
            {
                RefreshPlay();
                RefreshRecords();
            });
            _start.Clicked.AddListener(StartRun);
            _toTitle.Clicked.AddListener(() => Flow?.GoToTitle());

            foreach (UpgradeRow row in _upgrades)
            {
                UpgradeRow captured = row;
                row.Buy.Clicked.AddListener(() =>
                {
                    GrayboxUpgrades.Upgrade upgrade = GrayboxUpgrades.Find(captured.Id);
                    // A purchase raises CurrencyChanged, which refreshes the screen.
                    if (upgrade == null || !GrayboxUpgrades.TryBuyNext(upgrade)) UiFeedback.Raise(UiCue.Denied);
                });
            }
        }

        // The record of the level chosen on the Play tab.
        private LevelRecord Level(GrayboxRunMode mode)
        {
            return GrayboxSave.Data.FindLevel(GrayboxLevels.RecordId(SelectedLevelId, mode));
        }

        private static string BestWave(LevelRecord record)
        {
            return record.bestWaveRetries > 0
                ? $"wave {record.bestWave} ({record.bestWaveRetries} {(record.bestWaveRetries == 1 ? "retry" : "retries")})"
                : $"wave {record.bestWave}";
        }

        private static string FormatTime(float seconds)
        {
            int total = Mathf.FloorToInt(seconds);
            return total >= 3600 ? $"{total / 3600}:{total / 60 % 60:00}:{total % 60:00}" : $"{total / 60}:{total % 60:00}";
        }
    }
}
