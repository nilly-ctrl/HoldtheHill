using System.Collections.Generic;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The in-game HUD canvas. Shows each <see cref="GrayboxHudPanel"/> under it while gameplay is
    /// running and the panel has something to show, hides it otherwise, and refreshes the ones
    /// showing. Menus sit on a canvas above this one, so a menu simply covers the HUD; the HUD also
    /// steps aside whenever the game flow is not Playing.
    /// </summary>
    [DefaultExecutionOrder(100)]
    [AddComponentMenu("Hold the Hill/Graybox/Graybox HUD Root")]
    public class GrayboxHudRoot : MonoBehaviour
    {
        private readonly List<GrayboxHudPanel> _panels = new List<GrayboxHudPanel>();

        private void Awake()
        {
            GetComponentsInChildren(true, _panels);
        }

        private void LateUpdate()
        {
            bool playing = GrayboxGameFlow.GameplayActive;
            foreach (GrayboxHudPanel panel in _panels)
            {
                bool show = playing && panel.WantsToShow;
                if (panel.gameObject.activeSelf != show)
                {
                    panel.gameObject.SetActive(show);
                }

                if (show)
                {
                    panel.Refresh();
                }
            }
        }
    }
}
