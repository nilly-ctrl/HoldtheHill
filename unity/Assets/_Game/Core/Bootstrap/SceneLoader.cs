using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HoldTheHill.Core
{
    /// <summary>
    /// Lives in the Bootstrap scene. Loads the persistent scenes (UI) and a level's scenes additively,
    /// so pressing Play in ANY scene (Bootstrap, UI, Terrain or Gameplay) starts the full game.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        public const string BootstrapSceneName = "Bootstrap";

        [Tooltip("Scenes that stay loaded for the whole game.")]
        [SerializeField] private string[] _persistentScenes = { "UI" };

        [Tooltip("Scenes for the level to load when none is open. The last one becomes the active scene.")]
        [SerializeField] private string[] _defaultLevelScenes = { "Hill01_Terrain", "Hill01_Gameplay" };

        // Runs after the first scene loads, in the Editor and in builds.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBootstrapLoaded()
        {
            if (!IsLoaded(BootstrapSceneName))
            {
                SceneManager.LoadScene(BootstrapSceneName, LoadSceneMode.Additive);
            }
        }

        private IEnumerator Start()
        {
            foreach (string sceneName in _persistentScenes)
            {
                yield return LoadIfMissing(sceneName);
            }

            // If no level scene is open (e.g. Play pressed in Bootstrap or UI), open the default level.
            // If part of the default level is open (e.g. Play pressed in Hill01_Gameplay), open the rest of it.
            if (!AnyLevelSceneLoaded() || AnyLoaded(_defaultLevelScenes))
            {
                foreach (string sceneName in _defaultLevelScenes)
                {
                    yield return LoadIfMissing(sceneName);
                }

                if (_defaultLevelScenes.Length > 0)
                {
                    Scene active = SceneManager.GetSceneByName(_defaultLevelScenes[^1]);
                    if (active.isLoaded)
                    {
                        SceneManager.SetActiveScene(active);
                    }
                }
            }
        }

        private static IEnumerator LoadIfMissing(string sceneName)
        {
            if (!IsLoaded(sceneName))
            {
                yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            }
        }

        private bool AnyLevelSceneLoaded()
        {
            var nonLevel = new HashSet<string>(_persistentScenes) { BootstrapSceneName };
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded && !nonLevel.Contains(scene.name))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool AnyLoaded(IEnumerable<string> sceneNames)
        {
            foreach (string sceneName in sceneNames)
            {
                if (IsLoaded(sceneName))
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsLoaded(string sceneName)
        {
            return SceneManager.GetSceneByName(sceneName).isLoaded;
        }
    }
}
