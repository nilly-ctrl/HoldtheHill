using HoldTheHill.Features.Hud;
using System.Collections.Generic;
using System.Linq;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Towers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The regular enemy prefabs: one base prefab and a variant per enemy type.
    /// </summary>
    internal static class GrayboxEnemyPrefabBuilder
    {
        internal static GameObject BuildEnemyPrefab(
            string name, Sprite sprite, float health, float speed, Color color, float size, int bounty = 10, float breach = 0f)
        {
            return GrayboxBuildKit.SavePrefab(BuildBaseEnemyObject(name, sprite, health, speed, color, size, bounty, breach: breach), name);
        }

        internal static GameObject BuildShieldedEnemyPrefab(
            string name, Sprite sprite, float health, float shield, float speed, Color color, float size, int bounty = 25, float breach = 0f)
        {
            var go = BuildBaseEnemyObject(name, sprite, health, speed, color, size, bounty, breach: breach);
            EnemyShield s = go.AddComponent<EnemyShield>();
            GrayboxBuildKit.Apply(s, so =>
            {
                so.FindProperty("_maxShield").floatValue = shield;
                so.FindProperty("_regenRate").floatValue = 12f;
                so.FindProperty("_regenDelay").floatValue = 4.0f;
            });
            return GrayboxBuildKit.SavePrefab(go, name);
        }

        internal static GameObject BuildSplitterEnemyPrefab(
            string name, Sprite sprite, GameObject swarmPrefab, float health, float speed, Color color, float size, int bounty = 20, float breach = 0f)
        {
            var go = BuildBaseEnemyObject(name, sprite, health, speed, color, size, bounty, breach: breach);
            EnemySplitter splitter = go.AddComponent<EnemySplitter>();
            GrayboxBuildKit.Apply(splitter, so =>
            {
                so.FindProperty("_miniAntPrefab").objectReferenceValue = swarmPrefab;
                so.FindProperty("_splitCount").intValue = 3;
            });
            return GrayboxBuildKit.SavePrefab(go, name);
        }

        internal static GameObject BuildHealerEnemyPrefab(
            string name, Sprite sprite, float health, float speed, Color color, float size, int bounty = 30, float breach = 0f)
        {
            var go = BuildBaseEnemyObject(name, sprite, health, speed, color, size, bounty, breach: breach);
            EnemyHealer healer = go.AddComponent<EnemyHealer>();
            GrayboxBuildKit.Apply(healer, so =>
            {
                so.FindProperty("_healRadius").floatValue = 2.8f;
                so.FindProperty("_healInterval").floatValue = 1.4f;
                so.FindProperty("_healAmount").floatValue = 20f;
            });
            return GrayboxBuildKit.SavePrefab(go, name);
        }

        internal const string EnemyBaseName = "GrayboxEnemyBase";

        internal static GameObject s_enemyBase;

        /// <summary>Forgets the base prefab from the last build, so a rebuild makes a fresh one.</summary>
        internal static void Reset() => s_enemyBase = null;

        // What every enemy shares: a body towers can hit, health, a mover and a health bar.
        // A change here reaches all of them through the variants.
        internal static GameObject EnemyBase(Sprite sprite)
        {
            if (s_enemyBase != null)
            {
                return s_enemyBase;
            }

            var go = new GameObject(EnemyBaseName);
            GrayboxBuildKit.AddSprite(go, sprite, Color.white, 0.6f, sortingOrder: 2);

            go.AddComponent<CircleCollider2D>().radius = 0.3f;

            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;

            GrayboxBuildKit.Apply(go.AddComponent<EnemyHealth>(), so => so.FindProperty("_despawnDelay").floatValue = 0f);
            GrayboxBuildKit.Apply(go.AddComponent<EnemyMover>(),
                so => so.FindProperty("_onReachEnd").enumValueIndex = (int)EnemyMover.EndBehaviour.Despawn);
            go.AddComponent<EnemyHealthBar>();

            s_enemyBase = GrayboxBuildKit.SavePrefab(go, EnemyBaseName);
            return s_enemyBase;
        }

        /// <summary>
        /// An unsaved variant of the enemy base (or of <paramref name="basePrefab"/>) with its own
        /// look and numbers. Add what makes the enemy special, then <see cref="SavePrefab"/> it.
        /// </summary>
        internal static GameObject BuildBaseEnemyObject(
            string name, Sprite sprite, float health, float speed, Color color, float size, int bounty = 10,
            GameObject basePrefab = null, float breach = 0f)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab != null ? basePrefab : EnemyBase(sprite));
            go.name = name;

            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.color = color;
            renderer.size = new Vector2(size, size);
            GrayboxBuildKit.Animate(go);

            go.GetComponent<CircleCollider2D>().radius = size * 0.5f;

            GrayboxBuildKit.Apply(go.GetComponent<EnemyHealth>(), so =>
            {
                so.FindProperty("_maxHealth").floatValue = health;
                so.FindProperty("_bountyValue").intValue = bounty;
                so.FindProperty("_breachDamage").floatValue = breach;
            });

            GrayboxBuildKit.Apply(go.GetComponent<EnemyMover>(), so => so.FindProperty("_speed").floatValue = speed);

            return go;
        }
    }
}
