using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Towers;
using HoldTheHill.Sandbox.NillyCtrl;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HoldTheHill.Sandbox.Graybox.Tests
{
    /// <summary>
    /// Checks the graybox animation hookup reacts to what the game does: towers attack and
    /// upgrade, enemies flinch, die into a corpse, lose shields and heal, mines blow up.
    /// Also covers EnemySplitter, whose split the Splitter's death clip depends on.
    /// </summary>
    /// <remarks>
    /// Uses a small library of throwaway sprites built in code, so the tests don't depend on
    /// the generated art or the graybox scene.
    /// </remarks>
    public class GrayboxAnimationTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        private const float FrameTime = 0.05f;

        private readonly List<Object> _spawned = new List<Object>();
        private SpriteAnimLibrary _library;

        [SetUp]
        public void SetUp()
        {
            _library = ScriptableObject.CreateInstance<SpriteAnimLibrary>();
            var texture = new Texture2D(4, 4);
            _spawned.Add(texture);
            _spawned.Add(_library);

            SpriteAnimClip Clip(string name, int frames, bool loop) => new SpriteAnimClip
            {
                Name = name,
                Frames = Enumerable.Range(0, frames)
                    .Select(_ => Sprite.Create(texture, new Rect(0, 0, 4, 4), Vector2.one * 0.5f, 4))
                    .ToArray(),
                Durations = Enumerable.Repeat(FrameTime, frames).ToArray(),
                Loop = loop,
            };

            void Set(string key, params SpriteAnimClip[] clips) =>
                _library.Sets.Add(new SpriteAnimSet { Key = key, Clips = clips.ToList() });

            Set("TestTower", Clip("Idle", 4, true), Clip("Attack", 6, false), Clip("Upgrade", 6, false));
            Set("TestEnemy", Clip("Walk", 6, true), Clip("Hurt", 2, false), Clip("Death", 7, false),
                Clip("ShieldHit", 3, false), Clip("ShieldBreak", 6, false), Clip("WalkBare", 6, true), Clip("Heal", 6, false));
            Set("FxFrostPulse", Clip("Pulse", 8, false));
            Set("FxShockwave", Clip("Blast", 7, false));
            Set("FxHealPulse", Clip("Pulse", 7, false));
            Set("FxMine", Clip("Armed", 2, true), Clip("Explode", 7, false));
            Set("FxStatus", Clip("Slow", 4, true), Clip("Burn", 4, true), Clip("Poison", 4, true));
            Set("FxImpact", Clip("Acid", 5, false), Clip("Spark", 4, false), Clip("Pop", 4, false),
                Clip("Zap", 4, false), Clip("Sizzle", 3, false), Clip("Scratch", 3, false));
            Set("FxBlast", Clip("Blast", 6, false));
            Set("FxTier", Clip("Tier2", 4, true), Clip("Tier3", 4, true));
            Set("ProjLinear", Clip("Fly", 4, true));
            Set("ProjMortar", Clip("Fly", 4, true));
            Set("TowerLinear", Clip("Idle", 4, true), Clip("Attack", 6, false), Clip("Upgrade", 6, false));
            Set("FxTowerLinear", Clip("Trail", 4, false), Clip("Miss", 4, false), Clip("Muzzle", 3, false),
                Clip("Charge", 6, false), Clip("Bounce", 4, false));
            Set("FxTowerFrostAura", Clip("Freeze", 4, true));
            Set("FxStatusOverlay", Clip("Burn", 4, true), Clip("Poison", 4, true), Clip("Web", 2, true), Clip("Honey", 4, true));
            Set("FxMelee", Clip("Bite", 3, false), Clip("Slash", 4, false), Clip("Slam", 5, false), Clip("Block", 4, false),
                Clip("Heal", 6, false));
            Set("FxHillHit", Clip("Hit", 5, false), Clip("HitHeavy", 6, false), Clip("Breach", 6, false));
            Set("EnemyBrute", Clip("Walk", 6, true), Clip("Hurt", 2, false), Clip("Death", 7, false),
                Clip("Attack", 6, false), Clip("Spawn", 6, false));
            Set("WpnAcidSpit", Clip("Fly", 4, true), Clip("Trail", 4, false), Clip("Hit", 5, false), Clip("Miss", 4, false),
                Clip("Muzzle", 3, false), Clip("Fly2", 4, true), Clip("Hit2", 5, false));
            Set("WpnCannon", Clip("Fly", 4, true), Clip("Trail", 4, false), Clip("Hit", 5, false), Clip("Miss", 4, false),
                Clip("Muzzle", 3, false), Clip("Charge", 6, false));
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _spawned)
            {
                if (o != null)
                {
                    Object.DestroyImmediate(o);
                }
            }

            _spawned.Clear();

            // Corpses and effects are spawned by the code under test, not tracked above.
            foreach (SpriteClipPlayer player in Object.FindObjectsByType<SpriteClipPlayer>())
            {
                Object.DestroyImmediate(player.gameObject);
            }
        }

        // ---------- the player itself ----------

        [UnityTest]
        public IEnumerator Player_OneShotFallsBackToTheDefaultClip()
        {
            GameObject go = Track(new GameObject("Player", typeof(SpriteRenderer)));
            var player = go.AddComponent<SpriteClipPlayer>();
            player.Configure(_library, "TestTower", "Idle");
            yield return null;

            Assert.AreEqual("Idle", player.CurrentClip);
            player.Play("Attack");
            Assert.AreEqual("Attack", player.CurrentClip);

            yield return new WaitForSeconds(6 * FrameTime + 0.15f);
            Assert.AreEqual("Idle", player.CurrentClip, "A one-shot clip should hand back to the default clip.");
        }

        [UnityTest]
        public IEnumerator Player_SpawnedOneShotRemovesItselfWhenDone()
        {
            SpriteClipPlayer fx = SpriteClipPlayer.SpawnOneShot(_library, "FxMine", "Explode", Vector3.zero, Quaternion.identity, 1f, 3);
            Assert.IsNotNull(fx);
            yield return new WaitForSeconds(7 * FrameTime + 0.2f);
            Assert.IsTrue(fx == null, "A spawned effect should destroy itself after its clip.");
        }

        [Test]
        public void Attach_KeepsObjectsAtTheirRealSize()
        {
            // The placeholders are Sliced sprites; switching them to Simple once silently scaled
            // towers 4x and enemies 3x, colliders included.
            foreach (string kind in new[] { "tower", "enemy", "mine" })
            {
                GameObject go = Track(new GameObject(kind));
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = Sprite.Create(new Texture2D(4, 4), new Rect(0, 0, 4, 4), Vector2.one * 0.5f, 20);
                renderer.drawMode = SpriteDrawMode.Sliced;
                renderer.size = new Vector2(0.8f, 0.8f);
                go.AddComponent<CircleCollider2D>();

                if (kind == "tower") { go.AddComponent<Tower>(); GrayboxTowerAnimator.Attach(go, _library, "TestTower"); }
                else if (kind == "enemy") { go.AddComponent<EnemyHealth>(); GrayboxEnemyAnimator.Attach(go, _library, "TestEnemy"); }
                else { go.AddComponent<ProximityMine>(); GrayboxMineAnimator.Attach(go, _library); }

                Assert.AreEqual(Vector3.one, go.transform.localScale, $"{kind} was rescaled by Attach.");
            }
        }

        [Test]
        public void Attach_BeforeTheGameplayComponentDoesNotAutoAddADefaultOne()
        {
            // The builder attaches animation first and adds EnemyHealth / ProximityMine after.
            // A [RequireComponent] on the animation scripts once auto-added a default copy, so
            // every enemy had two EnemyHealths and ran on the default 30 HP.
            GameObject enemy = Track(new GameObject("enemy", typeof(SpriteRenderer)));
            GrayboxEnemyAnimator.Attach(enemy, _library, "TestEnemy");
            Assert.IsNull(enemy.GetComponent<EnemyHealth>(), "Attach added an EnemyHealth of its own.");

            GameObject mine = Track(new GameObject("mine", typeof(SpriteRenderer)));
            GrayboxMineAnimator.Attach(mine, _library);
            Assert.IsNull(mine.GetComponent<ProximityMine>(), "Attach added a ProximityMine of its own.");
        }

        // ---------- towers ----------

        [UnityTest]
        public IEnumerator Tower_PlaysAttackWhenItFires()
        {
            MakeEnemy(new Vector2(1f, 0f), 1000f);
            yield return null;

            Tower tower = MakeTower(Vector2.zero);
            SpriteClipPlayer player = tower.GetComponent<SpriteClipPlayer>();

            yield return WaitForClip(player, "Attack", 1f);
            Assert.Greater(tower.ShotsFired, 0);
            Assert.AreEqual("Attack", player.CurrentClip, "The tower fired but never played Attack.");
        }

        [UnityTest]
        public IEnumerator Tower_PlaysUpgradeWhenUpgraded()
        {
            Tower tower = MakeTower(Vector2.zero);
            yield return null;

            Assert.IsTrue(tower.Upgrade());
            Assert.AreEqual("Upgrade", tower.GetComponent<SpriteClipPlayer>().CurrentClip);
        }

        [UnityTest]
        public IEnumerator FrostTower_PulseAttacksAndDropsARing()
        {
            MakeEnemy(new Vector2(1f, 0f), 1000f);
            yield return null;

            Tower tower = MakeTower(Vector2.zero, addBeforeAnimator: go => go.AddComponent<FrostAuraTower>());
            SpriteClipPlayer player = tower.GetComponent<SpriteClipPlayer>();

            yield return WaitForClip(player, "Attack", 1f);
            Assert.AreEqual("Attack", player.CurrentClip, "The frost pulse never played Attack.");
            Assert.IsNotNull(GameObject.Find("FxFrostPulse Pulse"), "The frost pulse left no ring effect.");
        }

        // ---------- tower extras ----------

        [UnityTest]
        public IEnumerator TowerExtras_MuzzlePuffPointsAtTheTarget()
        {
            MakeEnemy(new Vector2(0f, 1f), 1000f);
            yield return null;

            Tower tower = MakeTower(Vector2.zero, key: "TowerLinear");
            yield return WaitForClip(tower.GetComponent<SpriteClipPlayer>(), "Attack", 1f);

            var extras = tower.GetComponent<GrayboxTowerExtras>();
            Assert.IsNotNull(extras, "The animator should add the extras to a tower by itself.");
            Assert.AreEqual("FxTowerLinear", extras.Key);

            GameObject puff = GameObject.Find("FxTowerLinear Muzzle");
            Assert.IsNotNull(puff, "The tower fired but left no muzzle puff.");
            Assert.Greater(puff.transform.position.y, 0.3f, "The puff should sit toward the target, above the tower.");
            Assert.AreEqual(90f, puff.transform.eulerAngles.z, 0.5f, "The puff should be turned to face the target.");
        }

        [UnityTest]
        public IEnumerator TowerExtras_ATowerWithNoFxFileIsLeftAlone()
        {
            MakeEnemy(new Vector2(1f, 0f), 1000f);
            yield return null;

            Tower tower = MakeTower(Vector2.zero);
            yield return WaitForClip(tower.GetComponent<SpriteClipPlayer>(), "Attack", 1f);
            yield return null;

            var extras = tower.GetComponent<GrayboxTowerExtras>();
            Assert.IsNotNull(extras);
            Assert.IsNull(extras.Key, "TestTower has no FxTower set, so the extras should be off.");
            Assert.AreEqual(0, extras.Spawned);
        }

        [UnityTest]
        public IEnumerator TowerExtras_ShotLeavesATrailAndAMissWhereItHitsNothing()
        {
            Tower tower = MakeTower(Vector2.zero, key: "TowerLinear");
            yield return null;
            yield return null; // the extras subscribe in their own Start

            GameObject go = Track(new GameObject("Shot", typeof(SpriteRenderer)));
            var shot = go.AddComponent<Projectile>();
            SetPrivate(shot, "_lifetime", 0.4f);
            SetPrivate(shot, "_speed", 2f);
            shot.Owner = tower.gameObject;
            int trailsBefore = GrayboxShotTrail.TrailsSpawned;
            int missesBefore = GrayboxShotTrail.MissesSpawned;
            shot.Launch(Vector2.zero, null, Vector2.right);

            var trail = go.GetComponent<GrayboxShotTrail>();
            Assert.IsNotNull(trail, "Launching a shot owned by the tower should put a trail on it.");
            Assert.IsTrue(trail.InFlight);

            yield return new WaitForSeconds(0.25f);
            Assert.Greater(GrayboxShotTrail.TrailsSpawned, trailsBefore, "A flying shot should leave trail puffs.");
            Assert.AreEqual(missesBefore, GrayboxShotTrail.MissesSpawned, "No miss while the shot is still flying.");

            yield return new WaitForSeconds(0.3f); // past its lifetime: it expires having hit nothing
            Assert.AreEqual(missesBefore + 1, GrayboxShotTrail.MissesSpawned, "A shot that hit nothing should leave a Miss.");
        }

        [UnityTest]
        public IEnumerator TowerExtras_AShotThatHitsLeavesNoMiss()
        {
            Tower tower = MakeTower(new Vector2(-50f, 0f), key: "TowerLinear");
            EnemyHealth enemy = MakeEnemy(new Vector2(-49.2f, 0f), 1000f);
            yield return null;
            yield return null;

            GameObject go = Track(new GameObject("Shot", typeof(SpriteRenderer)));
            var shot = go.AddComponent<Projectile>();
            SetPrivate(shot, "_speed", 6f);
            shot.Owner = tower.gameObject;
            int missesBefore = GrayboxShotTrail.MissesSpawned;
            shot.Launch(new Vector2(-50f, 0f), enemy, Vector2.right);

            yield return new WaitForSeconds(0.5f);
            Assert.Less(enemy.CurrentHealth, 1000f, "The shot should have hit the enemy.");
            Assert.AreEqual(missesBefore, GrayboxShotTrail.MissesSpawned, "A shot that hit should not also leave a Miss.");
        }

        [UnityTest]
        public IEnumerator Enemy_WearsAnIceCrustOnlyWhileSlowed()
        {
            EnemyHealth enemy = MakeEnemy(Vector2.zero, 1000f, animate: true);
            var status = enemy.GetComponent<GrayboxStatusVisuals>();
            yield return null;
            Assert.IsFalse(status.CrustShown);

            enemy.ApplyStatusEffect(new StatusEffectData
            {
                EffectName = "FrostChill", DamagePerTick = 0f, TickInterval = 0.5f, Duration = 0.3f, SlowMultiplier = 0.5f,
            });
            yield return null;
            Assert.IsTrue(status.CrustShown, "A slowed enemy should wear the ice crust.");

            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(status.CrustShown, "The crust should go when the slow wears off.");
        }

        [UnityTest]
        public IEnumerator Enemy_WearsTheOverlayThatMatchesItsStatus()
        {
            EnemyHealth enemy = MakeEnemy(Vector2.zero, 1000f, animate: true);
            var status = enemy.GetComponent<GrayboxStatusVisuals>();
            yield return null;
            Assert.IsNull(status.OverlayClip);

            enemy.ApplyStatusEffect(new StatusEffectData
            {
                EffectName = "WebSnare", DamagePerTick = 0f, TickInterval = 0.5f, Duration = 2f, SlowMultiplier = 0.5f,
            });
            yield return null;
            Assert.AreEqual("Web", status.OverlayClip, "A slow named Web... should show the web, not the ice crust.");

            enemy.ApplyStatusEffect(new StatusEffectData
            {
                EffectName = "HoneyTrap", DamagePerTick = 0f, TickInterval = 0.5f, Duration = 2f, SlowMultiplier = 0.4f,
            });
            yield return null;
            Assert.AreEqual("Honey", status.OverlayClip, "A newer slow should change what the enemy wears.");

            enemy.TakeDamage(new DamageInfo(5f, null, Vector2.zero, DamageType.Fire));
            yield return null;
            Assert.AreEqual("Burn", status.OverlayClip, "Fire should show flames over any slow.");
        }

        [UnityTest]
        public IEnumerator Enemy_PlaysSpawnWhenItAppears()
        {
            EnemyHealth enemy = MakeEnemy(Vector2.zero, 1000f, animate: true, key: "EnemyBrute");
            yield return null;
            Assert.AreEqual("Spawn", enemy.GetComponent<SpriteClipPlayer>().CurrentClip);

            yield return WaitForClip(enemy.GetComponent<SpriteClipPlayer>(), "Walk", 1f);
            Assert.AreEqual("Walk", enemy.GetComponent<SpriteClipPlayer>().CurrentClip, "Spawn should hand over to Walk.");
        }

        [UnityTest]
        public IEnumerator Enemy_ReachingTheHillLeavesAnAttackAndAHillHit()
        {
            EnemyHealth enemy = MakeEnemy(new Vector2(3f, 1f), 1000f, animate: true, key: "EnemyBrute");
            yield return null;

            // EnemyMover raises this just before it removes an enemy at the end of the trail.
            var raised = (System.Delegate)typeof(EnemyMover)
                .GetField("ReachedEnd", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            Assert.IsNotNull(raised, "Nothing is listening for enemies reaching the end.");
            raised.DynamicInvoke(enemy.gameObject);

            Assert.IsNotNull(GameObject.Find("EnemyBrute Attack"), "The enemy should be shown biting the hill.");
            GameObject hit = GameObject.Find("FxHillHit HitHeavy");
            Assert.IsNotNull(hit, "A brute reaching the hill should leave the heavy hit.");
            Assert.AreEqual(1.9f, hit.transform.position.x, 0.01f, "The hit belongs on the hill's rim, back along the enemy's facing.");
            Assert.AreEqual(1f, hit.transform.position.y, 0.01f);
            Assert.AreEqual("Breach", GrayboxEnemyAnimator.HillHitFor("EnemySwarm"));
            Assert.AreEqual("Hit", GrayboxEnemyAnimator.HillHitFor("EnemyGrunt"));
        }

        // ---------- melee castes ----------

        [UnityTest]
        public IEnumerator Melee_WorkerBitesItsTarget()
        {
            EnemyHealth enemy = MakeEnemy(new Vector2(1f, 0f), 1000f);
            yield return null;

            Tower tower = MakeMelee(GrayboxMeleeTower.Caste.Worker);
            yield return new WaitForSeconds(0.3f);

            Assert.Greater(tower.GetComponent<GrayboxMeleeTower>().Strikes, 0, "The worker never struck.");
            Assert.Less(enemy.CurrentHealth, 1000f, "The worker's bite should hurt its target.");
            Assert.Greater(tower.TotalDamageDealt, 0f, "The damage should be credited to the worker.");
        }

        [UnityTest]
        public IEnumerator Melee_MajorSlamHitsEverythingInRange()
        {
            EnemyHealth near = MakeEnemy(new Vector2(1f, 0f), 1000f);
            EnemyHealth alsoNear = MakeEnemy(new Vector2(-1f, 0.5f), 1000f);
            EnemyHealth far = MakeEnemy(new Vector2(6f, 0f), 1000f);
            yield return null;

            Tower tower = MakeMelee(GrayboxMeleeTower.Caste.Major);
            var major = tower.GetComponent<GrayboxMeleeTower>();
            for (float end = Time.time + 1f; major.Strikes == 0 && Time.time < end;)
            {
                yield return null; // the slam effect only lasts a quarter second, so look as soon as it lands
            }

            Assert.Less(near.CurrentHealth, 1000f);
            Assert.Less(alsoNear.CurrentHealth, 1000f, "The slam should hit every enemy in range, not just the target.");
            Assert.AreEqual(1000f, far.CurrentHealth, "The slam should not reach an enemy out of range.");
            Assert.IsNotNull(GameObject.Find("FxMelee Slam"));
        }

        [UnityTest]
        public IEnumerator Melee_NurseMendsAHurtHillAndLeavesEnemiesAlone()
        {
            GameObject hillObject = Track(new GameObject("Hill"));
            var hill = hillObject.AddComponent<GrayboxBaseHealth>();
            EnemyHealth enemy = MakeEnemy(new Vector2(0.5f, 0f), 1000f);
            yield return null;
            hill.TakeBaseDamage(20f, Vector3.zero);
            float hurt = hill.CurrentHealth;

            Tower tower = MakeMelee(GrayboxMeleeTower.Caste.Nurse, nurse => SetPrivate(nurse, "_healInterval", 0.1f));
            yield return new WaitForSeconds(0.4f);

            Assert.Greater(hill.CurrentHealth, hurt, "The nurse should mend a hurt hill.");
            Assert.LessOrEqual(hill.CurrentHealth, hill.MaxHealth);
            Assert.AreEqual(1000f, enemy.CurrentHealth, "The nurse does not fight.");
            Assert.IsNotNull(tower);
        }

        // ---------- weapon picker ----------

        [UnityTest]
        public IEnumerator WeaponPicker_ListsTheWeaponsAndPlaysEachStepOfAShot()
        {
            GameObject go = Track(new GameObject("Picker"));
            go.SetActive(false);
            var picker = go.AddComponent<GrayboxWeaponPicker>();
            picker.Configure(_library);
            SetPrivate(picker, "_shotSpeed", 30f); // short flights, so three shots fit in the test
            go.SetActive(true);
            yield return null;

            CollectionAssert.AreEqual(new[] { "WpnAcidSpit", "WpnCannon" }, picker.Weapons.ToArray());
            Assert.AreEqual("WpnAcidSpit", picker.Current);
            Assert.IsNotNull(GameObject.Find("WpnAcidSpit Muzzle"), "The first shot should open with a muzzle puff.");

            var steps = new HashSet<string>();
            float end = Time.time + 5f;
            while (Time.time < end && !(steps.Contains("Hit") && steps.Contains("Miss")))
            {
                steps.Add(picker.Step);
                yield return null;
            }

            CollectionAssert.IsSubsetOf(new[] { "Fly", "Hit", "Miss" }, steps,
                "A shot should fly and then land; every third shot misses.");

            picker.SetTier(2);
            picker.Select(1);
            Assert.AreEqual("WpnCannon", picker.Current);
            yield return null;
            Assert.AreEqual("Charge", picker.Step, "The cannon winds up before it fires.");

            Time.timeScale = 1f;
        }

        // ---------- enemies ----------

        [UnityTest]
        public IEnumerator Enemy_FlinchesWhenHitAndLeavesACorpseWhenKilled()
        {
            EnemyHealth enemy = MakeEnemy(Vector2.zero, 100f, animate: true);
            SpriteClipPlayer player = enemy.GetComponent<SpriteClipPlayer>();
            yield return null;

            Assert.AreEqual("Walk", player.CurrentClip);
            enemy.TakeDamage(new DamageInfo(10f, null, Vector2.zero, DamageType.Physical));
            Assert.AreEqual("Hurt", player.CurrentClip);

            enemy.TakeDamage(new DamageInfo(500f, null, Vector2.zero, DamageType.Physical));
            yield return null;

            Assert.IsTrue(enemy == null, "The enemy should be destroyed on death.");
            GameObject corpse = GameObject.Find("TestEnemy Death");
            Assert.IsNotNull(corpse, "Dying should leave a corpse playing Death.");
            Assert.AreEqual("Death", corpse.GetComponent<SpriteClipPlayer>().CurrentClip);
        }

        [UnityTest]
        public IEnumerator ShieldedEnemy_FlashesOnHitThenBreaksAndWalksBare()
        {
            EnemyHealth enemy = MakeEnemy(Vector2.zero, 100f, animate: true,
                addBeforeAnimator: go => SetPrivate(go.AddComponent<EnemyShield>(), "_maxShield", 50f));
            SpriteClipPlayer player = enemy.GetComponent<SpriteClipPlayer>();
            yield return null;

            enemy.TakeDamage(new DamageInfo(10f, null, Vector2.zero, DamageType.Physical));
            yield return null;
            Assert.AreEqual("ShieldHit", player.CurrentClip, "A hit the shield absorbed should flash the shield.");

            enemy.TakeDamage(new DamageInfo(60f, null, Vector2.zero, DamageType.Physical));
            yield return null;
            Assert.AreEqual("ShieldBreak", player.CurrentClip, "Emptying the shield should play ShieldBreak.");

            yield return new WaitForSeconds(6 * FrameTime + 0.15f);
            Assert.AreEqual("WalkBare", player.CurrentClip, "After the break the enemy should walk without its bubble.");
        }

        [UnityTest]
        public IEnumerator Healer_PlaysHealAndDropsARingOnEachPulse()
        {
            EnemyHealth healer = MakeEnemy(Vector2.zero, 100f, animate: true,
                addBeforeAnimator: go => go.AddComponent<EnemyHealer>());
            SpriteClipPlayer player = healer.GetComponent<SpriteClipPlayer>();

            yield return WaitForClip(player, "Heal", 1f);
            Assert.AreEqual("Heal", player.CurrentClip);
            Assert.IsNotNull(GameObject.Find("FxHealPulse Pulse"), "The heal pulse left no ring effect.");
        }

        [UnityTest]
        public IEnumerator Enemy_ShowsSlowThenBurnStatus()
        {
            EnemyHealth enemy = MakeEnemy(Vector2.zero, 1000f, animate: true);
            var status = enemy.GetComponent<GrayboxStatusVisuals>();
            var renderer = enemy.GetComponent<SpriteRenderer>();
            yield return null;
            Assert.IsNull(status.ActiveStatus);

            enemy.ApplyStatusEffect(new StatusEffectData
            {
                EffectName = "FrostChill", DamagePerTick = 0f, TickInterval = 0.5f, Duration = 2f, SlowMultiplier = 0.5f,
            });
            yield return null;
            Assert.AreEqual("Slow", status.ActiveStatus);
            Assert.AreNotEqual(Color.white, renderer.color, "A slowed enemy should be tinted.");
            Assert.IsNotNull(GameObject.Find("Enemy Status"), "A slowed enemy should show an icon.");

            enemy.TakeDamage(new DamageInfo(5f, null, Vector2.zero, DamageType.Fire));
            yield return null;
            Assert.AreEqual("Burn", status.ActiveStatus, "Fire damage should show Burn over Slow.");
        }

        [UnityTest]
        public IEnumerator Feedback_AnnouncesTowerShotsAndEnemyDeaths()
        {
            var attacked = new List<string>();
            var died = new List<string>();
            void OnAttack(string key, Vector3 at) => attacked.Add(key);
            void OnDied(string key, Vector3 at) => died.Add(key);
            GrayboxFeedback.TowerAttacked += OnAttack;
            GrayboxFeedback.EnemyDied += OnDied;
            try
            {
                EnemyHealth enemy = MakeEnemy(new Vector2(1f, 0f), 1000f, animate: true);
                yield return null;
                MakeTower(Vector2.zero);
                yield return new WaitForSeconds(0.2f);
                enemy.TakeDamage(new DamageInfo(5000f, null, Vector2.zero, DamageType.Physical));
                yield return null;

                CollectionAssert.Contains(attacked, "TestTower");
                CollectionAssert.AreEqual(new[] { "TestEnemy" }, died);
            }
            finally
            {
                GrayboxFeedback.TowerAttacked -= OnAttack;
                GrayboxFeedback.EnemyDied -= OnDied;
            }
        }

        [UnityTest]
        public IEnumerator Tower_ShowsATierOverlayAfterEachUpgrade()
        {
            Tower tower = MakeTower(Vector2.zero);
            var animator = tower.GetComponent<GrayboxTowerAnimator>();
            yield return null;
            Assert.IsNull(animator.TierClip, "A tier 1 tower should have no overlay.");

            tower.Upgrade();
            Assert.AreEqual("Tier2", animator.TierClip);
            tower.Upgrade();
            Assert.AreEqual("Tier3", animator.TierClip);
        }

        [UnityTest]
        public IEnumerator Impact_ProjectileHitLeavesItsEffectAtTheHitPoint()
        {
            GameObject fxHost = Track(new GameObject("ImpactFx"));
            var impacts = fxHost.AddComponent<GrayboxImpactFx>();
            impacts.Configure(_library);

            EnemyHealth enemy = MakeEnemy(Vector2.zero, 1000f);
            GameObject shot = Track(new GameObject("Shot", typeof(SpriteRenderer)));
            shot.AddComponent<SpriteClipPlayer>().Configure(_library, "ProjLinear", "Fly");
            yield return null;

            enemy.TakeDamage(new DamageInfo(5f, shot, new Vector2(0.25f, 0.5f), DamageType.Physical));

            GameObject splash = GameObject.Find("FxImpact Acid");
            Assert.IsNotNull(splash, "An acid glob hit should leave an acid splash.");
            Assert.AreEqual(new Vector3(0.25f, 0.5f, 0f), splash.transform.position);
        }

        [UnityTest]
        public IEnumerator Impact_SplashDamageLeavesOneBlastNotOnePerEnemy()
        {
            GameObject fxHost = Track(new GameObject("ImpactFx"));
            var impacts = fxHost.AddComponent<GrayboxImpactFx>();
            impacts.Configure(_library);

            EnemyHealth a = MakeEnemy(Vector2.zero, 1000f);
            EnemyHealth b = MakeEnemy(new Vector2(0.5f, 0f), 1000f);
            EnemyHealth c = MakeEnemy(new Vector2(1f, 0f), 1000f);
            GameObject shell = Track(new GameObject("Shell", typeof(SpriteRenderer)));
            shell.AddComponent<SpriteClipPlayer>().Configure(_library, "ProjMortar", "Fly");
            yield return null;

            foreach (EnemyHealth enemy in new[] { a, b, c })
            {
                enemy.TakeDamage(new DamageInfo(5f, shell, Vector2.zero, DamageType.Physical));
            }

            Assert.AreEqual(1, impacts.Spawned, "One shell should make one blast, however many enemies it hits.");

            // A damage-over-time tick is sourced from the enemy itself and should leave nothing.
            a.TakeDamage(new DamageInfo(1f, a.gameObject, Vector2.zero, DamageType.Poison));
            Assert.AreEqual(1, impacts.Spawned);
        }

        // ---------- mine ----------

        [UnityTest]
        public IEnumerator Mine_LeavesAnExplosionWhenItDetonates()
        {
            GameObject go = Track(new GameObject("Mine", typeof(SpriteRenderer)));
            go.SetActive(false);
            go.AddComponent<ProximityMine>();
            GrayboxMineAnimator.Attach(go, _library);
            go.SetActive(true);
            yield return null;

            Assert.AreEqual("Armed", go.GetComponent<SpriteClipPlayer>().CurrentClip);

            MakeEnemy(Vector2.zero, 1000f);
            float end = Time.time + 2f; // arm delay is 0.4 s
            while (go != null && Time.time < end)
            {
                yield return null;
            }

            Assert.IsTrue(go == null, "The mine never detonated.");
            Assert.IsNotNull(GameObject.Find("FxMine Explode"), "The mine left no explosion.");
        }

        // ---------- splitter ----------

        [UnityTest]
        public IEnumerator Splitter_SplitsEvenWhenKilledAfterItsOwnUpdate()
        {
            GameObject mini = Track(new GameObject("Mini"));

            EnemyHealth splitter = MakeEnemy(Vector2.zero, 50f, addBeforeAnimator: go =>
            {
                var split = go.AddComponent<EnemySplitter>();
                SetPrivate(split, "_miniAntPrefab", mini);
                SetPrivate(split, "_splitCount", 3);
            });
            yield return null;

            // A test coroutine resumes after every Update this frame, like a DoT tick or a
            // projectile updated later than the splitter: the case the old Update poll missed.
            splitter.TakeDamage(new DamageInfo(100f, null, Vector2.zero, DamageType.Physical));
            yield return null;
            yield return null;

            Transform[] clones = Object.FindObjectsByType<Transform>().Where(t => t.name == "Mini(Clone)").ToArray();
            int minis = clones.Length;
            foreach (Transform t in clones)
            {
                Object.DestroyImmediate(t.gameObject);
            }

            Assert.AreEqual(3, minis, "The splitter died without spawning its hatchlings.");
        }

        // ---------- helpers ----------

        private GameObject Track(GameObject go)
        {
            _spawned.Add(go);
            return go;
        }

        private Tower MakeMelee(GrayboxMeleeTower.Caste caste, System.Action<GrayboxMeleeTower> tune = null)
        {
            GrayboxMeleeTower.Stats stats = GrayboxMeleeTower.StatsFor(caste);
            GameObject go = Track(new GameObject("Melee"));
            go.SetActive(false);

            Tower tower = go.AddComponent<Tower>();
            SetPrivate(tower, "_range", stats.Range);
            SetPrivate(tower, "_fireInterval", stats.Interval);
            GrayboxTowerAnimator.Attach(go, _library, "TestTower");
            var melee = go.AddComponent<GrayboxMeleeTower>();
            melee.Configure(caste);
            tune?.Invoke(melee);

            go.SetActive(true);
            return tower;
        }

        private EnemyHealth MakeEnemy(Vector2 position, float health, bool animate = false,
            System.Action<GameObject> addBeforeAnimator = null, string key = "TestEnemy")
        {
            GameObject go = Track(new GameObject("Enemy", typeof(SpriteRenderer)));
            go.SetActive(false);
            go.transform.position = position;
            go.AddComponent<CircleCollider2D>().radius = 0.4f;

            EnemyHealth health_ = go.AddComponent<EnemyHealth>();
            SetPrivate(health_, "_maxHealth", health);
            addBeforeAnimator?.Invoke(go);
            if (animate)
            {
                GrayboxEnemyAnimator.Attach(go, _library, key);
            }

            go.SetActive(true);
            return health_;
        }

        private Tower MakeTower(Vector2 position, System.Action<GameObject> addBeforeAnimator = null, string key = "TestTower")
        {
            GameObject go = Track(new GameObject("Tower"));
            go.SetActive(false);
            go.transform.position = position;

            Tower tower = go.AddComponent<Tower>();
            SetPrivate(tower, "_range", 3f);
            addBeforeAnimator?.Invoke(go);
            GrayboxTowerAnimator.Attach(go, _library, key);

            go.SetActive(true);
            return tower;
        }

        private static IEnumerator WaitForClip(SpriteClipPlayer player, string clip, float timeout)
        {
            float end = Time.time + timeout;
            while (player.CurrentClip != clip && Time.time < end)
            {
                yield return null;
            }
        }

        private static void SetPrivate(object target, string fieldName, object value)
        {
            for (System.Type type = target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(fieldName, PrivateInstance);
                if (field != null)
                {
                    field.SetValue(target, value);
                    return;
                }
            }

            Assert.Fail($"No field {fieldName} on {target.GetType().Name}");
        }
    }
}
