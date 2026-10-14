using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HoldTheHill.EditorTools.Validation
{
    /// <summary>Runs the project validator and lists what it found, with a ping button per finding.</summary>
    public class ValidatorWindow : EditorWindow
    {
        private readonly List<ValidationFinding> _findings = new List<ValidationFinding>();
        private Vector2 _scroll;
        private bool _hasRun;
        private bool _showErrors = true;
        private bool _showWarnings = true;
        private bool _showInfo = true;
        private string _textFilter = string.Empty;

        [MenuItem("Tools/Hold the Hill/Validation/Run Project Validator", false, 100)]
        public static void ShowAndRun()
        {
            var window = GetWindow<ValidatorWindow>("HtH Validator");
            window.minSize = new Vector2(480f, 360f);
            window.Show();
            window.RunValidator();
        }

        private void RunValidator()
        {
            _findings.Clear();
            _findings.AddRange(ProjectValidator.Run());
            _hasRun = true;
            Repaint();
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Run", EditorStyles.toolbarButton, GUILayout.Width(60f)))
                {
                    RunValidator();
                }

                if (GUILayout.Button("Copy report", EditorStyles.toolbarButton, GUILayout.Width(90f)))
                {
                    EditorGUIUtility.systemCopyBuffer = ValidationReport.ToText(_findings);
                }

                GUILayout.FlexibleSpace();
                _showErrors = GUILayout.Toggle(_showErrors, "Errors", EditorStyles.toolbarButton);
                _showWarnings = GUILayout.Toggle(_showWarnings, "Warnings", EditorStyles.toolbarButton);
                _showInfo = GUILayout.Toggle(_showInfo, "Info", EditorStyles.toolbarButton);
                _textFilter = GUILayout.TextField(_textFilter, EditorStyles.toolbarSearchField, GUILayout.Width(140f));
            }

            if (!_hasRun)
            {
                EditorGUILayout.HelpBox("Press Run. The validator only reads; it never changes an asset.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField(ValidationReport.Summary(_findings), EditorStyles.boldLabel);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (ValidationFinding finding in _findings)
            {
                if (!Visible(finding))
                {
                    continue;
                }

                DrawFinding(finding);
            }

            EditorGUILayout.EndScrollView();
        }

        private bool Visible(ValidationFinding finding)
        {
            bool bySeverity = finding.Severity == ValidationSeverity.Error ? _showErrors
                : finding.Severity == ValidationSeverity.Warning ? _showWarnings
                : _showInfo;

            if (!bySeverity)
            {
                return false;
            }

            if (string.IsNullOrEmpty(_textFilter))
            {
                return true;
            }

            return finding.Message.IndexOf(_textFilter, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                   finding.AssetPath.IndexOf(_textFilter, System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                   finding.Check.IndexOf(_textFilter, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void DrawFinding(ValidationFinding finding)
        {
            MessageType type = finding.Severity == ValidationSeverity.Error ? MessageType.Error
                : finding.Severity == ValidationSeverity.Warning ? MessageType.Warning
                : MessageType.Info;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.HelpBox($"{finding.Check}: {finding.Message}", type);

                if (finding.AssetPath.Length == 0)
                {
                    return;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.SelectableLabel(finding.AssetPath, GUILayout.Height(EditorGUIUtility.singleLineHeight));

                    if (GUILayout.Button("Ping", GUILayout.Width(50f)) && finding.Target != null)
                    {
                        Selection.activeObject = finding.Target;
                        EditorGUIUtility.PingObject(finding.Target);
                    }
                }
            }
        }
    }
}
