using System.Collections.Generic;
using HoldTheHill.Features.Economy;
using HoldTheHill.Features.Towers;
using NUnit.Framework;
using UnityEngine;

namespace HoldTheHill.Tests
{
    public class TowerCommandsTests
    {
        private readonly List<Object> _made = new List<Object>();
        private Wallet _wallet;
        private TowerCommands _commands;
        private TowerData _data;
        private ResourceData _food;

        [SetUp]
        public void SetUp()
        {
            _food = ScriptableObject.CreateInstance<ResourceData>();
            _made.Add(_food);
            _wallet = new Wallet(_food, 100);
            _commands = new TowerCommands(() => _wallet, new[] { 30, 50 });
            _commands.Placed += tower => _made.Add(tower.gameObject);

            var prefab = new GameObject("TowerPrefab");
            _made.Add(prefab);
            Tower tower = prefab.AddComponent<Tower>();

            _data = ScriptableObject.CreateInstance<TowerData>();
            _made.Add(_data);
            var so = new UnityEditor.SerializedObject(_data);
            so.FindProperty("_displayName").stringValue = "Gunner";
            so.FindProperty("_baseCost").intValue = 70;
            so.FindProperty("_prefab").objectReferenceValue = tower;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _made)
            {
                if (o != null)
                {
                    Object.DestroyImmediate(o);
                }
            }

            _made.Clear();
        }

        [Test]
        public void Build_PaysTheCost_PlacesTheTower_AndRecordsWhatWasSpent()
        {
            Tower placed = null;
            _commands.Placed += t => placed = t;

            Tower tower = _commands.TryBuild(_data, new Vector3(2f, 3f, 0f));

            Assert.That(tower, Is.Not.Null);
            Assert.That(placed, Is.SameAs(tower));
            Assert.That(tower.name, Is.EqualTo("Gunner"));
            Assert.That(tower.transform.position, Is.EqualTo(new Vector3(2f, 3f, 0f)));
            Assert.That(tower.TotalFoodInvested, Is.EqualTo(70));
            Assert.That(_wallet.MainAmount, Is.EqualTo(30));
        }

        [Test]
        public void Build_FailsAndChargesNothing_WhenShort()
        {
            _commands.TryBuild(_data, Vector3.zero);

            Assert.That(_commands.CanAfford(_data), Is.False);
            Assert.That(_commands.TryBuild(_data, Vector3.one), Is.Null);
            Assert.That(_wallet.MainAmount, Is.EqualTo(30));
        }

        [Test]
        public void Upgrade_CostsByLevel_PaysFromTheMainResource_AndAddsToWhatWasInvested()
        {
            Tower tower = _commands.TryBuild(_data, Vector3.zero);
            Tower upgraded = null;
            _commands.Upgraded += t => upgraded = t;

            Assert.That(_commands.UpgradeCost(tower), Is.EqualTo(30), "level 1 to 2");
            Assert.That(_commands.TryUpgrade(tower), Is.True);
            Assert.That(upgraded, Is.SameAs(tower));
            Assert.That(tower.Level, Is.EqualTo(2));
            Assert.That(tower.TotalFoodInvested, Is.EqualTo(100));
            Assert.That(_wallet.MainAmount, Is.EqualTo(0));

            Assert.That(_commands.UpgradeCost(tower), Is.EqualTo(50), "level 2 to 3");
            Assert.That(_commands.TryUpgrade(tower), Is.False, "nothing left to pay with");
            Assert.That(tower.Level, Is.EqualTo(2));
        }

        [Test]
        public void UpgradeCost_IsZero_OnceATowerIsAtItsTop()
        {
            var free = new TowerCommands(() => null, new[] { 30, 50 });
            Tower tower = free.TryBuild(_data, Vector3.zero);
            _made.Add(tower.gameObject);
            free.TryUpgrade(tower);
            free.TryUpgrade(tower);

            Assert.That(tower.CanUpgrade, Is.False);
            Assert.That(free.UpgradeCost(tower), Is.EqualTo(0));
            Assert.That(free.TryUpgrade(tower), Is.False);
        }

        [Test]
        public void Sell_RefundsThreeQuartersOfWhatWasInvested_AndDestroysTheTower()
        {
            Tower tower = _commands.TryBuild(_data, Vector3.zero);
            _commands.TryUpgrade(tower);
            Tower sold = null;
            _commands.Sold += t => sold = t;

            int refund = _commands.Sell(tower);

            Assert.That(refund, Is.EqualTo(75), "75% of 100 with no refund skill");
            Assert.That(sold, Is.Not.Null, "Sold is raised before the tower goes");
            Assert.That(_wallet.MainAmount, Is.EqualTo(75));
            Assert.That(tower == null, Is.True, "destroyed");
        }

        [Test]
        public void WithoutAWallet_EverythingIsFree()
        {
            var free = new TowerCommands(() => null, new[] { 30 });

            Tower tower = free.TryBuild(_data, Vector3.zero);
            _made.Add(tower.gameObject);

            Assert.That(tower, Is.Not.Null);
            Assert.That(free.CanAfford(_data), Is.True);
            Assert.That(free.TryUpgrade(tower), Is.True);
        }
    }
}
