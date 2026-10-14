#if UNITY_EDITOR
using HoldTheHill.Core.Audio;
using HoldTheHill.Core.Icons;
using HoldTheHill.Features.Economy;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.GameFlow;
using HoldTheHill.Features.Progression;
using HoldTheHill.Core.Animation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HoldTheHill.Tests
{
    /// <summary>
    /// The managers live in one Systems prefab, and each generated scene holds one
    /// instance of it rather than its own copies.
    /// </summary>
    public class SystemsPrefabTests
    {
        private const string PrefabPath = "Assets/_Game/Features/GameFlow/Prefabs/Systems.prefab";
        // Mirrors SystemsBuilder.Members (an editor-assembly type the tests cannot see).
        private static readonly string[] Members =
        {
            "Wallet", "SkillTree", "CombatSystems", "ProceduralWaveGenerator",
            "Sfx", "IconLibrary", "ImpactFx", "Species", "PowerUps",
            "RunFlow",
        };

        private static readonly string[] Scenes =
        {
            "Assets/_Game/Scenes/Dev/CombatTest.unity",
            "Assets/_Game/Scenes/Levels/KitchenFloor/KitchenFloor.unity",
        };

        [Test]
        public void SystemsPrefab_HoldsTheFlowEconomyAndSkillTree()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab, "Run Tools > Hold the Hill > Build Combat Test Scene.");
            Assert.IsNotNull(prefab.GetComponentInChildren<RunFlow>(true), "flow");
            Assert.IsNotNull(prefab.GetComponentInChildren<WalletBehaviour>(true), "economy");
            Assert.IsNotNull(prefab.GetComponentInChildren<SkillTree>(true), "skill tree");
            Assert.IsNotNull(prefab.GetComponentInChildren<Queen>(true), "hill health");
        }

        [Test]
        public void EveryGeneratedScene_UsesTheSystemsPrefab_NotItsOwnManagers()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab, "Run Tools > Hold the Hill > Build Combat Test Scene.");
            string prefabGuid = AssetDatabase.AssetPathToGUID(PrefabPath);

            foreach (string path in Scenes)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) continue; // level not built on this machine

                string text = System.IO.File.ReadAllText(path);
                StringAssert.Contains(prefabGuid, text, $"{path} should hold a Systems instance");

                foreach (string member in Members)
                {
                    // A member built straight into the scene shows up as a root GameObject with that name;
                    // one from the prefab instance is only a modification record.
                    StringAssert.DoesNotContain($"\n  m_Name: {member}\n", text, $"{path}: '{member}' is a loose root object");
                }
            }
        }
    }
}
#endif
