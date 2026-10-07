using System;
using System.Collections.Generic;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>One animation: the sprites in order and how long each one shows.</summary>
    [Serializable]
    public class SpriteAnimClip
    {
        public string Name;
        public Sprite[] Frames = Array.Empty<Sprite>();
        public float[] Durations = Array.Empty<float>();
        public bool Loop;

        public float Length
        {
            get
            {
                float total = 0f;
                foreach (float d in Durations)
                {
                    total += d;
                }

                return total;
            }
        }
    }

    /// <summary>Every clip cut from one .aseprite file, keyed by the file name (e.g. "TowerLinear").</summary>
    [Serializable]
    public class SpriteAnimSet
    {
        public string Key;
        public List<SpriteAnimClip> Clips = new List<SpriteAnimClip>();

        public SpriteAnimClip Find(string clipName)
        {
            foreach (SpriteAnimClip clip in Clips)
            {
                if (clip.Name == clipName)
                {
                    return clip;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// The graybox's animation data, baked from Animations/Aseprite by GrayboxBuilder.
    /// </summary>
    /// <remarks>
    /// Baked into plain sprite arrays rather than used through Animator controllers, so
    /// objects created at runtime (towers from GrayboxTowerPlacer, death and blast effects)
    /// can play any clip from one reference, without an AnimatorController per sprite.
    /// </remarks>
    public class SpriteAnimLibrary : ScriptableObject
    {
        public List<SpriteAnimSet> Sets = new List<SpriteAnimSet>();

        public SpriteAnimSet Find(string key)
        {
            foreach (SpriteAnimSet set in Sets)
            {
                if (set.Key == key)
                {
                    return set;
                }
            }

            return null;
        }
    }
}
