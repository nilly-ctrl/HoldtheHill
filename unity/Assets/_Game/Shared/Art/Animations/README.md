# Animations (pixel art, v1)

> **Most of this library now lives in the team Google Drive, not in the repo (2026-10-07).**
> Only the 109 sprites the graybox and weapon picker use are kept here. The other 2,725, the review
> GIFs and gallery pages, and the screenshots are in `Turbulent Towers/Art/Animation Library/`
> (`Aseprite/`, `Sheets/`, `Review/`) and `Art/Screenshots/`, with their `.meta` files. To use one in
> the game, copy its `.aseprite`, sheet `.png` and `.json` and their `.meta` files back into the matching
> folder here. The build scripts in `Source~/` still write the whole library, so move new output
> to Drive instead of committing it. `Source~/ArtCatalogue.csv` still lists every sprite.

Animation sheets for every tower, every graybox enemy, and the effects the new towers and enemies need. They use the same palette and 1 px ink outline as `Icons/`, and they're generated from code.

| Folder | What's in it |
|---|---|
| `Aseprite/<Group>/` | One layered `.aseprite` per sprite, with one **tag per animation**. Unity's Aseprite Importer turns each tag into an AnimationClip and builds a prefab with an Animator. `Editor/PixelAnimImporter.cs` sets 32 PPU and a centred pivot the first time a file imports. |
| `Sheets/<Group>/` | The same frames packed into a PNG, one row per tag, plus a `.json` in Aseprite's json-array format (frame rects, durations, `frameTags`). Use these outside Unity or for review. |
| `Source~/` | `build_anim_sheets.py` (draws everything), `aseprite_file.py` (writes and reads `.aseprite`), `AnimSheetPreview.png` (every frame at 3x) and `Preview/*.gif` (4x, cycles every tag). Unity ignores this folder. |

**Format check.** `python Source~/validate_aseprite.py` reads every `.aseprite` byte by byte against the published format (header, frame and chunk sizes, layers, cels that inflate to the right size and sit inside the canvas, tags in range and unique, palette). It shares no code with the writer. Every file passes.

Aseprite isn't installed on the machine that built these. The script writes `.aseprite` files straight to [Aseprite's file spec](https://github.com/aseprite/aseprite/blob/main/docs/ase-file-specs.md), then reads each one back and compares every frame. All 21 were batch-imported in Unity 6000.6.0f1. They haven't been opened in Aseprite itself yet.

## Orientation and scale

- **Towers** are 32x32, top-down, facing up. They don't rotate in game. At 32 PPU one tower fills one world unit.
- **Enemies** are top-down and **face right (+x)**, because `EnemyMover` rotates the transform so that +x points along the path. Sizes: Swarm 16, Runner/Grunt 24, Shielded/Splitter/Healer 32, Brute 40.
- **Effects** are centred. The three ring effects are drawn at real size, so their pixels match everything else: FrostPulse ends 112 px out (3.5 units), Shockwave 96 px (3 units), HealPulse 90 px (2.8 units). The animators scale them only by the small amount an upgrade changes the radius.

## What's in each file

| File | Creature | Tags (one-shot tags marked *) |
|---|---|---|
| TowerLinear | Spitter | Idle, Attack*, Upgrade* |
| TowerHoming | Seeker | Idle, Attack*, Upgrade* |
| TowerMortar | Bombardier | Idle, Attack*, Upgrade* |
| TowerRicochet | Slinger | Idle, Attack*, Upgrade* |
| TowerChain | Storm Ant | Idle, Attack*, Upgrade* |
| TowerBeam | Dewdrop Lens | Idle, Attack*, Upgrade* |
| TowerOrbit | Swarm Nest | Idle, Attack*, Upgrade* |
| TowerFrostAura | Frost Ant | Idle, Attack*, Upgrade* |
| TowerKnockback | Kicker | Idle, Attack*, Upgrade* |
| TowerMineLayer | Sapper | Idle, Attack*, Upgrade* |
| EnemyRunner | Scarab | Walk, Hurt*, Death* |
| EnemyGrunt | Beetle | Walk, Hurt*, Death* |
| EnemyBrute | Stag Beetle | Walk, Hurt*, Death* |
| EnemyShielded | Rhino Beetle | Walk (shield up), ShieldHit*, ShieldBreak*, WalkBare, Hurt*, Death* |
| EnemySplitter | Centipede | Walk, Hurt*, Death* (bursts into three hatchlings) |
| EnemySwarm | Centipede Hatchling | Walk, Hurt*, Death* |
| EnemyHealer | Mantis | Walk, Heal*, Hurt*, Death* |
| FxMine | Sapper's mine | Armed, Explode* |
| FxFrostPulse | Frost Ant pulse | Pulse* |
| FxShockwave | Kicker stomp | Blast* |
| FxHealPulse | Mantis heal | Pulse* |
| FxOrbiter | Swarm Nest ant (16x16, faces right) | Fly |
| FxImpact | Hit effects (24x24) | Acid*, Spark*, Pop*, Zap*, Sizzle*, Scratch* |
| FxBlast | Mortar blast (48x48) | Blast* |
| FxBuild | Tower build and sell (48x48) | Rise*, Sell* |
| FxTier | Upgrade overlays drawn over a tower (32x32) | Tier2, Tier3 |
| PropHill | The hill being defended (64x64) | Healthy, Damaged, Critical, Destroyed |
| PropBurrow | Enemy burrow (48x48) | Idle, Spawn* |
| FxStatus | Status icons | Slow, Burn, Poison (12x12, float over enemies) |
| ProjLinear / ProjHoming / ProjMortar / ProjRicochet / ProjFragment | Projectiles (16x16, face right) | Fly |
| FxHazard | Mortar fire puddle (64x64) | Burn |

One-shot tags have Repeat = 1, so Unity imports them with Loop Time off. Layers are `Shadow`, `Base` (the tower's ant hill), `Body`, `Shield` (Shielded only) and `FX`. The importer merges them into one sprite per frame by default.

Creature and caste names are suggestions, matched to `Icons/`. Code still uses the mechanic names (Runner, Grunt, Linear, Mortar and so on).

## Weapon library

`Source~/build_weapon_sheets.py` adds three more groups. The graybox towers use their `FxTower` extras (see "In the graybox"). The 23 `Wpn` families and the melee castes are not in the game; they're here to pick from, and **Tools > Hold the Hill > Build Weapon Picker** makes a scene that plays each family end to end.

**`Aseprite/Weapons/Wpn<Family>` (23 files, 32x32).** One per firing-sound family in `Audio/Sfx/TowerFire/`, each redrawn as something an ant colony could make.

| Tag | What it is |
|---|---|
| `Fly` | The shot, looping, **facing right** so it can be rotated along its path. For Laser, LightningBolt, FlameBurst and SonicPulse it is a strip or burst running left to right; the Laser strip tiles end to end. |
| `Trail`* | Spawn copies at the shot's position as it flies and let them fade. |
| `Hit`* | Plays where the shot lands on an enemy. |
| `Miss`* | The same hit landing on bare ground: duller and dustier. |
| `Muzzle`* | Plays at the tower's mouth, opening to the right. |
| `Charge`* | Wind-up before firing. Only the heavier weapons have one. |
| `Fly2`, `Fly3` | The shot from a tier 2 and tier 3 tower: a size up and a rim of glow, plus sparkles and a wake at tier 3. |
| `Hit2`*, `Hit3`* | The hit to match: an extra ring at tier 2; rays and sparkles as well at tier 3. |

| Family | Drawn as | Family | Drawn as |
|---|---|---|---|
| AcidSpit | Formic acid glob | MagicOrb | Royal-jelly orb |
| BoulderToss | Heaved river pebble | Mortar | Mud bomb |
| BubbleShot | Dew bubble | PebbleSling | Slung pebble |
| Cannon | Seed-pod launcher | Plasma | Glow-spore |
| Catapult | Acorn lob | PollenPuff | Pollen cloud |
| Crossbow | Thorn bolt | SeedBurst | Seed scatter |
| EmberFlick | Glowing ember | SonicPulse | Stridulation wave |
| FlameBurst | Formic fire jet | StingerDart | Wasp-stinger dart |
| Harpoon | Barbed stinger on silk | VenomBolt | Venom bolt |
| HoneyGlob | Honey glob (its hit spreads into a puddle) | WebShot | Silk web ball (its hit is a web) |
| IceShard | Frost shard | Laser | Dew-lens ray |
| LightningBolt | Static arc | | |

**`Aseprite/Fx/FxTower<Name>` (10 files).** The pieces the ten graybox towers were missing: `Trail`*, `Miss`*, `Muzzle`*, `Charge`* where the weapon winds up, and one special each for five of them: Slinger `Bounce`*, Swarm Nest `Bite`*, Frost Ant `Freeze` (a looping ice crust to sit over a chilled enemy), Kicker `Crack`* (ground cracks), Sapper `Lay`* (dirt kicked up as a mine goes in). Their tier 1 shots and hits are the existing `Proj*` and `FxImpact` files; each `FxTower` file also carries `Hit2`* and `Hit3`*, and `Fly2` and `Fly3` for the four towers that fire a projectile. The tiered clips are drawn but nothing plays them yet.

**The four melee castes.** `Aseprite/Towers/TowerWorker`, `TowerSoldier`, `TowerMajor` and `TowerNurse` (Idle, Attack*, Upgrade*, same layout as the other towers), plus `Aseprite/Fx/FxMelee` with `Bite`*, `Slash`*, `Slam`*, `Block`* and `Heal`*.

Review sheets: `Source~/WeaponSheetPreview.png`, `TowerExtrasPreview.png`, `CasteSheetPreview.png`, and a GIF per file in `Source~/Preview/`.

## Enemy moves, hill hits and map dressing

`Source~/build_extra_sheets.py` writes these. It is a separate script so the files the graybox already uses are not rewritten.

| File | What it is | Tags (one-shot tags marked *) |
|---|---|---|
| `Enemies/Enemy<Name>Moves` (7 files) | Each enemy biting the hill and digging out of the ground. Same canvas, pivot and layers as `Enemy<Name>`, so the clips swap in without moving anything. | Attack*, Spawn* |
| `Fx/FxHillHit` (48x48) | What the hill shows when an enemy reaches it: a bite, a brute's blow, and something slipping inside. | Hit*, HitHeavy*, Breach* |
| `Props/DecorGrass`, `DecorFlower` (16x16) | Grass tufts and flowers. Each tag is one variant and sways. | A, B, C |
| `Props/DecorPebble`, `DecorMushroom` (16x16), `DecorLeaf`, `DecorTwig` (24x24), `DecorRoot` (32x32) | Still pieces. Each tag is one variant, a single frame. | A, B (and C) |
| `Props/DecorTrailEdge` (32x32) | Pieces laid along the trail so its border is not a ruled line: grass overhang (A), dirt crumbs (B), a line of stones (C). Drawn with the trail running left to right under them. | A, B, C |

The graybox plays the enemy moves and hill hits (the library builder folds each `Enemy<Name>Moves` into `Enemy<Name>`), and its build places the map dressing (below). Review sheets: `Source~/EnemyMovesPreview.png` and `DecorPreview.png`.

## Browse everything

Open `Source~/Gallery.html` in a browser. It is an index; each group (Towers, Enemies, Props and so on) opens as its own page in `Source~/Gallery/`, with every animation playing, a filter on name or description, a size setting and a choice of background. The pages are self-contained and need no server. `python build_gallery.py` rebuilds them from `Sheets/`.

`Source~/ArtCatalogue.csv` lists every sheet and ground tile: what it is, canvas size, frames, tags, which original a seasonal or biome version was made from, and whether the game uses it yet. That last column is worked out from the sandbox's C#, not typed in. `python build_catalogue.py` rebuilds it.

## Tower tiers, world and interface sheets (drawn, not used by anything yet)

**`Aseprite/Towers/Tower<Name>T2` and `T3` (28 files)**, from `Source~/build_tier_sheets.py`. Tier 2 and tier 3 body art for all 14 towers, with the same tags, canvas, pivot and timing as `Tower<Name>`, so a tier swaps in without moving anything. Tier 2 adds a ring of fitted stones, a tan banner, a stone back plate and a pale band on the ant. Tier 3 has gold-capped stones, two red banners, a gold plate, band and head crest, and a glint that travels round the mound while idle. `Source~/TowerTierPreview.png` shows every tower at all three tiers. These would replace the `FxTier` overlay.

**From `Source~/build_world_sheets.py`:**

| File | What it is | Tags (one-shot tags marked *) |
|---|---|---|
| `Props/PropQueen` (48x48) | The queen, top-down, facing up | Idle, Alarm*, Cheer*, Fall* (ends slumped and grey, crown rolled off) |
| `Props/PropChamber` (64x64) | Her chamber inside the hill: dais, eggs, glow-fungus lamps | Idle, Breached (lamps turn red, dirt falls) |
| `Props/PickupFood` (16x16) | Food on the field, bobbing | Crumb, Seed, Berry, Honey |
| `Fx/FxPickup` (16x16) | A pickup appearing and being collected | Spawn*, Collect* |
| `Ui/UiWaveBanner` (96x32) | Leaf-scroll banner that announces a wave. The middle is left blank for the game to draw the wave number on. | In*, Hold, Out* |
| `Ui/UiVictory`, `Ui/UiDefeat` (96x64) | End-of-run scenes: the hill standing with its flag and cheering ants, or fallen with a beetle on the ruin | Intro*, Loop |
| `Ui/UiCursor` (16x16) | Mouse cursors. The hot spot is the top-left pixel. | Point, Click*, Build, Sell, Deny |
| `Ui/UiPlaceMarker` (32x32) | The tile under the cursor | Valid, Invalid, Select, Placed* |

`Fx/FxStatusOverlay` (32x32, from `build_extra_sheets.py`) holds loops worn over an enemy while a status lasts: Burn, Poison, Web, Honey. The Frost Ant's `Freeze` crust is the fifth of that family.

Review sheets: `Source~/WorldSheetPreview.png`, `UiSheetPreview.png`, `StatusOverlayPreview.png`.

**Scenery, from `Source~/build_scenery_sheets.py` (drawn, not used by anything yet):**

| File | What it is | Tags (one-shot tags marked *) |
|---|---|---|
| `Props/LightTorch` (32x32) | Twig torch | Lit, Flare*, Out |
| `Props/LightEmberPit` | Pebble-ringed fire | Burn, Embers |
| `Props/LightGlowCap` | Glowing mushroom | Glow, Dim |
| `Props/LightLantern` | A firefly in a seed-pod cage | Glow |
| `Props/LightCrystal` | Dew crystal | Glow |
| `Fx/FxLightGlow` (64x64) | Dithered pools of light to lay under a light source | Warm, Cool, Green |
| `Props/BuildingGranary`, `BuildingNursery`, `BuildingBarracks`, `BuildingWatchPost`, `BuildingFungusFarm` (48x48) | Colony buildings. Each has Idle, Build* (rises out of the ground), Ruin, and one action. | Deliver*, Hatch*, Drill*, Alert*, Harvest* |
| `Props/ObjClutter` (16x16) | Small objects, one per tag | Acorn, SeedSack, Shell, Cairn, Dewdrop |
| `Props/ObjSignpost` (24x24), `ObjSideHole` (32x32) | A twig signpost; a side entrance a worker peeks out of | Idle, Wobble* / Idle, Peek* |
| `Props/ObjThornFence` (32x32) | Fence pieces | Horizontal, Vertical, Corner, Broken |
| `Props/ObjWeb` (32x32), `ObjLog` (48x32) | A corner web with something caught; a hollow log | Sway / Idle |
| `Fx/FxParticles` (16x16) | Small motes | Dust*, Ember, Spore, Pollen, Leaf*, Firefly, Sparkle, Smoke |
| `Fx/FxWeather` (32x32) | Pieces that tile in both directions | Rain, RainSplash*, Mist, Wind |

Review sheets: `Source~/LightSheetPreview.png`, `BuildingSheetPreview.png`, `ObjectSheetPreview.png`.

**Nature and colony life, from `Source~/build_nature_sheets.py` (drawn, not used by anything yet):**

| File | What it is | Tags (one-shot tags marked *) |
|---|---|---|
| `Props/WaterPuddle` (32x32) | Still water with a drifting glint | Small, Large |
| `Props/WaterStream` (32x32) | Flowing water that tiles along its length. The bend comes in from the left and leaves at the bottom; turn it for the other three. | Horizontal, Vertical, Bend |
| `Fx/FxWater` (32x32) | Water effects | Ripple*, Splash* |
| `Props/TerrainBridge` (48x32) | Stream crossings, drawn over a strip of stream running up and down | Twig, Leaf, Broken |
| `Props/TerrainCliff` (32x32) | Ledge pieces with the drop toward the bottom | Edge, CornerLeft, CornerRight |
| `Props/TerrainTunnel` (32x32) | Tunnel mouth | Idle, Eyes, Collapse* |
| `Props/PlantFern`, `PlantTallGrass` (32x32), `PlantClover` (24x24) | Bigger plants | Sway (clover also has Lucky, the four-leaf one) |
| `Props/PlantDandelion` (32x32) | Dandelion in flower and gone to seed | Bloom, Clock, Blow* |
| `Props/RockBoulder` (32x32) | Boulders | A, B, Mossy |
| `Props/AntWorker` (24x16) | Ambient worker. **Faces right**, like the enemies, so it can be turned along a trail. The body sits left of centre to leave room for what it carries. | Idle, Walk, Groom, CarrySeed, CarryCrumb, CarryLeaf, CarryPebble, Dig, Tamp |
| `Props/AntNurse` (24x16) | Ambient nurse | Idle, Walk, CarryEgg, Tend |
| `Props/AntGuard` (24x24) | Ambient guard | Idle, Walk, Alert* |
| `Props/AntPair` (40x16) | Two workers meeting head to head | Talk |

Review sheets: `Source~/NatureSheetPreview.png`, `ColonySheetPreview.png`.

## Roster, seasons and interface sheets (drawn, not used by anything yet)

**New enemies and towers, from `Source~/build_roster_sheets.py`:**

| File | What it is | Tags (one-shot tags marked *) |
|---|---|---|
| `Enemies/EnemyWasp` (32x32) | A flyer: its shadow sits apart from it | Walk, Hurt*, Death*, Attack*, Spawn*, Sting* |
| `Enemies/EnemySpider` (32x32) | Long-legged spider | the same five, plus Rear* |
| `Enemies/EnemySnail` (40x40) | A slow, armoured tank | the same five, plus Hide*, Unhide* |
| `Enemies/EnemyGrub` (24x24) | A grub that goes under the ground | the same five, plus Burrow*, Emerge* |
| `Enemies/EnemyBoss` (64x64) | The titan beetle | the same five, plus Roar* |
| `Towers/TowerHoneypot`, `TowerWeaver`, `TowerScout`, `TowerQueensGuard`, `TowerFungusFarmer`, each also as `...T2` and `...T3` | Five new castes at all three tiers | Idle, Attack*, Upgrade* |
| `Fx/FxTowerHoneypot` and the other four | Each new caste's shot | Fly, Trail*, Hit*, Miss*, Muzzle*, Fly2, Fly3, Hit2*, Hit3* (the Queen's Guard also has Charge*) |

These enemies keep all their animations in one file; there is no separate `Moves` file. Review sheets: `Source~/NewEnemyPreview.png`, `NewTowerPreview.png`, `NewTowerShotPreview.png`.

**Seasons and time of day, from `Source~/build_season_sheets.py`.** These are recoloured from the summer originals rather than redrawn, so every piece lines up with the one it replaces.

- `../Tiles/Seasons/`: `GroundGrass`, `RoadDirt` and `RoadDirtFill`, each as `Autumn`, `Winter` and `Night`. Same sizes; they still tile.
- `Props/<Name>Autumn`, `<Name>Winter`, `<Name>Night` for `PropHill`, `DecorGrass`, `PlantFern`, `PlantTallGrass`, `PlantClover` and `RockBoulder`, with the original's tags. Winter lays snow on every upward edge. Towers, buildings and enemies have no seasonal versions.
- `Props/DecorSnow` (A, B, C), `Props/DecorLeafPile` (A, B), `Props/WaterIce` (Small, Large, Crack*).
- `Fx/FxSeason` (32x32, tiles): Snow, Leaves, and Breath*. `Fx/FxMoonlight` (64x64): Moon, Cloud.

Review sheets: `Source~/SeasonTilePreview.png`, `SeasonPropPreview.png`, `SeasonExtrasPreview.png`.

**Animated interface pieces, from `Source~/build_hud_sheets.py`** (group `Ui`). None carries lettering except the logo; the game draws numbers and labels over them.

| File | Tags (one-shot tags marked *) |
|---|---|
| `UiHealthBar` (80x16) | Idle, Damage*, Heal*, Low. Drawn full: the game still owns how full the bar is. |
| `UiFoodCounter` (40x16) | Idle, Gain*, Spend*, Cant* |
| `UiWaveTrack` (96x16) | Idle, Advance*, Boss |
| `UiSkillNode` (24x24) | Locked, Available, Unlock*, Owned |
| `UiButton` (48x16) | Normal, Hover, Press*, Disabled |
| `UiTooltip` (64x32) | Open*, Idle, Close* |
| `UiStar` (16x16) | Empty, Fill*, Full |
| `UiTitleLogo` (160x64) | Intro*, Loop. "HOLD THE HILL" in the project's pixel letters, read from `Fonts/Source~/build_pixel_fonts.py`. |

Review sheets: `Source~/HudSheetPreview.png`, `LogoSheetPreview.png`.

**Mock-up.** `python build_mockup.py` (or `python build_mockup.py Winter`) composes a dressed map from the finished sheets alone and saves `Source~/MockupScene.png` and `.gif` (and `MockupSceneWinter`). It is only for judging how the pieces sit together; nothing in the game uses it.

## Elites, more bosses, a second biome, story scenes (drawn, not used by anything yet)

**From `Source~/build_elite_sheets.py`:**

- `Enemies/Enemy<Name>Elite` for all 12 enemies: the same frames and tags in a darker blood-red livery with a gold rim round the body. For the first seven, the Attack and Spawn from `Enemy<Name>Moves` are folded in, so each elite is one complete file.
- `Enemies/EnemyMantisQueen` (64x64): Walk, Hurt*, Death*, Attack*, Spawn*, Summon*.
- `Enemies/EnemyHornet` (64x64, a flyer): the same five, plus Dive*.

Review sheets: `Source~/ElitePreview.png`, `NewBossPreview.png`.

**Seasonal versions of everything else** (also `build_season_sheets.py`): every tower at every tier as `...Autumn` and `...Winter` (only the mound changes; the ant and its effects keep their colours), and Autumn and Winter versions of the five buildings, the objects, the rest of the decor, the burrow, bridge, cliff, tunnel and dandelion. The five lights have a Winter version only. `Source~/SeasonEverythingPreview.png` shows a sample.

**The riverbank, from `Source~/build_biome_sheets.py`.** A sandy shore beside slow water; towers, enemies and colony pieces are shared with the meadow.

- `../Tiles/Riverbank/`: `GroundSand` (64x64), `RoadSand` and `RoadSandFill` (32x32).
- `Props/PropHillSand` (the hill built of sand, four states), `WaterShore` (Edge, CornerLeft, CornerRight; sand above, water below, foam that breathes), `PlantReed` (32x48), `PlantBeachGrass`, `DecorShell` (A, B, C), `DecorDriftwood` (A, B), `RockRiver` (A, B, Wet), and `CritterStrider`, a water strider that faces right.

`Source~/RiverbankPreview.png` is a small composed view.

**Story scenes, from `Source~/build_story_sheets.py`** (group `Story`, 160x90, seen from the side):

| File | Tags (one-shot tags marked *) |
|---|---|
| `StoryFounding` | Intro* (a winged queen comes down out of the dawn and lands), Loop (she digs the first mound) |
| `StoryRaidWarning` | Loop (beetles cresting the ridge at dusk) |
| `StoryChamber` | Loop (a cut through the hill: tunnels, the queen, nurses and eggs, the seed store, workers passing) |
| `Ui/UiWaveCard` (128x48) | Normal, Swarm, Elite, Boss. No lettering; the right half is blank for the wave number. |

## More riverbank, the forest floor, more story art (drawn, not used by anything yet)

**From `Source~/build_biome_more_sheets.py`** (run `build_biome_sheets.py` first):

- **Riverbank:** `PropBurrowSand` and the five `Building<Kind>Sand`, `TerrainBridgeDrift` (Plank, Broken), `WaterShallows` (open water that tiles both ways), `LightShellLamp`, `LightReedTorch`, and `DecorRiver` (Feather, Weed, Tracks, Ripple, Pebbles).
- **Forest floor:** tiles in `../Tiles/Forest/` (`GroundLitter`, `RoadEarth`, `RoadEarthFill`); `PropHillForest`, `PropBurrowForest` and the five `Building<Kind>Forest` in dark loam with moss; `PlantToadstool` (Idle, Puff*), `PlantMoss` (A, B), `PlantSapling` (32x48), `ObjFallenLog` (64x32), `DecorLitter` (LeafA, LeafB, Needles, AcornCap, Cone), `LightFoxfire`, `CritterPillbug` (Walk, Roll*; faces right), and `Fx/FxDapple` (patches of sunlight that shift).
- **Both biomes in winter and at night:** each of their tiles also as `...Winter` and `...Night`, and `<Name>Winter` and `<Name>Night` for every prop unique to either biome.

`python build_mockup.py Riverbank` and `python build_mockup.py Forest` compose a map of each.

**From `Source~/build_story_more_sheets.py`:** `Story/StoryVictory` and `StoryDefeat` (Loop), `StoryBossBeetle`, `StoryBossMantis` and `StoryBossHornet` (Intro*, Loop), and `Ui/UiPortrait` (48x48): one tag for the Queen and each of the 19 tower castes, each a four-frame loop with a blink.

**Looking through the seasonal versions.** `python build_season_review.py` writes `SeasonReviewTowers.png` and `SeasonReviewProps.png`: every seasonal sprite beside its original, on a ground colour to match.

## Art themes (drawn, not used by anything yet)

**From `Source~/build_theme_sheets.py`.** Ten looks taken from the Ant Tower Formicary's art sets, drawn again at this project's 32 px scale, and two more made here (`NeonSpace` and `Vaporwave`). These are not recolours like the seasons: the ant and its mound are redrawn for each theme, and the rest of each tower (jaws, shots, sparks) is painted from the theme's palette. Each caste keeps one of the theme's three accent colours.

| Theme | The ant | The mound |
|---|---|---|
| `Neon` (Cyber Neon) | Black chrome, a visor, a power cell, lit antenna tips | Dark pad with a dashed ring of light |
| `Space` | White suit, bubble helmet, backpack, trim band | Cratered regolith with pad markers and a beacon |
| `Spooky` (Dark Horror) | Ashen, skull head, glowing sockets, ribs, tail spurs | Grave mound with a headstone, bones and a candle |
| `Steampunk` | Brass, top hat, goggles, pressure gauge, smokestack | A cog: teeth round the rim, rivets |
| `Medieval` | Steel helm with a plume, pauldrons, a cross on the tabard | Stone keep with merlons along the back |
| `Samurai` | Lacquered helmet with a gold crest, lamellar rows | Raked sand with a torii and fallen petals |
| `Candy` | Raspberry jelly, a cherry on top, candy stripe, sprinkles | Iced biscuit with a lollipop |
| `Jungle` | Feather headdress, war paint, bone necklace | Dark earth with leaves, a vine and bamboo stakes |
| `Pirate` | Tricorn, eye patch, striped shirt | Sand islet with planks, a keg, coins and a ring of water |
| `Robot` | Plated boxes instead of round segments, lamp eyes, hazard stripes | Steel deck with a hazard ring and a warning light |
| `NeonSpace` | Dark suit, a lit visor inside a neon-rimmed bubble helmet, backpack, trim band | Dark asteroid with craters, a lit landing ring, a beacon and stars |
| `Vaporwave` | Pastel lilac suit, pink bubble helmet, a sunset striped down the gaster | Purple pad with a teal grid running to the horizon and a striped sun setting behind it |

**`Military` changes the view** (`Source~/build_military_sheets.py`). The other themes keep the meadow's top-down ant facing up. Here each caste is an upright soldier ant in profile, facing right, in a sandbag emplacement, carrying its own weapon or tool, with an attack animation made for that weapon. Canvas, pivot, tags and timing are unchanged. Tier 2 adds sandbags, a pennant and a stripe on the helmet; tier 3 adds more sandbags, an ammo crate, a flag and a second stripe. Its enemies are the other army, side-on as well and walking right, in red helmets, with every tag the top-down originals have: the beetles carry a gun on the back like a turret (the Stag Beetle a cannon, the Rhino Beetle a shield), the Centipede is a column with packs and a gun on its head, the Mantis is a medic, the Wasp and Hornet carry a bomb, the Spider has twin guns, the Snail a howitzer through its shell, the Grub dynamite on its back; the Titan Beetle is a twin-cannon tank with a banner and the Mantis Queen a general with a sabre. Their shadows are flattened to lie under the feet. **To use them in game, flip these sprites instead of rotating them:** `EnemyMover` turns an enemy so +x points along the path, which is right for a top-down sprite and would stand a side-on one on its head.

| Caste | Soldier | Carries | Caste | Soldier | Carries |
|---|---|---|---|---|---|
| Spitter | Rifleman | Rifle | Worker | Engineer | Wrench |
| Seeker | Rocketeer | Shoulder launcher | Soldier | Infantry | Rifle and bayonet |
| Bombardier | Mortar crew | Mortar tube and shells | Major | Sergeant | Machete, peaked cap |
| Slinger | Shotgunner | Pump shotgun | Nurse | Medic | Syringe and satchel |
| Storm Ant | Signaller | Radio pack and arc lance | Honeypot | Foam trooper | Tank and sprayer |
| Dewdrop Lens | Sniper | Scoped long rifle | Weaver | Net gunner | Net launcher |
| Swarm Nest | Drone operator | Controller and two drones | Scout | Scout | Binoculars and pistol |
| Frost Ant | Cryo trooper | Coolant tank and sprayer | Queen's Guard | Commander | Megaphone, peaked cap |
| Kicker | Breacher | Sledgehammer | Fungus Farmer | Chem trooper | Gas mask, tank and sprayer |
| Sapper | Sapper | Shovel and a mine | | | |

- `Towers/Tower<Name><Theme>`, `...T2<Theme>`, `...T3<Theme>` for all 19 castes. Same tags, canvas, pivot and timing as `Tower<Name>`, so a theme swaps in without moving anything.
- `Enemies/Enemy<Name><Theme>` for the 12 enemies and the two bosses, and `Enemies/Enemy<Name>Elite<Theme>` for the 12 elites. Each is one complete file with Attack and Spawn folded in. Every creature gets drawn theme gear on top of its theme colours: markings along the trunk (a lit seam, glowing spots, ribs, rivets, a cross, lamellar rows, candy stripes, war paint, a striped shirt, hazard stripes) and headgear seen from above (visor, skull, top hat and goggles, helm and plume, crested helmet, cherry, feathers, tricorn, plated head). The headgear follows the head through every animation. Each theme's elites wear its own livery: the body steeped in one colour with its shading kept, and a two-colour rim outside the outline (`ELITE` in the script). Neon is overclocked magenta with a yellow rim, Space a purple strain rimmed in mint, Spooky possessed purple with an ecto rim, Steampunk copper with brass, Medieval black knights in gold, Samurai black lacquer with red and gold, Candy chocolate with gold foil, Jungle red war paint, Pirate black with gold braid, Robot red oxide with a hazard-striped rim, NeonSpace violet with cyan, Vaporwave cyan with yellow and pink, Military black with red and gold piping.

That is 83 files per theme.

**Shots, effects and map pieces.** Every theme also has 36 more sheets, named `<Name><Theme>` beside their originals and painted from the theme's palette: the five projectiles, `FxImpact`, `FxBlast`, `FxBuild`, `FxTier`, `FxStatus`, `FxStatusOverlay`, `FxHazard`, `FxMine`, the three ring effects, `FxOrbiter`, `FxMelee`, `FxHillHit`, all 15 `FxTower<Name>` files, `PropHill` and `PropBurrow`. `../Tiles/Themes/` holds `Ground<Theme>` (64x64), `Road<Theme>` and `RoadFill<Theme>` (32x32) for each: a lit grid for `Neon`, `NeonSpace` and `Vaporwave`, plain speckled ground for the rest. The sheets are palette versions, not redrawn, so how far they move from the originals depends on how much of the palette a theme replaces. Each theme has its own shot colours (`SHOT_COLOURS` in the script) except `Medieval`, whose shots, effects and enemies' colours stay close to the meadow's. 

**Decor, buildings and interface.** Every theme also has palette versions of the rest of the library, again named `<Name><Theme>`: the eight `Decor` files, the five lights and `FxLightGlow`, the five buildings, the objects, `FxParticles` and `FxWeather`, water, bridge, cliff, tunnel, the plants and boulders, the four ambient ants, `PropQueen`, `PropChamber`, `PickupFood` and `FxPickup`; and in `Ui/` the wave banner, victory and defeat, cursors, tile markers and the eight animated interface pieces including the logo. The seasonal, riverbank, forest and story sheets have no themed versions. Plants take whatever the theme does to the greens, so they come out cyan in Neon and orange in Space. `Theme<Theme>World.png` and `Theme<Theme>Ui.png` show them. `Theme<Theme>Extras.png` shows them. Review sheets: `Source~/ThemeCompare.png` (every caste, meadow beside each theme), `Theme<Theme>Towers.png` and `Theme<Theme>Enemies.png`. `python build_theme_sheets.py --preview` redraws those without writing any files; `python build_theme_sheets.py Neon Robot` builds only the themes named.

## Boss mechanics and special enemies

Art for the mechanics picked on 2026-10-06. The graybox plays all of it: see "Bosses and special enemies" under "In the graybox".

**New moves on the three bosses that were already drawn** (added where each sprite is built, so the elite versions have them too):

| File | New tags (one-shot tags marked *) | For |
|---|---|---|
| `Enemies/EnemyBoss` (Titan Beetle) | ChargeWindup*, Charge, PlateBreak*, WalkCracked, PlateBreak2*, WalkBare | Front armour; at 66% and 33% health it roars, charges a stretch of path and loses a wing-case plate |
| `Enemies/EnemyMantisQueen` | LayEgg*, Warded, WardBreak* | Lays `EnemyEggSac`s; takes no damage while three or more are alive |
| `Enemies/EnemyHornet` | Mark*, Carry, Flinch* | Marks a tower, then carries it off unless shot off its mark first |

**From `Source~/build_boss_sheets.py`:**

| File | What it is | Tags (one-shot tags marked *) |
|---|---|---|
| `Enemies/EnemyOrbWeaver` (64x64) | Boss. Strings threads over the map and walks them; cocoons towers | Walk, Hurt*, Death*, Attack*, Spawn*, Spin*, Cocoon*, ThreadWalk |
| `Enemies/EnemyBoulderBug` (64x64) | Boss. A giant pill bug: rolled up it is fast and immune; knockback or a mine pops it open | the same five, plus RollUp*, Roll, Pop*, Stunned, Recover* |
| `Enemies/EnemyRivalQueen` (64x64) | Boss. Queen of a red colony, walking to a spot to dig a second burrow | the same five, plus Dig, Call* |
| `Enemies/EnemySilverfish` (24x24) | Fast, one hit point; leaves an `FxQuickZone` where it dies | Walk, Hurt*, Death*, Attack*, Spawn* |
| `Enemies/EnemyThiefAnt` (24x24) | A masked red ant that takes food from the hill and runs back with a sack on its back. Faces right; turn it round for the run home | the same five, plus Grab*, WalkLoaded, Drop* |
| `Enemies/EnemyBombardier` (32x32) | Bombardier beetle; leaves an `FxScald` where it dies | the same five, plus Boil* (the tell before it bursts) |
| `Enemies/EnemyEggSac` (24x24) | What the Mantis Queen lays | Idle, Hurt*, Hatch*, Burst* (killed before hatching) |
| `Props/PropRivalBurrow` (48x48) | The rival queen's burrow as it is dug | Stage1, Stage2, Stage3, Complete, Collapse* |
| `Fx/FxWebThread` (32x32) | The orb-weaver's thread. `Strand` tiles left to right | Strand, Anchor, Snap*, Burn* |
| `Fx/FxQuickZone` (64x64) | Ground zone that speeds enemies up. Its chevrons run toward +x, so turn it along the path | Appear*, Loop, Fade* |
| `Fx/FxScald` (64x64) | Boiling puddle that silences towers | Burst*, Boil, Fade* |
| `Fx/FxTowerAffliction` (32x32) | Worn over a tower, same canvas and centre as the tower | Stunned, MarkLock*, Marked, WebWrap*, Webbed, WebBreak*, CocoonWrap*, Cocooned, CocoonBreak*, Scalded |

The Wasp's sting and the Titan's roar use `Stunned`; the Spider's web uses the `Web` tags; the Hornet uses `MarkLock` and `Marked`. The themed copies made by `build_theme_sheets.py` do not have the three bosses' new tags until that script is run again.

Review sheets: `Source~/BossMovesPreview.png`, `SpecialEnemyPreview.png`, `BossFxPreview.png`, and the first three bosses' new rows in `NewEnemyPreview.png` and `NewBossPreview.png`.

## In the graybox

**Tools > Hold the Hill > Build Graybox Combat Test** bakes these files into `Graybox/GrayboxAnimLibrary.asset` (you can also run **Rebuild Graybox Animation Library** on its own) and animates every scene tower, every enemy prefab, the mine, and towers placed at runtime. `SpriteClipPlayer` plays the clips. `GrayboxTowerAnimator`, `GrayboxEnemyAnimator` and `GrayboxMineAnimator` decide which clip plays when. After regenerating the `.aseprite` files, rebuild the graybox so the library picks up the changes.

Rebuilding the graybox also lays a grass and dirt-trail floor from `../Tiles/` (made by `Source~/build_tiles.py`), tints enemies and shows an icon while they're slowed, burning or poisoned (`GrayboxStatusVisuals`), and fills `Graybox/GrayboxSfxBank.asset` from `../Audio/Sfx/` for `GrayboxSfx`, which plays a sound for each of these moments. To change which sound family a cue uses, edit the table in `Editor/GrayboxSfxBankBuilder.cs` and rebuild.

The same rebuild also dresses the Swarm Nest's orbiters as ants, gives the lightning and beam their scrolling textures (`../Tiles/LineBolt.png`, `LineBeam.png`), and adds the pixel HUD skin (`GrayboxUi`, with art in `../Ui/`). Review screenshots from the last rebuild are in `../Screenshots~/`.

It also places the hill and burrow at the ends of the trail (`GrayboxPropVisuals`), adds hit effects wherever a weapon lands (`GrayboxImpactFx`), and shows tier overlays on upgraded towers. `../Screenshots~/preview_wave.gif` is an 11-second recording.

The same build scatters the `Decor` pieces over the grass, clear of the trail, and lays `DecorTrailEdge` pieces along both sides of it. The layout is seeded, so every rebuild gives the same map.

Towers pick up their `FxTower` extras by themselves (`GrayboxTowerExtras`, added by `GrayboxTowerAnimator`), so this part needs no rebuild beyond the library: a muzzle puff toward the target on each shot, a wind-up before the Bombardier, Storm Ant, Dewdrop Lens, Frost Ant and Kicker attack, a trail behind every shot and a puff of dirt where one lands on bare ground (`GrayboxShotTrail`), a spark where the Slinger's pebble bounces, cracks under the Kicker, dirt where the Sapper lays a mine, a nip where a Swarm Nest ant bites, and an ice crust over enemies the Frost Ant has slowed.

Enemies play `Spawn` as they leave the burrow and, on reaching the hill, `Attack` at its rim with an `FxHillHit` effect (heavy for the Stag Beetle, a breach for the two small ones). They wear an `FxStatusOverlay` loop while burning or poisoned, and for a slow the ice crust, or the web or honey when the slowing effect's name starts with `Web` or `Honey` (nothing applies those two yet). The Worker, Soldier, Major and Nurse can be built (Q, W, E, Y) and use `FxMelee`; see `GrayboxMeleeTower`.

**Sizing rule:** everything is authored at 32 px per unit and shown at scale 1. When swapping a placeholder for a sprite, reset the transform's scale: leaving Sliced draw mode makes Unity rescale the object.

### Bosses and special enemies

The code is in `../Graybox/Specials/`, one component per enemy, and `Editor/GrayboxSpecialsBuilder.cs` builds their prefabs. **Tools > Hold the Hill > Build Graybox Combat Test** adds them: seven waves after the original four (one per boss, plus a mixed wave of the others), and a `GrayboxSpecials` object in the scene. In Play mode, **B** opens a panel that spawns any of them at once.

| Enemy | What it does | What answers it |
|---|---|---|
| Titan Beetle | Hits from towers ahead of it do 35% (55%, then 80%, as plates break); from behind, full. At 66% and 33% health it roars (towers within 3.5 are stunned), then charges 5 units that slows cannot hold | Towers past a bend; poison and burn go round the armour; a mine or the Kicker ends a charge early |
| Mantis Queen | Lays an egg sac every 5 s. With 3 or more alive she takes no damage. A sac hatches into a Beetle after 8 s | Splash and chain towers on the sacs |
| Hornet | Flies. Picks the tower that has done most damage, marks it for 3 s, then carries it to the burrow. It is back when the next wave starts | Do 36 damage to it (2% of its health) during one mark and it is shaken off: that costs it another 6% of its health, it backs off for 3 s, and it tries a different tower next. Kill it while it carries and the tower is put back |
| Orb-Weaver | Cocoons the nearest tower for 8 s (released if she dies). Casts a thread across a corner and walks it | Fire or the Dewdrop Lens beam cuts the thread once: she hangs for 2.5 s taking double damage |
| Boulder Bug | Walks 5 s, then rolls for 6 s: fast, unslowable and immune | The Kicker or a mine pops it open: 4 s on its back taking 150% |
| Rival Queen | Stops 40% of the way along and digs for 14 s. If she finishes, 8 thief ants come out of her burrow | Kill her before she finishes and it falls in |
| Wasp | Flies straight to the hill; stuns the first tower it passes within 2.3 for 2.5 s | Anything but the Kicker, mines and fire puddles, which cannot reach a flyer |
| Spider | Every 5 s webs the nearest tower within 3.5 for 6 s: half rate of fire | |
| Silverfish | 1 health, very fast. Leaves a 5 s zone where enemies move 60% faster | Kill it somewhere the zone does no harm |
| Thief Ant | Takes 40 food at the hill and runs back to the burrow | Kill it on the way home and the food is returned |
| Bombardier Beetle | Leaves a 5 s puddle where it dies; towers within 2.2 cannot attack | Kill it away from your towers |

Towers have no health, so everything done to one is a timed disable (`GrayboxTowerAffliction`). A web only slows towers that fire on `Tower`'s own cooldown; the Frost Ant, Kicker and Sapper keep their own pace. The numbers above are first guesses and none of it is balanced, except the Hornet's shake-off, which was set from damage measured on its own wave (the reasoning is beside `_shakeOffFraction` in `GrayboxHornet.cs`): it takes towers that are busy with other enemies and is beaten by towers that cover each other. Review sheets from the scratch-copy run are `../Screenshots~/50_specials_*.png` to `54_`.

Three small hooks in the game scripts make this possible: `IIncomingDamageModifier` (asked by `EnemyHealth.TakeDamage`), `EnemyMover.SpeedScale`, `IgnoresKnockback` and `SkipToLastWaypoint`, and `Tower.FireRateScale`.

## Rebuild

```
cd Source~
python build_anim_sheets.py
python build_weapon_sheets.py
python build_extra_sheets.py
python build_tier_sheets.py
python build_world_sheets.py
python build_scenery_sheets.py
python build_nature_sheets.py
python build_roster_sheets.py
python build_season_sheets.py
python build_hud_sheets.py
python build_elite_sheets.py
python build_biome_sheets.py
python build_biome_more_sheets.py
python build_story_sheets.py
python build_story_more_sheets.py
python build_boss_sheets.py
python build_theme_sheets.py
python validate_aseprite.py
python build_gallery.py
python build_catalogue.py
python build_mockup.py
python build_tiles.py
```

Needs Python with Pillow. Each sprite is a function in `build_anim_sheets.py` (`draw_linear`, `enemy_healer`, `build_fx`, and so on). Edit it and rerun. If you hand-edit an `.aseprite` in Aseprite, a rerun overwrites it, so lock it first (`git lfs lock`) and stop regenerating that file.
