using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HoldTheHill.Features.Enemies;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HoldTheHill.Tests.PlayMode
{
    /// <summary>
    /// What an enemy does when it runs out of path, for each <see cref="EnemyMover.EndBehaviour"/>.
    /// See docs/ENEMY_END_OF_PATH_NOTE.md for the decisions these pin down.
    /// </summary>
    public class EnemyEndOfPathTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        private const float SecondsToWalk = 3f;

        private readonly List<GameObject> _spawned = new List<GameObject>();
        private readonly List<GameObject> _reachedEnd = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            _reachedEnd.Clear();
            EnemyMover.ReachedEnd += OnReachedEnd;
        }

        [TearDown]
        public void TearDown()
        {
            EnemyMover.ReachedEnd -= OnReachedEnd;
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null)
                {
                    Object.DestroyImmediate(_spawned[i]);
                }
            }

            _spawned.Clear();
        }

        private void OnReachedEnd(GameObject enemy) => _reachedEnd.Add(enemy);

        [UnityTest]
        public IEnumerator Despawn_PooledEnemy_IsDeactivatedNotDestroyed()
        {
            EnemyPath path = MakePath(new Vector2(0f, 0f), new Vector2(1f, 0f));
            GameObject enemy = MakeEnemy(path, EnemyMover.EndBehaviour.Despawn, pooled: true);

            yield return WalkUntil(() => enemy == null || !enemy.activeSelf || enemy.GetComponent<EnemyMover>().HasFinished);
            yield return null; // a Destroy would land at the end of this frame

            Assert.IsTrue(enemy != null, "A pooled enemy was destroyed on reaching the end instead of being returned.");
            Assert.IsFalse(enemy.activeSelf, "A pooled enemy should be deactivated when it walks in.");
            Assert.AreEqual(1, _reachedEnd.Count, "Walking in should be announced exactly once.");
        }

        [UnityTest]
        public IEnumerator Despawn_UnpooledEnemy_IsDestroyed()
        {
            EnemyPath path = MakePath(new Vector2(0f, 0f), new Vector2(1f, 0f));
            GameObject enemy = MakeEnemy(path, EnemyMover.EndBehaviour.Despawn, pooled: false);

            yield return WalkUntil(() => enemy == null || enemy.GetComponent<EnemyMover>().HasFinished);
            yield return null;

            Assert.IsTrue(enemy == null, "An enemy without pooling should be destroyed when it walks in.");
            Assert.AreEqual(1, _reachedEnd.Count, "Walking in should be announced exactly once.");
        }

        [UnityTest]
        public IEnumerator Loop_NeverReportsAnArrival()
        {
            EnemyPath path = MakePath(new Vector2(0f, 0f), new Vector2(1f, 0f));
            GameObject enemy = MakeEnemy(path, EnemyMover.EndBehaviour.Loop, pooled: false);
            EnemyMover mover = enemy.GetComponent<EnemyMover>();
            int arrived = 0;
            mover.Arrived += () => arrived++;

            // At 50 units a second on a one-unit route this is dozens of laps at any frame rate.
            yield return new WaitForSeconds(1f);

            Assert.AreEqual(0, _reachedEnd.Count, "A looping enemy is never a breach; ReachedEnd should stay quiet.");
            Assert.AreEqual(0, arrived, "A looping enemy never arrives; Arrived should stay quiet.");
            Assert.IsTrue(enemy != null && enemy.activeSelf, "A looping enemy should still be in play.");
            Assert.IsFalse(mover.HasFinished, "A looping enemy never finishes.");
        }

        [UnityTest]
        public IEnumerator Stop_ReportsTheArrivalAndStaysPut()
        {
            EnemyPath path = MakePath(new Vector2(0f, 0f), new Vector2(1f, 0f));
            GameObject enemy = MakeEnemy(path, EnemyMover.EndBehaviour.Stop, pooled: false);
            EnemyMover mover = enemy.GetComponent<EnemyMover>();

            yield return WalkUntil(() => mover.HasFinished);
            yield return null;

            Assert.AreEqual(1, _reachedEnd.Count, "Stopping at the end should be announced once; listeners decide what it means.");
            Assert.AreSame(enemy, _reachedEnd[0]);
            Assert.IsTrue(enemy != null && enemy.activeSelf, "A stopped enemy stays in the scene.");
            Assert.IsFalse(mover.DespawnsAtEnd);
        }

        // ---------- helpers ----------

        // Frame-counted waits are not enough: in batch mode frames are so short the enemy barely moves.
        private IEnumerator WalkUntil(System.Func<bool> done)
        {
            float deadline = Time.time + SecondsToWalk;
            while (!done() && Time.time < deadline)
            {
                yield return null;
            }
        }

        private EnemyPath MakePath(params Vector2[] points)
        {
            var go = new GameObject("TestPath");
            _spawned.Add(go);
            for (int i = 0; i < points.Length; i++)
            {
                var waypoint = new GameObject("Waypoint" + i);
                waypoint.transform.SetParent(go.transform);
                waypoint.transform.position = points[i];
            }

            // Awake reads the children into Waypoints, so the component goes on last.
            return go.AddComponent<EnemyPath>();
        }

        private GameObject MakeEnemy(EnemyPath path, EnemyMover.EndBehaviour endBehaviour, bool pooled)
        {
            var go = new GameObject("TestEnemy");
            go.SetActive(false);
            _spawned.Add(go);

            EnemyHealth health = go.AddComponent<EnemyHealth>();
            SetPrivate(health, "_deactivateInsteadOfDestroy", pooled);

            EnemyMover mover = go.AddComponent<EnemyMover>();
            SetPrivate(mover, "_path", path);
            SetPrivate(mover, "_speed", 50f); // crosses the one-unit test route in a frame
            SetPrivate(mover, "_onReachEnd", endBehaviour);

            go.SetActive(true);
            mover.SnapToStart();
            return go;
        }

        /// <summary>Sets a private serialized field, walking up the type hierarchy to find it.</summary>
        private static void SetPrivate(object target, string fieldName, object value)
        {
            System.Type type = target.GetType();
            while (type != null)
            {
                FieldInfo field = type.GetField(fieldName, PrivateInstance);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }

                type = type.BaseType;
            }

            Assert.Fail($"No private field '{fieldName}' on {target.GetType().Name} or its base types.");
        }
    }
}
