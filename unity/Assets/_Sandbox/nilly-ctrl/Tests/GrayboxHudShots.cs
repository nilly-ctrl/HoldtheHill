#if UNITY_EDITOR
using HoldTheHill.Features.GameFlow;
using System.Collections;
using System.IO;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Towers;
using HoldTheHill.Sandbox.NillyCtrl;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace HoldTheHill.Sandbox.Graybox.Tests
{
    /// <summary>
    /// Not a test: plays the graybox scene and saves a picture of each HUD state (idle, a tower being
    /// placed, the build menu, the tower card, the skill tree, achievements and specials windows,
    /// the font theme banner) and of the Kitchen Floor level. Explicit; run it with the editor window showing, not
    /// with <c>-batchmode</c>, with the folder for the pictures in HTH_SHOT_DIR.
    /// </summary>
    public class GrayboxHudShots
    {
        private const string ScenePath = "Assets/_Sandbox/nilly-ctrl/Graybox/GrayboxCombatTest.unity";
        private string _dir;

        [UnityTest, Explicit, Category("Screenshots")]
        public IEnumerator SaveAPictureOfEachHudState()
        {
            _dir = System.Environment.GetEnvironmentVariable("HTH_SHOT_DIR");
            if (string.IsNullOrEmpty(_dir)) _dir = Path.Combine(Application.temporaryCachePath, "GrayboxHudShots");
            Directory.CreateDirectory(_dir);

            string saveFile = Path.Combine(_dir, "shots-save.json");
            if (File.Exists(saveFile)) File.Delete(saveFile);
            GrayboxSave.Open(saveFile);

            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
            yield return null;
            yield return null;

            GrayboxGameFlow flow = GrayboxGameFlow.Instance;
            var menus = Object.FindAnyObjectByType<GrayboxMenus>();
            menus.Title.PlayButton.Clicked.Invoke();
            menus.Home.StartButton.Clicked.Invoke();
            yield return null;
            Assert.AreEqual(GameFlowState.Playing, flow.State);

            var placer = Object.FindAnyObjectByType<GrayboxTowerPlacer>();
            yield return Shot("hud-1-idle");

            placer.TogglePlacement(0);
            yield return Shot("hud-2-bar-slot-picked");
            placer.CancelInteraction();

            placer.Place(placer.Catalog[0], new Vector3(-2f, 2f, 0f), 100);
            yield return Shot("hud-3-tower-card");

            placer.CancelInteraction();
            typeof(GrayboxTowerPlacer).GetField("_buildWorldPos", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(placer, new Vector3(2f, 2f, 0f));
            typeof(GrayboxTowerPlacer).GetField("_showBuildMenu", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(placer, true);
            yield return Shot("hud-4-build-menu");
            placer.CancelInteraction();

            Object.FindAnyObjectByType<EnemySpawner>().StartNextWave();
            GrayboxBaseHealth.Instance.TakeBaseDamage(40f, Vector3.zero);
            GrayboxWarden.Instance.Health.TakeDamage(new HoldTheHill.Features.Combat.DamageInfo(6f, null, Vector2.zero));
            yield return new WaitForSeconds(2f);
            yield return Shot("hud-5-wave-running");

            GrayboxSkillTreePanel.Open = true;
            yield return Shot("hud-6-skill-tree");
            GrayboxSkillTreePanel.Open = false;

            GrayboxAchievementsPanel.Open = true;
            yield return Shot("hud-7-achievements");
            GrayboxAchievementsPanel.Open = false;

            GrayboxSpecialsPanel.Open = true;
            yield return Shot("hud-8-specials");
            GrayboxSpecialsPanel.Open = false;

            // What F5 does: the previous font theme, its name on the banner.
            var theme = Object.FindAnyObjectByType<PixelFontTheme>();
            if (theme != null)
            {
                string themeName = theme.StepTheme(-1);
                var banner = Object.FindAnyObjectByType<GrayboxWaveBanner>();
                if (banner != null)
                {
                    banner.Show(themeName, "Title", 3f);
                }
                yield return Shot("hud-9-font-theme-back");
                theme.StepTheme(1);
            }

            // The first hand-drawn level, as the Editor would open it.
            string[] levels = UnityEditor.AssetDatabase.FindAssets("t:Scene", new[] { "Assets/_Sandbox/nilly-ctrl/Levels" });
            if (levels.Length > 0)
            {
                string levelPath = UnityEditor.AssetDatabase.GUIDToAssetPath(levels[0]);
                yield return EditorSceneManager.LoadSceneAsyncInPlayMode(levelPath, new LoadSceneParameters(LoadSceneMode.Single));
                yield return null;
                yield return null;
                var levelMenus = Object.FindAnyObjectByType<GrayboxMenus>();
                if (levelMenus != null)
                {
                    levelMenus.Title.PlayButton.Clicked.Invoke();
                    levelMenus.Home.StartButton.Clicked.Invoke();
                }
                yield return new WaitForSeconds(1f);
                yield return Shot("level-1-kitchen-floor");
            }

            GrayboxSave.Close();
            Debug.Log($"[GrayboxHudShots] Pictures saved in {_dir}");
        }

        private IEnumerator Shot(string name)
        {
            yield return null;
            yield return null;
            ScreenCapture.CaptureScreenshot(Path.Combine(_dir, name + ".png"));
            yield return null;
            yield return null;
        }
    }
}
#endif
