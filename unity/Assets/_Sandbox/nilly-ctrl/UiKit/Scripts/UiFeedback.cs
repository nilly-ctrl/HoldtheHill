using System;

namespace HoldTheHill.Sandbox.UiKit
{
    /// <summary>The moments a menu makes a sound or other feedback.</summary>
    public enum UiCue
    {
        /// <summary>Focus moved to another control.</summary>
        Focus,

        /// <summary>A button or tab was pressed.</summary>
        Press,

        /// <summary>A slider, toggle or stepper changed value.</summary>
        Change,

        /// <summary>Back or cancel was pressed.</summary>
        Back,

        /// <summary>A disabled control was pressed.</summary>
        Denied,

        /// <summary>A screen or dialog opened.</summary>
        Open,

        /// <summary>A screen or dialog closed.</summary>
        Close,
    }

    /// <summary>
    /// The kit's one outlet for feedback. Widgets raise cues here; whatever plays sound (or rumble)
    /// subscribes. The kit itself has no audio, so it does not depend on the game's sound system.
    /// </summary>
    public static class UiFeedback
    {
        public static event Action<UiCue> Cue;

        private static int s_muted;

        /// <summary>
        /// Silences cues until the returned scope is disposed. Used while code, not the player,
        /// is moving focus or setting values.
        /// </summary>
        public static MuteScope Mute() => new MuteScope(true);

        public static void Raise(UiCue cue)
        {
            if (s_muted == 0) Cue?.Invoke(cue);
        }

        public readonly struct MuteScope : IDisposable
        {
            private readonly bool _active;

            internal MuteScope(bool active)
            {
                _active = active;
                if (active) s_muted++;
            }

            public void Dispose()
            {
                if (_active && s_muted > 0) s_muted--;
            }
        }
    }
}
