#if UNITY_EDITOR
using HoldTheHill.Sandbox.NillyCtrl;

namespace HoldTheHill.Sandbox.Graybox.Tests
{
    /// <summary>
    /// Small data sets for tests, made in memory so a test does not depend on the assets in the
    /// project. They copy the first entries of the shipped skill tree and achievement catalogs.
    /// </summary>
    internal static class GrayboxTestContent
    {
        /// <summary>Heavy Caliber (+20% damage), then Rapid Cycling (+25% fire rate) which needs it.</summary>
        public static GrayboxSkillTreeData SkillTree()
        {
            GrayboxSkillNodeData heavy = GrayboxSkillNodeData.Create("Ballistics_HeavyCaliber", "Heavy Caliber",
                "+20% Damage for all towers.", "Ballistics", 1, null,
                new SkillEffect { stat = SkillStat.DamageMultiplier, mode = SkillEffectMode.Multiply, value = 1.2f });
            GrayboxSkillNodeData rapid = GrayboxSkillNodeData.Create("Ballistics_RapidCycling", "Rapid Cycling",
                "+25% Fire Rate for all towers.", "Ballistics", 2, heavy,
                new SkillEffect { stat = SkillStat.FireRateMultiplier, mode = SkillEffectMode.Multiply, value = 1.25f });
            return GrayboxSkillTreeData.Create(heavy, rapid);
        }

        /// <summary>The kill achievements and a wave one, enough to try each way progress is counted.</summary>
        public static GrayboxAchievementCatalog Achievements()
        {
            return GrayboxAchievementCatalog.Create(
                GrayboxAchievementData.Create("first_blood", "First Blood", "Defeat your first enemy.", 1, AchievementTrigger.EnemyDefeated),
                GrayboxAchievementData.Create("colony_defender", "Colony Defender", "Defeat 50 enemies.", 50, AchievementTrigger.EnemyDefeated),
                GrayboxAchievementData.Create("boss_slayer", "Boss Slayer", "Finish wave 5.", 1, AchievementTrigger.WaveCompleted, everyNthWave: 5),
                GrayboxAchievementData.Create("wave_survivor", "Endless Survivor", "Reach wave 8.", 8, AchievementTrigger.WaveReached),
                GrayboxAchievementData.Create("fortress", "Impenetrable Fortress", "Finish 3 waves at full health.", 3, AchievementTrigger.WaveCompleted, requireFullBaseHealth: true));
        }
    }
}
#endif
