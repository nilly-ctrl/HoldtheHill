using System.IO;
using UnityEditor;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl.Editor
{
    /// <summary>
    /// Picks a balance CSV, shows every change it would make as an old -> new table (a dry run),
    /// and applies them only on request. Applying is one undo step.
    /// </summary>
    internal sealed class BalanceImportWindow : EditorWindow
    {
        private int _kindIndex;
        private string _file;
        private BalanceSheet.Preview _preview;
        private Vector2 _scroll;

        public static void Open()
        {
            var window = GetWindow<BalanceImportWindow>("Balance CSV Import");
            window.minSize = new Vector2(520f, 360f);
            window.Show();
        }

        private void OnGUI()
        {
            string[] names = new string[BalanceSheet.All.Length];
            for (int i = 0; i < names.Length; i++)
            {
                names[i] = BalanceSheet.All[i].Name;
            }

            EditorGUI.BeginChangeCheck();
            _kindIndex = EditorGUILayout.Popup("Data", _kindIndex, names);
            BalanceSheet.Kind kind = BalanceSheet.All[_kindIndex];

            using (new EditorGUILayout.HorizontalScope())
            {
                _file = EditorGUILayout.TextField("CSV file", _file);
                if (GUILayout.Button("Browse", GUILayout.Width(65f)))
                {
                    string folder = Directory.Exists(BalanceSheet.DefaultFolder)
                        ? BalanceSheet.DefaultFolder
                        : Application.dataPath;
                    string picked = EditorUtility.OpenFilePanel("Balance CSV", folder, "csv");
                    if (!string.IsNullOrEmpty(picked))
                    {
                        _file = picked;
                        GuessKind(picked);
                    }

                    GUIUtility.ExitGUI();
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                _preview = null;
            }

            if (!string.IsNullOrEmpty(kind.Warning))
            {
                EditorGUILayout.HelpBox(kind.Warning, MessageType.Warning);
            }

            using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_file) || !File.Exists(_file)))
            {
                if (GUILayout.Button("Preview changes (dry run)"))
                {
                    _preview = BalanceSheet.Compare(BalanceSheet.All[_kindIndex], _file);
                }
            }

            if (_preview == null)
            {
                EditorGUILayout.HelpBox(
                    "Nothing is changed until you press Apply on a preview. Blank cells leave a value as it is.",
                    MessageType.Info);
                return;
            }

            DrawPreview();
        }

        private void GuessKind(string path)
        {
            string file = Path.GetFileName(path);
            for (int i = 0; i < BalanceSheet.All.Length; i++)
            {
                if (string.Equals(BalanceSheet.All[i].FileName, file, System.StringComparison.OrdinalIgnoreCase))
                {
                    _kindIndex = i;
                }
            }
        }

        private void DrawPreview()
        {
            EditorGUILayout.LabelField(
                $"{_preview.Rows} rows read, {_preview.ApplicableCount} changes, {_preview.Unchanged} values already match, " +
                $"{_preview.Diffs.Count - _preview.ApplicableCount} rejected");

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            foreach (string problem in _preview.Problems)
            {
                EditorGUILayout.HelpBox(problem, MessageType.Warning);
            }

            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("Asset", EditorStyles.miniBoldLabel, GUILayout.MinWidth(180f));
                GUILayout.Label("Column", EditorStyles.miniBoldLabel, GUILayout.Width(110f));
                GUILayout.Label("Old", EditorStyles.miniBoldLabel, GUILayout.Width(90f));
                GUILayout.Label("New", EditorStyles.miniBoldLabel, GUILayout.Width(90f));
            }

            foreach (BalanceSheet.Diff d in _preview.Diffs)
            {
                Color previous = GUI.color;
                if (!d.Applicable)
                {
                    GUI.color = new Color(1f, 0.6f, 0.6f);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(Path.GetFileNameWithoutExtension(d.Key), GUILayout.MinWidth(180f));
                    GUILayout.Label(d.Column, GUILayout.Width(110f));
                    GUILayout.Label(d.Old, GUILayout.Width(90f));
                    GUILayout.Label(d.Applicable ? d.New : $"{d.New}  ({d.Error})", GUILayout.MinWidth(90f));
                }

                GUI.color = previous;
            }

            EditorGUILayout.EndScrollView();

            using (new EditorGUI.DisabledScope(_preview.ApplicableCount == 0))
            {
                if (GUILayout.Button($"Apply {_preview.ApplicableCount} changes (undoable)", GUILayout.Height(28f)))
                {
                    int set = BalanceSheet.Apply(_preview);
                    Debug.Log($"[Balance] Imported {set} values from {_preview.File}.");
                    _preview = BalanceSheet.Compare(BalanceSheet.All[_kindIndex], _file);
                    GUIUtility.ExitGUI();
                }
            }
        }
    }
}
