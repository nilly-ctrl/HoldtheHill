#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Reflection;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Reads and writes private fields by name. Dev tools only: it exists so the console and the
    /// tuning panel can see values the game classes keep private, without editing those classes.
    /// </summary>
    internal static class DevReflect
    {
        private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        /// <summary>The field's value, or <paramref name="fallback"/> if the object, field or type does not match.</summary>
        public static T Get<T>(object target, string fieldName, T fallback = default)
        {
            FieldInfo field = Find(target, fieldName);
            if (field == null) return fallback;

            object value = field.GetValue(target);
            return value is T typed ? typed : fallback;
        }

        public static bool Set(object target, string fieldName, object value)
        {
            FieldInfo field = Find(target, fieldName);
            if (field == null || (value != null && !field.FieldType.IsInstanceOfType(value))) return false;

            field.SetValue(target, value);
            return true;
        }

        private static FieldInfo Find(object target, string fieldName)
        {
            if (target == null) return null;

            // Private fields of a base class are not found from the derived type, so walk up.
            for (Type type = target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(fieldName, Instance | BindingFlags.DeclaredOnly);
                if (field != null) return field;
            }

            return null;
        }
    }
}
#endif
