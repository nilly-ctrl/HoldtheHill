using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>What happens in the game that moves an achievement forward.</summary>
    public enum AchievementTrigger
    {
        /// <summary>Nothing does by itself; code advances it with AddProgress and its id.</summary>
        Manual,

        /// <summary>Each enemy defeated adds 1.</summary>
        EnemyDefeated,

        /// <summary>Each proximity mine that goes off adds 1.</summary>
        MineDetonated,

        /// <summary>Each tower built adds 1.</summary>
        TowerBuilt,

        /// <summary>Each skill point spent adds 1.</summary>
        SkillPointSpent,

        /// <summary>The gold held when it is reported; progress is the highest amount seen.</summary>
        GoldHeld,

        /// <summary>Each wave completed adds 1, if the wave and base health conditions below hold.</summary>
        WaveCompleted,

        /// <summary>The highest wave number reached; progress is that number.</summary>
        WaveReached
    }

    /// <summary>
    /// One achievement: its text, how far it goes and what moves it. Add an achievement by
    /// making one of these and listing it in the achievement catalog.
    /// </summary>
    [CreateAssetMenu(menuName = "Hold the Hill/Sandbox/Graybox Achievement Data", fileName = "AchievementData")]
    public class GrayboxAchievementData : ScriptableObject
    {
        [Tooltip("Short unique id, e.g. first_blood. Saved with the player's progress, so don't rename it later.")]
        [SerializeField] private string _id;

        [Tooltip("Name shown to the player.")]
        [SerializeField] private string _title;

        [Tooltip("One line on how to earn it.")]
        [SerializeField, TextArea(1, 3)] private string _description;

        [Tooltip("Icon file name without the extension.")]
        [SerializeField] private string _iconName;

        [Tooltip("Progress needed to unlock it.")]
        [SerializeField, Min(1)] private int _requiredProgress = 1;

        [Tooltip("What in the game moves it forward.")]
        [SerializeField] private AchievementTrigger _trigger;

        [Tooltip("WaveCompleted only: counts only every Nth wave (5 = waves 5, 10, 15). 0 counts every wave.")]
        [SerializeField, Min(0)] private int _everyNthWave;

        [Tooltip("WaveCompleted only: counts only if the base is at full health when the wave ends.")]
        [SerializeField] private bool _requireFullBaseHealth;

        public string Id => string.IsNullOrEmpty(_id) ? name : _id;

        public string Title => string.IsNullOrEmpty(_title) ? Id : _title;

        public string Description => _description;

        public string IconName => _iconName;

        public int RequiredProgress => _requiredProgress;

        public AchievementTrigger Trigger => _trigger;

        public int EveryNthWave => _everyNthWave;

        public bool RequireFullBaseHealth => _requireFullBaseHealth;

        /// <summary>Makes an achievement in code, for tests and sets built at run time.</summary>
        public static GrayboxAchievementData Create(string id, string title, string description, int requiredProgress,
            AchievementTrigger trigger, int everyNthWave = 0, bool requireFullBaseHealth = false)
        {
            var data = CreateInstance<GrayboxAchievementData>();
            data.name = id;
            data._id = id;
            data._title = title;
            data._description = description;
            data._requiredProgress = requiredProgress;
            data._trigger = trigger;
            data._everyNthWave = everyNthWave;
            data._requireFullBaseHealth = requireFullBaseHealth;
            return data;
        }
    }
}
