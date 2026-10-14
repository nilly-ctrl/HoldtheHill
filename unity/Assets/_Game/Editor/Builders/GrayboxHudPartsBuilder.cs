using HoldTheHill.Features.Hud;
using HoldTheHill.Core.UiKit;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HoldTheHill.EditorTools.Builders
{
    /// <summary>
    /// The HUD's part prefabs (build slot, build option, wave marker, chip), saved under Prefabs/GrayboxHudLayout.Hud and instanced by <see cref="GrayboxHudBuilder"/>.
    /// </summary>
    internal static class GrayboxHudPartsBuilder
    {
        // One tower on the build bar: icon in a slot frame, hotkey on a keycap, price on a tag.
        internal static GrayboxBuildSlot BuildSlot()
        {
            RectTransform root = GrayboxHudLayout.NewRect("HudBuildSlot", null);
            root.sizeDelta = new Vector2(48f, 58f);
            GrayboxHudLayout.Layout(root, 48f, 58f);

            // The whole cell takes the click; nothing inside it does.
            Image hit = root.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;

            RectTransform frame = GrayboxHudLayout.NewRect("Frame", root);
            GrayboxHudLayout.Corner(frame, new Vector2(8f, -4f), new Vector2(36f, 36f));
            Image frameImage = GrayboxHudLayout.AddImage(frame, GrayboxHudLayout.Hud("Slot"));

            RectTransform icon = GrayboxHudLayout.NewRect("Icon", frame);
            GrayboxHudLayout.Centre(icon, new Vector2(32f, 32f));
            Image iconImage = GrayboxHudLayout.AddImage(icon, null);

            RectTransform keycap = GrayboxHudLayout.NewRect("Keycap", root);
            GrayboxHudLayout.Corner(keycap, new Vector2(2f, 0f), new Vector2(12f, 13f));
            GrayboxHudLayout.AddImage(keycap, GrayboxHudLayout.Hud("Keycap"));
            TextMeshProUGUI key = GrayboxHudLayout.AddText(keycap, "Key", "1", TextAlignmentOptions.Top, GrayboxHudBuilder.Ink);
            GrayboxHudLayout.Stretch(key.rectTransform, 0f, 0f, 0f, 1f);

            RectTransform tag = GrayboxHudLayout.NewRect("CostTag", root);
            GrayboxHudLayout.Corner(tag, new Vector2(5f, -43f), new Vector2(40f, 12f));
            Image tagImage = GrayboxHudLayout.AddImage(tag, GrayboxHudLayout.Hud("CostTag"));
            TextMeshProUGUI cost = GrayboxHudLayout.AddText(tag, "Cost", "100", TextAlignmentOptions.Center, GrayboxHudBuilder.Ink);
            GrayboxHudLayout.Stretch(cost.rectTransform, 8f, 0f, 0f, 0f);

            var slot = root.gameObject.AddComponent<GrayboxBuildSlot>();
            UiKitBuilder.Wire(slot, "_button", button);
            UiKitBuilder.Wire(slot, "_frame", frameImage);
            UiKitBuilder.Wire(slot, "_icon", iconImage);
            UiKitBuilder.Wire(slot, "_key", key);
            UiKitBuilder.Wire(slot, "_cost", cost);
            UiKitBuilder.Wire(slot, "_costTag", tagImage);
            UiKitBuilder.Wire(slot, "_frameNormal", GrayboxHudLayout.Hud("Slot"));
            UiKitBuilder.Wire(slot, "_frameSelected", GrayboxHudLayout.Hud("SlotSelected"));
            UiKitBuilder.Wire(slot, "_frameDisabled", GrayboxHudLayout.Hud("SlotDisabled"));
            UiKitBuilder.Wire(slot, "_tagAffordable", GrayboxHudLayout.Hud("CostTag"));
            UiKitBuilder.Wire(slot, "_tagTooDear", GrayboxHudLayout.Hud("CostTagCant"));

            return SavePart<GrayboxBuildSlot>(root.gameObject, "HudBuildSlot");
        }

        // One tower in the build menu: a kit button, taller than usual, with the icon on the left.
        internal static GrayboxBuildOption BuildOption()
        {
            GameObject root = UiKitBuilder.InstantiatePrefab("Button", null);
            root.name = "HudBuildOption";

            var layout = root.GetComponent<LayoutElement>();
            layout.minHeight = layout.preferredHeight = 34f;
            ((RectTransform)root.transform).sizeDelta = new Vector2(112f, 34f);

            Transform label = root.transform.Find("Label");
            var labelRect = (RectTransform)label;
            labelRect.offsetMin = new Vector2(38f, 0f);
            var labelText = label.GetComponent<TextMeshProUGUI>();
            labelText.alignment = TextAlignmentOptions.Left;

            RectTransform icon = GrayboxHudLayout.NewRect("Icon", root.transform);
            GrayboxHudLayout.Corner(icon, new Vector2(3f, -1f), new Vector2(32f, 32f));
            Image iconImage = GrayboxHudLayout.AddImage(icon, null);

            var option = root.AddComponent<GrayboxBuildOption>();
            UiKitBuilder.Wire(option, "_button", root.GetComponent<UiButton>());
            UiKitBuilder.Wire(option, "_icon", iconImage);
            return SavePart<GrayboxBuildOption>(root, "HudBuildOption");
        }

        // A flag on the wave track. Sits on the track's top edge, centred on its wave.
        internal static Image BuildWaveMarker()
        {
            RectTransform root = GrayboxHudLayout.NewRect("HudWaveMarker", null);
            root.anchorMin = root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0.5f, 0f);
            root.anchoredPosition = new Vector2(0f, -1f);
            Image image = GrayboxHudLayout.AddImage(root, GrayboxHudLayout.Hud("WaveMarker"));
            image.raycastTarget = false;
            GrayboxHudLayout.FitToSprite(image);
            return SavePart<Image>(root.gameObject, "HudWaveMarker");
        }

        // A small framed label for the Warden strip.
        internal static GrayboxHudChip BuildChip()
        {
            RectTransform root = GrayboxHudLayout.NewRect("HudChip", null);
            GrayboxHudLayout.AddImage(root, GrayboxHudLayout.Kit("Well")).raycastTarget = false;

            var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 2, 2);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var fitter = root.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TextMeshProUGUI text = GrayboxHudLayout.AddText(root, "Text", "Chip", TextAlignmentOptions.Left, UiKitStyle.Cream);
            var chip = root.gameObject.AddComponent<GrayboxHudChip>();
            UiKitBuilder.Wire(chip, "_text", text);
            return SavePart<GrayboxHudChip>(root.gameObject, "HudChip");
        }

        // Saves the object as a prefab asset and hands back the component of that name on the asset.
        internal static T SavePart<T>(GameObject instance, string name) where T : Component
        {
            string path = $"{GrayboxHudBuilder.GrayboxRoot}/Prefabs/{GrayboxHudBuilder.PartsFolder}/{name}.prefab";
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
            return saved.GetComponent<T>();
        }
    }
}
