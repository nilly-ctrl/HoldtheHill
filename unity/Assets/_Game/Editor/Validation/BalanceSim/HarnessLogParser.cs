using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace HoldTheHill.EditorTools.Validation
{
    /// <summary>
    /// Reads the per-wave lines BalanceHarness writes to the console, e.g.
    /// <c>wave 2: spawned 12, killed 11, leaked 1, 8.4s wall</c>, so a measured run can sit next to the estimate.
    /// </summary>
    public static class HarnessLogParser
    {
        private static readonly Regex WaveLine = new Regex(
            @"wave\s+(?<n>\d+):\s*spawned\s+(?<s>\d+),\s*killed\s+(?<k>\d+),\s*leaked\s+(?<l>\d+)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static List<MeasuredWave> Parse(string log)
        {
            var waves = new List<MeasuredWave>();
            if (string.IsNullOrEmpty(log))
            {
                return waves;
            }

            foreach (Match m in WaveLine.Matches(log))
            {
                waves.Add(new MeasuredWave(
                    int.Parse(m.Groups["n"].Value),
                    int.Parse(m.Groups["s"].Value),
                    int.Parse(m.Groups["k"].Value),
                    int.Parse(m.Groups["l"].Value)));
            }

            return waves;
        }

        /// <summary>Copies measured kills and leaks onto the matching wave results.</summary>
        public static void Apply(IEnumerable<MeasuredWave> measured, BalanceSimResult result)
        {
            if (measured == null || result == null)
            {
                return;
            }

            foreach (MeasuredWave m in measured)
            {
                foreach (BalanceWaveResult wave in result.Waves)
                {
                    if (wave.Number == m.Number)
                    {
                        wave.MeasuredKilled = m.Killed;
                        wave.MeasuredLeaked = m.Leaked;
                    }
                }
            }
        }
    }
}
