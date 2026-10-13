using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.GameFlow;
using NUnit.Framework;
using UnityEngine;

namespace HoldTheHill.Tests
{
    public class QueenTests
    {
        private GameObject _queenObject;
        private Queen _queen;
        private GameObject _enemyObject;

        [SetUp]
        public void SetUp()
        {
            _queenObject = new GameObject("TestQueen");
            _queen = _queenObject.AddComponent<Queen>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_queenObject != null)
            {
                Object.DestroyImmediate(_queenObject);
            }

            if (_enemyObject != null)
            {
                Object.DestroyImmediate(_enemyObject);
            }
        }

        // An enemy with the given breach damage. EnemyHealth's field is private, as it should be,
        // so the test sets it the way the Inspector would.
        private GameObject MakeEnemy(float breachDamage)
        {
            _enemyObject = new GameObject("TestEnemy");
            var health = _enemyObject.AddComponent<EnemyHealth>();
            var so = new UnityEditor.SerializedObject(health);
            so.FindProperty("_breachDamage").floatValue = breachDamage;
            so.ApplyModifiedPropertiesWithoutUndo();
            return _enemyObject;
        }

        [Test]
        public void Health_StartsAtMax_AndTheRunIsNotOver()
        {
            Assert.That(_queen.CurrentHealth, Is.EqualTo(100f));
            Assert.That(_queen.MaxHealth, Is.EqualTo(100f));
            Assert.That(_queen.IsFinished, Is.False);
            Assert.That(_queen.IsGameOver, Is.False);
        }

        [Test]
        public void TakeDamage_ReducesHealth_AndReportsCurrentAndMax()
        {
            float reportedCurrent = 0f;
            float reportedMax = 0f;
            _queen.OnHealthChanged += (current, max) =>
            {
                reportedCurrent = current;
                reportedMax = max;
            };

            _queen.TakeDamage(25.5f);

            Assert.That(_queen.CurrentHealth, Is.EqualTo(74.5f).Within(0.0001f));
            Assert.That(reportedCurrent, Is.EqualTo(74.5f).Within(0.0001f));
            Assert.That(reportedMax, Is.EqualTo(100f).Within(0.0001f));
        }

        [Test]
        public void EnemyReachingTheEnd_TakesItsBreachDamage_AndSaysWhere()
        {
            GameObject enemy = MakeEnemy(12f);
            enemy.transform.position = new Vector3(3f, 4f, 0f);
            float breachDamage = 0f;
            Vector3 breachAt = Vector3.zero;
            _queen.Breached += (damage, at) =>
            {
                breachDamage = damage;
                breachAt = at;
            };

            _queen.HandleEnemyReachedEnd(enemy);

            Assert.That(_queen.CurrentHealth, Is.EqualTo(88f));
            Assert.That(breachDamage, Is.EqualTo(12f));
            Assert.That(breachAt, Is.EqualTo(new Vector3(3f, 4f, 0f)));
        }

        [Test]
        public void EnemyWithNoBreachDamage_DoesNoHarm()
        {
            _queen.HandleEnemyReachedEnd(MakeEnemy(0f));

            Assert.That(_queen.CurrentHealth, Is.EqualTo(100f));
        }

        [Test]
        public void EnemyThatStopsAtTheHill_DoesNoHarm()
        {
            GameObject enemy = MakeEnemy(12f);
            var mover = enemy.AddComponent<EnemyMover>();
            var so = new UnityEditor.SerializedObject(mover);
            so.FindProperty("_onReachEnd").enumValueIndex = (int)EnemyMover.EndBehaviour.Stop;
            so.ApplyModifiedPropertiesWithoutUndo();

            _queen.HandleEnemyReachedEnd(enemy);

            Assert.That(_queen.CurrentHealth, Is.EqualTo(100f));
        }

        [Test]
        public void HealthReachingZero_EndsTheRunInDefeat_ExactlyOnce()
        {
            int gameOverCount = 0;
            int finishedCount = 0;
            bool? victory = null;
            _queen.OnGameOver += () => gameOverCount++;
            _queen.Finished += v =>
            {
                finishedCount++;
                victory = v;
            };

            _queen.TakeDamage(150f);

            Assert.That(_queen.CurrentHealth, Is.EqualTo(0f));
            Assert.That(_queen.IsGameOver, Is.True);
            Assert.That(gameOverCount, Is.EqualTo(1));
            Assert.That(finishedCount, Is.EqualTo(1));
            Assert.That(victory, Is.False);

            // Damage after the end changes nothing and fires nothing.
            _queen.TakeDamage(10f);
            Assert.That(gameOverCount, Is.EqualTo(1));
            Assert.That(finishedCount, Is.EqualTo(1));
        }

        [Test]
        public void Victory_EndsTheRun_AndStopsFurtherDamage()
        {
            bool? victory = null;
            _queen.Finished += v => victory = v;

            _queen.TriggerVictory();
            _queen.TakeDamage(50f);

            Assert.That(_queen.IsFinished, Is.True);
            Assert.That(_queen.IsVictory, Is.True);
            Assert.That(_queen.IsGameOver, Is.False);
            Assert.That(victory, Is.True);
            Assert.That(_queen.CurrentHealth, Is.EqualTo(100f));
        }

        [Test]
        public void Heal_RestoresHealthUpToTheMaximum()
        {
            _queen.TakeDamage(40f);
            Assert.That(_queen.CurrentHealth, Is.EqualTo(60f).Within(0.0001f));

            _queen.Heal(20f);
            Assert.That(_queen.CurrentHealth, Is.EqualTo(80f).Within(0.0001f));

            _queen.Heal(50f);
            Assert.That(_queen.CurrentHealth, Is.EqualTo(100f).Within(0.0001f));
        }

        [Test]
        public void ResetHealth_RefillsAndClearsTheEnd()
        {
            _queen.TakeDamage(100f);
            Assert.That(_queen.IsGameOver, Is.True);

            _queen.ResetHealth();

            Assert.That(_queen.CurrentHealth, Is.EqualTo(100f));
            Assert.That(_queen.IsFinished, Is.False);
        }

        [Test]
        public void RestoreHealth_PutsBackASavedValue_ClampedToTheMaximum()
        {
            _queen.TakeDamage(100f);

            _queen.RestoreHealth(35f);
            Assert.That(_queen.CurrentHealth, Is.EqualTo(35f));
            Assert.That(_queen.IsFinished, Is.False);

            _queen.RestoreHealth(500f);
            Assert.That(_queen.CurrentHealth, Is.EqualTo(100f));
        }

        [Test]
        public void SetBonusHealth_RaisesTheMaximumAboveTheBuiltValue_AndZeroPutsItBack()
        {
            _queen.SetBonusHealth(25f);
            Assert.That(_queen.MaxHealth, Is.EqualTo(125f));
            Assert.That(_queen.CurrentHealth, Is.EqualTo(125f));

            _queen.SetBonusHealth(0f);
            Assert.That(_queen.MaxHealth, Is.EqualTo(100f));
            Assert.That(_queen.CurrentHealth, Is.EqualTo(100f));
        }
    }
}
