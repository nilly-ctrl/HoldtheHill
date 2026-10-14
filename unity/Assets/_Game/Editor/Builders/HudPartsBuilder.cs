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
    /// The HUD's part prefabs (build slot, build option, wave marker, chip), saved under Prefabs/HudLayout.Hud and instanced by <see cref="HudBuilder"/>.
    /// </summary>
    internal static class HudPartsBuilder
    {
        // One tower on the build bar: icon in a slot frame, hotkey on a keycap, price on a tag.
        internal static BuildSlot BuildSlot()
        {
            RectTransform root = HudLayout.NewRect("HudBuildSlot", null);
            root.sizeDelta = new Vector2(48f, 58f);
            HudLayout.Layout(root, 48f, 58f);

            // The whole cell takes the click; nothing inside it does.
            Image hit = root.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;

            RectTransform frame = HudLayout.NewRect("Frame", root);
            HudLayout.Corner(frame, new Vector2(8f, -4f), new Vector2(36f, 36f));
            Image frameImage = HudLayout.AddImage(frame, HudLayout.Hud("Slot"));

            RectTransform icon = HudLayout.NewRect("Icon", frame);
            HudLayout.Centre(icon, new Vector2(32f, 32f));
            Image iconImage = HudLayout.AddImage(icon, null);

            RectTransform keycap = HudLayout.NewRect("Keycap", root);
            HudLayout.Corner(keycap, new Vector2(2f, 0f), new Vector2(12f, 13f));
            HudLayout.AddImage(keycap, HudLayout.Hud("Keycap"));
            TextMeshProUGUI key = HudLayout.AddText(keycap, "Key", "1", TextAlignmentOptions.Top, HudBuilder.Ink);
            HudLayout.Stretch(key.rectTransform, 0f, 0f, 0f, 1f);

            RectTransform tag = HudLayout.NewRect("CostTag", root);
            HudLayout.Corner(tag, new Vector2(5f, -43f), new Vector2(40f, 12f));
            Image tagImage = HudLayout.AddImage(tag, HudLayout.Hud("CostTag"));
            TextMeshProUGUI cost = HudLayout.AddText(tag, "Cost", "100", TextAlignmentOptions.Center, HudBuilder.Ink);
            HudLayout.Stretch(cost.rectTransform, 8f, 0f, 0f, 0f);

            var slot = root.gameObject.AddComponent<BuildSlot>();
            UiKitBuilder.Wire(slot, "_button", button);
            UiKitBuilder.Wire(slot, "_frame", frameImage);
            UiKitBuilder.Wire(slot, "_icon", iconImage);
            UiKitBuilder.Wire(slot, "_key", key);
            UiKitBuilder.Wire(slot, "_cost", cost);
            UiKitBuilder.Wire(slot, "_costTag", tagImage);
            UiKitBuilder.Wire(slot, "_frameNormal", HudLayout.Hud("Slot"));
            UiKitBuilder.Wire(slot, "_frameSelected", HudLayout.Hud("SlotSelected"));
            UiKitBuilder.Wire(slot, "_frameDisabled", HudLayout.Hud("SlotDisabled"));
            UiKitBuilder.Wire(slot, "_tagAffordable", HudLayout.Hud("CostTag"));
            UiKitBuilder.Wire(slot, "_tagTooDear", HudLayout.Hud("CostTagCant"));

            return SavePart<BuildSlot>(root.gameObject, "HudBuildSlot");
        }

        // One tower in the build menu: a kit button, taller than usual, with the icon on the left.
        internal static BuildOption BuildOption()
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

            RectTransform icon = HudLayout.NewRect("Icon", root.transform);
            HudLayout.Corner(icon, new Vector2(3f, -1f), new Vector2(32f, 32f));
            Image iconImage = HudLayout.AddImage(icon, null);

            var option = root.AddComponent<BuildOption>();
            UiKitBuilder.Wire(option, "_button", root.GetComponent<UiButton>());
            UiKitBuilder.Wire(option, "_icon", iconImage);
            return SavePart<BuildOption>(root, "HudBuildOption");
        }

        // A flag on the wave track. Sits on the track's top edge, centred on its wave.
        internal static Image BuildWaveMarker()
        {
            RectTransform root = HudLayout.NewRect("HudWaveMarker", null);
            root.anchorMin = root.anchorMax = new Vector2(0f, 1f);
            root.pivot = new Vector2(0.5f, 0f);
            root.anchoredPosition = new Vector2(0f, -1f);
            Image image = HudLayout.AddImage(root, HudLayout.Hud("WaveMarker"));
            image.raycastTarget = false;
            HudLayout.FitToSprite(image);
            return SavePart<Image>(root.gameObject, "HudWaveMarker");
        }

        // A small framed label for the Warden strip.
        internal static HudChip BuildChip()
        {
            RectTransform root = HudLayout.NewRect("HudChip", null);
            HudLayout.AddImage(root, HudLayout.Kit("Well")).raycastTarget = false;

            var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(4, 4, 2, 2);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var fitter = root.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            TextMeshProUGUI text = HudLayout.AddText(root, "Text", "Chip", TextAlignmentOptions.Left, UiKitStyle.Cream);
            var chip = root.gameObject.AddComponent<HudChip>();
            UiKitBuilder.Wire(chip, "_text", text);
            return SavePart<HudChip>(root.gameObject, "HudChip");
        }

        // Saves the object as a prefab asset and hands back the component of that name on the asset.
        internal static T SavePart<T>(GameObject instance, string name) where T : Component
        {
            string path = $"{HudBuilder.GrayboxRoot}/Prefabs/{HudBuilder.PartsFolder}/{name}.prefab";
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(instance, path);
            Object.DestroyImmediate(instance);
            return saved.GetComponent<T>();
        }
    }
}
