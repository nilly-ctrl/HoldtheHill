using System;
using System.Collections.Generic;

namespace HoldTheHill.Features.Enemies
{
    /// <summary>
    /// Which waves a map plays, by the spawner's priority: the override asset, then the asset list,
    /// then the inline list, matched on the map id without regard to case. Plain C#, so the
    /// EditMode tests can ask it directly.
    /// </summary>
    public static class MapWaveLookup
    {
        /// <summary>The waves for <paramref name="mapId"/>, or null when nothing names that map.</summary>
        public static List<Wave> Find(MapWaveDataSO overrideConfig, IReadOnlyList<MapWaveDataSO> assets, IReadOnlyList<MapWaveData> inline, string mapId)
        {
            if (overrideConfig != null)
            {
                return overrideConfig.waves;
            }

            if (assets != null)
            {
                for (int i = 0; i < assets.Count; i++)
                {
                    if (assets[i] != null && Matches(assets[i].mapId, mapId))
                    {
                        return assets[i].waves;
                    }
                }
            }

            if (inline != null)
            {
                for (int i = 0; i < inline.Count; i++)
                {
                    if (inline[i] != null && Matches(inline[i].mapId, mapId))
                    {
                        return inline[i].waves;
                    }
                }
            }

            return null;
        }

        private static bool Matches(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
    }
}
