using System.Collections.Generic;
using HoldTheHill.Core.UiKit;
using TMPro;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The achievements window over the field: how many are earned, and a line per achievement
    /// with its progress. Opened with the achievements key (<see cref="GrayboxRunHotkeys"/>).
    /// Replaces the IMGUI modal; the Home hub shows the same list between runs.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Graybox/HUD/Achievements Panel")]
    public class GrayboxAchievementsPanel : GrayboxHudPanel
    {
        [SerializeField] private TMP_Text _summary;
        [SerializeField] private UiButton _close;

        [Tooltip("An inactive line copied once per achievement.")]
        [SerializeField] private TMP_Text _rowTemplate;

        [SerializeField] private RectTransform _rowsRoot;

        private readonly List<TMP_Text> _rows = new List<TMP_Text>();
        private readonly List<int> _progressShown = new List<int>();
        private readonly List<bool> _unlockedShown = new List<bool>();
        private GrayboxAchievements _builtFor;
        private int _earnedShown = -1;

        /// <summary>True while the window is up. Toggled by the achievements key.</summary>
        public static bool Open { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Open = false;

        public override bool WantsToShow => Open && GrayboxAchievements.Instance != null;

        public int RowCount => _rows.Count;

        public string Summary => _summary.text;

        private void Awake()
        {
            OnClick(_close.Button, () => Open = false);
        }

        public override void Refresh()
        {
            GrayboxAchievements achievements = GrayboxAchievements.Instance;
            IReadOnlyList<Achievement> all = achievements.Achievements;
            if (achievements != _builtFor || _rows.Count != all.Count)
            {
                Rebuild(achievements, all.Count);
            }

            int earned = 0;
            for (int i = 0; i < all.Count; i++)
            {
                Achievement a = all[i];
                if (a.IsUnlocked) earned++;

                if (a.IsUnlocked != _unlockedShown[i] || a.CurrentProgress != _progressShown[i])
                {
                    _unlockedShown[i] = a.IsUnlocked;
                    _progressShown[i] = a.CurrentProgress;
                    string state = a.IsUnlocked ? $"earned {a.UnlockedTimeStr}" : $"{a.CurrentProgress}/{a.RequiredProgress}";
                    _rows[i].text = $"{a.Title}  <size=80%>{state}</size>\n<size=80%>{a.Description}</size>";
                    _rows[i].color = a.IsUnlocked ? (Color)UiKitStyle.Gold : (Color)UiKitStyle.Cream;
                }
            }

            if (earned != _earnedShown)
            {
                _earnedShown = earned;
                _summary.text = $"{earned} of {all.Count} earned";
            }
        }

        private void Rebuild(GrayboxAchievements achievements, int count)
        {
            foreach (TMP_Text row in _rows)
            {
                Destroy(row.gameObject);
            }

            _rows.Clear();
            _progressShown.Clear();
            _unlockedShown.Clear();
            _builtFor = achievements;
            _earnedShown = -1;

            for (int i = 0; i < count; i++)
            {
                TMP_Text row = Instantiate(_rowTemplate, _rowsRoot);
                row.gameObject.SetActive(true);
                _rows.Add(row);
                _progressShown.Add(-1);
                _unlockedShown.Add(false);
            }
        }
    }
}
