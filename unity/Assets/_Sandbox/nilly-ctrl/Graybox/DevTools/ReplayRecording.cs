#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>The kinds of player command a replay holds. Stored as text so the JSON stays readable.</summary>
    public static class ReplayCommandType
    {
        /// <summary>Build a tower. Name is the tower's name, X/Y its cell.</summary>
        public const string Place = "place";

        /// <summary>Sell the tower on the cell X/Y.</summary>
        public const string Sell = "sell";

        /// <summary>Upgrade the tower on the cell X/Y. Value is the level it reached.</summary>
        public const string Upgrade = "upgrade";

        /// <summary>Use a Warden ability. Name is its id; X/Y is where the Warden stood.</summary>
        public const string Ability = "ability";

        /// <summary>Unlock the skill node whose id is Name.</summary>
        public const string Skill = "skill";

        /// <summary>Start wave number Value (1-based).</summary>
        public const string Wave = "wave";

        /// <summary>The field was rewound. Value is 1 for "retry current wave", 0 for a new run.</summary>
        public const string Reset = "reset";

        /// <summary>A console command; Name is the whole line.</summary>
        public const string Dev = "dev";

        public static bool IsKnown(string type)
        {
            return type == Place || type == Sell || type == Upgrade || type == Ability
                || type == Skill || type == Wave || type == Reset || type == Dev;
        }
    }

    /// <summary>One recorded command and the fixed-step tick it happened on.</summary>
    [Serializable]
    public class ReplayCommand
    {
        /// <summary>Fixed steps since the recording started.</summary>
        public int tick;

        /// <summary>The same moment in seconds of game time: tick times the recording's fixed step.</summary>
        public float time;

        public string type;
        public string name;
        public float x;
        public float y;
        public int value;
    }

    /// <summary>
    /// A recorded run: how it was set up and the commands the player gave. This is what is
    /// written to JSON. It is not a video or a state dump; see the README for what it cannot reproduce.
    /// </summary>
    [Serializable]
    public class ReplayRecording
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public string unityVersion;
        public string mapId;

        /// <summary>"Campaign" or "Endless".</summary>
        public string mode;

        /// <summary>The seed UnityEngine.Random was set to when recording began.</summary>
        public int seed;

        /// <summary>Seconds per fixed step when it was recorded.</summary>
        public float fixedDeltaTime = 0.02f;

        /// <summary>0-based wave the spawner was on when recording began. Above 0 means a mid-run start.</summary>
        public int startWave;

        public int startGold;
        public int startTowers;

        /// <summary>Fixed steps recorded in total.</summary>
        public int endTick;

        /// <summary>"Victory", "Defeat", "Stopped" or empty while still recording.</summary>
        public string result;

        public List<ReplayCommand> commands = new List<ReplayCommand>();

        /// <summary>True if recording began after the run had already started, so playback cannot rebuild its start.</summary>
        public bool StartedMidRun => startWave > 0 || startTowers > 0;

        public ReplayCommand Add(int tick, string type, string name = null, float x = 0f, float y = 0f, int value = 0)
        {
            var command = new ReplayCommand
            {
                tick = tick,
                time = tick * fixedDeltaTime,
                type = type,
                name = name,
                x = x,
                y = y,
                value = value,
            };
            commands.Add(command);
            return command;
        }
    }

    /// <summary>Reads and writes <see cref="ReplayRecording"/> as JSON and checks what it read.</summary>
    public static class ReplaySerializer
    {
        public static string ToJson(ReplayRecording recording, bool pretty = true)
        {
            if (recording == null) throw new ArgumentNullException(nameof(recording));
            return JsonUtility.ToJson(recording, pretty);
        }

        /// <summary>
        /// Parses a recording. Returns null and sets <paramref name="error"/> if the text is not a
        /// replay of this version or its commands are out of order or of an unknown kind.
        /// </summary>
        public static ReplayRecording FromJson(string json, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                error = "The replay file is empty.";
                return null;
            }

            ReplayRecording recording;
            try
            {
                recording = JsonUtility.FromJson<ReplayRecording>(json);
            }
            catch (Exception ex)
            {
                error = "Not valid replay JSON: " + ex.Message;
                return null;
            }

            if (recording == null)
            {
                error = "Not valid replay JSON.";
                return null;
            }

            error = Validate(recording);
            return error == null ? recording : null;
        }

        /// <summary>Returns a description of the first problem, or null if the recording is playable.</summary>
        public static string Validate(ReplayRecording recording)
        {
            if (recording.version != ReplayRecording.CurrentVersion)
            {
                return "Replay version " + recording.version + " is not supported (this build reads version "
                    + ReplayRecording.CurrentVersion + ").";
            }

            if (recording.commands == null)
            {
                recording.commands = new List<ReplayCommand>();
            }

            int previous = 0;
            for (int i = 0; i < recording.commands.Count; i++)
            {
                ReplayCommand command = recording.commands[i];
                if (command == null) return "Command " + i + " is empty.";
                if (!ReplayCommandType.IsKnown(command.type)) return "Command " + i + " has unknown type '" + command.type + "'.";
                if (command.tick < previous) return "Command " + i + " is earlier than the one before it.";
                previous = command.tick;
            }

            if (recording.fixedDeltaTime <= 0f) return "The recorded fixed step is not positive.";
            return null;
        }
    }
}
#endif
