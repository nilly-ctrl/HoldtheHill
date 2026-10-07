using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Turns the baked pixel-font atlases in Fonts/Atlases (PNG + JSON from build_pixel_fonts.py)
    /// into one <see cref="PixelFontStyle"/> asset per style in Fonts/Generated, for the floating
    /// damage numbers. The themed sets in Atlases/Themes/&lt;Theme&gt; become Generated/Themes/&lt;Theme&gt;.
    /// HUD text uses the TTFs in Fonts/TTF and does not need these.
    /// </summary>
    public static class PixelFontBuilder
    {
        private const string FontsFolder = "Assets/_Sandbox/nilly-ctrl/Fonts";
        private const string AtlasFolder = FontsFolder + "/Atlases";
        private const string OutputFolder = FontsFolder + "/Generated";

        // Filled by JsonUtility, so the compiler never sees it assigned.
#pragma warning disable 0649
        [System.Serializable]
        private class FontData
        {
            public string style;
        }
#pragma warning restore 0649

        [MenuItem("Hold the Hill/Sandbox/Build Pixel Fonts")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(OutputFolder))
            {
                AssetDatabase.CreateFolder(FontsFolder, "Generated");
            }

            int built = 0;
            foreach (string file in Directory.GetFiles(AtlasFolder, "*.json", SearchOption.AllDirectories))
            {
                string jsonPath = file.Replace('\\', '/');
                // Atlases/Themes/Neon/Hud.json -> Generated/Themes/Neon/HudStyle.asset
                string folder = OutputFolder + jsonPath.Substring(0, jsonPath.LastIndexOf('/')).Substring(AtlasFolder.Length);
                EnsureFolder(folder);
                FontData data = JsonUtility.FromJson<FontData>(File.ReadAllText(jsonPath));
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Path.ChangeExtension(jsonPath, ".png"));
                if (data == null || string.IsNullOrEmpty(data.style) || texture == null)
                {
                    Debug.LogWarning($"PixelFontBuilder: skipped {jsonPath} (missing atlas or bad JSON)");
                    continue;
                }

                PixelFontStyle style = LoadOrCreate($"{folder}/{data.style}Style.asset",
                    ScriptableObject.CreateInstance<PixelFontStyle>);
                var styleSerialized = new SerializedObject(style);
                styleSerialized.FindProperty("_atlas").objectReferenceValue = texture;
                styleSerialized.FindProperty("_glyphData").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(jsonPath);
                styleSerialized.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(style);
                built++;
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"PixelFontBuilder: built {built} pixel font styles in {OutputFolder}");
        }

        [MenuItem("Hold the Hill/Sandbox/Add Damage Numbers To Scene")]
        public static void AddSpawnerToScene()
        {
            Build();

            var existing = Object.FindAnyObjectByType<DamageNumberSpawner>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                Debug.Log("PixelFontBuilder: the scene already has a DamageNumberSpawner; selected it.", existing);
                return;
            }

            var go = new GameObject("Damage Numbers");
            Configure(go.AddComponent<DamageNumberSpawner>());
            Undo.RegisterCreatedObjectUndo(go, "Add Damage Numbers");
            EditorSceneManager.MarkSceneDirty(go.scene);
            Selection.activeGameObject = go;
        }

        /// <summary>
        /// Assigns every font style and the sprite shader to a spawner. Call Build() first.
        /// Pass an art theme's name ("Neon", "Military"...) to use that theme's set.
        /// </summary>
        public static void Configure(DamageNumberSpawner spawner, int pixelScale = 1, string theme = null)
        {
            string folder = string.IsNullOrEmpty(theme) ? OutputFolder : $"{OutputFolder}/Themes/{theme}";
            var serialized = new SerializedObject(spawner);
            string[,] slots =
            {
                { "_physical", "DamageNormal" }, { "_magic", "DamageMagic" }, { "_true", "DamageTrue" },
                { "_fire", "DamageFire" }, { "_poison", "DamagePoison" }, { "_lightning", "DamageLightning" },
                { "_critical", "DamageCrit" }, { "_heal", "Heal" }, { "_resource", "Resource" },
            };
            for (int i = 0; i < slots.GetLength(0); i++)
            {
                serialized.FindProperty(slots[i, 0]).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<PixelFontStyle>($"{folder}/{slots[i, 1]}Style.asset");
            }

            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            serialized.FindProperty("_shader").objectReferenceValue = shader != null ? shader : Shader.Find("Sprites/Default");
            serialized.FindProperty("_pixelScale").intValue = Mathf.Max(1, pixelScale);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            int slash = folder.LastIndexOf('/');
            EnsureFolder(folder.Substring(0, slash));
            AssetDatabase.CreateFolder(folder.Substring(0, slash), folder.Substring(slash + 1));
        }

        private static T LoadOrCreate<T>(string path, System.Func<T> create) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = create();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
