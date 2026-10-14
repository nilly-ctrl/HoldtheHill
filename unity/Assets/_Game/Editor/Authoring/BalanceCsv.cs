using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HoldTheHill.EditorTools.Authoring
{
    /// <summary>Minimal RFC 4180 CSV reading and writing (quoted cells, doubled quotes, newlines in cells).</summary>
    internal static class BalanceCsv
    {
        public static void Write(string path, IEnumerable<string[]> rows)
        {
            var sb = new StringBuilder();
            foreach (string[] row in rows)
            {
                for (int i = 0; i < row.Length; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(',');
                    }

                    sb.Append(Escape(row[i] ?? string.Empty));
                }

                sb.Append("\r\n");
            }

            // UTF-8 with a BOM so Excel and Sheets read non-ASCII names correctly.
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
        }

        public static List<string[]> Read(string path)
        {
            string text = File.ReadAllText(path, Encoding.UTF8);
            if (text.Length > 0 && text[0] == '﻿')
            {
                text = text.Substring(1);
            }

            var rows = new List<string[]>();
            var row = new List<string>();
            var cell = new StringBuilder();
            bool quoted = false;
            bool any = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (quoted)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            cell.Append('"');
                            i++;
                        }
                        else
                        {
                            quoted = false;
                        }
                    }
                    else
                    {
                        cell.Append(c);
                    }

                    continue;
                }

                switch (c)
                {
                    case '"':
                        quoted = true;
                        any = true;
                        break;
                    case ',':
                        row.Add(cell.ToString());
                        cell.Length = 0;
                        any = true;
                        break;
                    case '\r':
                        break;
                    case '\n':
                        if (any || cell.Length > 0)
                        {
                            row.Add(cell.ToString());
                            rows.Add(row.ToArray());
                        }

                        row.Clear();
                        cell.Length = 0;
                        any = false;
                        break;
                    default:
                        cell.Append(c);
                        any = true;
                        break;
                }
            }

            if (any || cell.Length > 0)
            {
                row.Add(cell.ToString());
                rows.Add(row.ToArray());
            }

            return rows;
        }

        private static string Escape(string value)
        {
            if (value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0)
            {
                return value;
            }

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
