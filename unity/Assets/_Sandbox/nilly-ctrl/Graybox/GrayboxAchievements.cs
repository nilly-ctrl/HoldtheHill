using System;
using System.Collections.Generic;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Towers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>An achievement in play: its data asset and how far the player has got.</summary>
    [Serializable]
    public class Achievement
    {
        public Achievement(GrayboxAchievementData data)
        {
            Data = data;
            Id = data.Id;
            Title = data.Title;
            Description = data.Description;
            IconName = data.IconName;
            RequiredProgress = data.RequiredProgress;
        }

        public GrayboxAchievementData Data { get; }
        public string Id;
        public string Title;
        public string Description;
        public string IconName;
        public int RequiredProgress;
        public int CurrentProgress;
        public bool IsUnlocked;
        public string UnlockedTimeStr;
    }

    /// <summary>
    /// Achievement tracking system for the graybox sandbox environment.
    /// Monitors combat events, gold earnings, wave milestones, and tower placement.
    /// Provides an IMGUI Achievements modal and pop-up banners upon unlock.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Graybox/Graybox Achievements")]
    public class GrayboxAchievements : MonoBehaviour
    {
        public static GrayboxAchievements Instance { get; private set; }

        public bool ShowAchievementsWindow { get; set; }

        [Tooltip("The achievements in the game.")]
        [SerializeField] private GrayboxAchievementCatalog _catalog;

        private readonly List<Achievement> _achievements = new List<Achievement>();
        private readonly Queue<Achievement> _unlockBannerQueue = new Queue<Achievement>();
        private float _bannerTimer;
        private Achievement _currentBanner;

        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _btnStyle;

        public IReadOnlyList<Achievement> Achievements => _achievements;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitAchievements();
            LoadSaved();
        }

        /// <summary>Swaps the achievement set. Progress is read again from the save, if one is open.</summary>
        public void Configure(GrayboxAchievementCatalog catalog)
        {
            _catalog = catalog;
            _unlockBannerQueue.Clear();
            InitAchievements();
            LoadSaved();
        }

        // Achievements are lifetime, so progress comes from the save file when one is attached.
        // Without one (tests, a scene with no GrayboxSaveHost) they start empty every time.
        private void LoadSaved()
        {
            if (!GrayboxSave.IsOpen) return;

            foreach (Achievement ach in _achievements)
            {
                AchievementSave saved = GrayboxSave.Data.FindAchievement(ach.Id);
                ach.CurrentProgress = saved != null ? Mathf.Min(saved.progress, ach.RequiredProgress) : 0;
                ach.IsUnlocked = saved != null && saved.unlocked;
                ach.UnlockedTimeStr = saved != null ? saved.unlockedAt : null;
            }
        }

        private static void Persist(Achievement ach)
        {
            if (!GrayboxSave.IsOpen) return;

            AchievementSave saved = GrayboxSave.Data.FindAchievement(ach.Id);
            if (saved == null)
            {
                saved = new AchievementSave { id = ach.Id };
                GrayboxSave.Data.achievements.Add(saved);
            }

            saved.progress = ach.CurrentProgress;
            saved.unlocked = ach.IsUnlocked;
            saved.unlockedAt = ach.UnlockedTimeStr ?? string.Empty;
            GrayboxSave.MarkDirty();
        }

        private void InitAchievements()
        {
            _achievements.Clear();
            if (_catalog == null)
            {
                return;
            }

            foreach (GrayboxAchievementData data in _catalog.Achievements)
            {
                if (data != null && !_achievements.Exists(a => a.Id == data.Id))
                {
                    _achievements.Add(new Achievement(data));
                }
            }
        }

        private void OnEnable()
        {
            EnemyHealth.Defeated += OnEnemyDefeated;
            GrayboxFeedback.MineExploded += OnMineDetonated;
            GrayboxSave.Loaded += LoadSaved;
        }

        private void OnDisable()
        {
            EnemyHealth.Defeated -= OnEnemyDefeated;
            GrayboxFeedback.MineExploded -= OnMineDetonated;
            GrayboxSave.Loaded -= LoadSaved;
        }

        private void OnEnemyDefeated(GameObject enemy)
        {
            Report(AchievementTrigger.EnemyDefeated);
        }

        // ProximityMine has no static event; the mine animator announces each blast here.
        private void OnMineDetonated(Vector3 at)
        {
            Report(AchievementTrigger.MineDetonated);
        }

        public void ReportGoldEarned(int amount)
        {
            int currentGold = GrayboxEconomy.Instance != null ? GrayboxEconomy.Instance.CurrentGold : 0;
            Report(AchievementTrigger.GoldHeld, currentGold);
        }

        public void ReportTowerBuilt()
        {
            Report(AchievementTrigger.TowerBuilt);
        }

        public void ReportSkillPointSpent()
        {
            Report(AchievementTrigger.SkillPointSpent);
        }

        public void ReportWaveCompleted(int waveNumber)
        {
            Report(AchievementTrigger.WaveCompleted, waveNumber);
            Report(AchievementTrigger.WaveReached, waveNumber);
        }

        /// <summary>Moves every achievement that listens for this trigger forward.</summary>
        /// <param name="value">What the trigger reports: the gold held, or the wave number. Unused by the others.</param>
        private void Report(AchievementTrigger trigger, int value = 1)
        {
            foreach (Achievement ach in _achievements)
            {
                GrayboxAchievementData data = ach.Data;
                if (data.Trigger != trigger || ach.IsUnlocked)
                {
                    continue;
                }

                switch (trigger)
                {
                    case AchievementTrigger.GoldHeld:
                    case AchievementTrigger.WaveReached:
                        // Progress is the best value seen, not a running count.
                        if (value > ach.CurrentProgress)
                        {
                            AddProgress(ach.Id, value - ach.CurrentProgress);
                        }

                        break;
                    case AchievementTrigger.WaveCompleted:
                        if (WaveCounts(data, value))
                        {
                            AddProgress(ach.Id, 1);
                        }

                        break;
                    default:
                        AddProgress(ach.Id, 1);
                        break;
                }
            }
        }

        private static bool WaveCounts(GrayboxAchievementData data, int waveNumber)
        {
            if (data.EveryNthWave > 0 && waveNumber % data.EveryNthWave != 0)
            {
                return false;
            }

            if (data.RequireFullBaseHealth)
            {
                GrayboxBaseHealth hill = GrayboxBaseHealth.Instance;
                return hill != null && hill.CurrentHealth >= hill.MaxHealth;
            }

            return true;
        }

        public void AddProgress(string id, int amount)
        {
            Achievement ach = _achievements.Find(a => a.Id == id);
            if (ach == null || ach.IsUnlocked) return;

            ach.CurrentProgress = Mathf.Min(ach.RequiredProgress, ach.CurrentProgress + amount);
            if (ach.CurrentProgress >= ach.RequiredProgress)
            {
                Unlock(ach);
            }

            // Progress is written with the next save; an unlock is written straight away.
            Persist(ach);
            if (ach.IsUnlocked) GrayboxSave.Save();
        }

        private void Unlock(Achievement ach)
        {
            if (ach.IsUnlocked) return;

            ach.IsUnlocked = true;
            ach.UnlockedTimeStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            _unlockBannerQueue.Enqueue(ach);

            GrayboxSfx.PlayCue("Upgrade", Vector3.zero);
            Debug.Log($"[Achievement Unlocked] 🏆 {ach.Title}: {ach.Description}");
        }

        private void Update()
        {
            if (GrayboxGameFlow.GameplayActive && GrayboxControls.Pressed(GrayboxControls.Achievements))
            {
                ShowAchievementsWindow = !ShowAchievementsWindow;
            }

            if (_bannerTimer > 0f)
            {
                _bannerTimer -= Time.unscaledDeltaTime;
            }
            else if (_unlockBannerQueue.Count > 0)
            {
                _currentBanner = _unlockBannerQueue.Dequeue();
                _bannerTimer = 3.5f;
            }
        }

        private void OnGUI()
        {
            if (!GrayboxGameFlow.GameplayActive) return;

            HoldTheHill.Sandbox.NillyCtrl.GrayboxUi.Apply(); // pixel skin; a no-op without a GrayboxUi in the scene
            InitStyles();

            DrawUnlockBanner();
            DrawAchievementsModal();
        }

        private void InitStyles()
        {
            if (_titleStyle != null) return;

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                richText = true
            };

            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                richText = true
            };

            _btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold
            };
        }

        private void DrawUnlockBanner()
        {
            if (_bannerTimer <= 0f || _currentBanner == null) return;

            float width = 320f;
            float height = 55f;
            float x = (Screen.width - width) * 0.5f;
            float y = 45f;

            GUI.color = new Color(0.1f, 0.12f, 0.16f, 0.95f);
            GUI.Box(new Rect(x, y, width, height), GUIContent.none);
            GUI.color = Color.white;

            GUILayout.BeginArea(new Rect(x + 10f, y + 6f, width - 20f, height - 12f));
            GUILayout.Label($"<b>🏆 ACHIEVEMENT UNLOCKED!</b>", _headerStyle);
            GUILayout.Label($"<color=#ffd700>{_currentBanner.Title}</color> — {_currentBanner.Description}", _bodyStyle);
            GUILayout.EndArea();
        }

        private void DrawAchievementsModal()
        {
            if (!ShowAchievementsWindow) return;

            float width = 450f;
            float height = 480f;
            float x = (Screen.width - width) * 0.5f;
            float y = (Screen.height - height) * 0.5f;

            GUI.Box(new Rect(x, y, width, height), GUIContent.none);

            GUILayout.BeginArea(new Rect(x + 12f, y + 12f, width - 24f, height - 24f));
            GUILayout.Label("<b>🏆 ACHIEVEMENTS & MILESTONES</b>", _titleStyle);
            GUILayout.Space(6);

            int unlockedCount = 0;
            foreach (var a in _achievements) if (a.IsUnlocked) unlockedCount++;
            GUILayout.Label($"Unlocked: <color=#7fdd7f>{unlockedCount}</color> / {_achievements.Count} ({Mathf.RoundToInt((float)unlockedCount / _achievements.Count * 100f)}%)", _headerStyle);
            GUILayout.Space(8);

            for (int i = 0; i < _achievements.Count; i++)
            {
                Achievement a = _achievements[i];
                GUILayout.BeginHorizontal(GUI.skin.box);

                string iconStr = a.IsUnlocked ? "🏆" : "🔒";
                string colorHex = a.IsUnlocked ? "#ffd700" : "#808080";
                string statusStr = a.IsUnlocked ? $"<color=#7fdd7f>[UNLOCKED {a.UnlockedTimeStr}]</color>" : $"[{a.CurrentProgress}/{a.RequiredProgress}]";

                GUILayout.Label($"{iconStr} <b><color={colorHex}>{a.Title}</color></b>", _headerStyle, GUILayout.Width(180));
                GUILayout.Label($"{a.Description}\n{statusStr}", _bodyStyle);

                GUILayout.EndHorizontal();
                GUILayout.Space(2);
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Close Window [A]", _btnStyle, GUILayout.Height(28)))
            {
                ShowAchievementsWindow = false;
            }

            GUILayout.EndArea();
        }
    }
}
