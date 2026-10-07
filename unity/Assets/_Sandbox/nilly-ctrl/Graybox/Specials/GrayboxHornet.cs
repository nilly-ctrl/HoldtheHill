using System.Collections;
using System.Collections.Generic;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Towers;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Boss, and a flyer. It ignores the path and goes for towers: it picks the one that has
    /// done the most damage, hovers beside it and marks it, then dives and carries it to the
    /// burrow. Hurt it enough while it is marking and it is shaken off. Kill it while it is
    /// carrying and the tower is put back; once it reaches the burrow the tower is gone until
    /// the next wave starts.
    /// </summary>
    /// <remarks>
    /// The mover is held still (SpeedScale 0) while it hunts, and let go to fly at the hill
    /// only when there is no tower left to take.
    /// </remarks>
    public class GrayboxHornet : GrayboxFlyer
    {
        [SerializeField, Min(0.1f)] private float _flySpeed = 2.6f;
        [SerializeField, Min(0.1f)] private float _carrySpeed = 1.5f;
        [SerializeField, Min(0.2f)] private float _hoverDistance = 1.3f;
        [SerializeField, Min(0.5f)] private float _markSeconds = 3f;
        // Measured on the graybox map with level 1 towers, damage taken in one 3 s mark:
        //   nothing else on the field: about 40 from the marked tower alone, 60 with a neighbour;
        //   during its own wave, with towers busy on other enemies: 6, 15, 24, 32, 36, 42.
        // 2% of 1800 health (36) sits in the upper part of that spread. Played on its own wave it
        // takes two busy towers, then is shaken off again and again by two that cover each other,
        // and dies about 70 s in. At 1.7% it never took a tower; at 2.5% it was never shaken off.
        // Retune if tower damage or its health changes.
        [Tooltip("Share of its full health it must lose while marking to be shaken off.")]
        [SerializeField, Range(0.005f, 0.5f)] private float _shakeOffFraction = 0.02f;
        [SerializeField, Min(0f)] private float _restSeconds = 1.2f;
        [Tooltip("How long it backs off after being shaken off, before it tries again.")]
        [SerializeField, Min(0f)] private float _shakenRestSeconds = 3f;
        [Tooltip("Share of its full health a shake-off costs it, on top of the damage that caused it.")]
        [SerializeField, Range(0f, 0.5f)] private float _shakeOffWound = 0.06f;

        private Tower _target;
        private Tower _carried;
        private Tower _shookItOff;
        private bool _marking;
        private float _hurtWhileMarking;

        public Tower Carried => _carried;

        /// <summary>The tower it is going for or marking right now.</summary>
        public Tower Target => _target;

        public bool IsMarking => _marking;

        /// <summary>Health it has lost since it started marking. Reaches <see cref="ShakeOffDamage"/> and it lets go.</summary>
        public float HurtWhileMarking => _hurtWhileMarking;

        /// <summary>Damage it must take during one mark to be shaken off.</summary>
        public float ShakeOffDamage => Health != null ? Health.MaxHealth * _shakeOffFraction : 0f;

        protected override void Start()
        {
            base.Start();
            Mover.SpeedScale = 0f;
            StartCoroutine(Hunt());
        }

        protected override void OnHurt(DamageInfo info, float applied)
        {
            if (_marking)
            {
                _hurtWhileMarking += applied;
            }
        }

        protected override void OnDied()
        {
            if (_target != null)
            {
                GrayboxTowerAffliction.SetMarked(_target, false);
            }

            if (_carried != null)
            {
                Say("TOWER RESCUED", DamageNumberKind.Heal, _carried.transform.position);
                GrayboxTowerAffliction.Return(_carried);
                _carried = null;
            }
        }

        private static bool Standing(Tower tower) => tower != null && tower.gameObject.activeInHierarchy;

        // The tower that has hurt the wave most; the nearest one breaks a tie (and an untouched map).
        private Tower PickTarget()
        {
            Tower best = null;
            float bestScore = float.NegativeInfinity;
            foreach (Tower tower in ActiveTowers())
            {
                // It does not go straight back to the tower that just beat it: it tries the next best.
                if (tower == _shookItOff)
                {
                    continue;
                }

                float score = tower.TotalDamageDealt - Vector2.Distance(tower.transform.position, transform.position) * 0.01f;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = tower;
                }
            }

            if (best == null && Standing(_shookItOff))
            {
                best = _shookItOff; // the only tower left
            }

            return best;
        }

        private IEnumerator Hunt()
        {
            yield return new WaitForSeconds(ClipLength("Spawn", 0.5f));

            while (Alive)
            {
                _target = PickTarget();
                if (_target == null)
                {
                    // Nothing to take: make for the hill, and look again in a moment.
                    Mover.SkipToLastWaypoint();
                    Mover.SpeedScale = 1f;
                    IReadOnlyList<Vector3> points = Waypoints;
                    if (points != null && points.Count > 0)
                    {
                        FlipToward(points[points.Count - 1] - transform.position);
                    }

                    yield return new WaitForSeconds(1f);
                    continue;
                }

                Mover.SpeedScale = 0f;
                while (Alive && Standing(_target) && Vector2.Distance(transform.position, _target.transform.position) > _hoverDistance)
                {
                    FlyToward(_target.transform.position, _flySpeed, _hoverDistance);
                    yield return null;
                }

                if (!Standing(_target))
                {
                    continue;
                }

                // Mark: the warning, and the window to shake it off.
                _marking = true;
                _hurtWhileMarking = 0f;
                FlipToward(_target.transform.position - transform.position);
                GrayboxTowerAffliction.SetMarked(_target, true);
                Play("Mark");
                float waited = 0f;
                bool shaken = false;
                while (Alive && Standing(_target) && waited < _markSeconds)
                {
                    if (_hurtWhileMarking >= ShakeOffDamage)
                    {
                        shaken = true;
                        break;
                    }

                    waited += Time.deltaTime;
                    yield return null;
                }

                _marking = false;
                if (shaken || !Standing(_target))
                {
                    if (_target != null)
                    {
                        GrayboxTowerAffliction.SetMarked(_target, false);
                    }

                    if (shaken && _target != null)
                    {
                        _shookItOff = _target;
                        if (_shakeOffWound > 0f)
                        {
                            Health.TakeDamage(new DamageInfo(Health.MaxHealth * _shakeOffWound, gameObject, transform.position, DamageType.True));
                        }

                        Play("Flinch"); // after the wound, so its Hurt clip does not replace the flinch
                        Say("SHAKEN OFF", DamageNumberKind.Critical, transform.position);

                        Vector2 away = ((Vector2)transform.position - (Vector2)_target.transform.position).normalized;
                        Vector2 retreat = (Vector2)transform.position + away * 1.2f;
                        float flinch = ClipLength("Flinch", 0.45f);
                        for (float t = 0f; t < flinch && Alive; t += Time.deltaTime)
                        {
                            Vector2 next = Vector2.MoveTowards(transform.position, retreat, 3f * Time.deltaTime);
                            transform.position = new Vector3(next.x, next.y, transform.position.z);
                            yield return null;
                        }
                    }

                    yield return new WaitForSeconds(shaken ? _shakenRestSeconds : _restSeconds);
                    continue;
                }

                // Dive onto it.
                Play("Dive");
                float dive = ClipLength("Dive", 0.56f);
                for (float t = 0f; t < dive && Alive && Standing(_target); t += Time.deltaTime)
                {
                    FlyToward(_target.transform.position, _hoverDistance / dive);
                    yield return null;
                }

                if (!Alive || !Standing(_target))
                {
                    continue;
                }

                _shookItOff = null; // a successful raid, and the grudge is forgotten
                Say("TOWER TAKEN", DamageNumberKind.Fire, _target.transform.position);
                GrayboxTowerAffliction.CarryOff(_target);
                _carried = _target;
                _target = null;
                SetLoop("Carry");

                IReadOnlyList<Vector3> route = Waypoints;
                Vector2 home = route != null && route.Count > 0 ? (Vector2)route[0] : (Vector2)transform.position;
                while (Alive && !FlyToward(home, _carrySpeed, 0.15f))
                {
                    yield return null;
                }

                if (Alive)
                {
                    // Down the burrow with it. It comes back when the next wave starts.
                    _carried = null;
                    SetLoop("Walk");
                    yield return new WaitForSeconds(_restSeconds);
                }
            }
        }
    }
}
