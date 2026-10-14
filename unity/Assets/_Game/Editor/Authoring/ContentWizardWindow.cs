using HoldTheHill.EditorTools.Builders;
using HoldTheHill.Core.Icons;
using HoldTheHill.Features.PowerUps;
using HoldTheHill.Features.Economy;
using System;
using System.Collections.Generic;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Towers;
using UnityEditor;
using UnityEngine;


namespace HoldTheHill.EditorTools.Authoring
{
    /// <summary>
    /// New tower / enemy / pickup / power-up: fills in a form, then makes the data asset and the
    /// prefab variant the same way the graybox builders do (only when missing).
    /// </summary>
    internal sealed class ContentWizardWindow : EditorWindow
    {
        private static readonly string[] Tabs = { "Tower", "Enemy / Boss", "Pickup", "Power-up" };

        private int _tab;
        private Vector2 _scroll;
        private string _result;
        private MessageType _resultType = MessageType.Info;

        private readonly ContentWizardBuilder.TowerSpec _tower = new ContentWizardBuilder.TowerSpec
        {
            Color = new Color(0.4f, 0.7f, 1f),
        };

        private readonly ContentWizardBuilder.EnemySpec _enemy = new ContentWizardBuilder.EnemySpec();
        private readonly ContentWizardBuilder.PickupSpec _pickup = new ContentWizardBuilder.PickupSpec();
        private readonly ContentWizardBuilder.PowerUpSpec _powerUp = new ContentWizardBuilder.PowerUpSpec();

        private List<Type> _effectTypes;
        private string[] _effectNames;
        private int _effectIndex;

        [MenuItem("Tools/Hold the Hill/Authoring/Content Wizard")]
        public static void Open()
        {
            var window = GetWindow<ContentWizardWindow>("Content Wizard");
            window.minSize = new Vector2(380f, 460f);
            window.Show();
        }

        private void OnEnable()
        {
            _effectTypes = ContentWizardPickupBuilder.EffectTypes();
            _effectNames = _effectTypes.ConvertAll(t => t.Name).ToArray();
            if (_pickup.Resource == null)
            {
                _pickup.Resource = AssetDatabase.LoadAssetAtPath<ResourceData>(PickupBuilder.FoodPath);
            }
        }

        private void OnGUI()
        {
            int tab = GUILayout.Toolbar(_tab, Tabs);
            if (tab != _tab)
            {
                _tab = tab;
                _result = null;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.Space();
            switch (_tab)
            {
                case 0: TowerForm(); break;
                case 1: EnemyForm(); break;
                case 2: PickupForm(); break;
                default: PowerUpForm(); break;
            }

            if (!string.IsNullOrEmpty(_result))
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(_result, _resultType);
            }

            EditorGUILayout.EndScrollView();
        }

        // ---------- tower ----------

        private void TowerForm()
        {
            EditorGUILayout.HelpBox(
                "Makes a variant of a tower prefab, a HoldTheHill.Features.Towers.TowerData asset and (optionally) a catalog entry. " +
                "Nothing existing is overwritten. 'Rebuild Graybox Tower Prefabs' resets the catalog list to the built-in towers.",
                MessageType.None);

            _tower.Id = EditorGUILayout.TextField("Id (prefab name)", _tower.Id);
            _tower.DisplayName = EditorGUILayout.TextField("Display name", _tower.DisplayName);
            _tower.Archetype = (TowerArchetype)EditorGUILayout.EnumPopup("Archetype", _tower.Archetype);
            _tower.Cost = Mathf.Max(0, EditorGUILayout.IntField("Base cost", _tower.Cost));
            _tower.IconName = EditorGUILayout.TextField(new GUIContent("Icon name", "A IconLibrary name. Empty uses <Id>Icon."), _tower.IconName);
            _tower.Color = EditorGUILayout.ColorField("Colour", _tower.Color);
            EditorGUILayout.Space();
            _tower.BasePrefab = (GameObject)EditorGUILayout.ObjectField(
                new GUIContent("Variant of", "Empty means TowerBase. Pick an existing tower to inherit its weapon."),
                _tower.BasePrefab, typeof(GameObject), false);
            _tower.Range = Mathf.Max(0f, EditorGUILayout.FloatField("Range", _tower.Range));
            _tower.FireInterval = Mathf.Max(0.05f, EditorGUILayout.FloatField("Fire interval (s)", _tower.FireInterval));
            _tower.Priority = (TargetingPriority)EditorGUILayout.EnumPopup("Targeting", _tower.Priority);
            _tower.Projectile = (Projectile)EditorGUILayout.ObjectField("Projectile", _tower.Projectile, typeof(Projectile), false);
            _tower.AddToCatalog = EditorGUILayout.Toggle("Add to tower catalog", _tower.AddToCatalog);

            string error = ContentWizardBuilder.CheckId(_tower.Id);
            List<string> taken = error == null
                ? ContentWizardBuilder.Collisions(
                    TowerBuilder.PrefabPath(_tower.Id), ContentWizardBuilder.TowerDataPath(_tower.Id))
                : new List<string>();
            if (_tower.BasePrefab != null && _tower.BasePrefab.GetComponent<Tower>() == null)
            {
                error = "The 'Variant of' prefab has no Tower component.";
            }

            CreateButton(error, taken, () => ContentWizardUnitBuilder.CreateTower(_tower));
        }

        // ---------- enemy ----------

        private void EnemyForm()
        {
            EditorGUILayout.HelpBox(
                "Makes a variant of EnemyBase (or BossBase for a boss). Special behaviour " +
                "(shield, healer, splitter, ...) is added by hand afterwards. Waves still have to be told to spawn it.",
                MessageType.None);

            _enemy.Id = EditorGUILayout.TextField("Id (prefab Graybox<Id>)", _enemy.Id);
            _enemy.Boss = EditorGUILayout.Toggle("Boss", _enemy.Boss);
            if (_enemy.Boss)
            {
                _enemy.DisplayName = EditorGUILayout.TextField("Display name", _enemy.DisplayName);
                _enemy.BannerText = EditorGUILayout.TextField("Banner text", _enemy.BannerText);
            }

            _enemy.Health = Mathf.Max(1f, EditorGUILayout.FloatField("Max health", _enemy.Health));
            _enemy.Speed = Mathf.Max(0.01f, EditorGUILayout.FloatField("Speed", _enemy.Speed));
            _enemy.Size = Mathf.Max(0.1f, EditorGUILayout.FloatField("Size", _enemy.Size));
            _enemy.Bounty = Mathf.Max(0, EditorGUILayout.IntField("Bounty", _enemy.Bounty));
            _enemy.Color = EditorGUILayout.ColorField("Colour", _enemy.Color);
            _enemy.FaceTravelDirection = EditorGUILayout.Toggle("Face travel direction", _enemy.FaceTravelDirection);

            string error = ContentWizardBuilder.CheckId(_enemy.Id);
            List<string> taken = error == null
                ? ContentWizardBuilder.Collisions(
                    ContentWizardBuilder.EnemyPrefabPath(_enemy.Id),
                    _enemy.Boss ? ContentWizardBuilder.BossDataPath(_enemy.Id) : string.Empty)
                : new List<string>();
            taken.Remove(string.Empty);

            CreateButton(error, taken, () => ContentWizardUnitBuilder.CreateEnemy(_enemy));
        }

        // ---------- pickup ----------

        private void PickupForm()
        {
            EditorGUILayout.HelpBox(
                "Makes a variant of PickupBase that pays a resource when the Warden walks over it.",
                MessageType.None);

            _pickup.Id = EditorGUILayout.TextField("Id (prefab name)", _pickup.Id);
            _pickup.Resource = (ResourceData)EditorGUILayout.ObjectField(
                "Resource", _pickup.Resource, typeof(ResourceData), false);
            _pickup.Amount = Mathf.Max(1, EditorGUILayout.IntField("Amount", _pickup.Amount));
            _pickup.Size = Mathf.Max(0.1f, EditorGUILayout.FloatField("Size", _pickup.Size));
            _pickup.Lifetime = Mathf.Max(0f, EditorGUILayout.FloatField("Lifetime (s)", _pickup.Lifetime));
            _pickup.Color = EditorGUILayout.ColorField("Colour", _pickup.Color);

            string error = ContentWizardBuilder.CheckId(_pickup.Id);
            if (error == null && _pickup.Resource == null)
            {
                error = "Pick a resource.";
            }

            List<string> taken = ContentWizardBuilder.CheckId(_pickup.Id) == null
                ? ContentWizardBuilder.Collisions(ContentWizardBuilder.PickupPrefabPath(_pickup.Id))
                : new List<string>();
            CreateButton(error, taken, () => ContentWizardPickupBuilder.CreatePickup(_pickup));
        }

        // ---------- power-up ----------

        private void PowerUpForm()
        {
            EditorGUILayout.HelpBox(
                "Makes a PowerUpData, an effect prefab running the effect component you pick, and a pickup " +
                "that is a variant of PickupBase. A new effect needs its own PowerUpEffect subclass first.",
                MessageType.None);

            _powerUp.Id = EditorGUILayout.TextField("Id", _powerUp.Id);
            _powerUp.DisplayName = EditorGUILayout.TextField("Display name", _powerUp.DisplayName);
            _powerUp.Description = EditorGUILayout.TextField("Description", _powerUp.Description);
            _powerUp.Color = EditorGUILayout.ColorField("Colour", _powerUp.Color);
            _powerUp.Duration = Mathf.Max(0.1f, EditorGUILayout.FloatField("Duration (s)", _powerUp.Duration));

            if (_effectNames.Length == 0)
            {
                EditorGUILayout.HelpBox("No concrete PowerUpEffect classes were found.", MessageType.Warning);
            }
            else
            {
                _effectIndex = Mathf.Clamp(EditorGUILayout.Popup("Effect", _effectIndex, _effectNames), 0, _effectNames.Length - 1);
                _powerUp.EffectType = _effectTypes[_effectIndex];
            }

            _powerUp.AddToSceneSpawner = EditorGUILayout.Toggle(
                new GUIContent("Add to open scene's spawner", "Adds it to the PowerUpSpawner in the open scene. Save the scene afterwards."),
                _powerUp.AddToSceneSpawner);

            string error = ContentWizardBuilder.CheckId(_powerUp.Id);
            if (error == null && _powerUp.EffectType == null)
            {
                error = "Pick an effect.";
            }

            List<string> taken = ContentWizardBuilder.CheckId(_powerUp.Id) == null
                ? ContentWizardBuilder.Collisions(
                    PowerUpBuilder.DataPath(_powerUp.Id),
                    ContentWizardBuilder.PowerUpEffectPath(_powerUp.Id),
                    ContentWizardBuilder.PowerUpPickupPath(_powerUp.Id))
                : new List<string>();
            CreateButton(error, taken, () => ContentWizardPickupBuilder.CreatePowerUp(_powerUp));
        }

        // ---------- shared ----------

        private void CreateButton(string error, List<string> taken, Func<List<string>> create)
        {
            EditorGUILayout.Space();
            if (error != null)
            {
                EditorGUILayout.HelpBox(error, MessageType.Warning);
            }

            foreach (string path in taken)
            {
                EditorGUILayout.HelpBox($"Already exists and would be left untouched: {path}", MessageType.Warning);
            }

            using (new EditorGUI.DisabledScope(error != null || taken.Count > 0 || EditorApplication.isPlayingOrWillChangePlaymode))
            {
                if (GUILayout.Button("Create", GUILayout.Height(28f)))
                {
                    try
                    {
                        List<string> made = create();
                        _result = "Made:\n" + string.Join("\n", made);
                        _resultType = MessageType.Info;
                        Debug.Log("[ContentWizard] " + _result);
                    }
                    catch (Exception e)
                    {
                        _result = e.Message;
                        _resultType = MessageType.Error;
                        Debug.LogError("[ContentWizard] " + e.Message);
                    }

                    GUIUtility.ExitGUI();
                }
            }
        }
    }
}
