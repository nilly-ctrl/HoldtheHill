#if UNITY_EDITOR
using HoldTheHill.Features.Economy;
using HoldTheHill.Features.PowerUps;
using HoldTheHill.Features.Species;
using HoldTheHill.Features.Hero;
using System.Collections.Generic;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Towers;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HoldTheHill.Tests
{
    /// <summary>
    /// The generated power-ups and species: their assets and prefabs, and what they do in a scene.
    /// </summary>
    public class PowerUpAndSpeciesTests
    {
        private const string Root = "Assets/_Game/Features";

        private readonly List<GameObject> _made = new List<GameObject>();

        private static PowerUpData PowerUp(string id) =>
            AssetDatabase.LoadAssetAtPath<PowerUpData>($"{Root}/PowerUps/Data/{id}Data.asset");

        private static SpeciesData Species(string id) =>
            AssetDatabase.LoadAssetAtPath<SpeciesData>($"{Root}/Species/Data/{id}.asset");

        [TearDown]
        public void TearDown()
        {
            PowerUpEffect.EndAll();
            foreach (PowerUpEffect effect in Object.FindObjectsByType<PowerUpEffect>())
            {
                _made.Add(effect.gameObject);
            }

            foreach (Warden warden in Object.FindObjectsByType<Warden>())
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
            var pickupBase = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Economy/Prefabs/PickupBase.prefab");
            foreach (string id in new[] { "HoneyDrop", "BerserkBerry" })
            {
                PowerUpData data = PowerUp(id);
                Assert.IsNotNull(data, id + ": run Tools > Hold the Hill > Build Graybox Combat Test.");
                Assert.Greater(data.Duration, 0f, id);
                Assert.IsNotNull(data.EffectPrefab, id);
                Assert.IsNotNull(data.PickupPrefab, id);
                Assert.AreEqual(pickupBase, PrefabUtility.GetCorrespondingObjectFromSource(data.PickupPrefab), id);
                Assert.AreEqual(data, data.PickupPrefab.GetComponent<PowerUpGain>().PowerUp, id);
            }
        }

        [Test]
        public void CollectingThePickup_StartsThePowerUp_AndASecondOneRestartsIt()
        {
            PowerUpData honey = PowerUp("HoneyDrop");
            GameObject first = Object.Instantiate(honey.PickupPrefab);
            GameObject second = Object.Instantiate(honey.PickupPrefab);
            _made.Add(first);
            _made.Add(second);

            first.GetComponent<Pickup>().Collect();
            Assert.AreEqual(1, PowerUpEffect.Active.Count);
            PowerUpEffect running = PowerUpEffect.Active[0];
            Assert.AreEqual(honey, running.Data);
            Assert.AreEqual(honey.Duration, running.Remaining, 0.001f);

            second.GetComponent<Pickup>().Collect();
            Assert.AreEqual(1, PowerUpEffect.Active.Count, "the same power-up does not stack");

            running.End();
            Assert.AreEqual(0, PowerUpEffect.Active.Count);
        }

        [Test]
        public void HoneyDrop_SlowsEnemies()
        {
            var health = Make("Enemy", typeof(CircleCollider2D)).AddComponent<EnemyHealth>();
            Physics2D.SyncTransforms();
            Assert.AreEqual(1f, health.SpeedMultiplier);

            PowerUpEffect.Activate(PowerUp("HoneyDrop"));

            Assert.Less(health.SpeedMultiplier, 1f);
        }

        [Test]
        public void BerserkBerry_MakesTheShoveHitHarder_UntilItEnds()
        {
            SpeciesData garden = Species("GardenAnt");
            Warden warden = Object.Instantiate(garden.WardenPrefab);
            var shove = warden.GetComponentInChildren<WardenShove>();
            Assert.AreEqual(1f, shove.DamageMultiplier);

            PowerUpEffect effect = PowerUpEffect.Activate(PowerUp("BerserkBerry"));
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
            var spawner = Make("Spawner").AddComponent<PowerUpSpawner>();
            spawner.Configure(new List<PowerUpData> { PowerUp("HoneyDrop"), PowerUp("BerserkBerry") });

            GameObject first = spawner.SpawnNext();
            GameObject second = spawner.SpawnNext();
            _made.Add(first);
            _made.Add(second);

            Assert.AreEqual(PowerUp("HoneyDrop"), first.GetComponent<PowerUpGain>().PowerUp);
            Assert.AreEqual(PowerUp("BerserkBerry"), second.GetComponent<PowerUpGain>().PowerUp);
        }

        // ---------- species ----------

        [Test]
        public void GardenAnt_IsTheWholeGame()
        {
            SpeciesData garden = Species("GardenAnt");
            Assert.IsNotNull(garden, "Run Tools > Hold the Hill > Build Graybox Combat Test.");
            Assert.AreEqual(
                AssetDatabase.LoadAssetAtPath<TowerCatalog>($"{Root}/Towers/Data/TowerCatalog.asset"), garden.TowerCatalog);
            Assert.AreEqual(14, garden.TowerCatalog.Count);
            Assert.IsNotNull(garden.WardenPrefab);
        }

        [Test]
        public void SampleSpecies_BuildsVariantsOfTheSharedTowers()
        {
            SpeciesData fire = Species("FireAnt");
            Assert.IsNotNull(fire);
            Assert.AreEqual(3, fire.TowerCatalog.Count);

            TowerCatalog shared = Species("GardenAnt").TowerCatalog;
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
            var placer = Make("Placer").AddComponent<GameTowerPlacer>();
            var loader = Make("Species").AddComponent<SpeciesLoader>();
            Assert.IsNull(placer.Catalog);

            SpeciesData fire = Species("FireAnt");
            loader.Load(fire);

            Assert.AreEqual(fire, SpeciesLoader.Current);
            Assert.AreEqual(fire.TowerCatalog, placer.Catalog);
            Assert.AreEqual(1, Object.FindObjectsByType<Warden>().Length);

            SpeciesData garden = Species("GardenAnt");
            loader.Load(garden);
            Assert.AreEqual(garden.TowerCatalog, placer.Catalog);
        }
    }
}
#endif
