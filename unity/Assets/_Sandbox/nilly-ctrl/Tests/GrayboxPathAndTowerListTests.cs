#if UNITY_EDITOR
using System.Collections.Generic;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Towers;
using NUnit.Framework;
using UnityEngine;

namespace HoldTheHill.Sandbox.Graybox.Tests
{
    /// <summary>
    /// The route maths on <see cref="EnemyPath"/> and the list of standing towers on <see cref="Tower"/>.
    /// </summary>
    public class GrayboxPathAndTowerListTests
    {
        private readonly List<GameObject> _made = new List<GameObject>();
        private readonly List<Tower> _towers = new List<Tower>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _made)
            {
                if (go != null) Object.DestroyImmediate(go);
            }

            _made.Clear();
        }

        // An L: 4 along x, then 3 up. 7 long.
        private EnemyPath MakePath()
        {
            var go = new GameObject("Path");
            _made.Add(go);
            foreach (Vector3 point in new[] { new Vector3(0, 0), new Vector3(4, 0), new Vector3(4, 3) })
            {
                var waypoint = new GameObject("Waypoint");
                waypoint.transform.SetParent(go.transform, false);
                waypoint.transform.position = point;
            }

            return go.AddComponent<EnemyPath>();
        }

        private Tower MakeTower(string name)
        {
            var go = new GameObject(name);
            _made.Add(go);
            return go.AddComponent<Tower>();
        }

        [Test]
        public void Path_MeasuresLengthAndPointsAlongIt()
        {
            EnemyPath path = MakePath();

            Assert.AreEqual(7f, path.Length, 0.001f);
            Assert.AreEqual(new Vector3(2, 0, 0), path.PointAt(2f));
            Assert.AreEqual(new Vector3(4, 1, 0), path.PointAt(5f));
            Assert.AreEqual(new Vector3(0, 0, 0), path.PointAt(-3f));
            Assert.AreEqual(new Vector3(4, 3, 0), path.PointAt(99f));
        }

        [Test]
        public void Path_MeasuresDistanceToAndAlongTheRoute()
        {
            EnemyPath path = MakePath();

            Assert.AreEqual(1f, path.DistanceToRoute(new Vector2(2f, 1f)), 0.001f);
            Assert.AreEqual(2f, path.DistanceToRoute(new Vector2(6f, 2f)), 0.001f);
            Assert.AreEqual(2f, path.DistanceAlong(new Vector2(2f, 1f)), 0.001f);
            Assert.AreEqual(6f, path.DistanceAlong(new Vector2(6f, 2f)), 0.001f);
            Assert.AreEqual(7f, path.DistanceAlong(new Vector2(4f, 9f)), 0.001f);
        }

        [Test]
        public void TowerList_HoldsStandingTowers_IncludingSwitchedOffOnes()
        {
            Tower.GetActive(_towers);
            int before = _towers.Count;

            Tower standing = MakeTower("Standing");
            Tower stunned = MakeTower("Stunned");
            Tower carried = MakeTower("Carried");
            stunned.enabled = false;
            carried.gameObject.SetActive(false);

            Tower.GetActive(_towers);
            Assert.AreEqual(before + 2, _towers.Count);
            CollectionAssert.Contains(_towers, standing);
            CollectionAssert.Contains(_towers, stunned);
            CollectionAssert.DoesNotContain(_towers, carried);

            carried.gameObject.SetActive(true);
            Object.DestroyImmediate(standing.gameObject);

            Tower.GetActive(_towers);
            CollectionAssert.Contains(_towers, carried);
            CollectionAssert.DoesNotContain(_towers, standing);
            Assert.AreEqual(Object.FindObjectsByType<Tower>().Length, _towers.Count, "the list should match a scene search");
        }
    }
}
#endif
