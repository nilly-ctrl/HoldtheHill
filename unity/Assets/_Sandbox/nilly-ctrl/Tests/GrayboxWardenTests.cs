#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Towers;
using HoldTheHill.Sandbox.NillyCtrl;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HoldTheHill.Sandbox.Graybox.Tests
{
    /// <summary>
    /// The Warden prefab and its five abilities, used directly rather than through the keyboard.
    /// </summary>
    public class GrayboxWardenTests
    {
        private const string Root = "Assets/_Sandbox/nilly-ctrl/Graybox";

        private readonly List<GameObject> _made = new List<GameObject>();
        private GrayboxWarden _warden;

        [SetUp]
        public void SetUp()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Warden/Warden.prefab");
            Assert.IsNotNull(prefab, "Run Tools > Hold the Hill > Build Graybox Combat Test.");
            GameObject go = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity);
            _made.Add(go);
            _warden = go.GetComponent<GrayboxWarden>();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GrayboxPickup pickup in Object.FindObjectsByType<GrayboxPickup>())
            {
                _made.Add(pickup.gameObject);
            }

            foreach (GameObject go in _made)
            {
                if (go != null) Object.DestroyImmediate(go);
            }

            _made.Clear();
        }

        private T Ability<T>() where T : GrayboxWardenAbility => _warden.GetComponentInChildren<T>();

        private Tower MakeTower(Vector3 at)
        {
            var go = new GameObject("Tower");
            go.transform.position = at;
            _made.Add(go);
            return go.AddComponent<Tower>();
        }

        [Test]
        public void Warden_HasFiveAbilities_EachWithDataAndAKey()
        {
            Assert.AreEqual(5, _warden.Abilities.Count);
            var ids = new HashSet<string>(GrayboxControls.Entries.Select(entry => entry.Id));

            foreach (GrayboxWardenAbility ability in _warden.Abilities)
            {
                Assert.IsNotNull(ability.Data, ability.name);
                Assert.IsFalse(string.IsNullOrEmpty(ability.Data.DisplayName), ability.name);
                CollectionAssert.Contains(ids, ability.Data.ControlId, ability.name);
                Assert.IsTrue(PrefabUtility.IsPartOfAnyPrefab(
                    AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Warden/{ability.name}.prefab")), ability.name);
            }
        }

        [Test]
        public void Shove_HurtsEnemiesInReachALittle_AndThenCoolsDown()
        {
            var near = new GameObject("Near", typeof(CircleCollider2D));
            near.transform.position = new Vector3(0.6f, 0f, 0f);
            var far = new GameObject("Far", typeof(CircleCollider2D));
            far.transform.position = new Vector3(6f, 0f, 0f);
            _made.Add(near);
            _made.Add(far);
            var nearHealth = near.AddComponent<EnemyHealth>();
            var farHealth = far.AddComponent<EnemyHealth>();
            Physics2D.SyncTransforms();
            float before = nearHealth.CurrentHealth;

            var shove = Ability<GrayboxWardenShove>();
            Assert.IsTrue(shove.TryUse());

            Assert.Less(nearHealth.CurrentHealth, before);
            Assert.IsFalse(nearHealth.IsDead, "a shove is not meant to kill");
            Assert.AreEqual(farHealth.MaxHealth, farHealth.CurrentHealth);
            Assert.IsFalse(shove.IsReady);
            Assert.IsFalse(shove.TryUse(), "still cooling down");
            Assert.AreEqual(1, shove.Uses);
        }

        [Test]
        public void Rally_SpeedsUpTowersInReach_AndMultipliesWithAWeb()
        {
            var rally = Ability<GrayboxWardenRally>();
            Assert.IsFalse(rally.CanUse(), "no towers nearby");

            Tower near = MakeTower(new Vector3(1f, 0f, 0f));
            Tower far = MakeTower(new Vector3(9f, 0f, 0f));
            Assert.IsTrue(rally.TryUse());

            Assert.AreEqual(1.5f, near.FireRateScale, 0.001f);
            Assert.AreEqual(1f, far.FireRateScale, 0.001f);

            GrayboxTowerAffliction.Apply(near, GrayboxTowerAffliction.Kind.Web, 5f);
            Assert.AreEqual(0.75f, near.FireRateScale, 0.001f);
        }

        [Test]
        public void Repair_ClearsWhatIsWrongWithATowerInReach()
        {
            var repair = Ability<GrayboxWardenRepair>();
            Tower tower = MakeTower(new Vector3(1f, 0f, 0f));
            Assert.IsFalse(repair.CanUse(), "nothing is wrong with it");

            GrayboxTowerAffliction.Apply(tower, GrayboxTowerAffliction.Kind.Stun, 30f);
            Assert.IsTrue(GrayboxTowerAffliction.Silenced(tower));
            Assert.IsTrue(repair.TryUse());

            Assert.IsFalse(GrayboxTowerAffliction.Silenced(tower));
            Assert.IsTrue(tower.enabled);
        }

        [Test]
        public void Dig_TurnsUpAPickup_AndThenCoolsDown()
        {
            var dig = Ability<GrayboxWardenDig>();
            Assert.Greater(dig.Data.HoldSeconds, 0f, "digging is a hold");
            Assert.IsTrue(dig.TryUse());

            Assert.AreEqual(1, Object.FindObjectsByType<GrayboxPickup>().Length);
            Assert.IsFalse(dig.TryUse(), "still cooling down");
        }

        [Test]
        public void Carry_PicksATowerUp_AndPutsItDownOnTheGrid()
        {
            var carry = Ability<GrayboxWardenCarry>();
            Assert.IsFalse(carry.CanUse(), "nothing to carry");

            Tower tower = MakeTower(new Vector3(0.5f, 0f, 0f));
            _made.Add(tower.gameObject);
            Assert.IsTrue(carry.TryUse());
            Assert.AreSame(tower, carry.Carried);
            Assert.IsFalse(tower.enabled, "a carried tower does not attack");
            Assert.Less(_warden.SpeedScale, 1f);

            _warden.transform.position = new Vector3(4.3f, 2.6f, 0f);
            SetCooldownOver(carry);
            Assert.IsTrue(carry.TryUse());

            Assert.IsNull(carry.Carried);
            Assert.AreEqual(new Vector3(4f, 3f, 0f), tower.transform.position);
            Assert.IsTrue(tower.enabled);
            Assert.AreEqual(1f, _warden.SpeedScale);
        }

        private static void SetCooldownOver(GrayboxWardenAbility ability)
        {
            typeof(GrayboxWardenAbility)
                .GetField("_readyAt", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .SetValue(ability, 0f);
        }
    }
}
#endif
