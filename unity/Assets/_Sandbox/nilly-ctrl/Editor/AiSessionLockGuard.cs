using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Stops an automated Unity run when a different AI session holds this project's lock.
    /// </summary>
    /// <remarks>
    /// Several AI sessions sometimes work here at once. One rebuilding the graybox or running
    /// tests while another is mid-run has broken runs more than once. Sessions take turns with
    /// <c>B:\Projects\GameDev\Tools\unity-session-lock.sh</c>, which creates
    /// <c>Library/AiSessionLock/owner.txt</c> and passes its session id in the
    /// <c>UNITY_AI_SESSION</c> environment variable. This is the enforcement half: a run started
    /// with -batchmode, -executeMethod or -runTests exits with code 42 unless it owns the lock
    /// or the lock is free.
    ///
    /// It never affects a person using the Editor normally, and it does nothing on a machine
    /// with no lock folder, so teammates are unaffected.
    /// </remarks>
    [InitializeOnLoad]
    internal static class AiSessionLockGuard
    {
        private const string LockFile = "Library/AiSessionLock/owner.txt";
        private const string SessionVariable = "UNITY_AI_SESSION";
        private const double StaleMinutes = 45;
        public const int BlockedExitCode = 42;

        static AiSessionLockGuard()
        {
            if (!IsAutomatedRun() || !File.Exists(LockFile))
            {
                return;
            }

            string holder = Holder(out string purpose);
            string me = Environment.GetEnvironmentVariable(SessionVariable);
            if (!string.IsNullOrEmpty(me) && me == holder)
            {
                return;
            }

            double age = (DateTime.Now - File.GetLastWriteTime(LockFile)).TotalMinutes;
            if (age >= StaleMinutes)
            {
                Debug.LogWarning($"[AiSessionLock] Ignoring a stale lock from session '{holder}' ({age:0} min old).");
                return;
            }

            Debug.LogError(
                $"[AiSessionLock] This project is locked by AI session '{holder}' ({purpose}, {age:0} min ago). " +
                $"This run is '{(string.IsNullOrEmpty(me) ? "unidentified" : me)}', so it is stopping. " +
                "Start Unity through Tools/unity-session-lock.sh, which waits its turn.");
            EditorApplication.Exit(BlockedExitCode);
        }

        private static bool IsAutomatedRun()
        {
            if (Application.isBatchMode)
            {
                return true;
            }

            string[] args = Environment.GetCommandLineArgs();
            return args.Contains("-executeMethod") || args.Contains("-runTests");
        }

        private static string Holder(out string purpose)
        {
            string session = "unknown";
            purpose = "unknown purpose";
            foreach (string line in File.ReadAllLines(LockFile))
            {
                if (line.StartsWith("session=")) session = line.Substring("session=".Length).Trim();
                else if (line.StartsWith("purpose=")) purpose = line.Substring("purpose=".Length).Trim();
            }

            return session;
        }
    }
}
