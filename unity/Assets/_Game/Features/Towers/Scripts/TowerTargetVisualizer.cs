using HoldTheHill.Features.GameFlow;
using HoldTheHill.Features.DamageNumbers;
using HoldTheHill.Features.Towers;
using UnityEngine;

namespace HoldTheHill.Features.Towers
{
    /// <summary>
    /// Visualizes tower range circles, target lock-on lines, and a priority and level label
    /// under the tower (a <see cref="PixelText"/> in the scene's Hud style) in PlayMode.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Tower Target Visualizer")]
    [RequireComponent(typeof(Tower))]
    public class TowerTargetVisualizer : MonoBehaviour
    {
        [SerializeField] private Color _rangeColor = new Color(0.3f, 0.8f, 1f, 0.35f);
        [SerializeField] private Color _targetLineColor = new Color(1f, 0.3f, 0.3f, 0.85f);
        [SerializeField] private int _circleSegments = 40;

        private Tower _tower;
        private LineRenderer _rangeLine;
        private LineRenderer _targetLine;
        private PixelText _label;
        private PixelFontTheme _theme;
        private bool _lookedForTheme;
        private int _levelShown = -1;
        private bool _hasTargetShown;
        private TargetingPriority _priorityShown;

        public static bool ShowRangeRings { get; set; } = true;
        public static bool ShowTargetLines { get; set; } = true;
        public static bool ShowTowerLabels { get; set; } = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ShowRangeRings = true;
            ShowTargetLines = true;
            ShowTowerLabels = true;
        }

        private RunFlow _flow;

        private void Awake()
        {
            _tower = GetComponent<Tower>();
            _flow = FindAnyObjectByType<RunFlow>();

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
            // Explicit null checks: ?? would skip Unity's destroyed-object check on a Shader.
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            return new Material(shader);
        }

        private void Update()
        {
            if (_tower == null)
            {
                return;
            }

            UpdateRangeRing();
            bool hasTarget = UpdateTargetLine();
            UpdateLabel(hasTarget);
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

        /// <returns>True while the tower has a live target.</returns>
        private bool UpdateTargetLine()
        {
            // CurrentTarget is only refreshed in Tower.Update, so this frame it can still point at an
            // enemy destroyed last frame. It's an interface, so `== null` alone can't see that:
            // Unity's destroyed-object check only runs through UnityEngine.Object.
            var target = _tower.CurrentTarget;
            bool gone = target == null || (target is Object unityObject && unityObject == null) || target.IsDead;
            if (!ShowTargetLines || gone)
            {
                _targetLine.enabled = false;
                return !gone;
            }

            _targetLine.enabled = true;
            _targetLine.SetPosition(0, transform.position);
            _targetLine.SetPosition(1, target.Transform.position);
            return true;
        }

        // "Closest  LVL 2  TARGET" under the tower. Only when the scene has a font theme with a Hud style.
        private void UpdateLabel(bool hasTarget)
        {
            bool show = ShowTowerLabels && (_flow == null || _flow.IsPlaying);
            if (!show)
            {
                if (_label != null && _label.gameObject.activeSelf) _label.gameObject.SetActive(false);
                return;
            }

            if (_label == null && !CreateLabel())
            {
                return;
            }

            if (_tower.Level != _levelShown || hasTarget != _hasTargetShown || _tower.Priority != _priorityShown)
            {
                _levelShown = _tower.Level;
                _hasTargetShown = hasTarget;
                _priorityShown = _tower.Priority;
                string level = _tower.Level > 1 ? $"  LVL {_tower.Level}" : string.Empty;
                _label.Text = $"{_tower.Priority}{level}  {(hasTarget ? "TARGET" : "IDLE")}";
            }

            if (!_label.gameObject.activeSelf) _label.gameObject.SetActive(true);
        }

        private bool CreateLabel()
        {
            if (!_lookedForTheme)
            {
                _lookedForTheme = true;
                _theme = FindAnyObjectByType<PixelFontTheme>();
            }

            PixelFontStyle style = _theme != null ? _theme.Find("Hud") : null;
            if (style == null)
            {
                return false;
            }

            var go = new GameObject("Label");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.down * 0.55f;
            _label = go.AddComponent<PixelText>();
            _label.ThemeStyle = "Hud";
            _label.Style = style;
            _label.Alignment = PixelText.Anchor.Centre;
            _label.SetRendering(1, 6);
            return true;
        }
    }
}
