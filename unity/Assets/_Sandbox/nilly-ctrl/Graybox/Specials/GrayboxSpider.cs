using System.Collections;
using HoldTheHill.Features.Towers;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Stops now and then to rear up and web the nearest tower, halving its rate of fire until
    /// the web wears off.
    /// </summary>
    public class GrayboxSpider : GrayboxSpecialEnemy
    {
        [SerializeField, Min(0.5f)] private float _webRange = 3.5f;
        [SerializeField, Min(0.5f)] private float _webInterval = 5f;
        [SerializeField, Min(0.5f)] private float _webSeconds = 6f;

        private float _cooldown;
        private bool _busy;

        protected override void OnEnable()
        {
            base.OnEnable();
            _cooldown = _webInterval * 0.4f;
            _busy = false;
        }

        private void Update()
        {
            if (_busy || !Alive)
            {
                return;
            }

            _cooldown -= Time.deltaTime;
            if (_cooldown > 0f)
            {
                return;
            }

            Tower tower = NearestTower(transform.position, _webRange,
                t => !GrayboxTowerAffliction.IsAfflicted(t, GrayboxTowerAffliction.Kind.Web));
            if (tower == null)
            {
                _cooldown = 0.5f; // nothing in reach yet; look again shortly
                return;
            }

            StartCoroutine(WebRoutine(tower));
        }

        private IEnumerator WebRoutine(Tower tower)
        {
            _busy = true;
            Mover.SpeedScale = 0f;
            Play("Rear");
            yield return new WaitForSeconds(ClipLength("Rear", 0.45f));

            if (tower != null && Alive)
            {
                GrayboxTowerAffliction.Apply(tower, GrayboxTowerAffliction.Kind.Web, _webSeconds);
            }

            Mover.SpeedScale = 1f;
            _cooldown = _webInterval;
            _busy = false;
        }
    }
}
