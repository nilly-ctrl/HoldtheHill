using HoldTheHill.Sandbox.NillyCtrl;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Displays Hill Base HP readout in the top HUD and renders full-screen Victory / Defeat modal cards when the game finishes.
    /// </summary>
    public class GrayboxBaseHealthUI : MonoBehaviour
    {
        private GUIStyle _headerStyle;
        private GUIStyle _bannerStyle;
        private GUIStyle _btnStyle;

        private void OnGUI()
        {
            if (!GrayboxGameFlow.GameplayActive) return;

            HoldTheHill.Sandbox.NillyCtrl.GrayboxUi.Apply(); // pixel skin; a no-op without a GrayboxUi in the scene
            InitStyles();

            if (GrayboxBaseHealth.Instance == null) return;

            DrawTopHpBar();

            // With a flow controller the run-end screen is its job; this card is the fallback.
            if (GrayboxBaseHealth.Instance.IsFinished && GrayboxGameFlow.Instance == null)
            {
                DrawEndGameModal();
            }
        }

        private void DrawTopHpBar()
        {
            float width = 280f;
            float height = 36f;
            float x = (Screen.width - width) * 0.5f;
            float y = 10f;

            Rect rect = new Rect(x, y, width, height);
            GUI.Box(rect, GUIContent.none);

            float hpFrac = GrayboxBaseHealth.Instance.CurrentHealth / GrayboxBaseHealth.Instance.MaxHealth;
            Color barColor = hpFrac > 0.5f ? new Color(0.3f, 0.9f, 0.4f) : (hpFrac > 0.25f ? new Color(1f, 0.8f, 0.2f) : new Color(1f, 0.3f, 0.3f));

            // Pixel bar: framed well, a fill that turns gold then red as the hill weakens, quarter marks.
            var frameRect = new Rect(x + 18f, y + 18f, width - 24f, 14f);
            if (GrayboxIcons.DrawSliced(frameRect, "BarFrame", 3))
            {
                string fill = hpFrac > 0.5f ? "BarFillHealth" : (hpFrac > 0.25f ? "BarFillFood" : "BarFillDanger");
                float inner = frameRect.width - 6f;
                if (hpFrac > 0f)
                {
                    GrayboxIcons.DrawSliced(new Rect(frameRect.x + 3f, frameRect.y + 3f, Mathf.Max(2f, inner * hpFrac), 8f), fill, 1);
                }

                Texture2D tick = GrayboxIcons.Get("BarTick");
                for (int i = 1; i < 4 && tick != null; i++)
                {
                    GUI.DrawTexture(new Rect(frameRect.x + 3f + Mathf.Round(inner * i / 4f), frameRect.y + 3f, 1f, 8f), tick);
                }
            }
            else
            {
                Rect fillRect = new Rect(x + 4f, y + 20f, (width - 8f) * hpFrac, 12f);
                GUI.color = barColor;
                GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
                GUI.color = Color.white;
            }

            Texture2D heart = GrayboxIcons.Get("ResQueenHealthIcon");
            if (heart != null)
            {
                GUI.DrawTexture(new Rect(x - GrayboxIcons.Size * 0.5f, y + 2f, GrayboxIcons.Size, GrayboxIcons.Size), heart);
            }

            GUI.Label(new Rect(x, y + 2f, width, 20f), $"<b>HILL BASE HP:</b> {Mathf.RoundToInt(GrayboxBaseHealth.Instance.CurrentHealth)} / {Mathf.RoundToInt(GrayboxBaseHealth.Instance.MaxHealth)}", _headerStyle);
        }

        private void DrawEndGameModal()
        {
            bool isVictory = GrayboxBaseHealth.Instance.IsVictory;
            float width = 380f;
            float height = 180f;
            float x = (Screen.width - width) * 0.5f;
            float y = (Screen.height - height) * 0.5f;

            Rect modalRect = new Rect(x, y, width, height);
            GUI.Box(modalRect, GUIContent.none);

            GUILayout.BeginArea(new Rect(x + 16f, y + 16f, width - 32f, height - 32f));

            if (isVictory)
            {
                GUILayout.Label("<color=#55ff55><b>VICTORY! HILL DEFENDED!</b></color>", _bannerStyle);
                GUILayout.Label("All enemy waves repelled successfully!", _headerStyle);
            }
            else
            {
                GUILayout.Label("<color=#ff4444><b>DEFEAT! HILL OVERRUN!</b></color>", _bannerStyle);
                GUILayout.Label("The ant horde breached your perimeter defences!", _headerStyle);
            }

            GUILayout.Space(16);
            if (GUILayout.Button("Restart Wave Defense", _btnStyle, GUILayout.Height(34)))
            {
                GrayboxBaseHealth.Instance.ResetBaseHealth();
                GrayboxEconomy.Instance?.ResetGold(500);
                GrayboxSkillTree.Instance?.ResetSkills();

                var spawner = FindAnyObjectByType<EnemySpawner>();
                if (spawner != null)
                {
                    spawner.SetActiveMap(spawner.ActiveMapId);
                    spawner.StartNextWave();
                }
            }

            GUILayout.EndArea();
        }

        private void InitStyles()
        {
            if (_headerStyle != null) return;

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };

            _bannerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };

            _btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold
            };
        }
    }
}
