using System;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Numbers for the run in progress: what the run-end screen shows and what records are made from.
    /// </summary>
    [Serializable]
    public class GrayboxRunStats
    {
        /// <summary>Highest wave started, 1-based. Zero before the first wave.</summary>
        public int WaveReached;
        public int TotalWaves;
        public int Kills;

        /// <summary>Gold from kills only; refunds are not income.</summary>
        public int GoldEarned;

        /// <summary>Game-time seconds spent playing (paused time excluded, fast-forward counts faster).</summary>
        public float TimeSeconds;

        /// <summary>Times "retry current wave" was used this run. Shown beside records.</summary>
        public int WaveRetries;

        /// <summary>Towers on the field when the run ended.</summary>
        public int TowersStanding;

        public GrayboxRunStats Clone() => (GrayboxRunStats)MemberwiseClone();
    }
}
