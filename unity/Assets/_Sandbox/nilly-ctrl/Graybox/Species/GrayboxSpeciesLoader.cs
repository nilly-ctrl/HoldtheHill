using System;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Sets the scene up for one species when it starts: hands the tower placer that species'
    /// catalog, puts its Warden on the map and sets its starting food.
    /// </summary>
    /// <remarks>
    /// The species is the one assigned here in the Inspector. There is no picker yet;
    /// <see cref="Load"/> is what a species screen would call.
    /// </remarks>
    [AddComponentMenu("Hold the Hill/Graybox/Graybox Species Loader")]
    public class GrayboxSpeciesLoader : MonoBehaviour
    {
        [Tooltip("The species this scene is played as.")]
        [SerializeField] private GrayboxSpeciesData _species;

        private GrayboxWarden _warden;

        /// <summary>Raised after a species has been loaded.</summary>
        public static event Action<GrayboxSpeciesData> Loaded;

        /// <summary>The species being played, or null before one is loaded.</summary>
        public static GrayboxSpeciesData Current { get; private set; }

        private void Start()
        {
            if (_species != null)
            {
                Load(_species);
            }
        }

        private void OnDestroy()
        {
            if (Current == _species)
            {
                Current = null;
            }
        }

        /// <summary>Switches the scene to a species. Towers already built stay as they are.</summary>
        public void Load(GrayboxSpeciesData species)
        {
            _species = species;
            Current = species;

            var placer = FindAnyObjectByType<GrayboxTowerPlacer>();
            if (placer != null && species.TowerCatalog != null)
            {
                placer.SetCatalog(species.TowerCatalog);
            }

            if (_warden != null)
            {
                Destroy(_warden.gameObject);
                _warden = null;
            }

            if (species.WardenPrefab != null)
            {
                _warden = Instantiate(species.WardenPrefab);
                _warden.name = species.WardenPrefab.name;
            }

            if (species.StartingFood > 0 && GrayboxEconomy.Instance != null)
            {
                GrayboxEconomy.Instance.ResetGold(species.StartingFood);
            }

            Loaded?.Invoke(species);
        }
    }
}
