using HoldTheHill.Features.Economy;
using HoldTheHill.Features.Hero;
using HoldTheHill.Features.Towers;
using System;
using UnityEngine;

namespace HoldTheHill.Features.Species
{
    /// <summary>
    /// Sets the scene up for one species when it starts: hands the tower placer that species'
    /// catalog, puts its Warden on the map and sets its starting food.
    /// </summary>
    /// <remarks>
    /// The species is the one assigned here in the Inspector. There is no picker yet;
    /// <see cref="Load"/> is what a species screen would call.
    /// </remarks>
    [AddComponentMenu("Hold the Hill/Species Loader")]
    public class SpeciesLoader : MonoBehaviour
    {
        [Tooltip("The species this scene is played as.")]
        [SerializeField] private SpeciesData _species;

        private Warden _warden;

        /// <summary>Raised after a species has been loaded.</summary>
        public static event Action<SpeciesData> Loaded;

        /// <summary>The species being played, or null before one is loaded.</summary>
        public static SpeciesData Current { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Loaded = null;
            Current = null;
        }

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
        public void Load(SpeciesData species)
        {
            _species = species;
            Current = species;

            var placer = FindAnyObjectByType<TowerPlacer>();
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

            WalletBehaviour economy = FindAnyObjectByType<WalletBehaviour>();
            if (species.StartingFood > 0 && economy != null)
            {
                economy.ResetFood(species.StartingFood);
            }

            Loaded?.Invoke(species);
        }
    }
}
