"""Key, mouse and gamepad prompts for the body TTF, drawn in one colour.

Each is nine rows tall: one row above the capitals to one row below the baseline. They live in
the Private Use Area from U+E000 (Unicode has no code points for them); the four face buttons
are also at the circled letters. BUTTONS is (code point, name, rows); the names become the
constants in DamageNumbers/PixelGlyphs.cs.
"""
import glyphs_tiny

TOP = 8      # rows from a prompt's first row down to the baseline


def _tiny(text):
    """Tiny text as a list of rows of booleans, one column between letters."""
    rows = [[] for _ in range(5)]
    for i, ch in enumerate(text):
        g = glyphs_tiny.GLYPHS[ch]
        for y in range(5):
            if i:
                rows[y].append(False)
            rows[y].extend(c == "#" for c in g[y])
    return rows


def _rows(grid):
    return ["".join("#" if c else "." for c in row) for row in grid]


def keycap(text):
    """A filled key with the text cut out of it."""
    t = _tiny(text)
    w = len(t[0]) + 4
    grid = [[True] * w for _ in range(9)]
    for y in (0, 8):
        grid[y][0] = grid[y][w - 1] = False
    for y in range(5):
        for x, on in enumerate(t[y]):
            if on:
                grid[y + 2][x + 2] = False
    return _rows(grid)


def disc(letter):
    """A filled round button with a letter cut out of it."""
    shape = ["..#####..", ".#######.", "#########", "#########", "#########",
             "#########", "#########", ".#######.", "..#####.."]
    grid = [[c == "#" for c in row] for row in shape]
    t = _tiny(letter)
    x0 = (9 - len(t[0])) // 2
    for y in range(5):
        for x, on in enumerate(t[y]):
            if on:
                grid[y + 2][x + x0] = False
    return _rows(grid)


def dpad(direction):
    """A d-pad drawn as an outline with one arm filled."""
    inside = lambda x, y: (3 <= x <= 5) or (3 <= y <= 5)
    arm = {"up": lambda x, y: y < 3, "down": lambda x, y: y > 5,
           "left": lambda x, y: x < 3, "right": lambda x, y: x > 5}[direction]
    grid = [[False] * 9 for _ in range(9)]
    for y in range(9):
        for x in range(9):
            if not inside(x, y):
                continue
            edge = any(not (0 <= x + dx < 9 and 0 <= y + dy < 9 and inside(x + dx, y + dy))
                       for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)))
            grid[y][x] = edge or arm(x, y)
    return _rows(grid)


def mouse(button):
    """A mouse outline with the left, right or middle button filled (or none)."""
    shape = [".#####.", "#..#..#", "#..#..#", "#..#..#", "#######", "#.....#", "#.....#", "#.....#", ".#####."]
    grid = [[c == "#" for c in row] for row in shape]
    for y in range(1, 4):
        if button == "left":
            grid[y][1] = grid[y][2] = True
        elif button == "right":
            grid[y][4] = grid[y][5] = True
        elif button == "middle" and y < 3:
            grid[y][2] = grid[y][4] = True
    return _rows(grid)


def _build():
    out = []
    add = lambda code, name, rows: out.append((code, name, rows))
    for i, (name, text) in enumerate([("KeyEsc", "ESC"), ("KeyTab", "TAB"), ("KeySpace", "SPACE"),
                                      ("KeyEnter", "ENTER"), ("KeyShift", "SHIFT"), ("KeyCtrl", "CTRL"),
                                      ("KeyAlt", "ALT"), ("KeyDel", "DEL"), ("KeyUp", "^"), ("KeyDown", "v"),
                                      ("KeyLeft", "<"), ("KeyRight", ">")]):
        add(0xE000 + i, name, keycap(text))
    for d in range(10):
        add(0xE010 + d, f"Key{d}", keycap(str(d)))
    for i in range(26):
        add(0xE020 + i, "Key" + chr(65 + i), keycap(chr(65 + i)))
    for n in range(1, 13):
        add(0xE040 + n - 1, f"KeyF{n}", keycap(f"F{n}"))
    for i, b in enumerate(["left", "right", "middle", "none"]):
        add(0xE050 + i, "Mouse" + ("" if b == "none" else b.capitalize()), mouse(b))
    for i, letter in enumerate("ABXY"):
        add(0xE060 + i, "Pad" + letter, disc(letter))
    for i, text in enumerate(["LB", "RB", "LT", "RT"]):
        add(0xE064 + i, "Pad" + text.capitalize(), keycap(text))
    add(0xE068, "PadLeftStick", disc("L"))
    add(0xE069, "PadRightStick", disc("R"))
    for i, d in enumerate(["up", "down", "left", "right"]):
        add(0xE06A + i, "PadDpad" + d.capitalize(), dpad(d))
    add(0xE06E, "PadStart", keycap("START"))
    add(0xE06F, "PadSelect", keycap("SELECT"))
    return out


BUTTONS = _build()
# The circled capitals type the same glyphs as the four face buttons and the two sticks.
ALIASES = {"Ⓐ": 0xE060, "Ⓑ": 0xE061, "Ⓧ": 0xE062, "Ⓨ": 0xE063, "Ⓛ": 0xE068, "Ⓡ": 0xE069}
