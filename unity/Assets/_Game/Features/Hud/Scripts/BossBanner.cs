using HoldTheHill.Features.Specials;
using HoldTheHill.Features.DamageNumbers;
using UnityEngine;

namespace HoldTheHill.Features.Hud
{
    /// <summary>
    /// Announces a boss. When one appears its name fills the screen in the dripping Gothic blood
    /// style (through the scene's <see cref="WaveBanner"/>), and a small name label then
    /// rides above it until it dies. It listens for <see cref="Boss.Appeared"/>, so it works
    /// for bosses from a wave and from the spawn panel alike, and a new boss needs no change here.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Graybox Boss Banner")]
    public class BossBanner : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] private float _bannerSeconds = 2.6f;
        [Tooltip("Style of the announcement. GothicBlood drips; any baked style name works.")]
        [SerializeField] private string _bannerStyle = "GothicBlood";
        [Tooltip("Style of the label above the boss.")]
        [SerializeField] private string _labelStyle = "TinyLabel";
        [Tooltip("World offset from the boss's pivot to its label.")]
        [SerializeField] private Vector2 _labelOffset = new Vector2(0f, 1.1f);
        [SerializeField, Min(1)] private int _labelPixelScale = 2;

        private PixelFontTheme _theme;
        private WaveBanner _banner;

        private void OnEnable()
        {
            Boss.Appeared += OnBossAppeared;
        }

        private void OnDisable()
        {
            Boss.Appeared -= OnBossAppeared;
        }

        private void OnBossAppeared(Boss boss)
        {
            if (boss.Data != null)
            {
                Announce(boss.transform, boss.Data.BannerText);
            }
        }

        private void Announce(Transform boss, string bossName)
        {
            if (_banner == null)
            {
                _banner = FindAnyObjectByType<WaveBanner>();
            }

            if (_banner != null)
            {
                _banner.Show(bossName, _bannerStyle, _bannerSeconds);
            }

            if (_theme == null)
            {
                _theme = FindAnyObjectByType<PixelFontTheme>();
            }

            PixelFontStyle style = _theme != null ? _theme.Find(_labelStyle) : null;
            if (style == null)
            {
                return;
            }

            // A child of the boss, so it follows it and goes when the boss does.
            var go = new GameObject("Boss Name");
            go.transform.SetParent(boss, false);
            go.transform.localPosition = _labelOffset;
            var label = go.AddComponent<PixelText>();
            label.ThemeStyle = _labelStyle;
            label.Style = style;
            label.Text = bossName;
            label.SetRendering(_labelPixelScale, 450);
        }
    }
}
