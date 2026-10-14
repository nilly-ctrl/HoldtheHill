using System.Collections.Generic;
using System.Linq;
using HoldTheHill.Core.UiKit;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace HoldTheHill.EditorTools.Builders
{
    /// <summary>
    /// The low-level pieces the UI kit and screen builders are made of: rects, rows, images, text, the focus ring, and serialized-property setters. Used by <see cref="UiKitBuilder"/> and the widget builder.
    /// </summary>
    internal static class UiKitPrimitives
    {
        internal static Sprite Sprite(string name)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteDir}/{name}.png");
            if (sprite == null)
            {
                Debug.LogError($"[UiKit] Missing sprite '{name}'. Run UiKit/Sprites/Source~/build_ui_sprites.py.");
            }

            return sprite;
        }

        internal static TMP_FontAsset Font()
        {
            if (s_font == null) s_font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiKitBuilder.FontPath);
            return s_font;
        }

        internal static RectTransform NewRect(string name, Transform parent = null)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = LayerMask.NameToLayer("UI") };
            var rect = (RectTransform)go.transform;
            if (parent != null) rect.SetParent(parent, false);
            return rect;
        }

        // A full-width row of the standard height, for use inside a panel or page.
        internal static RectTransform NewRow(string name)
        {
            RectTransform row = NewRect(name);
            row.sizeDelta = new Vector2(240f, UiKitStyle.RowHeight);
            AddRowLayout(row, minWidth: 0f, flexibleWidth: 1f);
            return row;
        }

        internal static void AddRowLayout(RectTransform rect, float minWidth, float flexibleWidth)
        {
            var element = rect.gameObject.AddComponent<LayoutElement>();
            element.minHeight = element.preferredHeight = UiKitStyle.RowHeight;
            element.minWidth = minWidth;
            element.flexibleWidth = flexibleWidth;
        }

        internal static void Stretch(RectTransform rect, float left = 0f, float bottom = 0f, float right = 0f, float top = 0f)
        {
            Anchor(rect, Vector2.zero, Vector2.one, new Vector2(left, bottom), new Vector2(-right, -top));
        }

        internal static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        internal static Image AddImage(RectTransform rect, string spriteName, bool raycast = true)
        {
            var image = rect.gameObject.AddComponent<Image>();
            Sprite sprite = Sprite(spriteName);
            image.sprite = sprite;
            image.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            image.raycastTarget = raycast;
            return image;
        }

        // An invisible graphic: uGUI only delivers clicks to something that draws.
        internal static Image AddHitArea(RectTransform rect)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = Color.clear;
            return image;
        }

        internal static TextMeshProUGUI AddText(Transform parent, string name, string text, int size, TextAlignmentOptions alignment)
        {
            RectTransform rect = NewRect(name, parent);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = Font();
            label.fontSize = size;
            label.color = UiKitStyle.Cream;
            label.alignment = alignment;
            label.text = text;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            return label;
        }

        // Sits just outside the control and is ignored by layout groups.
        internal static GameObject AddFocusRing(RectTransform parent)
        {
            RectTransform ring = NewRect("FocusRing", parent);
            Stretch(ring, -3f, -3f, -3f, -3f);
            AddImage(ring, "FocusRing", raycast: false);
            ring.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            ring.gameObject.SetActive(false);
            return ring.gameObject;
        }

        internal static void SetLabel(GameObject widget, string text) => SetText(widget.transform.Find("Label"), text);

        internal static void SetText(Transform target, string text)
        {
            if (target != null && target.TryGetComponent(out TMP_Text label)) label.text = text;
        }

        internal static string Tidy(string label) => new string(label.Where(char.IsLetterOrDigit).ToArray());

        internal static void Set(Object target, string property, object value)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(property);
            if (prop == null)
            {
                Debug.LogError($"[UiKit] {target.GetType().Name} has no serialized field '{property}'.");
                return;
            }

            switch (value)
            {
                case bool b: prop.boolValue = b; break;
                case int i: prop.intValue = i; break;
                case float f: prop.floatValue = f; break;
                case string s: prop.stringValue = s; break;
                case Object o: prop.objectReferenceValue = o; break;
                default: Debug.LogError($"[UiKit] Unsupported value for '{property}'."); return;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void SetStrings(Object target, string property, string[] values)
        {
            var so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(property);
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
            {
                prop.GetArrayElementAtIndex(i).stringValue = values[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static TMP_FontAsset s_font;

        internal const string SpriteDir = UiKitBuilder.Root + "/Sprites";
    }
}
