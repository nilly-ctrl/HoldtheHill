using HoldTheHill.Features.Enemies;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Manages the Test Start Menu, Settings Menu, Audio Configuration, and Visual Preferences modals.
    /// Toggleable via on-screen buttons or keyboard shortcuts (M for Start Menu, O for Settings Menu).
    /// </summary>
    [AddComponentMenu("Hold the Hill/Graybox/Graybox Menu Manager")]
    public class GrayboxMenuManager : MonoBehaviour
    {
        public static GrayboxMenuManager Instance { get; private set; }

        public bool ShowStartMenu { get; set; }
        public bool ShowSettingsMenu { get; set; }

        [Header("Audio Settings State")]
        public float MasterVolume = 1.0f;
        public float SfxVolume = 0.8f;
        public bool IsMuted = false;

        [Header("Gameplay & Visual State")]
        public bool AutoStartWaves = false;
        public bool ShowFloatingDamage = true;
        public int SpeedIndex = 0;

        private readonly float[] _speeds = { 1f, 2f, 4f };
        private EnemySpawner _spawner;

        private bool _settingsWasOpen;

        private GUIStyle _titleStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _bodyStyle;
        private GUIStyle _btnStyle;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            _spawner = FindAnyObjectByType<EnemySpawner>();
        }

        private void Start()
        {
            LoadSettings();
        }

        // Saved settings win over the inspector defaults, but only when a save file is attached.
        private void LoadSettings()
        {
            if (!GrayboxSave.IsOpen) return;

            SettingsSave saved = GrayboxSave.Data.settings;
            MasterVolume = saved.masterVolume;
            SfxVolume = saved.sfxVolume;
            IsMuted = saved.muted;
            AutoStartWaves = saved.autoStartWaves;
            ShowFloatingDamage = saved.showDamageNumbers;
            EnemyHealthBar.ShowHealthBars = saved.showHealthBars;
            TowerTargetVisualizer.ShowRangeRings = saved.showRangeRings;
            TowerTargetVisualizer.ShowTargetLines = saved.showRangeRings;
            TowerTargetVisualizer.ShowTowerLabels = saved.showRangeRings;
            AudioListener.volume = IsMuted ? 0f : MasterVolume;
        }

        private void StoreSettings()
        {
            SettingsSave saved = GrayboxSave.Data.settings;
            saved.masterVolume = MasterVolume;
            saved.sfxVolume = SfxVolume;
            saved.muted = IsMuted;
            saved.autoStartWaves = AutoStartWaves;
            saved.showDamageNumbers = ShowFloatingDamage;
            saved.showHealthBars = EnemyHealthBar.ShowHealthBars;
            saved.showRangeRings = TowerTargetVisualizer.ShowRangeRings;
            GrayboxSave.MarkDirty();
            GrayboxSave.Save();
        }

        private void Update()
        {
            // Written when the settings panel closes rather than on every slider tick.
            if (_settingsWasOpen && !ShowSettingsMenu) StoreSettings();
            _settingsWasOpen = ShowSettingsMenu;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !GrayboxGameFlow.GameplayActive) return;

            if (keyboard.mKey.wasPressedThisFrame)
            {
                ShowStartMenu = !ShowStartMenu;
                if (ShowStartMenu) ShowSettingsMenu = false;
            }

            if (keyboard.oKey.wasPressedThisFrame)
            {
                ShowSettingsMenu = !ShowSettingsMenu;
                if (ShowSettingsMenu) ShowStartMenu = false;
            }
        }

        private void OnGUI()
        {
            if (!GrayboxGameFlow.GameplayActive) return;

            HoldTheHill.Sandbox.NillyCtrl.GrayboxUi.Apply(); // pixel skin; a no-op without a GrayboxUi in the scene
            InitStyles();

            DrawStartMenuModal();
            DrawSettingsMenuModal();
        }

        private void InitStyles()
        {
            if (_titleStyle != null) return;

            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                richText = true
            };

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                richText = true
            };

            _bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 10,
                richText = true
            };

            _btnStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 10,
                fontStyle = FontStyle.Bold
            };
        }

        private void DrawStartMenuModal()
        {
            if (!ShowStartMenu) return;

            float width = 360f;
            float height = 380f;
            float x = (Screen.width - width) * 0.5f;
            float y = (Screen.height - height) * 0.5f;

            GUI.Box(new Rect(x, y, width, height), GUIContent.none);

            GUILayout.BeginArea(new Rect(x + 14f, y + 14f, width - 28f, height - 28f));
            GUILayout.Label("<b>🏰 HOLD THE HILL</b>\n<color=#80d0ff>Sandbox Test Environment</color>", _titleStyle);
            GUILayout.Space(12);

            if (GUILayout.Button("🎮 Start Campaign (Wave 1)", _btnStyle, GUILayout.Height(34)))
            {
                if (_spawner != null)
                {
                    _spawner.StartNextWave();
                    GrayboxSfx.PlayCue("WaveStart", Vector3.zero);
                }
                ShowStartMenu = false;
            }

            GUILayout.Space(6);
            if (GUILayout.Button("♾️ Launch Endless Procedural Waves", _btnStyle, GUILayout.Height(34)))
            {
                var customSpawner = FindAnyObjectByType<GrayboxCustomSpawner>();
                if (customSpawner != null)
                {
                    customSpawner.SpawnProceduralWave(1);
                    GrayboxSfx.PlayCue("WaveStart", Vector3.zero);
                }
                ShowStartMenu = false;
            }

            GUILayout.Space(6);
            if (GUILayout.Button("⚙️ Open Settings & Audio [O]", _btnStyle, GUILayout.Height(34)))
            {
                ShowStartMenu = false;
                ShowSettingsMenu = true;
            }

            GUILayout.Space(6);
            if (GUILayout.Button("🏆 Achievements & Records [A]", _btnStyle, GUILayout.Height(34)))
            {
                ShowStartMenu = false;
                if (GrayboxAchievements.Instance != null)
                {
                    GrayboxAchievements.Instance.ShowAchievementsWindow = true;
                }
            }

            GUILayout.Space(6);
            if (GUILayout.Button("💰 Dev Boost (+$500 Gold & 5 SP)", _btnStyle, GUILayout.Height(32)))
            {
                GrayboxEconomy.Instance?.EarnGold(500);
                GrayboxSkillTree.Instance?.AddSkillPoints(5);
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Resume Game [Esc / M]", _btnStyle, GUILayout.Height(30)))
            {
                ShowStartMenu = false;
            }

            GUILayout.EndArea();
        }

        private void DrawSettingsMenuModal()
        {
            if (!ShowSettingsMenu) return;

            float width = 380f;
            float height = 440f;
            float x = (Screen.width - width) * 0.5f;
            float y = (Screen.height - height) * 0.5f;

            GUI.Box(new Rect(x, y, width, height), GUIContent.none);

            GUILayout.BeginArea(new Rect(x + 14f, y + 14f, width - 28f, height - 28f));
            GUILayout.Label("<b>⚙️ GAME SETTINGS</b>", _titleStyle);
            GUILayout.Space(10);

            // Audio Section
            GUILayout.Label("<b>Audio & Volume Controls:</b>", _headerStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Master Volume: {Mathf.RoundToInt(MasterVolume * 100)}%", _bodyStyle, GUILayout.Width(130));
            MasterVolume = GUILayout.HorizontalSlider(MasterVolume, 0f, 1f);
            AudioListener.volume = IsMuted ? 0f : MasterVolume;
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"SFX Volume: {Mathf.RoundToInt(SfxVolume * 100)}%", _bodyStyle, GUILayout.Width(130));
            SfxVolume = GUILayout.HorizontalSlider(SfxVolume, 0f, 1f);
            GUILayout.EndHorizontal();

            IsMuted = GUILayout.Toggle(IsMuted, " Mute All Audio Sounds");

            GUILayout.Space(10);

            // Visuals Section
            GUILayout.Label("<b>Visual & HUD Preferences:</b>", _headerStyle);

            bool showRings = TowerTargetVisualizer.ShowRangeRings;
            bool newShowRings = GUILayout.Toggle(showRings, " Show Tower Range Rings & Target Lock Lines");
            if (newShowRings != showRings)
            {
                TowerTargetVisualizer.ShowRangeRings = newShowRings;
                TowerTargetVisualizer.ShowTargetLines = newShowRings;
                TowerTargetVisualizer.ShowTowerLabels = newShowRings;
            }

            bool showHp = EnemyHealthBar.ShowHealthBars;
            bool newShowHp = GUILayout.Toggle(showHp, " Show Floating Enemy Health Bars");
            if (newShowHp != showHp)
            {
                EnemyHealthBar.ShowHealthBars = newShowHp;
            }

            ShowFloatingDamage = GUILayout.Toggle(ShowFloatingDamage, " Show Floating Damage & Healing Popups");

            GUILayout.Space(10);

            // Speed Section
            GUILayout.Label("<b>Game Speed & Simulation:</b>", _headerStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Speed Multiplier:", _bodyStyle, GUILayout.Width(110));
            // Time scale belongs to the flow controller when there is one.
            GrayboxGameFlow flow = GrayboxGameFlow.Instance;
            if (flow != null) SpeedIndex = flow.SpeedIndex;

            for (int i = 0; i < _speeds.Length; i++)
            {
                GUI.color = SpeedIndex == i ? new Color(0.4f, 0.9f, 1f) : Color.white;
                if (GUILayout.Button($"{_speeds[i]}x", _btnStyle, GUILayout.Width(45), GUILayout.Height(24)))
                {
                    SpeedIndex = i;
                    if (flow != null) flow.SetSpeedIndex(i);
                    else Time.timeScale = _speeds[i];
                }
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Space(12);
            if (GUILayout.Button("Close Settings [O]", _btnStyle, GUILayout.Height(30)))
            {
                ShowSettingsMenu = false;
            }

            GUILayout.EndArea();
        }
    }
}
