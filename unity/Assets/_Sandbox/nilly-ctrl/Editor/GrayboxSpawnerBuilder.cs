using HoldTheHill.Features.Progression;
using System.Collections.Generic;
using System.Linq;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Towers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The wave asset and the enemy spawner object, with the custom-horde spawner and the bridge beside it.
    /// </summary>
    internal static class GrayboxSpawnerBuilder
    {
        internal static MapWaveDataSO BuildWaveAsset(
            GameObject runner,
            GameObject grunt,
            GameObject brute,
            GameObject shielded,
            GameObject splitter,
            GameObject healer)
        {
            var asset = ScriptableObject.CreateInstance<MapWaveDataSO>();
            asset.mapId = "Graybox";
            asset.waves = new List<Wave>();

            const float Gap = 1.1f;

            // Wave 1: Eases in with Runners, Grunts, and Shielded Ants.
            // Wave 2: Introduces Splitter Ants (which split into 3 Swarm Ants on death).
            // Wave 3: Features Healer Support Ants protecting Brutes and Shielded Ants.
            // Wave 4: Combined assault with all enemy traits.
            var compositions = new[]
            {
                new[] { runner, runner, grunt, shielded, runner, grunt, shielded },
                new[] { grunt, runner, splitter, grunt, splitter, runner, grunt, splitter },
                new[] { brute, healer, shielded, brute, healer, grunt, shielded, brute },
                new[] { brute, splitter, healer, shielded, brute, splitter, healer, runner, brute, shielded },
            };

            for (int w = 0; w < compositions.Length; w++)
            {
                var wave = new Wave { waveName = $"Graybox Wave {w + 1}", enemies = new List<EnemySpawnEntry>() };
                foreach (GameObject prefab in compositions[w])
                {
                    wave.enemies.Add(new EnemySpawnEntry { enemyPrefab = prefab, delayBeforeNext = Gap });
                }

                asset.waves.Add(wave);
            }

            AssetDatabase.DeleteAsset(GrayboxBuilder.WaveAssetPath);
            AssetDatabase.CreateAsset(asset, GrayboxBuilder.WaveAssetPath);

            // Flush, force the import, then re-load. Assigning a freshly created
            // instance straight into a scene field serialized as {fileID: 0}: the
            // asset had no GUID registered yet, the reference came out null, and the
            // spawner silently found no waves. Nothing errored — the scene just did
            // nothing when you pressed Play.
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(GrayboxBuilder.WaveAssetPath, ImportAssetOptions.ForceSynchronousImport);

            var loaded = AssetDatabase.LoadAssetAtPath<MapWaveDataSO>(GrayboxBuilder.WaveAssetPath);
            if (loaded == null)
            {
                throw new System.InvalidOperationException(
                    $"[Graybox] Could not load the wave asset back from {GrayboxBuilder.WaveAssetPath}. " +
                    "Refusing to build a scene whose spawner would be silently empty.");
            }

            return loaded;
        }

        internal static void BuildSpawner(
            EnemyPath path,
            MapWaveDataSO waves,
            GameObject runner,
            GameObject grunt,
            GameObject brute,
            GameObject shielded,
            GameObject swarm,
            GameObject splitter,
            GameObject healer)
        {
            // Re-load rather than trusting the reference passed in. The asset is created
            // before EditorSceneManager.NewScene, and opening a new scene unloads assets
            // nothing is holding onto - which quietly turns that reference into null.
            waves = AssetDatabase.LoadAssetAtPath<MapWaveDataSO>(GrayboxBuilder.WaveAssetPath);
            if (waves == null)
            {
                throw new System.InvalidOperationException(
                    $"[Graybox] Wave asset missing at {GrayboxBuilder.WaveAssetPath} when wiring the spawner.");
            }

            var go = new GameObject("Enemy Spawner");
            go.transform.position = new Vector3(GrayboxSceneDressingBuilder.Route[0].x + 0.5f, GrayboxSceneDressingBuilder.Route[0].y + 0.5f, 0f);

            var container = new GameObject("Spawned Enemies");

            EnemySpawner spawner = go.AddComponent<EnemySpawner>();
            GrayboxBuildKit.Apply(spawner, so =>
            {
                so.FindProperty("defaultSpawnPoint").objectReferenceValue = go.transform;
                so.FindProperty("enemyContainer").objectReferenceValue = container.transform;
                so.FindProperty("activeMapId").stringValue = "Graybox";
                so.FindProperty("activeMapConfigOverride").objectReferenceValue = waves;
                so.FindProperty("autoStartFirstWave").boolValue = true;
                so.FindProperty("requireEnemiesClearedBeforeNextWave").boolValue = false;
            });

            // Read it straight back. An object reference that fails to resolve writes
            // {fileID: 0} without complaining, which produces a scene that loads fine
            // and then does absolutely nothing.
            var check = new SerializedObject(spawner);
            if (check.FindProperty("activeMapConfigOverride").objectReferenceValue == null)
            {
                throw new System.InvalidOperationException(
                    "[Graybox] The wave asset did not survive assignment to the spawner. " +
                    "The scene would spawn no enemies.");
            }

            // Custom horde spawner for stress-testing setups
            GrayboxCustomSpawner customSpawner = go.AddComponent<GrayboxCustomSpawner>();
            customSpawner.Configure(
                new List<GrayboxCustomSpawner.Entry>
                {
                    new GrayboxCustomSpawner.Entry { Id = "Runner", Prefab = runner },
                    new GrayboxCustomSpawner.Entry { Id = "Grunt", Prefab = grunt },
                    new GrayboxCustomSpawner.Entry { Id = "Brute", Prefab = brute },
                    new GrayboxCustomSpawner.Entry { Id = "Shielded", Prefab = shielded },
                    new GrayboxCustomSpawner.Entry { Id = "Swarm", Prefab = swarm, Mixed = false },
                    new GrayboxCustomSpawner.Entry { Id = "Splitter", Prefab = splitter },
                    new GrayboxCustomSpawner.Entry { Id = "Healer", Prefab = healer },
                },
                go.transform,
                container.transform);

            GrayboxProgressionBuilder.ConfigureAchievements(go.AddComponent<GrayboxAchievements>());
        }
    }
}
