using HoldTheHill.Core.Audio;
using HoldTheHill.Core.Input;
using HoldTheHill.Core.Save;
using HoldTheHill.Features.Hud;
using HoldTheHill.Features.Towers;
using HoldTheHill.Features.Enemies;
using UnityEngine;

namespace HoldTheHill.Features.GameFlow
{
    /// <summary>
    /// The in-run keys that are not building or the Warden: next wave, speed, restart, the skill
    /// tree, achievements and specials windows, range rings and health bars. Every key comes from <see cref="GameControls"/>, so the controls
    /// screen can rebind it, and every action is handed to the flow controller or the spawner.
    /// </summary>
    /// <remarks>
    /// Was the other half of <c>GrayboxHud</c> (now RunCounters and RunHotkeys). Speed and restart need a <see cref="RunFlow"/>
    /// in the scene; without one those keys do nothing, because nothing else may own time.
    /// </remarks>
    [AddComponentMenu("Hold the Hill/Run Hotkeys")]
    public class RunHotkeys : MonoBehaviour
    {
        [Tooltip("The spawner the next-wave key prompts. Found automatically if left empty.")]
        [SerializeField] private EnemySpawner _spawner;

        private RunFlow _flow;

        private void Awake()
        {
            if (_spawner == null) _spawner = FindAnyObjectByType<EnemySpawner>();
            _flow = GetComponent<RunFlow>();
            if (_flow == null) _flow = FindAnyObjectByType<RunFlow>();
        }

        private void Update()
        {
            if (_flow != null && !_flow.IsPlaying) return; // paused or on a menu

            if (GameControls.Pressed(GameControls.NextWave) && _spawner != null)
            {
                _spawner.StartNextWave();
                Sfx.PlayCue("WaveStart", Vector3.zero);
            }

            RunFlow flow = _flow;
            if (GameControls.Pressed(GameControls.Speed) && flow != null) flow.CycleSpeed();
            if (GameControls.Pressed(GameControls.Restart) && flow != null) flow.StartNewRun();

            // The windows over the field (HUD panels; the HUD root shows and hides them).
            if (GameControls.Pressed(GameControls.SkillTree)) SkillTreePanel.Open = !SkillTreePanel.Open;
            if (GameControls.Pressed(GameControls.Achievements)) AchievementsPanel.Open = !AchievementsPanel.Open;
            if (GameControls.Pressed(GameControls.SpecialsPanel)) SpecialsPanel.Open = !SpecialsPanel.Open;

            if (GameControls.Pressed(GameControls.ToggleRings))
            {
                bool toggle = !TowerTargetVisualizer.ShowRangeRings;
                TowerTargetVisualizer.ShowRangeRings = toggle;
                TowerTargetVisualizer.ShowTargetLines = toggle;
                TowerTargetVisualizer.ShowTowerLabels = toggle;
                SaveStore.Data.settings.showRangeRings = toggle; // keeps the settings screen in step
                SaveStore.MarkDirty();
            }

            if (GameControls.Pressed(GameControls.ToggleHealthBars))
            {
                EnemyHealthBar.ShowHealthBars = !EnemyHealthBar.ShowHealthBars;
                SaveStore.Data.settings.showHealthBars = EnemyHealthBar.ShowHealthBars;
                SaveStore.MarkDirty();
            }
        }
    }
}
