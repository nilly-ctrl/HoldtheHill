using UnityEngine;

namespace HoldTheHill.Sandbox.UiKit
{
    /// <summary>Colours and sizes shared by every kit widget, in canvas units (pixels at 1x).</summary>
    public static class UiKitStyle
    {
        public static readonly Color32 Cream = new Color32(0xff, 0xf1, 0xd6, 0xff);
        public static readonly Color32 Dim = new Color32(0xc4, 0xbf, 0xb2, 0xff);
        public static readonly Color32 Disabled = new Color32(0x8a, 0x80, 0x74, 0xff);
        public static readonly Color32 Gold = new Color32(0xec, 0xc4, 0x77, 0xff);

        // The pixel font is exact at multiples of 10 (one font pixel per 1/10 em).
        public const int BodySize = 10;
        public const int TitleSize = 20;

        public const float RowHeight = 20f;
        public const float Spacing = 4f;
        public const float Padding = 8f;

        /// <summary>The screen size the UI is laid out for at 1x; larger screens scale by whole numbers.</summary>
        public const int ReferenceWidth = 640;
        public const int ReferenceHeight = 360;
    }
}
