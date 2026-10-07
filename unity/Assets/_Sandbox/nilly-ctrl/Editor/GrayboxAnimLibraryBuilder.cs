using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Bakes every .aseprite under Animations/Aseprite into one <see cref="SpriteAnimLibrary"/>
    /// asset, by reading the sprite keyframes out of the clips the Aseprite Importer made.
    /// </summary>
    internal static class GrayboxAnimLibraryBuilder
    {
        private const string AsepriteRoot = "Assets/_Sandbox/nilly-ctrl/Animations/Aseprite";
        internal const string LibraryPath = "Assets/_Sandbox/nilly-ctrl/Graybox/GrayboxAnimLibrary.asset";

        [MenuItem("Tools/Hold the Hill/Rebuild Graybox Animation Library")]
        public static SpriteAnimLibrary Build()
        {
            var sets = new List<SpriteAnimSet>();
            string[] paths = AssetDatabase.FindAssets("", new[] { AsepriteRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.EndsWith(".aseprite"))
                .Distinct()
                .OrderBy(p => p)
                .ToArray();

            foreach (string path in paths)
            {
                var set = new SpriteAnimSet { Key = System.IO.Path.GetFileNameWithoutExtension(path) };
                foreach (AnimationClip clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>())
                {
                    SpriteAnimClip baked = Bake(clip);
                    if (baked != null)
                    {
                        set.Clips.Add(baked);
                    }
                }

                if (set.Clips.Count == 0)
                {
                    Debug.LogWarning($"[Graybox] {path} produced no animation clips. Is it imported with Generate Animation Clips on?");
                    continue;
                }

                sets.Add(set);
            }

            // "EnemyGruntMoves" holds extra clips for "EnemyGrunt" (same canvas and pivot), kept in
            // a second file only so the first is not rewritten. One set per enemy is what plays.
            const string movesSuffix = "Moves";
            foreach (SpriteAnimSet moves in sets.Where(s => s.Key.EndsWith(movesSuffix)).ToList())
            {
                string baseKey = moves.Key.Substring(0, moves.Key.Length - movesSuffix.Length);
                SpriteAnimSet target = sets.Find(s => s.Key == baseKey);
                if (target == null)
                {
                    continue;
                }

                foreach (SpriteAnimClip clip in moves.Clips)
                {
                    if (target.Find(clip.Name) == null)
                    {
                        target.Clips.Add(clip);
                    }
                }

                sets.Remove(moves);
            }

            var library = AssetDatabase.LoadAssetAtPath<SpriteAnimLibrary>(LibraryPath);
            if (library == null)
            {
                library = ScriptableObject.CreateInstance<SpriteAnimLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }

            library.Sets = sets;
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Graybox] Animation library: {sets.Count} sprites, {sets.Sum(s => s.Clips.Count)} clips -> {LibraryPath}");
            return AssetDatabase.LoadAssetAtPath<SpriteAnimLibrary>(LibraryPath);
        }

        private static SpriteAnimClip Bake(AnimationClip clip)
        {
            EditorCurveBinding binding = AnimationUtility.GetObjectReferenceCurveBindings(clip)
                .FirstOrDefault(b => b.type == typeof(SpriteRenderer) && b.propertyName == "m_Sprite");
            if (binding.type == null)
            {
                return null;
            }

            ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
            var frames = new List<Sprite>();
            var durations = new List<float>();

            for (int i = 0; i < keys.Length; i++)
            {
                float end = i + 1 < keys.Length ? keys[i + 1].time : clip.length;
                float duration = end - keys[i].time;
                var sprite = keys[i].value as Sprite;
                if (sprite == null || duration <= 0.0001f)
                {
                    continue; // the importer's closing key, which only holds the last frame's length
                }

                // The importer shares one sprite between identical frames; merging them keeps the timing.
                if (frames.Count > 0 && frames[frames.Count - 1] == sprite)
                {
                    durations[durations.Count - 1] += duration;
                    continue;
                }

                frames.Add(sprite);
                durations.Add(duration);
            }

            if (frames.Count == 0)
            {
                return null;
            }

            return new SpriteAnimClip
            {
                Name = clip.name,
                Frames = frames.ToArray(),
                Durations = durations.ToArray(),
                Loop = AnimationUtility.GetAnimationClipSettings(clip).loopTime,
            };
        }
    }
}
