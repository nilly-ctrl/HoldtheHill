using System;
using System.Collections.Generic;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Announces a boss. When one appears its name fills the screen in the dripping Gothic blood
    /// style (through the scene's <see cref="GrayboxWaveBanner"/>), and a small name label then
    /// rides above it until it dies. It watches for the boss components themselves, so it works
    /// for bosses from a wave and from the spawn panel alike.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Graybox/Graybox Boss Banner")]
    public class GrayboxBossBanner : MonoBehaviour
    {
        [SerializeField, Min(0.5f)] private float _bannerSeconds = 2.6f;
        [Tooltip("Style of the announcement. GothicBlood drips; any baked style name works.")]
        [SerializeField] private string _bannerStyle = "GothicBlood";
        [Tooltip("Style of the label above the boss.")]
        [SerializeField] private string _labelStyle = "TinyLabel";
        [Tooltip("World offset from the boss's pivot to its label.")]
        [SerializeField] private Vector2 _labelOffset = new Vector2(0f, 1.1f);
        [SerializeField, Min(1)] private int _labelPixelScale = 2;

        private const float CheckSeconds = 0.4f;

        private static readonly Dictionary<Type, string> Names = new Dictionary<Type, string>
        {
            { typeof(GrayboxTitanBeetle), "TITAN BEETLE" },
            { typeof(GrayboxMantisQueen), "MANTIS QUEEN" },
            { typeof(GrayboxHornet), "HORNET" },
            { typeof(GrayboxOrbWeaver), "ORB WEAVER" },
            { typeof(GrayboxBoulderBug), "BOULDER BUG" },
            { typeof(GrayboxRivalQueen), "RIVAL QUEEN" },
        };

        private readonly HashSet<GrayboxSpecialEnemy> _announced = new HashSet<GrayboxSpecialEnemy>();
        private PixelFontTheme _theme;
        private GrayboxWaveBanner _banner;
        private float _nextCheck;

        private void Update()
        {
            if (Time.time < _nextCheck)
            {
                return;
            }

            _nextCheck = Time.time + CheckSeconds;
            _announced.RemoveWhere(boss => boss == null);      // bosses that have died
            foreach (GrayboxSpecialEnemy special in FindObjectsByType<GrayboxSpecialEnemy>())
            {
                if (!Names.TryGetValue(special.GetType(), out string bossName) || !_announced.Add(special))
                {
                    continue;
                }

                Announce(special, bossName);
            }
        }

        private void Announce(GrayboxSpecialEnemy boss, string bossName)
        {
            if (_banner == null)
            {
                _banner = FindAnyObjectByType<GrayboxWaveBanner>();
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
            go.transform.SetParent(boss.transform, false);
            go.transform.localPosition = _labelOffset;
            var label = go.AddComponent<PixelText>();
            label.ThemeStyle = _labelStyle;
            label.Style = style;
            label.Text = bossName;
            label.SetRendering(_labelPixelScale, 450);
        }
    }
}
