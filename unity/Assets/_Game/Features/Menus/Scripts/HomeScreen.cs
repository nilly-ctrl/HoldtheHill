using HoldTheHill.Core.Input;
using HoldTheHill.Core.Localization;
using HoldTheHill.Core.Save;
using HoldTheHill.Features.GameFlow;
using HoldTheHill.Features.Progression;
using HoldTheHill.Features.DamageNumbers;
using System;
using System.Collections.Generic;
using HoldTheHill.Core.UiKit;
using TMPro;
using UnityEngine;

namespace HoldTheHill.Features.Menus
{
    /// <summary>
    /// The hub between runs. Four tabs: Play (campaign or endless), Upgrades (the permanent shop,
    /// paid in Honeydew), Awards (achievements) and Records.
    /// </summary>
    [RequireComponent(typeof(UiScreen))]
    [AddComponentMenu("Hold the Hill/Home Screen")]
    public class HomeScreen : MonoBehaviour
    {
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

        private readonly List<LevelList.Level> _levels = new List<LevelList.Level>();
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

        private RunFlow _flow;

        private RunFlow Flow
        {
            get
            {
                if (_flow == null) _flow = FindAnyObjectByType<RunFlow>();
                return _flow;
            }
        }

        private RunMode SelectedMode => _mode.Index == 1 ? RunMode.Endless : RunMode.Campaign;

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
            MetaProgress.CurrencyChanged += OnCurrencyChanged;
            if (Flow != null)
            {
                string[] modes = { Loc.Get("Campaign"), Loc.Get("Endless") };
                _mode.SetOptions(modes, Flow.Mode == RunMode.Endless ? 1 : 0);
            }


            ShowLevels();
            Refresh();
        }

        private void OnDisable() => MetaProgress.CurrencyChanged -= OnCurrencyChanged;

        private void OnCurrencyChanged(int balance) => Refresh();

        /// <summary>Fills every tab from the save data.</summary>
        public void Refresh()
        {
            _honeydew.text = $"{PixelGlyphs.IconGem} {MetaProgress.Currency} {PermanentUpgrades.CurrencyName}";
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
            _levels.AddRange(LevelList.Available());
            if (here != null && _levels.FindIndex(l => l.Id == here) < 0)
            {
                _levels.Insert(0, new LevelList.Level { Id = here, Name = here });
            }

            var names = new List<string>();
            foreach (LevelList.Level level in _levels) names.Add(level.Name);
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
                LevelList.LoadAndPlay(_levels[_level.Index], SelectedMode);
            }
        }

        private void RefreshPlay()
        {
            LevelRecord record = Level(SelectedMode);
            if (record == null || record.bestWave <= 0)
            {
                _best.text = SelectedMode == RunMode.Endless
                    ? Loc.Get("Waves keep coming until the hill falls.")
                    : Loc.Get("Hold the hill through every wave.");
            }
            else
            {
                _best.text = Loc.Get("Best: {0}", BestWave(record));
            }
        }

        private void RefreshUpgrades()
        {
            foreach (UpgradeRow row in _upgrades)
            {
                PermanentUpgrades.Upgrade upgrade = PermanentUpgrades.Find(row.Id);
                if (upgrade == null) continue;

                int level = PermanentUpgrades.Level(upgrade);
                int cost = PermanentUpgrades.NextCost(upgrade);
                bool maxed = cost < 0;

                row.Name.text = $"{upgrade.Name}  {level}/{upgrade.MaxLevel}";
                row.Effect.text = maxed
                    ? PermanentUpgrades.Describe(upgrade, level)
                    : Loc.Get("Next: {0}", PermanentUpgrades.Describe(upgrade, level + 1));
                row.Buy.Label = maxed ? Loc.Get("Owned") : Loc.Get("Buy {0}{1}", PixelGlyphs.IconGem, cost);
                row.Buy.Interactable = !maxed && MetaProgress.Currency >= cost;
            }
        }

        private void RefreshAwards()
        {
            IReadOnlyList<Achievement> all = AchievementTracker.Instance != null ? AchievementTracker.Instance.Achievements : null;
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

            _awardCount.text = all == null ? Loc.Get("No achievements here.") : Loc.Get("{0} of {1} earned", unlocked, all.Count);
        }

        private void RefreshRecords()
        {
            RecordsSave records = SaveStore.Data.records;
            _runs.text = records.runsFinished.ToString();
            _wins.text = records.victories.ToString();
            _kills.text = records.totalKills.ToString();
            _food.text = records.totalFoodEarned.ToString();
            _time.text = FormatTime(records.totalPlaySeconds);

            LevelRecord campaign = Level(RunMode.Campaign);
            LevelRecord endless = Level(RunMode.Endless);
            _bestCampaign.text = campaign != null && campaign.bestWave > 0 ? BestWave(campaign) : BindingText.None;
            _fastestWin.text = campaign != null && campaign.fastestVictorySeconds > 0f ? FormatTime(campaign.fastestVictorySeconds) : BindingText.None;
            _bestEndless.text = endless != null && endless.bestWave > 0 ? BestWave(endless) : BindingText.None;
        }

        private void Hook()
        {
            if (_hooked) return;
            _hooked = true;

            // Back from the hub is the title screen.
            Screen.CloseOnBack = false;
            Screen.BackPressed += GoToTitle;

            _mode.IndexChanged.AddListener(_ => RefreshPlay());
            _level.IndexChanged.AddListener(_ =>
            {
                RefreshPlay();
                RefreshRecords();
            });
            _start.Clicked.AddListener(StartRun);
            _toTitle.Clicked.AddListener(GoToTitle);

            foreach (UpgradeRow row in _upgrades)
            {
                UpgradeRow captured = row;
                row.Buy.Clicked.AddListener(() =>
                {
                    PermanentUpgrades.Upgrade upgrade = PermanentUpgrades.Find(captured.Id);
                    // A purchase raises CurrencyChanged, which refreshes the screen.
                    if (upgrade == null || !PermanentUpgrades.TryBuyNext(upgrade)) UiFeedback.Raise(UiCue.Denied);
                });
            }
        }

        // The record of the level chosen on the Play tab.
        private LevelRecord Level(RunMode mode)
        {
            return SaveStore.Data.FindLevel(LevelList.RecordId(SelectedLevelId, mode));
        }

        private static string BestWave(LevelRecord record)
        {
            if (record.bestWaveRetries <= 0) return Loc.Get("wave {0}", record.bestWave);
            if (record.bestWaveRetries == 1) return Loc.Get("wave {0} ({1} retry)", record.bestWave, record.bestWaveRetries);
            return Loc.Get("wave {0} ({1} retries)", record.bestWave, record.bestWaveRetries);
        }

        private void GoToTitle()
        {
            if (Flow != null) Flow.GoToTitle();
        }

        private static string FormatTime(float seconds)
        {
            int total = Mathf.FloorToInt(seconds);
            return total >= 3600 ? $"{total / 3600}:{total / 60 % 60:00}:{total % 60:00}" : $"{total / 60}:{total % 60:00}";
        }
    }
}
