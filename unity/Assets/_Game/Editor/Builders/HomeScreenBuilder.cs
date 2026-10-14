using HoldTheHill.Core.Save;
using HoldTheHill.Features.GameFlow;
using HoldTheHill.Features.Menus;
using System.Collections.Generic;
using HoldTheHill.Core.UiKit;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HoldTheHill.EditorTools.Builders
{
    /// <summary>
    /// The Home hub screen: level pick, the Honeydew upgrade shop, awards and records. The largest screen, so it has its own file.
    /// </summary>
    internal static class HomeScreenBuilder
    {
        internal static HomeScreen BuildHome(Transform canvas)
        {
            RectTransform screen = UiKitBuilder.AddScreen(canvas, "HomeScreen", dim: false);

            Image backdrop = UiKitBuilder.AddPicture(screen, "Backdrop", MenusBuilder.PixelSprite(MenusBuilder.TitleBackgroundPath), Vector2.zero);
            backdrop.rectTransform.anchorMin = Vector2.zero;
            backdrop.rectTransform.anchorMax = Vector2.one;
            backdrop.rectTransform.offsetMin = backdrop.rectTransform.offsetMax = Vector2.zero;
            backdrop.color = new Color(0.55f, 0.55f, 0.6f, 1f); // pushed back so the panel reads
            backdrop.raycastTarget = true;

            RectTransform panel = UiKitBuilder.AddPanel(screen, 320f);
            UiKitBuilder.AddTitle(panel, "Home");
            TextMeshProUGUI honeydew = UiKitBuilder.AddBody(panel, "0 Honeydew");
            honeydew.name = "Honeydew";
            honeydew.color = UiKitStyle.Gold;

            UiTabBar tabs = UiKitBuilder.AddTabBar(panel);
            RectTransform playPage = UiKitBuilder.AddPage(panel, "PlayPage");
            RectTransform upgradesPage = UiKitBuilder.AddPage(panel, "UpgradesPage");
            RectTransform awardsPage = UiKitBuilder.AddPage(panel, "AwardsPage");
            RectTransform recordsPage = UiKitBuilder.AddPage(panel, "RecordsPage");
            UiButton playTab = UiKitBuilder.AddTab(tabs, "Play", playPage.gameObject);
            UiKitBuilder.AddTab(tabs, "Upgrades", upgradesPage.gameObject);
            UiKitBuilder.AddTab(tabs, "Awards", awardsPage.gameObject);
            UiKitBuilder.AddTab(tabs, "Records", recordsPage.gameObject);

            var home = screen.gameObject.AddComponent<HomeScreen>();
            UiKitBuilder.Wire(home, "_honeydew", honeydew);

            // Play
            UiStepper level = UiKitBuilder.AddStepper(playPage, "Level", new[] { LevelList.All[0].Name }, 0);
            UiStepper mode = UiKitBuilder.AddStepper(playPage, "Mode", new[] { "Campaign", "Endless" }, 0);
            TextMeshProUGUI best = UiKitBuilder.AddBody(playPage, "Hold the hill through every wave.");
            best.name = "Best";
            best.color = UiKitStyle.Dim;
            UiButton start = UiKitBuilder.AddButton(playPage, "Start run");
            UiButton toTitle = UiKitBuilder.AddButton(playPage, "Back to title");
            UiKitBuilder.Wire(home, "_level", level);
            UiKitBuilder.Wire(home, "_mode", mode);
            UiKitBuilder.Wire(home, "_best", best);
            UiKitBuilder.Wire(home, "_start", start);
            UiKitBuilder.Wire(home, "_toTitle", toTitle);

            // Upgrades: a name-and-buy row with the effect of the next level under it.
            var homeObject = new SerializedObject(home);
            SerializedProperty upgradeRows = homeObject.FindProperty("_upgrades");
            foreach (PermanentUpgrades.Upgrade upgrade in PermanentUpgrades.All)
            {
                UiButton buy = UiKitBuilder.AddButtonRow(upgradesPage, upgrade.Name, "Buy", out TextMeshProUGUI name);
                buy.transform.parent.name = MenusBuilder.Tidy(upgrade.Id) + "Row";
                TextMeshProUGUI effect = UiKitBuilder.AddBody(upgradesPage, "Next: " + PermanentUpgrades.Describe(upgrade, 1));
                effect.name = MenusBuilder.Tidy(upgrade.Id) + "Effect";
                effect.alignment = TextAlignmentOptions.Left;
                effect.color = UiKitStyle.Dim;

                SerializedProperty row = MenusBuilder.Append(upgradeRows);
                row.FindPropertyRelative("Id").stringValue = upgrade.Id;
                row.FindPropertyRelative("Name").objectReferenceValue = name;
                row.FindPropertyRelative("Effect").objectReferenceValue = effect;
                row.FindPropertyRelative("Buy").objectReferenceValue = buy;
            }

            // Awards
            TextMeshProUGUI awardCount = UiKitBuilder.AddBody(awardsPage, "0 of 0 earned");
            awardCount.name = "AwardCount";
            awardCount.color = UiKitStyle.Dim;
            SerializedProperty awardRows = homeObject.FindProperty("_awards");
            for (int i = 0; i < MenusBuilder.AwardRows; i++)
            {
                TextMeshProUGUI value = UiKitBuilder.AddValueRow(awardsPage, "Award " + (i + 1), "0/1");
                SerializedProperty row = MenusBuilder.Append(awardRows);
                row.FindPropertyRelative("Root").objectReferenceValue = value.transform.parent.gameObject;
                row.FindPropertyRelative("Label").objectReferenceValue = value.transform.parent.Find("Label").GetComponent<TMP_Text>();
                row.FindPropertyRelative("Value").objectReferenceValue = value;
            }

            // Records
            homeObject.FindProperty("_awardCount").objectReferenceValue = awardCount;
            homeObject.FindProperty("_runs").objectReferenceValue = UiKitBuilder.AddValueRow(recordsPage, "Runs finished", "0");
            homeObject.FindProperty("_wins").objectReferenceValue = UiKitBuilder.AddValueRow(recordsPage, "Hills defended", "0");
            homeObject.FindProperty("_kills").objectReferenceValue = UiKitBuilder.AddValueRow(recordsPage, "Kills", "0");
            homeObject.FindProperty("_food").objectReferenceValue = UiKitBuilder.AddValueRow(recordsPage, "Food earned", "0");
            homeObject.FindProperty("_time").objectReferenceValue = UiKitBuilder.AddValueRow(recordsPage, "Time played", "0:00");
            UiKitBuilder.AddDivider(recordsPage);
            TextMeshProUGUI recordsNote = UiKitBuilder.AddBody(recordsPage, "For the level chosen on the Play tab:");
            recordsNote.color = UiKitStyle.Dim;
            homeObject.FindProperty("_bestCampaign").objectReferenceValue = UiKitBuilder.AddValueRow(recordsPage, "Best campaign", "-");
            homeObject.FindProperty("_fastestWin").objectReferenceValue = UiKitBuilder.AddValueRow(recordsPage, "Fastest win", "-");
            homeObject.FindProperty("_bestEndless").objectReferenceValue = UiKitBuilder.AddValueRow(recordsPage, "Best endless", "-");
            homeObject.ApplyModifiedPropertiesWithoutUndo();

            UiKitBuilder.Wire(screen.GetComponent<UiScreen>(), "_firstSelected", playTab.Button);
            return home;
        }
    }
}
