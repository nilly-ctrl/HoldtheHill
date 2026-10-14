namespace HoldTheHill.EditorTools.Validation
{
    /// <summary>What the model estimates for one wave.</summary>
    public sealed class BalanceWaveResult
    {
        /// <summary>1-based wave number.</summary>
        public int Number;

        public string Name;

        public int Enemies;

        public float TotalHp;

        public int TowersOwned;

        /// <summary>Sum of the towers' single-target damage per second, before range and placement.</summary>
        public float DefenceDps;

        public int FoodAtStart;

        /// <summary>Gold spent on towers before the wave.</summary>
        public int FoodSpent;

        /// <summary>Bounties plus any wave clear bonus.</summary>
        public int FoodEarned;

        public int FoodAtEnd;

        public int Killed;

        public int Leaked;

        /// <summary>Hill health lost to leaks in this wave.</summary>
        public float LeakPoints;

        /// <summary>Seconds from the wave start until its last enemy died or leaked.</summary>
        public float ClearSeconds;

        /// <summary>Seconds from the first spawn to the last spawn.</summary>
        public float SpawnSeconds;

        /// <summary>Average seconds from spawn to death, over the enemies that died. 0 when none did.</summary>
        public float AvgTimeToKill;

        /// <summary>Average position of a death as a fraction of the route (0 = entrance, 1 = hill). 0 when none died.</summary>
        public float MeanKillDepth;

        public BalanceVerdict Verdict;

        public string Reason;

        /// <summary>Real numbers from a harness run, when supplied. -1 means none.</summary>
        public int MeasuredKilled = -1;

        public int MeasuredLeaked = -1;
    }
}
