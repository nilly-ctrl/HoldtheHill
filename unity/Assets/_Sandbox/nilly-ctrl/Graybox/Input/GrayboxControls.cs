using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Every key the graybox listens for, as one Input System action map, so the controls screen
    /// can list and rebind them. Scripts ask <see cref="Pressed"/> instead of reading the keyboard.
    /// </summary>
    /// <remarks>
    /// The map is made in code, not in <c>Settings/InputSystem_Actions.inputactions</c>: that asset
    /// is the team's, and the menus' own move/submit/cancel stay on its UI map. Changed bindings
    /// are kept in the save file (<see cref="GrayboxSaveData.bindingOverrides"/>).
    ///
    /// Gamepad is for menus only (docs/PLAN_MENUS_AND_FLOW.md), so the one gameplay action with a
    /// pad binding is Pause, on Start.
    /// </remarks>
    public static class GrayboxControls
    {
        public const string Pause = "Pause";
        public const string NextWave = "NextWave";
        public const string Speed = "Speed";
        public const string Restart = "Restart";
        public const string ToggleRings = "ToggleRings";
        public const string ToggleHealthBars = "ToggleHealthBars";
        public const string SkillTree = "SkillTree";
        public const string Achievements = "Achievements";
        public const string SpecialsPanel = "SpecialsPanel";
        public const string FontTheme = "FontTheme";

        // The Warden: walking, and one action per ability (GrayboxAbilityData.ControlId).
        public const string MoveUp = "MoveUp";
        public const string MoveDown = "MoveDown";
        public const string MoveLeft = "MoveLeft";
        public const string MoveRight = "MoveRight";
        public const string Shove = "Shove";
        public const string Rally = "Rally";
        public const string Repair = "Repair";
        public const string Dig = "Dig";
        public const string Carry = "Carry";

        /// <summary>Build slots are "Build1" to "Build10", in build bar order.</summary>
        public const int BuildSlots = 10;

        public const string KeyboardGroup = "Keyboard";
        public const string GamepadGroup = "Gamepad";

        /// <summary>One row of the controls screen.</summary>
        public sealed class Entry
        {
            public string Id;
            public string Label;
            public string Page;
            public string DefaultKey;
            public string DefaultPad;
        }

        private static readonly List<Entry> s_entries = BuildEntries();

        private static List<Entry> BuildEntries()
        {
            var entries = new List<Entry>
            {
                new Entry { Id = Pause, Label = "Pause or cancel", Page = "Game", DefaultKey = "<Keyboard>/escape", DefaultPad = "<Gamepad>/start" },
                new Entry { Id = NextWave, Label = "Start next wave", Page = "Game", DefaultKey = "<Keyboard>/n" },
                new Entry { Id = Speed, Label = "Change speed", Page = "Game", DefaultKey = "<Keyboard>/t" },
                new Entry { Id = Restart, Label = "Restart run", Page = "Game", DefaultKey = "<Keyboard>/r" },
                new Entry { Id = ToggleRings, Label = "Range rings", Page = "Game", DefaultKey = "<Keyboard>/v" },
                new Entry { Id = ToggleHealthBars, Label = "Health bars", Page = "Game", DefaultKey = "<Keyboard>/h" },
                new Entry { Id = SkillTree, Label = "Skill tree", Page = "Panels", DefaultKey = "<Keyboard>/k" },
                new Entry { Id = Achievements, Label = "Achievements", Page = "Panels", DefaultKey = "<Keyboard>/a" },
                new Entry { Id = SpecialsPanel, Label = "Spawn specials", Page = "Panels", DefaultKey = "<Keyboard>/b" },
                new Entry { Id = FontTheme, Label = "Next font theme", Page = "Panels", DefaultKey = "<Keyboard>/f6" },
                new Entry { Id = MoveUp, Label = "Warden up", Page = "Warden", DefaultKey = "<Keyboard>/upArrow" },
                new Entry { Id = MoveDown, Label = "Warden down", Page = "Warden", DefaultKey = "<Keyboard>/downArrow" },
                new Entry { Id = MoveLeft, Label = "Warden left", Page = "Warden", DefaultKey = "<Keyboard>/leftArrow" },
                new Entry { Id = MoveRight, Label = "Warden right", Page = "Warden", DefaultKey = "<Keyboard>/rightArrow" },
                new Entry { Id = Shove, Label = "Shove", Page = "Warden", DefaultKey = "<Keyboard>/space" },
                new Entry { Id = Rally, Label = "Rally", Page = "Warden", DefaultKey = "<Keyboard>/q" },
                new Entry { Id = Repair, Label = "Repair (hold)", Page = "Warden", DefaultKey = "<Keyboard>/e" },
                new Entry { Id = Dig, Label = "Dig (hold)", Page = "Warden", DefaultKey = "<Keyboard>/f" },
                new Entry { Id = Carry, Label = "Carry", Page = "Warden", DefaultKey = "<Keyboard>/c" },
            };

            for (int slot = 0; slot < BuildSlots; slot++)
            {
                int digit = (slot + 1) % 10; // the tenth slot is on 0
                entries.Add(new Entry
                {
                    Id = BuildId(slot),
                    Label = "Build slot " + (slot + 1),
                    Page = slot < 5 ? "Build 1-5" : "Build 6-10",
                    DefaultKey = "<Keyboard>/" + digit,
                });
            }

            return entries;
        }

        private static InputActionMap s_map;
        private static int s_suspendedUntilFrame = -1;

        /// <summary>Raised after a binding changes, is reset, or is loaded from the save file.</summary>
        public static event Action Changed;

        public static IReadOnlyList<Entry> Entries
        {
            get => s_entries;
        }

        /// <summary>True while a key is being captured for a rebind; gameplay input is ignored.</summary>
        public static bool IsRebinding { get; private set; }

        public static string BuildId(int slot) => "Build" + (slot + 1);

        /// <summary>True on the frame the action's key (or button) went down.</summary>
        public static bool Pressed(string id)
        {
            if (IsRebinding || Time.frameCount <= s_suspendedUntilFrame) return false;

            InputAction action = Find(id);
            return action != null && action.WasPressedThisFrame();
        }

        /// <summary>True while the action's key (or button) is down.</summary>
        public static bool Held(string id)
        {
            if (IsRebinding || Time.frameCount <= s_suspendedUntilFrame) return false;

            InputAction action = Find(id);
            return action != null && action.IsPressed();
        }

        public static InputAction Find(string id)
        {
            Ensure();
            return s_map.FindAction(id);
        }

        /// <summary>The binding of an action for one device group, or -1 when it has none.</summary>
        public static int BindingIndex(string id, string group)
        {
            InputAction action = Find(id);
            if (action == null) return -1;

            for (int i = 0; i < action.bindings.Count; i++)
            {
                if (action.bindings[i].groups == group) return i;
            }

            return -1;
        }

        /// <summary>The control path in use, e.g. "&lt;Keyboard&gt;/n", or null when the action has none for the group.</summary>
        public static string Path(string id, string group)
        {
            int index = BindingIndex(id, group);
            return index < 0 ? null : Find(id).bindings[index].effectivePath;
        }

        /// <summary>Short text for a binding: a prompt glyph from the pixel font where it has one.</summary>
        public static string Display(string id, string group) => GrayboxBindingText.For(Path(id, group));

        /// <summary>The key as plain text ("N", "Escape", "F6"), for IMGUI labels that have no prompt glyphs.</summary>
        public static string Name(string id, string group = KeyboardGroup)
        {
            string path = Path(id, group);
            return string.IsNullOrEmpty(path)
                ? GrayboxBindingText.None
                : InputControlPath.ToHumanReadableString(path, InputControlPath.HumanReadableStringOptions.OmitDevice);
        }

        /// <summary>Other actions in the same group bound to the same control.</summary>
        public static List<Entry> Conflicts(string id, string group)
        {
            var found = new List<Entry>();
            string path = Path(id, group);
            if (string.IsNullOrEmpty(path)) return found;

            foreach (Entry other in Entries)
            {
                if (other.Id != id && string.Equals(Path(other.Id, group), path, StringComparison.OrdinalIgnoreCase)) found.Add(other);
            }

            return found;
        }

        /// <summary>
        /// Waits for the next key (or pad button) and binds it. Escape cancels. The callback gets
        /// true when a new binding was set.
        /// </summary>
        public static void StartRebind(string id, string group, Action<bool> finished)
        {
            InputAction action = Find(id);
            int index = BindingIndex(id, group);
            if (action == null || index < 0 || IsRebinding)
            {
                finished?.Invoke(false);
                return;
            }

            IsRebinding = true;
            s_map.Disable();

            InputActionRebindingExtensions.RebindingOperation operation = action.PerformInteractiveRebinding(index)
                .WithCancelingThrough("<Keyboard>/escape")
                .WithControlsExcluding("<Mouse>")
                .WithControlsExcluding("<Pointer>")
                .OnMatchWaitForAnother(0.1f);

            operation.WithControlsHavingToMatchPath(group == GamepadGroup ? "<Gamepad>" : "<Keyboard>");

            void End(bool changed)
            {
                operation.Dispose();
                s_map.Enable();
                IsRebinding = false;
                // The key that was just bound (or Escape) is still down this frame.
                s_suspendedUntilFrame = Time.frameCount + 1;
                if (changed) Store();
                finished?.Invoke(changed);
            }

            operation.OnComplete(_ => End(true)).OnCancel(_ => End(false)).Start();
        }

        public static void ResetToDefaults()
        {
            Ensure();
            s_map.RemoveAllBindingOverrides();
            Store();
        }

        /// <summary>Binds a control directly, as a rebind would. For tests and tools.</summary>
        public static void SetBinding(string id, string group, string path)
        {
            int index = BindingIndex(id, group);
            if (index < 0) return;

            Find(id).ApplyBindingOverride(index, path);
            Store();
        }

        // ---------- internals ----------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            s_map?.Dispose();
            s_map = null;
            IsRebinding = false;
            s_suspendedUntilFrame = -1;
            Changed = null;
            GrayboxSave.Loaded -= Load;
        }

        private static void Ensure()
        {
            if (s_map != null) return;

            s_map = new InputActionMap("Graybox");
            foreach (Entry entry in s_entries)
            {
                InputAction action = s_map.AddAction(entry.Id, InputActionType.Button);
                action.AddBinding(entry.DefaultKey, groups: KeyboardGroup);
                if (!string.IsNullOrEmpty(entry.DefaultPad)) action.AddBinding(entry.DefaultPad, groups: GamepadGroup);
            }

            GrayboxSave.Loaded -= Load;
            GrayboxSave.Loaded += Load;
            Load();
            s_map.Enable();
        }

        private static void Load()
        {
            if (s_map == null) return;

            s_map.RemoveAllBindingOverrides();
            string saved = GrayboxSave.Data.bindingOverrides;
            if (!string.IsNullOrEmpty(saved))
            {
                try
                {
                    s_map.LoadBindingOverridesFromJson(saved);
                }
                catch (Exception e) when (e is ArgumentException || e is InvalidOperationException)
                {
                    Debug.LogWarning($"[GrayboxControls] Saved key bindings could not be read ({e.Message}); using the defaults.");
                }
            }

            Changed?.Invoke();
        }

        private static void Store()
        {
            GrayboxSave.Data.bindingOverrides = s_map.SaveBindingOverridesAsJson();
            GrayboxSave.MarkDirty();
            GrayboxSave.Save();
            Changed?.Invoke();
        }
    }
}
