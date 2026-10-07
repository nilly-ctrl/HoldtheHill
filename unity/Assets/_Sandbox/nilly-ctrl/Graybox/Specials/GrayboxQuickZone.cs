using System.Collections.Generic;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Enemies;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// A patch of ground that speeds up enemies crossing it, left by a dying silverfish.
    /// </summary>
    /// <remarks>
    /// It only touches a mover whose <see cref="EnemyMover.SpeedScale"/> is exactly 1, and only
    /// puts back the value it set, so it never fights a boss that is steering its own speed.
    /// </remarks>
    public class GrayboxQuickZone : MonoBehaviour
    {
        private const int GroundOrder = 1; // under the enemies
        private const float ArtRadius = 27f / 32f; // the ring in FxQuickZone is 27 px out

        private readonly List<IDamageable> _inside = new List<IDamageable>();
        private readonly HashSet<EnemyMover> _boosted = new HashSet<EnemyMover>();
        private readonly List<EnemyMover> _leaving = new List<EnemyMover>();
        private SpriteAnimLibrary _library;
        private float _left;
        private float _radius;
        private float _scale;

        public static GrayboxQuickZone Spawn(SpriteAnimLibrary library, Vector3 position, Quaternion rotation, float seconds, float radius, float speedScale)
        {
            SpriteClipPlayer visual = GrayboxSpecials.LoopFx(library, "FxQuickZone", "Loop", "Appear", position, rotation, GroundOrder);
            GameObject go = visual != null ? visual.gameObject : new GameObject("Quick Zone");
            go.transform.SetPositionAndRotation(position, rotation);
            go.transform.localScale = Vector3.one * (radius / ArtRadius);
            var zone = go.AddComponent<GrayboxQuickZone>();
            zone._library = library;
            zone._left = seconds;
            zone._radius = radius;
            zone._scale = speedScale;
            return zone;
        }

        public float Radius => _radius;

        private void Update()
        {
            _left -= Time.deltaTime;
            if (_left <= 0f)
            {
                SpriteClipPlayer.SpawnOneShot(_library, "FxQuickZone", "Fade", transform.position, transform.rotation, transform.localScale.x, GroundOrder);
                Destroy(gameObject);
                return;
            }

            CombatUtil.OverlapDamageables(transform.position, _radius, ~0, _inside);

            _leaving.Clear();
            foreach (EnemyMover mover in _boosted)
            {
                if (mover == null || !StillInside(mover))
                {
                    _leaving.Add(mover);
                }
            }

            foreach (EnemyMover mover in _leaving)
            {
                Release(mover);
            }

            foreach (IDamageable target in _inside)
            {
                var mover = target.Transform != null ? target.Transform.GetComponent<EnemyMover>() : null;
                if (mover != null && !_boosted.Contains(mover) && Mathf.Approximately(mover.SpeedScale, 1f))
                {
                    mover.SpeedScale = _scale;
                    _boosted.Add(mover);
                }
            }
        }

        private bool StillInside(EnemyMover mover)
        {
            foreach (IDamageable target in _inside)
            {
                if (target.Transform == mover.transform)
                {
                    return true;
                }
            }

            return false;
        }

        private void Release(EnemyMover mover)
        {
            _boosted.Remove(mover);
            if (mover != null && Mathf.Approximately(mover.SpeedScale, _scale))
            {
                mover.SpeedScale = 1f;
            }
        }

        private void OnDestroy()
        {
            foreach (EnemyMover mover in _boosted)
            {
                if (mover != null && Mathf.Approximately(mover.SpeedScale, _scale))
                {
                    mover.SpeedScale = 1f;
                }
            }

            _boosted.Clear();
        }
    }
}
