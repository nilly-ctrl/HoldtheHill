using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Stand-in Title, Home, Pause and Run End cards so every <see cref="GrayboxGameFlow"/> state
    /// can be reached and tested. Temporary: the real screens are uGUI prefabs
    /// (docs/PLAN_MENUS_AND_FLOW.md, themes 4 and 7) and replace this file.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Graybox/Graybox Flow Placeholder UI")]
    public class GrayboxFlowPlaceholderUi : MonoBehaviour
    {
        private const float CardWidth = 380f;
        private const float ButtonHeight = 32f;

        private GUIStyle _title;
        private GUIStyle _body;
        private GUIStyle _button;

        private void OnGUI()
        {
            GrayboxGameFlow flow = GrayboxGameFlow.Instance;
            if (flow == null || flow.State == GameFlowState.Playing) return;

            GrayboxUi.Apply(); // pixel skin; a no-op without a GrayboxUi in the scene
            _title ??= new GUIStyle(GUI.skin.label) { fontSize = 20, alignment = TextAnchor.MiddleCenter, richText = true };
            _body ??= new GUIStyle(GUI.skin.label) { fontSize = 10, alignment = TextAnchor.MiddleCenter, richText = true };
            _button ??= new GUIStyle(GUI.skin.button) { fontSize = 10 };

            // Dim the frozen game behind the card.
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            float height = flow.State == GameFlowState.RunEnd ? 350f : 230f;
            var card = new Rect((Screen.width - CardWidth) * 0.5f, (Screen.height - height) * 0.5f, CardWidth, height);
            GUI.Box(card, GUIContent.none);
            GUILayout.BeginArea(new Rect(card.x + 16f, card.y + 16f, card.width - 32f, card.height - 32f));

            switch (flow.State)
            {
                case GameFlowState.Title: DrawTitle(flow); break;
                case GameFlowState.Home: DrawHome(flow); break;
                case GameFlowState.Paused: DrawPaused(flow); break;
                case GameFlowState.RunEnd: DrawRunEnd(flow); break;
            }

            GUILayout.EndArea();
        }

        private void DrawTitle(GrayboxGameFlow flow)
        {
            GUILayout.Label("HOLD THE HILL", _title);
            GUILayout.Label("placeholder title screen", _body);
            GUILayout.Space(16f);
            if (Button("Start")) flow.QuitToHome();
        }

        private void DrawHome(GrayboxGameFlow flow)
        {
            GUILayout.Label("HOME", _title);
            GUILayout.Label("placeholder hub", _body);
            GUILayout.Space(16f);
            if (Button("Play Campaign")) flow.StartNewRun();
            if (Button("Back to Title")) flow.GoToTitle();
        }

        private void DrawPaused(GrayboxGameFlow flow)
        {
            GUILayout.Label("PAUSED", _title);
            GUILayout.Space(16f);
            if (Button("Resume  [Esc]")) flow.Resume();
            if (Button("Restart from Wave 1")) flow.StartNewRun();
            if (Button("Quit to Home")) flow.QuitToHome();
        }

        private void DrawRunEnd(GrayboxGameFlow flow)
        {
            GrayboxRunStats stats = flow.Stats;
            GUILayout.Label(flow.LastRunWasVictory
                ? "<color=#55ff55>HILL DEFENDED</color>"
                : "<color=#ff4444>HILL OVERRUN</color>", _title);
            GUILayout.Space(8f);
            GUILayout.Label($"Wave {stats.WaveReached} of {stats.TotalWaves}", _body);
            GUILayout.Label($"Kills {stats.Kills}     Food earned {stats.GoldEarned}", _body);
            GUILayout.Label($"Towers standing {stats.TowersStanding}     Time {FormatTime(stats.TimeSeconds)}", _body);
            GUILayout.Label($"Wave retries used {stats.WaveRetries}", _body);

            RunRecordResult record = flow.LastRunResult;
            string best = $"Best: wave {record.BestWave}" + (record.BestWaveRetries > 0 ? $" ({record.BestWaveRetries} retries)" : string.Empty);
            GUILayout.Label(record.NewBestWave ? $"<color=#ffd700>NEW BEST!</color>  {best}" : best, _body);
            GUILayout.Space(12f);

            if (!flow.LastRunWasVictory && flow.CanRetryWave && Button($"Retry Wave {flow.RetryWaveNumber}")) flow.RetryWave();
            if (Button("Restart from Wave 1")) flow.StartNewRun();
            if (Button("Quit to Home")) flow.QuitToHome();
        }

        private bool Button(string label) => GUILayout.Button(label, _button, GUILayout.Height(ButtonHeight));

        private static string FormatTime(float seconds)
        {
            int total = Mathf.FloorToInt(seconds);
            return $"{total / 60}:{total % 60:00}";
        }
    }
}
