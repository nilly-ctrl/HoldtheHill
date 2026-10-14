#if UNITY_EDITOR
using HoldTheHill.Features.Hud;
using HoldTheHill.Features.Progression;
using HoldTheHill.Features.Specials;
using System.Collections;
using System.Collections.Generic;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Sandbox.NillyCtrl;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace HoldTheHill.Sandbox.Graybox.Tests
{
    /// <summary>
    /// The uGUI windows that replaced the IMGUI ones: skill tree, achievements, specials, and the
    /// sprite health bar. Each test spawns the HUD prefab with a bare scene object for the
    /// system the window reads; no flow controller, so the HUD root treats the game as playing.
    /// </summary>
    public class GrayboxHudWindowsTests
    {
        private const string HudPath = "Assets/_Sandbox/nilly-ctrl/Graybox/Prefabs/GrayboxHud.prefab";

        private readonly List<Object> _made = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _made)
            {
                if (o != null) Object.DestroyImmediate(o);
            }

            _made.Clear();
            GrayboxSkillTreePanel.Open = false;
            GrayboxAchievementsPanel.Open = false;
            GrayboxSpecialsPanel.Open = false;
        }

        [UnityTest]
        public IEnumerator SkillTreePanel_ListsEveryNode_AndItsButtonBuysOne()
        {
            GrayboxSkillTree tree = Systems().AddComponent<GrayboxSkillTree>();
            tree.Configure(GrayboxTestContent.SkillTree());
            tree.AddSkillPoints(50);

            var panel = Hud().GetComponentInChildren<GrayboxSkillTreePanel>(true);
            GrayboxSkillTreePanel.Open = true;
            yield return null;
            yield return null;

            Assert.IsTrue(panel.gameObject.activeSelf, "the HUD root shows an open window");
            Assert.AreEqual(tree.GetAllNodes().Count, panel.Rows.Count);

            GrayboxSkillNodeRow affordable = null;
            foreach (GrayboxSkillNodeRow row in panel.Rows)
            {
                if (tree.CanUnlock(row.Node.id)) { affordable = row; break; }
            }

            Assert.IsNotNull(affordable, "with 50 SP something is affordable");
            affordable.Button.Button.onClick.Invoke();
            Assert.IsTrue(tree.IsUnlocked(affordable.Node.id));

            GrayboxSkillTreePanel.Open = false;
            yield return null;
            Assert.IsFalse(panel.gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator AchievementsPanel_CountsWhatIsEarned()
        {
            GrayboxAchievements achievements = Systems().AddComponent<GrayboxAchievements>();
            achievements.Configure(GrayboxTestContent.Achievements());

            var panel = Hud().GetComponentInChildren<GrayboxAchievementsPanel>(true);
            GrayboxAchievementsPanel.Open = true;
            yield return null;
            yield return null;

            Assert.AreEqual(achievements.Achievements.Count, panel.RowCount);
            StringAssert.StartsWith("0 of", panel.Summary);

            Achievement first = achievements.Achievements[0];
            achievements.AddProgress(first.Id, first.RequiredProgress);
            yield return null;
            StringAssert.StartsWith("1 of", panel.Summary);
        }

        [UnityTest]
        public IEnumerator SpecialsPanel_HasAButtonPerSpecial_ThatSpawnsIt()
        {
            // Kept active: GameObject.Find below skips inactive objects, and a clone of an
            // inactive template is inactive too.
            var template = new GameObject("TestSpecial");
            _made.Add(template);

            GrayboxSpecials specials = Systems().AddComponent<GrayboxSpecials>();
            specials.Configure(null, new List<GrayboxSpecials.Entry>
            {
                new GrayboxSpecials.Entry { Id = "Wasp", Label = "Wasp", Prefab = template },
                new GrayboxSpecials.Entry { Id = "Titan", Label = "Titan Beetle", Prefab = template, Boss = true },
            }, null, null);

            var panel = Hud().GetComponentInChildren<GrayboxSpecialsPanel>(true);
            GrayboxSpecialsPanel.Open = true;
            yield return null;
            yield return null;

            Assert.AreEqual(2, panel.Buttons.Count);
            StringAssert.StartsWith("BOSS", panel.Buttons[1].Label);

            int before = GameObject.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            panel.Buttons[0].Button.onClick.Invoke();
            GameObject spawned = GameObject.Find("TestSpecial(Clone)");
            Assert.IsNotNull(spawned, "the button spawned the entry's prefab");
            _made.Add(spawned);
            Assert.Greater(GameObject.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, before);
        }

        [UnityTest]
        public IEnumerator EnemyHealthBar_IsSprites_AndShrinksWithHealth()
        {
            var enemy = new GameObject("TestEnemy");
            _made.Add(enemy);
            var health = enemy.AddComponent<EnemyHealth>();
            enemy.AddComponent<EnemyHealthBar>();
            yield return null;

            Transform fill = enemy.transform.Find("Health Bar/Fill");
            Assert.IsNotNull(fill, "the bar is child sprites, not IMGUI");
            Assert.IsNotNull(fill.GetComponent<SpriteRenderer>());
            float full = fill.localScale.x;

            health.TakeDamage(new DamageInfo { Amount = health.MaxHealth * 0.5f });
            yield return null;
            Assert.Less(fill.localScale.x, full * 0.6f);
            Assert.Greater(fill.localScale.x, full * 0.4f);

            EnemyHealthBar.ShowHealthBars = false;
            yield return null;
            Assert.IsFalse(enemy.transform.Find("Health Bar").gameObject.activeSelf);
            EnemyHealthBar.ShowHealthBars = true;
        }

        // ---------- helpers ----------

        private GameObject Systems()
        {
            var go = new GameObject("TestSystems");
            _made.Add(go);
            return go;
        }

        private GameObject Hud()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPath);
            Assert.IsNotNull(prefab, "Run Tools > Hold the Hill > Build Graybox HUD.");
            GameObject hud = Object.Instantiate(prefab);
            _made.Add(hud);
            return hud;
        }
    }
}
#endif
