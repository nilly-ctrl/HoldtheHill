using HoldTheHill.Core.Input;
using HoldTheHill.Core.Localization;
using System;
using System.Collections.Generic;
using HoldTheHill.Core.UiKit;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace HoldTheHill.Features.Menus
{
    /// <summary>
    /// The controls screen: every action in <see cref="GameControls"/> with its key. Press a
    /// row to rebind it; the next key pressed becomes the binding and Esc cancels.
    /// </summary>
    /// <remarks>
    /// The rows show the keyboard until a gamepad button is pressed, then the gamepad, and back
    /// again on a key or click. Only Pause has a gamepad binding: the pad is for menus, and the
    /// menus' own move, press and back are fixed (the line at the bottom lists them).
    ///
    /// Two actions on one key is allowed but called out, since swapping two keys passes through it.
    /// </remarks>
    [RequireComponent(typeof(UiScreen))]
    [AddComponentMenu("Hold the Hill/Controls Screen")]
    public class ControlsScreen : MonoBehaviour
    {
        private static readonly Color32 Clash = new Color32(0xff, 0x9a, 0x5a, 0xff);

        [Serializable]
        public class Row
        {
            public string Id;
            public TMP_Text Label;
            public UiButton Button;
        }

        [SerializeField] private List<Row> _rows = new List<Row>();
        [SerializeField] private TMP_Text _device;
        [SerializeField] private TMP_Text _message;
        [SerializeField] private UiButton _reset;
        [SerializeField] private UiButton _done;

        private UiScreen _screen;
        private bool _hooked;
        private string _group = GameControls.KeyboardGroup;
        private Row _capturing;
        private float _ignoreClicksUntil;
        private int _resumeMenuInputFrame = -1;

        public UiScreen Screen => _screen != null ? _screen : _screen = GetComponent<UiScreen>();
        public IReadOnlyList<Row> Rows => _rows;
        public TMP_Text Message => _message;
        public UiButton ResetButton => _reset;
        public UiButton DoneButton => _done;

        /// <summary>"Keyboard" or "Gamepad": whose bindings the rows show.</summary>
        public string Group => _group;

        public Row FindRow(string id) => _rows.Find(r => r.Id == id);

        private void OnEnable()
        {
            Hook();
            GameControls.Changed += Refresh;
            _message.color = UiKitStyle.Dim;
            _message.text = Loc.Get("Press a row to change its key.");
            Refresh();
        }

        private void OnDisable()
        {
            GameControls.Changed -= Refresh;
            ResumeMenuInput();
        }

        private void Update()
        {
            if (_resumeMenuInputFrame >= 0 && Time.frameCount >= _resumeMenuInputFrame) ResumeMenuInput();
            if (GameControls.IsRebinding || !Screen.IsTop) return;

            Gamepad pad = Gamepad.current;
            Keyboard keys = Keyboard.current;
            Mouse mouse = Mouse.current;

            bool padUsed = pad != null && pad.wasUpdatedThisFrame && AnyButton(pad);
            bool keysUsed = (keys != null && keys.anyKey.wasPressedThisFrame) || (mouse != null && mouse.leftButton.wasPressedThisFrame);
            if (padUsed) ShowGroup(GameControls.GamepadGroup);
            else if (keysUsed) ShowGroup(GameControls.KeyboardGroup);
        }

        /// <summary>Switches the rows between keyboard and gamepad bindings.</summary>
        public void ShowGroup(string group)
        {
            if (_group == group) return;

            _group = group;
            Refresh();
        }

        /// <summary>Starts capturing a key for a row, as pressing its button does.</summary>
        public void BeginRebind(Row row)
        {
            if (GameControls.IsRebinding || Time.unscaledTime < _ignoreClicksUntil) return;
            if (GameControls.BindingIndex(row.Id, _group) < 0) return;

            _capturing = row;
            row.Button.Label = Loc.Get("Press...");
            _message.color = UiKitStyle.Dim;
            _message.text = _group == GameControls.GamepadGroup
                ? Loc.Get("Press a button. Esc cancels.")
                : Loc.Get("Press a key. Esc cancels.");

            // Until the capture ends, keys are answers: no back, no tab switching, no moving focus.
            UiInput.Suspended = true;
            _resumeMenuInputFrame = -1;
            if (EventSystem.current != null) EventSystem.current.sendNavigationEvents = false;

            GameControls.StartRebind(row.Id, _group, OnRebindFinished);
        }

        private void OnRebindFinished(bool changed)
        {
            Row row = _capturing;
            _capturing = null;

            // The captured key is still down: let it come up before the menu listens again.
            _resumeMenuInputFrame = Time.frameCount + 2;
            _ignoreClicksUntil = Time.unscaledTime + 0.25f;

            Refresh();
            if (row == null) return;

            List<GameControls.Entry> clashes = changed ? GameControls.Conflicts(row.Id, _group) : null;
            if (clashes != null && clashes.Count > 0)
            {
                _message.color = Clash;
                _message.text = Loc.Get("{0} is also \"{1}\".", GameControls.Display(row.Id, _group), clashes[0].Label);
            }
            else
            {
                _message.color = UiKitStyle.Dim;
                _message.text = changed ? Loc.Get("Saved.") : Loc.Get("Unchanged.");
            }
        }

        private void ResumeMenuInput()
        {
            _resumeMenuInputFrame = -1;
            UiInput.Suspended = false;
            if (EventSystem.current != null) EventSystem.current.sendNavigationEvents = true;
        }

        private void Refresh()
        {
            bool pad = _group == GameControls.GamepadGroup;
            _device.text = pad
                ? Loc.Get("Gamepad. Stick or d-pad moves, A presses, shoulders switch tab, B goes back.")
                : Loc.Get("Keyboard. Arrows move, Enter presses, Q and E switch tab, Esc goes back.");

            foreach (Row row in _rows)
            {
                if (row == _capturing) continue;

                bool bound = GameControls.BindingIndex(row.Id, _group) >= 0;
                row.Button.Label = GameControls.Display(row.Id, _group);
                row.Button.Interactable = bound;
                row.Label.color = bound && GameControls.Conflicts(row.Id, _group).Count > 0 ? (Color)Clash : (Color)UiKitStyle.Cream;
            }
        }

        private void Hook()
        {
            if (_hooked) return;
            _hooked = true;

            foreach (Row row in _rows)
            {
                Row captured = row;
                row.Button.Clicked.AddListener(() => BeginRebind(captured));
            }

            _reset.Clicked.AddListener(() =>
            {
                GameControls.ResetToDefaults();
                _message.color = UiKitStyle.Dim;
                _message.text = Loc.Get("Back to the default keys.");
            });
            _done.Clicked.AddListener(Screen.Hide);
        }

        private static bool AnyButton(Gamepad pad)
        {
            return pad.buttonSouth.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame
                || pad.buttonWest.wasPressedThisFrame || pad.buttonNorth.wasPressedThisFrame
                || pad.startButton.wasPressedThisFrame || pad.selectButton.wasPressedThisFrame
                || pad.leftShoulder.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame
                || pad.dpad.up.wasPressedThisFrame || pad.dpad.down.wasPressedThisFrame
                || pad.dpad.left.wasPressedThisFrame || pad.dpad.right.wasPressedThisFrame
                || pad.leftStick.ReadValue().sqrMagnitude > 0.25f;
        }
    }
}
