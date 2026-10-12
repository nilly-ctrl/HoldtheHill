using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using HoldTheHill.Features.Enemies;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace HoldTheHill.Sandbox.NillyCtrl.Editor
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
        private const string SandboxRoot = "Assets/_Sandbox/nilly-ctrl";
        private const string EnemyPrefabFolder = SandboxRoot + "/Graybox/Prefabs";

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

        public static readonly Kind Towers = MakeTowers();
        public static readonly Kind Enemies = MakeEnemies();
        public static readonly Kind PowerUps = MakePowerUps();
        public static readonly Kind[] All = { Towers, Enemies, PowerUps };

        // ---------- definitions ----------

        private static Kind MakeTowers()
        {
            var kind = new Kind
            {
                Name = "Towers",
                FileName = "towers.csv",
                Accepts = o => o is GrayboxTowerData,
                Roots = () => FindAssets<GrayboxTowerData>(),
            };
            kind.Fields.Add(new Field { Column = "displayName", Property = "_displayName", Target = r => r });
            kind.Fields.Add(new Field { Column = "archetype", Property = "_archetype", Target = r => r });
            kind.Fields.Add(new Field { Column = "baseCost", Property = "_baseCost", Target = r => r });
            kind.Fields.Add(new Field { Column = "iconName", Property = "_iconName", Target = r => r });
            kind.Fields.Add(new Field { Column = "range", Property = "_range", Target = TowerOf });
            kind.Fields.Add(new Field { Column = "fireInterval", Property = "_fireInterval", Target = TowerOf });
            kind.Fields.Add(new Field { Column = "priority", Property = "_priority", Target = TowerOf });
            kind.Fields.Add(new Field
            {
                Column = "projectile",
                ReadOnly = true,
                Computed = r =>
                {
                    Object projectile = Follow(TowerOf(r), "_projectilePrefab");
                    return projectile != null ? AssetDatabase.GetAssetPath(projectile) : string.Empty;
                },
            });
            kind.Fields.Add(new Field
            {
                Column = "projectileDamage",
                Property = "_damage",
                Target = r => Follow(TowerOf(r), "_projectilePrefab"),
            });
            return kind;
        }

        private static Kind MakeEnemies()
        {
            var kind = new Kind
            {
                Name = "Enemies and bosses",
                FileName = "enemies.csv",
                Warning = "Enemy and boss prefabs are regenerated by the graybox builders on every Build Graybox, " +
                          "so these numbers are overwritten by the next rebuild unless the builder is changed to match.",
                Accepts = o => o is GameObject go && go.GetComponent<EnemyHealth>() != null,
                Roots = EnemyPrefabs,
            };
            kind.Fields.Add(new Field
            {
                Column = "boss",
                ReadOnly = true,
                Computed = r => r is GameObject go && go.GetComponent<GrayboxBoss>() != null ? "true" : "false",
            });
            kind.Fields.Add(new Field { Column = "maxHealth", Property = "_maxHealth", Target = r => Part<EnemyHealth>(r) });
            kind.Fields.Add(new Field { Column = "bounty", Property = "_bountyValue", Target = r => Part<EnemyHealth>(r) });
            kind.Fields.Add(new Field { Column = "speed", Property = "_speed", Target = r => Part<EnemyMover>(r) });
            kind.Fields.Add(new Field { Column = "maxShield", Property = "_maxShield", Target = r => Part<EnemyShield>(r) });
            kind.Fields.Add(new Field { Column = "breachDamage", Property = "_breachDamage", Target = r => Part<EnemyHealth>(r) });
            kind.Fields.Add(new Field
            {
                Column = "bossName",
                Property = "_displayName",
                Target = r => Follow(Part<GrayboxBoss>(r), "_data"),
            });
            kind.Fields.Add(new Field
            {
                Column = "bannerText",
                Property = "_bannerText",
                Target = r => Follow(Part<GrayboxBoss>(r), "_data"),
            });
            return kind;
        }

        private static Kind MakePowerUps()
        {
            var kind = new Kind
            {
                Name = "Power-ups",
                FileName = "powerups.csv",
                Accepts = o => o is GrayboxPowerUpData,
                Roots = () => FindAssets<GrayboxPowerUpData>(),
            };
            kind.Fields.Add(new Field { Column = "id", Property = "_id", ReadOnly = true, Target = r => r });
            kind.Fields.Add(new Field { Column = "displayName", Property = "_displayName", Target = r => r });
            kind.Fields.Add(new Field { Column = "description", Property = "_description", Target = r => r });
            kind.Fields.Add(new Field { Column = "duration", Property = "_duration", Target = r => r });
            return kind;
        }

        private static List<Object> FindAssets<T>() where T : Object
        {
            var found = new List<Object>();
            foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { SandboxRoot }))
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null)
                {
                    found.Add(asset);
                }
            }

            found.Sort((a, b) => string.CompareOrdinal(AssetDatabase.GetAssetPath(a), AssetDatabase.GetAssetPath(b)));
            return found;
        }

        private static List<Object> EnemyPrefabs()
        {
            var found = new List<Object>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { EnemyPrefabFolder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetDirectoryName(path)?.Replace('\\', '/') != EnemyPrefabFolder)
                {
                    continue;
                }

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab != null && prefab.GetComponent<EnemyHealth>() != null && !prefab.name.EndsWith("Base"))
                {
                    found.Add(prefab);
                }
            }

            found.Sort((a, b) => string.CompareOrdinal(AssetDatabase.GetAssetPath(a), AssetDatabase.GetAssetPath(b)));
            return found;
        }

        private static Object TowerOf(Object root) => (root as GrayboxTowerData)?.Prefab;

        private static Object Part<T>(Object root) where T : Component =>
            root is GameObject go ? go.GetComponent<T>() : null;

        /// <summary>The object an object-reference property points at, or null.</summary>
        private static Object Follow(Object holder, string property)
        {
            if (holder == null)
            {
                return null;
            }

            SerializedProperty p = new SerializedObject(holder).FindProperty(property);
            return p != null && p.propertyType == SerializedPropertyType.ObjectReference ? p.objectReferenceValue : null;
        }

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
                    row.Add(CellFor(f, root));
                }

                rows.Add(row.ToArray());
            }

            string path = Path.Combine(folder, kind.FileName);
            BalanceCsv.Write(path, rows);
            return path;
        }

        private static string CellFor(Field field, Object root)
        {
            if (field.Computed != null)
            {
                return field.Computed(root);
            }

            Object target = field.Target(root);
            if (target == null)
            {
                return string.Empty;
            }

            SerializedProperty p = new SerializedObject(target).FindProperty(field.Property);
            return p != null ? Read(p) : string.Empty;
        }

        private static string Read(SerializedProperty p)
        {
            switch (p.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return p.intValue.ToString(CultureInfo.InvariantCulture);
                case SerializedPropertyType.Float:
                    return p.floatValue.ToString("R", CultureInfo.InvariantCulture);
                case SerializedPropertyType.String:
                    return p.stringValue ?? string.Empty;
                case SerializedPropertyType.Boolean:
                    return p.boolValue ? "true" : "false";
                case SerializedPropertyType.Enum:
                    return p.enumValueIndex >= 0 && p.enumValueIndex < p.enumNames.Length
                        ? p.enumNames[p.enumValueIndex]
                        : string.Empty;
                default:
                    return string.Empty;
            }
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

                    string old = Read(p);
                    if (!Normalize(p, text, out string canonical, out string error))
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
                    if (p != null && Normalize(p, d.New, out _, out _) && Write(p, d.New))
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

        private static bool Write(SerializedProperty p, string canonical)
        {
            switch (p.propertyType)
            {
                case SerializedPropertyType.Integer:
                    p.intValue = int.Parse(canonical, CultureInfo.InvariantCulture);
                    return true;
                case SerializedPropertyType.Float:
                    p.floatValue = float.Parse(canonical, CultureInfo.InvariantCulture);
                    return true;
                case SerializedPropertyType.String:
                    p.stringValue = canonical;
                    return true;
                case SerializedPropertyType.Boolean:
                    p.boolValue = canonical == "true";
                    return true;
                case SerializedPropertyType.Enum:
                    int index = Array.IndexOf(p.enumNames, canonical);
                    if (index < 0)
                    {
                        return false;
                    }

                    p.enumValueIndex = index;
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Checks a cell against the property's type and returns the form <see cref="Read"/> would give.</summary>
        private static bool Normalize(SerializedProperty p, string text, out string canonical, out string error)
        {
            canonical = null;
            error = null;
            switch (p.propertyType)
            {
                case SerializedPropertyType.Integer:
                    if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i))
                    {
                        error = "not a whole number";
                        return false;
                    }

                    if (i < 0)
                    {
                        error = "negative";
                        return false;
                    }

                    canonical = i.ToString(CultureInfo.InvariantCulture);
                    return true;

                case SerializedPropertyType.Float:
                    if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float f)
                        || float.IsNaN(f) || float.IsInfinity(f))
                    {
                        error = "not a number";
                        return false;
                    }

                    if (f < 0f)
                    {
                        error = "negative";
                        return false;
                    }

                    canonical = f.ToString("R", CultureInfo.InvariantCulture);
                    return true;

                case SerializedPropertyType.String:
                    canonical = text;
                    return true;

                case SerializedPropertyType.Boolean:
                    if (text.Equals("true", StringComparison.OrdinalIgnoreCase) || text == "1")
                    {
                        canonical = "true";
                        return true;
                    }

                    if (text.Equals("false", StringComparison.OrdinalIgnoreCase) || text == "0")
                    {
                        canonical = "false";
                        return true;
                    }

                    error = "not true or false";
                    return false;

                case SerializedPropertyType.Enum:
                    for (int n = 0; n < p.enumNames.Length; n++)
                    {
                        if (p.enumNames[n].Equals(text, StringComparison.OrdinalIgnoreCase))
                        {
                            canonical = p.enumNames[n];
                            return true;
                        }
                    }

                    error = "not one of " + string.Join("/", p.enumNames);
                    return false;

                default:
                    error = "unsupported property type " + p.propertyType;
                    return false;
            }
        }
    }
}
