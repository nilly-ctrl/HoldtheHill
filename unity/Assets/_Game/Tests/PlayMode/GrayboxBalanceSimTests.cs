// Tests for the analytic balance model (Editor/Validation/BalanceSim).
//
// Compiles only once HoldTheHill.Tests.PlayMode references HoldTheHill.Editor.Validation and
// HTH_VALIDATION_TESTS is defined (see Editor/Validation/README.md); until then it is empty so the
// test assembly keeps compiling.
#if UNITY_EDITOR && HTH_VALIDATION_TESTS
using HoldTheHill.EditorTools.Validation;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Towers;
using System.Collections.Generic;
using NUnit.Framework;

namespace HoldTheHill.Tests
{
    /// <summary>
    /// The model is simple enough to check by hand: one tower, one enemy on a 20 unit route
    /// dies after hp / dps seconds, so every expected number below is plain arithmetic.
    /// </summary>
    public class GrayboxBalanceSimTests
    {
        // One tower unless a test asks for more, so a test's arithmetic is about that one tower.
        private static BalanceSettings Settings(float pathLength = 20f, int gold = 100, int maxTowers = 1)
        {
            return new BalanceSettings
            {
                PathLength = pathLength,
                StartingFood = gold,
                MaxTowers = maxTowers,
                PathOffset = 0f,
                TimeStep = 0.01f,
            };
        }

        // A tower with a reach longer than any test route, so placement does not matter.
        private static BalanceTower Tower(string name, int cost, float dps, int targets = 1, float falloff = 1f)
        {
            return new BalanceTower(name, cost, 1000f, dps, targets, falloff);
        }

        private static BalanceWave Wave(string name, int count, float hp, float speed, int bounty = 10,
            float spawnGap = 0f, float breach = 5f)
        {
            var enemies = new List<BalanceEnemy>();
            for (int i = 0; i < count; i++)
            {
                enemies.Add(new BalanceEnemy("Ant", i * spawnGap, hp, speed, bounty, breach));
            }

            return new BalanceWave(name, enemies);
        }

        private static BalanceSimResult Run(BalanceSettings settings, BalanceTower[] towers, params BalanceWave[] waves)
        {
            return BalanceSimulator.Run(towers, waves, settings);
        }

        // ---------- combat arithmetic ----------

        [Test]
        public void OneTower_KillsAnEnemyAfterHpOverDps()
        {
            // 50 hp at 10 per second: 5 seconds, by which time a speed-1 enemy is 5 of 20 units along.
            BalanceSimResult result = Run(Settings(), new[] { Tower("Gun", 50, 10f) }, Wave("W1", 1, 50f, 1f));

            BalanceWaveResult wave = result.Waves[0];
            Assert.AreEqual(1, wave.Killed);
            Assert.AreEqual(0, wave.Leaked);
            Assert.AreEqual(5f, wave.AvgTimeToKill, 0.05f);
            Assert.AreEqual(5f, wave.ClearSeconds, 0.05f);
            Assert.AreEqual(0.25f, wave.MeanKillDepth, 0.02f);
        }

        [Test]
        public void MultiTargetTower_ClearsACrowdInParallel()
        {
            // Three enemies of 100 hp at 10 dps each: 10 s with three target slots, 30 s with one.
            BalanceSettings settings = Settings(pathLength: 1000f);

            BalanceSimResult crowd = Run(settings, new[] { Tower("Splash", 50, 10f, targets: 3) }, Wave("W", 3, 100f, 1f));
            BalanceSimResult single = Run(settings, new[] { Tower("Gun", 50, 10f) }, Wave("W", 3, 100f, 1f));

            Assert.AreEqual(10f, crowd.Waves[0].ClearSeconds, 0.1f);
            Assert.AreEqual(30f, single.Waves[0].ClearSeconds, 0.1f);
        }

        [Test]
        public void TargetFalloff_WeakensEachFurtherTarget()
        {
            var tower = Tower("Chain", 50, 10f, targets: 3, falloff: 0.5f);

            Assert.AreEqual(17.5f, tower.CrowdDps, 0.001f, "10 + 5 + 2.5");
        }

        [Test]
        public void NoTowers_EveryEnemyLeaks_AndTheHillFalls()
        {
            BalanceSettings settings = Settings();
            settings.HillHealth = 100f;

            BalanceSimResult result = Run(settings, new BalanceTower[0], Wave("W1", 4, 50f, 2f, breach: 30f));

            BalanceWaveResult wave = result.Waves[0];
            Assert.AreEqual(4, wave.Leaked);
            Assert.AreEqual(0, wave.Killed);
            Assert.AreEqual(120f, wave.LeakPoints, 0.001f);
            Assert.AreEqual(1, result.HillFallsInWave);
            Assert.AreEqual(BalanceVerdict.UnderTuned, wave.Verdict);
        }

        [Test]
        public void ADefenceThatCannotReachTheRoad_IsNeverBought()
        {
            BalanceSettings settings = Settings();
            settings.PathOffset = 3f;
            var tooShort = new BalanceTower("Short", 10, 2f, 100f);

            BalanceSimResult result = Run(settings, new[] { tooShort }, Wave("W1", 2, 10f, 5f));

            Assert.AreEqual(0, result.Waves[0].TowersOwned);
            Assert.AreEqual(2, result.Waves[0].Leaked);
            StringAssert.Contains("does not reach the route", string.Join("\n", result.Notes));
        }

        // ---------- gold ----------

        [Test]
        public void Food_IsSpentBeforeAWave_AndEarnedFromBounties()
        {
            BalanceSimResult result = Run(Settings(gold: 100), new[] { Tower("Gun", 40, 100f) },
                new BalanceWave("W1", new[]
                {
                    new BalanceEnemy("A", 0f, 10f, 1f, 15, 5f),
                    new BalanceEnemy("B", 0.5f, 10f, 1f, 25, 5f),
                }));

            BalanceWaveResult wave = result.Waves[0];
            Assert.AreEqual(100, wave.FoodAtStart);
            Assert.AreEqual(40, wave.FoodSpent);
            Assert.AreEqual(40, wave.FoodEarned);
            Assert.AreEqual(100, wave.FoodAtEnd, "100 - 40 + 40");
        }

        [Test]
        public void Food_CarriesOverAndBuysTheNextTowerWhenAffordable()
        {
            BalanceSettings settings = Settings(pathLength: 100f, gold: 150, maxTowers: 5);
            BalanceSimResult result = Run(settings, new[] { Tower("Gun", 100, 10f) },
                Wave("W1", 4, 10f, 0.5f, bounty: 30),
                Wave("W2", 4, 10f, 0.5f, bounty: 30));

            Assert.AreEqual(1, result.Waves[0].TowersOwned, "150 gold buys one 100 gold tower");
            Assert.AreEqual(170, result.Waves[0].FoodAtEnd, "50 left plus 4 x 30");
            Assert.AreEqual(2, result.Waves[1].TowersOwned, "170 gold buys the second");
        }

        [Test]
        public void RoundRobin_SavesWhenTheNextTowerInTheCycleIsTooDear()
        {
            var cheap = Tower("Cheap", 50, 5f);
            var dear = Tower("Dear", 500, 50f);

            BalanceSimResult result = Run(Settings(gold: 120, maxTowers: 5), new[] { dear, cheap },
                Wave("W1", 1, 10f, 0.1f, bounty: 0));

            Assert.AreEqual(1, result.Waves[0].TowersOwned, "buys Cheap, then waits for Dear");
            Assert.AreEqual(70, result.Waves[0].FoodAtEnd);
        }

        [Test]
        public void BestValue_PrefersMoreDamagePerFood()
        {
            BalanceSettings settings = Settings(gold: 100, maxTowers: 1);
            settings.Strategy = BalanceBuyStrategy.BestValue;
            var poor = Tower("Poor", 10, 1f);
            var rich = Tower("Rich", 10, 9f);

            BalanceSimResult result = Run(settings, new[] { poor, rich }, Wave("W1", 1, 10f, 1f));

            StringAssert.Contains("Rich", result.Purchases[0]);
        }

        [Test]
        public void TowerCap_StopsSpending()
        {
            BalanceSimResult result = Run(Settings(gold: 1000, maxTowers: 2), new[] { Tower("Gun", 10, 5f) },
                Wave("W1", 1, 10f, 0.1f));

            Assert.AreEqual(2, result.Waves[0].TowersOwned);
            Assert.AreEqual(980, result.Waves[0].FoodAtStart - result.Waves[0].FoodSpent);
        }

        // ---------- verdicts ----------

        [Test]
        public void Verdict_UnderTuned_WhenEnoughEnemiesLeak()
        {
            BalanceSimResult result = Run(Settings(), new[] { Tower("Weak", 10, 1f) }, Wave("W1", 5, 100f, 5f));

            Assert.AreEqual(BalanceVerdict.UnderTuned, result.Waves[0].Verdict);
            StringAssert.Contains("leak", result.Waves[0].Reason);
        }

        [Test]
        public void Verdict_OverTuned_WhenNothingLeaksAndEnemiesDieAtTheEntrance()
        {
            BalanceSimResult result = Run(Settings(), new[] { Tower("Huge", 10, 1000f) }, Wave("W1", 3, 10f, 1f));

            Assert.AreEqual(0, result.Waves[0].Leaked);
            Assert.AreEqual(BalanceVerdict.OverTuned, result.Waves[0].Verdict);
        }

        [Test]
        public void Verdict_OnTarget_WhenEnemiesDieMidRoute()
        {
            // 100 hp at 10 dps, speed 1: dies 10 units along a 20 unit route.
            BalanceSimResult result = Run(Settings(), new[] { Tower("Fair", 10, 10f) }, Wave("W1", 1, 100f, 1f));

            Assert.AreEqual(0.5f, result.Waves[0].MeanKillDepth, 0.02f);
            Assert.AreEqual(BalanceVerdict.OnTarget, result.Waves[0].Verdict);
        }

        [Test]
        public void ADefenceThatDoesNothing_IsNotModelled_AndIsNoted()
        {
            var nurse = new BalanceTower("Nurse", 50, 5f, 0f, note: "support");

            BalanceSimResult result = Run(Settings(), new[] { nurse }, Wave("W1", 1, 10f, 5f));

            Assert.AreEqual(0, result.Waves[0].TowersOwned);
            StringAssert.Contains("Nurse", string.Join("\n", result.Notes));
        }

        [Test]
        public void Run_IsDeterministic()
        {
            var towers = new[] { Tower("A", 30, 8f, 2, 0.8f), Tower("B", 60, 20f) };
            BalanceWave[] waves = { Wave("W1", 6, 40f, 1.5f, spawnGap: 0.4f), Wave("W2", 8, 60f, 2f, spawnGap: 0.3f) };

            string first = BalanceReportWriter.ToCsv(Run(Settings(gold: 300), towers, waves));
            string second = BalanceReportWriter.ToCsv(Run(Settings(gold: 300), towers, waves));

            Assert.AreEqual(first, second);
        }

        // ---------- output ----------

        [Test]
        public void Csv_HasOneRowPerWave_AndMatchingColumnCounts()
        {
            BalanceSimResult result = Run(Settings(gold: 300), new[] { Tower("A", 30, 8f) },
                Wave("One, with a comma", 2, 10f, 1f), Wave("Two", 2, 10f, 1f));

            string[] lines = BalanceReportWriter.ToCsv(result).TrimEnd().Split('\n');

            Assert.AreEqual(3, lines.Length, "header plus two waves");
            StringAssert.StartsWith("wave,name,enemies", lines[0]);
            StringAssert.Contains("\"One, with a comma\"", lines[1]);
            Assert.AreEqual(Columns(lines[0]), Columns(lines[2]));
        }

        [Test]
        public void Report_SaysItIsAnEstimate_AndNamesFlaggedWaves()
        {
            BalanceSimResult result = Run(Settings(), new BalanceTower[0], Wave("Boss Wave", 3, 50f, 5f));

            string text = BalanceReportWriter.ToText(result);

            StringAssert.Contains("ESTIMATE", text);
            StringAssert.Contains("UNDER-TUNED", text);
            StringAssert.Contains("Boss Wave", text);
        }

        // ---------- harness comparison ----------

        [Test]
        public void HarnessLogParser_ReadsWaveLines_AndCopiesThemOntoResults()
        {
            const string log =
                "[Graybox balance]\n" +
                "  waves in the asset: 2\n" +
                "  wave 1: spawned 5, killed 5, leaked 0, 12.3s wall\n" +
                "  wave 2: spawned 8, killed 6, leaked 2, 20.1s wall\n" +
                "  TOTAL: spawned 13, killed 11, leaked 2\n";

            List<MeasuredWave> measured = HarnessLogParser.Parse(log);
            Assert.AreEqual(2, measured.Count);
            Assert.AreEqual(8, measured[1].Spawned);
            Assert.AreEqual(2, measured[1].Leaked);

            BalanceSimResult result = Run(Settings(), new[] { Tower("A", 10, 10f) }, Wave("W1", 5, 10f, 1f), Wave("W2", 8, 10f, 1f));
            HarnessLogParser.Apply(measured, result);

            Assert.AreEqual(6, result.Waves[1].MeasuredKilled);
            StringAssert.Contains("harness: killed 6, leaked 2", BalanceReportWriter.ToText(result));
        }

        private static int Columns(string csvLine)
        {
            int columns = 1;
            bool quoted = false;
            foreach (char c in csvLine)
            {
                if (c == '"') quoted = !quoted;
                else if (c == ',' && !quoted) columns++;
            }

            return columns;
        }
    }
}
#endif
