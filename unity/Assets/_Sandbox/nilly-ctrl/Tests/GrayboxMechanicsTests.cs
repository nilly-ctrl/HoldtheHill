#if UNITY_EDITOR
using System.Collections;
using HoldTheHill.Sandbox.NillyCtrl;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HoldTheHill.Sandbox.Graybox.Tests
{
    /// <summary>
    /// Test suite validating Economy, Skill Tree, Base HP Breach, and Procedural Wave scaling mechanics.
    /// </summary>
    public class GrayboxMechanicsTests
    {
        private GameObject _holder;

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("TestHolder");
        }

        [TearDown]
        public void TearDown()
        {
            if (_holder != null)
            {
                Object.DestroyImmediate(_holder);
            }
        }

        // ---------- Economy Tests ----------

        [Test]
        public void Economy_StartingGold_Is500()
        {
            var economy = _holder.AddComponent<GrayboxEconomy>();
            Assert.AreEqual(500, economy.CurrentGold);
        }

        [Test]
        public void Economy_TrySpendGold_DeductsBalance()
        {
            var economy = _holder.AddComponent<GrayboxEconomy>();
            bool success = economy.TrySpendGold(200);

            Assert.IsTrue(success);
            Assert.AreEqual(300, economy.CurrentGold);
        }

        [Test]
        public void Economy_TrySpendGold_FailsWhenOverspending()
        {
            var economy = _holder.AddComponent<GrayboxEconomy>();
            bool success = economy.TrySpendGold(600);

            Assert.IsFalse(success);
            Assert.AreEqual(500, economy.CurrentGold);
        }

        [Test]
        public void Economy_EarnGold_AddsToBalance()
        {
            var economy = _holder.AddComponent<GrayboxEconomy>();
            economy.EarnGold(150);

            Assert.AreEqual(650, economy.CurrentGold);
        }

        // ---------- Skill Tree Tests ----------

        [Test]
        public void SkillTree_StartingSP_Is5()
        {
            var skillTree = _holder.AddComponent<GrayboxSkillTree>();
            Assert.AreEqual(5, skillTree.SkillPoints);
        }

        [Test]
        public void SkillTree_UnlockNode_DeductsSP_AndRecalculatesMultipliers()
        {
            var skillTree = _holder.AddComponent<GrayboxSkillTree>();
            bool unlocked = skillTree.TryUnlock(SkillNodeId.Ballistics_HeavyCaliber);

            Assert.IsTrue(unlocked);
            Assert.AreEqual(4, skillTree.SkillPoints);
            Assert.AreEqual(1.20f, skillTree.DamageMultiplier, 0.001f);
        }

        [Test]
        public void SkillTree_PrerequisiteLock_PreventsEarlyUnlock()
        {
            var skillTree = _holder.AddComponent<GrayboxSkillTree>();
            // RapidCycling requires HeavyCaliber first
            bool unlocked = skillTree.TryUnlock(SkillNodeId.Ballistics_RapidCycling);

            Assert.IsFalse(unlocked);
            Assert.IsFalse(skillTree.IsUnlocked(SkillNodeId.Ballistics_RapidCycling));
        }

        [Test]
        public void SkillTree_PrerequisiteChain_UnlocksSuccessfully()
        {
            var skillTree = _holder.AddComponent<GrayboxSkillTree>();
            skillTree.TryUnlock(SkillNodeId.Ballistics_HeavyCaliber);
            bool unlocked = skillTree.TryUnlock(SkillNodeId.Ballistics_RapidCycling);

            Assert.IsTrue(unlocked);
            Assert.AreEqual(1.25f, skillTree.FireRateMultiplier, 0.001f);
        }

        // ---------- Base Health Tests ----------

        [Test]
        public void BaseHealth_StartingHP_Is100()
        {
            var baseHp = _holder.AddComponent<GrayboxBaseHealth>();
            Assert.AreEqual(100f, baseHp.CurrentHealth);
            Assert.IsFalse(baseHp.IsFinished);
        }

        [Test]
        public void BaseHealth_TakeBaseDamage_DeductsHP()
        {
            var baseHp = _holder.AddComponent<GrayboxBaseHealth>();
            baseHp.TakeBaseDamage(25f, Vector3.zero);

            Assert.AreEqual(75f, baseHp.CurrentHealth);
            Assert.IsFalse(baseHp.IsFinished);
        }

        [Test]
        public void BaseHealth_ZeroHP_TriggersDefeat()
        {
            var baseHp = _holder.AddComponent<GrayboxBaseHealth>();
            baseHp.TakeBaseDamage(100f, Vector3.zero);

            Assert.AreEqual(0f, baseHp.CurrentHealth);
            Assert.IsTrue(baseHp.IsFinished);
            Assert.IsFalse(baseHp.IsVictory);
        }

        // ---------- Procedural Wave Scaling Tests ----------

        [Test]
        public void ProceduralWaveGenerator_GeneratesScalingWaves()
        {
            var gen = _holder.AddComponent<GrayboxProceduralWaveGenerator>();
            float hpMultW1 = gen.GetHealthMultiplier(1);
            float hpMultW5 = gen.GetHealthMultiplier(5);

            Assert.AreEqual(1.0f, hpMultW1, 0.001f);
            Assert.AreEqual(1.48f, hpMultW5, 0.001f);
        }

        [Test]
        public void ProceduralWaveGenerator_GenerateWave_CreatesWaveData()
        {
            var gen = _holder.AddComponent<GrayboxProceduralWaveGenerator>();
            var grunt = new GameObject("TestGrunt");
            try
            {
                var wave = gen.GenerateWave(5, grunt, grunt, grunt, grunt, grunt, grunt);
                Assert.NotNull(wave);
                Assert.Greater(wave.enemies.Count, 20);
                Assert.IsTrue(wave.waveName.Contains("BOSS"));
            }
            finally
            {
                Object.DestroyImmediate(grunt);
            }
        }

        // ---------- Menu & Achievement System Tests ----------

        [Test]
        public void MenuManager_InitialState_MenusClosed()
        {
            var menu = _holder.AddComponent<GrayboxMenuManager>();
            Assert.IsFalse(menu.ShowStartMenu);
            Assert.IsFalse(menu.ShowSettingsMenu);
            Assert.AreEqual(1.0f, menu.MasterVolume);
        }

        [Test]
        public void Achievements_ReportProgress_UnlocksAchievement()
        {
            var achSystem = _holder.AddComponent<GrayboxAchievements>();
            achSystem.AddProgress("first_blood", 1);

            var firstBlood = System.Linq.Enumerable.FirstOrDefault(achSystem.Achievements, a => a.Id == "first_blood");
            Assert.NotNull(firstBlood);
            Assert.IsTrue(firstBlood.IsUnlocked);
        }
    }
}
#endif
