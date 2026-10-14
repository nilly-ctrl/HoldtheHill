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
    internal static class GrayboxProjectilePrefabBuilder
    {
        internal const string LineMaterialPath = "Assets/_Game/Shared/Materials/GrayboxLine.mat";

        internal static GameObject BuildBulletPrefab(Sprite sprite)
        {
            var go = new GameObject("GrayboxBullet");
            GrayboxBuildKit.AddSprite(go, sprite, new Color(1f, 0.9f, 0.3f), 0.22f, sortingOrder: 3);
            GrayboxBuildKit.AnimateVisual(go, "ProjLinear", "Fly");

            Projectile p = go.AddComponent<Projectile>();
            GrayboxBuildKit.Apply(p, so =>
            {
                so.FindProperty("_flightMode").enumValueIndex = (int)FlightMode.Linear;
                so.FindProperty("_speed").floatValue = 9f;
                so.FindProperty("_damage").floatValue = 7f;
                so.FindProperty("_lifetime").floatValue = 3f;
                so.FindProperty("_hitRadius").floatValue = 0.28f;
            });

            return GrayboxBuildKit.SavePrefab(go, "Combat/GrayboxBullet");
        }

        internal static GameObject BuildHomingPrefab(Sprite sprite)
        {
            var go = new GameObject("GrayboxHomingBullet");
            GrayboxBuildKit.AddSprite(go, sprite, new Color(0.4f, 1f, 0.6f), 0.24f, sortingOrder: 3);
            GrayboxBuildKit.AnimateVisual(go, "ProjHoming", "Fly");

            Projectile p = go.AddComponent<Projectile>();
            GrayboxBuildKit.Apply(p, so =>
            {
                so.FindProperty("_flightMode").enumValueIndex = (int)FlightMode.Homing;
                so.FindProperty("_speed").floatValue = 5.5f;
                // Deliberately low, so the shot visibly overshoots and loops back around.
                so.FindProperty("_turnRate").floatValue = 110f;
                so.FindProperty("_damage").floatValue = 9f;
                so.FindProperty("_lifetime").floatValue = 7f;
                so.FindProperty("_hitRadius").floatValue = 0.3f;
            });

            return GrayboxBuildKit.SavePrefab(go, "Combat/GrayboxHomingBullet");
        }

        internal static GameObject BuildFragmentPrefab(Sprite sprite)
        {
            var go = new GameObject("GrayboxFragment");
            GrayboxBuildKit.AddSprite(go, sprite, new Color(1f, 0.6f, 0.2f), 0.16f, sortingOrder: 3);
            GrayboxBuildKit.AnimateVisual(go, "ProjFragment", "Fly");

            Projectile p = go.AddComponent<Projectile>();
            GrayboxBuildKit.Apply(p, so =>
            {
                so.FindProperty("_flightMode").enumValueIndex = (int)FlightMode.Linear;
                so.FindProperty("_speed").floatValue = 5f;
                so.FindProperty("_damage").floatValue = 3f;
                so.FindProperty("_lifetime").floatValue = 1.1f;
                so.FindProperty("_hitRadius").floatValue = 0.22f;
            });

            return GrayboxBuildKit.SavePrefab(go, "Combat/GrayboxFragment");
        }

        internal static GameObject BuildMortarPrefab(Sprite sprite, GameObject fragment, GameObject hazard)
        {
            var go = new GameObject("GrayboxMortar");
            GrayboxBuildKit.AddSprite(go, sprite, new Color(0.9f, 0.4f, 0.1f), 0.3f, sortingOrder: 3);
            GrayboxBuildKit.AnimateVisual(go, "ProjMortar", "Fly");

            ClusterProjectile p = go.AddComponent<ClusterProjectile>();
            GrayboxBuildKit.Apply(p, so =>
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

            return GrayboxBuildKit.SavePrefab(go, "Combat/GrayboxMortar");
        }

        internal static GameObject BuildRicochetPrefab(Sprite sprite)
        {
            var go = new GameObject("GrayboxRicochet");
            GrayboxBuildKit.AddSprite(go, sprite, new Color(0.6f, 0.8f, 1f), 0.24f, sortingOrder: 3);
            GrayboxBuildKit.AnimateVisual(go, "ProjRicochet", "Fly");

            RicochetProjectile p = go.AddComponent<RicochetProjectile>();
            GrayboxBuildKit.Apply(p, so =>
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

            return GrayboxBuildKit.SavePrefab(go, "Combat/GrayboxRicochet");
        }

        internal static GameObject BuildHazardPrefab(Sprite sprite)
        {
            var go = new GameObject("GrayboxHazard");
            GrayboxBuildKit.AddSprite(go, sprite, new Color(1f, 0.45f, 0.1f, 0.45f), 2.2f, sortingOrder: 0);
            // The puddle art's rim is 29 px from its centre; match the hazard's 1.1 radius.
            GrayboxBuildKit.AnimateVisual(go, "FxHazard", "Burn", visualScale: 1.1f * 32f / 29f);

            go.AddComponent<CircleCollider2D>();
            GroundHazard hazard = go.AddComponent<GroundHazard>();
            GrayboxBuildKit.Apply(hazard, so =>
            {
                so.FindProperty("_radius").floatValue = 1.1f;
                so.FindProperty("_damagePerTick").floatValue = 2f;
                so.FindProperty("_tickInterval").floatValue = 0.5f;
                so.FindProperty("_duration").floatValue = 4f;
            });

            return GrayboxBuildKit.SavePrefab(go, "Combat/GrayboxHazard");
        }

        internal static GameObject BuildMinePrefab(Sprite sprite)
        {
            var go = new GameObject("GrayboxMine");
            GrayboxBuildKit.AddSprite(go, sprite, new Color(1f, 0.9f, 0.2f), 0.35f, sortingOrder: 2);
            if (GrayboxBuildKit.Anim != null)
            {
                GrayboxMineAnimator.Attach(go, GrayboxBuildKit.Anim);
            }

            go.AddComponent<CircleCollider2D>();
            ProximityMine mine = go.AddComponent<ProximityMine>();
            GrayboxBuildKit.Apply(mine, so =>
            {
                so.FindProperty("_triggerRadius").floatValue = 0.7f;
                so.FindProperty("_splashRadius").floatValue = 1.6f;
                so.FindProperty("_damage").floatValue = 35f;
                so.FindProperty("_armDelay").floatValue = 0.4f;
            });

            return GrayboxBuildKit.SavePrefab(go, "Combat/GrayboxMine");
        }

        internal static void AddChainLightning(GameObject tower, Material lineMaterial)
        {
            ChainLightning chain = tower.AddComponent<ChainLightning>();
            ConfigureLine(tower.GetComponent<LineRenderer>(), lineMaterial, new Color(0.6f, 0.9f, 1f), 0.08f);
            GrayboxLineScroll.Dress(tower.GetComponent<LineRenderer>(), LineMaterial("GrayboxBolt", "LineBolt"), 0.25f, -6f, 1f);

            GrayboxBuildKit.Apply(chain, so =>
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
            GrayboxLineScroll.Dress(tower.GetComponent<LineRenderer>(), LineMaterial("GrayboxBeam", "LineBeam"), 0.25f, -3f, 0f);

            GrayboxBuildKit.Apply(beam, so =>
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
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>($"{GrayboxSceneDressingBuilder.TileRoot}/{textureName}.png");
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

            var material = new Material(shader) { name = "GrayboxLine" };
            AssetDatabase.CreateAsset(material, LineMaterialPath);
            return material;
        }
    }
}
