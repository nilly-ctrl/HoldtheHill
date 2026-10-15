using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Towers;
using NUnit.Framework;
using UnityEngine;

namespace HoldTheHill.Tests
{
    public class TowerDataTests
    {
        private TowerData _data;
        private GameObject _prefab;

        [SetUp]
        public void SetUp()
        {
            _data = ScriptableObject.CreateInstance<TowerData>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_data);
            if (_prefab != null)
            {
                Object.DestroyImmediate(_prefab);
            }
        }

        private void Set(string field, System.Action<UnityEditor.SerializedProperty> apply)
        {
            var so = new UnityEditor.SerializedObject(_data);
            apply(so.FindProperty(field));
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [Test]
        public void TheOldShopFields_StillRead_ThroughTheirNewNames()
        {
            // The serialized names Adam's assets carry.
            Set("_displayName", p => p.stringValue = "FireAnt");
            Set("_baseCost", p => p.intValue = 250);

            Assert.That(_data.DisplayName, Is.EqualTo("FireAnt"));
            Assert.That(_data.BaseCost, Is.EqualTo(250));
            Assert.That(_data.CostResource, Is.Null, "no resource means the run's main one");
        }

        [Test]
        public void WithoutAPrefab_RangeIsZero_AndTheRoleIsGunner()
        {
            Assert.That(_data.Prefab, Is.Null);
            Assert.That(_data.Range, Is.EqualTo(0f));
            Assert.That(_data.Archetype, Is.EqualTo(TowerArchetype.Gunner));
        }

        [Test]
        public void ARoleSetOnTheAsset_Wins()
        {
            Set("_archetype", p => p.enumValueIndex = (int)TowerArchetype.Support);

            Assert.That(_data.Archetype, Is.EqualTo(TowerArchetype.Support));
        }

        [Test]
        public void RangeComesFromThePrefab()
        {
            _prefab = new GameObject("TowerPrefab");
            var tower = _prefab.AddComponent<Tower>();
            var so = new UnityEditor.SerializedObject(tower);
            so.FindProperty("_range").floatValue = 3.5f;
            so.ApplyModifiedPropertiesWithoutUndo();
            Set("_prefab", p => p.objectReferenceValue = tower);

            Assert.That(_data.Range, Is.EqualTo(3.5f));
        }
    }
}
