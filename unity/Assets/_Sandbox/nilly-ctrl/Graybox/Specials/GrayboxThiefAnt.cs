using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Runs to the hill, takes food from the stockpile, and runs it back to the burrow. Kill it
    /// on the way home and the food is recovered; let it reach the burrow and the food is gone.
    /// </summary>
    /// <remarks>
    /// Its mover must be set to stop at the end of the path, not despawn: this component walks
    /// it home again. It does the hill no other harm.
    /// </remarks>
    public class GrayboxThiefAnt : GrayboxSpecialEnemy
    {
        [SerializeField, Min(1)] private int _stealAmount = 40;
        [SerializeField, Min(1f)] private float _runHomeSpeedScale = 1.25f;

        private int _stolen;
        private bool _running;

        /// <summary>Food it is carrying right now.</summary>
        public int Stolen => _stolen;

        protected override void OnEnable()
        {
            base.OnEnable();
            _stolen = 0;
            _running = false;
        }

        protected override void OnReachedHill()
        {
            if (!_running && Alive)
            {
                _running = true;
                StartCoroutine(StealAndRun());
            }
        }

        private IEnumerator StealAndRun()
        {
            GrayboxEconomy economy = GrayboxEconomy.Instance;
            if (economy != null)
            {
                _stolen = Mathf.Min(_stealAmount, economy.CurrentGold);
                economy.TrySpendGold(_stolen);
            }

            Say(_stolen > 0 ? $"-{_stolen} STOLEN" : "NOTHING TO STEAL", DamageNumberKind.Fire, transform.position);
            Play("Grab");
            SetLoop(_stolen > 0 ? "WalkLoaded" : "Walk");
            yield return new WaitForSeconds(ClipLength("Grab", 0.48f));

            IReadOnlyList<Vector3> points = Waypoints;
            for (int i = points != null ? points.Count - 2 : -1; i >= 0; i--)
            {
                Vector2 target = points[i];
                while (Alive && Vector2.Distance(transform.position, target) > 0.05f)
                {
                    Vector2 position = transform.position;
                    Face(target - position);
                    float step = Mover.Speed * _runHomeSpeedScale * Health.SpeedMultiplier * Time.deltaTime;
                    Vector2 next = Vector2.MoveTowards(position, target, step);
                    transform.position = new Vector3(next.x, next.y, transform.position.z);
                    yield return null;
                }
            }

            if (Alive)
            {
                Escape();
            }
        }

        // Back down the burrow with the food. Not a kill: no bounty, and the spawner has to be told.
        private void Escape()
        {
            if (_stolen > 0)
            {
                Say("GOT AWAY", DamageNumberKind.Fire, transform.position);
            }

            var spawner = FindAnyObjectByType<EnemySpawner>();
            if (spawner != null)
            {
                spawner.NotifyEnemyDefeated(gameObject);
            }

            Destroy(gameObject);
        }

        protected override void OnDied()
        {
            if (_stolen <= 0)
            {
                return;
            }

            if (GrayboxEconomy.Instance != null)
            {
                GrayboxEconomy.Instance.EarnGold(_stolen);
            }

            Say($"+{_stolen} RECOVERED", DamageNumberKind.Resource, transform.position);
            SpriteClipPlayer.SpawnOneShot(Library, "FxPickup", "Collect", transform.position, Quaternion.identity, 1f, 6);
            _stolen = 0;
        }
    }
}
