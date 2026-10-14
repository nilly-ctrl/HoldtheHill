using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The levels the Home screen offers. Each level is its own generated scene (the graybox with
    /// a different map under it), so choosing another level loads another scene.
    /// </summary>
    /// <remarks>
    /// A level's <see cref="Level.Id"/> is its spawner's map id, which is also what its records are
    /// kept under. A level whose scene has not been built (or is not in the build) is left out of
    /// <see cref="Available"/> rather than offered and failing.
    ///
    /// To add one: build its scene (docs in <c>Levels/README.md</c>) and add a line below.
    /// </remarks>
    public static class GrayboxLevels
    {
        public sealed class Level
        {
            public string Id;
            public string Name;
            public string ScenePath;

            public string SceneName => System.IO.Path.GetFileNameWithoutExtension(ScenePath);
        }

        private const string Root = "Assets/_Sandbox/nilly-ctrl";

        private static readonly List<Level> s_all = new List<Level>
        {
            new Level { Id = "Graybox", Name = "Meadow road", ScenePath = Root + "/Graybox/GrayboxCombatTest.unity" },
            new Level { Id = "KitchenFloor", Name = "Kitchen floor", ScenePath = Root + "/Levels/KitchenFloor/KitchenFloor.unity" },
        };

        private static GrayboxRunMode? s_pendingRun;

        public static IReadOnlyList<Level> All => s_all;

        public static Level Find(string id) => s_all.Find(l => l.Id == id);

        /// <summary>The levels that can be loaded here, in catalogue order.</summary>
        public static List<Level> Available()
        {
            return s_all.FindAll(CanLoad);
        }

        public static bool CanLoad(Level level)
        {
#if UNITY_EDITOR
            return System.IO.File.Exists(level.ScenePath);
#else
            return Application.CanStreamedLevelBeLoaded(level.SceneName);
#endif
        }

        /// <summary>The id records are kept under for a level and mode.</summary>
        public static string RecordId(string levelId, GrayboxRunMode mode)
        {
            return mode == GrayboxRunMode.Endless ? levelId + "-Endless" : levelId;
        }

        /// <summary>
        /// Loads a level's scene and starts a run there as soon as it is up, so the player goes
        /// from Home straight into play.
        /// </summary>
        public static void LoadAndPlay(Level level, GrayboxRunMode mode)
        {
            if (!CanLoad(level))
            {
                Debug.LogError($"[GrayboxLevels] '{level.Name}' has no scene at {level.ScenePath}. Build it first.");
                return;
            }

            GrayboxSave.SaveIfDirty();
            s_pendingRun = mode;
#if UNITY_EDITOR
            // Works for a scene that is not in Build Settings, which these are not.
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                level.ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene(level.SceneName, LoadSceneMode.Single);
#endif
        }

        /// <summary>
        /// The run a scene was loaded to play, handed over once. The flow controller asks in its
        /// Awake; null means the scene was opened the ordinary way.
        /// </summary>
        public static GrayboxRunMode? TakePendingRun()
        {
            GrayboxRunMode? pending = s_pendingRun;
            s_pendingRun = null;
            return pending;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => s_pendingRun = null;
    }
}
