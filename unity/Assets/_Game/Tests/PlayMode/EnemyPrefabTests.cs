#if UNITY_EDITOR
using HoldTheHill.Features.Specials;
using System.Collections.Generic;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Enemies;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HoldTheHill.Tests
{
    /// <summary>
    /// The generated enemy and boss prefabs, the per-enemy events and the custom spawner's list.
    /// </summary>
    public class EnemyPrefabTests
    {
        private const string PrefabRoot = "Assets/_Game/Features/Enemies/Prefabs";

        private static readonly string[] Regular =
        {
            "Runner", "Grunt", "Brute", "Shielded", "Swarm", "Splitter", "Healer",
            "Wasp", "Spider", "Silverfish", "ThiefAnt", "Bombardier",
        };

        private static readonly string[] Bosses =
        {
            "TitanBeetle", "MantisQueen", "Hornet", "OrbWeaver", "BoulderBug", "RivalQueen",
        };

        private GameObject _made;

        private static GameObject Prefab(string name) =>
            AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabRoot}/{name}.prefab");

        [TearDown]
        public void TearDown()
        {
            if (_made != null) Object.DestroyImmediate(_made);
        }

        [Test]
        public void EveryEnemy_IsAVariantOfTheEnemyBase()
        {
            GameObject enemyBase = Prefab("EnemyBase");
            Assert.IsNotNull(enemyBase, "Run Tools > Hold the Hill > Build Graybox Combat Test.");

            foreach (string name in Regular)
            {
                GameObject prefab = Prefab(name);
                Assert.IsNotNull(prefab, name);
                Assert.AreEqual(enemyBase, PrefabUtility.GetCorrespondingObjectFromSource(prefab), name);
                Assert.AreEqual(1, prefab.GetComponents<EnemyHealth>().Length, name);
                Assert.IsNotNull(prefab.GetComponent<EnemyMover>(), name);
                Assert.IsNull(prefab.GetComponent<Boss>(), name);
            }
        }

        [Test]
        public void EveryEnemyThatWalksIntoTheHill_HasItsBreachDamageSet()
        {
            // Breach damage is data on the prefab (EnemyHealth), not a name table. 0 is "never set".
            foreach (string[] group in new[] { Regular, Bosses })
            {
                foreach (string name in group)
                {
                    GameObject prefab = Prefab(name);
                    Assert.IsNotNull(prefab, name);
                    var mover = prefab.GetComponent<EnemyMover>();
                    if (mover == null || !mover.DespawnsAtEnd) continue; // stops at the hill (thief ant): no breach

                    Assert.Greater(prefab.GetComponent<EnemyHealth>().BreachDamage, 0f,
                        $"{name}: set breach damage in the builder and rebuild the graybox.");
                }
            }
        }

        [Test]
        public void EveryBoss_IsAVariantOfTheBossBase_AndKnowsItsData()
        {
            GameObject bossBase = Prefab("BossBase");
            Assert.IsNotNull(bossBase);
            Assert.AreEqual(Prefab("EnemyBase"), PrefabUtility.GetCorrespondingObjectFromSource(bossBase));

            foreach (string name in Bosses)
            {
                GameObject prefab = Prefab(name);
                Assert.IsNotNull(prefab, name);
                Assert.AreEqual(bossBase, PrefabUtility.GetCorrespondingObjectFromSource(prefab), name);

                BossData data = prefab.GetComponent<Boss>().Data;
                Assert.IsNotNull(data, name);
                Assert.AreEqual(name, data.Id);
                Assert.IsFalse(string.IsNullOrEmpty(data.BannerText), name);
                Assert.AreEqual(prefab, data.Prefab, name);
                Assert.Greater(prefab.GetComponent<EnemyHealth>().MaxHealth, 1000f, name);
            }
        }

        [Test]
        public void EnemyHealth_RaisesItsOwnHurtAndDiedEvents()
        {
            _made = new GameObject("Enemy");
            var health = _made.AddComponent<EnemyHealth>();
            float hurt = 0f;
            int died = 0;
            health.Hurt += (info, applied) => hurt += applied;
            health.Died += () => died++;

            health.TakeDamage(new DamageInfo(5f, null, Vector3.zero, DamageType.Physical));
            Assert.AreEqual(5f, hurt, 0.001f);
            Assert.AreEqual(0, died);

            health.TakeDamage(new DamageInfo(10000f, null, Vector3.zero, DamageType.Physical));
            Assert.AreEqual(1, died);
        }

        [Test]
        public void CustomSpawner_FindsEnemiesById_WithAFallback()
        {
            _made = new GameObject("Spawner");
            var spawner = _made.AddComponent<CustomSpawner>();
            GameObject grunt = Prefab("Grunt");
            spawner.Configure(
                new List<CustomSpawner.Entry> { new CustomSpawner.Entry { Id = "Grunt", Prefab = grunt } },
                null, null);

            Assert.AreEqual(grunt, spawner.Find("Grunt"));
            Assert.IsNull(spawner.Find("Healer"));
            Assert.AreEqual(grunt, spawner.Find("Healer", "Grunt"));
        }
    }
}
#endif
