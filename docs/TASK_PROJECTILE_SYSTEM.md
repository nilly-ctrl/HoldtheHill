# Task Hand-off: Tower Projectile & Combat System Suite

> **Branch:** Create `nilly/tower-projectile-system` off `Indev`.  
> **Status:** Architecture and design finalized. Ready for code implementation.  
> **Repo Root:** `B:\Projects\GameDev\Projects\hold-the-hill`  
> **Unity Project:** `Hold the Hill/` (inside repo root, quote paths due to space).  
> **Assembly:** `Hold the Hill/Assets/_Game/HoldTheHill.Runtime.asmdef` (namespace root: `HoldTheHill`).  

---

## ⚠️ Non-Negotiable Rules for Claude

1. **NEVER commit, push, or merge to `main` or `Indev`.** Both are shared. All work belongs strictly on `nilly/tower-projectile-system`.
2. **Git Identity:** Commits must use `nilly-ctrl` and `322642514+nilly-ctrl@users.noreply.github.com` (already set locally).
3. **Unity Version:** Locked to **`6000.6.0f1`**. Never modify `ProjectSettings/ProjectVersion.txt`.
4. **AI Devlog:** Every AI-assisted change must be logged as a row in `docs/AI_DEVLOG.md` (Date, Who, Tool, What the AI helped with, What we decided, Not tested yet) with a matching `Co-Authored-By: Claude ...` in the commit.
5. **No `Resources/` folders.** Meta files must always be committed alongside new assets.

---

## Task Scope & Architecture

Implement a complete, modular projectile and combat system for towers and units in `Hold the Hill/Assets/_Game/`.

### 1. Core Combat Contracts (`Assets/_Game/Features/Combat/Scripts/`)

* **`IDamageable.cs`** (namespace `HoldTheHill.Features.Combat`):
  * `void TakeDamage(DamageInfo info);`
  * `void ApplyStatusEffect(StatusEffectData status);`
  * `bool IsDead { get; }`
  * `Transform Transform { get; }`
* **`DamageInfo.cs`**:
  * `float Amount;`
  * `GameObject Source;`
  * `Vector2 HitPoint;`
  * `DamageType Type;` (Enum: `Physical`, `Magic`, `True`, `Fire`, `Poison`, `Lightning`)
* **`StatusEffectData.cs`**:
  * Serializable struct for DoTs: `string EffectName; float DamagePerTick; float TickInterval; float Duration; float SlowMultiplier;`
* **`GroundHazard.cs`**:
  * Persistent zone prefab spawned on impact (e.g. fire puddle, acid pool).
  * Has a 2D trigger collider, deals periodic tick damage, lasts for `_duration` seconds, then despawns/recycles.

### 2. Core Projectile & Flight Physics (`Assets/_Game/Features/Combat/Scripts/`)

* **`Projectile.cs`**:
  * **Enum `FlightMode`**:
    * `Linear`: Straight trajectory toward target/direction.
    * `Homing`: Steering physics via `_turnRate` (degrees/sec) using `Vector3.RotateTowards`. If the target dodges or passes, the projectile overshoots and **loops back around**. If the target dies, it scans for the next valid `IDamageable`.
    * `Lobbed`: 2D parabolic arc simulating height and gravity to landing point.
    * `Instant`: Instant hitscan strike.
    * `SeekAndDestroy`: Loiters in air/hover for `_loiterDuration`, acquires target, then dive-bombs into it at high speed (2.5×).
  * **Piercing**: `bool _isPiercing`, `int _maxPierceHits`, with internal hit-cooldown per target.
  * **Impact & Damage**: Direct hit damage, optional AoE splash (`_splashRadius`), status effect infliction, and ground hazard spawning.
  * **Pooling Lifecycle**: Supports `ProjectilePool.Release(this)` with fallback to `Destroy(gameObject)`.
* **`ProjectilePool.cs`**:
  * Wrapper around Unity's `UnityEngine.Pool.ObjectPool<Projectile>` to eliminate GC stutter during intense waves.

### 3. Special Weapon Mechanics (`Assets/_Game/Features/Combat/Scripts/`)

* **`ClusterProjectile.cs`**: Subclasses `Projectile`. Detonates on impact or apex, scattering $N$ child projectiles in an arc or radius.
* **`RicochetProjectile.cs`**: Subclasses `Projectile`. Bounces between up to $K$ nearby enemies within range upon impact.
* **`ChainLightning.cs`**: Instant multi-target electric arc between enemies using `LineRenderer` with configurable damage falloff per jump.
* **`ContinuousBeam.cs`**: Continuous laser/flamethrower beam via `LineRenderer` with ramping damage while focused on a single target.
* **`OrbitingDamageField.cs`**: Spawns $N$ orbiting wisps/blades/dots circling the tower in custom paths (circular, elliptical, expanding), dealing contact damage and applying DoTs to colliding enemies.

### 4. Tower Integration (`Assets/_Game/Features/Towers/Scripts/`)

* **`TargetingPriority.cs`** (namespace `HoldTheHill.Features.Towers`):
  * Enum: `First` (furthest on `EnemyPath`), `Last`, `Closest`, `Strongest` (highest HP), `Weakest` (lowest HP).
* **`Tower.cs`** (update existing):
  * Adds target acquisition using `TargetingPriority`.
  * Cooldown timer and firing logic dispatching assigned `_projectilePrefab`, `_continuousBeam`, or `_orbitingField`.
  * Visual range gizmos.

### 5. Enemy Integration (`Assets/_Game/Features/Enemies/Scripts/`)

* **`EnemyHealth.cs`** (namespace `HoldTheHill.Features.Enemies`):
  * Implements `IDamageable`.
  * Tracks HP, processes instant damage, and manages active DoT coroutines.
  * On death: triggers cleanup and calls `EnemySpawner.NotifyEnemyDefeated(gameObject)` if present.

---

## Step-by-Step Implementation Sequence

1. `git checkout -b nilly/tower-projectile-system Indev`
2. Create `IDamageable.cs`, `DamageInfo.cs`, `StatusEffectData.cs`, and `GroundHazard.cs` in `Hold the Hill/Assets/_Game/Features/Combat/Scripts/`.
3. Create `ProjectilePool.cs` and `Projectile.cs` in `Features/Combat/Scripts/`.
4. Create special weapons: `ClusterProjectile.cs`, `RicochetProjectile.cs`, `ChainLightning.cs`, `ContinuousBeam.cs`, `OrbitingDamageField.cs`.
5. Create `TargetingPriority.cs` and update `Tower.cs` in `Hold the Hill/Assets/_Game/Features/Towers/Scripts/`.
6. Create `EnemyHealth.cs` in `Hold the Hill/Assets/_Game/Features/Enemies/Scripts/`.
7. Verify all files compile cleanly under `HoldTheHill.Runtime.asmdef` (zero errors/warnings).
8. Add an entry to `docs/AI_DEVLOG.md`.
9. Commit changes to `nilly/tower-projectile-system` with proper co-author attribution.
