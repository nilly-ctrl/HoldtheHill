#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Enemies;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The dev console's replay commands: rec and replay. Registered by <see cref="DevBuiltinCommands"/>.
    /// </summary>
    internal static class DevReplayCommands
    {
        internal static string Rec(DevCommandArgs a)
        {
            DevReplayRecorder recorder = DevReplayRecorder.Instance;
            if (recorder == null) throw new DevCommandException("No replay recorder in the scene (add DevTools).");

            string verb = a.Has(0) ? a[0].ToLowerInvariant() : "status";
            switch (verb)
            {
                case "start":
                    if (!recorder.StartRecording(out string error)) throw new DevCommandException(error);
                    return "Recording. Seed " + recorder.Current.seed + ".";

                case "stop":
                    if (!recorder.IsRecording) throw new DevCommandException("Not recording.");
                    string path = recorder.StopRecording("Stopped");
                    return path != null ? "Saved " + path : "Stopped, but the file could not be written (see the console log).";

                case "status":
                    if (!recorder.IsRecording) return "Not recording." + (recorder.LastSavedPath != null ? " Last file: " + recorder.LastSavedPath : string.Empty);
                    return "Recording: " + recorder.Tick + " fixed steps, " + recorder.Current.commands.Count + " commands.";

                default:
                    throw new DevCommandException("Unknown '" + verb + "'.");
            }
        }

        internal static string Replay(DevCommandArgs a)
        {
            DevReplayPlayer player = DevReplayPlayer.Instance;
            if (player == null) throw new DevCommandException("No replay player in the scene (add DevTools).");

            string verb = a.Has(0) ? a[0].ToLowerInvariant() : "status";
            switch (verb)
            {
                case "list":
                    List<string> names = DevReplayFiles.List();
                    return names.Count == 0 ? "No replays in " + DevReplayFiles.Directory : string.Join("\n", names);

                case "play":
                    string path = DevReplayFiles.Resolve(a.Has(1) ? a.Rest(1) : "latest");
                    if (path == null) throw new DevCommandException("No such replay. Try: replay list");

                    ReplayRecording recording = DevReplayFiles.Load(path, out string loadError);
                    if (recording == null) throw new DevCommandException(loadError);
                    if (!player.Play(recording, out string message)) throw new DevCommandException(message);
                    return message;

                case "stop":
                    player.Stop();
                    return "Stopped.";

                case "status":
                    return player.Status();

                default:
                    throw new DevCommandException("Unknown '" + verb + "'.");
            }
        }
    }
}
#endif
