using System;
using System.Collections.Generic;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Everything that outlives a play session, written as one JSON file by <see cref="GrayboxSave"/>.
    /// </summary>
    /// <remarks>
    /// Fields, not properties: JsonUtility only writes fields. Adding a field is safe (old files
    /// load with its default). Renaming or removing one needs a bump of
    /// <see cref="GrayboxSave.CurrentVersion"/> and a step in <c>GrayboxSave.Migrate</c>.
    /// </remarks>
    [Serializable]
    public class GrayboxSaveData
    {
        public int version = GrayboxSave.CurrentVersion;
        public SettingsSave settings = new SettingsSave();

        /// <summary>Input System binding overrides, as the JSON the Input System produces. Empty until theme 6.</summary>
        public string bindingOverrides = string.Empty;

        public RecordsSave records = new RecordsSave();
        public List<AchievementSave> achievements = new List<AchievementSave>();

        /// <summary>The permanent currency spent at Home. Not named or awarded yet.</summary>
        public int metaCurrency;

        /// <summary>Ids of permanent upgrades bought at Home.</summary>
        public List<string> purchasedUpgrades = new List<string>();

        public AchievementSave FindAchievement(string id) => achievements.Find(a => a.id == id);

        public LevelRecord FindLevel(string levelId) => records.levels.Find(l => l.levelId == levelId);
    }

    [Serializable]
    public class SettingsSave
    {
        // Audio
        public float masterVolume = 1f;
        public float musicVolume = 0.8f;
        public float sfxVolume = 0.8f;
        public bool muted;

        // Gameplay and HUD
        public bool autoStartWaves;
        public bool showDamageNumbers = true;
        public bool showHealthBars = true;
        public bool showRangeRings = true;

        // Display. Width and height 0 mean "the monitor's own resolution".
        public bool fullscreen = true;
        public int resolutionWidth;
        public int resolutionHeight;
        public bool vsync = true;

        // Accessibility
        /// <summary>Added to the automatic whole-number UI scale: -1 smaller, +1 larger.</summary>
        public int uiScaleOffset;
        public bool pauseOnFocusLoss = true;
    }

    [Serializable]
    public class RecordsSave
    {
        public int runsFinished;
        public int victories;
        public int totalKills;
        public int totalFoodEarned;
        public float totalPlaySeconds;
        public List<LevelRecord> levels = new List<LevelRecord>();
    }

    [Serializable]
    public class LevelRecord
    {
        public string levelId;
        public int bestWave;

        /// <summary>Wave retries used in the run that set <see cref="bestWave"/>. Shown beside it.</summary>
        public int bestWaveRetries;

        public int victories;

        /// <summary>Fastest win in game-time seconds, or 0 when the level has never been won.</summary>
        public float fastestVictorySeconds;
    }

    [Serializable]
    public class AchievementSave
    {
        public string id;
        public int progress;
        public bool unlocked;

        /// <summary>Local date and time of the unlock, "yyyy-MM-dd HH:mm".</summary>
        public string unlockedAt = string.Empty;
    }
}
