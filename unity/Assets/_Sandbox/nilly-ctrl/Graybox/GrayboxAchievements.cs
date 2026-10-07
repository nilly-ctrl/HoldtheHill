using System;
using System.Collections.Generic;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Towers;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    [Serializable]
    public class Achievement
    {
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

            InitDefaultAchievements();
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

        private void InitDefaultAchievements()
        {
            _achievements.Clear();
            _achievements.Add(new Achievement { Id = "first_blood", Title = "First Blood", Description = "Defeat your first enemy worker/grunt.", IconName = "AchFirstBloodIcon", RequiredProgress = 1 });
            _achievements.Add(new Achievement { Id = "colony_defender", Title = "Colony Defender", Description = "Defeat 50 invading enemies.", IconName = "AchColonyDefenderIcon", RequiredProgress = 50 });
            _achievements.Add(new Achievement { Id = "ant_terminator", Title = "Ant Terminator", Description = "Defeat 200 total enemies.", IconName = "AchAntTerminatorIcon", RequiredProgress = 200 });
            _achievements.Add(new Achievement { Id = "gold_tycoon", Title = "Gold Tycoon", Description = "Accumulate $1,000 total Gold in treasury.", IconName = "AchGoldTycoonIcon", RequiredProgress = 1000 });
            _achievements.Add(new Achievement { Id = "architect", Title = "Master Architect", Description = "Build or upgrade 10 defense towers.", IconName = "AchArchitectIcon", RequiredProgress = 10 });
            _achievements.Add(new Achievement { Id = "boss_slayer", Title = "Boss Slayer", Description = "Defeat a Wave 5 or Wave 10 Boss Horde.", IconName = "AchBossSlayerIcon", RequiredProgress = 1 });
            _achievements.Add(new Achievement { Id = "wave_survivor", Title = "Endless Survivor", Description = "Reach Procedural Wave 8.", IconName = "AchWaveSurvivorIcon", RequiredProgress = 8 });
            _achievements.Add(new Achievement { Id = "commander", Title = "Supreme Commander", Description = "Spend 5 Skill Points in the Commander Tree.", IconName = "AchCommanderIcon", RequiredProgress = 5 });
            _achievements.Add(new Achievement { Id = "mine_master", Title = "Minefield Master", Description = "Detonate 10 Proximity Landmines.", IconName = "AchMineMasterIcon", RequiredProgress = 10 });
            _achievements.Add(new Achievement { Id = "fortress", Title = "Impenetrable Fortress", Description = "Complete 3 waves with 100% Base HP.", IconName = "AchFortressIcon", RequiredProgress = 3 });
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
            AddProgress("first_blood", 1);
            AddProgress("colony_defender", 1);
            AddProgress("ant_terminator", 1);
        }

        // ProximityMine has no static event; the mine animator announces each blast here.
        private void OnMineDetonated(Vector3 at)
        {
            AddProgress("mine_master", 1);
        }

        public void ReportGoldEarned(int amount)
        {
            int currentGold = GrayboxEconomy.Instance != null ? GrayboxEconomy.Instance.CurrentGold : 0;
            if (currentGold >= 1000)
            {
                AddProgress("gold_tycoon", 1000);
            }
        }

        public void ReportTowerBuilt()
        {
            AddProgress("architect", 1);
        }

        public void ReportSkillPointSpent()
        {
            AddProgress("commander", 1);
        }

        public void ReportWaveCompleted(int waveNumber)
        {
            if (waveNumber % 5 == 0)
            {
                AddProgress("boss_slayer", 1);
            }
            if (waveNumber >= 8)
            {
                AddProgress("wave_survivor", waveNumber);
            }

            if (GrayboxBaseHealth.Instance != null && GrayboxBaseHealth.Instance.CurrentHealth >= 100)
            {
                AddProgress("fortress", 1);
            }
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
            Keyboard keyboard = Keyboard.current;
            if (GrayboxGameFlow.GameplayActive && keyboard != null && keyboard.aKey.wasPressedThisFrame)
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
