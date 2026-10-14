using HoldTheHill.Core.Audio;
using HoldTheHill.Core.Icons;
using HoldTheHill.Features.Economy;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.GameFlow;
using HoldTheHill.Features.Progression;
using HoldTheHill.Core.Animation;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HoldTheHill.EditorTools.Builders
{
    /// <summary>
    /// Gathers the graybox's manager objects (flow, economy, skill tree, sounds, icons, species,
    /// power-ups and the rest) into one <c>GrayboxSystems</c> prefab, so a level scene carries
    /// one prefab instance instead of its own copy of every manager.
    /// </summary>
    /// <remarks>
    /// Runs at the end of <see cref="Build"/>: the managers are built as root
    /// objects the way they always were, then parented under one root and saved as a prefab that
    /// the open scene stays connected to. Scene-bound objects (camera, path, ground, spawner,
    /// tower placer, the specials panel that points at the spawner) stay at the root; a level
    /// builder may move or replace those without touching the prefab.
    /// </remarks>
    internal static class SystemsBuilder
    {
        public const string RootName = "Systems";
        public const string PrefabPath = "Assets/_Game/Features/GameFlow/Prefabs/Systems.prefab";

        /// <summary>Root objects that hold managers with no reference to anything in the scene.</summary>
        public static readonly string[] Members =
        {
            "Wallet", "SkillTree", "CombatSystems", "ProceduralWaveGenerator",
            "Sfx", "IconLibrary", "ImpactFx", "Species", "PowerUps",
            "RunFlow",
        };

        /// <summary>
        /// Moves the member roots of the open scene under one <c>GrayboxSystems</c> object and saves it
        /// as the prefab, leaving the scene with a connected instance. Members that are not in the
        /// scene (an optional system that did not build) are skipped.
        /// </summary>
        public static GameObject Collect(Scene scene)
        {
            var byName = new Dictionary<string, GameObject>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                byName[root.name] = root;
            }

            if (byName.TryGetValue(RootName, out GameObject existing))
            {
                // Built twice without a fresh scene: start again rather than nest a copy.
                Object.DestroyImmediate(existing);
            }

            var systems = new GameObject(RootName);
            foreach (string name in Members)
            {
                if (byName.TryGetValue(name, out GameObject member))
                {
                    member.transform.SetParent(systems.transform, false);
                }
            }

            BuildKit.EnsureFolder("Assets/_Game/Features/GameFlow/Prefabs");
            return PrefabUtility.SaveAsPrefabAssetAndConnect(systems, PrefabPath, InteractionMode.AutomatedAction);
        }
    }
}
