using System;
using System.Collections.Generic;
using HoldTheHill.Features.Towers;
using HoldTheHill.Sandbox.UiKit;
using TMPro;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The card for the selected tower: its targeting priority, level, combat record, and the
    /// Upgrade, Dismantle and Close buttons. Shows only while the placer has a tower selected.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Graybox/HUD/Tower Card")]
    public class GrayboxTowerCard : GrayboxHudPanel
    {
        [SerializeField] private TMP_Text _title;
        [SerializeField] private TMP_Text _role;
        [SerializeField] private UiStepper _priority;
        [SerializeField] private TMP_Text _levelLine;
        [SerializeField] private TMP_Text _damage;
        [SerializeField] private TMP_Text _record;
        [SerializeField] private UiButton _upgrade;
        [SerializeField] private UiButton _sell;
        [SerializeField] private UiButton _close;

        private readonly TargetingPriority[] _priorities = (TargetingPriority[])Enum.GetValues(typeof(TargetingPriority));
        private GrayboxTowerPlacer _placer;

        // What the texts were last built from, so a frame with nothing new allocates nothing.
        private Tower _shownTower;
        private int _shownLevel = -1;
        private int _shownDamage = -1;
        private int _shownKills = -1;
        private int _shownShots = -1;
        private int _shownUpgradeCost = -1;
        private int _shownRefund = -1;
        private float _shownRange = -1f;
        private TargetingPriority _shownPriority;

        public override bool WantsToShow => FindPlacer() != null && _placer.SelectedTower != null;

        private void Awake()
        {
            var names = new List<string>();
            foreach (TargetingPriority priority in _priorities)
            {
                names.Add(priority.ToString());
            }

            _priority.Label = "Target";
            _priority.SetOptions(names, 0);
            _priority.IndexChanged.AddListener(index =>
            {
                if (_placer != null && _placer.SelectedTower != null)
                {
                    _placer.SelectedTower.Priority = _priorities[index];
                }

                ReleaseFocus();
            });

            OnClick(_upgrade.Button, () => _placer.TryUpgradeSelected());
            OnClick(_sell.Button, () => _placer.SellSelected());
            OnClick(_close.Button, () => _placer.Deselect());
        }

        public override void Refresh()
        {
            Tower tower = _placer.SelectedTower;
            bool newTower = tower != _shownTower;
            if (newTower)
            {
                _shownTower = tower;
                _title.text = $"<color=#80d0ff>{tower.name}</color>";
                _role.text = GrayboxArchetypeStyle.Coloured(_placer.ArchetypeOf(tower));
                _shownLevel = -1; // rebuild everything below
                _shownDamage = _shownKills = _shownShots = _shownUpgradeCost = _shownRefund = -1;
                _shownRange = -1f;
                _shownPriority = (TargetingPriority)(-1);
            }

            TargetingPriority priority = tower.Priority;
            if (priority != _shownPriority)
            {
                _shownPriority = priority;
                _priority.Index = Array.IndexOf(_priorities, priority);
            }

            if (tower.Level != _shownLevel || !Mathf.Approximately(tower.Range, _shownRange))
            {
                _shownLevel = tower.Level;
                _shownRange = tower.Range;
                _levelLine.text = $"Range {tower.Range:0.0}m  Level {tower.Level}/{tower.MaxLevel}";
            }

            int damage = Mathf.RoundToInt(tower.TotalDamageDealt);
            if (damage != _shownDamage)
            {
                _shownDamage = damage;
                _damage.text = $"Damage dealt  <color=#7fdd7f>{damage}</color>";
            }

            if (tower.TotalKills != _shownKills || tower.ShotsFired != _shownShots)
            {
                _shownKills = tower.TotalKills;
                _shownShots = tower.ShotsFired;
                _record.text = $"Kills <color=#7fdd7f>{tower.TotalKills}</color>   Shots {tower.ShotsFired}";
            }

            RefreshButtons(tower);
        }

        private void RefreshButtons(Tower tower)
        {
            int upgradeCost = _placer.UpgradeCost(tower);
            bool canAfford = GrayboxEconomy.Instance == null || GrayboxEconomy.Instance.CanAfford(upgradeCost);
            bool canUpgrade = tower.CanUpgrade;

            // The label changes with the level and the price; whether it can be pressed changes with the gold.
            if (upgradeCost != _shownUpgradeCost || _upgrade.Interactable != (canUpgrade && canAfford))
            {
                _shownUpgradeCost = upgradeCost;
                _upgrade.Label = canUpgrade ? $"Upgrade ${upgradeCost} → Lv {tower.Level + 1}" : "Max level";
                _upgrade.Interactable = canUpgrade && canAfford;
            }

            if (tower.RefundValue != _shownRefund)
            {
                _shownRefund = tower.RefundValue;
                _sell.Label = $"Dismantle +${tower.RefundValue}";
            }
        }

        private GrayboxTowerPlacer FindPlacer()
        {
            if (_placer == null)
            {
                _placer = FindAnyObjectByType<GrayboxTowerPlacer>();
            }

            return _placer;
        }
    }
}
