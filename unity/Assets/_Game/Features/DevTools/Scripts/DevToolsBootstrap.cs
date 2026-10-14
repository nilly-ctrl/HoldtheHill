#if UNITY_EDITOR || DEVELOPMENT_BUILD
using HoldTheHill.Features.GameFlow;
using UnityEngine;

namespace HoldTheHill.Features.DevTools
{
    /// <summary>
    /// Puts the dev tools in a scene: the console, the live tuning panel, the replay recorder and
    /// player, the cheat state and the performance overlay. Add it with
    /// <c>Tools/Hold the Hill/DevTools/Add DevTools To Open Scene</c>, or let the menu's
    /// auto-create option add it on Play.
    /// </summary>
    /// <remarks>
    /// The whole dev tools folder is compiled out of release builds, so this class does not
    /// exist there either.
    /// </remarks>
    [AddComponentMenu("Hold the Hill/DevTools/Dev Tools Bootstrap")]
    public class DevToolsBootstrap : MonoBehaviour
    {
        [Tooltip("Show the FPS and counts overlay on start. F4 toggles it.")]
        [SerializeField] private bool _showPerfOnStart = true;

        [Tooltip("Start a replay recording at the start of every run, and save it when the run ends.")]
        [SerializeField] private bool _autoRecordRuns;

        /// <summary>
        /// When true, a bootstrap is created after a scene loads if the scene has the game flow but no
        /// bootstrap. Set by the Editor menu; stays false in builds.
        /// </summary>
        public static bool AutoCreate { get; set; }

        public static DevToolsBootstrap Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Instance = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreateIfWanted()
        {
            if (!AutoCreate || Instance != null) return;
            if (FindAnyObjectByType<DevToolsBootstrap>() != null) return;
            if (FindAnyObjectByType<GrayboxGameFlow>() == null) return;

            var holder = new GameObject("DevTools (auto)");
            holder.AddComponent<DevToolsBootstrap>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            DevBuiltinCommands.RegisterAll(DevCommandRegistry.Default);

            GetOrAdd<DevToolsCheats>();
            GetOrAdd<DevToolsConsole>();
            GetOrAdd<DevTuningPanel>();
            GetOrAdd<DevPerfHud>();
            GetOrAdd<DevReplayPlayer>();
            GetOrAdd<DevReplayRecorder>().AutoRecord = _autoRecordRuns;

            DevPerfHud.Visible = _showPerfOnStart;
        }

        private void Update()
        {
            DevGui.UpdatePointerBlock();
        }

        private void OnDestroy()
        {
            if (Instance != this) return;

            Instance = null;
            DevGui.ReleasePointerBlock();
            DevInput.Reset();
        }

        private T GetOrAdd<T>() where T : Component
        {
            T existing = GetComponent<T>();
            return existing != null ? existing : gameObject.AddComponent<T>();
        }
    }
}
#endif
