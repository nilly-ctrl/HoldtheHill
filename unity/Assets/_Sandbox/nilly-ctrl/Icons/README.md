# Icons (pixel art, v2)

95 game icons, the title logo and the app icon for Hold the Hill, as **32x32 pixel art**, generated from code so they stay consistent and easy to change.

| Folder | What's in it |
|---|---|
| `PNG/` | In-game icons, 32x32, transparent. `Editor/PixelIconImporter.cs` imports them as Sprites with Point filter, no compression, 32 PPU. Scale them up by whole numbers (2x, 3x, 4x) so pixels stay square. |
| `AppIcon/` | `AppIcon_32.png` is the source; the other sizes are nearest-neighbour upscales for Player Settings and mobile. `AppIconRounded_*.png` has transparent corners; `AppIcon.ico` is for Windows. |
| `Source~/` | `build_pixel_icons.py` and `IconSheet.png` (4x preview). `vector-v1/` holds the earlier vector set. Unity ignores this folder because its name ends in `~`. |

## Rebuild

```
cd Source~
python build_pixel_icons.py
```

Needs Python with Pillow. Creatures are hand-placed pixel sprites (the ASCII grids in `SPR`); props and UI are drawn straight onto the 32x32 grid and get an automatic 1 px outline. To change an icon, edit its function and rerun.

## Colour key

Amber badge = tower · red = enemy · purple = commander ability · green = resource/HUD · round badges = damage types, status effects, targeting priority (slate) · no badge = UI buttons.

## Tower names are placeholders

Towers are named by mechanic in code. The ant caste names are suggestions: Linear = Spitter, Homing = Seeker, Mortar = Bombardier, Ricochet = Slinger, Chain = Storm Ant, Beam = Dewdrop Lens, Orbit = Swarm Nest, Frost Aura = Frost Ant, Knockback = Kicker, Mine Layer = Sapper. Worker, Soldier, Major and Nurse come from the v2 design doc's castes, as do the enemies (Scarab, Beetle, Stag Beetle, Wasp Drone, Centipede, Mantis, Queen Wasp).

## Added 2026-10-04

- **Status effects (8):** `StatusBurnIcon`, `StatusPoisonIcon`, `StatusChillIcon`, `StatusShockIcon`, `StatusShieldIcon`, `StatusRegenIcon`, `StatusKnockbackIcon`, `StatusVulnerableIcon`. Round badges showing the affected beetle with the effect on it, so they read differently from the damage-type icons.
- **Combos (3):** `ComboFrostShatterIcon`, `ComboOverchargeIcon`, `ComboMineChainIcon`, for the combos in `GrayboxStatusCombos`. Navy round badges.
- **Skill tree (9):** one per `SkillNodeId`, named `Skill<Branch><Skill>Icon` (for example `SkillControlDeepFreezeIcon`). Hexagon badges coloured by branch: red = Ballistics, ice blue = Control, gold = Economy.
- **Title logo:** `TitleLogo.png`, 168x98. Built by `Source~/build_logo.py` from the same alphabet as the number fonts. Scale it by whole numbers.

## Added 2026-10-05

- **Tower icons redrawn (10):** one large weapon shape each, with a small ant head in the corner, so they read apart at a glance. Soldier, Major, Worker and Swarm Nest kept their own shapes. The earlier ant-body versions are in `Source~/towers-ant-body-v2/`.
- **Graybox enemies (7):** `EnemyRunnerIcon`, `EnemyGruntIcon`, `EnemyBruteIcon`, `EnemyShieldedIcon`, `EnemySwarmIcon`, `EnemySplitterIcon`, `EnemyHealerIcon`. These match the enemies in the graybox scene. The older enemy icons (Scarab, Beetle, Wasp and so on) are the design-doc roster and are still here.
- **Achievements (10):** `Ach<Name>Icon` on a blue crest badge, one per achievement in `GrayboxAchievements` (which now names them).
- **Redone:** `SkillEconomyScavengerBountiesIcon`, `ComboMineChainIcon`, and the ants on `TitleLogo.png`.
- **`Source~/TowerSpriteProposal.png`:** a proposal for giving the in-game tower sprites the same distinct shapes as the icons. Nothing in `Animations/` was changed. `build_tower_proposal.py` makes it.
