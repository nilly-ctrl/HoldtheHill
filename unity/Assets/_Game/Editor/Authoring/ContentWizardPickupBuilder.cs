using HoldTheHill.EditorTools.Builders;
using HoldTheHill.Features.PowerUps;
using HoldTheHill.Features.Economy;
using System;
using System.Collections.Generic;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Towers;
using UnityEditor;
using UnityEngine;

using TowerSpec = HoldTheHill.EditorTools.Authoring.ContentWizardBuilder.TowerSpec;
using EnemySpec = HoldTheHill.EditorTools.Authoring.ContentWizardBuilder.EnemySpec;
using PickupSpec = HoldTheHill.EditorTools.Authoring.ContentWizardBuilder.PickupSpec;
using PowerUpSpec = HoldTheHill.EditorTools.Authoring.ContentWizardBuilder.PowerUpSpec;

namespace HoldTheHill.EditorTools.Authoring
{
    /// <summary>
    /// The content wizard's pickups and power-ups: prefab variants and effect prefabs, made the way <see cref="ContentWizardBuilder"/> describes.
    /// </summary>
    internal static class ContentWizardPickupBuilder
    {
        public static List<string> CreatePickup(PickupSpec spec)
        {
            string path = ContentWizardBuilder.PickupPrefabPath(spec.Id);
            ContentWizardBuilder.Refuse(path);
            if (spec.Resource == null)
            {
                throw new InvalidOperationException("Pick the resource this pickup pays.");
            }

            var pickupBase = AssetDatabase.LoadAssetAtPath<GameObject>(GrayboxPickupBuilder.PrefabPath("PickupBase"));
            if (pickupBase == null)
            {
                throw new InvalidOperationException("PickupBase.prefab is missing. Run Build Graybox Combat Test first.");
            }

            GrayboxBuildKit.EnsureFolder(ContentWizardBuilder.GrayboxRoot + "/Economy/Prefabs");

            var go = (GameObject)PrefabUtility.InstantiatePrefab(pickupBase);
            go.name = spec.Id;

            var renderer = go.GetComponent<SpriteRenderer>();
            renderer.color = spec.Color;
            renderer.size = new Vector2(spec.Size, spec.Size);

            GrayboxBuildKit.Apply(go.GetComponent<GrayboxPickup>(), so =>
            {
                so.FindProperty("_reach").floatValue = Mathf.Max(0.35f, spec.Size);
                so.FindProperty("_lifetime").floatValue = spec.Lifetime;
            });

            GrayboxBuildKit.Apply(go.AddComponent<GrayboxResourceGain>(), so =>
            {
                so.FindProperty("_resource").objectReferenceValue = spec.Resource;
                so.FindProperty("_amount").intValue = spec.Amount;
            });

            GameObject prefab = GrayboxBuildKit.SavePrefab(go, $"Pickups/{spec.Id}");
            ContentWizardBuilder.Finish(prefab);
            return new List<string> { path };
        }

        /// <summary>The concrete effect components a power-up can run, for the wizard's picker.</summary>
        public static List<Type> EffectTypes()
        {
            var found = new List<Type>();
            foreach (Type type in TypeCache.GetTypesDerivedFrom<GrayboxPowerUpEffect>())
            {
                if (!type.IsAbstract && !type.IsGenericType)
                {
                    found.Add(type);
                }
            }

            found.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            return found;
        }

        public static List<string> CreatePowerUp(PowerUpSpec spec)
        {
            string dataPath = GrayboxPowerUpBuilder.DataPath(spec.Id);
            string effectPath = ContentWizardBuilder.PowerUpEffectPath(spec.Id);
            string pickupPath = ContentWizardBuilder.PowerUpPickupPath(spec.Id);
            ContentWizardBuilder.Refuse(dataPath, effectPath, pickupPath);
            if (spec.EffectType == null)
            {
                throw new InvalidOperationException("Pick the effect component the power-up runs.");
            }

            var pickupBase = AssetDatabase.LoadAssetAtPath<GameObject>(GrayboxPickupBuilder.PrefabPath("PickupBase"));
            if (pickupBase == null)
            {
                throw new InvalidOperationException("PickupBase.prefab is missing. Run Build Graybox Combat Test first.");
            }

            GrayboxBuildKit.EnsureFolder(ContentWizardBuilder.GrayboxRoot + "/PowerUps/Prefabs");
            GrayboxBuildKit.EnsureFolder(ContentWizardBuilder.DataRoot + "/PowerUps/Data");

            var data = ScriptableObject.CreateInstance<GrayboxPowerUpData>();
            AssetDatabase.CreateAsset(data, dataPath);
            GrayboxBuildKit.Apply(data, so =>
            {
                so.FindProperty("_id").stringValue = spec.Id;
                so.FindProperty("_displayName").stringValue = string.IsNullOrEmpty(spec.DisplayName) ? spec.Id : spec.DisplayName;
                so.FindProperty("_description").stringValue = spec.Description ?? string.Empty;
                so.FindProperty("_color").colorValue = spec.Color;
                so.FindProperty("_duration").floatValue = spec.Duration;
            });

            var effectObject = new GameObject("Effect" + spec.Id);
            effectObject.AddComponent(spec.EffectType);
            GameObject effect = GrayboxBuildKit.SavePrefab(effectObject, $"PowerUps/Effect{spec.Id}");

            var pickupObject = (GameObject)PrefabUtility.InstantiatePrefab(pickupBase);
            pickupObject.name = spec.Id;
            var renderer = pickupObject.GetComponent<SpriteRenderer>();
            renderer.color = spec.Color;
            renderer.size = new Vector2(0.5f, 0.5f);
            GrayboxBuildKit.Apply(pickupObject.GetComponent<GrayboxPickup>(), so =>
            {
                so.FindProperty("_reach").floatValue = 0.5f;
                so.FindProperty("_lifetime").floatValue = 25f;
            });
            GrayboxBuildKit.Apply(pickupObject.AddComponent<GrayboxPowerUpGain>(),
                so => so.FindProperty("_powerUp").objectReferenceValue = data);
            GameObject pickup = GrayboxBuildKit.SavePrefab(pickupObject, $"PowerUps/{spec.Id}");

            GrayboxBuildKit.Apply(data, so =>
            {
                so.FindProperty("_pickupPrefab").objectReferenceValue = pickup;
                so.FindProperty("_effectPrefab").objectReferenceValue = effect.GetComponent<GrayboxPowerUpEffect>();
            });
            EditorUtility.SetDirty(data);

            var made = new List<string> { dataPath, effectPath, pickupPath };
            if (spec.AddToSceneSpawner)
            {
                var spawner = UnityEngine.Object.FindAnyObjectByType<GrayboxPowerUpSpawner>();
                if (spawner != null)
                {
                    var so = new SerializedObject(spawner);
                    SerializedProperty list = so.FindProperty("_powerUps");
                    int at = list.arraySize;
                    list.arraySize = at + 1;
                    list.GetArrayElementAtIndex(at).objectReferenceValue = data;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(spawner);
                    made.Add($"{spawner.name} in the open scene (power-up added; save the scene)");
                }
            }

            ContentWizardBuilder.Finish(data);
            return made;
        }
    }
}
