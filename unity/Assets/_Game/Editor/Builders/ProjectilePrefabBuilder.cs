using HoldTheHill.Core.Animation;
using System.Collections.Generic;
using System.Linq;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Towers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HoldTheHill.EditorTools.Builders
{
    /// <summary>
    /// Projectile, hazard and mine prefabs, and the line-renderer weapons (chain lightning, beam).
    /// </summary>
    internal static class ProjectilePrefabBuilder
    {
        internal const string LineMaterialPath = "Assets/_Game/Shared/Materials/Line.mat";

        internal static GameObject BuildBulletPrefab(Sprite sprite)
        {
            var go = new GameObject("Bullet");
            BuildKit.AddSprite(go, sprite, new Color(1f, 0.9f, 0.3f), 0.22f, sortingOrder: 3);
            BuildKit.AnimateVisual(go, "ProjLinear", "Fly");

            Projectile p = go.AddComponent<Projectile>();
            BuildKit.Apply(p, so =>
            {
                so.FindProperty("_flightMode").enumValueIndex = (int)FlightMode.Linear;
                so.FindProperty("_speed").floatValue = 9f;
                so.FindProperty("_damage").floatValue = 7f;
                so.FindProperty("_lifetime").floatValue = 3f;
                so.FindProperty("_hitRadius").floatValue = 0.28f;
            });

            return BuildKit.SavePrefab(go, "Combat/Bullet");
        }

        internal static GameObject BuildHomingPrefab(Sprite sprite)
        {
            var go = new GameObject("HomingBullet");
            BuildKit.AddSprite(go, sprite, new Color(0.4f, 1f, 0.6f), 0.24f, sortingOrder: 3);
            BuildKit.AnimateVisual(go, "ProjHoming", "Fly");

            Projectile p = go.AddComponent<Projectile>();
            BuildKit.Apply(p, so =>
            {
                so.FindProperty("_flightMode").enumValueIndex = (int)FlightMode.Homing;
                so.FindProperty("_speed").floatValue = 5.5f;
                // Deliberately low, so the shot visibly overshoots and loops back around.
                so.FindProperty("_turnRate").floatValue = 110f;
                so.FindProperty("_damage").floatValue = 9f;
                so.FindProperty("_lifetime").floatValue = 7f;
                so.FindProperty("_hitRadius").floatValue = 0.3f;
            });

            return BuildKit.SavePrefab(go, "Combat/HomingBullet");
        }

        internal static GameObject BuildFragmentPrefab(Sprite sprite)
        {
            var go = new GameObject("Fragment");
            BuildKit.AddSprite(go, sprite, new Color(1f, 0.6f, 0.2f), 0.16f, sortingOrder: 3);
            BuildKit.AnimateVisual(go, "ProjFragment", "Fly");

            Projectile p = go.AddComponent<Projectile>();
            BuildKit.Apply(p, so =>
            {
                so.FindProperty("_flightMode").enumValueIndex = (int)FlightMode.Linear;
                so.FindProperty("_speed").floatValue = 5f;
                so.FindProperty("_damage").floatValue = 3f;
                so.FindProperty("_lifetime").floatValue = 1.1f;
                so.FindProperty("_hitRadius").floatValue = 0.22f;
            });

            return BuildKit.SavePrefab(go, "Combat/Fragment");
        }

        internal static GameObject BuildMortarPrefab(Sprite sprite, GameObject fragment, GameObject hazard)
        {
            var go = new GameObject("Mortar");
            BuildKit.AddSprite(go, sprite, new Color(0.9f, 0.4f, 0.1f), 0.3f, sortingOrder: 3);
            BuildKit.AnimateVisual(go, "ProjMortar", "Fly");

            ClusterProjectile p = go.AddComponent<ClusterProjectile>();
            BuildKit.Apply(p, so =>
            {
                so.FindProperty("_flightMode").enumValueIndex = (int)FlightMode.Lobbed;
                so.FindProperty("_speed").floatValue = 4.5f;
                so.FindProperty("_arcHeight").floatValue = 2.5f;
                so.FindProperty("_damage").floatValue = 6f;
                so.FindProperty("_lifetime").floatValue = 6f;
                so.FindProperty("_splashRadius").floatValue = 1.2f;
                so.FindProperty("_splashDamage").floatValue = 5f;
                so.FindProperty("_hitRadius").floatValue = 0.3f;
                so.FindProperty("_fragmentCount").intValue = 8;
                so.FindProperty("_spreadAngle").floatValue = 360f;
                so.FindProperty("_splitOn").enumValueIndex = (int)ClusterProjectile.SplitTrigger.OnImpact;
                so.FindProperty("_fragmentPrefab").objectReferenceValue = fragment.GetComponent<Projectile>();
                so.FindProperty("_groundHazardPrefab").objectReferenceValue = hazard.GetComponent<GroundHazard>();
                so.FindProperty("_groundHazardRadius").floatValue = 1.1f;
                so.FindProperty("_groundHazardDuration").floatValue = 4f;
                so.FindProperty("_groundHazardDamagePerTick").floatValue = 2f;
            });

            return BuildKit.SavePrefab(go, "Combat/Mortar");
        }

        internal static GameObject BuildRicochetPrefab(Sprite sprite)
        {
            var go = new GameObject("Ricochet");
            BuildKit.AddSprite(go, sprite, new Color(0.6f, 0.8f, 1f), 0.24f, sortingOrder: 3);
            BuildKit.AnimateVisual(go, "ProjRicochet", "Fly");

            RicochetProjectile p = go.AddComponent<RicochetProjectile>();
            BuildKit.Apply(p, so =>
            {
                so.FindProperty("_flightMode").enumValueIndex = (int)FlightMode.Linear;
                so.FindProperty("_speed").floatValue = 10f;
                so.FindProperty("_damage").floatValue = 8f;
                so.FindProperty("_lifetime").floatValue = 4f;
                so.FindProperty("_hitRadius").floatValue = 0.28f;
                so.FindProperty("_maxBounces").intValue = 3;
                so.FindProperty("_bounceRange").floatValue = 3.5f;
                so.FindProperty("_damageFalloff").floatValue = 0.75f;
            });

            return BuildKit.SavePrefab(go, "Combat/Ricochet");
        }

        internal static GameObject BuildHazardPrefab(Sprite sprite)
        {
            var go = new GameObject("Hazard");
            BuildKit.AddSprite(go, sprite, new Color(1f, 0.45f, 0.1f, 0.45f), 2.2f, sortingOrder: 0);
            // The puddle art's rim is 29 px from its centre; match the hazard's 1.1 radius.
            BuildKit.AnimateVisual(go, "FxHazard", "Burn", visualScale: 1.1f * 32f / 29f);

            go.AddComponent<CircleCollider2D>();
            GroundHazard hazard = go.AddComponent<GroundHazard>();
            BuildKit.Apply(hazard, so =>
            {
                so.FindProperty("_radius").floatValue = 1.1f;
                so.FindProperty("_damagePerTick").floatValue = 2f;
                so.FindProperty("_tickInterval").floatValue = 0.5f;
                so.FindProperty("_duration").floatValue = 4f;
            });

            return BuildKit.SavePrefab(go, "Combat/Hazard");
        }

        internal static GameObject BuildMinePrefab(Sprite sprite)
        {
            var go = new GameObject("Mine");
            BuildKit.AddSprite(go, sprite, new Color(1f, 0.9f, 0.2f), 0.35f, sortingOrder: 2);
            if (BuildKit.Anim != null)
            {
                MineAnimator.Attach(go, BuildKit.Anim);
            }

            go.AddComponent<CircleCollider2D>();
            ProximityMine mine = go.AddComponent<ProximityMine>();
            BuildKit.Apply(mine, so =>
            {
                so.FindProperty("_triggerRadius").floatValue = 0.7f;
                so.FindProperty("_splashRadius").floatValue = 1.6f;
                so.FindProperty("_damage").floatValue = 35f;
                so.FindProperty("_armDelay").floatValue = 0.4f;
            });

            return BuildKit.SavePrefab(go, "Combat/Mine");
        }

        internal static void AddChainLightning(GameObject tower, Material lineMaterial)
        {
            ChainLightning chain = tower.AddComponent<ChainLightning>();
            ConfigureLine(tower.GetComponent<LineRenderer>(), lineMaterial, new Color(0.6f, 0.9f, 1f), 0.08f);
            LineScroll.Dress(tower.GetComponent<LineRenderer>(), LineMaterial("Bolt", "LineBolt"), 0.25f, -6f, 1f);

            BuildKit.Apply(chain, so =>
            {
                so.FindProperty("_maxTargets").intValue = 4;
                so.FindProperty("_jumpRange").floatValue = 3.5f;
                so.FindProperty("_damage").floatValue = 10f;
            });
        }

        internal static void AddBeam(GameObject tower, Material lineMaterial)
        {
            ContinuousBeam beam = tower.AddComponent<ContinuousBeam>();
            ConfigureLine(tower.GetComponent<LineRenderer>(), lineMaterial, new Color(1f, 0.55f, 0.25f), 0.15f);
            LineScroll.Dress(tower.GetComponent<LineRenderer>(), LineMaterial("Beam", "LineBeam"), 0.25f, -3f, 0f);

            BuildKit.Apply(beam, so =>
            {
                so.FindProperty("_range").floatValue = 4f;
                so.FindProperty("_baseDamagePerSecond").floatValue = 5f;
                so.FindProperty("_rampPerSecond").floatValue = 5f;
                so.FindProperty("_maxDamagePerSecond").floatValue = 25f;
            });
        }

        /// <summary>
        /// A material that tiles one of the Tiles/Line*.png strips along a LineRenderer. Returns
        /// null if the texture is missing, and the line then keeps its flat colour.
        /// </summary>
        internal static Material LineMaterial(string materialName, string textureName)
        {
            string path = $"Assets/_Game/Shared/Materials/{materialName}.mat";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{SceneDressingBuilder.TileRoot}/{textureName}.png");
            if (texture == null)
            {
                return null;
            }

            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");
                material = new Material(shader) { name = materialName };
                AssetDatabase.CreateAsset(material, path);
            }

            material.mainTexture = texture;
            EditorUtility.SetDirty(material);
            return material;
        }

        internal static void ConfigureLine(LineRenderer line, Material material, Color color, float width)
        {
            if (line == null)
            {
                return;
            }

            line.sharedMaterial = material;
            line.startColor = color;
            line.endColor = color;
            line.startWidth = width;
            line.endWidth = width;
            line.numCapVertices = 2;
            line.sortingOrder = 4;
        }

        internal static Material CreateLineMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(LineMaterialPath);
            if (existing != null)
            {
                return existing;
            }

            Shader shader = Shader.Find("Sprites/Default")
                            ?? Shader.Find("Universal Render Pipeline/Unlit")
                            ?? Shader.Find("Unlit/Color");

            var material = new Material(shader) { name = "Line" };
            AssetDatabase.CreateAsset(material, LineMaterialPath);
            return material;
        }
    }
}
