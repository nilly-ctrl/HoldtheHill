# Fork documentation index

The fork's technical documents, one per system, in the same format as the team's documents in the
folder above. Where the team's folder has a document for the same system, the two are meant to be
read side by side; each fork document ends with what differs. Nothing here replaces a team document
until the team has chosen whose code stays; the decision column is blank until then.

| Document | Covers | Team counterpart | Decision |
| :--- | :--- | :--- | :--- |
| `EnemyScripts_Documentation.md` | `EnemyHealth`, `EnemyMover`, `EnemyShield`, `EnemyHealer`, `EnemySplitter`, `EnemyPath` | `../EnemyScripts_Documentation.md` (BasicEnemy) | |
| `EnemySpawner_Documentation.md` | `EnemySpawner`, `MapWaveLookup` | `../EnemySpawner_Documentation.md` | |
| `MapWaveData_Documentation.md` | `EnemySpawnEntry`, `Wave`, `MapWaveData`, `MapWaveDataSO` | `../MapWaveData_Documentation.md` | |
| `Combat_Documentation.md` | `IDamageable`, `IHealthReadable`, `IOwned`, `DamageInfo`, `StatusEffectData`, `CombatUtil` | none | |
| `Towers_Documentation.md` | `Tower`, `TargetingPriority`, `TowerCredit`, `TowerWeapon` and the weapon catalogue, `Projectile`, `ProjectileFlight`, `ProjectilePool` | none for the weapons; `Tower.cs` on Indev was a range-and-cell stub | |

Template for new ones: `_TEMPLATE_Documentation.md`. Plan: `../../docs/DOCUMENTATION_PLAN.md`.
