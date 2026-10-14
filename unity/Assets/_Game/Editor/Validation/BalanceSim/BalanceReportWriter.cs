using System.Globalization;
using System.Text;

namespace HoldTheHill.EditorTools.Validation
{
    /// <summary>Formats a <see cref="BalanceSimResult"/> as a text report and as CSV. Pure C#.</summary>
    public static class BalanceReportWriter
    {
        public const string CsvHeader =
            "wave,name,enemies,total_hp,towers_owned,defence_dps,gold_start,gold_spent,gold_earned,gold_end," +
            "killed,leaked,leak_points,clear_seconds,avg_time_to_kill,mean_kill_depth,verdict,reason," +
            "measured_killed,measured_leaked";

        public static string ToCsv(BalanceSimResult result)
        {
            var sb = new StringBuilder();
            sb.AppendLine(CsvHeader);

            foreach (BalanceWaveResult w in result.Waves)
            {
                sb.Append(w.Number).Append(',')
                  .Append(Escape(w.Name)).Append(',')
                  .Append(w.Enemies).Append(',')
                  .Append(F(w.TotalHp)).Append(',')
                  .Append(w.TowersOwned).Append(',')
                  .Append(F(w.DefenceDps)).Append(',')
                  .Append(w.FoodAtStart).Append(',')
                  .Append(w.FoodSpent).Append(',')
                  .Append(w.FoodEarned).Append(',')
                  .Append(w.FoodAtEnd).Append(',')
                  .Append(w.Killed).Append(',')
                  .Append(w.Leaked).Append(',')
                  .Append(F(w.LeakPoints)).Append(',')
                  .Append(F(w.ClearSeconds)).Append(',')
                  .Append(F(w.AvgTimeToKill)).Append(',')
                  .Append(F(w.MeanKillDepth)).Append(',')
                  .Append(w.Verdict).Append(',')
                  .Append(Escape(w.Reason)).Append(',')
                  .Append(w.MeasuredKilled >= 0 ? w.MeasuredKilled.ToString(CultureInfo.InvariantCulture) : string.Empty).Append(',')
                  .Append(w.MeasuredLeaked >= 0 ? w.MeasuredLeaked.ToString(CultureInfo.InvariantCulture) : string.Empty)
                  .AppendLine();
            }

            return sb.ToString();
        }

        public static string ToText(BalanceSimResult result)
        {
            BalanceSettings s = result.Settings;
            var sb = new StringBuilder();

            sb.AppendLine("HOLD THE HILL BALANCE SIM - ESTIMATE, NOT THE REAL COMBAT");
            sb.AppendLine("Analytic model on a single line of route: towers hit the front-most enemies in reach.");
            sb.AppendLine("It leaves out slows, statuses, healers, shields regenerating, projectile travel, upgrades,");
            sb.AppendLine("abilities and the Warden. Trust the direction of a flag, not the exact numbers.");
            sb.AppendLine();
            sb.AppendLine($"Route {s.PathLength:0.#} units, start {s.StartingFood} gold, up to {s.MaxTowers} towers, " +
                          $"{s.Strategy} buying, towers {s.PathOffset:0.##} units from the road, " +
                          $"damage x{s.DamageMultiplier:0.##}, bounty x{s.BountyMultiplier:0.##}.");
            sb.AppendLine();

            sb.AppendLine("wave  enemies  towers  gold(start/spent/earned/end)   killed leaked  leakHP  clear(s)  avgTTK(s)  depth  verdict");
            foreach (BalanceWaveResult w in result.Waves)
            {
                sb.AppendLine(
                    $"{w.Number,4}  {w.Enemies,7}  {w.TowersOwned,6}  " +
                    $"{w.FoodAtStart,6}/{w.FoodSpent,5}/{w.FoodEarned,5}/{w.FoodAtEnd,6}   " +
                    $"{w.Killed,6} {w.Leaked,6}  {w.LeakPoints,6:0}  {w.ClearSeconds,8:0.0}  {w.AvgTimeToKill,9:0.0}  " +
                    $"{w.MeanKillDepth * 100f,4:0}%  {w.Verdict}" + Measured(w));
            }

            sb.AppendLine();
            sb.AppendLine($"Total: killed {result.TotalKilled}, leaked {result.TotalLeaked}, leak points {result.TotalLeakPoints:0} of {s.HillHealth:0} hill health.");
            sb.AppendLine(result.HillFallsInWave > 0
                ? $"The hill falls in wave {result.HillFallsInWave} in this model."
                : "The hill holds through every wave in this model.");

            sb.AppendLine();
            sb.AppendLine("Flagged waves:");
            bool any = false;
            foreach (BalanceWaveResult w in result.Waves)
            {
                if (w.Verdict == BalanceVerdict.OnTarget)
                {
                    continue;
                }

                any = true;
                string label = w.Verdict == BalanceVerdict.UnderTuned ? "UNDER-TUNED" : "OVER-TUNED";
                sb.AppendLine($"  wave {w.Number} ({w.Name}): {label} - {w.Reason}");
            }

            if (!any)
            {
                sb.AppendLine("  none");
            }

            if (result.Purchases.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Towers bought:");
                foreach (string purchase in result.Purchases)
                {
                    sb.AppendLine("  " + purchase);
                }
            }

            if (result.Notes.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("Notes and gaps:");
                foreach (string note in result.Notes)
                {
                    sb.AppendLine("  - " + note);
                }
            }

            return sb.ToString();
        }

        private static string Measured(BalanceWaveResult w)
        {
            return w.MeasuredKilled >= 0
                ? $"   [harness: killed {w.MeasuredKilled}, leaked {w.MeasuredLeaked}]"
                : string.Empty;
        }

        private static string F(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0)
            {
                return value;
            }

            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
