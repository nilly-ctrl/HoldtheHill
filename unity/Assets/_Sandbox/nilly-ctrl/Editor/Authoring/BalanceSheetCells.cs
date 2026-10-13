using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using HoldTheHill.Features.Enemies;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Field = HoldTheHill.Sandbox.NillyCtrl.Editor.BalanceSheet.Field;

namespace HoldTheHill.Sandbox.NillyCtrl.Editor
{
    /// <summary>
    /// One cell of the balance sheet: reading a serialized property as text, and writing text back into one with its value checked. Used by <see cref="BalanceSheet"/>.
    /// </summary>
    internal static class BalanceSheetCells
    {
        internal static string CellFor(Field field, Object root)
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

        internal static string Read(SerializedProperty p)
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

        internal static bool Write(SerializedProperty p, string canonical)
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
        internal static bool Normalize(SerializedProperty p, string text, out string canonical, out string error)
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
