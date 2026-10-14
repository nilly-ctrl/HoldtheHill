# Floating damage numbers

Pixel-art popups for hits, crits, damage-over-time ticks, heals and bounties, using the styles in `../Fonts`.

## Set up (once per scene)

**Hold the Hill > Sandbox > Add Damage Numbers To Scene.** This builds the font styles, adds a `Damage Numbers` object with every style assigned, and selects it. That's all: it listens to `EnemyHealth` automatically.

## What shows up

| Event | Popup |
|---|---|
| Hit | The number in its damage type's style (Physical, Magic, True, Fire, Poison, Lightning). It slams in, arcs up and fades. |
| `DamageInfo.IsCritical = true` | The Crit style with a "!" and a bigger slam. |
| Damage-over-time tick | Smaller, shorter, no slam. EnemyHealth sends ticks with the enemy itself as `Source`. |
| `EnemyHealth.Heal` | Pink `+N`. |
| Enemy dies | Gold `+bounty` above it. Turn off with *Show Bounty*. |

Numbers are abbreviated: `1.2K`, `16K`, `2.5M`.

## In the graybox scene

`GrayboxBuilder` adds the spawner itself (on `GrayboxCombatSystems`) at pixel scale 2, so rebuilding the scene keeps it. It replaced the plain-text `GrayboxDamageNumbers`, which is no longer added to the scene. The "BREACH!", "FROST SHATTER!" and "OVERCHARGE!" messages go through `DamageNumberSpawner.ShowText`.

- **Merging:** hits of the same kind on one enemy within 0.3 s add onto the popup already showing, so a fast tower shows one number counting up and not a pile. A popup stops absorbing after 1 s.
- **Lanes:** each damage kind spawns in its own lane beside the enemy (fire and poison furthest out), and the bounty starts higher, so different kinds don't cover each other.
- **Ticks** draw one pixel-scale step smaller than direct hits.

This folder is its own assembly, `HoldTheHill.Sandbox.DamageNumbers`. The Graybox assembly references it.

## From code

```csharp
DamageNumberSpawner.Show(42, DamageNumberKind.Fire, enemy.position);
DamageNumberSpawner.Show(25, DamageNumberKind.Resource, hill.position);   // "+25"
DamageNumberSpawner.ShowText("MISS", DamageNumberKind.Physical, enemy.position);
```

To make a hit a crit, set `info.IsCritical = true` on the `DamageInfo` before `TakeDamage`.

## Tuning (on the component)

- **Pixels Per Unit:** match your sprites so a font pixel equals an art pixel. Default 32.
- **Pixel Scale:** 2 or 3 if the numbers read too small.
- **Motion:** one block each for hits, crits, ticks, heals and rewards. Each sets lifetime, rise speed, gravity, sideways scatter, fade start, slam scale and slam duration.
- **Max Active:** past this many popups, the oldest one is recycled.
- **Sorting Layer / Order:** draw above enemies and towers.

Popups are pooled meshes, one per number, with no per-hit allocation once the pool is warm.
