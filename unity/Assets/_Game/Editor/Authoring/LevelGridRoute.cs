using System;
using System.Collections.Generic;
using Kind = HoldTheHill.EditorTools.Authoring.LevelGridMap.Kind;

namespace HoldTheHill.EditorTools.Authoring
{
    /// <summary>
    /// The road rules of a <see cref="LevelGridMap"/>, same as level_grid.py: one unbranched route
    /// from S to H that never touches itself, its corners as waypoints, and the cells far enough
    /// from it to build on. Static over the map so the map file stays the grammar and the checks.
    /// </summary>
    internal static class LevelGridRoute
    {
        private static readonly (int Dx, int Dy)[] Steps = { (0, -1), (1, 0), (0, 1), (-1, 0) };

        internal static List<(int X, int Y)> Cells(LevelGridMap map, params Kind[] kinds)
        {
            var found = new List<(int X, int Y)>();
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    if (Array.IndexOf(kinds, map.KindAt(x, y)) >= 0)
                    {
                        found.Add((x, y));
                    }
                }
            }

            return found;
        }

        internal static int CountRegions(LevelGridMap map, Kind kind)
        {
            var seen = new HashSet<(int X, int Y)>();
            int regions = 0;
            foreach ((int X, int Y) start in Cells(map, kind))
            {
                if (!seen.Add(start))
                {
                    continue;
                }

                regions++;
                var todo = new Stack<(int X, int Y)>();
                todo.Push(start);
                while (todo.Count > 0)
                {
                    (int x, int y) = todo.Pop();
                    foreach ((int dx, int dy) in Steps)
                    {
                        var n = (X: x + dx, Y: y + dy);
                        if (n.X >= 0 && n.Y >= 0 && n.X < map.Width && n.Y < map.Height && map.KindAt(n.X, n.Y) == kind && seen.Add(n))
                        {
                            todo.Push(n);
                        }
                    }
                }
            }

            return regions;
        }

        /// <summary>Follows the road from S to H into <paramref name="route"/>; false with the reason in the map's errors.</summary>
        internal static bool Trace(LevelGridMap map, List<(int X, int Y)> route, HashSet<(int X, int Y)> routeSet)
        {
            List<(int X, int Y)> spawns = Cells(map, Kind.Spawn);
            if (spawns.Count != 1)
            {
                map.Errors.Add($"Needs exactly one S, found {spawns.Count}.");
                return false;
            }

            if (Cells(map, Kind.Hill).Count == 0)
            {
                map.Errors.Add("Needs at least one H.");
                return false;
            }

            if (CountRegions(map, Kind.Hill) != 1)
            {
                map.Errors.Add("The H cells must touch each other (one hill).");
                return false;
            }

            route.Add(spawns[0]);
            var inRoute = new HashSet<(int X, int Y)> { spawns[0] };
            while (true)
            {
                (int x, int y) = route[route.Count - 1];
                (int X, int Y)? previous = route.Count > 1 ? route[route.Count - 2] : ((int X, int Y)?)null;

                var nexts = new List<(int X, int Y)>();
                foreach ((int dx, int dy) in Steps)
                {
                    var n = (X: x + dx, Y: y + dy);
                    if (n.X < 0 || n.Y < 0 || n.X >= map.Width || n.Y >= map.Height)
                    {
                        continue;
                    }

                    Kind kind = map.KindAt(n.X, n.Y);
                    if ((kind == Kind.Road || kind == Kind.Hill) && (!previous.HasValue || previous.Value != n))
                    {
                        nexts.Add(n);
                    }
                }

                (int X, int Y)? hill = null;
                foreach ((int X, int Y) n in nexts)
                {
                    if (map.KindAt(n.X, n.Y) == Kind.Hill)
                    {
                        hill = n;
                        break;
                    }
                }

                if (hill.HasValue)
                {
                    route.Add(hill.Value);
                    break;
                }

                if (nexts.Count == 0)
                {
                    map.Errors.Add($"The road stops at row {y + 1}, column {x + 1} before reaching H.");
                    return false;
                }

                if (nexts.Count > 1)
                {
                    map.Errors.Add($"The road forks at row {y + 1}, column {x + 1}.");
                    return false;
                }

                if (inRoute.Contains(nexts[0]))
                {
                    map.Errors.Add($"The road loops back on itself at row {y + 1}, column {x + 1}.");
                    return false;
                }

                route.Add(nexts[0]);
                inRoute.Add(nexts[0]);
            }

            var routeIndex = new Dictionary<(int X, int Y), int>();
            for (int i = 0; i < route.Count; i++)
            {
                routeIndex[route[i]] = i;
                routeSet.Add(route[i]);
            }

            List<(int X, int Y)> stray = Cells(map, Kind.Road).FindAll(c => !routeSet.Contains(c));
            if (stray.Count > 0)
            {
                map.Errors.Add($"{stray.Count} road cell(s) are not on the route, first at row {stray[0].Y + 1}, " +
                           $"column {stray[0].X + 1}.");
                return false;
            }

            // Two stretches side by side would read as one wide road.
            for (int i = 0; i < route.Count; i++)
            {
                (int x, int y) = route[i];
                foreach ((int dx, int dy) in Steps)
                {
                    var n = (X: x + dx, Y: y + dy);
                    if (routeIndex.TryGetValue(n, out int j) && Math.Abs(j - i) != 1)
                    {
                        map.Errors.Add($"The road touches itself at row {y + 1}, column {x + 1}; leave a cell between stretches.");
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>The route's corners, start and end included.</summary>
        internal static void Corners(IReadOnlyList<(int X, int Y)> route, List<(int X, int Y)> waypoints)
        {
            waypoints.Add(route[0]);
            for (int i = 1; i < route.Count - 1; i++)
            {
                (int X, int Y) a = route[i - 1];
                (int X, int Y) b = route[i];
                (int X, int Y) c = route[i + 1];
                if ((b.X - a.X, b.Y - a.Y) != (c.X - b.X, c.Y - b.Y))
                {
                    waypoints.Add(b);
                }
            }

            waypoints.Add(route[route.Count - 1]);
        }

        internal static float DistanceToRoute(IReadOnlyList<(int X, int Y)> waypoints, int x, int y)
        {
            float best = float.PositiveInfinity;
            for (int i = 0; i + 1 < waypoints.Count; i++)
            {
                (int ax, int ay) = waypoints[i];
                (int bx, int by) = waypoints[i + 1];
                float abx = bx - ax;
                float aby = by - ay;
                float length = abx * abx + aby * aby;
                float t = length > 0f ? ((x - ax) * abx + (y - ay) * aby) / length : 0f;
                t = Math.Max(0f, Math.Min(1f, t));
                float px = ax + abx * t;
                float py = ay + aby * t;
                float dx = x - px;
                float dy = y - py;
                best = Math.Min(best, (float)Math.Sqrt(dx * dx + dy * dy));
            }

            return best;
        }

        /// <summary>Every floor or mat cell at least the clearance away from the route.</summary>
        internal static void BuildCells(LevelGridMap map, IReadOnlyList<(int X, int Y)> waypoints, HashSet<(int X, int Y)> buildCells)
        {
            foreach ((int X, int Y) c in Cells(map, Kind.Floor, Kind.Mat))
            {
                if (DistanceToRoute(waypoints, c.X, c.Y) >= LevelGridMap.Clearance)
                {
                    buildCells.Add(c);
                }
            }
        }
    }
}
