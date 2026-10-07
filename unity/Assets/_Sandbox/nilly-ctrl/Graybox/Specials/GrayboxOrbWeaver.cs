using System.Collections;
using System.Collections.Generic;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Towers;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Boss. Two tricks. She casts a thread to a point further along the route and walks it in
    /// a straight line, cutting the corner. And she cocoons the nearest tower, which cannot
    /// attack until the silk wears off or she dies.
    /// </summary>
    /// <remarks>
    /// Fire, or a Dewdrop Lens beam, landing on her while she is on a thread cuts it: she hangs
    /// there stunned and takes extra damage until she recovers, then finishes the crossing.
    /// </remarks>
    public class GrayboxOrbWeaver : GrayboxSpecialEnemy, IIncomingDamageModifier
    {
        private const int ThreadOrder = 1;

        [Header("Thread shortcut")]
        [SerializeField, Min(2f)] private float _threadInterval = 11f;
        [Tooltip("How far along the route, measured on the path, each thread carries her.")]
        [SerializeField, Min(1f)] private float _threadSkips = 7f;
        [SerializeField, Min(0.5f)] private float _threadSpeedScale = 1.5f;
        [SerializeField, Min(0.5f)] private float _cutStunSeconds = 2.5f;
        [SerializeField, Min(1f)] private float _cutDamageScale = 2f;

        [Header("Cocoon")]
        [SerializeField, Min(2f)] private float _cocoonInterval = 9f;
        [SerializeField, Min(0.5f)] private float _cocoonRange = 4.5f;
        [SerializeField, Min(0.5f)] private float _cocoonSeconds = 8f;

        private readonly List<Tower> _cocooned = new List<Tower>();
        private readonly List<SpriteClipPlayer> _thread = new List<SpriteClipPlayer>();
        private float _threadCooldown;
        private float _cocoonCooldown;
        private bool _busy;
        private bool _onThread;
        private float _hangingFor;
        private bool _threadCut;

        public bool IsOnThread => _onThread;

        /// <summary>True while she hangs stunned on a cut thread.</summary>
        public bool IsHanging => _hangingFor > 0f;

        protected override void OnEnable()
        {
            base.OnEnable();
            _threadCooldown = _threadInterval * 0.5f;
            _cocoonCooldown = _cocoonInterval * 0.35f;
            _busy = _onThread = false;
            _hangingFor = 0f;
        }

        public float ModifyIncomingDamage(EnemyHealth health, DamageInfo info)
        {
            // One cut per thread: a fire puddle under it would otherwise hold her there for good.
            if (_onThread && !_threadCut && CutsThread(info))
            {
                _threadCut = true;
                _hangingFor = _cutStunSeconds;
                Say("THREAD CUT", DamageNumberKind.Critical, transform.position);
                foreach (SpriteClipPlayer piece in _thread)
                {
                    if (piece != null)
                    {
                        piece.Play("Burn");
                    }
                }
            }

            return _hangingFor > 0f ? info.Amount * _cutDamageScale : info.Amount;
        }

        private static bool CutsThread(DamageInfo info)
        {
            if (info.Type == DamageType.Fire)
            {
                return true;
            }

            GameObject owner = CombatUtil.OwnerOf(info.Source);
            return owner != null && owner.GetComponent<ContinuousBeam>() != null;
        }

        private void Update()
        {
            if (_busy || !Alive)
            {
                return;
            }

            _threadCooldown -= Time.deltaTime;
            _cocoonCooldown -= Time.deltaTime;

            if (_cocoonCooldown <= 0f)
            {
                Tower tower = NearestTower(transform.position, _cocoonRange, t => !GrayboxTowerAffliction.Silenced(t));
                if (tower != null)
                {
                    StartCoroutine(Cocoon(tower));
                    return;
                }

                _cocoonCooldown = 1f;
            }

            if (_threadCooldown <= 0f)
            {
                float from = Mover.DistanceTravelled;
                float to = Mathf.Min(from + _threadSkips, PathLength() - 1.5f);
                // Only worth a thread if the straight line is a real saving over walking round.
                if (to - from > 2f && Vector2.Distance(PointAlongPath(from), PointAlongPath(to)) < (to - from) * 0.8f)
                {
                    StartCoroutine(WalkThread(to));
                    return;
                }

                _threadCooldown = 2f;
            }
        }

        private IEnumerator Cocoon(Tower tower)
        {
            _busy = true;
            float held = LeavePath();
            Face(tower.transform.position - transform.position);
            Play("Cocoon");
            yield return new WaitForSeconds(ClipLength("Cocoon", 0.68f));

            if (Alive && tower != null)
            {
                GrayboxTowerAffliction.Apply(tower, GrayboxTowerAffliction.Kind.Cocoon, _cocoonSeconds);
                _cocooned.Add(tower);
            }

            if (Alive)
            {
                ReturnToPath(held);
            }

            _cocoonCooldown = _cocoonInterval;
            _busy = false;
        }

        private IEnumerator WalkThread(float toDistance)
        {
            _busy = true;
            LeavePath();
            Vector2 start = transform.position;
            Vector2 end = PointAlongPath(toDistance);
            Face(end - start);
            Play("Spin");
            yield return new WaitForSeconds(ClipLength("Spin", 0.63f));

            LayThread(start, end);
            _threadCut = false;
            _onThread = true;
            SetLoop("ThreadWalk");
            while (Alive && Vector2.Distance(transform.position, end) > 0.05f)
            {
                if (_hangingFor > 0f)
                {
                    _hangingFor -= Time.deltaTime;
                }
                else
                {
                    float step = Mover.Speed * _threadSpeedScale * Health.SpeedMultiplier * Time.deltaTime;
                    Vector2 next = Vector2.MoveTowards(transform.position, end, step);
                    transform.position = new Vector3(next.x, next.y, transform.position.z);
                }

                yield return null;
            }

            _onThread = false;
            _hangingFor = 0f;
            ClearThread(snap: true);
            if (Alive)
            {
                ReturnToPath(toDistance);
                SetLoop("Walk");
            }

            _threadCooldown = _threadInterval;
            _busy = false;
        }

        // One tile of FxWebThread per unit of length, turned along the line, with a knot at each end.
        private void LayThread(Vector2 start, Vector2 end)
        {
            Vector2 along = end - start;
            float length = along.magnitude;
            Quaternion turn = Quaternion.Euler(0f, 0f, Mathf.Atan2(along.y, along.x) * Mathf.Rad2Deg);
            int pieces = Mathf.Max(1, Mathf.RoundToInt(length));
            for (int i = 0; i < pieces; i++)
            {
                Vector2 centre = start + along * ((i + 0.5f) / pieces);
                SpriteClipPlayer piece = GrayboxSpecials.LoopFx(Library, "FxWebThread", "Strand", null, centre, turn, ThreadOrder);
                if (piece != null)
                {
                    piece.transform.localScale = new Vector3(length / pieces, 1f, 1f);
                    _thread.Add(piece);
                }
            }

            foreach (Vector2 knot in new[] { start, end })
            {
                SpriteClipPlayer anchor = GrayboxSpecials.LoopFx(Library, "FxWebThread", "Anchor", null, knot, Quaternion.identity, ThreadOrder);
                if (anchor != null)
                {
                    _thread.Add(anchor);
                }
            }
        }

        private void ClearThread(bool snap)
        {
            foreach (SpriteClipPlayer piece in _thread)
            {
                if (piece == null)
                {
                    continue;
                }

                if (snap && piece.Has("Snap") && piece.DefaultClip == "Strand")
                {
                    SpriteClipPlayer.SpawnOneShot(piece.Library, "FxWebThread", "Snap", piece.transform.position, piece.transform.rotation, 1f, ThreadOrder);
                }

                Destroy(piece.gameObject);
            }

            _thread.Clear();
        }

        protected override void OnDied()
        {
            ClearThread(snap: true);
            foreach (Tower tower in _cocooned)
            {
                GrayboxTowerAffliction.Clear(tower, GrayboxTowerAffliction.Kind.Cocoon);
            }

            _cocooned.Clear();
        }

        private void OnDestroy()
        {
            ClearThread(snap: false);
        }
    }
}
