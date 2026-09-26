using UnityEngine;

namespace HoldTheHill.Core
{
    /// <summary>
    /// Shared placeholder art so features run before real sprites exist.
    /// </summary>
    public static class SpriteUtil
    {
        private static Sprite _square;

        /// <summary>A plain white square, one world unit across.</summary>
        public static Sprite Square
        {
            get
            {
                // Unity may unload the sprite when leaving Play mode, so recreate it if it's gone.
                if (_square != null)
                {
                    return _square;
                }

                var texture = new Texture2D(4, 4) { filterMode = FilterMode.Point };
                var pixels = new Color[16];
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = Color.white;
                }
                texture.SetPixels(pixels);
                texture.Apply();

                _square = Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
                return _square;
            }
        }

        // Domain reload is on in this project, but clearing the cache keeps Play mode safe if that ever changes.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _square = null;
        }
    }
}
