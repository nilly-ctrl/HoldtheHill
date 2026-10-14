using System.Collections.Generic;

namespace HoldTheHill.EditorTools.Validation
{
    /// <summary>The model's output for a whole run.</summary>
    public sealed class BalanceSimResult
    {
        public BalanceSettings Settings;

        public List<BalanceWaveResult> Waves = new List<BalanceWaveResult>();

        /// <summary>Names of the towers bought, in order, with the wave they were bought before.</summary>
        public List<string> Purchases = new List<string>();

        /// <summary>Things the model left out or guessed, from the input builder and the simulator.</summary>
        public List<string> Notes = new List<string>();

        /// <summary>1-based wave in which cumulative leak points reach the hill's health, or 0 if they never do.</summary>
        public int HillFallsInWave;

        public int TotalKilled;

        public int TotalLeaked;

        public float TotalLeakPoints;
    }
}
