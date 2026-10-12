using System;
using System.Collections.Generic;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Towers;
using UnityEditor;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl.Editor
{
    /// <summary>
    /// Reads tower prefabs and a wave asset and turns them into the plain data
    /// <see cref="BalanceSimulator"/> runs on. Numbers that live on private fields are read through
    /// SerializedObject, so nothing is instantiated and no scene is touched.
    /// </summary>
    public static class BalanceInputBuilder
    {
        // Assumed number of enemies a splash, pulse or mine catches in a crowd.
        private const int AssumedSplashTargets = 3;
        private const int AssumedPulseTargets = 4;

        // Orbiters touch an enemy only part of the time.
        private const float OrbitDutyCycle = 0.5f;

        private static readonly HashSet<string> KnownWeapons = new HashSet<string>
        {
            "ChainLightning", "ContinuousBeam", "FrostAuraTower", "KnockbackTower",
            "MineLayerTower", "OrbitingDamageField", "GrayboxMeleeTower"
        };

        public static List<BalanceTower> BuildTowers(GrayboxTowerCatalog catalog, List<string> notes)
        {
            var towers = new List<BalanceTower>();
            if (catalog == null)
            {
                notes.Add("No tower catalog given.");
                return towers;
            }

            for (int i = 0; i < catalog.Count; i++)
            {
                GrayboxTowerData data = catalog[i];
                if (data != null)
                {
                    towers.Add(FromData(data, notes));
                }
            }

            return towers;
        }

        public static List<BalanceWave> BuildWaves(MapWaveDataSO asset, List<string> notes)
        {
            var waves = new List<BalanceWave>();
            if (asset == null || asset.waves == null)
            {
                notes.Add("No wave asset given.");
                return waves;
            }

            var cache = new Dictionary<GameObject, BalanceEnemy>();
            var reportedSplitters = new HashSet<string>();

            for (int w = 0; w < asset.waves.Count; w++)
            {
                Wave wave = asset.waves[w];
                var enemies = new List<BalanceEnemy>();
                float time = 0f;

                if (wave != null && wave.enemies != null)
                {
                    foreach (EnemySpawnEntry entry in wave.enemies)
                    {
                        if (entry == null || entry.enemyPrefab == null)
                        {
                            notes.Add($"Wave {w + 1}: an entry has no enemy prefab and was skipped.");
                            continue;
                        }

                        if (!cache.TryGetValue(entry.enemyPrefab, out BalanceEnemy template))
                        {
                            template = EnemyFromPrefab(entry.enemyPrefab, notes, reportedSplitters);
                            cache[entry.enemyPrefab] = template;
                        }

                        if (template != null)
                        {
                            enemies.Add(new BalanceEnemy(template.Name, time, template.Hp, template.Speed,
                                template.Bounty, template.BreachDamage));
                        }

                        time += Mathf.Max(0f, entry.delayBeforeNext);
                    }
                }

                string name = wave != null && !string.IsNullOrEmpty(wave.waveName) ? wave.waveName : $"Wave {w + 1}";
                waves.Add(new BalanceWave(name, enemies));
            }

            return waves;
        }

        // ---------- towers ----------

        private static BalanceTower FromData(GrayboxTowerData data, List<string> notes)
        {
            string label = string.IsNullOrEmpty(data.DisplayName) ? data.name : data.DisplayName;
            Tower tower = data.Prefab;

            if (tower == null)
            {
                notes.Add($"{label}: no prefab, no damage modelled.");
                return new BalanceTower(label, data.BaseCost, 0f, 0f, note: "no prefab");
            }

            float interval = Mathf.Max(0.05f, tower.FireInterval);
            float towerRange = tower.Range;
            float reach = 0f;
            float dps = 0f;
            int targets = 1;
            float falloff = 1f;
            var parts = new List<string>();

            var towerObject = new SerializedObject(tower);
            var projectile = ObjectField(towerObject, "_projectilePrefab") as Projectile;
            if (projectile != null)
            {
                var shot = new SerializedObject(projectile);
                float damage = Float(shot, "_damage");
                dps += damage / interval;
                reach = Mathf.Max(reach, towerRange);
                parts.Add($"projectile {damage:0.#} per {interval:0.##}s");

                if (Float(shot, "_splashRadius") > 0f && Float(shot, "_splashDamage") > 0f && damage > 0f)
                {
                    targets = Mathf.Max(targets, AssumedSplashTargets);
                    falloff = Mathf.Min(falloff, Mathf.Clamp01(Float(shot, "_splashDamage") / damage));
                    parts.Add($"splash assumed to catch {AssumedSplashTargets}");
                }

                if (Bool(shot, "_isPiercing"))
                {
                    targets = Mathf.Max(targets, Mathf.Max(1, Int(shot, "_maxPierceHits")));
                    parts.Add("piercing");
                }
            }

            foreach (TowerWeapon weapon in tower.GetComponents<TowerWeapon>())
            {
                string kind = KnownKind(weapon.GetType());
                if (kind == null)
                {
                    parts.Add($"{weapon.GetType().Name}: no damage model");
                    continue;
                }

                var so = new SerializedObject(weapon);
                switch (kind)
                {
                    case "ChainLightning":
                    {
                        float damage = Float(so, "_damage");
                        dps += damage / interval;
                        reach = Mathf.Max(reach, towerRange);
                        targets = Mathf.Max(targets, Mathf.Max(1, Int(so, "_maxTargets")));
                        falloff = Mathf.Min(falloff, Mathf.Clamp(Float(so, "_damageFalloff"), 0.1f, 1f));
                        parts.Add($"chain {damage:0.#} x{Int(so, "_maxTargets")}");
                        break;
                    }

                    case "ContinuousBeam":
                    {
                        float average = (Float(so, "_baseDamagePerSecond") + Float(so, "_maxDamagePerSecond")) * 0.5f;
                        dps += average;
                        reach = Mathf.Max(reach, towerRange);
                        parts.Add($"beam averaged to {average:0.#}/s (it ramps up on one target)");
                        break;
                    }

                    case "FrostAuraTower":
                    {
                        float pulse = Mathf.Max(0.05f, Float(so, "_pulseInterval"));
                        dps += Float(so, "_damagePerPulse") / pulse;
                        reach = Mathf.Max(reach, Float(so, "_radius"));
                        targets = Mathf.Max(targets, AssumedPulseTargets);
                        parts.Add("frost pulse damage only; the slow is not modelled");
                        break;
                    }

                    case "KnockbackTower":
                    {
                        float pulse = Mathf.Max(0.05f, Float(so, "_pulseInterval"));
                        dps += Float(so, "_damage") / pulse;
                        reach = Mathf.Max(reach, Float(so, "_radius"));
                        targets = Mathf.Max(targets, AssumedPulseTargets);
                        parts.Add("knockback pulse damage only; the push is not modelled");
                        break;
                    }

                    case "MineLayerTower":
                    {
                        float lay = Mathf.Max(0.1f, Float(so, "_layInterval"));
                        var mine = ObjectField(so, "_minePrefab") as GameObject;
                        var mineComponent = mine != null ? mine.GetComponent<ProximityMine>() : null;
                        float mineDamage = mineComponent != null ? Float(new SerializedObject(mineComponent), "_damage") : 0f;
                        dps += mineDamage / lay;
                        reach = Mathf.Max(reach, Float(so, "_range"));
                        targets = Mathf.Max(targets, AssumedSplashTargets);
                        parts.Add($"mines {mineDamage:0.#} every {lay:0.##}s");
                        break;
                    }

                    case "OrbitingDamageField":
                    {
                        float cooldown = Mathf.Max(0.05f, Float(so, "_hitCooldown"));
                        int count = Mathf.Max(1, Int(so, "_orbiterCount"));
                        dps += Float(so, "_contactDamage") / cooldown * OrbitDutyCycle;
                        reach = Mathf.Max(reach, Float(so, "_radius"));
                        targets = Mathf.Max(targets, count);
                        parts.Add($"{count} orbiters at {OrbitDutyCycle * 100f:0}% contact");
                        break;
                    }

                    case "GrayboxMeleeTower":
                    {
                        float damage = Float(so, "_damage");
                        dps += damage / interval;
                        reach = Mathf.Max(reach, towerRange);
                        parts.Add($"melee {damage:0.#} per {interval:0.##}s");
                        break;
                    }
                }
            }

            if (reach <= 0f)
            {
                reach = towerRange;
            }

            string note = parts.Count > 0 ? string.Join("; ", parts) : "no damage source found";
            if (dps <= 0f)
            {
                notes.Add($"{label}: no damage modelled ({note}).");
            }

            return new BalanceTower(label, data.BaseCost, reach, dps, targets, falloff, note);
        }

        private static string KnownKind(Type type)
        {
            for (Type t = type; t != null && t != typeof(TowerWeapon); t = t.BaseType)
            {
                if (KnownWeapons.Contains(t.Name))
                {
                    return t.Name;
                }
            }

            return null;
        }

        // ---------- enemies ----------

        private static BalanceEnemy EnemyFromPrefab(GameObject prefab, List<string> notes, HashSet<string> reportedSplitters)
        {
            var health = prefab.GetComponent<EnemyHealth>();
            if (health == null)
            {
                notes.Add($"Enemy '{prefab.name}' has no EnemyHealth and was skipped.");
                return null;
            }

            var shield = prefab.GetComponent<EnemyShield>();
            var mover = prefab.GetComponent<EnemyMover>();

            float hp = health.MaxHealth + (shield != null ? shield.MaxShield : 0f);
            int bounty = health.BountyValue;
            float speed = mover != null ? mover.Speed : 0f;

            if (mover == null)
            {
                notes.Add($"Enemy '{prefab.name}' has no EnemyMover; it is modelled as standing still and leaking at the timeout.");
            }

            var splitter = prefab.GetComponent<EnemySplitter>();
            if (splitter != null)
            {
                var so = new SerializedObject(splitter);
                var mini = ObjectField(so, "_miniAntPrefab") as GameObject;
                int count = Mathf.Max(1, Int(so, "_splitCount"));
                var miniHealth = mini != null ? mini.GetComponent<EnemyHealth>() : null;
                if (miniHealth != null)
                {
                    hp += count * miniHealth.MaxHealth;
                    bounty += count * miniHealth.BountyValue;
                }

                if (reportedSplitters.Add(prefab.name))
                {
                    notes.Add($"'{prefab.name}' splits into {count}; the children are folded into its health and bounty.");
                }
            }

            return new BalanceEnemy(prefab.name, 0f, hp, speed, bounty, health.BreachDamage);
        }

        // ---------- SerializedObject helpers ----------

        private static float Float(SerializedObject so, string field)
        {
            SerializedProperty p = so.FindProperty(field);
            return p != null && p.propertyType == SerializedPropertyType.Float ? p.floatValue : 0f;
        }

        private static int Int(SerializedObject so, string field)
        {
            SerializedProperty p = so.FindProperty(field);
            return p != null && p.propertyType == SerializedPropertyType.Integer ? p.intValue : 0;
        }

        private static bool Bool(SerializedObject so, string field)
        {
            SerializedProperty p = so.FindProperty(field);
            return p != null && p.propertyType == SerializedPropertyType.Boolean && p.boolValue;
        }

        private static UnityEngine.Object ObjectField(SerializedObject so, string field)
        {
            SerializedProperty p = so.FindProperty(field);
            return p != null && p.propertyType == SerializedPropertyType.ObjectReference ? p.objectReferenceValue : null;
        }
    }
}
