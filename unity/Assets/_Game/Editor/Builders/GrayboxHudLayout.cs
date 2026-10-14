using HoldTheHill.Core.UiKit;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HoldTheHill.EditorTools.Builders
{
    /// <summary>
    /// Layout and asset helpers shared by the HUD builders: rectangles, anchoring, images, text, fonts and sprite lookups.
    /// </summary>
    internal static class GrayboxHudLayout
    {
        // A column that stacks its panels from a corner of the screen and shrinks to fit.
        internal static RectTransform Column(string name, Transform parent, Vector2 corner, Vector2 offset)
        {
            RectTransform column = NewRect(name, parent);
            Anchor(column, corner, corner, corner, offset, Vector2.zero);

            var layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = UiKitStyle.Spacing;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var fitter = column.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return column;
        }

        // A kit panel of a fixed width inside a column.
        internal static RectTransform HudPanel(Transform parent, string name, float width)
        {
            var panel = (RectTransform)UiKitBuilder.InstantiatePrefab("Panel", parent).transform;
            panel.name = name;
            var layout = panel.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            return panel;
        }

        // One line of text in a panel, left aligned.
        internal static TextMeshProUGUI Line(Transform panel, string text)
        {
            TextMeshProUGUI line = UiKitBuilder.AddBody(panel, text);
            line.alignment = TextAlignmentOptions.Left;
            return line;
        }

        internal static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = LayerMask.NameToLayer("UI") };
            var rect = (RectTransform)go.transform;
            if (parent != null)
            {
                rect.SetParent(parent, false);
            }

            return rect;
        }

        internal static void Anchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        // Pinned to the top left of its parent by its own top left corner.
        internal static void Corner(RectTransform rect, Vector2 position, Vector2 size)
        {
            Anchor(rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), position, size);
        }

        internal static void Centre(RectTransform rect, Vector2 size)
        {
            Anchor(rect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
        }

        internal static void Stretch(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        internal static void Layout(RectTransform rect, float width, float height)
        {
            var layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.minWidth = layout.preferredWidth = width;
            layout.minHeight = layout.preferredHeight = height;
        }

        // Does not take clicks unless the caller turns that back on.
        internal static Image AddImage(RectTransform rect, Sprite sprite)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            image.raycastTarget = false;

            // The HUD art is imported at 32 pixels per unit for the world; here one art pixel is one canvas unit.
            if (sprite != null)
            {
                image.pixelsPerUnitMultiplier = 100f / sprite.pixelsPerUnit;
            }

            return image;
        }

        // Draws the sprite at its own pixel size, one art pixel to a canvas unit.
        internal static void FitToSprite(Image image)
        {
            image.rectTransform.sizeDelta = image.sprite.rect.size;
        }

        internal static TextMeshProUGUI AddText(Transform parent, string name, string text, TextAlignmentOptions alignment, Color colour)
        {
            RectTransform rect = NewRect(name, parent);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.font = Font();
            label.fontSize = UiKitStyle.BodySize;
            label.color = colour;
            label.alignment = alignment;
            label.text = text;
            label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            return label;
        }

        internal static TMP_FontAsset Font()
        {
            if (GrayboxHudBuilder.s_font == null)
            {
                GrayboxHudBuilder.s_font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(UiKitBuilder.FontPath);
            }

            return GrayboxHudBuilder.s_font;
        }

        internal static Sprite Hud(string name) => LoadSprite($"{GrayboxHudBuilder.HudSprites}/{name}.png");

        internal static Sprite Kit(string name) => LoadSprite($"{GrayboxHudBuilder.KitSprites}/{name}.png");

        internal static Sprite Icon(string name) => LoadSprite($"{GrayboxHudBuilder.IconSprites}/{name}.png");

        internal static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                Debug.LogError($"[Graybox] Missing HUD sprite {path}.");
            }

            return sprite;
        }
    }
}
