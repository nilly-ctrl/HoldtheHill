using HoldTheHill.Features.GameFlow;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HoldTheHill.Features.GameFlow
{
    /// <summary>
    /// Opt-in check for a player build, started with <c>-hthSmoke</c> on the command line (best with
    /// <c>-batchmode -nographics</c>). It goes Title, Play, Kitchen Floor, then ends the run in
    /// victory, logging each step with the <c>[Smoke]</c> prefix, and exits 0 on success or 1.
    /// </summary>
    /// <remarks>
    /// This proves the scenes are in the build and the flow reaches the win screen. It does not play
    /// the waves: the win is forced with <see cref="Queen.TriggerVictory"/>. Without the
    /// flag it does nothing.
    /// </remarks>
    public static class GrayboxPlayerSmoke
    {
        private const string Flag = "-hthSmoke";
        private const string LevelId = "KitchenFloor";
        private const float StepTimeout = 30f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Start()
        {
            if (!System.Array.Exists(System.Environment.GetCommandLineArgs(), a => a == Flag)) return;

            var host = new GameObject("GrayboxPlayerSmoke");
            Object.DontDestroyOnLoad(host);
            host.AddComponent<Runner>().StartCoroutine(Run(host.GetComponent<Runner>()));
        }

        private sealed class Runner : MonoBehaviour { }

        private static IEnumerator Run(MonoBehaviour _)
        {
            foreach (GrayboxLevels.Level level in GrayboxLevels.All)
            {
                Debug.Log($"[Smoke] CanLoad {level.Id} ({level.SceneName}): {GrayboxLevels.CanLoad(level)}");
            }

            GrayboxLevels.Level kitchen = GrayboxLevels.Find(LevelId);
            if (kitchen == null || !GrayboxLevels.CanLoad(kitchen)) { Finish(false, "Kitchen Floor cannot be loaded in this build"); yield break; }

            float until = Time.realtimeSinceStartup + StepTimeout;
            while (Object.FindAnyObjectByType<GrayboxGameFlow>() == null && Time.realtimeSinceStartup < until) yield return null;
            GrayboxGameFlow flow = Object.FindAnyObjectByType<GrayboxGameFlow>();
            if (flow == null) { Finish(false, "no game flow in the first scene"); yield break; }
            Debug.Log($"[Smoke] First scene '{SceneManager.GetActiveScene().name}', state {flow.State}");

            // What the Title's Play button does, then what Home's level card does.
            flow.QuitToHome();
            Debug.Log($"[Smoke] After Play: state {flow.State}");
            GrayboxLevels.LoadAndPlay(kitchen, GrayboxRunMode.Campaign);

            until = Time.realtimeSinceStartup + StepTimeout;
            while (Time.realtimeSinceStartup < until
                   && !(SceneManager.GetActiveScene().name == kitchen.SceneName
                        && Object.FindAnyObjectByType<GrayboxGameFlow>() != null && Object.FindAnyObjectByType<GrayboxGameFlow>() != flow
                        && Object.FindAnyObjectByType<Queen>() != null)) yield return null;

            flow = Object.FindAnyObjectByType<GrayboxGameFlow>();
            if (SceneManager.GetActiveScene().name != kitchen.SceneName || flow == null || Object.FindAnyObjectByType<Queen>() == null)
            {
                Finish(false, $"Kitchen Floor did not come up (scene '{SceneManager.GetActiveScene().name}')");
                yield break;
            }
            Debug.Log($"[Smoke] In '{SceneManager.GetActiveScene().name}': state {flow.State}, map {flow.MapId}");

            yield return new WaitForSeconds(2f);
            Object.FindAnyObjectByType<Queen>().TriggerVictory();

            until = Time.realtimeSinceStartup + StepTimeout;
            while (flow.State != GameFlowState.RunEnd && Time.realtimeSinceStartup < until) yield return null;
            bool won = flow.State == GameFlowState.RunEnd && flow.LastRunWasVictory;
            Finish(won, $"end state {flow.State}, victory {flow.LastRunWasVictory}, wave {flow.Stats.WaveReached}");
        }

        private static void Finish(bool ok, string detail)
        {
            Debug.Log($"[Smoke] {(ok ? "PASS" : "FAIL")}: {detail}");
            Application.Quit(ok ? 0 : 1);
        }
    }
}
