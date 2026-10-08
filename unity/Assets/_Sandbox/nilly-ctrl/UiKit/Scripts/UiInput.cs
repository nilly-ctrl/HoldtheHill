using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace HoldTheHill.Sandbox.UiKit
{
    /// <summary>
    /// The menu inputs Unity's UI module does not deliver as events: back, and previous/next tab.
    /// </summary>
    /// <remarks>
    /// Tab switching reads devices directly for now (shoulder buttons, Q/E, Page Up/Down).
    /// It moves into the game's action map with the controls work (plan theme 6).
    /// </remarks>
    public static class UiInput
    {
        /// <summary>
        /// While true, back and tab switching are ignored. Set while a key is being captured for a
        /// rebind, when Esc, Q and E are answers, not commands.
        /// </summary>
        public static bool Suspended { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => Suspended = false;

        /// <summary>True on the frame back/cancel was pressed (Esc, gamepad B/Circle by default).</summary>
        public static bool BackPressed()
        {
            if (Suspended) return false;

            var module = EventSystem.current != null ? EventSystem.current.currentInputModule as InputSystemUIInputModule : null;
            InputAction cancel = module != null && module.cancel != null ? module.cancel.action : null;
            if (cancel != null)
            {
                return cancel.WasPerformedThisFrame();
            }

            return (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
        }

        /// <summary>-1 for previous tab, +1 for next tab, 0 for neither, on the frame it was pressed.</summary>
        public static int TabStep()
        {
            if (Suspended) return 0;

            Gamepad pad = Gamepad.current;
            Keyboard keys = Keyboard.current;

            bool previous = (pad != null && pad.leftShoulder.wasPressedThisFrame)
                || (keys != null && (keys.qKey.wasPressedThisFrame || keys.pageUpKey.wasPressedThisFrame));
            bool next = (pad != null && pad.rightShoulder.wasPressedThisFrame)
                || (keys != null && (keys.eKey.wasPressedThisFrame || keys.pageDownKey.wasPressedThisFrame));

            if (previous == next) return 0;
            return previous ? -1 : 1;
        }
    }
}
