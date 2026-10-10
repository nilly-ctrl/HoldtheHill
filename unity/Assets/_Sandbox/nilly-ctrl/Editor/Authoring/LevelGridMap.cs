using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace HoldTheHill.Sandbox.NillyCtrl.Editor
{
    /// <summary>
    /// A hand-drawn level map read from a text grid and checked with the same rules as
    /// <c>Levels/Source~/level_grid.py</c>: exactly one S, one hill, one unbranched road from S to H.
    /// </summary>
    /// <remarks>
    /// Editor-only and independent of UnityEngine so it stays easy to compare with the python checker.
    /// Errors are collected in <see cref="Errors"/>; a map with any error has no route or build cells.
    /// </remarks>
    internal sealed class LevelGridMap
    {
        public enum Kind
        {
            Floor,
            Road,
            Spawn,
            Hill,
            Cabinet,
            Fridge,
            Leg,
            Mat,
            Crumbs,
            Spill,
        }

        /// <summary>GrayboxTowerPlacer.IsPositionClearOfPath, same as CLEARANCE in level_grid.py.</summary>
        public const float Clearance = 1.2f;

        private static readonly (int Dx, int Dy)[] Steps = { (0, -1), (1, 0), (0, 1), (-1, 0) };

        private readonly List<(int X, int Y)> _route = new List<(int X, int Y)>();
        private readonly List<(int X, int Y)> _waypoints = new List<(int X, int Y)>();
        private readonly HashSet<(int X, int Y)> _buildCells = new HashSet<(int X, int Y)>();
        private readonly HashSet<(int X, int Y)> _routeSet = new HashSet<(int X, int Y)>();
        private string[] _rows = new string[0];

        public string SourcePath { get; private set; }

        public Dictionary<string, string> Meta { get; } = new Dictionary<string, string>();

        public string Name { get; private set; } = "Level";

        public string Theme { get; private set; } = "kitchen";

        public int Width { get; private set; }

        public int Height { get; private set; }

        public List<string> Errors { get; } = new List<string>();

        public List<string> Warnings { get; } = new List<string>();

        public bool Valid => Errors.Count == 0;

        public IReadOnlyList<(int X, int Y)> Route => _route;

        public IReadOnlyList<(int X, int Y)> Waypoints => _waypoints;

        public IReadOnlyCollection<(int X, int Y)> BuildCells => _buildCells;

        /// <summary>The asset/folder name build_level.py uses: the name without spaces, each word capitalised.</summary>
        public string AssetName => ToAssetName(Name);

        public static LevelGridMap Load(string path)
        {
            string text;
            try
            {
                text = File.ReadAllText(path, Encoding.UTF8);
            }
            catch (Exception e)
            {
                var failed = new LevelGridMap { SourcePath = path };
                failed.Errors.Add($"Cannot read the file: {e.Message}");
                return failed;
            }

            return Parse(text, path);
        }

        public static LevelGridMap Parse(string text, string label)
        {
            var map = new LevelGridMap { SourcePath = label };
            var rows = new List<string>();
            bool inGrid = false;

            foreach (string raw in text.Split('\n'))
            {
                string line = raw.TrimEnd('\r');
                if (!inGrid)
                {
                    if (line.Trim() == "---")
                    {
                        inGrid = true;
                    }
                    else
                    {
                        int colon = line.IndexOf(':');
                        if (colon >= 0)
                        {
                            map.Meta[line.Substring(0, colon).Trim().ToLowerInvariant()] = line.Substring(colon + 1).Trim();
                        }
                    }

                    continue;
                }

                if (line.Trim().Length > 0)
                {
                    rows.Add(line.TrimEnd());
                }
            }

            if (rows.Count == 0)
            {
                map.Errors.Add("No grid found after a '---' line.");
                return map;
            }

            int width = 0;
            foreach (string row in rows)
            {
                width = Math.Max(width, row.Length);
            }

            map.Width = width;
            map.Height = rows.Count;
            for (int y = 0; y < rows.Count; y++)
            {
                if (rows[y].Length != width)
                {
                    map.Errors.Add($"Row {y + 1} is {rows[y].Length} wide, the widest row is {width}.");
                    return map;
                }

                for (int x = 0; x < width; x++)
                {
                    if (!TryKind(rows[y][x], out _))
                    {
                        map.Errors.Add($"Row {y + 1}, column {x + 1}: unknown character '{rows[y][x]}'.");
                        return map;
                    }
                }
            }

            map._rows = rows.ToArray();
            map.Name = map.Meta.TryGetValue("name", out string name) && name.Length > 0 ? name : "Level";
            map.Theme = map.Meta.TryGetValue("theme", out string theme) && theme.Length > 0 ? theme : "kitchen";

            if (!map.TraceRoute())
            {
                return map;
            }

            map.FindCorners();
            map.FindBuildCells();
            if (map._buildCells.Count == 0)
            {
                map.Warnings.Add("No cell is buildable: towers need floor or mat at least 1.2 cells from the road.");
            }

            if (AssetNameIsEmpty(map.Name))
            {
                map.Errors.Add("The name: line has no letters or digits, so no asset name can be made from it.");
            }

            return map;
        }

        public Kind KindAt(int x, int y)
        {
            // Anything off the map counts as cabinet (a wall), as in level_grid.py.
            if (x < 0 || y < 0 || x >= Width || y >= Height || _rows.Length == 0)
            {
                return Kind.Cabinet;
            }

            TryKind(_rows[y][x], out Kind kind);
            return kind;
        }

        public bool IsBuildCell(int x, int y) => _buildCells.Contains((x, y));

        public bool IsOnRoute(int x, int y) => _routeSet.Contains((x, y));

        /// <summary>The grid as text with every buildable cell shown as '+'.</summary>
        public string ToAscii()
        {
            var sb = new StringBuilder();
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    sb.Append(IsBuildCell(x, y) ? '+' : _rows[y][x]);
                }

                sb.Append('\n');
            }

            return sb.ToString();
        }

        /// <summary>build_level.py: the name's words capitalised, everything but letters and digits dropped.</summary>
        public static string ToAssetName(string name)
        {
            var sb = new StringBuilder();
            bool previousIsLetter = false;
            foreach (char c in name)
            {
                bool letter = char.IsLetter(c);
                if (letter && c < 128)
                {
                    sb.Append(previousIsLetter ? char.ToLowerInvariant(c) : char.ToUpperInvariant(c));
                }
                else if (char.IsDigit(c) && c < 128)
                {
                    sb.Append(c);
                }

                previousIsLetter = letter;
            }

            return sb.ToString();
        }

        private static bool AssetNameIsEmpty(string name) => ToAssetName(name).Length == 0;

        private static bool TryKind(char c, out Kind kind)
        {
            switch (c)
            {
                case '.': kind = Kind.Floor; return true;
                case '#': kind = Kind.Road; return true;
                case 'S': kind = Kind.Spawn; return true;
                case 'H': kind = Kind.Hill; return true;
                case '^': kind = Kind.Cabinet; return true;
                case 'F': kind = Kind.Fridge; return true;
                case 'T': kind = Kind.Leg; return true;
                case 'M': kind = Kind.Mat; return true;
                case '*': kind = Kind.Crumbs; return true;
                case '~': kind = Kind.Spill; return true;
                default: kind = Kind.Floor; return false;
            }
        }

        private List<(int X, int Y)> Cells(params Kind[] kinds)
        {
            var found = new List<(int X, int Y)>();
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (Array.IndexOf(kinds, KindAt(x, y)) >= 0)
                    {
                        found.Add((x, y));
                    }
                }
            }

            return found;
        }

        private int CountRegions(Kind kind)
        {
            var seen = new HashSet<(int X, int Y)>();
            int regions = 0;
            foreach ((int X, int Y) start in Cells(kind))
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
                        if (n.X >= 0 && n.Y >= 0 && n.X < Width && n.Y < Height && KindAt(n.X, n.Y) == kind && seen.Add(n))
                        {
                            todo.Push(n);
                        }
                    }
                }
            }

            return regions;
        }

        private bool TraceRoute()
        {
            List<(int X, int Y)> spawns = Cells(Kind.Spawn);
            if (spawns.Count != 1)
            {
                Errors.Add($"Needs exactly one S, found {spawns.Count}.");
                return false;
            }

            if (Cells(Kind.Hill).Count == 0)
            {
                Errors.Add("Needs at least one H.");
                return false;
            }

            if (CountRegions(Kind.Hill) != 1)
            {
                Errors.Add("The H cells must touch each other (one hill).");
                return false;
            }

            _route.Add(spawns[0]);
            var inRoute = new HashSet<(int X, int Y)> { spawns[0] };
            while (true)
            {
                (int x, int y) = _route[_route.Count - 1];
                (int X, int Y)? previous = _route.Count > 1 ? _route[_route.Count - 2] : ((int X, int Y)?)null;

                var nexts = new List<(int X, int Y)>();
                foreach ((int dx, int dy) in Steps)
                {
                    var n = (X: x + dx, Y: y + dy);
                    if (n.X < 0 || n.Y < 0 || n.X >= Width || n.Y >= Height)
                    {
                        continue;
                    }

                    Kind kind = KindAt(n.X, n.Y);
                    if ((kind == Kind.Road || kind == Kind.Hill) && (!previous.HasValue || previous.Value != n))
                    {
                        nexts.Add(n);
                    }
                }

                (int X, int Y)? hill = null;
                foreach ((int X, int Y) n in nexts)
                {
                    if (KindAt(n.X, n.Y) == Kind.Hill)
                    {
                        hill = n;
                        break;
                    }
                }

                if (hill.HasValue)
                {
                    _route.Add(hill.Value);
                    break;
                }

                if (nexts.Count == 0)
                {
                    Errors.Add($"The road stops at row {y + 1}, column {x + 1} before reaching H.");
                    return false;
                }

                if (nexts.Count > 1)
                {
                    Errors.Add($"The road forks at row {y + 1}, column {x + 1}.");
                    return false;
                }

                if (inRoute.Contains(nexts[0]))
                {
                    Errors.Add($"The road loops back on itself at row {y + 1}, column {x + 1}.");
                    return false;
                }

                _route.Add(nexts[0]);
                inRoute.Add(nexts[0]);
            }

            var routeIndex = new Dictionary<(int X, int Y), int>();
            for (int i = 0; i < _route.Count; i++)
            {
                routeIndex[_route[i]] = i;
                _routeSet.Add(_route[i]);
            }

            List<(int X, int Y)> stray = Cells(Kind.Road).FindAll(c => !_routeSet.Contains(c));
            if (stray.Count > 0)
            {
                Errors.Add($"{stray.Count} road cell(s) are not on the route, first at row {stray[0].Y + 1}, " +
                           $"column {stray[0].X + 1}.");
                return false;
            }

            // Two stretches side by side would read as one wide road.
            for (int i = 0; i < _route.Count; i++)
            {
                (int x, int y) = _route[i];
                foreach ((int dx, int dy) in Steps)
                {
                    var n = (X: x + dx, Y: y + dy);
                    if (routeIndex.TryGetValue(n, out int j) && Math.Abs(j - i) != 1)
                    {
                        Errors.Add($"The road touches itself at row {y + 1}, column {x + 1}; leave a cell between stretches.");
                        return false;
                    }
                }
            }

            return true;
        }

        private void FindCorners()
        {
            _waypoints.Add(_route[0]);
            for (int i = 1; i < _route.Count - 1; i++)
            {
                (int X, int Y) a = _route[i - 1];
                (int X, int Y) b = _route[i];
                (int X, int Y) c = _route[i + 1];
                if ((b.X - a.X, b.Y - a.Y) != (c.X - b.X, c.Y - b.Y))
                {
                    _waypoints.Add(b);
                }
            }

            _waypoints.Add(_route[_route.Count - 1]);
        }

        private float DistanceToRoute(int x, int y)
        {
            float best = float.PositiveInfinity;
            for (int i = 0; i + 1 < _waypoints.Count; i++)
            {
                (int ax, int ay) = _waypoints[i];
                (int bx, int by) = _waypoints[i + 1];
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

        private void FindBuildCells()
        {
            foreach ((int X, int Y) c in Cells(Kind.Floor, Kind.Mat))
            {
                if (DistanceToRoute(c.X, c.Y) >= Clearance)
                {
                    _buildCells.Add(c);
                }
            }
        }
    }
}
