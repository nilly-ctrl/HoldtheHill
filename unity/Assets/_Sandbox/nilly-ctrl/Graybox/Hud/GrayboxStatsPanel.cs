using HoldTheHill.Features.Enemies;
using TMPro;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The tuning readout in the top left corner: food and wave (when the pixel HUD is not already
    /// showing them), skill points, the spawner's state, and how many enemies spawned, died, got
    /// through and are alive. Reads its numbers from <see cref="GrayboxHud"/>.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Graybox/HUD/Stats Panel")]
    public class GrayboxStatsPanel : GrayboxHudPanel
    {
        [SerializeField] private TMP_Text _food;
        [SerializeField] private TMP_Text _skills;
        [SerializeField] private TMP_Text _state;
        [SerializeField] private TMP_Text _spawned;
        [SerializeField] private TMP_Text _killed;
        [SerializeField] private TMP_Text _leaked;
        [SerializeField] private TMP_Text _alive;
        [SerializeField] private TMP_Text _time;
        [SerializeField] private TMP_Text _keys;

        private GrayboxHud _hud;
        private EnemySpawner _spawner;
        private float _nextKeysRefresh;

        // The last numbers shown, so a frame where nothing moved builds no strings.
        private int _foodShown = -1;
        private int _waveShown = -1;
        private int _totalShown = -1;
        private int _skillsShown = -1;
        private int _stateShown = -1;
        private int _spawnedShown = -1;
        private int _killedShown = -1;
        private int _leakedShown = -1;
        private int _aliveShown = -1;
        private int _secondsShown = -1;
        private float _speedShown = -1f;

        public override void Refresh()
        {
            if (_hud == null)
            {
                _hud = FindAnyObjectByType<GrayboxHud>();
            }

            if (_spawner == null)
            {
                _spawner = FindAnyObjectByType<EnemySpawner>();
            }

            if (_hud == null || _spawner == null)
            {
                return;
            }

            // The pixel HUD, when the scene has one, shows food and wave itself.
            bool pixelHud = GrayboxPixelHud.Showing;
            if (_food.gameObject.activeSelf == pixelHud)
            {
                _food.gameObject.SetActive(!pixelHud);
            }

            if (!pixelHud)
            {
                int gold = GrayboxEconomy.Instance != null ? GrayboxEconomy.Instance.CurrentGold : 500;
                if (gold != _foodShown || _spawner.CurrentWaveNumber != _waveShown || _spawner.TotalWaves != _totalShown)
                {
                    _foodShown = gold;
                    _waveShown = _spawner.CurrentWaveNumber;
                    _totalShown = _spawner.TotalWaves;
                    _food.text = $"<color=#ffd700>{PixelGlyphs.IconFood} {gold}</color>  {PixelGlyphs.IconWave} {_waveShown}/{_totalShown}";
                }
            }

            int skillPoints = GrayboxSkillTree.Instance != null ? GrayboxSkillTree.Instance.SkillPoints : 0;
            if (skillPoints != _skillsShown)
            {
                _skillsShown = skillPoints;
                _skills.text = $"Skills  <color=#80ff80>{skillPoints} SP</color>";
            }

            if ((int)_spawner.CurrentState != _stateShown)
            {
                _stateShown = (int)_spawner.CurrentState;
                _state.text = $"State  {_spawner.CurrentState}";
            }

            Set(_spawned, "Spawned  ", _hud.Spawned, ref _spawnedShown, null);
            Set(_killed, "Killed  ", _hud.Killed, ref _killedShown, "#7fdd7f");
            Set(_leaked, "Leaked  ", _hud.Leaked, ref _leakedShown, "#ff8080");
            Set(_alive, "Alive  ", _spawner.LivingEnemyCount, ref _aliveShown, null);

            // With the pixel HUD up the clock is already on screen; only the speed is added here.
            float speed = GrayboxGameFlow.Instance != null ? GrayboxGameFlow.Instance.Speed : Time.timeScale;
            int seconds = Mathf.FloorToInt(_hud.ElapsedSeconds);
            if (seconds != _secondsShown || !Mathf.Approximately(speed, _speedShown))
            {
                _secondsShown = seconds;
                _speedShown = speed;
                _time.text = pixelHud ? $"Speed  x{speed:0}" : $"Time  {_hud.ElapsedSeconds:0}s  (x{speed:0})";
            }

            // Keys can be rebound, so the hint is rebuilt now and then rather than kept.
            if (Time.unscaledTime >= _nextKeysRefresh)
            {
                _nextKeysRefresh = Time.unscaledTime + 0.5f;
                _keys.text = $"{GrayboxControls.Name(GrayboxControls.NextWave)} next wave   {GrayboxControls.Name(GrayboxControls.Pause)} pause";
            }
        }

        private static void Set(TMP_Text text, string label, int value, ref int shown, string colour)
        {
            if (value == shown)
            {
                return;
            }

            shown = value;
            text.text = colour == null ? $"{label}{value}" : $"{label}<color={colour}>{value}</color>";
        }
    }
}
