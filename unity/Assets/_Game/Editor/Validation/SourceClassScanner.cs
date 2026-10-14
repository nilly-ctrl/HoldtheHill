using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace HoldTheHill.EditorTools.Validation
{
    /// <summary>
    /// Finds class declarations in C# source text. A light scanner, not a compiler: it blanks
    /// comments and strings first so a word in a comment cannot look like a class. Pure C#.
    /// </summary>
    public static class SourceClassScanner
    {
        private static readonly Regex ClassPattern = new Regex(
            @"(?<mods>(?:\b(?:public|internal|private|protected|abstract|sealed|static|partial|unsafe|new)\s+)*)" +
            @"\bclass\s+(?<name>[A-Za-z_]\w*)\s*(?:<[^>{]*>)?\s*(?::\s*(?<bases>[^{;]+?))?\s*(?:\bwhere\b[^{;]*)?\{",
            RegexOptions.Compiled);

        /// <summary>The source with comments, string literals and char literals replaced by spaces (newlines kept).</summary>
        public static string Blank(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(source.Length);
            int i = 0;
            int n = source.Length;
            while (i < n)
            {
                char c = source[i];
                char next = i + 1 < n ? source[i + 1] : '\0';

                if (c == '/' && next == '/')
                {
                    while (i < n && source[i] != '\n')
                    {
                        sb.Append(' ');
                        i++;
                    }
                }
                else if (c == '/' && next == '*')
                {
                    sb.Append("  ");
                    i += 2;
                    while (i < n && !(source[i] == '*' && i + 1 < n && source[i + 1] == '/'))
                    {
                        sb.Append(source[i] == '\n' ? '\n' : ' ');
                        i++;
                    }

                    if (i < n)
                    {
                        sb.Append("  ");
                        i += 2;
                    }
                }
                else if (c == '@' && next == '"')
                {
                    sb.Append("  ");
                    i += 2;
                    while (i < n)
                    {
                        if (source[i] == '"' && i + 1 < n && source[i + 1] == '"')
                        {
                            sb.Append("  ");
                            i += 2;
                            continue;
                        }

                        if (source[i] == '"')
                        {
                            sb.Append(' ');
                            i++;
                            break;
                        }

                        sb.Append(source[i] == '\n' ? '\n' : ' ');
                        i++;
                    }
                }
                else if (c == '"' || c == '\'')
                {
                    char quote = c;
                    sb.Append(' ');
                    i++;
                    while (i < n && source[i] != quote && source[i] != '\n')
                    {
                        if (source[i] == '\\' && i + 1 < n)
                        {
                            sb.Append("  ");
                            i += 2;
                            continue;
                        }

                        sb.Append(' ');
                        i++;
                    }

                    if (i < n && source[i] == quote)
                    {
                        sb.Append(' ');
                        i++;
                    }
                }
                else
                {
                    sb.Append(c);
                    i++;
                }
            }

            return sb.ToString();
        }

        /// <summary>Every class declared in the source, nested ones included, in file order.</summary>
        public static List<DeclaredClass> Scan(string source)
        {
            var classes = new List<DeclaredClass>();
            string clean = Blank(source);

            foreach (Match m in ClassPattern.Matches(clean))
            {
                bool isAbstract = m.Groups["mods"].Value.IndexOf("abstract", StringComparison.Ordinal) >= 0;
                var bases = new List<string>();

                if (m.Groups["bases"].Success)
                {
                    foreach (string part in SplitTopLevel(m.Groups["bases"].Value))
                    {
                        string name = part.Trim();
                        int lt = name.IndexOf('<');
                        if (lt >= 0)
                        {
                            name = name.Substring(0, lt);
                        }

                        int dot = name.LastIndexOf('.');
                        if (dot >= 0)
                        {
                            name = name.Substring(dot + 1);
                        }

                        if (name.Length > 0)
                        {
                            bases.Add(name);
                        }
                    }
                }

                classes.Add(new DeclaredClass(m.Groups["name"].Value, isAbstract, bases));
            }

            return classes;
        }

        /// <summary>
        /// Names of the concrete classes in the source that are MonoBehaviours. <paramref name="isMonoBehaviour"/>
        /// answers for a class name (use the loaded types in the Editor); with null, only a class that
        /// names MonoBehaviour as its direct base counts.
        /// </summary>
        public static List<string> MonoBehaviourNames(string source, Func<string, bool> isMonoBehaviour)
        {
            var names = new List<string>();
            foreach (DeclaredClass declared in Scan(source))
            {
                if (declared.IsAbstract)
                {
                    continue;
                }

                bool mono = isMonoBehaviour != null
                    ? isMonoBehaviour(declared.Name)
                    : declared.HasBase("MonoBehaviour");

                if (mono)
                {
                    names.Add(declared.Name);
                }
            }

            return names;
        }

        // Splits "A, B<C, D>, E" on commas that are not inside angle brackets.
        private static IEnumerable<string> SplitTopLevel(string text)
        {
            int depth = 0;
            int start = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '<')
                {
                    depth++;
                }
                else if (c == '>')
                {
                    depth--;
                }
                else if (c == ',' && depth == 0)
                {
                    yield return text.Substring(start, i - start);
                    start = i + 1;
                }
            }

            yield return text.Substring(start);
        }
    }
}
