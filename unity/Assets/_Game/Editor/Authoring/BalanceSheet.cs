using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using HoldTheHill.Features.Enemies;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HoldTheHill.EditorTools.Authoring
{
    /// <summary>
    /// What goes into the balance CSVs and how it is read back: tower, enemy/boss and power-up
    /// numbers, taken from and written to the data assets and prefabs through SerializedObject.
    /// </summary>
    /// <remarks>
    /// A row is one asset (its project path is the key). A blank cell on import means "leave as
    /// it is". Import is always two steps: <see cref="Compare"/> builds a list of differences
    /// without touching anything, <see cref="Apply"/> writes them (with undo).
    /// </remarks>
    internal static class BalanceSheet
    {
        internal const string SandboxRoot = "Assets/_Sandbox/nilly-ctrl";
        internal const string EnemyPrefabFolder = SandboxRoot + "/Graybox/Prefabs";

        public static string DefaultFolder =>
            Path.GetFullPath(Path.Combine(Application.dataPath, "_Sandbox/nilly-ctrl/Balance~"));

        public sealed class Field
        {
            public string Column;
            public string Property;
            public bool ReadOnly;
            public Func<Object, Object> Target;
            public Func<Object, string> Computed;
        }

        public sealed class Kind
        {
            public string Name;
            public string FileName;
            public string Warning;
            public Func<List<Object>> Roots;
            public Func<Object, bool> Accepts;
            public List<Field> Fields = new List<Field>();
        }

        public sealed class Diff
        {
            public string Key;
            public string Column;
            public string Old;
            public string New;
            public string Error;
            public Object Target;
            public string Property;

            public bool Applicable => Error == null;
        }

        public sealed class Preview
        {
            public Kind Kind;
            public string File;
            public readonly List<Diff> Diffs = new List<Diff>();
            public readonly List<string> Problems = new List<string>();
            public int Rows;
            public int Unchanged;

            public int ApplicableCount
            {
                get
                {
                    int n = 0;
                    foreach (Diff d in Diffs)
                    {
                        if (d.Applicable)
                        {
                            n++;
                        }
                    }

                    return n;
                }
            }
        }

        public static readonly Kind Towers = BalanceSheetKinds.MakeTowers();
        public static readonly Kind Enemies = BalanceSheetKinds.MakeEnemies();
        public static readonly Kind PowerUps = BalanceSheetKinds.MakePowerUps();
        public static readonly Kind[] All = { Towers, Enemies, PowerUps };

        // ---------- definitions ----------

        // ---------- export ----------

        public static string Export(Kind kind, string folder)
        {
            Directory.CreateDirectory(folder);
            var rows = new List<string[]>();

            var header = new List<string> { "asset", "name" };
            foreach (Field f in kind.Fields)
            {
                header.Add(f.Column);
            }

            rows.Add(header.ToArray());

            foreach (Object root in kind.Roots())
            {
                var row = new List<string> { AssetDatabase.GetAssetPath(root), root.name };
                foreach (Field f in kind.Fields)
                {
                    row.Add(BalanceSheetCells.CellFor(f, root));
                }

                rows.Add(row.ToArray());
            }

            string path = Path.Combine(folder, kind.FileName);
            BalanceCsv.Write(path, rows);
            return path;
        }

        // ---------- import ----------

        /// <summary>Reads the CSV and lists what would change. Changes nothing.</summary>
        public static Preview Compare(Kind kind, string csvPath)
        {
            var preview = new Preview { Kind = kind, File = csvPath };
            List<string[]> rows;
            try
            {
                rows = BalanceCsv.Read(csvPath);
            }
            catch (Exception e)
            {
                preview.Problems.Add($"Cannot read the file: {e.Message}");
                return preview;
            }

            if (rows.Count < 2)
            {
                preview.Problems.Add("The file has no data rows.");
                return preview;
            }

            string[] header = rows[0];
            var columns = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < header.Length; i++)
            {
                columns[header[i].Trim()] = i;
            }

            if (!columns.TryGetValue("asset", out int assetColumn))
            {
                preview.Problems.Add("There is no 'asset' column, so rows cannot be matched to assets.");
                return preview;
            }

            var known = new HashSet<string> { "asset", "name" };
            foreach (Field f in kind.Fields)
            {
                known.Add(f.Column);
            }

            foreach (string column in columns.Keys)
            {
                if (!known.Contains(column))
                {
                    preview.Problems.Add($"Column '{column}' is not one of the {kind.Name} columns and is ignored.");
                }
            }

            var serialized = new Dictionary<Object, SerializedObject>();
            var seen = new Dictionary<(Object Target, string Property), string>();

            for (int r = 1; r < rows.Count; r++)
            {
                string[] row = rows[r];
                string key = assetColumn < row.Length ? row[assetColumn].Trim() : string.Empty;
                preview.Rows++;
                if (key.Length == 0)
                {
                    preview.Problems.Add($"Row {r + 1}: empty asset path.");
                    continue;
                }

                Object root = AssetDatabase.LoadMainAssetAtPath(key);
                if (root == null)
                {
                    preview.Problems.Add($"Row {r + 1}: no asset at {key}.");
                    continue;
                }

                if (!kind.Accepts(root))
                {
                    preview.Problems.Add($"Row {r + 1}: {key} is not a {kind.Name} asset.");
                    continue;
                }

                foreach (Field field in kind.Fields)
                {
                    if (field.ReadOnly || field.Computed != null || !columns.TryGetValue(field.Column, out int at)
                        || at >= row.Length)
                    {
                        continue;
                    }

                    string text = row[at].Trim();
                    if (text.Length == 0)
                    {
                        continue;
                    }

                    Object target = field.Target(root);
                    if (target == null)
                    {
                        preview.Diffs.Add(new Diff
                        {
                            Key = key, Column = field.Column, Old = string.Empty, New = text,
                            Error = "this asset has no such part",
                        });
                        continue;
                    }

                    if (!serialized.TryGetValue(target, out SerializedObject so))
                    {
                        so = new SerializedObject(target);
                        serialized[target] = so;
                    }

                    SerializedProperty p = so.FindProperty(field.Property);
                    if (p == null)
                    {
                        preview.Diffs.Add(new Diff
                        {
                            Key = key, Column = field.Column, Old = string.Empty, New = text,
                            Error = $"property {field.Property} not found",
                        });
                        continue;
                    }

                    string old = BalanceSheetCells.Read(p);
                    if (!BalanceSheetCells.Normalize(p, text, out string canonical, out string error))
                    {
                        preview.Diffs.Add(new Diff { Key = key, Column = field.Column, Old = old, New = text, Error = error });
                        continue;
                    }

                    if (canonical == old)
                    {
                        preview.Unchanged++;
                        continue;
                    }

                    // Two rows can point at one shared object (towers sharing a projectile).
                    var identity = (target, field.Property);
                    if (seen.TryGetValue(identity, out string first) && first != canonical)
                    {
                        preview.Diffs.Add(new Diff
                        {
                            Key = key, Column = field.Column, Old = old, New = canonical,
                            Error = $"shared with another row that sets {first}",
                        });
                        continue;
                    }

                    seen[identity] = canonical;
                    preview.Diffs.Add(new Diff
                    {
                        Key = key, Column = field.Column, Old = old, New = canonical,
                        Target = target, Property = field.Property,
                    });
                }
            }

            return preview;
        }

        /// <summary>Writes the applicable differences. Returns how many values were set.</summary>
        public static int Apply(Preview preview)
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Import balance CSV");
            int group = Undo.GetCurrentGroup();

            var perTarget = new Dictionary<Object, List<Diff>>();
            foreach (Diff d in preview.Diffs)
            {
                if (!d.Applicable)
                {
                    continue;
                }

                if (!perTarget.TryGetValue(d.Target, out List<Diff> list))
                {
                    list = new List<Diff>();
                    perTarget[d.Target] = list;
                }

                list.Add(d);
            }

            int set = 0;
            foreach (KeyValuePair<Object, List<Diff>> pair in perTarget)
            {
                var so = new SerializedObject(pair.Key);
                foreach (Diff d in pair.Value)
                {
                    SerializedProperty p = so.FindProperty(d.Property);
                    if (p != null && BalanceSheetCells.Normalize(p, d.New, out _, out _) && BalanceSheetCells.Write(p, d.New))
                    {
                        set++;
                    }
                }

                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(pair.Key);
            }

            Undo.CollapseUndoOperations(group);
            AssetDatabase.SaveAssets();
            return set;
        }

    }
}
