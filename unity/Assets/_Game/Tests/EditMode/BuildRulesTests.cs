using System.Collections.Generic;
using HoldTheHill.Features.Towers;
using NUnit.Framework;
using UnityEngine;

namespace HoldTheHill.Tests
{
    public class BuildRulesTests
    {
        private readonly List<GameObject> _made = new List<GameObject>();
        private readonly List<Tower> _towers = new List<Tower>();
        private readonly List<IBuildBlocker> _blockers = new List<IBuildBlocker>();

        private sealed class Wall : IBuildBlocker
        {
            private readonly Vector2Int _cell;
            public Wall(int x, int y) => _cell = new Vector2Int(x, y);
            public bool Blocks(Vector3 cellCentre) => BuildGrid.CellOf(cellCentre) == _cell;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _made)
            {
                Object.DestroyImmediate(go);
            }

            _made.Clear();
            _towers.Clear();
            _blockers.Clear();
        }

        private Tower TowerAt(float x, float y)
        {
            var go = new GameObject("Tower");
            _made.Add(go);
            go.transform.position = new Vector3(x, y, 0f);
            Tower tower = go.AddComponent<Tower>();
            _towers.Add(tower);
            return tower;
        }

        [Test]
        public void Snap_GoesToTheNearestWholeUnit_AndDropsZ()
        {
            Assert.That(BuildGrid.Snap(new Vector3(2.4f, -1.6f, 5f)), Is.EqualTo(new Vector3(2f, -2f, 0f)));
            Assert.That(BuildGrid.CellOf(new Vector3(2.4f, -1.6f, 5f)), Is.EqualTo(new Vector2Int(2, -2)));
        }

        [Test]
        public void AnEmptyCell_CanBeBuiltOn()
        {
            Assert.That(BuildRules.CanBuildAt(new Vector3(3.2f, 3.1f), _towers, null, 0.8f, _blockers), Is.True);
        }

        [Test]
        public void ATowerTooClose_BlocksTheCell_UnlessItIsTheOneBeingMoved()
        {
            Tower standing = TowerAt(3f, 3f);

            Assert.That(BuildRules.CanBuildAt(new Vector3(3f, 3f), _towers, null, 0.8f, _blockers), Is.False);
            Assert.That(BuildRules.CanBuildAt(new Vector3(4f, 3f), _towers, null, 0.8f, _blockers), Is.True, "a whole cell away is fine");
            Assert.That(BuildRules.CanBuildAt(new Vector3(3f, 3f), _towers, standing, 0.8f, _blockers), Is.True, "the tower being moved does not block itself");
        }

        [Test]
        public void ABlocker_ForbidsItsCell_AndNothingElse()
        {
            _blockers.Add(new Wall(5, 5));

            Assert.That(BuildRules.CanBuildAt(new Vector3(5.3f, 4.8f), _towers, null, 0.8f, _blockers), Is.False, "the position snaps into the walled cell");
            Assert.That(BuildRules.CanBuildAt(new Vector3(6f, 5f), _towers, null, 0.8f, _blockers), Is.True);
        }

        [Test]
        public void TowerAt_FindsTheOneInReach_AndSkipsTheIgnoredOne()
        {
            Tower near = TowerAt(1f, 1f);
            TowerAt(9f, 9f);

            Assert.That(BuildRules.TowerAt(new Vector3(1.3f, 1f), _towers, 0.5f), Is.SameAs(near));
            Assert.That(BuildRules.TowerAt(new Vector3(1.3f, 1f), _towers, 0.5f, near), Is.Null);
            Assert.That(BuildRules.TowerAt(new Vector3(5f, 5f), _towers, 0.5f), Is.Null);
        }

        [Test]
        public void BuildBlockers_RegistryAnswersForAllRegisteredBlockers()
        {
            var wall = new Wall(2, 2);
            BuildBlockers.Register(wall);
            BuildBlockers.Register(wall);
            try
            {
                Assert.That(BuildBlockers.All.Count, Is.EqualTo(1), "registering twice counts once");
                Assert.That(BuildBlockers.Blocked(new Vector3(2f, 2f)), Is.True);
                Assert.That(BuildBlockers.Blocked(new Vector3(3f, 2f)), Is.False);
            }
            finally
            {
                BuildBlockers.Unregister(wall);
            }

            Assert.That(BuildBlockers.Blocked(new Vector3(2f, 2f)), Is.False);
        }
    }
}
