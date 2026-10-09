using HoldTheHill.Sandbox.UiKit;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Buttons that send a preset horde down the trail, for testing the towers against crowds.
    /// Shows when the scene has a <see cref="GrayboxCustomSpawner"/>.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Graybox/HUD/Horde Panel")]
    public class GrayboxHordePanel : GrayboxHudPanel
    {
        [SerializeField] private UiButton _swarmRush;
        [SerializeField] private UiButton _bruteParade;
        [SerializeField] private UiButton _phalanx;
        [SerializeField] private UiButton _hydraSplitters;
        [SerializeField] private UiButton _megaHorde;

        private GrayboxCustomSpawner _spawner;

        public override bool WantsToShow
        {
            get
            {
                if (_spawner == null)
                {
                    _spawner = FindAnyObjectByType<GrayboxCustomSpawner>();
                }

                return _spawner != null;
            }
        }

        private void Awake()
        {
            OnClick(_swarmRush.Button, () => _spawner.SpawnPreset_SwarmRush());
            OnClick(_bruteParade.Button, () => _spawner.SpawnPreset_BruteParade());
            OnClick(_phalanx.Button, () => _spawner.SpawnPreset_Phalanx());
            OnClick(_hydraSplitters.Button, () => _spawner.SpawnPreset_HydraSplitters());
            OnClick(_megaHorde.Button, () => _spawner.SpawnPreset_MegaHorde());
        }

        public override void Refresh()
        {
        }
    }
}
