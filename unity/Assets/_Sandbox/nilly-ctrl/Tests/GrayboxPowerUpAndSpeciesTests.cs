#if UNITY_EDITOR
using System.Collections.Generic;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Towers;
using HoldTheHill.Sandbox.NillyCtrl;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HoldTheHill.Sandbox.Graybox.Tests
{
    /// <summary>
    /// The generated power-ups and species: their assets and prefabs, and what they do in a scene.
    /// </summary>
    public class GrayboxPowerUpAndSpeciesTests
    {
        private const string Root = "Assets/_Sandbox/nilly-ctrl/Graybox";

        private readonly List<GameObject> _made = new List<GameObject>();

        private static GrayboxPowerUpData PowerUp(string id) =>
            AssetDatabase.LoadAssetAtPath<GrayboxPowerUpData>($"{Root}/Data/PowerUps/{id}Data.asset");

        private static GrayboxSpeciesData Species(string id) =>
            AssetDatabase.LoadAssetAtPath<GrayboxSpeciesData>($"{Root}/Data/Species/{id}.asset");

        [TearDown]
        public void TearDown()
        {
            GrayboxPowerUpEffect.EndAll();
            foreach (GrayboxPowerUpEffect effect in Object.FindObjectsByType<GrayboxPowerUpEffect>())
            {
                _made.Add(effect.gameObject);
            }

            foreach (GrayboxWarden warden in Object.FindObjectsByType<GrayboxWarden>())
            {
                _made.Add(warden.gameObject);
            }

            foreach (GameObject go in _made)
            {
                if (go != null) Object.DestroyImmediate(go);
            }

            _made.Clear();
        }

        private GameObject Make(string name, params System.Type[] components)
        {
            var go = new GameObject(name, components);
            _made.Add(go);
            return go;
        }

        // ---------- power-ups ----------

        [Test]
        public void PowerUps_HaveAPickupVariantAndAnEffectPrefab()
        {
            var pickupBase = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Pickups/PickupBase.prefab");
            foreach (string id in new[] { "HoneyDrop", "BerserkBerry" })
            {
                GrayboxPowerUpData data = PowerUp(id);
                Assert.IsNotNull(data, id + ": run Tools > Hold the Hill > Build Graybox Combat Test.");
                Assert.Greater(data.Duration, 0f, id);
                Assert.IsNotNull(data.EffectPrefab, id);
                Assert.IsNotNull(data.PickupPrefab, id);
                Assert.AreEqual(pickupBase, PrefabUtility.GetCorrespondingObjectFromSource(data.PickupPrefab), id);
                Assert.AreEqual(data, data.PickupPrefab.GetComponent<GrayboxPowerUpGain>().PowerUp, id);
            }
        }

        [Test]
        public void CollectingThePickup_StartsThePowerUp_AndASecondOneRestartsIt()
        {
            GrayboxPowerUpData honey = PowerUp("HoneyDrop");
            GameObject first = Object.Instantiate(honey.PickupPrefab);
            GameObject second = Object.Instantiate(honey.PickupPrefab);
            _made.Add(first);
            _made.Add(second);

            first.GetComponent<GrayboxPickup>().Collect();
            Assert.AreEqual(1, GrayboxPowerUpEffect.Active.Count);
            GrayboxPowerUpEffect running = GrayboxPowerUpEffect.Active[0];
            Assert.AreEqual(honey, running.Data);
            Assert.AreEqual(honey.Duration, running.Remaining, 0.001f);

            second.GetComponent<GrayboxPickup>().Collect();
            Assert.AreEqual(1, GrayboxPowerUpEffect.Active.Count, "the same power-up does not stack");

            running.End();
            Assert.AreEqual(0, GrayboxPowerUpEffect.Active.Count);
        }

        [Test]
        public void HoneyDrop_SlowsEnemies()
        {
            var health = Make("Enemy", typeof(CircleCollider2D)).AddComponent<EnemyHealth>();
            Physics2D.SyncTransforms();
            Assert.AreEqual(1f, health.SpeedMultiplier);

            GrayboxPowerUpEffect.Activate(PowerUp("HoneyDrop"));

            Assert.Less(health.SpeedMultiplier, 1f);
        }

        [Test]
        public void BerserkBerry_MakesTheShoveHitHarder_UntilItEnds()
        {
            GrayboxSpeciesData garden = Species("GardenAnt");
            GrayboxWarden warden = Object.Instantiate(garden.WardenPrefab);
            var shove = warden.GetComponentInChildren<GrayboxWardenShove>();
            Assert.AreEqual(1f, shove.DamageMultiplier);

            GrayboxPowerUpEffect effect = GrayboxPowerUpEffect.Activate(PowerUp("BerserkBerry"));
            Assert.AreEqual(3f, shove.DamageMultiplier, 0.001f);
            Assert.Greater(shove.RadiusMultiplier, 1f);
            Assert.IsTrue(shove.StripsShields);

            effect.End();
            Assert.AreEqual(1f, shove.DamageMultiplier);
            Assert.AreEqual(1f, shove.RadiusMultiplier);
            Assert.IsFalse(shove.StripsShields);
        }

        [Test]
        public void Spawner_OffersThePowerUpsInTurn()
        {
            var spawner = Make("Spawner").AddComponent<GrayboxPowerUpSpawner>();
            spawner.Configure(new List<GrayboxPowerUpData> { PowerUp("HoneyDrop"), PowerUp("BerserkBerry") });

            GameObject first = spawner.SpawnNext();
            GameObject second = spawner.SpawnNext();
            _made.Add(first);
            _made.Add(second);

            Assert.AreEqual(PowerUp("HoneyDrop"), first.GetComponent<GrayboxPowerUpGain>().PowerUp);
            Assert.AreEqual(PowerUp("BerserkBerry"), second.GetComponent<GrayboxPowerUpGain>().PowerUp);
        }

        // ---------- species ----------

        [Test]
        public void GardenAnt_IsTheWholeGame()
        {
            GrayboxSpeciesData garden = Species("GardenAnt");
            Assert.IsNotNull(garden, "Run Tools > Hold the Hill > Build Graybox Combat Test.");
            Assert.AreEqual(
                AssetDatabase.LoadAssetAtPath<GrayboxTowerCatalog>($"{Root}/Data/GrayboxTowerCatalog.asset"), garden.TowerCatalog);
            Assert.AreEqual(14, garden.TowerCatalog.Count);
            Assert.IsNotNull(garden.WardenPrefab);
        }

        [Test]
        public void SampleSpecies_BuildsVariantsOfTheSharedTowers()
        {
            GrayboxSpeciesData fire = Species("FireAnt");
            Assert.IsNotNull(fire);
            Assert.AreEqual(3, fire.TowerCatalog.Count);

            GrayboxTowerCatalog shared = Species("GardenAnt").TowerCatalog;
            var sharedPrefabs = new List<GameObject>();
            for (int i = 0; i < shared.Count; i++)
            {
                sharedPrefabs.Add(shared[i].Prefab.gameObject);
            }

            for (int i = 0; i < fire.TowerCatalog.Count; i++)
            {
                Tower prefab = fire.TowerCatalog[i].Prefab;
                GameObject source = PrefabUtility.GetCorrespondingObjectFromSource(prefab.gameObject);
                CollectionAssert.Contains(sharedPrefabs, source, prefab.name);
                Assert.Less(prefab.Range, source.GetComponent<Tower>().Range, prefab.name);
                Assert.Less(prefab.FireInterval, source.GetComponent<Tower>().FireInterval, prefab.name);
            }
        }

        [Test]
        public void Loader_GivesThePlacerTheCatalog_AndFieldsTheWarden()
        {
            var placer = Make("Placer").AddComponent<GrayboxTowerPlacer>();
            var loader = Make("Species").AddComponent<GrayboxSpeciesLoader>();
            Assert.IsNull(placer.Catalog);

            GrayboxSpeciesData fire = Species("FireAnt");
            loader.Load(fire);

            Assert.AreEqual(fire, GrayboxSpeciesLoader.Current);
            Assert.AreEqual(fire.TowerCatalog, placer.Catalog);
            Assert.AreEqual(1, Object.FindObjectsByType<GrayboxWarden>().Length);

            GrayboxSpeciesData garden = Species("GardenAnt");
            loader.Load(garden);
            Assert.AreEqual(garden.TowerCatalog, placer.Catalog);
        }
    }
}
#endif
