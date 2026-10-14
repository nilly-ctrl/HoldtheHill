using HoldTheHill.Core.Animation;
using HoldTheHill.Core.Audio;
using UnityEngine;

namespace HoldTheHill.Features.Towers
{
    /// <summary>
    /// The sound and the puff when a tower is placed, upgraded or sold: listens to the
    /// <see cref="TowerPlacer"/> on the same object. The placer itself makes no noise.
    /// </summary>
    [RequireComponent(typeof(TowerPlacer))]
    [AddComponentMenu("Hold the Hill/Towers/Tower Placer Feedback")]
    public class TowerPlacerFeedback : MonoBehaviour
    {
        [Tooltip("Holds the build and sell effects. Left empty, towers appear without them.")]
        [SerializeField] private SpriteAnimLibrary _animLibrary;

        private TowerPlacer _placer;

        private void Awake()
        {
            _placer = GetComponent<TowerPlacer>();
        }

        private void OnEnable()
        {
            _placer.TowerPlaced += OnPlaced;
            _placer.TowerUpgraded += OnUpgraded;
            _placer.TowerSold += OnSold;
        }

        private void OnDisable()
        {
            _placer.TowerPlaced -= OnPlaced;
            _placer.TowerUpgraded -= OnUpgraded;
            _placer.TowerSold -= OnSold;
        }

        private void OnPlaced(Tower tower)
        {
            Sfx.PlayCue("Place", tower.transform.position);
            SpriteClipPlayer.SpawnOneShot(_animLibrary, "FxBuild", "Rise", tower.transform.position, Quaternion.identity, 1f, 3);
        }

        private void OnUpgraded(Tower tower)
        {
            Sfx.PlayCue("Upgrade", tower.transform.position);
        }

        private void OnSold(Tower tower)
        {
            Sfx.PlayCue("Sell", tower.transform.position);
            SpriteClipPlayer.SpawnOneShot(_animLibrary, "FxBuild", "Sell", tower.transform.position, Quaternion.identity, 1f, 3);
        }
    }
}
