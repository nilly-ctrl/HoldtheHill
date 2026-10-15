# BasicEnemy (team `Indev`) against the fork's enemy (EnemyHealth + EnemyMover)

Written 2026-10-15 from `upstream/Indev` (`127d56bf`) and the fork branch. Jason's `BasicEnemy` is
one class of 292 lines; the fork splits the same job into `EnemyHealth` (304 lines) and
`EnemyMover` (278 lines), with `EnemyShield`, `EnemyHealer` and `EnemySplitter` as optional
extras on the same object.

## Side by side

| | `BasicEnemy` (Indev) | Fork: `EnemyHealth` + `EnemyMover` |
|---|---|---|
| Shape | One MonoBehaviour: health, hill damage, movement, facing, gizmos | Two components with one job each, plus optional ones; towers talk to `IDamageable`, never to a concrete enemy class |
| Health type | `double`, no lower bound on the field | `float` with `[Min(1)]`; `HealthFraction` for bars |
| Taking damage | `TakeDamage(double)`: ignores zero or less, clamps at 0, raises `OnHealthChanged`, dies | `TakeDamage(DamageInfo)`: amount, source, hit point and damage type; shields and modifiers can intercept (`IIncomingDamageModifier`); raises `Hurt` and the static `Damaged` so towers can be credited |
| Healing | `Heal(double)` | `Heal(float)` plus the static `Healed`; `EnemyHealer` heals neighbours |
| Status effects | none | `ApplyStatusEffect(StatusEffectData)`: slows through `SpeedMultiplier`, ticks, and `StatusApplied` for visuals |
| Reward | none | `BountyValue`, paid by the wallet on `Defeated` |
| Damage to the hill | `Damage` (int, public setter), meant for "upon reaching destination", but nothing on Indev reads it | `BreachDamage` (float, serialized); `Queen` reads it when `EnemyMover.ReachedEnd` fires |
| Death | `Die()`: `IsDead`, `OnDied`, `Destroy(gameObject)` at once | `IsDead`, `Died`, static `Defeated`; despawn after `_despawnDelay`, or deactivate instead of destroy for a pool |
| Path | Copies waypoint positions into its own `_pathCoordinates` list (`SetPath` has three overloads); finds an `EnemyPath` by search in `Start` if the list is empty | Holds a reference to the `EnemyPath` (serialized, or found once); never copies the route |
| Walking | `Update`, `MoveTowards`, tolerance 0.05, z kept | `Update` along the path with `DistanceTravelled`, `SpeedScale`, knockback (`Knockback`, `IgnoresKnockback`), `SnapToStart`, `SkipToLastWaypoint`, `UpdatePositionToMatchDistance` |
| End of path | `OnDestinationReached` (instance), `OnAnyDestinationReached` (static, never reset), then `Destroy` | `EndBehaviour` (despawn, stop, ...), instance `Arrived`, static `ReachedEnd`; statics reset on `SubsystemRegistration` |
| Facing | flips the sprite on X by default | `_faceTravelDirection`, off by default (the animator decides) |
| Targeting support | none; towers would need to compare positions | `DistanceTravelled` gives First/Last targeting its number |
| Pooling | not possible (destroys itself) | ready (`_deactivateInsteadOfDestroy`) |
| Extension | `protected virtual` on most methods, for subclasses like `SilverfishEnemy` | composition: add a component (shield, healer, splitter, special behaviours) |
| Tests | 5 EditMode tests; 2 fail on Indev regardless (health read before `Awake` in edit mode, `Destroy` in edit mode) | PlayMode combat tests (8) and the enemy, mechanics and specials suites; `QueenTests` cover the breach side |

## What the fork keeps from Jason's work

- `EnemyPath` is his, extended with route maths (length, distance along the route) rather than
  replaced.
- `Triangle.prefab` keeps its GUID and his numbers (1 health, 1 damage on arrival, speed 2), rebuilt
  on the two components, so `Map_01` still spawns it.
- The shape of the events (instance plus a static "any" event for decoupled listeners) is the
  same idea, kept with the reset the static was missing.
- `Heal` and the dead-enemy guard on damage.

## What changes for anyone who used `BasicEnemy`

- `double` health is `float`. His first test asserted the type itself; that assertion has no
  equivalent.
- `Damage` (int, settable) is `BreachDamage` (float, serialized, read-only at run time).
- `SetPath(...)` and `PathCoordinates` are gone: an enemy walks the path it references, and
  `SnapToStart` puts it on the first waypoint.
- `OnDestinationReached` / `OnAnyDestinationReached` are `EnemyMover.Arrived` /
  `EnemyMover.ReachedEnd`; `OnDied` is `EnemyHealth.Died` / `Defeated`.
- Subclassing is not the way to make a new enemy; adding components and a data asset is
  (`EnemyShield`, `EnemyHealer`, the specials). `SilverfishEnemy` was the only subclass and had no
  references.

## Why the split, in one paragraph

`BasicEnemy` mixes three things that change for different reasons: how an enemy takes damage,
how it walks, and what it looks like. Every tower feature the prototype grew (slows, shields,
bounties, breach damage, knockback, First/Last targeting, pooling) needed a field on the enemy,
and in one class that means one file growing and every enemy carrying everything. Two components
with contracts let a tower hit anything `IDamageable`, let a shield sit in front of the health
without the health knowing, and let the mover answer "how far along" without the health caring.
