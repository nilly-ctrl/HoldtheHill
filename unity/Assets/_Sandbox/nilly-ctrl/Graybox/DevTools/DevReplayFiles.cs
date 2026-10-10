#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>Where replays are kept (<c>persistentDataPath/replays</c>) and how they are named.</summary>
    public static class DevReplayFiles
    {
        public static string Directory => Path.Combine(Application.persistentDataPath, "replays");

        /// <summary>Writes the recording and returns its full path.</summary>
        public static string Save(ReplayRecording recording)
        {
            System.IO.Directory.CreateDirectory(Directory);
            string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            string map = string.IsNullOrEmpty(recording.mapId) ? "run" : Sanitize(recording.mapId);
            string path = Path.Combine(Directory, $"replay-{stamp}-{map}-seed{recording.seed}.json");
            File.WriteAllText(path, ReplaySerializer.ToJson(recording));
            return path;
        }

        /// <summary>Reads a replay file. Returns null and sets <paramref name="error"/> on failure.</summary>
        public static ReplayRecording Load(string path, out string error)
        {
            error = null;
            if (!File.Exists(path))
            {
                error = "No such file: " + path;
                return null;
            }

            return ReplaySerializer.FromJson(File.ReadAllText(path), out error);
        }

        /// <summary>File names in the replays folder, newest first.</summary>
        public static List<string> List()
        {
            var names = new List<string>();
            if (!System.IO.Directory.Exists(Directory)) return names;

            string[] files = System.IO.Directory.GetFiles(Directory, "*.json");
            Array.Sort(files, (a, b) => File.GetLastWriteTimeUtc(b).CompareTo(File.GetLastWriteTimeUtc(a)));
            foreach (string file in files) names.Add(Path.GetFileName(file));
            return names;
        }

        /// <summary>
        /// Turns "latest", a file name (with or without .json), a name fragment or a full path into a
        /// full path. Returns null if nothing matches.
        /// </summary>
        public static string Resolve(string nameOrPath)
        {
            if (string.IsNullOrWhiteSpace(nameOrPath)) return null;
            if (File.Exists(nameOrPath)) return Path.GetFullPath(nameOrPath);

            List<string> names = List();
            if (names.Count == 0) return null;
            if (string.Equals(nameOrPath, "latest", StringComparison.OrdinalIgnoreCase)) return Path.Combine(Directory, names[0]);

            foreach (string name in names)
            {
                if (string.Equals(name, nameOrPath, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(name, nameOrPath + ".json", StringComparison.OrdinalIgnoreCase))
                {
                    return Path.Combine(Directory, name);
                }
            }

            foreach (string name in names)
            {
                if (name.IndexOf(nameOrPath, StringComparison.OrdinalIgnoreCase) >= 0) return Path.Combine(Directory, name);
            }

            return null;
        }

        private static string Sanitize(string text)
        {
            foreach (char bad in Path.GetInvalidFileNameChars()) text = text.Replace(bad, '_');
            return text.Replace(' ', '_');
        }
    }
}
#endif
