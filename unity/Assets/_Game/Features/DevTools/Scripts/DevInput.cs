#if UNITY_EDITOR || DEVELOPMENT_BUILD
using HoldTheHill.Core.Input;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace HoldTheHill.Features.DevTools
{
    /// <summary>
    /// Switches the game's input off while a dev text box has focus, so typing "n" or "3" into the
    /// console does not start a wave or pick a tower. It disables the action map that
    /// <see cref="GameControls"/> reads, through the map's own public API; the game scripts
    /// are not touched. The dev tools read the keyboard device directly, so they still work.
    /// </summary>
    internal static class DevInput
    {
        private static readonly HashSet<object> s_owners = new HashSet<object>();
        private static InputActionMap s_map;

        public static bool Locked => s_owners.Count > 0;

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_owners.Clear();
            s_map = null;
        }

        public static void Lock(object owner, bool locked)
        {
            bool before = Locked;
            if (locked) s_owners.Add(owner);
            else s_owners.Remove(owner);

            if (before == Locked) return;

            if (s_map == null)
            {
                if (!Locked) return; // nothing was disabled, so there is nothing to put back

                InputAction action = GameControls.Find(GameControls.Pause);
                s_map = action != null ? action.actionMap : null;
            }

            if (s_map == null) return;

            if (Locked) s_map.Disable();
            else s_map.Enable();
        }

        /// <summary>Drops every lock, for when the dev tools are torn down.</summary>
        public static void Reset()
        {
            s_owners.Clear();
            if (s_map != null) s_map.Enable();
            s_map = null;
        }
    }
}
#endif
