using System.Collections.Generic;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// A strip of chips above the build bar: the Warden's health, then each ability with its key and
    /// the cooldown or hold in progress, then any power-up that is running with its time left.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Graybox/HUD/Warden Panel")]
    public class GrayboxWardenPanel : GrayboxHudPanel
    {
        [Tooltip("Made as needed, one per chip.")]
        [SerializeField] private GrayboxHudChip _chipPrefab;

        [SerializeField] private RectTransform _chipRoot;

        private readonly List<GrayboxHudChip> _chips = new List<GrayboxHudChip>();
        private int _used;

        public override bool WantsToShow => GrayboxWarden.Instance != null && GrayboxWarden.Instance.Abilities.Count > 0;

        public override void Refresh()
        {
            GrayboxWarden warden = GrayboxWarden.Instance;
            _used = 0;

            if (warden.Health != null)
            {
                ShowHealth(warden.Health);
            }

            foreach (GrayboxWardenAbility ability in warden.Abilities)
            {
                ShowAbility(ability);
            }

            for (int i = 0; i < GrayboxPowerUpEffect.Active.Count; i++)
            {
                ShowPowerUp(GrayboxPowerUpEffect.Active[i]);
            }

            for (int i = _used; i < _chips.Count; i++)
            {
                if (_chips[i].gameObject.activeSelf)
                {
                    _chips[i].gameObject.SetActive(false);
                }
            }
        }

        // The next free chip, shown.
        private GrayboxHudChip Next()
        {
            if (_used == _chips.Count)
            {
                _chips.Add(Instantiate(_chipPrefab, _chipRoot));
            }

            GrayboxHudChip chip = _chips[_used++];
            if (!chip.gameObject.activeSelf)
            {
                chip.gameObject.SetActive(true);
                chip.StateKey = int.MinValue;
            }

            return chip;
        }

        private void ShowHealth(GrayboxWardenHealth health)
        {
            GrayboxHudChip chip = Next();
            int key = health.IsDowned ? -1 - Mathf.CeilToInt(health.DownedLeft * 10f) : Mathf.CeilToInt(health.CurrentHealth * 10f);
            if (key == chip.StateKey)
            {
                return;
            }

            chip.StateKey = key;
            chip.Text = health.IsDowned
                ? $"<color=#ff8080>DOWN {health.DownedLeft:0.0}s</color>"
                : $"{PixelGlyphs.Heart} {Mathf.CeilToInt(health.CurrentHealth)}/{Mathf.RoundToInt(health.MaxHealth)}";
        }

        private void ShowAbility(GrayboxWardenAbility ability)
        {
            GrayboxHudChip chip = Next();
            GrayboxAbilityData data = ability.Data;
            if (data == null)
            {
                chip.Text = ability.name;
                return;
            }

            // What the chip shows changes with the cooldown (tenths), the hold (percent) and whether it can be used.
            int key = !ability.IsReady ? 1000000 + Mathf.CeilToInt(ability.CooldownLeft * 10f)
                : ability.HoldProgress > 0f ? 2000000 + Mathf.RoundToInt(ability.HoldProgress * 100f)
                : ability.CanUse() ? 0 : 1;
            if (key == chip.StateKey)
            {
                return;
            }

            chip.StateKey = key;
            string label = $"{GrayboxControls.Name(data.ControlId)} {data.DisplayName}";
            if (!ability.IsReady)
            {
                chip.Text = $"{label} <color=#ff8080>{ability.CooldownLeft:0.0}s</color>";
            }
            else if (ability.HoldProgress > 0f)
            {
                chip.Text = $"{label} <color=#80d0ff>{Mathf.RoundToInt(ability.HoldProgress * 100f)}%</color>";
            }
            else
            {
                chip.Text = ability.CanUse() ? label : $"<color=#808080>{label}</color>";
            }
        }

        private void ShowPowerUp(GrayboxPowerUpEffect effect)
        {
            GrayboxHudChip chip = Next();
            int key = Mathf.CeilToInt(effect.Remaining * 10f);
            if (key == chip.StateKey)
            {
                return;
            }

            chip.StateKey = key;
            string colour = ColorUtility.ToHtmlStringRGB(effect.Data.Color);
            chip.Text = $"<color=#{colour}>{effect.Data.DisplayName} {effect.Remaining:0.0}s</color>";
        }
    }
}
