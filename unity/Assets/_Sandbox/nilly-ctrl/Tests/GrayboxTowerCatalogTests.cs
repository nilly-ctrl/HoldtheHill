#if UNITY_EDITOR
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Towers;
using HoldTheHill.Sandbox.NillyCtrl;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HoldTheHill.Sandbox.Graybox.Tests
{
    /// <summary>
    /// The generated tower prefabs and the catalog the build bar reads.
    /// </summary>
    public class GrayboxTowerCatalogTests
    {
        private const string Root = "Assets/_Sandbox/nilly-ctrl/Graybox";

        private GameObject _placerObject;
        private Tower _placed;

        private static GrayboxTowerCatalog Catalog =>
            AssetDatabase.LoadAssetAtPath<GrayboxTowerCatalog>($"{Root}/Data/GrayboxTowerCatalog.asset");

        private static GameObject Prefab(string id) =>
            AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Towers/{id}.prefab");

        [TearDown]
        public void TearDown()
        {
            if (_placed != null) Object.DestroyImmediate(_placed.gameObject);
            if (_placerObject != null) Object.DestroyImmediate(_placerObject);
        }

        [Test]
        public void Catalog_HasEveryTower_EachWithAPrefabVariant()
        {
            GrayboxTowerCatalog catalog = Catalog;
            Assert.IsNotNull(catalog, "Run Tools > Hold the Hill > Build Graybox Combat Test.");
            Assert.AreEqual(14, catalog.Count);

            for (int i = 0; i < catalog.Count; i++)
            {
                GrayboxTowerData data = catalog[i];
                Assert.IsNotNull(data, $"slot {i}");
                Assert.IsFalse(string.IsNullOrEmpty(data.DisplayName), $"slot {i} name");
                Assert.Greater(data.BaseCost, 0, data.DisplayName);
                Assert.IsNotNull(data.Prefab, data.DisplayName);
                Assert.Greater(data.Range, 0f, data.DisplayName);
                Assert.AreEqual(PrefabAssetType.Variant, PrefabUtility.GetPrefabAssetType(data.Prefab.gameObject), data.DisplayName);
                Assert.IsNotNull(data.Prefab.GetComponent<TowerTargetVisualizer>(), data.DisplayName);
            }
        }

        [Test]
        public void Prefabs_KeepTheirOwnNumbersAndWeapons()
        {
            Tower linear = Prefab("TowerLinear").GetComponent<Tower>();
            Assert.AreEqual(3.6f, linear.Range, 0.001f);
            Assert.AreEqual(0.45f, linear.FireInterval, 0.001f);
            Assert.AreEqual(TargetingPriority.First, linear.Priority);

            Assert.AreEqual(TargetingPriority.Weakest, Prefab("TowerChain").GetComponent<Tower>().Priority);
            Assert.IsNotNull(Prefab("TowerChain").GetComponent<ChainLightning>());
            Assert.IsNotNull(Prefab("TowerChain").GetComponent<LineRenderer>().sharedMaterial);
            Assert.IsNotNull(Prefab("TowerBeam").GetComponent<ContinuousBeam>());
            Assert.IsNotNull(Prefab("TowerOrbit").GetComponent<OrbitingDamageField>());
            Assert.IsNotNull(Prefab("TowerFrostAura").GetComponent<FrostAuraTower>());
            Assert.IsNotNull(Prefab("TowerKnockback").GetComponent<KnockbackTower>());
            Assert.IsNotNull(Prefab("TowerMineLayer").GetComponent<MineLayerTower>());

            Assert.AreEqual(GrayboxMeleeTower.Caste.Soldier, Prefab("TowerSoldier").GetComponent<GrayboxMeleeTower>().Kind);
            Assert.IsNotNull(Prefab("TowerNurse").GetComponent<GrayboxNurseTower>());
            Assert.IsNull(Prefab("TowerNurse").GetComponent<GrayboxMeleeTower>());
            Assert.AreEqual(GrayboxMeleeTower.StatsFor(GrayboxMeleeTower.Caste.Major).Range,
                Prefab("TowerMajor").GetComponent<Tower>().Range, 0.001f);
        }

        [Test]
        public void EveryPrefab_HasAWeaponOrAProjectile()
        {
            GrayboxTowerCatalog catalog = Catalog;
            for (int i = 0; i < catalog.Count; i++)
            {
                Tower prefab = catalog[i].Prefab;
                bool hasProjectile = new SerializedObject(prefab).FindProperty("_projectilePrefab").objectReferenceValue != null;
                Assert.IsTrue(hasProjectile || prefab.GetComponent<TowerWeapon>() != null, catalog[i].DisplayName);
            }
        }

        [Test]
        public void Upgrade_ReachesTheWeapon_AndRaisesTheEvent()
        {
            _placerObject = new GameObject("Tower");
            var tower = _placerObject.AddComponent<Tower>();
            var frost = _placerObject.AddComponent<FrostAuraTower>(); // added after the tower woke up
            float before = frost.Radius;
            int heard = 0;
            tower.Upgraded += level => heard = level;

            Assert.IsTrue(tower.Upgrade());

            Assert.AreEqual(before * 1.15f, frost.Radius, 0.001f);
            Assert.AreEqual(2, heard);
        }

        [Test]
        public void FireRateScale_IsPassedToEveryWeapon()
        {
            _placerObject = new GameObject("Tower");
            var tower = _placerObject.AddComponent<Tower>();
            var frost = _placerObject.AddComponent<FrostAuraTower>();
            var layer = _placerObject.AddComponent<MineLayerTower>();

            tower.FireRateScale = 0.5f;

            Assert.AreEqual(0.5f, frost.RateScale);
            Assert.AreEqual(0.5f, layer.RateScale);
        }

        [Test]
        public void Place_InstantiatesThePrefab_AndSelectsIt()
        {
            _placerObject = new GameObject("Placer");
            var placer = _placerObject.AddComponent<GrayboxTowerPlacer>();
            GrayboxTowerData frost = null;
            for (int i = 0; i < Catalog.Count; i++)
            {
                if (Catalog[i].Prefab.GetComponent<FrostAuraTower>() != null) frost = Catalog[i];
            }

            Assert.IsNotNull(frost);
            _placed = placer.Place(frost, new Vector3(3f, -2f, 0f), 123);

            Assert.AreEqual(new Vector3(3f, -2f, 0f), _placed.transform.position);
            Assert.AreEqual(frost.DisplayName, _placed.name);
            Assert.AreEqual(123, _placed.TotalGoldInvested);
            Assert.AreEqual(frost.Range, _placed.Range, 0.001f);
            Assert.IsNotNull(_placed.GetComponent<FrostAuraTower>());
            Assert.AreSame(_placed, placer.SelectedTower);
        }
    }
}
#endif
