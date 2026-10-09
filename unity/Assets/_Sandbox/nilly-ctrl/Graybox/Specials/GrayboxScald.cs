using System.Collections.Generic;
using HoldTheHill.Features.Towers;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The boiling puddle a bombardier beetle leaves. Towers within reach are silenced for as
    /// long as it lasts.
    /// </summary>
    public class GrayboxScald : MonoBehaviour
    {
        private const int GroundOrder = 1;
        private const float Tick = 0.25f;
        private static readonly List<Tower> Towers = new List<Tower>();

        private SpriteAnimLibrary _library;
        private float _left;
        private float _radius;
        private float _tick;

        public static GrayboxScald Spawn(SpriteAnimLibrary library, Vector3 position, float seconds, float radius)
        {
            SpriteClipPlayer visual = GrayboxSpecials.LoopFx(library, "FxScald", "Boil", "Burst", position, Quaternion.identity, GroundOrder);
            GameObject go = visual != null ? visual.gameObject : new GameObject("Scald");
            go.transform.position = position;
            var scald = go.AddComponent<GrayboxScald>();
            scald._library = library;
            scald._left = seconds;
            scald._radius = radius;
            return scald;
        }

        public float Radius => _radius;

        private void Update()
        {
            _left -= Time.deltaTime;
            if (_left <= 0f)
            {
                SpriteClipPlayer.SpawnOneShot(_library, "FxScald", "Fade", transform.position, Quaternion.identity, 1f, GroundOrder);
                Destroy(gameObject);
                return;
            }

            _tick -= Time.deltaTime;
            if (_tick > 0f)
            {
                return;
            }

            _tick = Tick;
            Tower.GetActive(Towers);
            foreach (Tower tower in Towers)
            {
                if (((Vector2)tower.transform.position - (Vector2)transform.position).sqrMagnitude <= _radius * _radius)
                {
                    // A little longer than one tick, so the silence holds between ticks and ends with the puddle.
                    GrayboxTowerAffliction.Apply(tower, GrayboxTowerAffliction.Kind.Scald, Tick * 2f);
                }
            }
        }
    }
}
