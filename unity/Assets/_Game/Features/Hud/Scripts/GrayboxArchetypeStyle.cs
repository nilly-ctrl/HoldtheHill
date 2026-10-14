using HoldTheHill.Features.Combat;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>How a tower archetype is named and coloured on the HUD.</summary>
    public static class GrayboxArchetypeStyle
    {
        public static Color Colour(TowerArchetype archetype)
        {
            switch (archetype)
            {
                case TowerArchetype.Gunner: return new Color32(0x80, 0xd0, 0xff, 0xff);
                case TowerArchetype.Artillery: return new Color32(0xff, 0xa0, 0x50, 0xff);
                case TowerArchetype.Arc: return new Color32(0xc8, 0xa0, 0xff, 0xff);
                case TowerArchetype.Controller: return new Color32(0x70, 0xe0, 0xd0, 0xff);
                case TowerArchetype.Brawler: return new Color32(0xff, 0x80, 0x80, 0xff);
                case TowerArchetype.Support: return new Color32(0x7f, 0xdd, 0x7f, 0xff);
                case TowerArchetype.Summoner: return new Color32(0xec, 0xc4, 0x77, 0xff);
                default: return Color.white;
            }
        }

        /// <summary>The archetype name wrapped in its colour, for a rich-text label.</summary>
        public static string Coloured(TowerArchetype archetype)
        {
            return $"<color=#{ColorUtility.ToHtmlStringRGB(Colour(archetype))}>{archetype}</color>";
        }
    }
}
