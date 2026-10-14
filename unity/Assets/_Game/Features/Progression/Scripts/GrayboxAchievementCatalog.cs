using System.Collections.Generic;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>The achievements in the game, in the order they are listed to the player.</summary>
    [CreateAssetMenu(menuName = "Hold the Hill/Sandbox/Graybox Achievement Catalog", fileName = "GrayboxAchievementCatalog")]
    public class GrayboxAchievementCatalog : ScriptableObject
    {
        [SerializeField] private List<GrayboxAchievementData> _achievements = new List<GrayboxAchievementData>();

        public IReadOnlyList<GrayboxAchievementData> Achievements => _achievements;

        /// <summary>Makes a catalog in code, for tests and sets built at run time.</summary>
        public static GrayboxAchievementCatalog Create(params GrayboxAchievementData[] achievements)
        {
            var catalog = CreateInstance<GrayboxAchievementCatalog>();
            catalog._achievements = new List<GrayboxAchievementData>(achievements);
            return catalog;
        }
    }
}
