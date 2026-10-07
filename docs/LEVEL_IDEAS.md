# Hold the Hill — Level Map Ideas, Batch 1

Twelve standalone map concepts, ordered from grounded to strange. Dream-big brief: nothing here is limited by the semester or by what the code supports today. Local brainstorm file, not for commit.

**Brief (from Nilly, 2026-10-05):** ant-scale world that escalates with no ceiling on weirdness; loose collection, no campaign order yet; cover fixed roads, hill-in-the-centre, open-field mazing and changing paths; vary levels through hazards and weather, player traversal, map resources, and special rules or bosses. Player role is undecided, so each level notes which role it favours.

**Map legend**

| Mark | Meaning |
|---|---|
| `H` | the hill |
| `S` | enemy spawn |
| `#` | enemy path |
| `.` | buildable ground |
| `^` | blocked or raised ground |
| `~` | water or other hazard zone |
| `*` | resource |
| other letters | explained under each map |

---

## 1. The Crack in the Patio
*Fixed road. The grounded opener.*

```
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
S#######..^^^^^^..*..^^^^^^.....^
^......#..^^^^^^.....^^^^^^.....^
^..*...#######################..^
^.........^^^^^^.....^^^^^^..#..^
~~~~~~~~~~^^^^^^~~~~~^^^^^^~~#~~^
^..###########################..^
^..#......^^^^^^..*..^^^^^^.....^
^..############################HH
```

- **Hook:** the colony lives under a paving slab. Enemies march along the grout lines between slabs in a long switchback.
- **Hazard:** rain. The middle grout channel (`~`) fills during storm waves, slowing everything in it, towers included.
- **Traversal:** slabs (`^`) are high ground. The player can climb them to cross the map quickly; enemies cannot.
- **Resources:** sugar crumbs (`*`) dropped from the table above, refreshed between waves.
- **Twist:** the last wave is a garden hose. A wall of water travels the whole path once and washes away anything built in the grout.
- **Favours:** builder. Lots of time, clear lanes, a teaching map.

## 2. Picnic Blanket
*Hill in the centre.*

```
..............S..............
.*............#............*.
..............#..............
.......B......#......B.......
S############HHH############S
.......B......#......B.......
..............#..............
.*............#............*.
..............S..............
```

- **Hook:** the hill is a dropped sandwich in the middle of a checkered blanket. Rival ants and beetles come from all four edges.
- **Hazard:** the human. A shadow covers a 5x5 area for three seconds, then a hand comes down and flattens towers and enemies alike.
- **Traversal:** `B` are plates and cups. The player can push one to block a lane for a wave, which sends that lane's enemies the long way round.
- **Resources:** food in each corner (`*`), far from the hill. Each trip leaves one front undefended.
- **Boss:** a wasp that ignores paths, lands on a tower, and carries it off unless shot down.
- **Favours:** fighter. Four fronts and never enough towers.

## 3. The Sandbox
*Open-field mazing.*

```
SSSSSSSSSSSSSSSSSSSSSSSSSSSSS
.............................
....*...............^^^......
.............T......^^^......
.......^^....................
.......^^...........*........
.............................
..*....................T.....
.............................
HHHHHHHHHHHHHHHHHHHHHHHHHHHHH
```

- **Hook:** no path at all. Enemies cross the sand top to bottom by the shortest route, and towers are the walls.
- **Hazard:** soft sand. Every tower sinks slowly and must be dug out by the player or it disappears, so the maze keeps opening holes.
- **Traversal:** the player can dig trenches that slow enemies and raise sand walls that cost nothing but crumble after one wave.
- **Resources:** buried toys (`*`). Digging one up gives a large one-time payout and leaves a pit enemies must walk around.
- **Twist:** `T` are a child's toys. Between waves the shovel and bucket move, flattening one region and piling up another.
- **Favours:** builder. The maze is the whole game.

## 4. Storm Drain
*Changing paths.*

```
S#########...........^^^^^^^^^
^^^^^^^^^#.....*.....^^^^^^^^^   high ledge
.........#####################
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~#   tide line 2
S###########.......*.........#
~~~~~~~~~~~#~~~~~~~~~~~~~~~~~#   tide line 1
S####......#.................#
....#......###################
....########...............#HH
```

- **Hook:** three lanes stacked at different heights inside a drain pipe during a storm.
- **Hazard:** the water rises on a timer. At tide line 1 the bottom lane and everything built there is under water and its spawn moves up. At tide line 2 only the top ledge is left.
- **Traversal:** leaf rafts. The player can ride the current across the map in seconds, but only downstream.
- **Resources:** washed-in debris (`*`) that floats to a new spot each time the water rises.
- **Twist:** towers can be built on rafts, so a few survive the flood if the player planned for it.
- **Favours:** builder who plans ahead, because the first two-thirds of the build is temporary.

## 5. The Long Walk
*Special rule: the hill moves.*

```
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
S..>>>>>>>>>>>>>>>>>>>>>>>>>..S
....>   .-----------.     >....
S...>  /  . . H . .  \    >...S
....>  \  . . . . .  /    >....
S..>>>>>'-----------'>>>>>>>..S
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
          camera scrolls ->
```

- **Hook:** the old hill is gone and the colony is migrating on the back of a tortoise. The level scrolls; the shell is the only buildable ground.
- **Rule:** about twelve build tiles, total. Every placement means removing something else.
- **Hazard:** the scenery. The tortoise walks through tall grass (ambushes from both sides), a puddle (it swims and the low tiles flood), and under a hedge (flyers only).
- **Traversal:** the player can jump off to fight or gather on the ground, then has to catch up before the tortoise leaves the screen.
- **Resources:** only what the tortoise passes. Miss it and it is gone.
- **Boss:** a bird that pecks at the shell. The tortoise pulls its head in and stops, and the level becomes a stationary siege until the bird is driven off.
- **Favours:** gatherer and fighter.

## 6. Motherboard
*Changing paths the player controls.*

```
S####[1]##########....F....
.....|...........#.........
.....|.....*.....#...[CPU].
.....####[2]######....^^^..
.........|.......#....^^^..
S####....|.......####......
....#....|...*......#......
....#############[3]#####HH
```

- **Hook:** the colony has moved into a gaming PC. Paths are circuit traces and the enemies are literal bugs.
- **Mechanic:** `[1] [2] [3]` are switches. The player walks to one and flips it, sending enemies down the alternate trace (`|`). Flip at the right moment and a wave doubles back through the kill zone.
- **Hazard:** heat. Every shot adds to a heat meter; when it is full the fan `F` spins up and blows projectiles and light enemies sideways across the board.
- **Resources:** capacitors (`*`) store charge. Draining one gives nearby towers a burst of fire rate and leaves that area unpowered for a wave.
- **Boss:** a short circuit. A spark jumps from trace to trace, ignoring path order, and disables each tower it passes.
- **Favours:** a player who runs between switches rather than fighting.

## 7. Tilt
*Fixed roads, physics enemies.*

```
^^^^^^^^^^^^^^^^^^^^^^^^^
^S#####.........#######S^
^.....#...(O)...#.......^
^..*..##.......##..*....^
^......#.(O).(O)#.......^
^......##.....##........^
^...\\..#.....#..//.....^
^....\\.#######.//......^
^.....\\..HHH..//.......^
^^^^^^^^^^^^^^^^^^^^^^^^^
```

- **Hook:** inside a pinball machine. Enemies are pill bugs that roll, pick up speed downhill, and bounce.
- **Mechanic:** `\\` and `//` are the flippers. The player stands on a button to fire one, sending anything on it back to the top of the table.
- **Hazard:** bumpers `(O)` fling enemies in random directions, which can help or can skip them past the defenses.
- **Resources:** score. Every bumper hit pays out, so letting enemies bounce is profitable and dangerous.
- **Twist:** multiball. One wave releases three bosses at once and tilts the table, shifting every path one tile sideways.
- **Favours:** fighter with good timing.

## 8. Game Night
*Fixed roads chosen by dice.*

```
S1 ############...........?..
...#..........#..............
S2 #####......#######.....*..
.......#............#........
S3 #####....?.......#####....
...#....................#....
S4 #################....###HH
S5 ####....*.......#######...
S6 ###########################
```

- **Hook:** the map is a board game left on a table. Before each wave a die the size of a house rolls across the board, and the number decides which spawn opens.
- **Mechanic:** enemies move in turns, a set number of squares per beat, so the player can count exactly when one will reach a tile.
- **Hazard:** the die itself crushes whatever it rolls over.
- **Resources:** `?` are card squares. The player draws a card: a free tower, a skipped wave, or "all enemies move again".
- **Boss:** the opposing player's king piece. It cannot be damaged. It has to be surrounded by towers on all four sides.
- **Favours:** planner. Almost a puzzle level.

## 9. Wet Paint
*The player draws the path.*

```
+---------------------------+
|S                          |
|                           |
|      (blank canvas)       |
|   *                  *    |
|                           |
|              *            |
|                         HH|
+---------------------------+
```

- **Hook:** the colony has wandered into an unfinished painting in a museum. There is no road until the player paints one.
- **Mechanic:** in the build phase the player walks a brush stroke from spawn to hill and enemies follow it exactly. A longer stroke buys more time, but a limited paint supply caps its length.
- **Hazard:** the painter returns every few waves and paints over a region. Anything there is gone and the path through it has to be redrawn.
- **Rule:** colour matters. Enemies are tinted, towers deal bonus damage to their complementary colour, and a stretch of path painted in a colour tints whoever walks on it.
- **Resources:** paint pots (`*`) refill the supply and unlock new colours.
- **Favours:** builder with an eye for routing.

## 10. Corrupted Save
*Changing paths, no ceiling.*

```
[HP ######....] [GOLD 120] [WAVE 7]   <- enemies can walk on these
-------------------------------------
>>####......#####....S......####>>      screen wraps left <-> right
.....#......#...#....#......#....
.....########...######......#....
..*..............?%&#...*...#....
.....HH.........#@!?........#....
>>###############...........##>>>
```

- **Hook:** the colony is inside a game cartridge with a dirty connector. The level is a broken version of Hold the Hill itself.
- **Mechanic:** the screen wraps. Enemies that leave the right edge re-enter on the left, and so do projectiles.
- **Hazard:** glitches (`?%&`). Tiles swap, a tower's sprite and behaviour change into another tower's, and gravity briefly applies to a top-down map.
- **Twist:** the HUD is terrain. Late waves climb onto the health bar and eat it directly, and the player has to jump up there and fight on the interface.
- **Resources:** glitched tiles pay double but may delete what is built on them.
- **Boss:** MissingNo-style block of garbage tiles that grows by absorbing the map. Kill it by letting it absorb a tower rigged to explode.
- **Favours:** fighter who enjoys chaos.

## 11. Ant Farm in Orbit
*Hill in the centre, circular.*

```
          S   .  -  ~  -  .   S
        .  '    ########    '  .
      .'    ####    *   ####    '.
     /   ###      .---.     ###   \
    S   #        /  H  \       #   S
     \   ###     '-----'    ###   /
      '.    ####   *    ####    .'
        '  .    ########    .  '
          S   '  -  ~  -  '   S
```

- **Hook:** a school science experiment on a space station. The ant farm is a rotating ring, the hill is at the hub, and "down" is outward.
- **Mechanic:** the path is a spiral inward. Projectiles curve with the spin, so a tower's shots lead or trail depending on which side of the ring it sits.
- **Hazard:** the station's day is ninety minutes. Every few waves the sun comes round and one arc of the ring is too hot to stand in; then it freezes.
- **Traversal:** the player can jump off the ring, float across the middle, and land on the far side. Miss the landing and drift for a while.
- **Resources:** water droplets (`*`) floating free, which have to be herded rather than picked up.
- **Boss:** an astronaut's finger tapping the glass, which shakes every loose thing toward one side.
- **Favours:** a mobile player. Positioning matters more than tower count.

## 12. The Under-Hill
*Open-field mazing, built from the player's own history.*

```
S . . . . . . . . . . . . . . S
. + . . . + . . . . + . . . . .
. . . . . . . . + . . . . + . .
. . + . . . . . . . . . . . . .
. . . . . . . H H . . . . . . .
. . . . + . . H H . . + . . . .
. + . . . . . . . . . . . . . .
. . . . . . + . . . . . . + . .
S . . . . . . . . . . . . . . S
```

- **Hook:** where ants go. A grey field with the hill in the middle and a grave marker (`+`) for every tower the player has lost in earlier levels, in the positions where they fell.
- **Mechanic:** grave markers can be raised as ghost towers for free. They work as they did in life but fade after one wave.
- **Enemies:** every enemy type the player has killed, returning along the routes where it died most often. A player who always held the left lane faces an army coming from the left.
- **Hazard:** light. A lantern the player carries is the only thing that makes ghosts solid enough to hit, so towers outside its glow do nothing.
- **Resources:** none on the map. The only income is memory: the level pays out for each different tower type raised.
- **Boss:** the first enemy the player ever let through to the hill, now enormous.
- **Favours:** undecided on purpose. It asks for whichever role the player has leaned on least.

---

## Coverage check

| Structure | Levels |
|---|---|
| Fixed road(s) | 1, 7, 8 |
| Hill in the centre | 2, 11, 12 |
| Open-field mazing | 3, 12 |
| Changing paths | 4, 6, 9, 10 |
| Special rule (moving hill) | 5 |

---

# Batch 2

Twelve more, same legend and same brief, again ordered from grounded to strange. None repeats a core mechanic from Batch 1.

## 13. Saturday Morning
*Fixed road with a sweeping hazard.*

```
S#####.....o.........*.......
.....#...........o...........
=====#=======================   mower stripe A
.....#########...............
..*..........#.......o.......
=============#===============   mower stripe B
.............##########......
.....o................#....*.
......................#####HH
```

- **Hook:** a lawn seen from the roots. Grass blades are a forest, and it is the day the lawn gets cut.
- **Hazard:** the mower. A distant engine starts, a stripe (`=`) is marked, and thirty seconds later everything standing on it is gone, towers and enemies alike. The stripes alternate.
- **Traversal:** `o` are sprinkler heads and divots. Anything built in one sits below the blade and survives, so they are the only permanent tower spots.
- **Resources:** dew (`*`). Plentiful in the first waves, then it evaporates as the sun climbs, so the economy shrinks over the level.
- **Twist:** after the last pass the grass is short. Sight lines open across the whole map, every tower's range doubles, and so does every enemy archer's.
- **Favours:** builder who can rebuild quickly.

## 14. Kitchen Floor, 2 a.m.
*Fixed roads in the dark.*

```
^^^^^^^^^^^ FRIDGE ^^^^^^^^^^^
S####......:::::::.........*.^
....#.....:::::::::..........^
....#....:::::::::::..R>>>>..^
....#######################..^
..*......:::::::::::......#..^
S####.....:::::::::.......#..^
....#######################..^
^^^^^^^^^^^^^^^^^^^^^^^^^HH^^^
```

- **Hook:** tile grout roads across a dark kitchen. Nothing is visible beyond a short radius around the player and each tower.
- **Hazard:** `R` is a robot vacuum on a fixed patrol. It eats enemies and towers equally. Its route can be learned, and a crumb trail laid by the player will lure it somewhere useful.
- **Mechanic:** the fridge door. When someone opens it, the cone (`:`) is lit, towers inside it see across the room, and cockroach enemies freeze until it closes.
- **Resources:** spills and crumbs (`*`) under the cabinets, where it is darkest.
- **Boss:** a house centipede that only moves while unlit.
- **Favours:** a player who scouts. The character is the main light source.

## 15. Borrowed Web
*Hill in the centre, paths the player can cut.*

```
        S           S
         \    |    /
      ----\---|---/----
     /     \  |  /     \
S---+-------\-+-/-------+---S
     |       HHH       |
S---+-------/-+-\-------+---S
     \     /  |  \     /
      ----/---|---\----
         /    |    \
        S           S
```

- **Hook:** the colony has nested at the hub of an abandoned spider web. The radial threads are the only roads in.
- **Mechanic:** towers can only hang on thread crossings. The player can cut any thread, which removes that road and drops every tower hanging from it. The ring threads are sticky and slow whoever crosses.
- **Hazard:** wind. Gusts swing the whole web, and projectiles miss until it settles.
- **Resources:** wrapped prey left in the web (`+` crossings). Unwrapping one pays well and sends a tremor down the thread.
- **Boss:** the web is not abandoned. Enough tremors and the spider comes home, walking any thread it likes and re-spinning the ones the player cut.
- **Favours:** builder making hard cuts under pressure.

## 16. Honeycomb
*Open-field mazing on hexes.*

```
S  / \ / \ / \ / \ / \ / \
  | . | . | W | . | * | . |
   \ / \ / \ / \ / \ / \ / \
    | . | W | . | . | . | . |
   / \ / \ / \ / \ / \ / \ /
  | . | . | . | Q | W | . |
   \ / \ / \ / \ / \ / \ / \
    | * | . | . | . | . | H |
     \ / \ / \ / \ / \ / \ /
```

- **Hook:** a hollow log shared with a beehive. The floor is hexagonal cells, so mazes have six directions and no straight corridors.
- **Mechanic:** `W` cells are capped with wax and act as walls. The player can cap an empty cell for a fee or chew one open.
- **Hazard:** heat. Each tower warms its neighbours, and a cluster that gets too hot melts the wax around it and floods the adjacent cells with slow, sticky honey.
- **Resources:** honey cells (`*`), which are also the best mazing material if left alone.
- **Special:** `Q` is the bee queen, neutral. Enemies that pass her anger the hive against them; towers that fire near her anger it against the player.
- **Favours:** builder. A spatial puzzle with a temperature budget.

## 17. B4
*Fixed road, side view.*

```
+---------------------------+
| S======@@@@@=========     |   shelf 4
|                     #     |
|    ========@@@@@=====     |   shelf 3
|    #                      |
|    ======@@@@@========    |   shelf 2
|                      #    |
|  * ===================    |   shelf 1
|                     #     |
| [ PUSH ]           HH     |   tray
+---------------------------+
```

- **Hook:** inside a vending machine, seen from the side. Gravity matters. Enemies cross each shelf, drop to the next, and the hill is in the collection tray.
- **Mechanic:** `@` are the spiral coils. When a customer buys something, that coil turns, carrying everything on it forward and dropping a snack down the shaft, which crushes whatever is beneath.
- **Traversal:** the player climbs between shelves by the price-tag rails and can press a coil's button from inside to turn it on purpose.
- **Resources:** the coin box (`*`), heavily guarded by the level's layout rather than by enemies: it is at the far end of the lowest shelf.
- **Twist:** someone shakes the machine. The view tilts and every loose enemy, coin and unanchored tower slides to one side.
- **Favours:** fighter. Knocking enemies off shelves is the best damage in the level.

## 18. Thin Ice
*Open-field mazing where everything slides.*

```
^^^^^^^^^^^SSSSS^^^^^^^^^^^^
^..........................^
^....o.........*...........^
^..............o.....xx....^
^.......o............xx....^
^..*.......................^
^..............o...........^
^.....xx...................^
^^^^^^^^^^^HHHHH^^^^^^^^^^^^
```

- **Hook:** a frozen puddle in winter. Anything that starts moving keeps going in a straight line until it hits something.
- **Mechanic:** towers and rocks (`o`) are stoppers. The maze is not walls to walk around but a set of bumpers that decide where sliding enemies end up, in the style of an ice-floor puzzle.
- **Hazard:** thin ice (`xx`). Too much weight on a patch cracks it, and whatever was there falls through. A crack can be bait.
- **Traversal:** the player slides too. Getting to a tower means working out the bounces.
- **Resources:** frozen seeds (`*`) that have to be chipped out, which weakens the ice around them.
- **Twist:** midday thaw. The ice turns to slush from the edges inward and normal movement returns, on a shrinking map.
- **Favours:** planner.

## 19. 33 RPM
*Hill in the centre on rotating ground.*

```
            . - ~ ~ - .
        . '   #######   ' .
      /    ###       ###    \
     |   ##   . ' ' .   ##   |
S####|###    |   H   |       |   <- needle arm, does not rotate
     |   ##   ' . . '   ##   |
      \    ###       ###    /
        ' .   #######   . '
            ' - ~ ~ - '
```

- **Hook:** the hill sits on the label of a record on a turntable. Enemies walk in along the needle arm and follow the groove inward.
- **Mechanic:** the record turns and the arm does not. Towers are built on the record, so each one sweeps past the entry point once per rotation and then spends the rest of the turn out of reach.
- **Rule:** the music is the wave. Enemies spawn on the beat, a quiet passage is a build window, and the chorus is the rush.
- **Hazard:** someone switches to 45. Everything on the record speeds up, towers and groove-walkers both, but not the player.
- **Resources:** dust in the grooves (`*` drifts round with the record). Clearing it also removes a skip that was sending enemies back a ring.
- **Boss:** a scratch. The needle jumps and drops a whole wave three rings from the hill.
- **Favours:** a player with rhythm. Timing beats placement.

## 20. Belly of the Beast
*Hill in the centre with an exit objective.*

```
            throat
             | U |
      .------'   '------.
    /   S             S   \
   |    #      *      #    |
   |    #####HHHH######    |
   |      *                |
    \ ~~~~~~~~~~~~~~~~~~~ /     acid, rising
     '-------------------'
```

- **Hook:** an anteater ate the hill. What is left of the colony is holding out in its stomach.
- **Rule:** this cannot be won by surviving. The acid (`~`) rises steadily. The player has to climb to `U` and irritate the throat until the anteater coughs everyone out, while the towers hold the hill without them.
- **Hazard:** the stomach contracts. Walls close in, the buildable area shrinks, then relaxes again.
- **Enemies:** enzymes that dissolve towers rather than attacking the hill, and other swallowed insects that can be fought or recruited.
- **Resources:** half-digested food (`*`), worth less the longer it is left.
- **Twist:** the player is absent for the hardest waves. Everything depends on what was built before leaving.
- **Favours:** builder first, then a solo traversal run.

## 21. Pop-Up Book
*Changing paths: the whole map turns over.*

```
   left page             |   right page
S#######.....[tab].....  |  ....[tab]...........
.......#...............  |  ......*.....#######S
..*....#############=====|=====######...........
.......................  |  ...........[tab]....
....[tab]..............  |  .................HH.
                        fold
```

- **Hook:** a children's pop-up book left open on the floor. Every few waves a page turns and the map is replaced by the next spread.
- **Mechanic:** towers built on paper are flattened and lost when the page turns. Towers built on a pull-tab (`[tab]`) travel with the tab and reappear on the next page, somewhere the player has not seen yet.
- **Traversal:** the player can pull tabs and turn wheels to operate the page: raise a paper drawbridge, slide a dragon across a lane, spin a wheel that swaps two paths.
- **Hazard:** the fold. Anything standing on the centre crease when the page turns is pressed flat.
- **Resources:** stickers (`*`) that can be peeled off one page and stuck down on another as terrain.
- **Boss:** the last page is torn out. The final wave is fought on the bare inside cover, with only what the tabs carried over.
- **Favours:** planner who thinks a page ahead.

## 22. Quarter Past
*Fixed roads on moving gears, with time as a resource.*

```
            S (XII)
        . ' # ' .
     .'  (G1)#     '.
    /    ####(G2)    \
S (IX)###   [H]   ###S (III)
    \    (G3)####    /
     '.     #     .'
        ' . # . '
            S (VI)
```

- **Hook:** inside a grandfather clock. The roads run across gear faces, so each gear (`G`) carries its piece of road round and the route only connects at certain moments.
- **Mechanic:** towers on a gear rotate with it. The player can jam a gear with a twig to freeze a good alignment, at the cost of stopping everything downstream of it.
- **Rule:** on the hour the clock chimes and time rewinds ten seconds for everything except the player and whatever they are carrying. Dead enemies return, spent money returns, and the player keeps their position and knowledge.
- **Hazard:** the pendulum sweeps the bottom third of the map on a slow, readable beat.
- **Resources:** seconds. The currency is time shaved off the next chime.
- **Boss:** the cuckoo, which appears on the hour, takes one tower, and is the only enemy the rewind does not restore.
- **Favours:** a player who likes replaying ten seconds until it is perfect.

## 23. Ant Mill
*A path with no end.*

```
        . - ####### - .
     .'  ###       ###  '.
    /  ##    . . .    ##   \
   |  #    .       .    #   |
S>>>>>#    .  H H  .    #   |
   |  #    .       .    #   |
    \  ##    . . .    ##   /
     '.  ###       ###  .'
        ' - ####### - '
```

- **Hook:** a real phenomenon. Army ants that lose the trail follow each other in a circle until they drop. Here the enemy army is caught in one, circling the hill.
- **Mechanic:** enemies never leave the loop on their own. They keep arriving and the ring keeps getting denser. The ring is held together by a pheromone trail that fades wherever enemies die, and when a section fades completely the loop breaks there and the whole crowd spills straight at the hill.
- **Rule:** so killing is dangerous. The player has to thin the ring evenly, or re-lay the trail by walking it, or break it on purpose at the one spot the defenses are ready for.
- **Hazard:** exhaustion. Enemies that have circled long enough collapse and become obstacles other enemies climb over, building a ramp inward.
- **Resources:** fallen enemies, harvested from inside the ring.
- **Favours:** a player managing a pressure gauge rather than a kill count.

## 24. Powers of Ten
*Every structure in turn; the map keeps zooming out.*

```
+---------------------------------------+
| S   waves 9+: the planet              |
|   +-----------------------------+     |
|   | S   waves 6-8: the yard     |     |
|   |   +-------------------+     |     |
|   |   | S  waves 1-5:     |     |     |
|   |   |    #####HH        |     |     |
|   |   +-------------------+     |     |
|   +-----------------------------+     |
+---------------------------------------+
```

- **Hook:** it starts as the smallest possible level: one short road, one hill. Then the camera pulls back.
- **Mechanic:** at each zoom the whole previous map shrinks to a single tile in a larger one, and the defense built there becomes one tower whose stats are whatever that defense achieved. The hill is in a yard, the yard is on a planet, the planet is a marble in a child's hand, the child is in a snow globe on a shelf near an anthill.
- **Enemies:** scale with the frame. Beetles, then birds, then weather, then moons.
- **Traversal:** the player can dive back into any earlier frame to repair or rebuild it at its own scale while the outer level keeps running.
- **Resources:** each frame has its own currency, and the exchange rate inward is generous while outward is terrible.
- **Twist:** the final frame is the first map again, and the enemy walking the road is the player's character from wave 1.
- **Favours:** every role in turn.

---

## Coverage check, Batch 2

| Structure | Levels |
|---|---|
| Fixed road(s) | 13, 14, 17, 22 |
| Hill in the centre | 15, 19, 20 |
| Open-field mazing | 16, 18 |
| Changing paths | 15, 21, 22 |
| Special rule | 20 (escape), 23 (endless loop), 24 (nested scales) |

---

# Batch 3: the strange ones

Twelve concepts where the odd part is the rule, not the scenery. Each one breaks an assumption a tower defense normally keeps: that there is one army, that the ground stays put, that time runs forward, that the level stays inside the game. The sketches are diagrams of the idea more than maps.

## 25. Negative Space
*Assumption broken: there is one map.*

```
....................##########
S...........######..##########   light ants (S) walk on .
######......######..........##   dark ants  (Z) walk on #
######......######..######..##
######..............######..HH
Z#########################....
```

- **Hook:** a black-and-white optical illusion. One army walks on the white, a second walks on the black, and each one's road is the other's wall.
- **Mechanic:** towers stand on either colour. The player's main tool is a brush that flips a tile, which opens a road for one army in the same stroke that closes it for the other.
- **Rule:** when the two armies touch across an edge they fight each other. Good play is arranging for them to meet.
- **Hazard:** every few waves the whole image inverts.
- **Resources:** grey tiles (`*` in play), which both armies can walk and both want.
- **Boss:** a creature that is only an outline. It walks the borders between black and white and grows with every edge the player has created.
- **Favours:** planner.

## 26. Lava Lamp
*Assumption broken: the ground stays where it is.*

```
        .-------------.
       /     (   )     \
      |   (        )  S |
      |     (    )      |
      |             ( ) |
      |   (          )  |
      |  (     HH     ) |
      |   (          )  |
       \   heat coil   /
        '-------------'
```

- **Hook:** the colony lives on a wax blob inside a lava lamp. The blobs are the only ground and they drift, merge and split.
- **Mechanic:** there is a road only while blobs are touching. Enemies wait at the edge of theirs until it bumps into the next. Towers ride whatever blob they were built on, and a blob that splits takes half the defense away with it.
- **Traversal:** the player can jump between blobs and can warm or cool one with their own body, making it rise or sink.
- **Hazard:** the coil at the bottom. A blob that sinks too far melts into it and everything on it is lost; a blob at the top cools, hardens and stops moving.
- **Resources:** bubbles trapped in the wax, reachable only when a blob is stretched thin.
- **Twist:** someone switches the lamp off. Everything slowly settles to the bottom in one heap and the last wave is fought on a single crowded island.
- **Favours:** a mobile player.

## 27. Shadow Play
*Assumption broken: the enemies are objects.*

```
 lamp     toys (real)         bedroom wall (shadows)
  L  ->     T                |S########.......
  :         T     T          |........#...*...
  :               T          |S#######........
 (player moves L and T)      |........######HH
```

- **Hook:** a child's bedroom at night. The enemies are shadows on the wall, cast by toys standing in front of a lamp. The hill is a shadow too.
- **Mechanic:** the player works in the room, not on the wall. Pull a toy closer to the lamp and its shadow grows huge and slow; push it back and the shadow is small and fast. Turn a toy and its shadow changes shape and becomes a different enemy type.
- **Rule:** towers are shadows of things the player props up, so every tower also has a real object that can be knocked over.
- **Hazard:** headlights from a passing car sweep the wall and briefly throw every shadow sideways into a different lane.
- **Boss:** the child's hand making a shadow animal. It has no toy to move.
- **Twist:** the lamp can be switched off. No shadows, no enemies, no towers, no hill, for as long as the player dares.
- **Favours:** a player who would rather rearrange the problem than shoot it.

## 28. The Sleeper
*Assumption broken: the map is a place.*

```
           z  z  z      (o)  dream bubble = spawn
     ______________________________
    /  S###          chest          \____
   | head  #####^^^^ rises ^^^^######     feet
    \__ear________###########________HH__/
        ^ whisper here
```

- **Hook:** the map is a person asleep in a hammock. The road runs from the head to the feet, where the hill is.
- **Mechanic:** enemies are whatever the sleeper is dreaming about, drifting out of a bubble over their head. The player can climb to the ear and whisper a word, which changes the dream and therefore the next wave. "Rain" brings slow heavy things. "Falling" brings flyers.
- **Hazard:** breathing lifts and drops the middle of the map on a slow rhythm, changing tower range and road length. An itch brings a hand down. A roll-over turns the whole map upside down and the road now runs along the back.
- **Rule:** noise is a meter. Too much firing and the sleeper wakes up, stands, and the level ends in a loss for everybody.
- **Resources:** pocket lint, crumbs, a watch whose ticking masks noise.
- **Boss:** a nightmare, which the whisper cannot change, only wake.
- **Favours:** a quiet player. Loud towers are a liability.

## 29. Block Party
*Assumption broken: the level exists before the player arrives.*

```
|         [##]           |   <- next piece
|         [#.]           |
|                        |
| S###..                 |
| ...#..      ##.        |
| ...####.....#..        |
| ......#######..####HH  |
+------------------------+
```

- **Hook:** the map falls from the top of the screen in pieces. Each piece is a chunk of terrain with some road on it, and the player decides where it lands and which way round.
- **Mechanic:** the enemy road is whatever the pieces add up to. A piece that fails to join the road leaves a dead end; one that joins it badly makes a shortcut.
- **Rule:** a completed row vanishes with everything on it. That is the level's only way to clear a mistake and its quickest way to lose a good defense.
- **Hazard:** the pieces speed up, and they fall during waves as well as between them.
- **Resources:** some pieces carry a resource tile, usually in an awkward shape.
- **Twist:** if the stack reaches the top the board does not end. It flips, and the enemies now walk the road from the other end.
- **Favours:** builder under time pressure.

## 30. The Line
*Assumption broken: the map has two dimensions.*

```
S -------o------T-----o-----[H]-----T-------o-------- S
```

- **Hook:** a one-dimensional level. Everything is on a single line: the hill in the middle, a spawn at each end, and nothing above or below.
- **Mechanic:** nothing can pass anything else. A tower is a wall for enemies and for the player. Enemies queue behind their slowest member. The player cannot walk around a tower, only swap places with it.
- **Rule:** order is the whole strategy. Which tower is in front, which enemy is in front, and which side the player is stuck on when the other side breaks.
- **Hazard:** the line is a thread and sometimes it is pulled. Everything shifts three places toward one end.
- **Resources:** knots (`o`). Untying one gives resources and removes the only thing that was slowing the queue there.
- **Twist:** late in the level a second line crosses the first at the hill, and for the first time something can come from the side.
- **Favours:** fighter. The character is the only piece that can change the order.

## 31. Möbius
*Assumption broken: the map has another side that is somewhere else.*

```
 top side:     S###################>>>  twist
                                         \
 underside:    HH<<<(#)(#)(#)(#)(#)(#)<<<
               (#) = seen through the paper, as a silhouette
```

- **Hook:** the level is a paper strip with a half twist. Enemies walk the whole top side, go round the twist, and come back along the underside to the hill, which is directly beneath where they started.
- **Mechanic:** towers are built on one side but fire through the paper at reduced strength. Enemies on the far side are only visible as silhouettes. The best spots are the ones that cover a stretch of road twice.
- **Traversal:** the player can punch a hole and climb through to the other side. Enemies can use the hole too.
- **Hazard:** scissors. Halfway through, the strip is cut lengthwise down the middle, and as with a real Möbius strip it does not fall into two: it becomes one loop twice as long, with every tower now on a different stretch of road than before.
- **Resources:** paper clips and staples, which pin the two sides together and make a shortcut for whoever finds it first.
- **Favours:** planner with good spatial sense.

## 32. HILL IS WIN
*Assumption broken: the rules are fixed.*

```
S#####################HH
........................
..[ANT][IS][SLOW].......
........................
..[TOWER][IS][WALL].....
..........[HILL]...[WIN]
..[ROAD][IS][LAVA]...*..
```

- **Hook:** the rules of the level are written on the map as word blocks, and they are true only while the words stay lined up.
- **Mechanic:** the player pushes words. Break `TOWER IS WALL` and enemies walk through towers. Build `ROAD IS LAVA` and the road hurts everything on it, the player included. Build `ANT IS TOWER` and find out.
- **Rule:** enemies can push words too, and some waves are sent to do exactly that. A wave that reaches `[HILL]` and shoves it away from `[IS][WIN]` has not damaged the hill at all, and has won.
- **Hazard:** a full stop block that wanders the map and ends whatever sentence it touches.
- **Resources:** spare words lying around: `FAST`, `GOLD`, `NOT`.
- **Boss:** a creature called `IS`. Killing it disables every rule on the map at once.
- **Favours:** a puzzle player. Combat is the fallback.

## 33. Family Tree
*Assumption broken: waves are separate events.*

```
                          .--- armoured ----- armoured flyer
               .-- shell -+
S (ancestor) --+          '--- spiked
               |
               '-- wing --+--- swarm -------- swarm splitter
                          '--- glider                         -> H
   wave 1         wave 3         wave 6           wave 9
```

- **Hook:** the map is the enemy's evolutionary tree, drawn left to right, and the roads are its branches. Every wave is the next generation.
- **Mechanic:** enemies inherit from whichever ancestors survived longest. Fire kills most of wave 2, so wave 3 is the fire-resistant ones. A lineage that is wiped out entirely loses every descendant, and whole branches of the map go dark.
- **Rule:** so the player is breeding the enemy. Lean on one tower type and it produces its own counter. Let a weak lineage through on purpose and the future is full of its weak grandchildren.
- **Traversal:** the player can walk back along the tree to an earlier generation and fight there, changing what follows.
- **Resources:** fossils on dead branches, which unlock a tower built from that lineage's traits.
- **Boss:** whatever the player's choices made. It is different every run and the tree shows how it happened.
- **Favours:** a player who thinks three waves ahead.

## 34. Turncoat
*Assumption broken: the player is on one side.*

```
   HILL A                                HILL B
HH#############.......|.......#############HH
..#...........########|########...........#..
..#############.......|.......#############..
          <==  sides swap every wave  ==>
```

- **Hook:** two hills and one war. After every wave the player changes sides.
- **Mechanic:** on odd waves the player defends hill A with the towers on the left. On even waves those towers belong to the enemy, and the player leads the attack against them from hill B, choosing the units and walking in with them.
- **Rule:** the score is the lower of the two hills' health. A defense too strong to beat will beat its own builder next wave; one too weak falls this wave. The goal is the best defense the player can still personally break.
- **Hazard:** the swap takes five seconds and happens mid-fight if a wave runs long.
- **Resources:** shared. Money spent on towers is money the attack will not have.
- **Twist:** in the final wave the line down the middle is removed and both of the player's own builds fight each other with no one in charge.
- **Favours:** every role, alternately.

## 35. Aftermath
*Assumption broken: time runs forward.*

```
S####x#####..........
.....#....x..........      x  a body, already lying here
.....#####x##x###....      ?  a scorch mark with no tower
....?.........?.#....
................x#(HH)     hill: three bites taken
```

- **Hook:** the level opens on the end of a battle. Bodies along the road, scorch marks on the ground, a damaged hill, and no towers anywhere. The fight has already happened and the player has to supply the cause.
- **Mechanic:** the player places towers, then time runs forward from the start. The level is passed only if the battle ends exactly as found: each enemy dying on its mark, the hill taking exactly three bites, no more and no fewer.
- **Rule:** killing too well is a failure. An enemy that dies early is not where its body is supposed to be.
- **Traversal:** the player's own footprints are on the map too, and have to be walked.
- **Hazard:** later versions show the ending with details missing, or with one body that could not have been killed by any tower in the game.
- **Resources:** a fixed kit. This is a deduction level.
- **Favours:** puzzle player.

## 36. Desktop
*Assumption broken: the level is inside the game.*

```
+----------------------------------------------------+
| [Recycle]    +-- Notes.txt -------[x]+             |
|     S########|#######################|####         |
|              +-----------------------+   #         |
|   +-- Photos ------[x]+       +-- HoldTheHill.exe --[x]+
|   |       *           |#######|          HH            |
|   +-------------------+       +------------------------+
| [Start] ________________________________   2:14 AM  |
+----------------------------------------------------+
```

- **Hook:** the game minimises itself. The level is a computer desktop, the hill is the game's own window, and enemies crawl out of the recycle bin and across the other windows to reach it.
- **Mechanic:** windows are the terrain. The player drags them to lengthen the road, stacks them to make walls, and resizes them to make a bridge too narrow for large enemies. Closing a window deletes everything standing on it.
- **Hazard:** the desktop is in use. Notifications slide in and block a lane, a screensaver starts if the player idles, and an update prompt offers to restart now or in ten minutes.
- **Rule:** enemies that reach the game window do not damage the hill. They grab the window's edge and shrink it. The level is lost when the window is too small to play in.
- **Resources:** files. Opening a folder spills icons that can be dragged into place as towers: a calculator that does maths damage, a clock that slows, a paint program that draws walls.
- **Boss:** the cursor, which is not the player's.
- **Favours:** builder, at speed.

---

# Batch 4: stranger still

Twelve more rule-breakers, numbered 37–48. Same approach as Batch 3: each one removes something a tower defense normally relies on.

## 37. Echo
*Assumption broken: there is one of you.*

```
S############.......
....a1......#.......     a1 a2 a3  recordings of you from waves 1-3
....a2..a3..#.......     P         you, now
.......P....#####HH.
```

- **Hook:** every wave, the game records what the player's character did. In the next wave that recording plays back beside them, and the one after that adds another.
- **Mechanic:** by wave 8 there are seven earlier selves on the field, each repeating one wave's worth of walking, fighting and building. They are real: their shots hit, their repairs count, and they block the way.
- **Rule:** a recording replays its actions whether or not they still make sense. An echo that repaired a tower which no longer exists repairs empty air. Good play is performing each wave so that it stays useful as a loop.
- **Hazard:** enemies learn where the echoes stand and start arriving where they are not.
- **Resources:** an echo can be erased to get back the money it spent.
- **Boss:** an enemy that also echoes, so each wave it brings one more copy of itself.
- **Favours:** fighter who can choreograph.

## 38. Trail
*Assumption broken: the road was designed.*

```
S . : . . . . . . . . . . .
. : : : . . . * . . . . . .
. . . : : : : . . . . . . .       :  faint trail
. . . . . # # # # : . . . .       #  strong trail
. * . . . . . . # # # # . .
. . . . . . . . . . . # H H
```

- **Hook:** an empty field and no road. The first enemies wander at random, leaving scent. Later enemies follow the strongest scent. The road is whatever they collectively find.
- **Mechanic:** scent fades over time and strengthens with traffic, so the route shortens and straightens as the level goes on, which is how real ant trails work.
- **Rule:** the player lays scent too, just by walking. A false trail laid into a kill zone works until enough enemies die on it, because death leaves a warning scent that pushes the road somewhere else.
- **Hazard:** rain washes every trail off the map and the search starts again from nothing.
- **Resources:** food (`*`) pulls trails toward it. Moving food moves the road.
- **Boss:** a scout that leaves triple scent and has to be caught before it gets home.
- **Favours:** a player who walks a lot. The character is the level editor.

## 39. Everything Must Go
*Assumption broken: you get stronger.*

```
S###TTT###TTT###TTT####
...T###T.T###T.T###T..#      wave 1:  24 towers, fully upgraded
...TTT...TTT...TTT....#      wave 12: the 2 you chose to keep
.....................HH
     [ the repo truck arrives after every wave ]
```

- **Hook:** the level begins with the best defense in the game, already built, fully upgraded, paid for with money the colony did not have.
- **Mechanic:** after every wave the debt collectors take two towers. The player chooses which. The waves do not get easier.
- **Rule:** nothing can be bought. Things can only be sold, and selling early gets a better price than waiting to have them seized.
- **Hazard:** interest. Every enemy that reaches the hill adds to the debt, which brings the truck round sooner.
- **Resources:** the player can hide one tower per wave by standing in front of it when the truck comes.
- **Twist:** the final wave is fought with whatever is left, plus one item bought back with everything saved.
- **Favours:** fighter, increasingly.

## 40. Hold Still
*Assumption broken: time passes on its own.*

```
S##e##e####e##........
..........#...........      everything is frozen
......P...#..->o......      a shot hangs in the air
..........#######HH...
```

- **Hook:** time only moves when the player's character moves. Stand still and the whole battle stops mid-step.
- **Mechanic:** building, aiming and planning are free while standing still. But towers only fire while time runs, and time only runs while the player walks, so every second of defense has to be paid for in footsteps.
- **Rule:** the map is small and the walking has to go somewhere. Pacing in a safe corner works until the enemies learn to path toward wherever the player paces.
- **Hazard:** conveyor patches and ice, which move the character, and so move time, without permission.
- **Resources:** only collectable by walking to them, which costs time in the literal sense.
- **Boss:** something that moves only while the player is standing still.
- **Favours:** a careful player. It is a turn-based level disguised as a real-time one.

## 41. Clumsy
*Assumption broken: the player is small and safe to have around.*

```
 .___________.
 |           |       S###########
 |   BOOT    |       ....t..t...#
 |   (you)   |       ..t.....t..#
 '-----------'       ...........hh    <- the hill, the size of a coin
   footprint
```

- **Hook:** something has gone wrong with scale. The player's character is the size of a person and the colony is the size it really is.
- **Mechanic:** the character is by far the most powerful thing on the map and the most dangerous. One step crushes a whole wave. It also crushes any towers under it, and the hill is the size of a coin.
- **Rule:** building is done with fingertips, slowly, and the towers are too small to see properly without crouching, which narrows the view to a small circle.
- **Hazard:** the player's own shadow panics the colony's workers, who stop gathering while it is over them.
- **Resources:** things in the giant's pockets, dropped onto the map as terrain: a coin as a wall, a key as a bridge.
- **Boss:** a second giant, who is not being careful.
- **Favours:** nobody comfortably. It is a level about restraint.

## 42. Don't Blink
*Assumption broken: things carry on when you are not looking.*

```
. . . . . . . . . . . . . . . . . . . .
. S#####+-------------+ . . . . . . . .
. . . . |##           | . . . . . . . .
. . . . | ####   T    |########## . . .
. . . . +-------------+ . . . . #####HH
           the camera (what you can see)
```

- **Hook:** the map is several screens wide and the camera shows one. Towers only fire while on screen. Enemies only move while off screen.
- **Mechanic:** looking at an enemy freezes it and lets towers shoot it. Looking away lets it walk. The player is choosing, every second, which part of the war is allowed to happen.
- **Rule:** the camera follows the character, so holding one front means abandoning the others to move freely.
- **Hazard:** mirrors and windows on the map count as looking. So does a screenshot.
- **Resources:** lanterns that keep one small area "watched" without the camera, for a while.
- **Boss:** an enemy that can only be damaged while off screen, by towers that only fire while on it. The answer is a mirror.
- **Favours:** a mobile player with a good memory for where everything was.

## 43. Union
*Assumption broken: towers do what they are told.*

```
S###########.......
....T!.....#.......      T!  on strike
....T?..T..#..[picket]   T?  wants to be moved
...........#####HH..     T   working, grudgingly
```

- **Hook:** the towers are staffed by ants, and the ants have had enough.
- **Mechanic:** every tower has morale. It drops when a tower is overworked, left unrepaired, placed next to one it dislikes, or watches a neighbour get sold. Low morale means slow fire, then a go-slow, then a strike.
- **Rule:** the player negotiates between waves. Demands are specific: a shorter range to cover, a shade tile, a day off every third wave, the removal of a particular rival tower.
- **Hazard:** solidarity. One strike spreads along any line of adjacent towers.
- **Resources:** money buys overtime, which works brilliantly once and is remembered.
- **Boss:** management. A wave arrives offering the towers a better deal to switch sides, and the ones the player treated worst accept.
- **Favours:** a player who repairs and visits. The character is the shop steward.

## 44. The Long Thing
*Assumption broken: the road and the enemy are different things.*

```
S==(  )==(  )==(  )==.
                     (  )
  .==(  )==(  )==(  )=='
 (  )
  '==(  )==(  )==(head)>  HH
```

- **Hook:** there is one enemy in this level. It is a millipede long enough to reach from the spawn to the hill, and its back is the road every other enemy walks on.
- **Mechanic:** it moves. As it shifts and coils, the road shifts with it, sliding past towers that were in range a moment ago.
- **Rule:** each segment can be killed. A dead segment is a gap in the road that the small enemies cannot cross, but the millipede closes up around it and gets shorter, so the route to the hill gets shorter too.
- **Traversal:** the player can ride it, and fight along its back.
- **Resources:** each segment carries something different, visible from above.
- **Twist:** the head never attacks. It is only trying to get past. Kill enough segments and it turns around and leaves, taking the road and everyone on it away.
- **Favours:** fighter.

## 45. Feeding Time
*Assumption broken: enemies reaching the hill is bad.*

```
S####################
....................#
..[ hunger: 40% ]...#
...................\#/
                   (HH)    <- a pit with teeth
```

- **Hook:** this hill is an antlion's pit, and the colony has an arrangement with the antlion. Enemies are food. The job is to deliver them.
- **Mechanic:** the hill has a hunger meter that drains constantly. An enemy that reaches it alive and weakened is a meal. An enemy killed on the road is wasted. Starve the antlion and it eats the colony.
- **Rule:** it is fussy. It cannot swallow anything at full health, chokes on armour unless it has been cracked first, and is poisoned by the green ones. The defense is a kitchen: tenderise, shell, sort, and reject.
- **Hazard:** overfeeding. A full antlion sleeps, and a sleeping pit is just a hole that enemies walk through to the nest behind it.
- **Resources:** leftovers the antlion spits out.
- **Boss:** something too big to eat that has to be delivered in pieces, in the right order.
- **Favours:** builder tuning damage to an exact number.

## 46. Unlabelled
*Assumption broken: you know what your towers do.*

```
S#########.......    shop:   [ @ ] 30    [ % ] 30    [ & ] 30
.........#.......             ???         ???         ???
....@....#...%...
.........####HH..
```

- **Hook:** the shop sells towers as unmarked symbols. No names, no stats, no descriptions, and the mapping is different every run.
- **Mechanic:** the only way to learn what `@` does is to build one and watch. Some are ordinary. Some only work at night, or facing east, or next to water, or never, until the player works out what they were for.
- **Rule:** nothing in the interface helps. Damage numbers are in an unknown numeral system. The player keeps their own notes, and an in-game notebook lets them draw their own labels onto the symbols.
- **Hazard:** two of the symbols are not towers.
- **Resources:** enemy corpses can be examined to see what killed them, which is the main source of evidence.
- **Twist:** the final wave unlocks a symbol that combines any two, and by then the player should be able to predict what the result does.
- **Favours:** an experimenter.

## 47. Life
*Assumption broken: anything on the map is making decisions.*

```
. . . . . . . . . . . . . .
. . o . . . . . . # # . . .      o  glider (enemy): moves by the rules
. . . o . . . . . # # . . .      #  block (tower): stable
. o o o . . . . . . . . . .
. . . . . . . . . . . H H .
```

- **Hook:** the map is a cellular automaton, in the style of Conway's Game of Life. Every cell lives or dies by counting its neighbours. Nothing has a path, a target or a brain.
- **Mechanic:** enemies are gliders and spaceships, patterns that travel because of the rules. Towers are stable patterns the player places cell by cell. A "shot" is a pattern built to launch a glider back the other way.
- **Rule:** collisions are the combat. A glider hitting a block can vanish, or destroy the block, or turn into something much worse, depending on the exact angle and timing.
- **Hazard:** stray cells. One misplaced cell next to a defense can make it boil over and consume itself.
- **Resources:** the player gets a budget of cells per generation.
- **Boss:** a glider gun, which never stops, and has to be jammed with a pattern placed in its mouth.
- **Favours:** puzzle player. No reflexes involved.

## 48. Erosion
*Assumption broken: there are enemies.*

```
  , , , , , , , , , , , , ,     rain
 wind ->
           ____HH____
       ___/          \___       no S anywhere on the map
  ~~~~/   gutter >>>>    \~~~~
```

- **Hook:** nobody is coming. The level has no spawn point and no waves. The hill is simply outdoors, and it is autumn.
- **Mechanic:** rain cuts channels down the slope and each channel carries a little of the hill away. Wind strips the dry side. Frost splits what the water soaked. The structures are gutters, windbreaks, umbrellas of leaf, drains and retaining walls.
- **Rule:** water is routed, never stopped. A wall that blocks a stream makes a pond, the pond finds the weakest point, and it leaves all at once.
- **Hazard:** the seasons advance through the level, and each one undoes the previous one's solution. Snow is harmless until it melts.
- **Resources:** whatever the weather brings down: twigs after wind, clay after rain.
- **Twist:** in spring a single seed that landed on the hill in the first minute has become a plant, and its roots are either the thing holding the hill together or the thing splitting it, depending on where the player let it grow.
- **Favours:** builder. A calm level, and a long one.

---

# Batch 5: further out

Twelve more rule-breakers, numbered 49–60. Several of these go after the title itself: what "the hill" is, and whether it can or should be held.

## 49. Chambers
*Assumption broken: the hill is one thing with one health bar.*

```
 surface  S===========================S
             \                    /
           [guard]            [guard]
              |    \        /    |
          [larder]--[nursery]--[fungus]
              |         |         |
           [store]---[QUEEN]---[midden]
```

- **Hook:** the camera goes underground. The level is a side-on cutaway of the hill's interior, and the enemy is already through the door.
- **Mechanic:** every chamber is its own objective with its own effect. Lose the larder and income stops. Lose the nursery and no new workers arrive. Lose the fungus garden and repairs slow. Only the queen ends the level.
- **Rule:** the player cannot hold everything and is not meant to. Tunnels can be collapsed to seal a chamber off for good, saving what is behind it and abandoning whoever is in front.
- **Hazard:** enemies dig. A sealed tunnel buys time, not safety, and new tunnels arrive where the soil is softest.
- **Resources:** each chamber's contents can be carried deeper, one load at a time, before it falls.
- **Boss:** a rival queen, who is not there to kill anything. She is moving in.
- **Favours:** a player who can decide what to lose.

## 50. Garden
*Assumption broken: a tower works when you build it.*

```
wave 1      wave 4       wave 8        wave 12
  .           i            Y            (Y)
 seed       sprout       sapling        tree, in fruit

S#############################
....i.....Y.......(Y)...,...#     , planted this wave: useless until wave 9
............................HH
```

- **Hook:** towers are plants. A seed put in the ground now does nothing for four waves, something for the next four, and reaches full strength long after the moment it was needed.
- **Mechanic:** the player is always building for a wave they have not seen. What is strong now was planted ages ago, and what is planted now is a guess.
- **Rule:** plants interact. Tall ones shade short ones, roots compete, neighbours cross-pollinate into hybrids, and an old tree drops seeds the player did not choose.
- **Hazard:** enemies eat seedlings first. A dry spell stops growth. Autumn comes whether the garden is ready or not.
- **Resources:** water, carried by hand, and compost made from enemies.
- **Twist:** the level is long enough for plants to die of old age. The last waves are fought with the second generation.
- **Favours:** builder who tends rather than fights.

## 51. Neighbours
*Assumption broken: you are the only one defending.*

```
S#########[ HILL A ]#########[ YOUR HILL ]#########[ HILL C ]
            upstream              you              downstream
           (leaks to you)                       (you leak to them)
```

- **Hook:** three colonies live along the same road. Enemies that get past the first hill carry on to the second, and past the second to the third. The player is in the middle.
- **Mechanic:** every enemy the player fails to stop becomes hill C's problem, and hill C keeps count. Every enemy that reaches the player is one hill A let through, possibly on purpose.
- **Rule:** the neighbours are run by the game and they have tempers. They trade, lend towers, send help, send bills, and if pushed far enough, redirect their own leaks.
- **Hazard:** a neighbour that falls stops absorbing anything. Lose hill A and the full wave arrives unfiltered.
- **Resources:** favours. The only currency that buys a neighbour's help is having helped.
- **Twist:** the best-scoring strategy is the one where all three survive, and the easiest strategy is the one where the player quietly lets hill C take the worst of it.
- **Favours:** any role. The decisions are political.

## 52. Procession
*Assumption broken: whoever is on the road is attacking.*

```
S o o o o o o o o o o o o o o o o o o o o o >> exit
.............................................
.....................HH......................
   (they are walking past, not toward)
```

- **Hook:** a long column of ants from another colony comes down the road carrying their dead. It is a funeral. The road happens to pass the hill.
- **Mechanic:** they are not hostile and will stay that way if nothing hits them. Every tower's targeting has to be set by hand, because anything left on automatic opens fire.
- **Rule:** the real threat is what follows a funeral: scavengers, coming from the sides for the bodies. Towers must hit the scavengers and miss the mourners walking between them.
- **Hazard:** one stray shot and that section of the column turns and fights. They were soldiers.
- **Resources:** the mourners leave offerings at the roadside for a colony that lets them pass.
- **Boss:** none. The last figure in the column is their queen, walking alone, and the level is judged on whether she reached the exit.
- **Favours:** a careful player with precise towers.

## 53. Carry
*Assumption broken: the hill is a place.*

```
S###########..........
...........#....Q.....      Q   the queen, on your back
...........#...(P)....      (P) you, slow, hands full
...........########...
```

- **Hook:** there is no hill on this map. The colony is between homes, and the hill is the queen, who the player is carrying. Holding the hill is meant literally.
- **Mechanic:** while carrying her the player is slow and cannot build, fight or gather. To do anything, she has to be set down, and wherever she is set down is where the enemy goes.
- **Rule:** the road redraws itself toward her every time she moves. A defense is only useful while she is near it, so the player is always choosing between a strong position and a new one.
- **Traversal:** she can be passed to workers, hidden under a leaf for a few seconds, or thrown across a gap, which she does not appreciate.
- **Hazard:** she gets hungry, and says so loudly.
- **Resources:** found along the way; there is no going back for them.
- **Boss:** something fast that ignores towers and follows only her.
- **Favours:** every role, one at a time, never two at once.

## 54. Tipping Point
*Assumption broken: the map is level.*

```
        S###########....T..T...
   \    ...........#.........        /
    \   ..T........########HH       /
     \_____________________________/
                   /\
                  /  \     the whole map balances here
```

- **Hook:** the map is a plate balanced on a pin. Everything on it has weight.
- **Mechanic:** build too much on one side and the plate tilts that way. Enemies walk faster downhill and slower up. Loose resources roll. Projectiles fall short or long. Tilt far enough and unanchored towers slide.
- **Rule:** the enemies weigh something too, so a big wave arriving on the left tips the map toward itself, speeding its own advance. The counter is ballast.
- **Traversal:** the player's own position is a weight, and standing on the far edge is a legitimate defensive move.
- **Hazard:** dead enemies stay where they fall until cleared.
- **Resources:** heavy, all of them. Carrying gold across the map changes the fight on the other side.
- **Boss:** a single enormous beetle whose only attack is walking to one edge and waiting.
- **Favours:** a mobile player who watches the whole board.

## 55. Let Go
*Assumption broken: the hill can be held.*

```
S###########################
..........................##
.... evacuation tunnel <<<<[HH]     flood: 9 waves away
....<<<<<<<<<<<<<<<<<<<<<<<<..      colony out: 212 / 4,000
exit
```

- **Hook:** the level says so at the start: this hill will fall. Water is coming and nothing stops it. The score is how much of the colony gets out first.
- **Mechanic:** the defense exists to buy time for a line of workers carrying eggs, food and the queen down an escape road on the far side.
- **Rule:** the two jobs compete for everything. Workers defending are not carrying. Towers by the gate are not covering the convoy. Every wave the player has to choose how much of the hill to stop defending.
- **Hazard:** enemies find the escape road halfway through, and it has no towers.
- **Resources:** whatever is left in the hill when it goes is lost, so spending everything is correct for once.
- **Twist:** the player's character leaves last and the level does not end until they do. Staying longer saves more of the colony and costs the walk out.
- **Favours:** a player willing to lose on purpose and do it well.

## 56. Their Eyes
*Assumption broken: you see the battlefield from above.*

```
+--------------------------------+
|        .      T                |    the view from the front
|     .     ####T###             |    enemy of the column
|  .    ####        ####    hh   |
| ######                ####     |    when it dies, the view
+--------------------------------+    jumps to the next one
```

- **Hook:** the camera is fixed to the lead enemy. The player sees the level the way the attackers do: low, forward-facing, walking toward a hill on the horizon.
- **Mechanic:** towers are built and commanded from this view. The player sees only what the enemy is looking at, and knows a tower is working when it starts hurting.
- **Rule:** killing the camera's host throws the view back to the next enemy in line, further from the hill, so the best defense makes the game harder to see.
- **Traversal:** the player's character appears in the frame as a small figure in the distance, controlled in reverse.
- **Hazard:** some enemies look at the ground. Some are short-sighted. One has compound eyes and shows the map as two hundred small tiles.
- **Boss:** an enemy with no eyes. The screen is black for its whole approach and the player works from sound and memory.
- **Favours:** a player who has learned the map by heart.

## 57. Stairs
*Assumption broken: the geometry is honest.*

```
        ____
   ____|    |____
  |  S  ->  ->   |____
  |  ^          ->    |      four flights, each going down,
  |__  <-   <-    v   |      arriving back where it started
     |____  HH  <- ___|
          |______|
```

- **Hook:** the map is an impossible drawing, the kind where a staircase descends on all four sides and comes back to its own top.
- **Mechanic:** distance depends on the angle of view. The player can rotate the camera, and two platforms that line up on screen are connected for as long as they stay lined up. A tower's range is measured on the picture, not in the world.
- **Rule:** enemies obey the same rule. Rotating the view to open a shortcut for a shot opens it as a road too.
- **Hazard:** gravity is local. Enemies on the ceiling of one stair are on the floor of another, and things dropped from one fall sideways through the next.
- **Resources:** visible from everywhere and reachable from one angle only.
- **Boss:** a figure that walks the endless staircase and gets a little larger on every lap.
- **Favours:** puzzle player.

## 58. Helpful Tips
*Assumption broken: the game is telling you the truth.*

```
S############.......      +----------------------------------+
............#.......      | TIP: Archer towers are strong    |
....T.......####HH..      | against flying enemies!          |
                          +----------------------------------+
                            (they are not)
```

- **Hook:** it presents itself as a tutorial. A cheerful box explains each thing in turn. About one tip in three is false, and it gets worse.
- **Mechanic:** the tutorial gates progress on following its instructions, and following the bad ones loses the level. The player has to find ways to satisfy the letter of a tip while doing something else.
- **Rule:** the tip boxes are solid. They pop up over the map and block the view, the towers and eventually the road, and enemies can stand on them.
- **Hazard:** the tutorial notices disobedience. It becomes hurt, then strict, then starts "helping" by placing towers itself.
- **Resources:** dismissing a tip before reading it pays a small reward, which trains the player to skip the few that are true.
- **Boss:** the final tip, which is true, important, and delivered at the worst possible moment in very small text.
- **Favours:** a sceptical player.

## 59. Sheet1
*Assumption broken: the map is a physical place.*

```
     A        B          C          D         E
1  [ S ]   [ bug ]   [ bug ]    [      ]  [      ]
2  [    ]  [      ]  =SUM(B1:C1)[      ]  [      ]
3  [    ]  [      ]  [      ]   =IF(D2>5) [      ]
4  [    ]  [      ]  [      ]   [      ]  [ HILL ]
5  status: 2 errors     wave 4 of 12        #REF!
```

- **Hook:** the level is a spreadsheet. The hill is a cell, the enemies are errors spreading from cell to cell, and the towers are formulas.
- **Mechanic:** a formula tower acts on the cells it references. `=SUM` gathers the enemies in a range into one cell. `=IF` splits a lane by a condition. `=ROUND` trims health. Towers can reference other towers, so a defense is a chain of dependencies.
- **Rule:** enemies corrupt what they touch. An error entering a formula's range breaks it, and everything that depended on it shows `#REF!` and stops.
- **Hazard:** circular references. Two towers that feed each other either loop harmlessly or freeze the sheet.
- **Resources:** inserting a row or column, which pushes the whole map over by one and breaks every reference that was not anchored.
- **Boss:** a merged cell that occupies a 4x4 block and cannot be referenced in part.
- **Favours:** puzzle player, and anyone who has built a spreadsheet they were afraid to touch.

## 60. Overnight
*Assumption broken: a level happens in one sitting.*

```
 real clock:   8 pm      midnight      4 am       8 am
               |-----------|-----------|-----------|
 waves:        1  2        3     4        5  6      7
               ^ you set up            ^ you are asleep
```

- **Hook:** this level lasts twenty-four hours of real time and keeps running while the game is closed. It uses the actual clock. Night on the map is night outside.
- **Mechanic:** waves arrive hours apart. The player builds, leaves, and comes back to find out what happened, with a log of the night written by the colony.
- **Rule:** nothing can be done in a hurry. Towers take real minutes to build, and a single visit allows only a handful of actions before the workers need rest.
- **Hazard:** real weather, if the player allows it, and real days of the week. Sunday is quiet. Monday is not.
- **Resources:** accumulate slowly while away, up to a limit, so never checking in wastes them and checking constantly gains nothing.
- **Twist:** the hardest wave is scheduled for a time the player has to choose in advance, and has to keep.
- **Favours:** a patient player. It is the only level that asks to be thought about while doing something else.

---

# Batch 6: further still

Twelve more rule-breakers, numbered 61–72. This batch leans on who the player is and what they are allowed to do: how many towers, how much authority, how long a life.

## 61. Only Child
*Assumption broken: you have more than one tower.*

```
S##############.........
..............#.........
......(T)<----#---P.....     one tower, carried from place to place
..............#######HH.
```

- **Hook:** the colony owns exactly one tower. There is no shop. It is the only one there will ever be.
- **Mechanic:** the player picks it up and carries it. It does not fire while carried. Every wave is a question of where it should be standing, and when to risk moving it.
- **Rule:** it grows. Every kill adds to it permanently, and it changes with what it kills: fast enemies make it quicker, armoured ones make it heavier to carry. By the end it is a monster that takes most of a wave to relocate.
- **Hazard:** the map has three lanes.
- **Resources:** spent on the ground, not the tower: ramps, shade, a second pedestal so the move is shorter.
- **Boss:** an enemy that wants the tower, not the hill, and walks off with it if it gets there.
- **Favours:** fighter. The player covers whatever the tower is not facing.

## 62. Zero Sum
*Assumption broken: building materials come from nowhere.*

```
before:  S##########......      after:  S####__####......
         ................               ....T...........
         ..........####HH               ..........####HH
                                   (the tower is made of the road it stood beside)
```

- **Hook:** nothing in this level is created or destroyed. There is a fixed amount of stuff on the map and it only moves.
- **Mechanic:** a tower is built out of tiles dug from somewhere else, leaving a pit. Dig up road and the road has a hole. Dig up ground and there is less ground to build on. A dead enemy becomes a tile of whatever it was made of, exactly where it fell.
- **Rule:** so the map is slowly rearranged by the fighting. Kill everything at one chokepoint and it fills with bodies, becomes a wall, and the road goes round it.
- **Hazard:** projectiles are matter too. Every shot fired is a pebble that has to be fetched back.
- **Resources:** the level has no income, only an inventory.
- **Twist:** the hill is made of the same tiles. In an emergency it can be spent.
- **Favours:** builder who counts.

## 63. Sequencer
*Assumption broken: towers act whenever they are ready.*

```
  beat:   1   2   3   4   5   6   7   8
        +---+---+---+---+---+---+---+---+
 lane A | T |   |   | T |   |   | T |   |
 lane B |   | T |   |   |   | T |   |   |
 lane C |   |   |   |   | T |   |   | T |
        +---+---+---+---+---+---+---+---+
          ^ playhead, sweeping left to right, looping
```

- **Hook:** the map is a step sequencer. A line of light sweeps across it on a loop, and a tower fires only at the instant the line crosses it.
- **Mechanic:** where a tower stands decides when it fires as well as what it can reach. Enemies move in time with the loop, so a tower placed one column over can miss every enemy for the whole level.
- **Rule:** towers in the same column fire together and combine. Towers in a row fire in sequence and chain. The defense is a pattern, and it can be heard.
- **Traversal:** the player can stand on the tempo dial. Faster means more shots and faster enemies. Slower means time to think and a longer gap between volleys.
- **Hazard:** an enemy that moves on the off-beat.
- **Resources:** extra steps, lengthening the loop.
- **Boss:** it changes the time signature.
- **Favours:** builder with an ear.

## 64. Shell Game
*Assumption broken: everyone knows where the hill is.*

```
S####################
....................#
.........[ ? ]......#        three mounds
....[ ? ]......[ ? ]#        one queen
....................#        the enemy has to guess
```

- **Hook:** there are three hills and only one has the queen in it. The enemy does not know which. Neither does anything else on the map that could tell them.
- **Mechanic:** enemies attack the mound they believe is real, and belief is a visible number over each one. It rises with evidence: workers going in and out, towers clustered round it, the player standing near it.
- **Rule:** defending the real hill well is the surest way to reveal it. The player has to spend on decoys, guard the fakes convincingly, and leave the real one looking neglected.
- **Hazard:** scouts. One that gets inside a mound and back out again settles the question for its whole side.
- **Traversal:** between waves the queen can be moved through a tunnel, if nobody sees.
- **Resources:** split three ways, by the player's own bluff.
- **Boss:** an enemy that attacks all three at once and watches which one the player runs to.
- **Favours:** a player with a straight face.

## 65. Market Day
*Assumption broken: this is a war.*

```
S o  o   o o    o   o  o o   >> exit
..[honey]..[seeds]...[silk]....
............................HH   <- the till
   o  browsing     o! impatient     o$ buying
```

- **Hook:** the creatures on the road are customers. The towers are stalls. Nobody is here to fight, at first.
- **Mechanic:** each visitor has money, wants, and patience. A stall "hits" by making a sale. Range is how far the smell carries. Fire rate is service speed. The hill's health bar is the day's takings against the rent.
- **Rule:** a customer who walks the whole road without finding what they wanted leaves angry, and angry customers come back in the evening with friends, at which point it becomes the other kind of level, defended by stalls.
- **Hazard:** a rival market opens across the road and undercuts.
- **Resources:** stock, which runs out, and has to be carried from the hill by hand during opening hours.
- **Boss:** the inspector, who must be walked past every stall without seeing the one that is not strictly legal.
- **Favours:** gatherer. The player is the supply chain.

## 66. Generations
*Assumption broken: the player's character lasts the whole level.*

```
 wave 1      wave 2      wave 3      wave 4
  P(a) --x    P(b) --x    P(c) --x    P(d) ...
  strong      fast        can dig     short-sighted
       \________\___________\__________ what each left behind
```

- **Hook:** a worker ant lives a few weeks. In this level each wave is a lifetime. The player's character dies of old age at the end of every one, and the next wave is played as someone new.
- **Mechanic:** each successor has different traits, not chosen. One is strong, one is quick, one can dig, one cannot see far. The plan has to survive being handed to someone who cannot carry it out the same way.
- **Rule:** nothing passes on except what was physically left on the map: towers, stockpiles, paths worn in the ground, and one short message scratched in the dirt for whoever comes next.
- **Hazard:** a wave that runs long is fought by a character slowing down.
- **Resources:** each life starts with what the last one saved rather than spent.
- **Twist:** the last character is the queen's own replacement, and the level ends when she is old enough to take over, not when the enemy stops.
- **Favours:** each role in turn, by lot.

## 67. One of Many
*Assumption broken: you are in charge.*

```
S############.........
.....a..a...#..a......     a  the rest of the colony, deciding for itself
...a....P...#....a....     P  you: no build menu, no orders
.......a....####HH....
```

- **Hook:** the player is an ordinary worker. There is no build menu, no cursor, no command over anything. The colony builds its own defense by consensus, and the player is one voice in four thousand.
- **Mechanic:** influence is the only tool. The player lays scent toward a spot they think needs a tower, carries material there to make the idea easier, and starts the work so others join. If enough ants agree, it gets built. Often it gets built somewhere slightly wrong.
- **Rule:** the colony is not stupid, only slow, and it is sometimes right when the player is wrong. A crowd of ants ignoring the player's trail is information.
- **Hazard:** panic, which spreads the same way good ideas do, and faster.
- **Resources:** gathered by everyone. The player's share is one ant's worth.
- **Boss:** nothing special arrives. The hardest wave is hard because the colony has decided, wrongly, that the danger is on the other side.
- **Favours:** a persuasive player. Leading by example is the whole control scheme.

## 68. Cordyceps
*Assumption broken: the enemy comes from outside.*

```
       S   (nothing ever comes out of it)

   a   a   a   A?  a   a
   a   A?  a   a   a   a        A?  behaving a little oddly
   a   a   a   a   A!  a        A!  climbing toward the light
              [HH]
```

- **Hook:** a real thing. A fungus infects an ant, steers it to a high place, kills it there, and rains spores on the colony below. In this level the spawn point stays empty. The enemy is already inside.
- **Mechanic:** infected workers look normal, then slightly wrong: walking a little apart, pausing, drifting upward. The player has to notice. Towers here are quarantine posts, checkpoints, and high places made unreachable.
- **Rule:** every response costs the colony. Isolate too early and a healthy worker is lost. Wait for certainty and it has already climbed.
- **Hazard:** the towers are staffed by ants as well.
- **Resources:** fall as the workforce does, so a careless quarantine is its own kind of defeat.
- **Boss:** the player's character starts walking a little apart. The last stretch is played with controls that pull gently upward.
- **Favours:** an observant player. Almost no shooting.

## 69. Opening Night
*Assumption broken: winning efficiently is the goal.*

```
   ~~~~~~~~~~~ curtain ~~~~~~~~~~~
   S##########................
   ..........#....T...........       stage
   ..........#########HH......
   -----------------------------
    o o o o o o o o o o o o o o      audience
    applause: [#####.....]
```

- **Hook:** the battle is a play, performed on a stage in front of an audience of insects. The enemies are the cast. The score is applause.
- **Mechanic:** the audience wants drama. A wave wiped out at the door is booed. A hill saved at the last moment with one point of health gets a standing ovation. The player has to defend well enough to survive and badly enough to be exciting.
- **Rule:** the crowd has taste. Repeating the same trick bores them. Reversals, last stands, a tower sacrificed at the right moment and an underdog enemy almost making it all pay well.
- **Hazard:** stage machinery. Scenery flies in and out, a trapdoor opens, the lights change and towers fire at silhouettes.
- **Resources:** thrown onto the stage by the audience. Flowers when pleased. Other things when not.
- **Boss:** the critic, a single audience member whose reaction counts for half the house, and who has seen this play before.
- **Favours:** a show-off.

## 70. Panels
*Assumption broken: time and space are continuous.*

```
+-----------+ +-----------+ +-----------+
| 1         | | 2         | | 3         |
| S####     | |   ####    | |    T      |
|     #     | |      #    | |    ####HH |
+-----------+ +-----------+ +-----------+
        the gutter: nothing exists here
```

- **Hook:** the level is a page of a comic. Each panel is a place and a moment, and enemies cross from one to the next through the blank gutter between them.
- **Mechanic:** the player can rearrange the panels. Order on the page is order in time, so swapping two panels changes what happens first. Put the panel where the bridge collapses before the panel where the enemies cross it.
- **Rule:** towers fire inside their own panel only, but a big enough shot breaks the frame and lands in the next one. Sound effects are solid objects. Speech bubbles can be stood on.
- **Hazard:** the gutter. Anything can happen in the gap between two panels, and the reader's imagination is not on the player's side: enemies arrive in the next panel rested, or doubled, or already past.
- **Resources:** a limited number of panels can be redrawn larger, which slows time inside them.
- **Boss:** a splash page. One enemy too big for any panel, drawn across all of them at once.
- **Favours:** planner.

## 71. Input
*Assumption broken: the controls are outside the level.*

```
 [Esc]
 [ Q ][ W ][ E ][ R ]S############
 [ A ][ S ][ D ][ F ]............#
 [Shift][ Z ][ X ]...........####HH
 [Ctrl][        Space        ]
   every key you press goes down, here, on the map
```

- **Hook:** the map is a keyboard, and it is the player's own. Every key pressed to play the game is pressed on the map as well.
- **Mechanic:** walking with W pushes the W key down and makes a pit where it was. The build hotkey slams down its own key along with whatever was standing there. The space bar is a quarter of the map.
- **Rule:** the player is using the terrain to play and the terrain to fight with the same hand. Enemies crossing a key can be dropped by pressing it. So can the player's own towers.
- **Hazard:** enemies press keys by walking on them. A heavy one crossing the build key places something. A column marching across Esc is a problem.
- **Resources:** crumbs under the keycaps, reachable only while a key is held.
- **Boss:** something that sits on Caps Lock.
- **Favours:** a player who can rebind their habits mid-level.

## 72. Roll Credits
*Assumption broken: the game is not over yet.*

```
            HOLD THE HILL

          Turbulent Towers Studio

     S### Programming ####
                         #
          Art  ##########         ^ everything scrolls up
          #                       and off the top
          ##### Design ####HH
```

- **Hook:** the final boss is dead and the credits are rolling. Then something walks in from the edge of the screen. The last level is played on the credits.
- **Mechanic:** the names and headings are the terrain. Enemies walk along lines of text, towers are built on letters, and the whole map scrolls upward at a fixed speed, carrying defenses off the top of the screen.
- **Rule:** new ground arrives from the bottom and there is no telling what shape it will be. A long job title is a bridge. A short name is a gap. The special thanks section is one enormous flat field.
- **Hazard:** the music is ending. When the credits run out there is nothing left to stand on.
- **Resources:** the logos, which are the only solid objects, and scroll past once.
- **Twist:** the team's own names are in there, and each one does something when an enemy reaches it, chosen by that person.
- **Favours:** builder on a conveyor belt.
