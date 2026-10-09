#if UNITY_EDITOR
using HoldTheHill.Sandbox.NillyCtrl;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HoldTheHill.Sandbox.Graybox.Tests
{
    /// <summary>
    /// The wallet in <see cref="GrayboxEconomy"/>, pickups and the generated crumb prefabs.
    /// </summary>
    public class GrayboxResourceTests
    {
        private const string Root = "Assets/_Sandbox/nilly-ctrl/Graybox";

        private GameObject _holder;
        private GameObject _pickup;
        private GrayboxResourceData _jelly;

        private static GameObject Pickup(string name) =>
            AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/Pickups/{name}.prefab");

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("Economy");
            _jelly = ScriptableObject.CreateInstance<GrayboxResourceData>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_holder != null) Object.DestroyImmediate(_holder);
            if (_pickup != null) Object.DestroyImmediate(_pickup);
            Object.DestroyImmediate(_jelly);
        }

        [Test]
        public void Wallet_TreatsNoResourceAsTheMainOne()
        {
            var economy = _holder.AddComponent<GrayboxEconomy>();

            economy.Earn(null, 25);
            Assert.AreEqual(525, economy.CurrentGold);
            Assert.AreEqual(525, economy.Get(null));
            Assert.IsTrue(economy.TrySpend(null, 500));
            Assert.IsFalse(economy.TrySpend(null, 500));
            Assert.AreEqual(25, economy.CurrentGold);
        }

        [Test]
        public void Wallet_KeepsOtherResourcesApartFromTheMainOne()
        {
            var economy = _holder.AddComponent<GrayboxEconomy>();
            GrayboxResourceData changed = null;
            int amount = -1;
            System.Action<GrayboxResourceData, int> listen = (r, a) => { changed = r; amount = a; };
            GrayboxEconomy.OnResourceChanged += listen;
            try
            {
                Assert.AreEqual(0, economy.Get(_jelly));
                Assert.IsFalse(economy.TrySpend(_jelly, 1));

                economy.Earn(_jelly, 3);
                Assert.AreEqual(3, economy.Get(_jelly));
                Assert.AreEqual(_jelly, changed);
                Assert.AreEqual(3, amount);

                Assert.IsTrue(economy.TrySpend(_jelly, 2));
                Assert.AreEqual(1, economy.Get(_jelly));
                Assert.AreEqual(500, economy.CurrentGold, "spending another resource leaves the main one alone");
            }
            finally
            {
                GrayboxEconomy.OnResourceChanged -= listen;
            }
        }

        [Test]
        public void Crumbs_AreVariantsOfThePickupBase_AndPayFood()
        {
            GameObject pickupBase = Pickup("PickupBase");
            Assert.IsNotNull(pickupBase, "Run Tools > Hold the Hill > Build Graybox Combat Test.");
            var food = AssetDatabase.LoadAssetAtPath<GrayboxResourceData>($"{Root}/Data/Economy/Food.asset");
            Assert.IsNotNull(food);

            int last = 0;
            foreach (string name in new[] { "CrumbSmall", "CrumbBig", "CrumbGolden" })
            {
                GameObject prefab = Pickup(name);
                Assert.IsNotNull(prefab, name);
                Assert.AreEqual(pickupBase, PrefabUtility.GetCorrespondingObjectFromSource(prefab), name);
                Assert.IsNotNull(prefab.GetComponent<GrayboxPickup>(), name);

                var gain = prefab.GetComponent<GrayboxResourceGain>();
                Assert.AreEqual(food, gain.Resource, name);
                Assert.Greater(gain.Amount, last, name);
                last = gain.Amount;
            }
        }

        [Test]
        public void CollectingACrumb_PaysItsAmountOnce()
        {
            var economy = _holder.AddComponent<GrayboxEconomy>();
            var wallet = new SerializedObject(economy);
            wallet.FindProperty("_mainResource").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<GrayboxResourceData>($"{Root}/Data/Economy/Food.asset");
            wallet.ApplyModifiedPropertiesWithoutUndo();
            _pickup = Object.Instantiate(Pickup("CrumbBig"));
            var pickup = _pickup.GetComponent<GrayboxPickup>();
            int amount = _pickup.GetComponent<GrayboxResourceGain>().Amount;
            int heard = 0;
            System.Action<GrayboxPickup> listen = p => heard++;
            GrayboxPickup.Collected += listen;
            try
            {
                pickup.Collect();
                pickup.Collect();
            }
            finally
            {
                GrayboxPickup.Collected -= listen;
            }

            Assert.AreEqual(500 + amount, economy.CurrentGold);
            Assert.AreEqual(1, heard);
            Assert.IsTrue(pickup.IsCollected);
        }

        [Test]
        public void BossBase_DropsTheGoldenCrumb()
        {
            var bossBase = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Prefabs/GrayboxBossBase.prefab");
            Object drop = new SerializedObject(bossBase.GetComponent<GrayboxBoss>()).FindProperty("_dropPrefab").objectReferenceValue;
            Assert.AreEqual(Pickup("CrumbGolden"), drop);
        }
    }
}
#endif
