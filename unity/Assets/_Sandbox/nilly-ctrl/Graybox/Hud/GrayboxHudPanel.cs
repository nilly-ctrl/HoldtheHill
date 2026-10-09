using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// One piece of the in-game HUD: the build bar, the tower card, the hill health bar and so on.
    /// A panel says when it wants to be on screen and how to bring itself up to date; the
    /// <see cref="GrayboxHudRoot"/> above it does the showing, hiding and refreshing.
    /// </summary>
    /// <remarks>
    /// Every panel is a child of the HUD prefab, so a new one is a new child object with a
    /// subclass on it, and no change to the root.
    /// </remarks>
    public abstract class GrayboxHudPanel : MonoBehaviour
    {
        /// <summary>False while the panel has nothing to show, e.g. a tower card with no tower selected.</summary>
        public virtual bool WantsToShow => true;

        /// <summary>Brings the panel up to date. Called every frame it is showing.</summary>
        public abstract void Refresh();

        /// <summary>The HUD canvas, for turning world positions into places on it.</summary>
        protected RectTransform CanvasRect
        {
            get
            {
                if (_canvasRect == null)
                {
                    Canvas canvas = GetComponentInParent<Canvas>();
                    _canvasRect = canvas != null ? canvas.rootCanvas.transform as RectTransform : null;
                }

                return _canvasRect;
            }
        }

        private RectTransform _canvasRect;

        /// <summary>
        /// Runs <paramref name="action"/> when the button is clicked, then gives up the keyboard
        /// focus the click left on it. Otherwise Space or Enter, which the game uses for the
        /// Warden and the next wave, would press the button again.
        /// </summary>
        protected static void OnClick(Button button, UnityAction action)
        {
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            button.onClick.AddListener(() =>
            {
                action();
                ReleaseFocus();
            });
        }

        /// <summary>Takes the keyboard focus off whatever was clicked, so keys keep going to the game.</summary>
        protected static void ReleaseFocus()
        {
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }
        }

        /// <summary>
        /// Puts <paramref name="rect"/> (anchored at the centre of the canvas) over a world
        /// position, with an offset in canvas units. False if the position is behind the camera.
        /// </summary>
        protected bool PlaceAtWorld(RectTransform rect, Vector3 world, Vector2 offset)
        {
            Camera camera = Camera.main;
            RectTransform canvas = CanvasRect;
            if (camera == null || canvas == null)
            {
                return false;
            }

            Vector3 screen = camera.WorldToScreenPoint(world);
            if (screen.z < 0f)
            {
                return false;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas, screen, null, out Vector2 local))
            {
                return false;
            }

            rect.anchoredPosition = local + offset;
            return true;
        }
    }
}
