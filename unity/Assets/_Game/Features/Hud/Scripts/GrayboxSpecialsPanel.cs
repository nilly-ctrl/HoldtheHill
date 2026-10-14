using HoldTheHill.Features.Specials;
using System.Collections.Generic;
using HoldTheHill.Core.UiKit;
using UnityEngine;

namespace HoldTheHill.Features.Hud
{
    /// <summary>
    /// A button per boss and special enemy that spawns one at the burrow, for trying them out.
    /// Opened with the specials key (<see cref="GrayboxRunHotkeys"/>). A tuning panel, like the
    /// horde panel; replaces the IMGUI list that <see cref="GrayboxSpecials"/> used to draw.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Graybox/HUD/Specials Panel")]
    public class GrayboxSpecialsPanel : GrayboxHudPanel
    {
        [Tooltip("The kit button, copied once per special.")]
        [SerializeField] private UiButton _buttonPrefab;

        [SerializeField] private RectTransform _buttonsRoot;
        [SerializeField] private UiButton _close;

        private readonly List<UiButton> _buttons = new List<UiButton>();
        private GrayboxSpecials _builtFor;

        /// <summary>True while the panel is up. Toggled by the specials key.</summary>
        public static bool Open { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Open = false;

        public override bool WantsToShow =>
            Open && GrayboxSpecials.Instance != null && GrayboxSpecials.Instance.Entries.Count > 0;

        public IReadOnlyList<UiButton> Buttons => _buttons;

        private void Awake()
        {
            OnClick(_close.Button, () => Open = false);
        }

        public override void Refresh()
        {
            GrayboxSpecials specials = GrayboxSpecials.Instance;
            if (specials != _builtFor || _buttons.Count != specials.Entries.Count)
            {
                Rebuild(specials);
            }
        }

        private void Rebuild(GrayboxSpecials specials)
        {
            foreach (UiButton button in _buttons)
            {
                Destroy(button.gameObject);
            }

            _buttons.Clear();
            _builtFor = specials;

            foreach (GrayboxSpecials.Entry entry in specials.Entries)
            {
                UiButton button = Instantiate(_buttonPrefab, _buttonsRoot);
                button.name = entry.Id;
                button.Label = (entry.Boss ? "BOSS  " : string.Empty) + entry.Label;
                string id = entry.Id;
                OnClick(button.Button, () => specials.Spawn(id));
                _buttons.Add(button);
            }
        }
    }
}
