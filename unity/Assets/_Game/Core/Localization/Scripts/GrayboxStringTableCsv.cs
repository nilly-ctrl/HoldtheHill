using System;
using System.Collections.Generic;
using System.Text;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// CSV export, import and diff for <see cref="GrayboxStringTable"/>. Pure logic with no file or
    /// editor access, so it is covered by EditMode tests.
    /// </summary>
    /// <remarks>
    /// Layout: header <c>key,en,fr,...</c>, then one row per key. Fields are quoted per RFC 4180 when
    /// they hold a comma, quote or line break. Importing only adds or changes cells: an empty cell
    /// and a key absent from the CSV leave the table alone, so a half-translated sheet is safe.
    /// </remarks>
    public static class GrayboxStringTableCsv
    {
        public const string KeyColumn = "key";

        public static string Export(GrayboxStringTable table)
        {
            var languages = new List<string>(table.Languages);
            // Include languages that only appear in entries, so nothing is lost on export.
            foreach (GrayboxStringEntry entry in table.Entries)
            {
                if (entry == null) continue;
                foreach (GrayboxLocalizedText t in entry.texts)
                {
                    if (t != null && !string.IsNullOrEmpty(t.language) && !languages.Contains(t.language))
                    {
                        languages.Add(t.language);
                    }
                }
            }

            var sb = new StringBuilder();
            sb.Append(KeyColumn);
            foreach (string language in languages)
            {
                sb.Append(',').Append(Escape(language));
            }

            sb.Append("\r\n");
            foreach (GrayboxStringEntry entry in table.Entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.key)) continue;
                sb.Append(Escape(entry.key));
                foreach (string language in languages)
                {
                    sb.Append(',').Append(Escape(entry.GetText(language) ?? string.Empty));
                }

                sb.Append("\r\n");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Reads CSV text into entries. Throws <see cref="FormatException"/> when the header has no
        /// <c>key</c> column.
        /// </summary>
        public static List<GrayboxStringEntry> Parse(string csv, out List<string> languages)
        {
            List<List<string>> rows = ReadRows(csv ?? string.Empty);
            languages = new List<string>();
            var entries = new List<GrayboxStringEntry>();
            if (rows.Count == 0)
            {
                return entries;
            }

            List<string> header = rows[0];
            int keyIndex = header.FindIndex(h => string.Equals(h.Trim(), KeyColumn, StringComparison.OrdinalIgnoreCase));
            if (keyIndex < 0)
            {
                throw new FormatException("CSV header has no 'key' column.");
            }

            for (int c = 0; c < header.Count; c++)
            {
                string name = header[c].Trim();
                if (c != keyIndex && name.Length > 0 && !languages.Contains(name))
                {
                    languages.Add(name);
                }
            }

            for (int r = 1; r < rows.Count; r++)
            {
                List<string> row = rows[r];
                if (keyIndex >= row.Count || string.IsNullOrWhiteSpace(row[keyIndex]))
                {
                    continue;
                }

                var entry = new GrayboxStringEntry(row[keyIndex].Trim());
                for (int c = 0; c < header.Count && c < row.Count; c++)
                {
                    string language = header[c].Trim();
                    if (c == keyIndex || language.Length == 0 || row[c].Length == 0)
                    {
                        continue;
                    }

                    entry.SetText(language, row[c]);
                }

                entries.Add(entry);
            }

            return entries;
        }

        /// <summary>The cell changes that importing these entries would make. Does not modify the table.</summary>
        public static List<GrayboxStringChange> Diff(GrayboxStringTable table, IEnumerable<GrayboxStringEntry> incoming)
        {
            var changes = new List<GrayboxStringChange>();
            foreach (GrayboxStringEntry entry in incoming)
            {
                foreach (GrayboxLocalizedText t in entry.texts)
                {
                    if (string.IsNullOrEmpty(t.text)) continue;
                    table.TryGetExact(entry.key, t.language, out string current);
                    if (current == null)
                    {
                        changes.Add(new GrayboxStringChange(GrayboxStringChange.ChangeKind.Added, entry.key, t.language, null, t.text));
                    }
                    else if (current != t.text)
                    {
                        changes.Add(new GrayboxStringChange(GrayboxStringChange.ChangeKind.Changed, entry.key, t.language, current, t.text));
                    }
                }
            }

            return changes;
        }

        public static void Apply(GrayboxStringTable table, IEnumerable<GrayboxStringChange> changes)
        {
            foreach (GrayboxStringChange change in changes)
            {
                table.Set(change.Key, change.Language, change.NewText);
            }
        }

        private static string Escape(string field)
        {
            if (field.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
            {
                return field;
            }

            return "\"" + field.Replace("\"", "\"\"") + "\"";
        }

        private static List<List<string>> ReadRows(string text)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var field = new StringBuilder();
            bool inQuotes = false;
            bool fieldStarted = false;

            int i = 0;
            if (text.Length > 0 && text[0] == '﻿') i = 1; // byte order mark from spreadsheet exports

            for (; i < text.Length; i++)
            {
                char ch = text[i];
                if (inQuotes)
                {
                    if (ch == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            field.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        field.Append(ch);
                    }

                    continue;
                }

                if (ch == '"')
                {
                    inQuotes = true;
                    fieldStarted = true;
                }
                else if (ch == ',')
                {
                    row.Add(field.ToString());
                    field.Length = 0;
                    fieldStarted = true;
                }
                else if (ch == '\r' || ch == '\n')
                {
                    if (ch == '\r' && i + 1 < text.Length && text[i + 1] == '\n') i++;
                    if (fieldStarted || field.Length > 0 || row.Count > 0)
                    {
                        row.Add(field.ToString());
                        rows.Add(row);
                    }

                    row = new List<string>();
                    field.Length = 0;
                    fieldStarted = false;
                }
                else
                {
                    field.Append(ch);
                    fieldStarted = true;
                }
            }

            if (fieldStarted || field.Length > 0 || row.Count > 0)
            {
                row.Add(field.ToString());
                rows.Add(row);
            }

            return rows;
        }
    }
}
