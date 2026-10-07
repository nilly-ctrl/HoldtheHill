using UnityEngine;
using UnityEngine.UI;

namespace HoldTheHill.Sandbox.UiKit
{
    /// <summary>
    /// Scales the canvas by whole numbers only, so pixel art and the pixel font stay square:
    /// 1x up to 1279x719, 2x at 720p, 3x at 1080p, 6x at 4K.
    /// </summary>
    [RequireComponent(typeof(CanvasScaler))]
    [AddComponentMenu("Hold the Hill/UI Kit/Pixel Canvas Scaler")]
    public class UiPixelCanvasScaler : MonoBehaviour
    {
        [Tooltip("Added to the automatic scale. The UI-size setting: -1 smaller, +1 larger.")]
        [SerializeField] private int _scaleOffset;

        private CanvasScaler _scaler;
        private int _width;
        private int _height;
        private int _appliedOffset = int.MinValue;

        public int ScaleOffset
        {
            get => _scaleOffset;
            set => _scaleOffset = value;
        }

        /// <summary>The largest whole scale at which the reference layout still fits the screen.</summary>
        public static int ComputeScale(int screenWidth, int screenHeight, int offset = 0)
        {
            int fit = Mathf.FloorToInt(Mathf.Min(
                screenWidth / (float)UiKitStyle.ReferenceWidth,
                screenHeight / (float)UiKitStyle.ReferenceHeight));
            return Mathf.Max(1, fit + offset);
        }

        private void OnEnable()
        {
            _scaler = GetComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            _width = 0; // force a refresh
            Refresh();
        }

        private void Update() => Refresh();

        private void Refresh()
        {
            if (Screen.width == _width && Screen.height == _height && _scaleOffset == _appliedOffset) return;

            _width = Screen.width;
            _height = Screen.height;
            _appliedOffset = _scaleOffset;
            _scaler.scaleFactor = ComputeScale(_width, _height, _scaleOffset);
        }
    }
}
