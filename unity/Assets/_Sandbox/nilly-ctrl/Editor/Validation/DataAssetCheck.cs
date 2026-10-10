using System.Collections.Generic;
using System.IO;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Towers;
using UnityEditor;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl.Editor
{
    /// <summary>
    /// Data assets with empty required fields: name, icon, prefab, cost, and stats at or below zero.
    /// Covers towers, bosses, resources, power-ups, species, Warden abilities, skill nodes,
    /// achievements and the enemies a wave asset spawns.
    /// </summary>
    public static class DataAssetCheck
    {
        private const string Name = "DataAsset";

        private static readonly string[] IconFolders = { "Icons/PNG", "Ui/Hud", "Ui/Markers" };

        public static void Run(string root, List<ValidationFinding> findings)
        {
            foreach (var (path, tower) in GrayboxProjectValidator.FindAll<GrayboxTowerData>(root)) CheckTower(tower, path, findings);
            foreach (var (path, boss) in GrayboxProjectValidator.FindAll<GrayboxBossData>(root)) CheckBoss(boss, path, findings);
            foreach (var (path, resource) in GrayboxProjectValidator.FindAll<GrayboxResourceData>(root)) CheckResource(resource, path, findings);
            foreach (var (path, powerUp) in GrayboxProjectValidator.FindAll<GrayboxPowerUpData>(root)) CheckPowerUp(powerUp, path, findings);
            foreach (var (path, species) in GrayboxProjectValidator.FindAll<GrayboxSpeciesData>(root)) CheckSpecies(species, path, findings);
            foreach (var (path, ability) in GrayboxProjectValidator.FindAll<GrayboxAbilityData>(root)) CheckAbility(ability, path, findings);
            foreach (var (path, node) in GrayboxProjectValidator.FindAll<GrayboxSkillNodeData>(root)) CheckSkillNode(node, path, findings);
            foreach (var (path, achievement) in GrayboxProjectValidator.FindAll<GrayboxAchievementData>(root)) CheckAchievement(achievement, path, findings);
            foreach (var (path, waves) in GrayboxProjectValidator.FindAll<MapWaveDataSO>(root)) CheckWaves(waves, path, findings);
        }

        private static void CheckTower(GrayboxTowerData data, string path, List<ValidationFinding> findings)
        {
            Empty(findings, data.DisplayName, "display name", path, ValidationSeverity.Error);
            CheckIcon(findings, data.IconName, path);

            if (data.BaseCost <= 0)
            {
                Add(findings, ValidationSeverity.Warning, "Cost is 0 or less: the tower would be free to build.", path);
            }

            if (data.Prefab == null)
            {
                Add(findings, ValidationSeverity.Error, "No tower prefab set.", path);
                return;
            }

            if (data.Prefab.Range <= 0f)
            {
                Add(findings, ValidationSeverity.Error, "Tower prefab range is 0 or less.", path);
            }

            if (data.Prefab.FireInterval <= 0f)
            {
                Add(findings, ValidationSeverity.Error, "Tower prefab fire interval is 0 or less.", path);
            }

            bool hasProjectile = new SerializedObject(data.Prefab).FindProperty("_projectilePrefab").objectReferenceValue != null;
            if (!hasProjectile && data.Prefab.GetComponent<TowerWeapon>() == null)
            {
                Add(findings, ValidationSeverity.Warning, "Tower prefab has neither a projectile nor a weapon component.", path);
            }
        }

        private static void CheckBoss(GrayboxBossData data, string path, List<ValidationFinding> findings)
        {
            Empty(findings, data.Id, "id", path, ValidationSeverity.Error);
            Empty(findings, data.DisplayName, "display name", path, ValidationSeverity.Error);
            Empty(findings, data.BannerText, "banner text", path, ValidationSeverity.Warning);
            CheckEnemyPrefab(findings, data.Prefab, "Boss prefab", path);
        }

        private static void CheckResource(GrayboxResourceData data, string path, List<ValidationFinding> findings)
        {
            Empty(findings, data.Id, "id", path, ValidationSeverity.Error);
            Empty(findings, data.DisplayName, "display name", path, ValidationSeverity.Error);
            CheckIcon(findings, data.IconName, path);
        }

        private static void CheckPowerUp(GrayboxPowerUpData data, string path, List<ValidationFinding> findings)
        {
            Empty(findings, data.Id, "id", path, ValidationSeverity.Error);
            Empty(findings, data.DisplayName, "display name", path, ValidationSeverity.Error);
            Empty(findings, data.Description, "description", path, ValidationSeverity.Info);

            if (data.Duration <= 0f)
            {
                Add(findings, ValidationSeverity.Error, "Duration is 0 or less.", path);
            }

            if (data.PickupPrefab == null)
            {
                Add(findings, ValidationSeverity.Error, "No pickup prefab set.", path);
            }

            if (data.EffectPrefab == null)
            {
                Add(findings, ValidationSeverity.Error, "No effect prefab set.", path);
            }
        }

        private static void CheckSpecies(GrayboxSpeciesData data, string path, List<ValidationFinding> findings)
        {
            Empty(findings, data.Id, "id", path, ValidationSeverity.Error);
            Empty(findings, data.DisplayName, "display name", path, ValidationSeverity.Error);

            if (data.TowerCatalog == null)
            {
                Add(findings, ValidationSeverity.Error, "No tower catalog set: the species could build nothing.", path);
            }
            else if (data.TowerCatalog.Count == 0)
            {
                Add(findings, ValidationSeverity.Warning, "Its tower catalog is empty.", path);
            }

            if (data.WardenPrefab == null)
            {
                Add(findings, ValidationSeverity.Info, "No Warden prefab: this species fields no Warden.", path);
            }
        }

        private static void CheckAbility(GrayboxAbilityData data, string path, List<ValidationFinding> findings)
        {
            Empty(findings, data.Id, "id", path, ValidationSeverity.Error);
            Empty(findings, data.DisplayName, "display name", path, ValidationSeverity.Error);
            Empty(findings, data.ControlId, "control id", path, ValidationSeverity.Error);

            if (data.Cooldown <= 0f)
            {
                Add(findings, ValidationSeverity.Info, "Cooldown is 0: the ability can be used every frame.", path);
            }
        }

        private static void CheckSkillNode(GrayboxSkillNodeData data, string path, List<ValidationFinding> findings)
        {
            if (string.IsNullOrEmpty(GrayboxProjectValidator.RawString(data, "_id")))
            {
                Add(findings, ValidationSeverity.Warning, $"Id is empty; the asset name '{data.name}' is used instead.", path);
            }

            if (string.IsNullOrEmpty(GrayboxProjectValidator.RawString(data, "_displayName")))
            {
                Add(findings, ValidationSeverity.Warning, "Display name is empty.", path);
            }

            Empty(findings, data.Description, "description", path, ValidationSeverity.Warning);
            Empty(findings, data.Branch, "branch", path, ValidationSeverity.Error);

            if (data.Cost <= 0)
            {
                Add(findings, ValidationSeverity.Warning, "Cost is 0 or less: the node is free.", path);
            }

            if (data.Effects == null || data.Effects.Count == 0)
            {
                Add(findings, ValidationSeverity.Error, "The node has no effects.", path);
            }
            else
            {
                for (int i = 0; i < data.Effects.Count; i++)
                {
                    SkillEffect effect = data.Effects[i];
                    if (effect.mode == SkillEffectMode.Multiply && effect.value <= 0f)
                    {
                        Add(findings, ValidationSeverity.Error,
                            $"Effect {i} ({effect.stat}) multiplies by {effect.value}, which zeroes or inverts the stat.", path);
                    }
                    else if (effect.mode == SkillEffectMode.Add && Mathf.Approximately(effect.value, 0f))
                    {
                        Add(findings, ValidationSeverity.Warning, $"Effect {i} ({effect.stat}) adds 0.", path);
                    }
                }
            }

            if (!IconExists(data.IconName))
            {
                Add(findings, ValidationSeverity.Warning, $"Icon '{data.IconName}' was not found in the icon folders.", path);
            }
        }

        private static void CheckAchievement(GrayboxAchievementData data, string path, List<ValidationFinding> findings)
        {
            if (string.IsNullOrEmpty(GrayboxProjectValidator.RawString(data, "_id")))
            {
                Add(findings, ValidationSeverity.Warning, $"Id is empty; the asset name '{data.name}' is used instead.", path);
            }

            if (string.IsNullOrEmpty(GrayboxProjectValidator.RawString(data, "_title")))
            {
                Add(findings, ValidationSeverity.Warning, "Title is empty.", path);
            }

            Empty(findings, data.Description, "description", path, ValidationSeverity.Warning);
            CheckIcon(findings, data.IconName, path);

            if (data.RequiredProgress <= 0)
            {
                Add(findings, ValidationSeverity.Error, "Required progress is 0 or less: it can never be earned properly.", path);
            }
        }

        private static void CheckWaves(MapWaveDataSO waves, string path, List<ValidationFinding> findings)
        {
            if (waves.waves == null || waves.waves.Count == 0)
            {
                Add(findings, ValidationSeverity.Error, "The wave asset has no waves.", path);
                return;
            }

            for (int w = 0; w < waves.waves.Count; w++)
            {
                Wave wave = waves.waves[w];
                if (wave == null || wave.enemies == null || wave.enemies.Count == 0)
                {
                    Add(findings, ValidationSeverity.Warning, $"Wave {w + 1} has no enemies.", path);
                    continue;
                }

                var checkedPrefabs = new HashSet<GameObject>();
                for (int e = 0; e < wave.enemies.Count; e++)
                {
                    EnemySpawnEntry entry = wave.enemies[e];
                    GameObject prefab = entry != null ? entry.enemyPrefab : null;
                    if (prefab == null)
                    {
                        Add(findings, ValidationSeverity.Error, $"Wave {w + 1}, entry {e + 1} has no enemy prefab.", path);
                    }
                    else if (checkedPrefabs.Add(prefab))
                    {
                        CheckEnemyPrefab(findings, prefab, $"Wave {w + 1} enemy '{prefab.name}'", path);
                    }
                }
            }
        }

        private static void CheckEnemyPrefab(List<ValidationFinding> findings, GameObject prefab, string label, string path)
        {
            if (prefab == null)
            {
                Add(findings, ValidationSeverity.Error, $"{label}: no prefab set.", path);
                return;
            }

            var health = prefab.GetComponent<EnemyHealth>();
            if (health == null)
            {
                Add(findings, ValidationSeverity.Error, $"{label} '{prefab.name}' has no EnemyHealth, so towers cannot hurt it.", path);
            }
            else if (health.MaxHealth <= 0f)
            {
                Add(findings, ValidationSeverity.Error, $"{label} '{prefab.name}' has max health 0 or less.", path);
            }

            var mover = prefab.GetComponent<EnemyMover>();
            if (mover == null)
            {
                Add(findings, ValidationSeverity.Warning, $"{label} '{prefab.name}' has no EnemyMover, so it will not walk the path.", path);
            }
            else if (mover.Speed <= 0f)
            {
                Add(findings, ValidationSeverity.Error, $"{label} '{prefab.name}' has speed 0 or less.", path);
            }
        }

        private static void CheckIcon(List<ValidationFinding> findings, string iconName, string path)
        {
            if (string.IsNullOrEmpty(iconName))
            {
                Add(findings, ValidationSeverity.Warning, "Icon name is empty.", path);
            }
            else if (!IconExists(iconName))
            {
                Add(findings, ValidationSeverity.Warning, $"Icon '{iconName}' was not found in the icon folders.", path);
            }
        }

        private static bool IconExists(string iconName)
        {
            if (string.IsNullOrEmpty(iconName))
            {
                return false;
            }

            foreach (string folder in IconFolders)
            {
                string assetPath = $"{GrayboxProjectValidator.SandboxRoot}/{folder}/{iconName}.png";
                if (File.Exists(ProjectTextIndex.AbsolutePath(assetPath)))
                {
                    return true;
                }
            }

            return false;
        }

        private static void Empty(List<ValidationFinding> findings, string value, string field, string path, ValidationSeverity severity)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                Add(findings, severity, $"The {field} is empty.", path);
            }
        }

        private static void Add(List<ValidationFinding> findings, ValidationSeverity severity, string message, string path)
        {
            GrayboxProjectValidator.Add(findings, severity, Name, message, path);
        }
    }
}
