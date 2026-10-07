using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>What a finished run changed in the records, for the run-end screen.</summary>
    public struct RunRecordResult
    {
        /// <summary>The run got further than any before on this level (or as far with fewer retries).</summary>
        public bool NewBestWave;

        /// <summary>The run was the fastest win on this level.</summary>
        public bool NewFastestVictory;

        public int BestWave;
        public int BestWaveRetries;
        public float FastestVictorySeconds;
    }

    /// <summary>
    /// Turns a finished run into lifetime totals and per-level bests in the save data.
    /// </summary>
    public static class GrayboxRecords
    {
        /// <summary>Records a run that just ended in victory or defeat.</summary>
        /// <param name="levelId">Which level the run was on.</param>
        /// <param name="stats">The run's numbers at the moment it ended.</param>
        /// <param name="victory">True for a win.</param>
        /// <param name="alreadyCounted">
        /// What an earlier ending of this same run already added to the totals, or null for its
        /// first ending. A run ends more than once when "retry current wave" is used after a
        /// defeat; only the difference is added so kills and time are not counted twice.
        /// </param>
        public static RunRecordResult Submit(string levelId, GrayboxRunStats stats, bool victory, GrayboxRunStats alreadyCounted = null)
        {
            GrayboxSaveData data = GrayboxSave.Data;
            RecordsSave records = data.records;

            if (alreadyCounted == null) records.runsFinished++;
            records.totalKills += Mathf.Max(0, stats.Kills - (alreadyCounted?.Kills ?? 0));
            records.totalFoodEarned += Mathf.Max(0, stats.GoldEarned - (alreadyCounted?.GoldEarned ?? 0));
            records.totalPlaySeconds += Mathf.Max(0f, stats.TimeSeconds - (alreadyCounted?.TimeSeconds ?? 0f));

            LevelRecord level = data.FindLevel(levelId);
            if (level == null)
            {
                level = new LevelRecord { levelId = levelId };
                records.levels.Add(level);
            }

            var result = new RunRecordResult();

            // Further is better; at the same wave, fewer retries is better.
            bool further = stats.WaveReached > level.bestWave;
            bool cleaner = stats.WaveReached == level.bestWave && stats.WaveReached > 0 && stats.WaveRetries < level.bestWaveRetries;
            if (further || cleaner)
            {
                level.bestWave = stats.WaveReached;
                level.bestWaveRetries = stats.WaveRetries;
                result.NewBestWave = true;
            }

            if (victory)
            {
                records.victories++;
                level.victories++;
                if (level.fastestVictorySeconds <= 0f || stats.TimeSeconds < level.fastestVictorySeconds)
                {
                    level.fastestVictorySeconds = stats.TimeSeconds;
                    result.NewFastestVictory = true;
                }
            }

            result.BestWave = level.bestWave;
            result.BestWaveRetries = level.bestWaveRetries;
            result.FastestVictorySeconds = level.fastestVictorySeconds;

            GrayboxSave.MarkDirty();
            return result;
        }
    }
}
