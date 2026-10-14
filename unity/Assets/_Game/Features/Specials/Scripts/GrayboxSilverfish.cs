using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Fast and dead in one hit. Where it dies it leaves a quick zone that speeds up whatever
    /// crosses it, so the question is where you kill it, not whether.
    /// </summary>
    public class GrayboxSilverfish : GrayboxSpecialEnemy
    {
        [SerializeField, Min(0.5f)] private float _zoneSeconds = 5f;
        [SerializeField, Min(0.2f)] private float _zoneRadius = 0.9f;
        [SerializeField, Min(1f)] private float _zoneSpeedScale = 1.6f;

        protected override void OnDied()
        {
            GrayboxQuickZone.Spawn(Library, transform.position, transform.rotation, _zoneSeconds, _zoneRadius, _zoneSpeedScale);
        }
    }
}
