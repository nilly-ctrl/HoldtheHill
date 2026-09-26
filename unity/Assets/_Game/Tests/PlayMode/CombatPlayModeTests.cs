using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Towers;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HoldTheHill.Tests.PlayMode
{
    /// <summary>
    /// Smoke tests that actually run the combat loop, rather than only proving it compiles.
    /// Each test builds the few objects it needs, so none of them depend on a scene asset.
    /// </summary>
    /// <remarks>
    /// Tuning values are private serialized fields, so these set them by reflection. Using
    /// <c>SerializedObject</c> would drag UnityEditor into an assembly that also builds for
    /// players, and adding public setters purely for tests would widen the runtime API.
    /// </remarks>
    public class CombatPlayModeTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

        private readonly List<GameObject> _spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _spawned.Count; i++)
            {
                if (_spawned[i] != null)
                {
                    Object.DestroyImmediate(_spawned[i]);
                }
            }

            _spawned.Clear();
        }

        [UnityTest]
        public IEnumerator LinearProjectile_DamagesEnemyItFliesInto()
        {
            EnemyHealth enemy = MakeEnemy(new Vector2(2f, 0f), health: 100f);
            Projectile prefab = MakeProjectilePrefab(FlightMode.Linear, speed: 8f, damage: 25f);
            ProjectilePool pool = MakePool(prefab);

            yield return null; // let colliders register with the physics world

            Projectile shot = pool.Get();
            shot.Launch(Vector2.zero, enemy, Vector2.right);

            yield return new WaitForSeconds(0.75f);

            Assert.Less(enemy.CurrentHealth, 100f, "The projectile never hit the enemy.");
            Assert.AreEqual(75f, enemy.CurrentHealth, 0.01f, "Wrong damage applied on a direct hit.");
        }

        [UnityTest]
        public IEnumerator HomingProjectile_TurnsTowardATargetOffToTheSide()
        {
            // Fired straight up with the target off to the right: only steering can connect.
            EnemyHealth enemy = MakeEnemy(new Vector2(3f, 0f), health: 100f);
            Projectile prefab = MakeProjectilePrefab(FlightMode.Homing, speed: 6f, damage: 20f);
            ProjectilePool pool = MakePool(prefab);

            yield return null;

            Projectile shot = pool.Get();
            shot.Launch(Vector2.zero, enemy, Vector2.up);

            yield return new WaitForSeconds(2.5f);

            Assert.Less(enemy.CurrentHealth, 100f, "The homing shot never curved back onto its target.");
        }

        [UnityTest]
        public IEnumerator StatusEffect_TicksDamageAndSlowsTheEnemy()
        {
            EnemyHealth enemy = MakeEnemy(Vector2.zero, health: 100f);

            var poison = new StatusEffectData
            {
                EffectName = "Poison",
                DamagePerTick = 5f,
                TickInterval = 0.1f,
                Duration = 0.55f,
                SlowMultiplier = 0.5f,
            };

            enemy.ApplyStatusEffect(poison);
            yield return null;

            Assert.AreEqual(0.5f, enemy.SpeedMultiplier, 0.01f, "The slow was not applied.");

            yield return new WaitForSeconds(1f);

            Assert.Less(enemy.CurrentHealth, 100f, "The damage-over-time never ticked.");
            Assert.AreEqual(1f, enemy.SpeedMultiplier, 0.01f, "The slow did not wear off when the effect expired.");
        }

        [UnityTest]
        public IEnumerator StatusEffect_ReapplyingRefreshesRatherThanStacking()
        {
            EnemyHealth enemy = MakeEnemy(Vector2.zero, health: 100f);

            var slow = new StatusEffectData
            {
                EffectName = "Chill",
                DamagePerTick = 0f,
                TickInterval = 0.1f,
                Duration = 5f,
                SlowMultiplier = 0.6f,
            };

            enemy.ApplyStatusEffect(slow);
            enemy.ApplyStatusEffect(slow);
            enemy.ApplyStatusEffect(slow);
            yield return null;

            // Stacking multiplicatively would give 0.6^3 = 0.216 and freeze the enemy solid.
            Assert.AreEqual(0.6f, enemy.SpeedMultiplier, 0.01f, "Repeated slows stacked instead of refreshing.");
        }

        [UnityTest]
        public IEnumerator EnemyHealth_RaisesDefeatedEventExactlyOnce()
        {
            EnemyHealth enemy = MakeEnemy(Vector2.zero, health: 10f);

            int defeatedCount = 0;
            void Handler(GameObject go) => defeatedCount++;

            EnemyHealth.Defeated += Handler;
            try
            {
                enemy.TakeDamage(new DamageInfo(50f, null, Vector2.zero, DamageType.Physical));
                enemy.TakeDamage(new DamageInfo(50f, null, Vector2.zero, DamageType.Physical));
                yield return null;

                Assert.AreEqual(1, defeatedCount, "Death fired the wrong number of times.");
            }
            finally
            {
                EnemyHealth.Defeated -= Handler;
            }
        }

        [UnityTest]
        public IEnumerator Tower_ClosestPriority_PicksTheNearerEnemy()
        {
            EnemyHealth near = MakeEnemy(new Vector2(1f, 0f), health: 100f);
            MakeEnemy(new Vector2(3f, 0f), health: 100f);

            Tower tower = MakeTower(Vector2.zero, TargetingPriority.Closest, range: 5f);

            yield return null;

            Assert.AreSame(near, tower.AcquireTarget(), "Closest targeting did not pick the nearer enemy.");
        }

        [UnityTest]
        public IEnumerator Tower_WeakestPriority_PicksTheLowestHealthEnemy()
        {
            MakeEnemy(new Vector2(1f, 0f), health: 100f);
            EnemyHealth weak = MakeEnemy(new Vector2(3f, 0f), health: 20f);

            Tower tower = MakeTower(Vector2.zero, TargetingPriority.Weakest, range: 5f);

            yield return null;

            // The weak one is further away, so distance alone would pick the other.
            Assert.AreSame(weak, tower.AcquireTarget(), "Weakest targeting did not pick the lowest-health enemy.");
        }

        [UnityTest]
        public IEnumerator ProjectilePool_ReusesInstancesRatherThanAllocating()
        {
            Projectile prefab = MakeProjectilePrefab(FlightMode.Linear, speed: 5f, damage: 1f);
            ProjectilePool pool = MakePool(prefab);

            yield return null;

            Projectile first = pool.Get();
            pool.Release(first);
            Projectile second = pool.Get();

            Assert.AreSame(first, second, "The pool allocated a new projectile instead of reusing the free one.");
        }

        // ---------- helpers ----------

        private EnemyHealth MakeEnemy(Vector2 position, float health)
        {
            var go = new GameObject("TestEnemy");
            go.SetActive(false);
            go.transform.position = position;
            _spawned.Add(go);

            CircleCollider2D collider = go.AddComponent<CircleCollider2D>();
            collider.radius = 0.4f;

            EnemyHealth enemyHealth = go.AddComponent<EnemyHealth>();
            SetPrivate(enemyHealth, "_maxHealth", health);

            // Enabling now runs OnEnable, which copies max health into current health.
            go.SetActive(true);

            return enemyHealth;
        }

        private Projectile MakeProjectilePrefab(FlightMode mode, float speed, float damage)
        {
            var go = new GameObject("TestProjectile");
            go.SetActive(false);
            _spawned.Add(go);

            Projectile projectile = go.AddComponent<Projectile>();
            SetPrivate(projectile, "_flightMode", mode);
            SetPrivate(projectile, "_speed", speed);
            SetPrivate(projectile, "_damage", damage);
            SetPrivate(projectile, "_lifetime", 5f);
            SetPrivate(projectile, "_hitRadius", 0.4f);
            SetPrivate(projectile, "_turnRate", 220f);

            return projectile;
        }

        private ProjectilePool MakePool(Projectile prefab)
        {
            ProjectilePool pool = ProjectilePool.For(prefab, prewarm: 2);
            _spawned.Add(pool.gameObject);
            return pool;
        }

        private Tower MakeTower(Vector2 position, TargetingPriority priority, float range)
        {
            var go = new GameObject("TestTower");
            go.SetActive(false);
            go.transform.position = position;
            _spawned.Add(go);

            Tower tower = go.AddComponent<Tower>();
            SetPrivate(tower, "_range", range);
            SetPrivate(tower, "_priority", priority);

            go.SetActive(true);

            return tower;
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
