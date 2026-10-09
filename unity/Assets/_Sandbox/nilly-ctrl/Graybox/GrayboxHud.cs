using HoldTheHill.Features.Enemies;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Keyboard controls and run counters for the gray-box combat scene.
/// </summary>
/// <remarks>
/// <see cref="EnemySpawner"/> stops after each wave and waits to be prompted, and nothing
/// in the scene could prompt it, so pressing Play gave you one wave and then silence.
/// This provides that prompt plus the numbers worth having while tuning: how many got
/// through, and how long it took. It draws nothing itself: the readout is the
/// <see cref="HoldTheHill.Sandbox.NillyCtrl.GrayboxStatsPanel"/> on the HUD canvas.
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

    // Keys come from GrayboxControls (next wave, restart, speed, range rings, health bars), so
    // the controls screen can rebind them.

    private readonly float[] _speeds = { 1f, 2f, 4f };

    private int _spawned;
    private int _killed;
    private int _speedIndex;
    private float _elapsed;

    /// <summary>Seconds since the run started, for other HUD pieces to show.</summary>
    public float ElapsedSeconds => _elapsed;

    /// <summary>Enemies spawned since the run (or the last field reset) began.</summary>
    public int Spawned => _spawned;

    /// <summary>Enemies killed since the run began.</summary>
    public int Killed => _killed;

    /// <summary>Enemies that got past the towers: spawned, and neither killed nor still alive.</summary>
    public int Leaked => _spawner != null ? Mathf.Max(0, _spawned - _killed - _spawner.LivingEnemyCount) : 0;

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

        if (HoldTheHill.Sandbox.NillyCtrl.GrayboxControls.Pressed(HoldTheHill.Sandbox.NillyCtrl.GrayboxControls.NextWave) && _spawner != null)
        {
            _spawner.StartNextWave();
            HoldTheHill.Sandbox.NillyCtrl.GrayboxSfx.PlayCue("WaveStart", Vector3.zero);
        }

        if (HoldTheHill.Sandbox.NillyCtrl.GrayboxControls.Pressed(HoldTheHill.Sandbox.NillyCtrl.GrayboxControls.Speed))
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

        if (HoldTheHill.Sandbox.NillyCtrl.GrayboxControls.Pressed(HoldTheHill.Sandbox.NillyCtrl.GrayboxControls.Restart))
        {
            Restart();
        }

        if (HoldTheHill.Sandbox.NillyCtrl.GrayboxControls.Pressed(HoldTheHill.Sandbox.NillyCtrl.GrayboxControls.ToggleRings))
        {
            bool toggle = !HoldTheHill.Sandbox.NillyCtrl.TowerTargetVisualizer.ShowRangeRings;
            HoldTheHill.Sandbox.NillyCtrl.TowerTargetVisualizer.ShowRangeRings = toggle;
            HoldTheHill.Sandbox.NillyCtrl.TowerTargetVisualizer.ShowTargetLines = toggle;
            HoldTheHill.Sandbox.NillyCtrl.TowerTargetVisualizer.ShowTowerLabels = toggle;
            HoldTheHill.Sandbox.NillyCtrl.GrayboxSave.Data.settings.showRangeRings = toggle; // keeps the settings screen in step
            HoldTheHill.Sandbox.NillyCtrl.GrayboxSave.MarkDirty();
        }

        if (HoldTheHill.Sandbox.NillyCtrl.GrayboxControls.Pressed(HoldTheHill.Sandbox.NillyCtrl.GrayboxControls.ToggleHealthBars))
        {
            HoldTheHill.Sandbox.NillyCtrl.EnemyHealthBar.ShowHealthBars = !HoldTheHill.Sandbox.NillyCtrl.EnemyHealthBar.ShowHealthBars;
            HoldTheHill.Sandbox.NillyCtrl.GrayboxSave.Data.settings.showHealthBars = HoldTheHill.Sandbox.NillyCtrl.EnemyHealthBar.ShowHealthBars;
            HoldTheHill.Sandbox.NillyCtrl.GrayboxSave.MarkDirty();
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
}
