using System.Collections.Generic;
using HoldTheHill.Features.Enemies;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// On-screen readout and keyboard controls for the gray-box combat scene.
/// </summary>
/// <remarks>
/// <see cref="EnemySpawner"/> stops after each wave and waits to be prompted, and nothing
/// in the scene could prompt it, so pressing Play gave you one wave and then silence.
/// This provides that prompt plus the numbers worth having while tuning: how many got
/// through, and how long it took.
///
/// Reads the keyboard through the Input System package rather than the legacy
/// <c>Input</c> class: this project is set to "Input System Package (New)" only, where
/// the old API throws at runtime.
///
/// It sits in <c>_Sandbox/nilly-ctrl/Graybox/</c> with no asmdef, so it compiles into
/// <c>Assembly-CSharp</c> and can see both <c>EnemySpawner</c> and the combat code.
/// Test scaffolding, not part of the game.
/// </remarks>
[AddComponentMenu("Hold the Hill/Graybox/Graybox HUD")]
public class GrayboxHud : MonoBehaviour
{
    [Tooltip("The spawner to read and drive. Found automatically if left empty.")]
    [SerializeField] private EnemySpawner _spawner;

    [Tooltip("Starts the next wave.")]
    [SerializeField] private Key _nextWaveKey = Key.N;

    [Tooltip("Clears the field and restarts from wave 1.")]
    [SerializeField] private Key _restartKey = Key.R;

    [Tooltip("Cycles 1x, 2x, 4x time, for watching long waves quickly.")]
    [SerializeField] private Key _speedKey = Key.T;

    [Tooltip("Toggles tower range rings, target lines, and priority labels.")]
    [SerializeField] private Key _visualsKey = Key.V;

    [Tooltip("Toggles enemy health bars.")]
    [SerializeField] private Key _healthBarKey = Key.H;

    private readonly float[] _speeds = { 1f, 2f, 4f };

    // Rebuilt each OnGUI, held as a field so the readout does not allocate a new
    // list every frame.
    private readonly List<string> _lines = new List<string>();

    // GUILayout adds its own spacing between labels; LineGap accounts for it when
    // measuring so the panel is not a few pixels short by the last line.
    private const float LineGap = 3f;
    private const float PadX = 12f;
    private const float PadY = 10f;

    // Space the tower placer's build bar takes along the bottom of the screen.
    private const float BottomBarHeight = 96f;
    private const float HordePanelHeight = 150f;

    private int _spawned;
    private int _killed;
    private int _speedIndex;
    private float _elapsed;
    private GUIStyle _style;
    private GUIStyle _bigStyle;

    // A line starting with this marker is "<marker>IconName|text": an icon row.
    private const char IconRow = '\u0001';
    private const float IconSize = 32f;
    private const float IconGap = 6f;
    private const char IconSep = '\u0002';
    private const float IconCellGap = 12f;

    private void Awake()
    {
        if (_spawner == null)
        {
            _spawner = FindAnyObjectByType<EnemySpawner>();
        }
    }

    private void OnEnable()
    {
        EnemyHealth.Defeated += OnEnemyDefeated;
        HoldTheHill.Sandbox.NillyCtrl.GrayboxGameFlow.FieldReset += OnFieldReset;

        if (_spawner != null)
        {
            _spawner.onEnemySpawned.AddListener(OnEnemySpawned);
        }
    }

    private void OnDisable()
    {
        EnemyHealth.Defeated -= OnEnemyDefeated;
        HoldTheHill.Sandbox.NillyCtrl.GrayboxGameFlow.FieldReset -= OnFieldReset;

        if (_spawner != null)
        {
            _spawner.onEnemySpawned.RemoveListener(OnEnemySpawned);
        }

        // Leaving Play mode with time scaled up would carry the scale into the editor.
        // With a flow controller in the scene, time scale is its job.
        if (HoldTheHill.Sandbox.NillyCtrl.GrayboxGameFlow.Instance == null)
        {
            Time.timeScale = 1f;
        }
    }

    // The flow controller rewound the field; enemies it removed were neither killed nor leaked.
    private void OnFieldReset()
    {
        _spawned = 0;
        _killed = 0;
        _elapsed = 0f;
    }

    private void OnEnemySpawned(GameObject enemy) => _spawned++;

    private void OnEnemyDefeated(GameObject enemy) => _killed++;

    private void Update()
    {
        if (!HoldTheHill.Sandbox.NillyCtrl.GrayboxGameFlow.GameplayActive)
        {
            return; // paused or on a menu
        }

        _elapsed += Time.unscaledDeltaTime;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard[_nextWaveKey].wasPressedThisFrame && _spawner != null)
        {
            _spawner.StartNextWave();
            HoldTheHill.Sandbox.NillyCtrl.GrayboxSfx.PlayCue("WaveStart", Vector3.zero);
        }

        if (keyboard[_speedKey].wasPressedThisFrame)
        {
            if (HoldTheHill.Sandbox.NillyCtrl.GrayboxGameFlow.Instance != null)
            {
                HoldTheHill.Sandbox.NillyCtrl.GrayboxGameFlow.Instance.CycleSpeed();
            }
            else
            {
                _speedIndex = (_speedIndex + 1) % _speeds.Length;
                Time.timeScale = _speeds[_speedIndex];
            }
        }

        if (keyboard[_restartKey].wasPressedThisFrame)
        {
            Restart();
        }

        if (keyboard[_visualsKey].wasPressedThisFrame)
        {
            bool toggle = !HoldTheHill.Sandbox.NillyCtrl.TowerTargetVisualizer.ShowRangeRings;
            HoldTheHill.Sandbox.NillyCtrl.TowerTargetVisualizer.ShowRangeRings = toggle;
            HoldTheHill.Sandbox.NillyCtrl.TowerTargetVisualizer.ShowTargetLines = toggle;
            HoldTheHill.Sandbox.NillyCtrl.TowerTargetVisualizer.ShowTowerLabels = toggle;
        }

        if (keyboard[_healthBarKey].wasPressedThisFrame)
        {
            HoldTheHill.Sandbox.NillyCtrl.EnemyHealthBar.ShowHealthBars = !HoldTheHill.Sandbox.NillyCtrl.EnemyHealthBar.ShowHealthBars;
        }
    }

    /// <summary>
    /// Clears the field and rewinds to wave 1.
    /// </summary>
    /// <remarks>
    /// Rebuilds in place rather than reloading the scene: the gray-box scene is not in
    /// Build Settings, so <c>SceneManager.LoadScene</c> would fail on it.
    /// </remarks>
    private void Restart()
    {
        // The flow controller also puts towers, gold, hill health and skills back, then waits
        // for the next-wave key so there is a build phase before wave 1.
        if (HoldTheHill.Sandbox.NillyCtrl.GrayboxGameFlow.Instance != null)
        {
            HoldTheHill.Sandbox.NillyCtrl.GrayboxGameFlow.Instance.StartNewRun();
            return;
        }

        foreach (EnemyHealth enemy in FindObjectsByType<EnemyHealth>())
        {
            Destroy(enemy.gameObject);
        }

        _spawned = 0;
        _killed = 0;
        _elapsed = 0f;
        _speedIndex = 0;
        Time.timeScale = 1f;

        if (_spawner != null)
        {
            // Reloads the wave list and resets progress back to the first wave.
            _spawner.SetActiveMap(_spawner.ActiveMapId);
            _spawner.StartNextWave();
        }
    }

    // CalcSize gives the real rendered box for each line, including descenders. Using
    // style.lineHeight instead cropped the tails off some lines and under-counted the total.
    // A progress line under the hill bar: one flag per wave (grey once cleared), a skull on every
    // fifth wave, and a gold marker under the wave being played.
    private void DrawWaveTrack()
    {
        if (_spawner == null || _spawner.TotalWaves < 2)
        {
            return;
        }

        const float width = 280f;
        float x = (Screen.width - width) * 0.5f;
        float y = 62f;
        var track = new Rect(x, y, width, 6f);
        if (!HoldTheHill.Sandbox.NillyCtrl.GrayboxIcons.DrawSliced(track, "WaveTrack", 2) || Event.current.type != EventType.Repaint)
        {
            return;
        }

        int total = _spawner.TotalWaves;
        int current = Mathf.Clamp(_spawner.CurrentWaveIndex, 0, total - 1);
        float inner = width - 4f;
        float done = inner * current / (total - 1);
        if (done >= 2f)
        {
            HoldTheHill.Sandbox.NillyCtrl.GrayboxIcons.DrawSliced(new Rect(x + 2f, y + 2f, done, 2f), "WaveFill", 1, 1, 0, 0);
        }

        for (int i = 0; i < total; i++)
        {
            bool boss = (i + 1) % 5 == 0;
            string name = boss ? "WaveMarkerBoss" : (i < current ? "WaveMarkerDone" : "WaveMarker");
            Texture2D marker = HoldTheHill.Sandbox.NillyCtrl.GrayboxIcons.Get(name);
            if (marker == null)
            {
                continue;
            }

            float mx = x + 2f + Mathf.Round(inner * i / (total - 1));
            GUI.color = boss && i < current ? new Color(1f, 1f, 1f, 0.5f) : Color.white;
            GUI.DrawTexture(new Rect(mx - marker.width / 2, y - marker.height + 1f, marker.width, marker.height), marker);
        }

        GUI.color = Color.white;
        Texture2D now = HoldTheHill.Sandbox.NillyCtrl.GrayboxIcons.Get("WaveMarkerNow");
        if (now != null)
        {
            GUI.DrawTexture(new Rect(x + 2f + Mathf.Round(done) - now.width / 2, y + 6f, now.width, now.height), now);
        }
    }

    private Vector2 Measure(float blankHeight)
    {
        float widest = 0f;
        float height = 0f;
        foreach (string line in _lines)
        {
            if (line.Length == 0)
            {
                height += blankHeight;
                continue;
            }

            if (line[0] == IconRow)
            {
                float rowWidth = 0f;
                foreach (string cell in line.Substring(1).Split(IconSep))
                {
                    string text = cell.Substring(cell.IndexOf('|') + 1);
                    rowWidth += IconSize + IconGap + _bigStyle.CalcSize(new GUIContent(text)).x + IconCellGap;
                }

                widest = Mathf.Max(widest, rowWidth - IconCellGap);
                height += IconSize + LineGap;
                continue;
            }

            Vector2 size = _style.CalcSize(new GUIContent(line));
            widest = Mathf.Max(widest, size.x);
            height += size.y + LineGap;
        }

        return new Vector2(widest, height);
    }

    private void OnGUI()
    {
        if (!HoldTheHill.Sandbox.NillyCtrl.GrayboxGameFlow.GameplayActive)
        {
            return; // IMGUI draws over every Canvas, so it must step aside for the menus
        }

        HoldTheHill.Sandbox.NillyCtrl.GrayboxUi.Apply(); // pixel skin; a no-op without a GrayboxUi in the scene
        _style ??= new GUIStyle(GUI.skin.label) { fontSize = 10, richText = true, wordWrap = false };
        _bigStyle ??= new GUIStyle(_style) { fontSize = 20, alignment = TextAnchor.MiddleLeft };

        _lines.Clear();
        int keysStart = -1;
        _lines.Add("<b>GRAYBOX COMBAT TEST</b>");

        if (_spawner == null)
        {
            _lines.Add("<color=#ff8080>No EnemySpawner in the scene.</color>");
        }
        else
        {
            int living = _spawner.LivingEnemyCount;

            // Anything spawned that is neither alive nor killed walked off the path.
            int leaked = Mathf.Max(0, _spawned - _killed - living);

            int gold = HoldTheHill.Sandbox.NillyCtrl.GrayboxEconomy.Instance != null ? HoldTheHill.Sandbox.NillyCtrl.GrayboxEconomy.Instance.CurrentGold : 500;
            int sp = HoldTheHill.Sandbox.NillyCtrl.GrayboxSkillTree.Instance != null ? HoldTheHill.Sandbox.NillyCtrl.GrayboxSkillTree.Instance.SkillPoints : 0;
            _lines.Add($"{IconRow}ResFoodIcon|<color=#ffd700>${gold}</color>{IconSep}ResWaveIcon|{_spawner.CurrentWaveNumber}/{_spawner.TotalWaves}");
            _lines.Add($"Skills    <color=#80ff80>{sp} SP</color>");
            _lines.Add($"State     {_spawner.CurrentState}");
            _lines.Add(string.Empty);
            _lines.Add($"Spawned   {_spawned}");
            _lines.Add($"Killed    <color=#7fdd7f>{_killed}</color>");
            _lines.Add($"Leaked    <color=#ff8080>{leaked}</color>");
            _lines.Add($"Alive     {living}");
            _lines.Add(string.Empty);
            float speed = HoldTheHill.Sandbox.NillyCtrl.GrayboxGameFlow.Instance != null ? HoldTheHill.Sandbox.NillyCtrl.GrayboxGameFlow.Instance.Speed : _speeds[_speedIndex];
            _lines.Add($"Time      {_elapsed:0.0}s  (x{speed:0})");
            keysStart = _lines.Count; // the blank line before the key list
            _lines.Add(string.Empty);
            _lines.Add($"<b>{_nextWaveKey}</b>  next wave");
            _lines.Add($"<b>M</b>  start menu");
            _lines.Add($"<b>O</b>  settings & audio");
            _lines.Add($"<b>A</b>  achievements");
            _lines.Add($"<b>K</b>  skill tree");
            _lines.Add($"<b>{_speedKey}</b>  cycle speed");
            _lines.Add($"<b>{_visualsKey}</b>  toggle tower visuals");
            _lines.Add($"<b>{_healthBarKey}</b>  toggle health bars");
            _lines.Add($"<b>{_restartKey}</b>  restart");
            if (HoldTheHill.Sandbox.NillyCtrl.GrayboxGameFlow.Instance != null) _lines.Add("<b>Esc</b>  pause");
        }

        // Measure rather than hardcode. CalcSize gives the real rendered box for each
        // line, including descenders — using style.lineHeight instead cropped the tails
        // off "Spawning" and "cycle speed", and under-counted the total so the last
        // line fell outside the panel entirely.
        float blankHeight = _style.lineHeight * 0.45f;
        Vector2 content = Measure(blankHeight);

        // On a short Game view the readout, the horde panel under it and the build bar along the
        // bottom don't all fit. Drop the key list first; the horde panel moves beside the readout
        // further down if it still has no room.
        var customSpawner = FindAnyObjectByType<HoldTheHill.Sandbox.NillyCtrl.GrayboxCustomSpawner>();
        float room = Screen.height - BottomBarHeight - 12f;
        float below = customSpawner != null ? HordePanelHeight + 10f : 0f;
        if (keysStart >= 0 && content.y + PadY * 2f + below > room)
        {
            _lines.RemoveRange(keysStart, _lines.Count - keysStart);
            content = Measure(blankHeight);
        }

        var box = new Rect(12f, 12f, content.x + PadX * 2f, content.y + PadY * 2f);

        GUI.Box(box, GUIContent.none);
        GUILayout.BeginArea(new Rect(box.x + PadX, box.y + PadY, box.width, box.height));

        foreach (string line in _lines)
        {
            // Labels are left unconstrained on purpose: any explicit height clips
            // glyphs that hang below the baseline.
            if (line.Length == 0)
            {
                GUILayout.Space(blankHeight);
            }
            else if (line[0] == IconRow)
            {
                // "<marker>IconName|text" cells: a 32 px icon with its value beside it in the large font.
                GUILayout.BeginHorizontal(GUILayout.Height(IconSize));
                string[] cells = line.Substring(1).Split(IconSep);
                for (int c = 0; c < cells.Length; c++)
                {
                    int bar = cells[c].IndexOf('|');
                    Texture2D icon = HoldTheHill.Sandbox.NillyCtrl.GrayboxIcons.Get(cells[c].Substring(0, bar));
                    if (icon != null)
                    {
                        Rect slot = GUILayoutUtility.GetRect(IconSize, IconSize, GUILayout.Width(IconSize), GUILayout.Height(IconSize));
                        GUI.DrawTexture(slot, icon);
                        GUILayout.Space(IconGap);
                    }

                    GUILayout.Label(cells[c].Substring(bar + 1), _bigStyle, GUILayout.Height(IconSize));
                    if (c < cells.Length - 1)
                    {
                        GUILayout.Space(IconCellGap);
                    }
                }

                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label(line, _style);
            }
        }

        GUILayout.EndArea();

        DrawWaveTrack();

        // Render Custom Horde Spawner Panel below main HUD box
        if (customSpawner != null)
        {
            float panelX = 12f;
            float panelY = box.y + box.height + 10f;
            float panelWidth = Mathf.Max(box.width, 170f);
            float panelHeight = HordePanelHeight;
            if (panelY + panelHeight > Screen.height - BottomBarHeight)
            {
                panelX = box.xMax + 10f;
                panelY = 12f;
            }

            Rect hordeBox = new Rect(panelX, panelY, panelWidth, panelHeight);
            GUI.Box(hordeBox, GUIContent.none);

            GUILayout.BeginArea(new Rect(hordeBox.x + PadX, hordeBox.y + 6f, hordeBox.width - PadX * 2f, hordeBox.height - 12f));
            GUILayout.Label("<color=#ffd700><b>CUSTOM HORDE SPAWNER</b></color>", _style);
            GUILayout.Space(4);

            if (GUILayout.Button("Swarm Rush (40)", GUILayout.Height(20))) customSpawner.SpawnPreset_SwarmRush();
            if (GUILayout.Button("Brute Parade (12)", GUILayout.Height(20))) customSpawner.SpawnPreset_BruteParade();
            if (GUILayout.Button("Phalanx Shield (16)", GUILayout.Height(20))) customSpawner.SpawnPreset_Phalanx();
            if (GUILayout.Button("Hydra Splitters (10)", GUILayout.Height(20))) customSpawner.SpawnPreset_HydraSplitters();
            if (GUILayout.Button("100-Enemy Mega Horde", GUILayout.Height(20))) customSpawner.SpawnPreset_MegaHorde();

            GUILayout.EndArea();
        }
    }
}
