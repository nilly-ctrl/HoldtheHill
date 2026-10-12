#if UNITY_EDITOR
using HoldTheHill.Sandbox.NillyCtrl;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HoldTheHill.Sandbox.Graybox.Tests
{
    /// <summary>
    /// The managers live in one GrayboxSystems prefab, and each generated scene holds one
    /// instance of it rather than its own copies.
    /// </summary>
    public class GrayboxSystemsPrefabTests
    {
        private const string PrefabPath = "Assets/_Sandbox/nilly-ctrl/Graybox/Prefabs/GrayboxSystems.prefab";
        // Mirrors GrayboxSystemsBuilder.Members (an editor-assembly type the tests cannot see).
        private static readonly string[] Members =
        {
            "GrayboxEconomy", "GrayboxSkillTree", "GrayboxCombatSystems", "GrayboxProceduralWaveGenerator",
            "GrayboxSfx", "GrayboxIcons", "GrayboxImpactFx", "GrayboxSpecies", "GrayboxPowerUps",
            "GrayboxGameFlow",
        };

        private static readonly string[] Scenes =
        {
            "Assets/_Sandbox/nilly-ctrl/Graybox/GrayboxCombatTest.unity",
            "Assets/_Sandbox/nilly-ctrl/Levels/KitchenFloor/KitchenFloor.unity",
        };

        [Test]
        public void SystemsPrefab_HoldsTheFlowEconomyAndSkillTree()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab, "Run Tools > Hold the Hill > Build Graybox Combat Test.");
            Assert.IsNotNull(prefab.GetComponentInChildren<GrayboxGameFlow>(true), "flow");
            Assert.IsNotNull(prefab.GetComponentInChildren<GrayboxEconomy>(true), "economy");
            Assert.IsNotNull(prefab.GetComponentInChildren<GrayboxSkillTree>(true), "skill tree");
            Assert.IsNotNull(prefab.GetComponentInChildren<GrayboxBaseHealth>(true), "hill health");
        }

        [Test]
        public void EveryGeneratedScene_UsesTheSystemsPrefab_NotItsOwnManagers()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab, "Run Tools > Hold the Hill > Build Graybox Combat Test.");
            string prefabGuid = AssetDatabase.AssetPathToGUID(PrefabPath);

            foreach (string path in Scenes)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) continue; // level not built on this machine

                string text = System.IO.File.ReadAllText(path);
                StringAssert.Contains(prefabGuid, text, $"{path} should hold a GrayboxSystems instance");

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
