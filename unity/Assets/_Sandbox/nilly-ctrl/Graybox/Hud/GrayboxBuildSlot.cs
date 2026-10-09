using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// One tower on the build bar: its icon in a slot frame, the hotkey on a keycap above the corner
    /// and its price on a tag underneath. The build bar makes one of these from a prefab for each
    /// tower in the catalog.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Graybox/HUD/Build Slot")]
    public class GrayboxBuildSlot : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [SerializeField] private Image _frame;
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _key;
        [SerializeField] private TMP_Text _cost;
        [SerializeField] private Image _costTag;

        [Header("Frames")]
        [SerializeField] private Sprite _frameNormal;
        [SerializeField] private Sprite _frameSelected;
        [SerializeField] private Sprite _frameDisabled;

        [Header("Cost tags")]
        [SerializeField] private Sprite _tagAffordable;
        [SerializeField] private Sprite _tagTooDear;

        private string _keyShown;
        private int _costShown = -1;

        public Button Button => _button;

        /// <summary>Shows a tower in the slot. <paramref name="onClick"/> is run when the slot is clicked.</summary>
        public void Bind(GrayboxTowerData data, UnityAction onClick)
        {
            Sprite icon = GrayboxIcons.GetSprite(data.IconName);
            _icon.sprite = icon;
            _icon.enabled = icon != null;
            name = data.DisplayName;
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(onClick);
            _button.navigation = new Navigation { mode = Navigation.Mode.None };
        }

        /// <summary>Brings the slot up to date: the hotkey, the price and whether it is picked or affordable.</summary>
        public void Show(string key, int cost, bool selected, bool canAfford)
        {
            if (key != _keyShown)
            {
                _keyShown = key;
                _key.text = key;
            }

            if (cost != _costShown)
            {
                _costShown = cost;
                _cost.text = cost.ToString();
            }

            _frame.sprite = selected ? _frameSelected : (canAfford ? _frameNormal : _frameDisabled);
            _costTag.sprite = canAfford ? _tagAffordable : _tagTooDear;
            _icon.color = canAfford ? Color.white : new Color(1f, 1f, 1f, 0.45f);
        }
    }
}
