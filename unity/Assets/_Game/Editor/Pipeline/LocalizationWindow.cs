using HoldTheHill.Core.Localization;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HoldTheHill.EditorTools.Pipeline
{
    /// <summary>
    /// Three tools around <see cref="StringTable"/>: a heuristic list of hard-coded UI strings,
    /// CSV export and import with a diff preview, and a check for keys used in code but missing from
    /// the table. Menu: Tools > Hold the Hill > Pipeline > Localization Tools.
    /// </summary>
    public class LocalizationWindow : EditorWindow
    {
        private static readonly string[] TabNames = { "Hard-coded strings", "CSV", "Missing keys" };

        private StringTable _table;
        private int _tab;
        private Vector2 _scroll;

        private List<LocalizationTools.Finding> _hardCoded;
        private List<LocalizationTools.Finding> _missing;
        private List<StringChange> _pending;
        private string _pendingPath;
        private string _message;

        [MenuItem("Tools/Hold the Hill/Pipeline/Localization Tools")]
        public static void Open()
        {
            var window = GetWindow<LocalizationWindow>("Localization");
            window.minSize = new Vector2(560, 360);
            if (window._table == null)
            {
                string[] guids = AssetDatabase.FindAssets("t:StringTable");
                if (guids.Length > 0)
                {
                    window._table = AssetDatabase.LoadAssetAtPath<StringTable>(AssetDatabase.GUIDToAssetPath(guids[0]));
                }
            }
        }

        private void OnGUI()
        {
            _table = (StringTable)EditorGUILayout.ObjectField("String table", _table, typeof(StringTable), false);
            _tab = GUILayout.Toolbar(_tab, TabNames);
            if (!string.IsNullOrEmpty(_message))
            {
                EditorGUILayout.HelpBox(_message, MessageType.Info);
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            switch (_tab)
            {
                case 0: DrawHardCoded(); break;
                case 1: DrawCsv(); break;
                default: DrawMissing(); break;
            }

            EditorGUILayout.EndScrollView();
        }

        // ---- tab 0 ----

        private void DrawHardCoded()
        {
            EditorGUILayout.HelpBox(
                "A heuristic: string literals in sandbox code and text fields in prefabs, scenes and data assets that look like text a player reads. Expect false positives and misses.",
                MessageType.None);
            if (GUILayout.Button("Scan sandbox")) {
                _hardCoded = LocalizationTools.ScanCode();
                _hardCoded.AddRange(LocalizationTools.ScanAssets());
                _message = $"{_hardCoded.Count} candidates.";
            }

            if (_hardCoded == null) return;
            foreach (LocalizationTools.Finding finding in _hardCoded)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button($"{finding.Path.Replace("Assets/_Game/", string.Empty)}:{finding.Line}",
                            EditorStyles.linkLabel, GUILayout.Width(300)))
                    {
                        Object asset = AssetDatabase.LoadMainAssetAtPath(finding.Path);
                        if (asset != null) AssetDatabase.OpenAsset(asset, finding.Line);
                    }

                    EditorGUILayout.SelectableLabel(finding.Text, GUILayout.Height(EditorGUIUtility.singleLineHeight));
                }
            }
        }

        // ---- tab 1 ----

        private void DrawCsv()
        {
            using (new EditorGUI.DisabledScope(_table == null))
            {
                if (GUILayout.Button("Export to CSV..."))
                {
                    string path = EditorUtility.SaveFilePanel("Export string table", string.Empty, _table.name + ".csv", "csv");
                    if (!string.IsNullOrEmpty(path))
                    {
                        LocalizationTools.ExportCsv(_table, path);
                        _message = "Exported " + _table.Count + " keys to " + path;
                    }
                }

                if (GUILayout.Button("Import from CSV (preview first)..."))
                {
                    string path = EditorUtility.OpenFilePanel("Import string table", string.Empty, "csv");
                    if (!string.IsNullOrEmpty(path))
                    {
                        try
                        {
                            _pending = LocalizationTools.PreviewImport(_table, path, out _);
                            _pendingPath = path;
                            _message = _pending.Count == 0 ? "The CSV matches the table; nothing to import." : null;
                        }
                        catch (System.Exception e)
                        {
                            _pending = null;
                            _message = "Could not read CSV: " + e.Message;
                        }
                    }
                }
            }

            if (_pending == null || _pending.Count == 0) return;

            EditorGUILayout.LabelField($"{_pending.Count} changes from {System.IO.Path.GetFileName(_pendingPath)}", EditorStyles.boldLabel);
            foreach (StringChange change in _pending)
            {
                EditorGUILayout.LabelField(change.ToString());
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Apply (undoable)"))
                {
                    Undo.RecordObject(_table, "Import string table CSV");
                    StringTableCsv.Apply(_table, _pending);
                    EditorUtility.SetDirty(_table);
                    AssetDatabase.SaveAssetIfDirty(_table);
                    _message = $"Applied {_pending.Count} changes.";
                    _pending = null;
                }

                if (GUILayout.Button("Discard"))
                {
                    _pending = null;
                }
            }
        }

        // ---- tab 2 ----

        private void DrawMissing()
        {
            if (GUILayout.Button("Check keys used in code"))
            {
                List<LocalizationTools.Finding> usages = LocalizationTools.FindKeyUsages();
                _missing = LocalizationTools.MissingKeys(_table, usages);
                _message = $"{usages.Count} Loc.Get(\"...\") calls, {_missing.Count} with a key missing from the table.";
            }

            if (_missing == null) return;

            using (new EditorGUI.DisabledScope(_table == null || _missing.Count == 0))
            {
                if (GUILayout.Button("Add missing keys (English text = the key)"))
                {
                    Undo.RecordObject(_table, "Add missing string keys");
                    foreach (LocalizationTools.Finding usage in _missing)
                    {
                        if (!_table.HasKey(usage.Text)) _table.Set(usage.Text, StringTable.DefaultLanguage, usage.Text);
                    }

                    EditorUtility.SetDirty(_table);
                    AssetDatabase.SaveAssetIfDirty(_table);
                    _missing = null;
                    return;
                }
            }

            foreach (LocalizationTools.Finding usage in _missing)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button($"{usage.Path.Replace("Assets/_Game/", string.Empty)}:{usage.Line}",
                            EditorStyles.linkLabel, GUILayout.Width(300)))
                    {
                        Object asset = AssetDatabase.LoadMainAssetAtPath(usage.Path);
                        if (asset != null) AssetDatabase.OpenAsset(asset, usage.Line);
                    }

                    EditorGUILayout.LabelField(usage.Text);
                }
            }
        }
    }
}
