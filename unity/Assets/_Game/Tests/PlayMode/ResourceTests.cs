#if UNITY_EDITOR
using HoldTheHill.Features.Specials;
using HoldTheHill.Features.Economy;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HoldTheHill.Tests
{
    /// <summary>
    /// The wallet in <see cref="Wallet"/>, pickups and the generated crumb prefabs.
    /// </summary>
    public class ResourceTests
    {
        private const string Root = "Assets/_Game/Features";

        private GameObject _holder;
        private GameObject _pickup;
        private ResourceData _jelly;

        private static GameObject LoadPickup(string name) =>
            AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Economy/Prefabs/{name}.prefab");

        [SetUp]
        public void SetUp()
        {
            _holder = new GameObject("Economy");
            _jelly = ScriptableObject.CreateInstance<ResourceData>();
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
            var economy = _holder.AddComponent<WalletBehaviour>();

            economy.Earn(null, 25);
            Assert.AreEqual(525, economy.CurrentFood);
            Assert.AreEqual(525, economy.Get(null));
            Assert.IsTrue(economy.TrySpend(null, 500));
            Assert.IsFalse(economy.TrySpend(null, 500));
            Assert.AreEqual(25, economy.CurrentFood);
        }

        [Test]
        public void Wallet_KeepsOtherResourcesApartFromTheMainOne()
        {
            var economy = _holder.AddComponent<WalletBehaviour>();
            ResourceData changed = null;
            int amount = -1;
            System.Action<ResourceData, int> listen = (r, a) => { changed = r; amount = a; };
            economy.Wallet.Changed += listen;
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
                Assert.AreEqual(500, economy.CurrentFood, "spending another resource leaves the main one alone");
            }
            finally
            {
                economy.Wallet.Changed -= listen;
            }
        }

        [Test]
        public void Crumbs_AreVariantsOfThePickupBase_AndPayFood()
        {
            GameObject pickupBase = LoadPickup("PickupBase");
            Assert.IsNotNull(pickupBase, "Run Tools > Hold the Hill > Build Graybox Combat Test.");
            var food = AssetDatabase.LoadAssetAtPath<ResourceData>($"{Root}/Economy/Data/Food.asset");
            Assert.IsNotNull(food);

            int last = 0;
            foreach (string name in new[] { "CrumbSmall", "CrumbBig", "CrumbGolden" })
            {
                GameObject prefab = LoadPickup(name);
                Assert.IsNotNull(prefab, name);
                Assert.AreEqual(pickupBase, PrefabUtility.GetCorrespondingObjectFromSource(prefab), name);
                Assert.IsNotNull(prefab.GetComponent<Pickup>(), name);

                var gain = prefab.GetComponent<ResourceGain>();
                Assert.AreEqual(food, gain.Resource, name);
                Assert.Greater(gain.Amount, last, name);
                last = gain.Amount;
            }
        }

        [Test]
        public void CollectingACrumb_PaysItsAmountOnce()
        {
            var economy = _holder.AddComponent<WalletBehaviour>();
            var wallet = new SerializedObject(economy);
            wallet.FindProperty("_mainResource").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<ResourceData>($"{Root}/Economy/Data/Food.asset");
            wallet.ApplyModifiedPropertiesWithoutUndo();
            _pickup = Object.Instantiate(LoadPickup("CrumbBig"));
            var pickup = _pickup.GetComponent<Pickup>();
            int amount = _pickup.GetComponent<ResourceGain>().Amount;
            int heard = 0;
            System.Action<Pickup> listen = p => heard++;
            Pickup.Collected += listen;
            try
            {
                pickup.Collect();
                pickup.Collect();
            }
            finally
            {
                Pickup.Collected -= listen;
            }

            Assert.AreEqual(500 + amount, economy.CurrentFood);
            Assert.AreEqual(1, heard);
            Assert.IsTrue(pickup.IsCollected);
        }

        [Test]
        public void BossBase_DropsTheGoldenCrumb()
        {
            var bossBase = AssetDatabase.LoadAssetAtPath<GameObject>($"{Root}/Enemies/Prefabs/BossBase.prefab");
            Object drop = new SerializedObject(bossBase.GetComponent<Boss>()).FindProperty("_dropPrefab").objectReferenceValue;
            Assert.AreEqual(LoadPickup("CrumbGolden"), drop);
        }
    }
}
#endif
