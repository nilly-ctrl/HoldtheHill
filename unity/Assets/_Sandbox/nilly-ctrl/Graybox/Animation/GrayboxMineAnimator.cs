using HoldTheHill.Features.Combat;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Blinks the mine while it waits, and leaves an explosion behind when it detonates
    /// (the mine destroys itself in the same frame, so the blast is a separate object).
    /// </summary>
    // SpriteClipPlayer only: requiring ProximityMine too would make Unity auto-add a second,
    // default mine when this is attached before the real one.
    [RequireComponent(typeof(SpriteClipPlayer))]
    public class GrayboxMineAnimator : MonoBehaviour
    {
        // Fireball and smoke reach about 14 px from the centre; sized so they cover ~60% of the splash.
        private const float BlastPixels = 14f;
        private const float PixelsPerUnit = 32f;

        private ProximityMine _mine;
        private SpriteClipPlayer _player;

        public static GrayboxMineAnimator Attach(GameObject mine, SpriteAnimLibrary library)
        {
            var renderer = mine.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                renderer = mine.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = 2;
            }

            renderer.drawMode = SpriteDrawMode.Simple;
            renderer.color = Color.white;
            // Leaving Sliced mode makes Unity rescale the transform to keep the old on-screen size
            // (4x for a 0.8 placeholder square). The art is drawn at its real size, so undo that.
            mine.transform.localScale = Vector3.one;

            var player = mine.GetComponent<SpriteClipPlayer>();
            if (player == null) player = mine.AddComponent<SpriteClipPlayer>();
            player.Configure(library, "FxMine", "Armed");
            var animator = mine.GetComponent<GrayboxMineAnimator>();
            return animator != null ? animator : mine.AddComponent<GrayboxMineAnimator>();
        }

        private void Awake()
        {
            _mine = GetComponent<ProximityMine>();
            _player = GetComponent<SpriteClipPlayer>();
        }

        private void OnEnable()
        {
            if (_mine != null) _mine.Detonated += OnDetonated;
        }

        private void OnDisable()
        {
            if (_mine != null) _mine.Detonated -= OnDetonated;
        }

        private void OnDetonated()
        {
            GrayboxFeedback.RaiseMineExploded(transform.position);
            float scale = Mathf.Max(1f, _mine.SplashRadius * 0.6f * PixelsPerUnit / BlastPixels);
            SpriteClipPlayer.SpawnOneShot(_player.Library, "FxMine", "Explode", transform.position, Quaternion.identity, scale, 3);
        }
    }
}
