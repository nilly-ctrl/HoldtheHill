using HoldTheHill.Features.Enemies;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The in-run keys that are not building or the Warden: next wave, speed, restart, the skill
    /// tree, achievements and specials windows, range rings and health bars. Every key comes from <see cref="GrayboxControls"/>, so the controls
    /// screen can rebind it, and every action is handed to the flow controller or the spawner.
    /// </summary>
    /// <remarks>
    /// Was the other half of <c>GrayboxHud</c>. Speed and restart need a <see cref="GrayboxGameFlow"/>
    /// in the scene; without one those keys do nothing, because nothing else may own time.
    /// </remarks>
    [AddComponentMenu("Hold the Hill/Graybox/Run Hotkeys")]
    public class GrayboxRunHotkeys : MonoBehaviour
    {
        [Tooltip("The spawner the next-wave key prompts. Found automatically if left empty.")]
        [SerializeField] private EnemySpawner _spawner;

        private GrayboxGameFlow _flow;

        private void Awake()
        {
            if (_spawner == null) _spawner = FindAnyObjectByType<EnemySpawner>();
            _flow = GetComponent<GrayboxGameFlow>();
            if (_flow == null) _flow = FindAnyObjectByType<GrayboxGameFlow>();
        }

        private void Update()
        {
            if (_flow != null && !_flow.IsPlaying) return; // paused or on a menu

            if (GrayboxControls.Pressed(GrayboxControls.NextWave) && _spawner != null)
            {
                _spawner.StartNextWave();
                GrayboxSfx.PlayCue("WaveStart", Vector3.zero);
            }

            GrayboxGameFlow flow = _flow;
            if (GrayboxControls.Pressed(GrayboxControls.Speed) && flow != null) flow.CycleSpeed();
            if (GrayboxControls.Pressed(GrayboxControls.Restart) && flow != null) flow.StartNewRun();

            // The windows over the field (HUD panels; the HUD root shows and hides them).
            if (GrayboxControls.Pressed(GrayboxControls.SkillTree)) GrayboxSkillTreePanel.Open = !GrayboxSkillTreePanel.Open;
            if (GrayboxControls.Pressed(GrayboxControls.Achievements)) GrayboxAchievementsPanel.Open = !GrayboxAchievementsPanel.Open;
            if (GrayboxControls.Pressed(GrayboxControls.SpecialsPanel)) GrayboxSpecialsPanel.Open = !GrayboxSpecialsPanel.Open;

            if (GrayboxControls.Pressed(GrayboxControls.ToggleRings))
            {
                bool toggle = !TowerTargetVisualizer.ShowRangeRings;
                TowerTargetVisualizer.ShowRangeRings = toggle;
                TowerTargetVisualizer.ShowTargetLines = toggle;
                TowerTargetVisualizer.ShowTowerLabels = toggle;
                GrayboxSave.Data.settings.showRangeRings = toggle; // keeps the settings screen in step
                GrayboxSave.MarkDirty();
            }

            if (GrayboxControls.Pressed(GrayboxControls.ToggleHealthBars))
            {
                EnemyHealthBar.ShowHealthBars = !EnemyHealthBar.ShowHealthBars;
                GrayboxSave.Data.settings.showHealthBars = EnemyHealthBar.ShowHealthBars;
                GrayboxSave.MarkDirty();
            }
        }
    }
}
