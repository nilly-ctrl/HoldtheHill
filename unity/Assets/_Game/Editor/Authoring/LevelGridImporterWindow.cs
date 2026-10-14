using HoldTheHill.EditorTools.Builders;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace HoldTheHill.EditorTools.Authoring
{
    /// <summary>
    /// Pick a hand-drawn text-grid map, check it, see what it will look like, and hand it to the
    /// existing <see cref="LevelBuilder"/>.
    /// </summary>
    /// <remarks>
    /// The map is checked here in C# with the rules of <c>level_grid.py</c>. The floor picture and
    /// the route json still come from <c>build_level.py --install</c>, which this window can start
    /// (it needs python and Pillow on the PATH). Building the scene is LevelBuilder.Build,
    /// unchanged: it rebuilds the graybox scene first and saves the level as its own scene.
    /// </remarks>
    internal sealed class LevelGridImporterWindow : EditorWindow
    {
        private const string LevelsRoot = "Assets/_Game/Scenes/Levels";

        private string[] _mapPaths = new string[0];
        private string[] _mapLabels = new string[0];
        private int _selected;
        private LevelGridMap _map;
        private DateTime _loadedAt;
        private bool _showAscii;
        private Vector2 _scroll;
        private GUIStyle _mono;
        private GUIStyle _cellLabel;

        [MenuItem("Tools/Hold the Hill/Authoring/Level Grid Importer")]
        public static void Open()
        {
            var window = GetWindow<LevelGridImporterWindow>("Level Grid Importer");
            window.minSize = new Vector2(420f, 480f);
            window.Show();
        }

        private static string MapsFolder =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "_Game/Scenes/Levels/Source~/maps"));

        private static string SourceFolder =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "_Game/Scenes/Levels/Source~"));

        private void OnEnable()
        {
            Refresh();
        }

        private void Refresh()
        {
            string previous = _selected >= 0 && _selected < _mapPaths.Length ? _mapPaths[_selected] : null;
            string folder = MapsFolder;
            var paths = new List<string>();
            if (Directory.Exists(folder))
            {
                paths.AddRange(Directory.GetFiles(folder, "*.txt", SearchOption.AllDirectories));
                paths.Sort(StringComparer.OrdinalIgnoreCase);
            }

            _mapPaths = paths.ToArray();
            _mapLabels = new string[_mapPaths.Length];
            for (int i = 0; i < _mapPaths.Length; i++)
            {
                _mapLabels[i] = Path.GetRelativePath(folder, _mapPaths[i]).Replace('\\', '/');
            }

            _selected = previous != null ? Math.Max(0, Array.IndexOf(_mapPaths, previous)) : 0;
            Reload();
        }

        private void Reload()
        {
            if (_selected < 0 || _selected >= _mapPaths.Length)
            {
                _map = null;
                return;
            }

            _map = LevelGridMap.Load(_mapPaths[_selected]);
            _loadedAt = File.GetLastWriteTimeUtc(_mapPaths[_selected]);
        }

        private void OnGUI()
        {
            EnsureStyles();

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                int picked = EditorGUILayout.Popup(_selected, _mapLabels, EditorStyles.toolbarPopup);
                if (picked != _selected)
                {
                    _selected = picked;
                    Reload();
                }

                if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(60f)))
                {
                    Refresh();
                }

                if (GUILayout.Button("Reveal", EditorStyles.toolbarButton, GUILayout.Width(55f)) && _map != null)
                {
                    EditorUtility.RevealInFinder(_mapPaths[_selected]);
                }
            }

            if (_mapPaths.Length == 0)
            {
                EditorGUILayout.HelpBox($"No .txt maps found in {MapsFolder}.", MessageType.Info);
                return;
            }

            // Pick up edits made in a text editor.
            if (_map != null && File.GetLastWriteTimeUtc(_mapPaths[_selected]) != _loadedAt)
            {
                Reload();
            }

            if (_map == null)
            {
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            EditorGUILayout.LabelField(_map.Name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Theme", _map.Theme);
            if (_map.Width > 0)
            {
                EditorGUILayout.LabelField("Size", $"{_map.Width} x {_map.Height} cells");
            }

            foreach (string error in _map.Errors)
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
            }

            foreach (string warning in _map.Warnings)
            {
                EditorGUILayout.HelpBox(warning, MessageType.Warning);
            }

            if (_map.Valid)
            {
                EditorGUILayout.LabelField("Road", $"{_map.Route.Count} cells, {_map.Waypoints.Count} waypoints");
                EditorGUILayout.LabelField("Buildable cells", _map.BuildCells.Count.ToString());
                EditorGUILayout.LabelField("Asset name", _map.AssetName);
            }

            if (_map.Width > 0)
            {
                DrawPreview();
            }

            _showAscii = EditorGUILayout.Foldout(_showAscii, "ASCII (+ marks a buildable cell)", true);
            if (_showAscii && _map.Valid)
            {
                EditorGUILayout.SelectableLabel(
                    _map.ToAscii(), _mono, GUILayout.Height(_mono.lineHeight * (_map.Height + 1)));
            }

            if (_map.Valid)
            {
                DrawBuildSection();
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawPreview()
        {
            float available = position.width - 30f;
            float cell = Mathf.Clamp(Mathf.Floor(available / _map.Width), 6f, 28f);
            Rect area = GUILayoutUtility.GetRect(cell * _map.Width, cell * _map.Height);
            area.width = cell * _map.Width;

            EditorGUI.DrawRect(new Rect(area.x - 1f, area.y - 1f, area.width + 2f, area.height + 2f), Color.black);
            for (int y = 0; y < _map.Height; y++)
            {
                for (int x = 0; x < _map.Width; x++)
                {
                    var rect = new Rect(area.x + x * cell, area.y + y * cell, cell, cell);
                    LevelGridMap.Kind kind = _map.KindAt(x, y);
                    EditorGUI.DrawRect(rect, ColourOf(kind));
                    if (_map.IsBuildCell(x, y))
                    {
                        float dot = Mathf.Max(2f, cell * 0.28f);
                        EditorGUI.DrawRect(
                            new Rect(rect.center.x - dot * 0.5f, rect.center.y - dot * 0.5f, dot, dot),
                            new Color(0.15f, 0.6f, 0.2f));
                    }

                    if (cell >= 14f && (kind == LevelGridMap.Kind.Spawn || kind == LevelGridMap.Kind.Hill))
                    {
                        GUI.Label(rect, kind == LevelGridMap.Kind.Spawn ? "S" : "H", _cellLabel);
                    }
                }
            }

            // The corner points the enemies steer through.
            if (_map.Valid)
            {
                foreach ((int X, int Y) w in _map.Waypoints)
                {
                    var rect = new Rect(area.x + w.X * cell + cell * 0.35f, area.y + w.Y * cell + cell * 0.35f,
                        cell * 0.3f, cell * 0.3f);
                    EditorGUI.DrawRect(rect, new Color(1f, 1f, 1f, 0.9f));
                }
            }
        }

        private static Color ColourOf(LevelGridMap.Kind kind)
        {
            switch (kind)
            {
                case LevelGridMap.Kind.Road: return new Color(0.36f, 0.3f, 0.28f);
                case LevelGridMap.Kind.Spawn: return new Color(0.85f, 0.25f, 0.2f);
                case LevelGridMap.Kind.Hill: return new Color(0.95f, 0.8f, 0.2f);
                case LevelGridMap.Kind.Cabinet: return new Color(0.33f, 0.22f, 0.14f);
                case LevelGridMap.Kind.Fridge: return new Color(0.75f, 0.8f, 0.85f);
                case LevelGridMap.Kind.Leg: return new Color(0.5f, 0.35f, 0.2f);
                case LevelGridMap.Kind.Mat: return new Color(0.7f, 0.3f, 0.3f);
                case LevelGridMap.Kind.Crumbs: return new Color(0.82f, 0.7f, 0.45f);
                case LevelGridMap.Kind.Spill: return new Color(0.3f, 0.5f, 0.8f);
                default: return new Color(0.76f, 0.7f, 0.58f);
            }
        }

        private void DrawBuildSection()
        {
            string name = _map.AssetName;
            string folder = $"{LevelsRoot}/{name}";
            bool hasJson = AssetDatabase.LoadAssetAtPath<TextAsset>($"{folder}/{name}.json") != null;
            bool hasGround = AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/{name}_Ground.png") != null;
            bool hasScene = File.Exists(Path.Combine(
                Path.GetDirectoryName(Application.dataPath) ?? string.Empty, LevelBuilder.ScenePath(name)));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Build", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Floor picture", hasGround ? "present" : "missing");
            EditorGUILayout.LabelField("Route json", hasJson ? "present" : "missing");
            EditorGUILayout.LabelField("Scene", hasScene ? LevelBuilder.ScenePath(name) : "not built yet");

            if (!hasJson || !hasGround)
            {
                EditorGUILayout.HelpBox(
                    "The scene builder needs the floor picture and route json first. " +
                    "Generate them with build_level.py --install (button below, needs python with Pillow).",
                    MessageType.Info);
            }

            if (GUILayout.Button("Generate floor picture + json (python build_level.py --install)"))
            {
                RunBuildLevelPython();
            }

            using (new EditorGUI.DisabledScope(!hasJson || !hasGround))
            {
                if (GUILayout.Button("Build level scene (LevelBuilder)"))
                {
                    BuildScene(name);
                }
            }
        }

        private void RunBuildLevelPython()
        {
            string script = Path.Combine(SourceFolder, "build_level.py");
            if (!File.Exists(script))
            {
                EditorUtility.DisplayDialog("Level Grid Importer", $"{script} is missing.", "OK");
                return;
            }

            if (!EditorUtility.DisplayDialog(
                    "Generate floor picture",
                    $"Run build_level.py --install for {_mapLabels[_selected]}?\n\n" +
                    $"This writes {_map.AssetName}_Ground.png and {_map.AssetName}.json under Levels/{_map.AssetName}.",
                    "Run", "Cancel"))
            {
                return;
            }

            var start = new ProcessStartInfo("python", $"\"{script}\" \"{_mapPaths[_selected]}\" --install")
            {
                WorkingDirectory = SourceFolder,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            try
            {
                using (Process process = Process.Start(start))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string errors = process.StandardError.ReadToEnd();
                    process.WaitForExit(120000);
                    if (process.ExitCode == 0)
                    {
                        Debug.Log($"[LevelGridImporter] build_level.py finished.\n{output}");
                    }
                    else
                    {
                        Debug.LogError($"[LevelGridImporter] build_level.py exited with {process.ExitCode}.\n{output}\n{errors}");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[LevelGridImporter] Could not start python: {e.Message}");
            }

            AssetDatabase.Refresh();
            Repaint();
        }

        private static void BuildScene(string name)
        {
            if (!EditorUtility.DisplayDialog(
                    "Build level scene",
                    $"This rebuilds the graybox scene and saves {LevelBuilder.ScenePath(name)}.\n\n" +
                    "Other AI sessions or a running Play mode will clash with it. Continue?",
                    "Build", "Cancel"))
            {
                return;
            }

            var open = new UnityEngine.SceneManagement.Scene[EditorSceneManager.sceneCount];
            for (int i = 0; i < open.Length; i++)
            {
                open[i] = EditorSceneManager.GetSceneAt(i);
            }

            if (!EditorSceneManager.SaveModifiedScenesIfUserWantsTo(open))
            {
                return;
            }

            LevelBuilder.Build(name);
        }

        private void EnsureStyles()
        {
            if (_mono == null)
            {
                _mono = new GUIStyle(EditorStyles.textArea)
                {
                    font = Font.CreateDynamicFontFromOSFont("Consolas", 12),
                    wordWrap = false,
                };
            }

            if (_cellLabel == null)
            {
                _cellLabel = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white },
                    fontSize = 10,
                };
            }
        }
    }
}
