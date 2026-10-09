using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The hill's health at the top of the screen: a framed bar that turns from green to gold to red
    /// as the hill weakens, and the numbers under a heart.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Graybox/HUD/Base Health Bar")]
    public class GrayboxBaseHealthBar : GrayboxHudPanel
    {
        [Tooltip("The coloured fill. Its right edge is moved to match the health.")]
        [SerializeField] private RectTransform _fill;

        [SerializeField] private Image _fillImage;
        [SerializeField] private TMP_Text _label;

        [Header("Fill art")]
        [SerializeField] private Sprite _healthy;
        [SerializeField] private Sprite _hurt;
        [SerializeField] private Sprite _danger;

        [Tooltip("Above this share of full health the fill is the healthy colour.")]
        [SerializeField, Range(0f, 1f)] private float _healthyAbove = 0.5f;

        [Tooltip("Above this share, and not healthy, the fill is the hurt colour; at or below it, the danger colour.")]
        [SerializeField, Range(0f, 1f)] private float _hurtAbove = 0.25f;

        private int _shownCurrent = -1;
        private int _shownMax = -1;

        public override bool WantsToShow => GrayboxBaseHealth.Instance != null;

        public override void Refresh()
        {
            GrayboxBaseHealth hill = GrayboxBaseHealth.Instance;
            float fraction = hill.MaxHealth > 0f ? Mathf.Clamp01(hill.CurrentHealth / hill.MaxHealth) : 0f;

            _fill.anchorMax = new Vector2(fraction, 1f);
            _fillImage.enabled = fraction > 0f;
            _fillImage.sprite = fraction > _healthyAbove ? _healthy : (fraction > _hurtAbove ? _hurt : _danger);

            int current = Mathf.RoundToInt(hill.CurrentHealth);
            int max = Mathf.RoundToInt(hill.MaxHealth);
            if (current != _shownCurrent || max != _shownMax)
            {
                _shownCurrent = current;
                _shownMax = max;
                _label.text = $"HILL BASE HP: {current} / {max}";
            }
        }
    }
}
