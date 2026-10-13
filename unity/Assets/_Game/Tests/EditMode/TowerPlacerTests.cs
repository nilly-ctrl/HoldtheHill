using System.Collections.Generic;
using HoldTheHill.Features.Towers;
using NUnit.Framework;
using UnityEngine;

namespace HoldTheHill.Tests
{
    public class TowerPlacerTests
    {
        private readonly List<Object> _made = new List<Object>();
        private TowerPlacer _placer;
        private TowerData _data;

        [SetUp]
        public void SetUp()
        {
            var prefab = new GameObject("TowerPrefab");
            _made.Add(prefab);
            Tower tower = prefab.AddComponent<Tower>();

            _data = ScriptableObject.CreateInstance<TowerData>();
            _made.Add(_data);
            var so = new UnityEditor.SerializedObject(_data);
            so.FindProperty("_displayName").stringValue = "Gunner";
            so.FindProperty("_baseCost").intValue = 50;
            so.FindProperty("_prefab").objectReferenceValue = tower;
            so.ApplyModifiedPropertiesWithoutUndo();

            var holder = new GameObject("Placer");
            _made.Add(holder);
            _placer = holder.AddComponent<TowerPlacer>();
            var placerSo = new UnityEditor.SerializedObject(_placer);
            var list = placerSo.FindProperty("_towers");
            list.arraySize = 1;
            list.GetArrayElementAtIndex(0).objectReferenceValue = _data;
            placerSo.ApplyModifiedPropertiesWithoutUndo();
            // Awake does not run on AddComponent in edit mode.
            typeof(TowerPlacer).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(_placer, null);
            _placer.TowerPlaced += t => _made.Add(t.gameObject);
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
        public void TogglePlacement_PicksAndPutsBack_AndClearsTheSelection()
        {
            Assert.That(_placer.IsBusy, Is.False);

            _placer.TogglePlacement(0);
            Assert.That(_placer.ActivePlacementIndex, Is.EqualTo(0));
            Assert.That(_placer.IsBusy, Is.True);

            _placer.TogglePlacement(0);
            Assert.That(_placer.ActivePlacementIndex, Is.EqualTo(-1));

            _placer.TogglePlacement(7);
            Assert.That(_placer.ActivePlacementIndex, Is.EqualTo(-1), "an index outside the catalog does nothing");
        }

        [Test]
        public void Place_SelectsTheTower_SetsItsCell_AndRaisesTheEvent()
        {
            Tower placed = null;
            _placer.TowerPlaced += t => placed = t;

            Tower tower = _placer.Place(_data, new Vector3(4.2f, -1.7f, 0f), 50);

            Assert.That(placed, Is.SameAs(tower));
            Assert.That(_placer.SelectedTower, Is.SameAs(tower));
            Assert.That(tower.Cell, Is.EqualTo(new Vector2Int(4, -2)));
            Assert.That(_placer.IsBusy, Is.True);

            _placer.Deselect();
            Assert.That(_placer.SelectedTower, Is.Null);
        }

        [Test]
        public void WithoutAWallet_BuildingIsFree_AndAnEmptyCellIsOpen()
        {
            // Towers register themselves in Awake, which edit mode does not run, so the taken-cell
            // case lives in BuildRulesTests with an explicit list.
            Assert.That(_placer.CanAfford(_data), Is.True);
            Assert.That(_placer.CostOf(_data), Is.EqualTo(50));
            Assert.That(_placer.CanBuildAt(new Vector3(5f, 5f, 0f)), Is.True);
        }

        [Test]
        public void SellSelected_RefundsAndClearsTheSelection()
        {
            Tower tower = _placer.Place(_data, Vector3.zero, 100);
            Tower sold = null;
            _placer.TowerSold += t => sold = t;

            _placer.SellSelected();

            Assert.That(sold, Is.Not.Null);
            Assert.That(_placer.SelectedTower, Is.Null);
            Assert.That(tower == null, Is.True);
        }

        [Test]
        public void CancelInteraction_DropsEverything()
        {
            _placer.Place(_data, Vector3.zero, 50);
            _placer.TogglePlacement(0);

            _placer.CancelInteraction();

            Assert.That(_placer.IsBusy, Is.False);
            Assert.That(_placer.Preview.Active, Is.False);
        }
    }
}
