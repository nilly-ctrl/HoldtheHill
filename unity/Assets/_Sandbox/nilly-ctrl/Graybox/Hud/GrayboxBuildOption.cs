using HoldTheHill.Sandbox.UiKit;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>One tower in the build menu: a kit button with the tower's icon, name and price.</summary>
    [AddComponentMenu("Hold the Hill/Graybox/HUD/Build Option")]
    public class GrayboxBuildOption : MonoBehaviour
    {
        [SerializeField] private UiButton _button;
        [SerializeField] private Image _icon;

        private string _name;
        private int _costShown = -1;

        public void Bind(GrayboxTowerData data, UnityAction onClick)
        {
            Sprite icon = GrayboxIcons.GetSprite(data.IconName);
            _icon.sprite = icon;
            _icon.enabled = icon != null;
            _name = data.DisplayName;
            name = _name;
            _costShown = -1;

            _button.Button.onClick.RemoveAllListeners();
            _button.Button.onClick.AddListener(onClick);
            _button.Button.navigation = new Navigation { mode = Navigation.Mode.None };
        }

        public void Show(int cost, bool canAfford)
        {
            if (cost != _costShown)
            {
                _costShown = cost;
                _button.Label = $"{_name}\n${cost}";
            }

            _icon.color = canAfford ? Color.white : new Color(1f, 1f, 1f, 0.45f);
            _button.LabelColour = canAfford ? (Color)UiKitStyle.Cream : (Color)UiKitStyle.Disabled;
        }
    }
}
