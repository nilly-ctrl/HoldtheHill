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
        _style ??= new GUIStyle(GUI.skin.label) { fontSize = 15, richText = true };

        GUILayout.BeginArea(new Rect(12f, 12f, 340f, 250f), GUI.skin.box);
        GUILayout.Label("<b>GRAYBOX COMBAT TEST</b>", _style);

        if (_spawner == null)
        {
            GUILayout.Label("<color=red>No EnemySpawner in the scene.</color>", _style);
            GUILayout.EndArea();
            return;
        }

        int living = _spawner.LivingEnemyCount;

        // Anything spawned that is neither alive nor killed walked off the end of the path.
        int leaked = Mathf.Max(0, _spawned - _killed - living);

        GUILayout.Label($"Wave     {_spawner.CurrentWaveNumber} / {_spawner.TotalWaves}", _style);
        GUILayout.Label($"State    {_spawner.CurrentState}", _style);
        GUILayout.Space(6f);
        GUILayout.Label($"Spawned  {_spawned}", _style);
        GUILayout.Label($"Killed   <color=#7fdd7f>{_killed}</color>", _style);
        GUILayout.Label($"Leaked   <color=#ff8080>{leaked}</color>", _style);
        GUILayout.Label($"Alive    {living}", _style);
        GUILayout.Space(6f);
        GUILayout.Label($"Time     {_elapsed:0.0}s   (x{_speeds[_speedIndex]:0})", _style);
        GUILayout.Space(8f);
        GUILayout.Label($"<b>{_nextWaveKey}</b> next wave    <b>{_speedKey}</b> speed    <b>{_restartKey}</b> restart", _style);
        GUILayout.EndArea();
    }
}
