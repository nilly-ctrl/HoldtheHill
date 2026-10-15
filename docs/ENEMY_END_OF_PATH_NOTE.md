# What happens when an enemy runs out of path

Written 2026-10-14 while documenting `EnemyMover` and `EnemyHealth`. Two things did not match
what the Inspector promises; this note records the decision and the change.

## 1. A pooled enemy that walks in was destroyed, not returned

`EnemyHealth` has **Deactivate Instead Of Destroy** so a pool can reuse the object after death.
`EnemyMover`'s `Despawn` end behaviour ignored it and always called `Destroy(gameObject)`, so a
pooled enemy that breached was gone for good while one that died came back. The flag read as if
it covered both.

**Decision:** one component decides how an enemy leaves the scene, and that is `EnemyHealth`.
Its `Despawn()` is now public: it deactivates or destroys per the flag, raises no `Died` or
`Defeated` (a breach is not a kill and pays no bounty), and runs at once (the despawn delay is
for the death animation only). The mover calls it when its end behaviour is `Despawn`, and only
falls back to `Destroy` when the object has no `EnemyHealth`.

## 2. `Loop` reported a breach every lap

`ReachedEnd` (static) and `Arrived` (instance) fired for every end behaviour, `Loop` included.
`Loop` exists for endless test scenes; nothing arrives anywhere, the enemy just starts over. Yet
every lap the spawner dropped it from `LivingEnemyCount` (and kept it dropped while it walked),
the Queen read its breach damage, and the graybox animator played a hill hit. Each listener had
to know about `DespawnsAtEnd` to protect itself, and only some did.

**Decision:** a lap is not an arrival. `Loop` raises neither event and just snaps back to the
start. `Stop` and `Despawn` are unchanged: both still raise `Arrived` then `ReachedEnd`, and
listeners still use `DespawnsAtEnd` to tell "stopped at the hill" (the thief ant, which walks
home again) from "walked in". `HasFinished` stays false while looping, as before.

## What is not changed

- `Stop` still announces itself. The thief ant's `OnReachedHill` depends on `Arrived`, and the
  spawner and Queen already ignore an enemy that stops.
- No pool exists yet (`Core/Pooling` is empty). The flag now behaves the same on both exits, so a
  pool can be added without touching the mover.

Tests: `_Game/Tests/PlayMode/EnemyEndOfPathTests.cs`.
