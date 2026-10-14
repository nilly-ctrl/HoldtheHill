using HoldTheHill.Core.Localization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace HoldTheHill.EditorTools.Pipeline
{
    /// <summary>
    /// Scanning and file helpers behind the Localization Tools window: find hard-coded UI strings in
    /// sandbox code and assets, find keys used through <c>Loc.Get("...")</c> that the table
    /// lacks, and read or write the table's CSV. CSV parsing and diffing are in
    /// <see cref="StringTableCsv"/> (runtime assembly, unit tested).
    /// </summary>
    public static class LocalizationTools
    {
        public const int MaxResults = 5000;

        public readonly struct Finding
        {
            public readonly string Path;   // project-relative, forward slashes
            public readonly int Line;      // 1-based
            public readonly string Text;

            public Finding(string path, int line, string text)
            {
                Path = path;
                Line = line;
                Text = text;
            }
        }

        private static readonly Regex StringLiteral = new Regex("\"((?:[^\"\\\\]|\\\\.)*)\"", RegexOptions.Compiled);
        private static readonly Regex KeyUsage = new Regex("Loc\\s*\\.\\s*Get\\s*\\(\\s*\"((?:[^\"\\\\]|\\\\.)*)\"", RegexOptions.Compiled);
        private static readonly Regex AssetTextLine = new Regex(
            "^\\s*(?:-\\s*)?(?:m_Text|m_text|text|label|title|description|displayName|_displayName|_description|_title|_label|_text):\\s*(.+?)\\s*$",
            RegexOptions.Compiled);

        // Lines that contain these are code, logs or editor chrome, not text a player reads.
        private static readonly string[] SkipLineMarkers =
        {
            "Debug.Log", "[MenuItem", "[Header", "[Tooltip", "[CreateAssetMenu", "[AddComponentMenu", "using ",
            "nameof(", "Loc.Get(", "throw new", "Assert", "PlayerPrefs", "new GameObject(", "Find(",
            "GetComponent", "LoadAsset", "EditorGUI", "[FormerlySerializedAs", "#region", "#if", "Shader.",
            "SetFloat(", "SetInt(", "SetColor(", "SetTexture(", "GetString(", "SetString(", "Path.Combine",
        };

        public static string SandboxRoot => Path.Combine(Application.dataPath, "_Sandbox", "nilly-ctrl");

        // ---- hard-coded strings ----

        /// <summary>Heuristic list of UI-looking string literals in sandbox C# files (not Editor, Tests or Localization).</summary>
        public static List<Finding> ScanCode(string root = null)
        {
            var results = new List<Finding>();
            foreach (string file in EnumerateFiles(root ?? SandboxRoot, "*.cs", skipEditorAndTests: true))
            {
                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    string trimmed = line.TrimStart();
                    if (trimmed.StartsWith("//") || trimmed.StartsWith("*") || trimmed.StartsWith("/*") || ContainsAny(line, SkipLineMarkers))
                    {
                        continue;
                    }

                    foreach (Match match in StringLiteral.Matches(line))
                    {
                        string text = match.Groups[1].Value;
                        if (IsLikelyUiText(text))
                        {
                            results.Add(new Finding(ToProjectPath(file), i + 1, text));
                            if (results.Count >= MaxResults) return results;
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>Text-like fields in prefabs, scenes and data assets (m_Text, label, title, description...).</summary>
        public static List<Finding> ScanAssets(string root = null)
        {
            var results = new List<Finding>();
            foreach (string pattern in new[] { "*.prefab", "*.asset", "*.unity" })
            {
                foreach (string file in EnumerateFiles(root ?? SandboxRoot, pattern, skipEditorAndTests: false))
                {
                    if (new FileInfo(file).Length > 8 * 1024 * 1024) continue; // generated atlases and baked data
                    string[] lines;
                    try
                    {
                        lines = File.ReadAllLines(file);
                    }
                    catch (IOException)
                    {
                        continue;
                    }

                    for (int i = 0; i < lines.Length; i++)
                    {
                        Match match = AssetTextLine.Match(lines[i]);
                        if (!match.Success) continue;
                        string text = Unquote(match.Groups[1].Value);
                        if (IsLikelyAssetText(text))
                        {
                            results.Add(new Finding(ToProjectPath(file), i + 1, text));
                            if (results.Count >= MaxResults) return results;
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>True for strings a player would read: words with a space, or one capitalised word.</summary>
        public static bool IsLikelyUiText(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length < 3) return false;
            if (text.IndexOfAny(new[] { '/', '\\', '_' }) >= 0) return false;        // paths, ids
            if (Regex.IsMatch(text, "^[\\w.-]+\\.(png|asset|prefab|json|txt|csv|unity|mat|ttf)$", RegexOptions.IgnoreCase)) return false;
            if (Regex.IsMatch(text, "^\\{\\d+(:[^}]*)?\\}$")) return false;           // a bare format placeholder
            int letters = 0;
            foreach (char c in text) if (char.IsLetter(c)) letters++;
            if (letters < 2) return false;

            if (text.IndexOf(' ') >= 0) return true;                                   // a phrase
            // A single word: must start upper case and not look like PascalCase / camelCase / dotted ids.
            if (text.IndexOf('.') >= 0 && !text.EndsWith(".") && !text.EndsWith("...")) return false;
            if (!char.IsUpper(text[0])) return false;
            for (int i = 1; i < text.Length; i++)
            {
                if (char.IsUpper(text[i]) && char.IsLower(text[i - 1])) return false;
            }

            return true;
        }

        private static bool IsLikelyAssetText(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Length < 2) return false;
            if (text.StartsWith("{") || text.StartsWith("[") || text == "~") return false;
            if (Regex.IsMatch(text, "^[0-9a-fA-F]{32}$")) return false;
            if (Regex.IsMatch(text, "^[-+0-9.eE]+$")) return false;
            foreach (char c in text) if (char.IsLetter(c)) return true;
            return false;
        }

        // ---- keys used in code ----

        /// <summary>Every <c>Loc.Get("key")</c> with a literal key, in sandbox code outside Tests and Localization.</summary>
        public static List<Finding> FindKeyUsages(string root = null)
        {
            var results = new List<Finding>();
            foreach (string file in EnumerateFiles(root ?? SandboxRoot, "*.cs", skipEditorAndTests: false))
            {
                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].TrimStart().StartsWith("//")) continue;
                    foreach (Match match in KeyUsage.Matches(lines[i]))
                    {
                        string key = match.Groups[1].Value.Replace("\\\"", "\"");
                        if (key.Length > 0) results.Add(new Finding(ToProjectPath(file), i + 1, key));
                    }
                }
            }

            return results;
        }

        /// <summary>Usages whose key is not in the table (any language).</summary>
        public static List<Finding> MissingKeys(StringTable table, IEnumerable<Finding> usages)
        {
            var missing = new List<Finding>();
            foreach (Finding usage in usages)
            {
                if (table == null || !table.HasKey(usage.Text)) missing.Add(usage);
            }

            return missing;
        }

        // ---- CSV files ----

        public static void ExportCsv(StringTable table, string path)
        {
            File.WriteAllText(path, StringTableCsv.Export(table), new UTF8Encoding(true));
        }

        public static List<StringChange> PreviewImport(StringTable table, string path, out List<string> languages)
        {
            string csv = File.ReadAllText(path, Encoding.UTF8);
            List<StringEntry> incoming = StringTableCsv.Parse(csv, out languages);
            return StringTableCsv.Diff(table, incoming);
        }

        // ---- helpers ----

        private static IEnumerable<string> EnumerateFiles(string root, string pattern, bool skipEditorAndTests)
        {
            if (!Directory.Exists(root)) yield break;
            var stack = new Stack<string>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                string dir = stack.Pop();
                foreach (string file in Directory.GetFiles(dir, pattern))
                {
                    yield return file;
                }

                foreach (string sub in Directory.GetDirectories(dir))
                {
                    string name = Path.GetFileName(sub);
                    if (name.EndsWith("~") || name == "Library" || name == "Localization") continue; // Unity ignores ~ folders
                    if (name == "Tests") continue;
                    if (skipEditorAndTests && name == "Editor") continue;
                    stack.Push(sub);
                }
            }
        }

        private static string ToProjectPath(string fullPath)
        {
            string assets = Application.dataPath.Replace('\\', '/');
            string path = fullPath.Replace('\\', '/');
            return path.StartsWith(assets, StringComparison.OrdinalIgnoreCase) ? "Assets" + path.Substring(assets.Length) : path;
        }

        private static string Unquote(string value)
        {
            if (value.Length >= 2 && value[0] == '\'' && value[value.Length - 1] == '\'')
            {
                return value.Substring(1, value.Length - 2).Replace("''", "'");
            }

            if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
            {
                return value.Substring(1, value.Length - 2).Replace("\\\"", "\"");
            }

            return value;
        }

        private static bool ContainsAny(string line, string[] markers)
        {
            foreach (string marker in markers)
            {
                if (line.IndexOf(marker, StringComparison.Ordinal) >= 0) return true;
            }

            return false;
        }
    }
}
