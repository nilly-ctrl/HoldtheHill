using System.Collections.Generic;
using HoldTheHill.Features.Economy;
using NUnit.Framework;
using UnityEngine;

namespace HoldTheHill.Tests
{
    public class WalletTests
    {
        private ResourceData _food;
        private ResourceData _jelly;

        [SetUp]
        public void SetUp()
        {
            _food = ScriptableObject.CreateInstance<ResourceData>();
            _jelly = ScriptableObject.CreateInstance<ResourceData>();
            var so = new UnityEditor.SerializedObject(_jelly);
            so.FindProperty("_startingAmount").intValue = 3;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_food);
            Object.DestroyImmediate(_jelly);
        }

        [Test]
        public void NoResource_MeansTheMainOne()
        {
            var wallet = new Wallet(_food, 500);

            wallet.Earn(null, 25);

            Assert.AreEqual(525, wallet.MainAmount);
            Assert.AreEqual(525, wallet.Get(null));
            Assert.AreEqual(525, wallet.Get(_food));
        }

        [Test]
        public void Spending_FailsAndChangesNothing_WhenShort()
        {
            var wallet = new Wallet(_food, 100);

            Assert.IsTrue(wallet.TrySpend(null, 100));
            Assert.IsFalse(wallet.TrySpend(null, 1));
            Assert.AreEqual(0, wallet.MainAmount);
            Assert.IsTrue(wallet.TrySpend(null, 0), "zero costs nothing and always succeeds");
        }

        [Test]
        public void OtherResources_StartAtTheirStartingAmount_AndAreKeptApart()
        {
            var wallet = new Wallet(_food, 500);

            Assert.AreEqual(3, wallet.Get(_jelly));
            wallet.Earn(_jelly, 2);
            Assert.IsTrue(wallet.TrySpend(_jelly, 4));

            Assert.AreEqual(1, wallet.Get(_jelly));
            Assert.AreEqual(500, wallet.MainAmount, "spending another resource leaves the main one alone");
        }

        [Test]
        public void Changes_AreReported_WithTheResourceAndItsNewAmount()
        {
            var wallet = new Wallet(_food, 10);
            var seen = new List<(ResourceData, int)>();
            wallet.Changed += (resource, amount) => seen.Add((resource, amount));

            wallet.Earn(null, 5);
            wallet.TrySpend(_jelly, 1);
            wallet.Earn(null, 0);
            wallet.TrySpend(null, 999);

            Assert.AreEqual(2, seen.Count, "nothing is reported for a no-op or a failed spend");
            Assert.AreEqual((_food, 15), seen[0]);
            Assert.AreEqual((_jelly, 2), seen[1]);
        }

        [Test]
        public void Set_NeverGoesBelowZero()
        {
            var wallet = new Wallet(null, 50);

            wallet.Set(null, -20);

            Assert.AreEqual(0, wallet.MainAmount);
        }

        [Test]
        public void AWalletWithoutAMainResource_ReportsNullForIt()
        {
            var wallet = new Wallet(null, 1);
            ResourceData reported = _food;
            wallet.Changed += (resource, _) => reported = resource;

            wallet.Earn(null, 1);

            Assert.IsNull(reported);
            Assert.AreEqual(2, wallet.MainAmount);
        }
    }
}
