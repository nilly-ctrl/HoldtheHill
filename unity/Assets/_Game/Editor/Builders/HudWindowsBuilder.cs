using HoldTheHill.Features.Hud;
using HoldTheHill.Features.Progression;
using HoldTheHill.Core.UiKit;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace HoldTheHill.EditorTools.Builders
{
    /// <summary>
    /// The windows a key opens over the field: the skill tree, the achievements list and the
    /// specials spawner. Each is a <see cref="HudPanel"/> in the HUD prefab, centred on the
    /// canvas, that fills itself from the scene's catalogs when it opens.
    /// </summary>
    internal static class HudWindowsBuilder
    {
        /// <summary>One skill in the tree: a kit button, taller than usual, with the icon on the left.</summary>
        internal static SkillNodeRow BuildSkillNodeRow()
        {
            GameObject root = UiKitBuilder.InstantiatePrefab("Button", null);
            root.name = "HudSkillNode";

            var layout = root.GetComponent<LayoutElement>();
            layout.minHeight = layout.preferredHeight = 44f;
            ((RectTransform)root.transform).sizeDelta = new Vector2(220f, 44f);

            Transform label = root.transform.Find("Label");
            var labelRect = (RectTransform)label;
            labelRect.offsetMin = new Vector2(38f, 0f);
            var labelText = label.GetComponent<TextMeshProUGUI>();
            labelText.alignment = TextAlignmentOptions.Left;
            labelText.richText = true;

            RectTransform icon = HudLayout.NewRect("Icon", root.transform);
            HudLayout.Corner(icon, new Vector2(3f, -6f), new Vector2(32f, 32f));
            Image iconImage = HudLayout.AddImage(icon, null);

            var row = root.AddComponent<SkillNodeRow>();
            UiKitBuilder.Wire(row, "_button", root.GetComponent<UiButton>());
            UiKitBuilder.Wire(row, "_icon", iconImage);
            return HudPartsBuilder.SavePart<SkillNodeRow>(root, "HudSkillNode");
        }

        internal static void BuildSkillTreePanel(Transform canvas, SkillNodeRow row)
        {
            RectTransform panel = Window(canvas, "SkillTreePanel", 700f, "COMMANDER SKILL TREE");
            TextMeshProUGUI points = HudLayout.Line(panel, "Skill points  0 SP");

            RectTransform columns = HudLayout.NewRect("Columns", panel);
            var side = columns.gameObject.AddComponent<HorizontalLayoutGroup>();
            side.spacing = 8f;
            side.childControlWidth = true;
            side.childControlHeight = true;
            side.childForceExpandWidth = true;
            side.childForceExpandHeight = false;
            side.childAlignment = TextAnchor.UpperCenter;

            // Copied to head each branch column at runtime; inactive so the layout ignores it here.
            TextMeshProUGUI branchTitle = HudLayout.Line(panel, "BRANCH");
            branchTitle.name = "BranchTitleTemplate";
            branchTitle.color = UiKitStyle.Gold;
            branchTitle.gameObject.SetActive(false);

            TextMeshProUGUI footer = HudLayout.Line(panel, "Damage x1.00   Fire rate x1.00   Food x1.00   Cost x1.00");
            footer.color = UiKitStyle.Dim;
            UiButton close = UiKitBuilder.AddButton(panel, "Close");

            var tree = panel.gameObject.AddComponent<SkillTreePanel>();
            UiKitBuilder.Wire(tree, "_points", points);
            UiKitBuilder.Wire(tree, "_footer", footer);
            UiKitBuilder.Wire(tree, "_close", close);
            UiKitBuilder.Wire(tree, "_rowPrefab", row);
            UiKitBuilder.Wire(tree, "_columnsRoot", columns);
            UiKitBuilder.Wire(tree, "_branchTitleTemplate", branchTitle);
        }

        internal static void BuildAchievementsPanel(Transform canvas)
        {
            RectTransform panel = Window(canvas, "AchievementsPanel", 450f, "ACHIEVEMENTS");
            TextMeshProUGUI summary = HudLayout.Line(panel, "0 of 0 earned");

            RectTransform rows = HudLayout.NewRect("Rows", panel);
            var stack = rows.gameObject.AddComponent<VerticalLayoutGroup>();
            stack.spacing = 4f;
            stack.childControlWidth = true;
            stack.childControlHeight = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;

            TextMeshProUGUI rowTemplate = HudLayout.Line(rows, "Achievement");
            rowTemplate.name = "RowTemplate";
            rowTemplate.richText = true;
            rowTemplate.gameObject.SetActive(false);

            UiButton close = UiKitBuilder.AddButton(panel, "Close");

            var achievements = panel.gameObject.AddComponent<AchievementsPanel>();
            UiKitBuilder.Wire(achievements, "_summary", summary);
            UiKitBuilder.Wire(achievements, "_rowTemplate", rowTemplate);
            UiKitBuilder.Wire(achievements, "_rowsRoot", rows);
            UiKitBuilder.Wire(achievements, "_close", close);
        }

        internal static void BuildSpecialsPanel(Transform canvas)
        {
            RectTransform panel = Window(canvas, "SpecialsPanel", 240f, "SPAWN A SPECIAL");

            RectTransform buttons = HudLayout.NewRect("Buttons", panel);
            var stack = buttons.gameObject.AddComponent<VerticalLayoutGroup>();
            stack.spacing = 2f;
            stack.childControlWidth = true;
            stack.childControlHeight = true;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;

            var buttonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiKitBuilder.PrefabDir + "/Button.prefab");
            UiButton close = UiKitBuilder.AddButton(panel, "Close");

            var specials = panel.gameObject.AddComponent<SpecialsPanel>();
            UiKitBuilder.Wire(specials, "_buttonPrefab", buttonPrefab != null ? buttonPrefab.GetComponent<UiButton>() : null);
            UiKitBuilder.Wire(specials, "_buttonsRoot", buttons);
            UiKitBuilder.Wire(specials, "_close", close);
        }

        // A kit panel centred on the canvas with a gold title line.
        private static RectTransform Window(Transform canvas, string name, float width, string title)
        {
            RectTransform panel = UiKitBuilder.AddPanel(canvas, width);
            panel.name = name;
            TextMeshProUGUI heading = HudLayout.Line(panel, title);
            heading.color = UiKitStyle.Gold;
            return panel;
        }
    }
}
