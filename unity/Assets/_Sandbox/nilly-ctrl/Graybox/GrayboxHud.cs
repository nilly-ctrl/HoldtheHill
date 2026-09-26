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

    private readonly float[] _speeds = { 1f, 2f, 4f };

    // Rebuilt each OnGUI, held as a field so the readout does not allocate a new
    // list every frame.
    private readonly List<string> _lines = new List<string>();

    // GUILayout adds its own spacing between labels; LineGap accounts for it when
    // measuring so the panel is not a few pixels short by the last line.
    private const float LineGap = 3f;
    private const float PadX = 12f;
    private const float PadY = 10f;

    private int _spawned;
    private int _killed;
    private int _speedIndex;
    private float _elapsed;
    private GUIStyle _style;

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

        if (_spawner != null)
        {
            _spawner.onEnemySpawned.AddListener(OnEnemySpawned);
        }
    }

    private void OnDisable()
    {
        EnemyHealth.Defeated -= OnEnemyDefeated;

        if (_spawner != null)
        {
            _spawner.onEnemySpawned.RemoveListener(OnEnemySpawned);
        }

        // Leaving Play mode with time scaled up would carry the scale into the editor.
        Time.timeScale = 1f;
    }

    private void OnEnemySpawned(GameObject enemy) => _spawned++;

    private void OnEnemyDefeated(GameObject enemy) => _killed++;

    private void Update()
    {
        _elapsed += Time.unscaledDeltaTime;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard[_nextWaveKey].wasPressedThisFrame && _spawner != null)
        {
            _spawner.StartNextWave();
        }

        if (keyboard[_speedKey].wasPressedThisFrame)
        {
            _speedIndex = (_speedIndex + 1) % _speeds.Length;
            Time.timeScale = _speeds[_speedIndex];
        }

        if (keyboard[_restartKey].wasPressedThisFrame)
        {
            Restart();
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

    private void OnGUI()
    {
        _style ??= new GUIStyle(GUI.skin.label) { fontSize = 15, richText = true, wordWrap = false };

        _lines.Clear();
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

            _lines.Add($"Wave      {_spawner.CurrentWaveNumber} / {_spawner.TotalWaves}");
            _lines.Add($"State     {_spawner.CurrentState}");
            _lines.Add(string.Empty);
            _lines.Add($"Spawned   {_spawned}");
            _lines.Add($"Killed    <color=#7fdd7f>{_killed}</color>");
            _lines.Add($"Leaked    <color=#ff8080>{leaked}</color>");
            _lines.Add($"Alive     {living}");
            _lines.Add(string.Empty);
            _lines.Add($"Time      {_elapsed:0.0}s  (x{_speeds[_speedIndex]:0})");
            _lines.Add(string.Empty);
            // One control per line. Putting all three on one row is what overflowed
            // the box, and a single long line is the first thing to clip on a
            // high-DPI display where IMGUI renders wider than you expect.
            _lines.Add($"<b>{_nextWaveKey}</b>  next wave");
            _lines.Add($"<b>{_speedKey}</b>  cycle speed");
            _lines.Add($"<b>{_restartKey}</b>  restart");
        }

        // Measure rather than hardcode. CalcSize gives the real rendered box for each
        // line, including descenders — using style.lineHeight instead cropped the tails
        // off "Spawning" and "cycle speed", and under-counted the total so the last
        // line fell outside the panel entirely.
        float widest = 0f;
        float height = 0f;
        float blankHeight = _style.lineHeight * 0.45f;

        foreach (string line in _lines)
        {
            if (line.Length == 0)
            {
                height += blankHeight;
                continue;
            }

            Vector2 size = _style.CalcSize(new GUIContent(line));
            widest = Mathf.Max(widest, size.x);
            height += size.y + LineGap;
        }

        var box = new Rect(12f, 12f, widest + PadX * 2f, height + PadY * 2f);

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
            else
            {
                GUILayout.Label(line, _style);
            }
        }

        GUILayout.EndArea();
    }
}
