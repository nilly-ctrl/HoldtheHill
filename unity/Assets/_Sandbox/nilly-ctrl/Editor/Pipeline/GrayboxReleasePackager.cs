using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEditor;
using Debug = UnityEngine.Debug;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Builds the Windows graybox player with the existing <see cref="GrayboxPlayerBuild"/>, then
    /// stamps a version, writes a changelog from <c>git log</c> and zips the folder.
    /// Menu: Tools > Hold the Hill > Pipeline > Build Release Package (Windows).
    /// Batch: <c>-executeMethod HoldTheHill.Sandbox.NillyCtrl.GrayboxReleasePackager.Build</c>
    /// (use <c>Pipeline~/build-release.ps1</c>, which goes through the Unity session lock).
    /// </summary>
    /// <remarks>
    /// Output: <c>&lt;repo&gt;/Builds~/HoldTheHill-Graybox-&lt;version&gt;/</c> (game, CHANGELOG.txt, VERSION.txt)
    /// and the same folder zipped beside it. Pass <c>-hthReleaseDir &lt;folder&gt;</c> to change the parent.
    /// The version is the <c>VERSION</c> file in <c>Pipeline~/</c> plus the git short hash; with no
    /// file it is <c>git describe --tags --always</c>. <c>PlayerSettings.bundleVersion</c> is set for
    /// the build and put back afterwards so ProjectSettings is left unchanged.
    /// </remarks>
    public static class GrayboxReleasePackager
    {
        private const string VersionFileRelative = "Assets/_Sandbox/nilly-ctrl/Pipeline~/VERSION";
        private const int MaxChangelogCommits = 300;

        [MenuItem("Tools/Hold the Hill/Pipeline/Build Release Package (Windows)")]
        public static void BuildFromMenu()
        {
            if (!EditorUtility.DisplayDialog("Build release package",
                    "Builds the Windows player (a few minutes), writes a changelog and zips it into Builds~/.\n\nContinue?",
                    "Build", "Cancel"))
            {
                return;
            }

            Build();
        }

        /// <summary>Entry point for batch mode and the menu.</summary>
        public static void Build()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, ".."));
            string repoRoot = Git(projectRoot, "rev-parse --show-toplevel");
            if (string.IsNullOrEmpty(repoRoot)) repoRoot = Path.GetFullPath(Path.Combine(projectRoot, ".."));

            string shortHash = Git(projectRoot, "rev-parse --short HEAD");
            string version = ComposeVersion(ReadVersionFile(projectRoot), Git(projectRoot, "describe --tags --always --dirty"), shortHash);

            string releaseParent = Path.Combine(repoRoot, "Builds~");
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-hthReleaseDir") releaseParent = args[i + 1];
            }

            string name = "HoldTheHill-Graybox-" + SanitizeForFileName(version);
            string stage = Path.Combine(releaseParent, name);
            string zipPath = stage + ".zip";

            // Build with the existing method. It writes to -hthBuildDir if given, else <project>/Builds/Graybox.
            string builtFolder = GrayboxPlayerBuild.OutputFolder;
            string exe = GrayboxPlayerBuild.ExePath;
            string previousVersion = PlayerSettings.bundleVersion;
            bool built;
            try
            {
                PlayerSettings.bundleVersion = BundleVersionFrom(version);
                built = GrayboxPlayerBuild.TryBuild();
            }
            finally
            {
                PlayerSettings.bundleVersion = previousVersion;
            }

            if (!built || !File.Exists(exe))
            {
                Fail($"[Release] The player build did not succeed (expected {exe}).");
                return;
            }

            try
            {
                if (Directory.Exists(stage)) Directory.Delete(stage, true);
                Directory.CreateDirectory(releaseParent);
                if (Path.GetFullPath(builtFolder) != Path.GetFullPath(stage))
                {
                    CopyDirectory(builtFolder, stage);
                }

                File.WriteAllText(Path.Combine(stage, "VERSION.txt"), version + Environment.NewLine);
                File.WriteAllText(Path.Combine(stage, "CHANGELOG.txt"), BuildChangelog(projectRoot, version, shortHash));

                if (File.Exists(zipPath)) File.Delete(zipPath);
                ZipFile.CreateFromDirectory(stage, zipPath, System.IO.Compression.CompressionLevel.Optimal, true);
                long mb = new FileInfo(zipPath).Length / (1024 * 1024);
                Debug.Log($"[Release] {version}: {zipPath} ({mb} MB). Folder: {stage}");
            }
            catch (Exception e)
            {
                Fail("[Release] Packaging failed: " + e);
            }
        }

        // ---- version ----

        /// <summary>"0.3.0" + "a1b2c3d" -> "0.3.0+a1b2c3d"; no file -> the git describe text.</summary>
        internal static string ComposeVersion(string fileVersion, string describe, string shortHash)
        {
            if (!string.IsNullOrWhiteSpace(fileVersion))
            {
                string v = fileVersion.Trim();
                return string.IsNullOrEmpty(shortHash) ? v : v + "+" + shortHash;
            }

            if (!string.IsNullOrWhiteSpace(describe)) return describe.Trim();
            return "0.0.0-unknown";
        }

        /// <summary>PlayerSettings wants something dotted-numeric: keep the part before '+' or '-'.</summary>
        internal static string BundleVersionFrom(string version)
        {
            int cut = version.IndexOfAny(new[] { '+', '-' });
            string head = cut > 0 ? version.Substring(0, cut) : version;
            return string.IsNullOrEmpty(head) ? "0.0.0" : head.TrimStart('v');
        }

        private static string ReadVersionFile(string projectRoot)
        {
            string path = Path.Combine(projectRoot, VersionFileRelative);
            return File.Exists(path) ? File.ReadAllText(path).Trim() : null;
        }

        private static string SanitizeForFileName(string text)
        {
            foreach (char c in Path.GetInvalidFileNameChars()) text = text.Replace(c, '_');
            return text.Replace(' ', '_');
        }

        // ---- changelog ----

        private static string BuildChangelog(string workDir, string version, string shortHash)
        {
            string lastTag = Git(workDir, "describe --tags --abbrev=0");
            string range = string.IsNullOrEmpty(lastTag) ? string.Empty : lastTag + "..HEAD";
            string log = Git(workDir,
                $"log {range} --no-merges --max-count={MaxChangelogCommits} --date=short --pretty=format:\"- %ad %h %s (%an)\"");

            var sb = new StringBuilder();
            sb.AppendLine("Hold the Hill (graybox) " + version);
            sb.AppendLine("Built " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
            sb.AppendLine("Commit " + shortHash + " on " + Git(workDir, "rev-parse --abbrev-ref HEAD"));
            sb.AppendLine(string.IsNullOrEmpty(lastTag)
                ? $"No tag yet: the last {MaxChangelogCommits} commits at most."
                : "Changes since " + lastTag);
            sb.AppendLine();
            sb.AppendLine(string.IsNullOrWhiteSpace(log) ? "(no commits in range)" : log);
            return sb.ToString();
        }

        // ---- helpers ----

        /// <summary>Runs git and returns trimmed stdout, or null when git is missing or exits non-zero.</summary>
        private static string Git(string workDir, string arguments)
        {
            try
            {
                var info = new ProcessStartInfo("git", arguments)
                {
                    WorkingDirectory = workDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                };
                using (Process process = Process.Start(info))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.StandardError.ReadToEnd();
                    process.WaitForExit(30000);
                    return process.ExitCode == 0 ? output.Trim() : null;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void CopyDirectory(string source, string destination)
        {
            Directory.CreateDirectory(destination);
            foreach (string file in Directory.GetFiles(source))
            {
                File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
            }

            foreach (string dir in Directory.GetDirectories(source))
            {
                CopyDirectory(dir, Path.Combine(destination, Path.GetFileName(dir)));
            }
        }

        private static void Fail(string message)
        {
            Debug.LogError(message);
            if (UnityEngine.Application.isBatchMode) EditorApplication.Exit(1);
        }
    }
}
