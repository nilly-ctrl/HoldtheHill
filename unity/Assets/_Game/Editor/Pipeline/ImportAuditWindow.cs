using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HoldTheHill.EditorTools.Pipeline
{
    /// <summary>
    /// Lists sandbox textures whose import settings differ from <see cref="ImportPresets"/>,
    /// with a per-row and a fix-all button. Fixes are undoable (Edit > Undo) and re-import the asset.
    /// Menu: Tools > Hold the Hill > Pipeline > Audit Import Settings.
    /// </summary>
    public class ImportAuditWindow : EditorWindow
    {
        private sealed class Row
        {
            public string Path;
            public ImportPresets.Rule Rule;
            public List<ImportPresets.Deviation> Deviations;
        }

        private readonly List<Row> _rows = new List<Row>();
        private int _scanned;
        private bool _scanDone;
        private Vector2 _scroll;

        [MenuItem("Tools/Hold the Hill/Pipeline/Audit Import Settings")]
        public static void Open()
        {
            var window = GetWindow<ImportAuditWindow>("Import Audit");
            window.minSize = new Vector2(520, 320);
            window.Scan();
        }

        private void Scan()
        {
            _rows.Clear();
            _scanned = 0;
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/_Game" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                ImportPresets.Rule rule = ImportPresets.Find(path);
                if (rule == null) continue;
                if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) continue;

                _scanned++;
                List<ImportPresets.Deviation> deviations = ImportPresets.Check(rule, importer);
                if (deviations.Count > 0)
                {
                    _rows.Add(new Row { Path = path, Rule = rule, Deviations = deviations });
                }
            }

            _rows.Sort((a, b) => string.CompareOrdinal(a.Path, b.Path));
            _scanDone = true;
        }

        private static void Fix(IEnumerable<Row> rows, string undoName)
        {
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (Row row in rows)
                {
                    if (!(AssetImporter.GetAtPath(row.Path) is TextureImporter importer)) continue;
                    Undo.RecordObject(importer, undoName);
                    ImportPresets.Apply(row.Rule, importer);
                    EditorUtility.SetDirty(importer);
                    importer.SaveAndReimport();
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Rescan", EditorStyles.toolbarButton, GUILayout.Width(70))) Scan();
                GUI.enabled = _rows.Count > 0;
                if (GUILayout.Button($"Fix all ({_rows.Count})", EditorStyles.toolbarButton, GUILayout.Width(100)))
                {
                    var all = new List<Row>(_rows);
                    Fix(all, "Fix texture import settings");
                    Scan();
                    GUIUtility.ExitGUI();
                }

                GUI.enabled = true;
                GUILayout.FlexibleSpace();
                GUILayout.Label(_scanDone ? $"{_scanned} textures checked, {_rows.Count} deviate" : "Not scanned", EditorStyles.miniLabel);
            }

            if (_scanDone && _rows.Count == 0)
            {
                EditorGUILayout.HelpBox("Every audited texture matches its folder preset.", MessageType.Info);
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            Row fixedRow = null;
            foreach (Row row in _rows)
            {
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button(row.Path.Replace("Assets/_Game/", string.Empty), EditorStyles.linkLabel))
                        {
                            Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(row.Path);
                            EditorGUIUtility.PingObject(Selection.activeObject);
                        }

                        if (GUILayout.Button("Fix", GUILayout.Width(50))) fixedRow = row;
                    }

                    EditorGUILayout.LabelField(row.Rule.Note, EditorStyles.miniLabel);
                    foreach (ImportPresets.Deviation deviation in row.Deviations)
                    {
                        EditorGUILayout.LabelField("  " + deviation, EditorStyles.miniLabel);
                    }
                }
            }

            EditorGUILayout.EndScrollView();

            if (fixedRow != null)
            {
                Fix(new[] { fixedRow }, "Fix texture import settings");
                Scan();
                GUIUtility.ExitGUI();
            }
        }
    }
}
