using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace HoldTheHill.Core.UiKit
{
    /// <summary>
    /// The menu inputs Unity's UI module does not deliver as events: back, and previous/next tab.
    /// </summary>
    /// <remarks>
    /// Back comes from the event system's own cancel action when there is one. Tab switching, and
    /// back without an event system, come from a small action map made here (shoulder buttons,
    /// Q/E, Page Up/Down), so no script reads a keyboard or a gamepad directly. The kit has no
    /// controls screen of its own; a game that wants these rebindable can swap the bindings
    /// through <see cref="Actions"/>.
    /// </remarks>
    public static class UiInput
    {
        public const string Back = "Back";
        public const string TabPrevious = "TabPrevious";
        public const string TabNext = "TabNext";

        private static InputActionMap s_map;

        /// <summary>
        /// While true, back and tab switching are ignored. Set while a key is being captured for a
        /// rebind, when Esc, Q and E are answers, not commands.
        /// </summary>
        public static bool Suspended { get; set; }

        /// <summary>The kit's own actions: Back, TabPrevious, TabNext.</summary>
        public static InputActionMap Actions
        {
            get
            {
                Ensure();
                return s_map;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Suspended = false;
            s_map?.Dispose();
            s_map = null;
        }

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

            return Actions[Back].WasPressedThisFrame();
        }

        /// <summary>-1 for previous tab, +1 for next tab, 0 for neither, on the frame it was pressed.</summary>
        public static int TabStep()
        {
            if (Suspended) return 0;

            bool previous = Actions[TabPrevious].WasPressedThisFrame();
            bool next = Actions[TabNext].WasPressedThisFrame();

            if (previous == next) return 0;
            return previous ? -1 : 1;
        }

        private static void Ensure()
        {
            if (s_map != null) return;

            s_map = new InputActionMap("UiKit");
            InputAction back = s_map.AddAction(Back, InputActionType.Button);
            back.AddBinding("<Keyboard>/escape");
            back.AddBinding("<Gamepad>/buttonEast");

            InputAction previous = s_map.AddAction(TabPrevious, InputActionType.Button);
            previous.AddBinding("<Keyboard>/q");
            previous.AddBinding("<Keyboard>/pageUp");
            previous.AddBinding("<Gamepad>/leftShoulder");

            InputAction next = s_map.AddAction(TabNext, InputActionType.Button);
            next.AddBinding("<Keyboard>/e");
            next.AddBinding("<Keyboard>/pageDown");
            next.AddBinding("<Gamepad>/rightShoulder");

            s_map.Enable();
        }
    }
}
