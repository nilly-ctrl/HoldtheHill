using System;
using HoldTheHill.Core.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HoldTheHill.Features.Towers
{
    /// <summary>
    /// What the placer reads: the pointer, confirm and cancel through Input System action
    /// references when they are assigned, else through <see cref="GameControls"/>, whose build
    /// keys 1 to 0 pick a catalog slot.
    /// </summary>
    [Serializable]
    public sealed class PlacerInput
    {
        [Tooltip("The pointer position on screen (the UI map's Point). Empty: the game controls' pointer.")]
        [SerializeField] private InputActionReference _point;

        [Tooltip("Confirms a cell or picks a tower (the UI map's Click). Empty: the game controls' Confirm.")]
        [SerializeField] private InputActionReference _confirm;

        [Tooltip("Drops the selection, build menu or placement (the UI map's Cancel). Empty: the game controls' Cancel or Pause.")]
        [SerializeField] private InputActionReference _cancel;

        public void Enable()
        {
            Enable(_point);
            Enable(_confirm);
            Enable(_cancel);
        }

        /// <summary>The pointer position on screen, if there is a pointer.</summary>
        public bool TryReadPointer(out Vector2 position)
        {
            if (_point != null && _point.action != null)
            {
                position = _point.action.enabled ? _point.action.ReadValue<Vector2>() : default;
                return _point.action.enabled;
            }

            return GameControls.TryPointerPosition(out position);
        }

        /// <summary>True on the frame confirm was pressed.</summary>
        public bool ConfirmPressed()
        {
            return _confirm != null && _confirm.action != null
                ? _confirm.action.WasPressedThisFrame()
                : GameControls.Pressed(GameControls.Confirm);
        }

        /// <summary>True on the frame cancel was pressed. Escape is both pause and cancel: the flow pauses, the placer drops what it holds.</summary>
        public bool CancelPressed()
        {
            return _cancel != null && _cancel.action != null
                ? _cancel.action.WasPressedThisFrame()
                : GameControls.Pressed(GameControls.Pause) || GameControls.Pressed(GameControls.Cancel);
        }

        /// <summary>The catalog slot whose build key was pressed this frame, or -1.</summary>
        public int BuildKeyPressed(int slots)
        {
            for (int i = 0; i < slots; i++)
            {
                if (GameControls.Pressed(GameControls.BuildId(i)))
                {
                    return i;
                }
            }

            return -1;
        }

        private static void Enable(InputActionReference reference)
        {
            if (reference != null && reference.action != null)
            {
                reference.action.Enable();
            }
        }
    }
}
