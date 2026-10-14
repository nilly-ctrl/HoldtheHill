using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Towers;
using System;
using System.Collections.Generic;

namespace HoldTheHill.Sandbox.NillyCtrl.Editor
{
    /// <summary>
    /// An analytic, scene-free estimate of how a tower set fares against a wave list.
    /// </summary>
    /// <remarks>
    /// This is a model, not the game. The route is one line of length <see cref="BalanceSettings.PathLength"/>.
    /// A tower reaches a stretch of it (a chord set by range and its distance from the road) and
    /// deals its damage per second to the front-most enemies in that stretch. Not modelled:
    /// slows and statuses, shields regenerating, healers, projectile travel time and misses,
    /// targeting priorities, tower upgrades, abilities, flyers and the Warden. Use it to see which
    /// waves are far from the mark and in which direction, not to predict exact kills.
    /// The real combat is measured by the in-scene harness (GrayboxBalanceHarness).
    /// </remarks>
    public static class BalanceSimulator
    {
        public static BalanceSimResult Run(IList<BalanceTower> towers, IList<BalanceWave> waves, BalanceSettings settings)
        {
            settings = settings ?? new BalanceSettings();
            var result = new BalanceSimResult { Settings = settings };

            float pathLength = Math.Max(0.01f, settings.PathLength);
            float dt = Math.Min(0.5f, Math.Max(0.005f, settings.TimeStep));

            List<BalanceTower> buyable = DamagingTowers(towers, settings, result.Notes);
            buyable.Sort((a, b) => a.Cost != b.Cost ? a.Cost.CompareTo(b.Cost) : string.CompareOrdinal(a.Name, b.Name));

            var placed = new List<BalancePlacedTower>();
            int gold = settings.StartingFood;
            int rotation = 0;
            float cumulativeLeak = 0f;

            if (waves == null)
            {
                return result;
            }

            for (int w = 0; w < waves.Count; w++)
            {
                BalanceWave wave = waves[w];
                var wr = new BalanceWaveResult
                {
                    Number = w + 1,
                    Name = wave.Name,
                    Enemies = wave.Enemies.Count,
                    TotalHp = wave.TotalHp,
                    FoodAtStart = gold
                };

                // Buy before the wave starts. Gold that cannot buy the next tower is kept.
                while (placed.Count < settings.MaxTowers)
                {
                    BalanceTower pick = PickPurchase(buyable, settings, gold, ref rotation, pathLength);
                    if (pick == null)
                    {
                        break;
                    }

                    gold -= pick.Cost;
                    wr.FoodSpent += pick.Cost;
                    placed.Add(Place(pick, placed, settings, pathLength));
                    result.Purchases.Add($"before wave {w + 1}: {pick.Name} ({pick.Cost})");
                }

                wr.TowersOwned = placed.Count;
                foreach (BalancePlacedTower tower in placed)
                {
                    wr.DefenceDps += tower.Definition.DpsPerTarget * settings.DamageMultiplier;
                }

                SimulateWave(placed, wave, settings, pathLength, dt, wr, result.Notes);

                gold += wr.FoodEarned;
                wr.FoodAtEnd = gold;

                cumulativeLeak += wr.LeakPoints;
                if (result.HillFallsInWave == 0 && cumulativeLeak >= settings.HillHealth)
                {
                    result.HillFallsInWave = wr.Number;
                }

                Judge(wr, settings);

                result.Waves.Add(wr);
                result.TotalKilled += wr.Killed;
                result.TotalLeaked += wr.Leaked;
                result.TotalLeakPoints += wr.LeakPoints;
            }

            if (placed.Count >= settings.MaxTowers)
            {
                result.Notes.Add($"Tower cap of {settings.MaxTowers} reached; later gold is not spent (no upgrades are modelled).");
            }

            return result;
        }

        private static List<BalanceTower> DamagingTowers(IList<BalanceTower> towers, BalanceSettings settings, List<string> notes)
        {
            var list = new List<BalanceTower>();
            if (towers == null)
            {
                return list;
            }

            foreach (BalanceTower tower in towers)
            {
                if (tower == null)
                {
                    continue;
                }

                if (!tower.DealsDamage)
                {
                    notes.Add($"{tower.Name}: no damage modelled, never bought. {tower.Note}".TrimEnd());
                }
                else if (tower.Range <= settings.PathOffset)
                {
                    notes.Add($"{tower.Name}: range {tower.Range:0.##} does not reach the route from {settings.PathOffset:0.##} away, never bought.");
                }
                else
                {
                    list.Add(tower);
                }
            }

            if (list.Count == 0)
            {
                notes.Add("No tower in the input deals damage: every enemy will leak.");
            }

            return list;
        }

        private static BalanceTower PickPurchase(List<BalanceTower> buyable, BalanceSettings settings, int gold,
            ref int rotation, float pathLength)
        {
            if (buyable.Count == 0)
            {
                return null;
            }

            if (settings.Strategy == BalanceBuyStrategy.RoundRobin)
            {
                BalanceTower next = buyable[rotation % buyable.Count];
                if (next.Cost > gold)
                {
                    return null;
                }

                rotation++;
                return next;
            }

            BalanceTower best = null;
            float bestValue = -1f;
            foreach (BalanceTower tower in buyable)
            {
                float reach = Math.Min(pathLength, 2f * HalfReach(tower, settings));
                float value = tower.CrowdDps * Math.Max(reach, 0.01f) / Math.Max(1, tower.Cost);
                if (value > bestValue)
                {
                    bestValue = value;
                    best = tower;
                }
            }

            return best != null && best.Cost <= gold ? best : null;
        }

        private static float HalfReach(BalanceTower tower, BalanceSettings settings)
        {
            float squared = tower.Range * tower.Range - settings.PathOffset * settings.PathOffset;
            return squared > 0f ? (float)Math.Sqrt(squared) : 0f;
        }

        // Puts the tower where the route is least defended: it maximises the sum, over the stretch
        // it would reach, of 1 / (1 + the damage already covering each point).
        private static BalancePlacedTower Place(BalanceTower tower, List<BalancePlacedTower> placed,
            BalanceSettings settings, float pathLength)
        {
            const float Step = 0.5f;
            float half = HalfReach(tower, settings);
            float bestCenter = 0f;
            float bestGain = float.NegativeInfinity;

            for (float center = 0f; center <= pathLength + 0.001f; center += Step)
            {
                float gain = 0f;
                float from = Math.Max(0f, center - half);
                float to = Math.Min(pathLength, center + half);
                for (float p = from; p <= to + 0.0001f; p += Step)
                {
                    gain += 1f / (1f + CoverageAt(placed, p));
                }

                if (gain > bestGain + 0.0001f)
                {
                    bestGain = gain;
                    bestCenter = center;
                }
            }

            return new BalancePlacedTower(tower, bestCenter, half);
        }

        private static float CoverageAt(List<BalancePlacedTower> placed, float position)
        {
            float total = 0f;
            foreach (BalancePlacedTower tower in placed)
            {
                if (tower.Covers(position))
                {
                    total += tower.Definition.DpsPerTarget;
                }
            }

            return total;
        }

        private static void SimulateWave(List<BalancePlacedTower> placed, BalanceWave wave, BalanceSettings settings,
            float pathLength, float dt, BalanceWaveResult wr, List<string> notes)
        {
            var enemies = new List<BalanceEnemy>(wave.Enemies);
            enemies.Sort((a, b) => a.SpawnTime.CompareTo(b.SpawnTime));

            int count = enemies.Count;
            var position = new float[count];
            var health = new float[count];
            var state = new byte[count]; // 0 waiting, 1 on the route, 2 killed, 3 leaked

            for (int i = 0; i < count; i++)
            {
                health[i] = enemies[i].Hp;
                if (enemies[i].SpawnTime > wr.SpawnSeconds)
                {
                    wr.SpawnSeconds = enemies[i].SpawnTime;
                }
            }

            var order = new List<int>(count);
            int resolved = 0;
            float time = 0f;
            float ttkSum = 0f;
            float depthSum = 0f;

            while (resolved < count && time <= settings.MaxWaveSeconds)
            {
                for (int i = 0; i < count; i++)
                {
                    if (state[i] == 0 && enemies[i].SpawnTime <= time + 1e-5f)
                    {
                        state[i] = 1;
                    }
                }

                order.Clear();
                for (int i = 0; i < count; i++)
                {
                    if (state[i] == 1)
                    {
                        order.Add(i);
                    }
                }

                // Front-most first: towers hit the enemies nearest the hill.
                order.Sort((a, b) => position[b].CompareTo(position[a]));

                foreach (BalancePlacedTower tower in placed)
                {
                    BalanceTower def = tower.Definition;
                    int hits = 0;
                    float factor = 1f;
                    foreach (int i in order)
                    {
                        if (health[i] <= 0f || !tower.Covers(position[i]))
                        {
                            continue;
                        }

                        health[i] -= def.DpsPerTarget * settings.DamageMultiplier * dt * factor;
                        factor *= def.FalloffPerTarget;
                        hits++;
                        if (hits >= def.MaxTargets)
                        {
                            break;
                        }
                    }
                }

                foreach (int i in order)
                {
                    if (health[i] > 0f)
                    {
                        continue;
                    }

                    state[i] = 2;
                    resolved++;
                    wr.Killed++;
                    wr.FoodEarned += (int)Math.Round(enemies[i].Bounty * settings.BountyMultiplier);
                    ttkSum += Math.Max(0f, time - enemies[i].SpawnTime);
                    depthSum += Math.Min(1f, Math.Max(0f, position[i] / pathLength));
                    wr.ClearSeconds = time;
                }

                foreach (int i in order)
                {
                    if (state[i] != 1)
                    {
                        continue;
                    }

                    position[i] += enemies[i].Speed * dt;
                    if (position[i] >= pathLength)
                    {
                        state[i] = 3;
                        resolved++;
                        wr.Leaked++;
                        wr.LeakPoints += enemies[i].BreachDamage;
                        wr.ClearSeconds = time + dt;
                    }
                }

                time += dt;
            }

            int stuck = 0;
            for (int i = 0; i < count; i++)
            {
                if (state[i] < 2)
                {
                    stuck++;
                    wr.Leaked++;
                    wr.LeakPoints += enemies[i].BreachDamage;
                }
            }

            if (stuck > 0)
            {
                wr.ClearSeconds = settings.MaxWaveSeconds;
                notes.Add($"Wave {wr.Number}: {stuck} enemy(ies) never finished within {settings.MaxWaveSeconds:0}s (speed 0?); counted as leaks.");
            }

            wr.FoodEarned += settings.WaveClearBonus;

            if (wr.Killed > 0)
            {
                wr.AvgTimeToKill = ttkSum / wr.Killed;
                wr.MeanKillDepth = depthSum / wr.Killed;
            }
        }

        private static void Judge(BalanceWaveResult wr, BalanceSettings settings)
        {
            if (wr.Enemies == 0)
            {
                wr.Verdict = BalanceVerdict.OnTarget;
                wr.Reason = "empty wave";
                return;
            }

            float leakFraction = (float)wr.Leaked / wr.Enemies;
            if (leakFraction >= settings.UnderTunedLeakFraction)
            {
                wr.Verdict = BalanceVerdict.UnderTuned;
                wr.Reason = $"{wr.Leaked} of {wr.Enemies} enemies leak ({leakFraction * 100f:0}%), costing {wr.LeakPoints:0} hill health";
            }
            else if (wr.Leaked == 0 && wr.Killed > 0 && wr.MeanKillDepth <= settings.OverTunedKillDepth)
            {
                wr.Verdict = BalanceVerdict.OverTuned;
                wr.Reason = $"nothing leaks and enemies die on average {wr.MeanKillDepth * 100f:0}% of the way along the route";
            }
            else
            {
                wr.Verdict = BalanceVerdict.OnTarget;
                wr.Reason = wr.Leaked > 0
                    ? $"{wr.Leaked} leak(s), under the {settings.UnderTunedLeakFraction * 100f:0}% limit"
                    : $"no leaks, enemies die {wr.MeanKillDepth * 100f:0}% of the way along the route";
            }
        }
    }
}
