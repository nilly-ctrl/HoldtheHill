using System.Collections.Generic;
using UnityEngine;

namespace HoldTheHill.Features.Progression
{
    /// <summary>The achievements in the game, in the order they are listed to the player.</summary>
    [CreateAssetMenu(menuName = "Hold the Hill/Achievement Catalog", fileName = "AchievementCatalog")]
    public class AchievementCatalog : ScriptableObject
    {
        [SerializeField] private List<AchievementData> _achievements = new List<AchievementData>();

        public IReadOnlyList<AchievementData> Achievements => _achievements;

        /// <summary>Makes a catalog in code, for tests and sets built at run time.</summary>
        public static AchievementCatalog Create(params AchievementData[] achievements)
        {
            var catalog = CreateInstance<AchievementCatalog>();
            catalog._achievements = new List<AchievementData>(achievements);
            return catalog;
        }
    }
}
