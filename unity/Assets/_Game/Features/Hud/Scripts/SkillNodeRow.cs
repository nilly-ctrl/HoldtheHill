using HoldTheHill.Core.Icons;
using HoldTheHill.Features.Progression;
using HoldTheHill.Core.UiKit;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HoldTheHill.Features.Hud
{
    /// <summary>One skill in the skill tree window: a kit button with the skill's icon, name, price and description.</summary>
    [AddComponentMenu("Hold the Hill/HUD/Skill Node Row")]
    public class SkillNodeRow : MonoBehaviour
    {
        [SerializeField] private UiButton _button;
        [SerializeField] private Image _icon;

        private SkillNode _node;
        private int _stateShown = -1;

        public SkillNode Node => _node;

        public UiButton Button => _button;

        public void Bind(SkillNode node, UnityAction onClick)
        {
            _node = node;
            name = node.id;
            Sprite icon = IconLibrary.GetSprite(node.Data.IconName);
            _icon.sprite = icon;
            _icon.enabled = icon != null;
            _stateShown = -1;

            _button.Button.onClick.RemoveAllListeners();
            _button.Button.onClick.AddListener(onClick);
            _button.Button.navigation = new Navigation { mode = Navigation.Mode.None };
        }

        /// <summary>Owned, affordable now, or not yet. Only rebuilds the text when that changes.</summary>
        public void Show(bool unlocked, bool canUnlock)
        {
            int state = unlocked ? 2 : canUnlock ? 1 : 0;
            if (state == _stateShown)
            {
                return;
            }

            _stateShown = state;
            string price = unlocked ? "OWNED" : $"{_node.cost} SP";
            _button.Label = $"{_node.name}  {price}\n<size=80%>{_node.description}</size>";
            _button.LabelColour = unlocked ? (Color)UiKitStyle.Gold : canUnlock ? (Color)UiKitStyle.Cream : (Color)UiKitStyle.Disabled;
            _button.Interactable = canUnlock;
            _icon.color = unlocked || canUnlock ? Color.white : new Color(1f, 1f, 1f, 0.45f);
        }
    }
}
