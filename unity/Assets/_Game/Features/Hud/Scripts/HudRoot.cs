using HoldTheHill.Features.GameFlow;
using System.Collections.Generic;
using UnityEngine;

namespace HoldTheHill.Features.Hud
{
    /// <summary>
    /// The in-game HUD canvas. Shows each <see cref="HudPanel"/> under it while gameplay is
    /// running and the panel has something to show, hides it otherwise, and refreshes the ones
    /// showing. Menus sit on a canvas above this one, so a menu simply covers the HUD; the HUD also
    /// steps aside whenever the game flow is not Playing.
    /// </summary>
    [DefaultExecutionOrder(100)]
    [AddComponentMenu("Hold the Hill/Graybox HUD Root")]
    public class HudRoot : MonoBehaviour
    {
        private readonly List<HudPanel> _panels = new List<HudPanel>();

        private RunFlow _flow;

        private void Awake()
        {
            GetComponentsInChildren(true, _panels);
            _flow = FindAnyObjectByType<RunFlow>();
        }

        private void LateUpdate()
        {
            bool playing = _flow == null || _flow.IsPlaying;
            foreach (HudPanel panel in _panels)
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
