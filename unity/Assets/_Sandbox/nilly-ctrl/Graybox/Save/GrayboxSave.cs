using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Loads and writes the save file. One file, one <see cref="GrayboxSaveData"/>.
    /// </summary>
    /// <remarks>
    /// Nothing touches the disk until <see cref="Open"/> is called (a <see cref="GrayboxSaveHost"/>
    /// in the scene does it). Before that <see cref="Data"/> is a plain in-memory default and
    /// <see cref="Save"/> does nothing, so tests and scenes without a host never read or overwrite
    /// a player's real save.
    ///
    /// Writes go to a temporary file that then replaces the real one, keeping the previous file
    /// as <c>.bak</c>. A crash mid-write leaves the old save intact, and a save that will not
    /// parse falls back to the backup.
    /// </remarks>
    public static class GrayboxSave
    {
        public const int CurrentVersion = 1;
        public const string FileName = "save.json";

        private static GrayboxSaveData s_data = new GrayboxSaveData();
        private static string s_path;
        private static bool s_dirty;

        /// <summary>Raised after <see cref="Open"/> or <see cref="ResetAll"/> replaces <see cref="Data"/>.</summary>
        public static event Action Loaded;

        /// <summary>The live save data. Never null. Call <see cref="MarkDirty"/> after changing it.</summary>
        public static GrayboxSaveData Data => s_data;

        /// <summary>True once a file is attached, so <see cref="Save"/> writes to disk.</summary>
        public static bool IsOpen => s_path != null;

        /// <summary>The attached file, or null before <see cref="Open"/>.</summary>
        public static string FilePath => s_path;

        public static string DefaultPath => Path.Combine(Application.persistentDataPath, FileName);

        /// <summary>Attaches a save file and loads it. A missing file starts a fresh save.</summary>
        /// <param name="path">File to use; the player's save when left out. Tests pass a temp file.</param>
        public static void Open(string path = null)
        {
            s_path = path ?? DefaultPath;
            s_data = Read(s_path) ?? Read(s_path + ".bak") ?? new GrayboxSaveData();
            Migrate(s_data);
            s_dirty = false;
            Loaded?.Invoke();
        }

        /// <summary>Detaches the file without writing and goes back to in-memory defaults.</summary>
        public static void Close()
        {
            s_path = null;
            s_data = new GrayboxSaveData();
            s_dirty = false;
        }

        /// <summary>Notes that <see cref="Data"/> changed, for the next <see cref="SaveIfDirty"/>.</summary>
        public static void MarkDirty() => s_dirty = true;

        /// <summary>Writes only if something changed since the last write.</summary>
        public static bool SaveIfDirty() => s_dirty && Save();

        /// <summary>Writes the save now. Returns false when no file is attached or the write failed.</summary>
        public static bool Save()
        {
            if (!IsOpen)
            {
                return false;
            }

            try
            {
                string directory = Path.GetDirectoryName(s_path);
                if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

                s_data.version = CurrentVersion;
                string temp = s_path + ".tmp";
                File.WriteAllText(temp, JsonUtility.ToJson(s_data, true));

                if (File.Exists(s_path))
                {
                    File.Replace(temp, s_path, s_path + ".bak");
                }
                else
                {
                    File.Move(temp, s_path);
                }

                s_dirty = false;
                return true;
            }
            catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogError($"[GrayboxSave] Could not write '{s_path}': {e.Message}");
                return false;
            }
        }

        /// <summary>Wipes all progress and settings and writes the empty save.</summary>
        public static void ResetAll()
        {
            s_data = new GrayboxSaveData();
            s_dirty = true;
            Save();
            Loaded?.Invoke();
        }

        private static GrayboxSaveData Read(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                var data = JsonUtility.FromJson<GrayboxSaveData>(File.ReadAllText(path));
                if (data == null)
                {
                    throw new ArgumentException("file is empty");
                }

                if (data.version > CurrentVersion)
                {
                    // Written by a newer build. Saving from this one would drop what it does not
                    // know about, so keep a copy first.
                    File.Copy(path, path + $".v{data.version}", true);
                    Debug.LogWarning($"[GrayboxSave] '{path}' is version {data.version}, newer than this build ({CurrentVersion}). A copy was kept.");
                }

                return data;
            }
            catch (Exception e) when (e is ArgumentException || e is IOException || e is UnauthorizedAccessException)
            {
                Debug.LogWarning($"[GrayboxSave] '{path}' could not be read ({e.Message}). Kept as '.corrupt'.");
                try
                {
                    File.Copy(path, path + ".corrupt", true);
                }
                catch (IOException)
                {
                    // Best effort: the unreadable file is still where it was.
                }

                return null;
            }
        }

        // Brings an older file up to CurrentVersion. Version 1 is the first, so there are no
        // steps yet; add "if (data.version < 2) { ... }" blocks here in order.
        private static void Migrate(GrayboxSaveData data)
        {
            // A hand-edited file can null these out; the rest of the game assumes they exist.
            data.settings ??= new SettingsSave();
            data.records ??= new RecordsSave();
            data.records.levels ??= new List<LevelRecord>();
            data.achievements ??= new List<AchievementSave>();
            data.purchasedUpgrades ??= new List<string>();
            data.bindingOverrides ??= string.Empty;

            data.version = CurrentVersion;
        }
    }
}
