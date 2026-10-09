using System.Collections.Generic;
using HoldTheHill.Sandbox.UiKit;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Generates every menu screen (title, home, pause, run end, settings, controls) from the UI
    /// kit as one prefab, and drops it into the graybox scene.
    /// Menu: Tools > Hold the Hill > Build Graybox Menus.
    /// </summary>
    /// <remarks>
    /// Kept apart from <see cref="GrayboxBuilder"/> so menu layout changes do not touch the scene
    /// builder. Needs the kit prefabs (Tools > Hold the Hill > Build UI Kit).
    /// </remarks>
    public static class GrayboxMenusBuilder
    {
        public const string PrefabPath = "Assets/_Sandbox/nilly-ctrl/Graybox/Prefabs/GrayboxMenus.prefab";

        private const string TitleBackgroundPath = "Assets/_Sandbox/nilly-ctrl/Ui/Title/TitleBackground.png";
        private const string TitleLogoPath = "Assets/_Sandbox/nilly-ctrl/Icons/PNG/TitleLogo.png";

        // More than the game has, so a new achievement shows without rebuilding the prefab.
        private const int AwardRows = 12;

        [MenuItem("Tools/Hold the Hill/Build Graybox Menus")]
        public static GameObject BuildPrefab()
        {
            GameObject root = UiKitBuilder.InstantiatePrefab("UiRoot", null);
            if (root == null)
            {
                return null;
            }

            root.name = "GrayboxMenus";
            Transform canvas = root.transform;

            // Order is draw order. Settings and Controls open over the screen that asked for them,
            // and the dialog comes last so it sits over whichever screen asked.
            GrayboxTitleScreen title = BuildTitle(canvas);
            GrayboxHomeScreen home = BuildHome(canvas);
            GrayboxPauseScreen pause = BuildPause(canvas);
            GrayboxRunEndScreen runEnd = BuildRunEnd(canvas);
            GrayboxSettingsScreen settings = BuildSettings(canvas);
            GrayboxControlsScreen controls = BuildControls(canvas);
            var dialog = UiKitBuilder.InstantiatePrefab("ConfirmDialog", canvas).GetComponent<UiConfirmDialog>();

            foreach (Object screen in new Object[] { title, pause })
            {
                UiKitBuilder.Wire(screen, "_dialog", dialog);
                UiKitBuilder.Wire(screen, "_settingsScreen", settings.GetComponent<UiScreen>());
                UiKitBuilder.Wire(screen, "_controlsScreen", controls.GetComponent<UiScreen>());
            }

            var menus = root.AddComponent<GrayboxMenus>();
            UiKitBuilder.Wire(menus, "_title", title);
            UiKitBuilder.Wire(menus, "_home", home);
            UiKitBuilder.Wire(menus, "_pause", pause);
            UiKitBuilder.Wire(menus, "_runEnd", runEnd);
            UiKitBuilder.Wire(menus, "_settings", settings);
            UiKitBuilder.Wire(menus, "_controls", controls);
            UiKitBuilder.Wire(menus, "_dialog", dialog);

            EnsureFolder("Assets/_Sandbox/nilly-ctrl/Graybox", "Prefabs");
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        /// <summary>Puts the menus, and an event system if the scene has none, into the open scene.</summary>
        public static void AddToScene()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                prefab = BuildPrefab();
            }

            if (prefab == null)
            {
                Debug.LogError("[Graybox] No menus: build the UI kit first (Tools > Hold the Hill > Build UI Kit).");
                return;
            }

            if (Object.FindAnyObjectByType<EventSystem>() == null)
            {
                UiKitBuilder.InstantiatePrefab("UiEventSystem", null);
            }

            PrefabUtility.InstantiatePrefab(prefab);
        }

        // ---------- title ----------

        // Laid out after Ui/Title/Source~/TitleMockup.png: the hill on the left, the logo and the
        // buttons on the right.
        private static GrayboxTitleScreen BuildTitle(Transform canvas)
        {
            RectTransform screen = UiKitBuilder.AddScreen(canvas, "TitleScreen", dim: false);

            // The backdrop is 320x180 and fills the 640x360 layout at exactly 2x.
            Image backdrop = UiKitBuilder.AddPicture(screen, "Backdrop", PixelSprite(TitleBackgroundPath), Vector2.zero);
            RectTransform backdropRect = backdrop.rectTransform;
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.offsetMin = backdropRect.offsetMax = Vector2.zero;
            backdrop.raycastTarget = true; // clicks on the picture must not reach the game behind

            Image logo = UiKitBuilder.AddPicture(screen, "Logo", PixelSprite(TitleLogoPath), new Vector2(336f, 196f)); // 2x, like the backdrop
            Place(logo.rectTransform, new Vector2(1f, 1f), new Vector2(-16f, -8f));

            RectTransform column = UiKitBuilder.AddColumn(screen, "Buttons", 168f);
            Place(column, new Vector2(1f, 0f), new Vector2(-100f, 30f));

            UiButton play = UiKitBuilder.AddButton(column, "Play");
            UiButton settings = UiKitBuilder.AddButton(column, "Settings");
            UiButton controls = UiKitBuilder.AddButton(column, "Controls");
            UiButton quit = UiKitBuilder.AddButton(column, "Quit");

            var title = screen.gameObject.AddComponent<GrayboxTitleScreen>();
            UiKitBuilder.Wire(title, "_play", play);
            UiKitBuilder.Wire(title, "_settings", settings);
            UiKitBuilder.Wire(title, "_controls", controls);
            UiKitBuilder.Wire(title, "_quit", quit);
            UiKitBuilder.Wire(screen.GetComponent<UiScreen>(), "_firstSelected", play.Button);
            return title;
        }

        // ---------- home ----------

        private static GrayboxHomeScreen BuildHome(Transform canvas)
        {
            RectTransform screen = UiKitBuilder.AddScreen(canvas, "HomeScreen", dim: false);

            Image backdrop = UiKitBuilder.AddPicture(screen, "Backdrop", PixelSprite(TitleBackgroundPath), Vector2.zero);
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

            var home = screen.gameObject.AddComponent<GrayboxHomeScreen>();
            UiKitBuilder.Wire(home, "_honeydew", honeydew);

            // Play
            UiStepper level = UiKitBuilder.AddStepper(playPage, "Level", new[] { GrayboxLevels.All[0].Name }, 0);
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
            foreach (GrayboxUpgrades.Upgrade upgrade in GrayboxUpgrades.All)
            {
                UiButton buy = UiKitBuilder.AddButtonRow(upgradesPage, upgrade.Name, "Buy", out TextMeshProUGUI name);
                buy.transform.parent.name = Tidy(upgrade.Id) + "Row";
                TextMeshProUGUI effect = UiKitBuilder.AddBody(upgradesPage, "Next: " + GrayboxUpgrades.Describe(upgrade, 1));
                effect.name = Tidy(upgrade.Id) + "Effect";
                effect.alignment = TextAlignmentOptions.Left;
                effect.color = UiKitStyle.Dim;

                SerializedProperty row = Append(upgradeRows);
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
            for (int i = 0; i < AwardRows; i++)
            {
                TextMeshProUGUI value = UiKitBuilder.AddValueRow(awardsPage, "Award " + (i + 1), "0/1");
                SerializedProperty row = Append(awardRows);
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

        // ---------- pause and run end ----------

        private static GrayboxPauseScreen BuildPause(Transform canvas)
        {
            RectTransform screen = UiKitBuilder.AddScreen(canvas, "PauseScreen");
            RectTransform panel = UiKitBuilder.AddPanel(screen, 200f);

            UiKitBuilder.AddTitle(panel, "Paused");
            TextMeshProUGUI subtitle = UiKitBuilder.AddBody(panel, "Wave 1 of 4");
            subtitle.color = UiKitStyle.Dim;
            UiKitBuilder.AddDivider(panel);

            UiButton resume = UiKitBuilder.AddButton(panel, "Resume");
            UiButton restart = UiKitBuilder.AddButton(panel, "Restart run");
            UiButton settings = UiKitBuilder.AddButton(panel, "Settings");
            UiButton controls = UiKitBuilder.AddButton(panel, "Controls");
            UiButton quit = UiKitBuilder.AddButton(panel, "Quit to Home");

            var pause = screen.gameObject.AddComponent<GrayboxPauseScreen>();
            UiKitBuilder.Wire(pause, "_subtitle", subtitle);
            UiKitBuilder.Wire(pause, "_resume", resume);
            UiKitBuilder.Wire(pause, "_restart", restart);
            UiKitBuilder.Wire(pause, "_settings", settings);
            UiKitBuilder.Wire(pause, "_controls", controls);
            UiKitBuilder.Wire(pause, "_quit", quit);
            UiKitBuilder.Wire(screen.GetComponent<UiScreen>(), "_firstSelected", resume.Button);
            return pause;
        }

        private static GrayboxRunEndScreen BuildRunEnd(Transform canvas)
        {
            RectTransform screen = UiKitBuilder.AddScreen(canvas, "RunEndScreen");
            RectTransform panel = UiKitBuilder.AddPanel(screen, 240f);

            TextMeshProUGUI title = UiKitBuilder.AddTitle(panel, "Hill overrun");
            TextMeshProUGUI newBest = UiKitBuilder.AddBody(panel, "New best!");
            newBest.name = "NewBest";
            newBest.color = UiKitStyle.Gold;
            UiKitBuilder.AddDivider(panel);

            TextMeshProUGUI wave = UiKitBuilder.AddValueRow(panel, "Wave reached", "1 of 4");
            TextMeshProUGUI kills = UiKitBuilder.AddValueRow(panel, "Kills", "0");
            TextMeshProUGUI food = UiKitBuilder.AddValueRow(panel, "Food earned", "0");
            TextMeshProUGUI towers = UiKitBuilder.AddValueRow(panel, "Towers standing", "0");
            TextMeshProUGUI time = UiKitBuilder.AddValueRow(panel, "Time", "0:00");
            TextMeshProUGUI retries = UiKitBuilder.AddValueRow(panel, "Wave retries used", "0");
            TextMeshProUGUI best = UiKitBuilder.AddValueRow(panel, "Best on this hill", "Wave 1");
            best.color = UiKitStyle.Gold;
            TextMeshProUGUI honeydew = UiKitBuilder.AddValueRow(panel, GrayboxUpgrades.CurrencyName + " earned", "0");
            honeydew.color = UiKitStyle.Gold;
            UiKitBuilder.AddDivider(panel);

            UiButton retryWave = UiKitBuilder.AddButton(panel, "Retry wave 1");
            retryWave.name = "RetryWaveButton";
            UiButton restart = UiKitBuilder.AddButton(panel, "Restart from wave 1");
            restart.name = "RestartButton";
            UiButton quit = UiKitBuilder.AddButton(panel, "Quit to Home");

            var runEnd = screen.gameObject.AddComponent<GrayboxRunEndScreen>();
            UiKitBuilder.Wire(runEnd, "_title", title);
            UiKitBuilder.Wire(runEnd, "_newBest", newBest);
            UiKitBuilder.Wire(runEnd, "_wave", wave);
            UiKitBuilder.Wire(runEnd, "_kills", kills);
            UiKitBuilder.Wire(runEnd, "_food", food);
            UiKitBuilder.Wire(runEnd, "_towers", towers);
            UiKitBuilder.Wire(runEnd, "_time", time);
            UiKitBuilder.Wire(runEnd, "_retries", retries);
            UiKitBuilder.Wire(runEnd, "_best", best);
            UiKitBuilder.Wire(runEnd, "_honeydew", honeydew);
            UiKitBuilder.Wire(runEnd, "_retryWave", retryWave);
            UiKitBuilder.Wire(runEnd, "_restart", restart);
            UiKitBuilder.Wire(runEnd, "_quit", quit);
            return runEnd;
        }

        // ---------- settings ----------

        private static GrayboxSettingsScreen BuildSettings(Transform canvas)
        {
            RectTransform screen = UiKitBuilder.AddScreen(canvas, "SettingsScreen");
            RectTransform panel = UiKitBuilder.AddPanel(screen, 300f);
            UiKitBuilder.AddTitle(panel, "Settings");

            UiTabBar tabs = UiKitBuilder.AddTabBar(panel);
            RectTransform audio = UiKitBuilder.AddPage(panel, "AudioPage");
            RectTransform display = UiKitBuilder.AddPage(panel, "DisplayPage");
            RectTransform gameplay = UiKitBuilder.AddPage(panel, "GameplayPage");
            RectTransform access = UiKitBuilder.AddPage(panel, "AccessPage");
            UiButton audioTab = UiKitBuilder.AddTab(tabs, "Audio", audio.gameObject);
            UiKitBuilder.AddTab(tabs, "Display", display.gameObject);
            UiKitBuilder.AddTab(tabs, "Gameplay", gameplay.gameObject);
            UiKitBuilder.AddTab(tabs, "Access", access.gameObject);

            var defaults = new SettingsSave();
            var settings = screen.gameObject.AddComponent<GrayboxSettingsScreen>();

            UiKitBuilder.Wire(settings, "_master", UiKitBuilder.AddSlider(audio, "Master volume", defaults.masterVolume));
            UiKitBuilder.Wire(settings, "_music", UiKitBuilder.AddSlider(audio, "Music", defaults.musicVolume));
            UiKitBuilder.Wire(settings, "_sfx", UiKitBuilder.AddSlider(audio, "Sound effects", defaults.sfxVolume));
            UiKitBuilder.Wire(settings, "_mute", UiKitBuilder.AddToggle(audio, "Mute everything", defaults.muted));

            UiKitBuilder.Wire(settings, "_fullscreen", UiKitBuilder.AddToggle(display, "Fullscreen", defaults.fullscreen));
            UiKitBuilder.Wire(settings, "_resolution", UiKitBuilder.AddStepper(display, "Resolution", new[] { "Native" }, 0));
            UiKitBuilder.Wire(settings, "_vsync", UiKitBuilder.AddToggle(display, "Vertical sync", defaults.vsync));

            UiKitBuilder.Wire(settings, "_autoStart", UiKitBuilder.AddToggle(gameplay, "Start waves automatically", defaults.autoStartWaves));
            UiKitBuilder.Wire(settings, "_damageNumbers", UiKitBuilder.AddToggle(gameplay, "Damage numbers", defaults.showDamageNumbers));
            UiKitBuilder.Wire(settings, "_healthBars", UiKitBuilder.AddToggle(gameplay, "Enemy health bars", defaults.showHealthBars));
            UiKitBuilder.Wire(settings, "_rangeRings", UiKitBuilder.AddToggle(gameplay, "Tower range rings", defaults.showRangeRings));

            UiKitBuilder.Wire(settings, "_uiSize", UiKitBuilder.AddStepper(access, "Menu size", new[] { "Small", "Normal", "Large" }, 1));
            UiKitBuilder.Wire(settings, "_pauseOnFocusLoss", UiKitBuilder.AddToggle(access, "Pause when the window loses focus", defaults.pauseOnFocusLoss));

            UiKitBuilder.AddDivider(panel);
            RectTransform buttons = UiKitBuilder.AddButtonRow(panel);
            UiKitBuilder.Wire(settings, "_defaults", UiKitBuilder.AddButton(buttons, "Defaults"));
            UiKitBuilder.Wire(settings, "_done", UiKitBuilder.AddButton(buttons, "Done"));

            UiKitBuilder.Wire(screen.GetComponent<UiScreen>(), "_firstSelected", audioTab.Button);
            return settings;
        }

        // ---------- controls ----------

        private static GrayboxControlsScreen BuildControls(Transform canvas)
        {
            RectTransform screen = UiKitBuilder.AddScreen(canvas, "ControlsScreen");
            RectTransform panel = UiKitBuilder.AddPanel(screen, 320f);
            UiKitBuilder.AddTitle(panel, "Controls");

            UiTabBar tabs = UiKitBuilder.AddTabBar(panel);
            var controls = screen.gameObject.AddComponent<GrayboxControlsScreen>();
            var controlsObject = new SerializedObject(controls);
            SerializedProperty rows = controlsObject.FindProperty("_rows");

            // One tab per page name, in the order the entries first use it.
            var pages = new Dictionary<string, RectTransform>();
            UiButton firstTab = null;
            foreach (GrayboxControls.Entry entry in GrayboxControls.Entries)
            {
                if (!pages.TryGetValue(entry.Page, out RectTransform page))
                {
                    page = UiKitBuilder.AddPage(panel, Tidy(entry.Page) + "Page");
                    pages.Add(entry.Page, page);
                    UiButton tab = UiKitBuilder.AddTab(tabs, entry.Page, page.gameObject);
                    if (firstTab == null) firstTab = tab;
                }

                UiButton button = UiKitBuilder.AddButtonRow(page, entry.Label, GrayboxBindingText.For(entry.DefaultKey), out TextMeshProUGUI label);
                button.transform.parent.name = entry.Id + "Row";

                SerializedProperty row = Append(rows);
                row.FindPropertyRelative("Id").stringValue = entry.Id;
                row.FindPropertyRelative("Label").objectReferenceValue = label;
                row.FindPropertyRelative("Button").objectReferenceValue = button;
            }

            UiKitBuilder.AddDivider(panel);
            TextMeshProUGUI message = UiKitBuilder.AddBody(panel, "Press a row to change its key.");
            message.name = "Message";
            message.color = UiKitStyle.Dim;
            TextMeshProUGUI device = UiKitBuilder.AddBody(panel, "Keyboard.");
            device.name = "Device";
            device.color = UiKitStyle.Dim;

            RectTransform buttons = UiKitBuilder.AddButtonRow(panel);
            UiButton reset = UiKitBuilder.AddButton(buttons, "Reset keys");
            UiButton done = UiKitBuilder.AddButton(buttons, "Done");

            controlsObject.FindProperty("_message").objectReferenceValue = message;
            controlsObject.FindProperty("_device").objectReferenceValue = device;
            controlsObject.FindProperty("_reset").objectReferenceValue = reset;
            controlsObject.FindProperty("_done").objectReferenceValue = done;
            controlsObject.ApplyModifiedPropertiesWithoutUndo();

            UiKitBuilder.Wire(screen.GetComponent<UiScreen>(), "_firstSelected", firstTab.Button);
            return controls;
        }

        // ---------- plumbing ----------

        // Anchors and pivots a rect to one corner of its parent, then offsets it from there.
        private static void Place(RectTransform rect, Vector2 corner, Vector2 offset)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = corner;
            rect.anchoredPosition = offset;
        }

        private static SerializedProperty Append(SerializedProperty array)
        {
            array.arraySize++;
            return array.GetArrayElementAtIndex(array.arraySize - 1);
        }

        private static string Tidy(string text)
        {
            var builder = new System.Text.StringBuilder();
            bool upper = true;
            foreach (char c in text)
            {
                if (!char.IsLetterOrDigit(c))
                {
                    upper = true;
                    continue;
                }

                builder.Append(upper ? char.ToUpperInvariant(c) : c);
                upper = false;
            }

            return builder.ToString();
        }

        // Pixel art for the canvas: a sprite, point-sampled, uncompressed, no mipmaps.
        private static Sprite PixelSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[Graybox] Menu art not found at {path}.");
                return null;
            }

            if (importer.textureType != TextureImporterType.Sprite || importer.filterMode != FilterMode.Point
                || importer.textureCompression != TextureImporterCompression.Uncompressed || importer.mipmapEnabled)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder($"{parent}/{child}"))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
