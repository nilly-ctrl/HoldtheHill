using HoldTheHill.Features.Towers;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Visualizes tower range circles, target lock-on lines, and priority tags in PlayMode.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Graybox/Tower Target Visualizer")]
    [RequireComponent(typeof(Tower))]
    public class TowerTargetVisualizer : MonoBehaviour
    {
        [SerializeField] private Color _rangeColor = new Color(0.3f, 0.8f, 1f, 0.35f);
        [SerializeField] private Color _targetLineColor = new Color(1f, 0.3f, 0.3f, 0.85f);
        [SerializeField] private int _circleSegments = 40;

        private Tower _tower;
        private LineRenderer _rangeLine;
        private LineRenderer _targetLine;
        private Camera _mainCamera;
        private GUIStyle _labelStyle;

        public static bool ShowRangeRings { get; set; } = true;
        public static bool ShowTargetLines { get; set; } = true;
        public static bool ShowTowerLabels { get; set; } = true;

        private void Awake()
        {
            _tower = GetComponent<Tower>();
            _mainCamera = Camera.main;

            SetupLines();
        }

        private void SetupLines()
        {
            // Range Circle Line
            GameObject rangeObj = new GameObject("RangeCircle");
            rangeObj.transform.SetParent(transform, false);
            _rangeLine = rangeObj.AddComponent<LineRenderer>();
            _rangeLine.material = GetUnlitMaterial();
            _rangeLine.startColor = _rangeColor;
            _rangeLine.endColor = _rangeColor;
            _rangeLine.startWidth = 0.05f;
            _rangeLine.endWidth = 0.05f;
            _rangeLine.useWorldSpace = true;
            _rangeLine.loop = true;
            _rangeLine.positionCount = _circleSegments;
            _rangeLine.sortingOrder = 1;

            // Target Lock-On Line
            GameObject targetObj = new GameObject("TargetLine");
            targetObj.transform.SetParent(transform, false);
            _targetLine = targetObj.AddComponent<LineRenderer>();
            _targetLine.material = GetUnlitMaterial();
            _targetLine.startColor = _targetLineColor;
            _targetLine.endColor = _targetLineColor;
            _targetLine.startWidth = 0.06f;
            _targetLine.endWidth = 0.03f;
            _targetLine.useWorldSpace = true;
            _targetLine.positionCount = 2;
            _targetLine.sortingOrder = 5;
            _targetLine.enabled = false;
        }

        private static Material GetUnlitMaterial()
        {
            Shader shader = Shader.Find("Sprites/Default")
                         ?? Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Unlit/Color");
            return new Material(shader);
        }

        private void Update()
        {
            if (_tower == null)
            {
                return;
            }

            UpdateRangeRing();
            UpdateTargetLine();
        }

        private void UpdateRangeRing()
        {
            if (!ShowRangeRings)
            {
                _rangeLine.enabled = false;
                return;
            }

            _rangeLine.enabled = true;
            float radius = _tower.Range;
            Vector3 center = transform.position;

            for (int i = 0; i < _circleSegments; i++)
            {
                float angle = (i / (float)_circleSegments) * Mathf.PI * 2f;
                float x = center.x + Mathf.Cos(angle) * radius;
                float y = center.y + Mathf.Sin(angle) * radius;
                _rangeLine.SetPosition(i, new Vector3(x, y, 0f));
            }
        }

        private void UpdateTargetLine()
        {
            // CurrentTarget is only refreshed in Tower.Update, so this frame it can still point at an
            // enemy destroyed last frame. It's an interface, so `== null` alone can't see that:
            // Unity's destroyed-object check only runs through UnityEngine.Object.
            var target = _tower.CurrentTarget;
            bool gone = target == null || (target is Object unityObject && unityObject == null) || target.IsDead;
            if (!ShowTargetLines || gone)
            {
                _targetLine.enabled = false;
                return;
            }

            _targetLine.enabled = true;
            _targetLine.SetPosition(0, transform.position);
            _targetLine.SetPosition(1, target.Transform.position);
        }

        private void OnGUI()
        {
            if (!GrayboxGameFlow.GameplayActive) return;

            GUI.depth = 10; // world-space overlay: draw behind the HUD panels

            HoldTheHill.Sandbox.NillyCtrl.GrayboxUi.Apply(); // pixel skin; a no-op without a GrayboxUi in the scene
            if (!ShowTowerLabels || _tower == null)
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

            Vector3 worldPos = transform.position + Vector3.down * 0.55f;
            Vector3 screenPos = _mainCamera.WorldToScreenPoint(worldPos);
            if (screenPos.z < 0f)
            {
                return;
            }

            float guiX = screenPos.x - 60f;
            float guiY = Screen.height - screenPos.y;

            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 10,
                    alignment = TextAnchor.MiddleCenter,
                    richText = true,
                    fontStyle = FontStyle.Bold
                };
            }

            string targetText = _tower.CurrentTarget != null ? "<color=#ff7070>● Target</color>" : "<color=#808080>Idle</color>";
            string lvlBadge = _tower.Level > 1 ? $" <color=#ffd700>[LVL {_tower.Level}]</color>" : string.Empty;
            string tag = $"<color=#80d0ff><b>{_tower.Priority}</b></color>{lvlBadge}\n{targetText}";
            GUI.Label(new Rect(guiX, guiY, 120f, 32f), tag, _labelStyle);
        }
    }
}
