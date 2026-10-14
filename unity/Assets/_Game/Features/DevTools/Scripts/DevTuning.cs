#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace HoldTheHill.Features.DevTools
{
    /// <summary>One number, switch or choice on a component that the tuning panel can change.</summary>
    public sealed class DevTunedField
    {
        public DevTunedField(Component owner, int componentIndex, FieldInfo info)
        {
            Owner = owner;
            ComponentIndex = componentIndex;
            Info = info;
            Original = info.GetValue(owner);
        }

        public Component Owner { get; }

        /// <summary>Which of the components of this type on the object this is (usually 0).</summary>
        public int ComponentIndex { get; }

        public FieldInfo Info { get; }

        /// <summary>The value when the object was picked.</summary>
        public object Original { get; }

        /// <summary>Text the panel's edit box holds while it is focused.</summary>
        public string Buffer { get; set; }

        public Type ComponentType => Owner.GetType();

        public string Name => Info.Name;

        public Type ValueType => Info.FieldType;

        public object Value
        {
            get => Info.GetValue(Owner);
            set => Info.SetValue(Owner, value);
        }

        public bool Changed => !Equals(Value, Original);

        public string Label => ComponentType.Name + "." + DevTuning.Pretty(Name);
    }

    /// <summary>A changed value to write back into an asset (see <see cref="DevTuningWriteBack"/>).</summary>
    public sealed class DevTunedChange
    {
        public Type ComponentType;
        public int ComponentIndex;
        public string FieldName;
        public Type ValueType;
        public object Value;
    }

    /// <summary>
    /// Finds the tunable fields of a game object by reflection: private [SerializeField] and
    /// public fields of types float, int, bool and enum, on the HoldTheHill components.
    /// </summary>
    /// <remarks>
    /// Values are written straight to the live component. A script that copies a number in Awake
    /// or Start will not see a later change; most of the combat numbers are read each use.
    /// </remarks>
    public static class DevTuning
    {
        private const int MaxFields = 80;
        private const BindingFlags Declared = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        public static List<DevTunedField> Collect(GameObject go)
        {
            var fields = new List<DevTunedField>();
            if (go == null) return fields;

            var counts = new Dictionary<Type, int>();
            foreach (MonoBehaviour behaviour in go.GetComponents<MonoBehaviour>())
            {
                if (behaviour == null) continue;

                Type type = behaviour.GetType();
                if (!IsGameType(type)) continue;

                counts.TryGetValue(type, out int index);
                counts[type] = index + 1;

                for (Type t = type; t != null && t != typeof(MonoBehaviour); t = t.BaseType)
                {
                    foreach (FieldInfo info in t.GetFields(Declared))
                    {
                        if (fields.Count >= MaxFields) return fields;
                        if (IsTunable(info)) fields.Add(new DevTunedField(behaviour, index, info));
                    }
                }
            }

            return fields;
        }

        private static bool IsGameType(Type type)
        {
            string ns = type.Namespace;
            return ns != null && ns.StartsWith("HoldTheHill", StringComparison.Ordinal);
        }

        private static bool IsTunable(FieldInfo info)
        {
            if (info.IsStatic || info.IsInitOnly || info.IsLiteral) return false;
            if (info.Name.StartsWith("<", StringComparison.Ordinal)) return false;

            bool serialised = info.IsPublic
                ? !info.IsDefined(typeof(NonSerializedAttribute), true)
                : info.IsDefined(typeof(SerializeField), true);
            if (!serialised) return false;

            Type type = info.FieldType;
            return type == typeof(float) || type == typeof(int) || type == typeof(bool) || type.IsEnum;
        }

        /// <summary>"_fireInterval" becomes "fireInterval".</summary>
        public static string Pretty(string fieldName)
        {
            return fieldName.TrimStart('_');
        }

        public static string Format(object value)
        {
            if (value is float f) return f.ToString("0.###", CultureInfo.InvariantCulture);
            if (value is IFormattable formattable) return formattable.ToString(null, CultureInfo.InvariantCulture);
            return value != null ? value.ToString() : string.Empty;
        }

        /// <summary>Sets a field from text. False if the text is not a number of the field's type.</summary>
        public static bool TryParse(DevTunedField field, string text, out object value)
        {
            value = null;
            if (field.ValueType == typeof(float)
                && float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out float f)
                && !float.IsNaN(f) && !float.IsInfinity(f))
            {
                value = f;
                return true;
            }

            if (field.ValueType == typeof(int) && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out int i))
            {
                value = i;
                return true;
            }

            return false;
        }

        /// <summary>The next value of an enum, wrapping round.</summary>
        public static object NextEnum(Type enumType, object current)
        {
            Array values = Enum.GetValues(enumType);
            int index = Array.IndexOf(values, current);
            return values.GetValue((index + 1) % values.Length);
        }

        /// <summary>The fields that were changed from their picked values, as write-back entries.</summary>
        public static List<DevTunedChange> Changes(IEnumerable<DevTunedField> fields)
        {
            var changes = new List<DevTunedChange>();
            foreach (DevTunedField field in fields)
            {
                if (!field.Changed) continue;
                changes.Add(new DevTunedChange
                {
                    ComponentType = field.ComponentType,
                    ComponentIndex = field.ComponentIndex,
                    FieldName = field.Name,
                    ValueType = field.ValueType,
                    Value = field.Value,
                });
            }

            return changes;
        }

        /// <summary>Copies the changed values onto <paramref name="other"/>'s matching components. Returns how many fields were set.</summary>
        public static int ApplyTo(GameObject other, IEnumerable<DevTunedField> fields)
        {
            int set = 0;
            foreach (DevTunedField field in fields)
            {
                if (!field.Changed) continue;

                Component[] candidates = other.GetComponents(field.ComponentType);
                if (field.ComponentIndex >= candidates.Length) continue;

                field.Info.SetValue(candidates[field.ComponentIndex], field.Value);
                set++;
            }

            return set;
        }

        /// <summary>The changed values as one "Type.field = value" line each, for pasting into a builder or a note.</summary>
        public static string AsText(string title, IEnumerable<DevTunedField> fields)
        {
            var text = new StringBuilder();
            text.Append(title).Append('\n');
            foreach (DevTunedField field in fields)
            {
                if (!field.Changed) continue;
                text.Append(field.ComponentType.Name).Append('.').Append(field.Name).Append(" = ")
                    .Append(Format(field.Value)).Append("  (was ").Append(Format(field.Original)).Append(")\n");
            }

            return text.ToString();
        }
    }

    /// <summary>
    /// The way the tuning panel's "copy to data asset" reaches the Editor. The runtime assembly
    /// cannot use UnityEditor, so <c>Editor/DevTools/DevTuningWriteBackEditor</c> installs
    /// <see cref="Handler"/> when the Editor loads. In a development build it stays null.
    /// </summary>
    public static class DevTuningWriteBack
    {
        /// <summary>(live instance, prefab to use if the instance has no link to one, changes) to a message.</summary>
        public static Func<GameObject, GameObject, IList<DevTunedChange>, string> Handler;

        public static bool Available => Handler != null;

        public static string Apply(GameObject instance, GameObject prefabHint, IList<DevTunedChange> changes)
        {
            if (Handler == null) return "Writing back to assets only works in the Unity Editor.";
            if (changes == null || changes.Count == 0) return "Nothing has been changed.";
            return Handler(instance, prefabHint, changes);
        }
    }
}
#endif
