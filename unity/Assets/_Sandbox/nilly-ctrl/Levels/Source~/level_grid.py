"""Reads a hand-drawn level map (a text grid) and checks it.

A map file is a few `key: value` lines, a line of `---`, then the grid, one character per cell:

    S  enemy spawn (exactly one)        .  floor, buildable
    #  enemy road                       M  mat or rug, buildable
    H  the hill (one or more, touching) ^  cabinet or wall, blocked
    *  crumbs (resource, not buildable) F  fridge, blocked
    ~  spill (hazard, not buildable)    T  table or chair leg, blocked

The road must be one unbranched line of `#` from `S` to an `H`, moving up, down, left or
right. The game has a single EnemyPath, so a second spawn or a fork is an error here, not
a feature. Towers snap to whole cells and must stand at least CLEARANCE from the road's
centre line (GrayboxTowerPlacer), so the cells touching the road are never buildable.
"""
import math

LEGEND = {
    ".": "floor", "#": "road", "S": "spawn", "H": "hill", "^": "cabinet", "F": "fridge",
    "T": "leg", "M": "mat", "*": "crumbs", "~": "spill",
}
BLOCKED = {"cabinet", "fridge", "leg"}
TALL = {"cabinet", "fridge"}
BUILDABLE = {"floor", "mat"}
CLEARANCE = 1.2  # GrayboxTowerPlacer.IsPositionClearOfPath
STEPS = ((0, -1), (1, 0), (0, 1), (-1, 0))  # N E S W, in grid rows (row 0 is the top)


class LevelError(ValueError):
    pass


class Level:
    def __init__(self, path):
        self.path = path
        self.meta = {}
        rows = []
        in_grid = False
        with open(path, encoding="utf-8") as fh:
            for raw in fh:
                line = raw.rstrip("\r\n")
                if not in_grid:
                    if line.strip() == "---":
                        in_grid = True
                    elif ":" in line:
                        key, value = line.split(":", 1)
                        self.meta[key.strip().lower()] = value.strip()
                    continue
                if line.strip():
                    rows.append(line.rstrip())
        if not rows:
            raise LevelError(f"{path}: no grid found after a '---' line")
        self.width = max(len(r) for r in rows)
        self.height = len(rows)
        for y, r in enumerate(rows):
            if len(r) != self.width:
                raise LevelError(f"{path}: row {y + 1} is {len(r)} wide, the widest row is {self.width}")
            for x, ch in enumerate(r):
                if ch not in LEGEND:
                    raise LevelError(f"{path}: row {y + 1}, column {x + 1}: unknown character {ch!r}")
        self.rows = rows
        self.name = self.meta.get("name", "Level")
        self.theme = self.meta.get("theme", "kitchen")
        self.route = self._trace_route()
        self.waypoints = self._corners(self.route)
        self.build_cells = self._build_cells()

    # ---- cells ----

    def kind(self, x, y):
        """What is at a cell; anything off the map counts as cabinet (a wall)."""
        if 0 <= x < self.width and 0 <= y < self.height:
            return LEGEND[self.rows[y][x]]
        return "cabinet"

    def cells(self, *kinds):
        return [(x, y) for y in range(self.height) for x in range(self.width) if self.kind(x, y) in kinds]

    def is_roadish(self, x, y):
        return 0 <= x < self.width and 0 <= y < self.height and self.kind(x, y) in ("road", "spawn", "hill")

    def on_route(self, x, y):
        return (x, y) in self._route_set

    def regions(self, kind):
        """Groups of touching cells of one kind, each a sorted list."""
        seen, out = set(), []
        for start in self.cells(kind):
            if start in seen:
                continue
            group, todo = [], [start]
            seen.add(start)
            while todo:
                x, y = todo.pop()
                group.append((x, y))
                for dx, dy in STEPS:
                    n = (x + dx, y + dy)
                    if n not in seen and 0 <= n[0] < self.width and 0 <= n[1] < self.height \
                            and self.kind(*n) == kind:
                        seen.add(n)
                        todo.append(n)
            out.append(sorted(group))
        return out

    # ---- road ----

    def _trace_route(self):
        where = f"{self.path}:"
        spawns = self.cells("spawn")
        if len(spawns) != 1:
            raise LevelError(f"{where} needs exactly one S, found {len(spawns)}")
        if not self.cells("hill"):
            raise LevelError(f"{where} needs at least one H")
        if len(self.regions("hill")) != 1:
            raise LevelError(f"{where} the H cells must touch each other (one hill)")

        route = [spawns[0]]
        while True:
            x, y = route[-1]
            previous = route[-2] if len(route) > 1 else None
            nexts = [(x + dx, y + dy) for dx, dy in STEPS
                     if self.kind(x + dx, y + dy) in ("road", "hill") and (x + dx, y + dy) != previous
                     and 0 <= x + dx < self.width and 0 <= y + dy < self.height]
            hills = [n for n in nexts if self.kind(*n) == "hill"]
            if hills:
                route.append(hills[0])
                break
            if not nexts:
                raise LevelError(f"{where} the road stops at row {y + 1}, column {x + 1} before reaching H")
            if len(nexts) > 1:
                raise LevelError(f"{where} the road forks at row {y + 1}, column {x + 1}")
            if nexts[0] in route:
                raise LevelError(f"{where} the road loops back on itself at row {y + 1}, column {x + 1}")
            route.append(nexts[0])

        self._route_set = set(route)
        stray = [c for c in self.cells("road") if c not in self._route_set]
        if stray:
            x, y = stray[0]
            raise LevelError(f"{where} {len(stray)} road cell(s) are not on the route, first at "
                             f"row {y + 1}, column {x + 1}")
        # Two stretches side by side would read as one wide road and let enemies look like they cut across.
        for i, (x, y) in enumerate(route):
            for dx, dy in STEPS:
                n = (x + dx, y + dy)
                if n in self._route_set and abs(route.index(n) - i) != 1:
                    raise LevelError(f"{where} the road touches itself at row {y + 1}, column {x + 1}; "
                                     "leave a cell between stretches")
        return route

    @staticmethod
    def _corners(route):
        points = [route[0]]
        for i in range(1, len(route) - 1):
            a, b, c = route[i - 1], route[i], route[i + 1]
            if (b[0] - a[0], b[1] - a[1]) != (c[0] - b[0], c[1] - b[1]):
                points.append(b)
        points.append(route[-1])
        return points

    def distance_to_route(self, x, y):
        best = float("inf")
        for (ax, ay), (bx, by) in zip(self.waypoints, self.waypoints[1:]):
            abx, aby = bx - ax, by - ay
            length = abx * abx + aby * aby
            t = max(0.0, min(1.0, ((x - ax) * abx + (y - ay) * aby) / length)) if length else 0.0
            best = min(best, math.hypot(x - (ax + abx * t), y - (ay + aby * t)))
        return best

    def _build_cells(self):
        return [c for c in self.cells(*BUILDABLE) if self.distance_to_route(*c) >= CLEARANCE]

    # ---- world space ----

    def world(self, x, y):
        """Cell centre in Unity units. Centres sit on whole numbers, because towers snap to them."""
        return (x - self.width // 2, self.height // 2 - y)

    def to_json(self):
        centre = ((self.width - 1) / 2 - self.width // 2, self.height // 2 - (self.height - 1) / 2)
        return {
            "name": self.name,
            "theme": self.theme,
            "width": self.width,
            "height": self.height,
            "pixelsPerCell": 32,
            "centre": {"x": centre[0], "y": centre[1]},
            "waypoints": [{"x": wx, "y": wy} for wx, wy in (self.world(*p) for p in self.waypoints)],
            "hill": [{"x": wx, "y": wy} for wx, wy in (self.world(*p) for p in self.cells("hill"))],
            "build": [{"x": wx, "y": wy} for wx, wy in (self.world(*p) for p in self.build_cells)],
            "blocked": [{"x": wx, "y": wy} for wx, wy in (self.world(*p) for p in self.cells(*BLOCKED))],
            "crumbs": [{"x": wx, "y": wy} for wx, wy in (self.world(*p) for p in self.cells("crumbs"))],
            "spills": [{"x": wx, "y": wy} for wx, wy in (self.world(*p) for p in self.cells("spill"))],
            "routeCells": len(self.route),
        }
