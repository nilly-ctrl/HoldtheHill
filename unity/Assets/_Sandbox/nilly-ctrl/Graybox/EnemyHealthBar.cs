using HoldTheHill.Features.Enemies;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Renders a floating world-space GUI health bar and status indicators above an enemy.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Graybox/Enemy Health Bar")]
    public class EnemyHealthBar : MonoBehaviour
    {
        [SerializeField] private Vector3 _offset = new Vector3(0f, 0.55f, 0f);
        [SerializeField] private Vector2 _size = new Vector2(38f, 5f);
        [SerializeField] private bool _showOnlyWhenDamaged = false;

        private EnemyHealth _health;
        private Camera _mainCamera;
        private GUIStyle _bgStyle;
        private GUIStyle _fillStyle;
        private GUIStyle _textStyle;

        public static bool ShowHealthBars { get; set; } = true;

        private void Awake()
        {
            _health = GetComponent<EnemyHealth>();
            _mainCamera = Camera.main;
        }

        private void OnGUI()
        {
            if (!GrayboxGameFlow.GameplayActive) return;

            GUI.depth = 10; // world-space overlay: draw behind the HUD panels

            HoldTheHill.Sandbox.NillyCtrl.GrayboxUi.Apply(); // pixel skin; a no-op without a GrayboxUi in the scene
            if (!ShowHealthBars || _health == null || _health.IsDead)
            {
                return;
            }

            if (_showOnlyWhenDamaged && Mathf.Approximately(_health.HealthFraction, 1f))
            {
                return;
            }

            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null)
                {
                    return;
                }
            }

            Vector3 worldPos = transform.position + _offset;
            Vector3 screenPos = _mainCamera.WorldToScreenPoint(worldPos);

            // Behind camera
            if (screenPos.z < 0f)
            {
                return;
            }

            // Convert GUI Y-coordinate (GUI 0 is top of screen)
            float guiX = screenPos.x - _size.x * 0.5f;
            float guiY = Screen.height - screenPos.y - _size.y * 0.5f;

            InitStyles();

            // Background rect
            Rect bgRect = new Rect(guiX, guiY, _size.x, _size.y);
            GUI.Box(bgRect, GUIContent.none, _bgStyle);

            // Fill rect
            float fillWidth = Mathf.Max(0f, (_size.x - 2f) * _health.HealthFraction);
            Rect fillRect = new Rect(guiX + 1f, guiY + 1f, fillWidth, _size.y - 2f);

            Color originalColor = GUI.color;
            GUI.color = GetHealthColor(_health.HealthFraction);
            GUI.Box(fillRect, GUIContent.none, _fillStyle);
            GUI.color = originalColor;

            // Render energy shield bar if present
            var shield = GetComponent<EnemyShield>();
            if (shield != null && shield.ShieldFraction > 0f)
            {
                Rect shieldBgRect = new Rect(guiX, guiY - 5f, _size.x, 3f);
                GUI.Box(shieldBgRect, GUIContent.none, _bgStyle);

                float shieldWidth = Mathf.Max(0f, (_size.x - 2f) * shield.ShieldFraction);
                Rect shieldFillRect = new Rect(guiX + 1f, guiY - 4f, shieldWidth, 2f);
                GUI.color = new Color(0.3f, 0.75f, 1f, 0.9f);
                GUI.Box(shieldFillRect, GUIContent.none, _fillStyle);
                GUI.color = originalColor;
            }

            // Slow indicator badge
            if (_health.SpeedMultiplier < 0.99f)
            {
                Rect slowRect = new Rect(guiX + _size.x + 3f, guiY - 3f, 30f, 12f);
                GUI.Label(slowRect, "<color=#70c0ff>SLOW</color>", _textStyle);
            }
        }

        private void InitStyles()
        {
            if (_bgStyle != null)
            {
                return;
            }

            Texture2D texDark = MakeTexture(2, 2, new Color(0.1f, 0.1f, 0.12f, 0.85f));
            Texture2D texWhite = MakeTexture(2, 2, Color.white);

            _bgStyle = new GUIStyle { normal = { background = texDark } };
            _fillStyle = new GUIStyle { normal = { background = texWhite } };
            _textStyle = new GUIStyle(GUI.skin.label) { fontSize = 10, richText = true, fontStyle = FontStyle.Bold };
        }

        private static Color GetHealthColor(float fraction)
        {
            if (fraction > 0.5f)
            {
                return Color.Lerp(new Color(0.9f, 0.8f, 0.2f), new Color(0.2f, 0.85f, 0.3f), (fraction - 0.5f) * 2f);
            }
            return Color.Lerp(new Color(0.9f, 0.2f, 0.2f), new Color(0.9f, 0.8f, 0.2f), fraction * 2f);
        }

        private static Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
            {
                pix[i] = color;
            }

            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }
}
