#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
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
    /// The rules behind the bosses and special enemies (Graybox/Specials), without any art:
    /// who takes how much damage from where, what happens to towers, and who gets the food.
    /// </summary>
    public class GrayboxSpecialsTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

        private readonly List<GameObject> _made = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            GrayboxTowerAffliction.RestoreAll();
            foreach (GameObject go in _made)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }

            _made.Clear();
            foreach (EnemyHealth stray in Object.FindObjectsByType<EnemyHealth>())
            {
                Object.DestroyImmediate(stray.gameObject);
            }

            foreach (GrayboxQuickZone zone in Object.FindObjectsByType<GrayboxQuickZone>())
            {
                Object.DestroyImmediate(zone.gameObject);
            }
        }

        // ---------- helpers ----------

        private GameObject Make(string name, Vector2 position)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            _made.Add(go);
            return go;
        }

        private static void Set(object target, string field, object value)
        {
            System.Type type = target.GetType();
            FieldInfo info = null;
            while (type != null && info == null)
            {
                info = type.GetField(field, PrivateInstance);
                type = type.BaseType;
            }

            Assert.IsNotNull(info, $"{target.GetType().Name} has no field {field}");
            info.SetValue(target, value);
        }

        /// <summary>An enemy with health, a mover and a collider, at full health, facing +x.</summary>
        private GameObject MakeEnemy(string name, Vector2 position, float health = 1000f)
        {
            GameObject go = Make(name, position);
            go.SetActive(false);
            var enemyHealth = go.AddComponent<EnemyHealth>();
            Set(enemyHealth, "_maxHealth", health);
            enemyHealth.BountyValue = 0;
            go.AddComponent<EnemyMover>();
            go.AddComponent<CircleCollider2D>().radius = 0.3f;
            go.SetActive(true); // health fills in OnEnable
            return go;
        }

        private T MakeSpecial<T>(string name, Vector2 position, float health = 1000f, System.Action<T> configure = null)
            where T : GrayboxSpecialEnemy
        {
            GameObject go = MakeEnemy(name, position, health);
            go.SetActive(false);
            var special = go.AddComponent<T>();
            configure?.Invoke(special);
            go.SetActive(true);
            return special;
        }

        private Tower MakeTower(Vector2 position)
        {
            return Make("Tower", position).AddComponent<Tower>();
        }

        private void MakePath(params Vector2[] points)
        {
            GameObject go = Make("Enemy Path", Vector2.zero);
            go.SetActive(false);
            foreach (Vector2 point in points)
            {
                var waypoint = new GameObject("Waypoint");
                waypoint.transform.SetParent(go.transform);
                waypoint.transform.position = point;
            }

            go.AddComponent<EnemyPath>();
            go.SetActive(true);
        }

        private static float Hit(GameObject enemy, float amount, GameObject source, DamageType type = DamageType.Physical)
        {
            var health = enemy.GetComponent<EnemyHealth>();
            float before = health.CurrentHealth;
            health.TakeDamage(new DamageInfo(amount, source, enemy.transform.position, type));
            return before - health.CurrentHealth;
        }

        private class HalveDamage : MonoBehaviour, IIncomingDamageModifier
        {
            public float ModifyIncomingDamage(EnemyHealth health, DamageInfo info) => info.Amount * 0.5f;
        }

        private class BlockDamage : MonoBehaviour, IIncomingDamageModifier
        {
            public float ModifyIncomingDamage(EnemyHealth health, DamageInfo info) => 0f;
        }

        // ---------- the hooks in the game scripts ----------

        [Test]
        public void DamageModifier_ScalesTheHit()
        {
            GameObject enemy = MakeEnemy("Enemy", Vector2.zero);
            enemy.AddComponent<HalveDamage>();
            Assert.AreEqual(50f, Hit(enemy, 100f, null), 0.01f);
        }

        [Test]
        public void DamageModifier_ReturningZero_CancelsTheHitAndItsEvent()
        {
            GameObject enemy = MakeEnemy("Enemy", Vector2.zero);
            enemy.AddComponent<BlockDamage>();
            int events = 0;
            System.Action<EnemyHealth, DamageInfo, float> count = (h, i, a) => events++;
            EnemyHealth.Damaged += count;
            try
            {
                Assert.AreEqual(0f, Hit(enemy, 100f, null), 0.01f);
                Assert.AreEqual(0, events);
            }
            finally
            {
                EnemyHealth.Damaged -= count;
            }
        }

        [Test]
        public void Tower_FireRateScale_StretchesTheInterval()
        {
            Tower tower = MakeTower(Vector2.zero);
            float normal = tower.EffectiveFireInterval;
            tower.FireRateScale = 0.5f;
            Assert.AreEqual(normal * 2f, tower.EffectiveFireInterval, 0.001f);
        }

        // ---------- path helpers ----------

        [Test]
        public void PathHelpers_MeasureAndWalkTheRoute()
        {
            MakePath(new Vector2(0, 0), new Vector2(4, 0), new Vector2(4, 3));
            Assert.AreEqual(7f, GrayboxSpecialEnemy.PathLength(), 0.001f);
            Assert.AreEqual(new Vector3(2, 0, 0), GrayboxSpecialEnemy.PointAlongPath(2f));
            Assert.AreEqual(new Vector3(4, 1, 0), GrayboxSpecialEnemy.PointAlongPath(5f));
            Assert.AreEqual(new Vector3(4, 3, 0), GrayboxSpecialEnemy.PointAlongPath(99f));
        }

        // ---------- towers ----------

        [Test]
        public void Stun_SilencesTheTower_AndClearingRestoresIt()
        {
            Tower tower = MakeTower(Vector2.zero);
            var frost = tower.gameObject.AddComponent<FrostAuraTower>();

            GrayboxTowerAffliction.Apply(tower, GrayboxTowerAffliction.Kind.Stun, 5f);
            Assert.IsTrue(GrayboxTowerAffliction.Silenced(tower));
            Assert.IsFalse(tower.enabled);
            Assert.IsFalse(frost.enabled);

            GrayboxTowerAffliction.RestoreAll();
            Assert.IsFalse(GrayboxTowerAffliction.Silenced(tower));
            Assert.IsTrue(tower.enabled);
            Assert.IsTrue(frost.enabled);
        }

        [Test]
        public void Silence_DoesNotSwitchOnWhatWasAlreadyOff()
        {
            Tower tower = MakeTower(Vector2.zero);
            var frost = tower.gameObject.AddComponent<FrostAuraTower>();
            frost.enabled = false;

            GrayboxTowerAffliction.Apply(tower, GrayboxTowerAffliction.Kind.Cocoon, 5f);
            GrayboxTowerAffliction.Clear(tower, GrayboxTowerAffliction.Kind.Cocoon);

            Assert.IsTrue(tower.enabled);
            Assert.IsFalse(frost.enabled);
        }

        [Test]
        public void TwoSilences_HoldUntilBothAreGone()
        {
            Tower tower = MakeTower(Vector2.zero);
            GrayboxTowerAffliction.Apply(tower, GrayboxTowerAffliction.Kind.Stun, 5f);
            GrayboxTowerAffliction.Apply(tower, GrayboxTowerAffliction.Kind.Scald, 5f);

            GrayboxTowerAffliction.Clear(tower, GrayboxTowerAffliction.Kind.Stun);
            Assert.IsFalse(tower.enabled);

            GrayboxTowerAffliction.Clear(tower, GrayboxTowerAffliction.Kind.Scald);
            Assert.IsTrue(tower.enabled);
        }

        [Test]
        public void Web_HalvesFireRate_WithoutSilencing()
        {
            Tower tower = MakeTower(Vector2.zero);
            GrayboxTowerAffliction.Apply(tower, GrayboxTowerAffliction.Kind.Web, 5f);
            Assert.AreEqual(0.5f, tower.FireRateScale, 0.001f);
            Assert.IsTrue(tower.enabled);

            GrayboxTowerAffliction.Clear(tower, GrayboxTowerAffliction.Kind.Web);
            Assert.AreEqual(1f, tower.FireRateScale, 0.001f);
        }

        [UnityTest]
        public IEnumerator Stun_WearsOffByItself()
        {
            Tower tower = MakeTower(Vector2.zero);
            GrayboxTowerAffliction.Apply(tower, GrayboxTowerAffliction.Kind.Stun, 0.15f);
            Assert.IsFalse(tower.enabled);
            yield return new WaitForSeconds(0.4f);
            Assert.IsTrue(tower.enabled);
        }

        [Test]
        public void CarriedTower_LeavesTheMap_AndComesBackOnRestore()
        {
            Tower tower = MakeTower(Vector2.zero);
            GrayboxTowerAffliction.CarryOff(tower);
            Assert.IsFalse(tower.gameObject.activeSelf);
            Assert.IsTrue(GrayboxTowerAffliction.Of(tower).IsCarried);

            GrayboxTowerAffliction.RestoreAll();
            Assert.IsTrue(tower.gameObject.activeSelf);
            Assert.IsFalse(GrayboxTowerAffliction.Of(tower).IsCarried);
        }

        // ---------- bosses ----------

        [Test]
        public void TitanBeetle_TakesLessFromAhead_FullFromBehind_AndFromItsOwnPoison()
        {
            GrayboxTitanBeetle titan = MakeSpecial<GrayboxTitanBeetle>("Titan", Vector2.zero, 100000f);
            GameObject ahead = Make("Ahead", new Vector2(3, 0));
            GameObject behind = Make("Behind", new Vector2(-3, 1));

            Assert.AreEqual(35f, Hit(titan.gameObject, 100f, ahead), 0.01f);
            Assert.AreEqual(100f, Hit(titan.gameObject, 100f, behind), 0.01f);
            Assert.AreEqual(100f, Hit(titan.gameObject, 100f, titan.gameObject, DamageType.Poison), 0.01f);
        }

        [Test]
        public void TitanBeetle_ArmourFollowsItsFacing()
        {
            GrayboxTitanBeetle titan = MakeSpecial<GrayboxTitanBeetle>("Titan", Vector2.zero, 100000f);
            titan.transform.rotation = Quaternion.Euler(0f, 0f, 90f); // now walking up the screen
            GameObject above = Make("Above", new Vector2(0, 3));
            GameObject below = Make("Below", new Vector2(0, -3));

            Assert.AreEqual(35f, Hit(titan.gameObject, 100f, above), 0.01f);
            Assert.AreEqual(100f, Hit(titan.gameObject, 100f, below), 0.01f);
        }

        [UnityTest]
        public IEnumerator TitanBeetle_AtItsFirstMark_RoarsStunsAndLosesAPlate()
        {
            GrayboxTitanBeetle titan = MakeSpecial<GrayboxTitanBeetle>("Titan", Vector2.zero, 1000f, t => Set(t, "_chargeMaxSeconds", 0.5f));
            Tower near = MakeTower(new Vector2(0, 2));
            Tower far = MakeTower(new Vector2(0, 20));
            GameObject behind = Make("Behind", new Vector2(-3, 0));

            Hit(titan.gameObject, 400f, behind);
            yield return null;
            Assert.IsTrue(GrayboxTowerAffliction.Silenced(near), "the roar should stun a tower beside it");
            Assert.IsFalse(GrayboxTowerAffliction.Silenced(far));

            float deadline = Time.time + 4f;
            while (titan.PlatesLost == 0 && Time.time < deadline)
            {
                yield return null;
            }

            Assert.AreEqual(1, titan.PlatesLost);
            GameObject ahead = Make("Ahead", new Vector2(3, 0));
            Assert.AreEqual(55f, Hit(titan.gameObject, 100f, ahead), 0.01f, "a cracked front lets more through");
        }

        [UnityTest]
        public IEnumerator MantisQueen_IsWardedWhileThreeEggsLive()
        {
            GameObject eggTemplate = Make("Egg Template", new Vector2(50, 50));
            eggTemplate.AddComponent<EnemyHealth>();
            eggTemplate.AddComponent<GrayboxEggSac>();

            GrayboxMantisQueen queen = MakeSpecial<GrayboxMantisQueen>("Queen", Vector2.zero, 1000f, q =>
            {
                Set(q, "_eggPrefab", eggTemplate);
                Set(q, "_layInterval", 999f);
            });

            var eggs = new List<GrayboxEggSac> { queen.LayEggNow(), queen.LayEggNow() };
            yield return null;
            Assert.IsFalse(queen.IsWarded, "two eggs are not enough");
            Assert.AreEqual(100f, Hit(queen.gameObject, 100f, null), 0.01f);

            eggs.Add(queen.LayEggNow());
            yield return null;
            Assert.IsTrue(queen.IsWarded);
            Assert.AreEqual(0f, Hit(queen.gameObject, 100f, null), 0.01f);

            eggs[0].GetComponent<EnemyHealth>().TakeDamage(new DamageInfo(9999f, null, Vector2.zero));
            yield return null;
            yield return null;
            Assert.IsFalse(queen.IsWarded, "killing an egg should drop the ward");
            Assert.AreEqual(100f, Hit(queen.gameObject, 100f, null), 0.01f);
        }

        [UnityTest]
        public IEnumerator EggSac_Hatches_IntoItsHatchlingAndIsGone()
        {
            GameObject hatchTemplate = MakeEnemy("Hatch Template", new Vector2(60, 60));
            GameObject eggGo = Make("Egg", Vector2.zero);
            eggGo.AddComponent<EnemyHealth>();
            var egg = eggGo.AddComponent<GrayboxEggSac>();
            Set(egg, "_hatchPrefab", hatchTemplate);

            GameObject hatchling = egg.Hatch();
            Assert.IsNotNull(hatchling);
            Assert.IsFalse(egg.Alive);
            yield return null;
            Assert.IsTrue(egg == null, "the egg should be destroyed once hatched");
        }

        [UnityTest]
        public IEnumerator BoulderBug_RolledUp_IsImmune_UntilAKickerPopsIt()
        {
            GrayboxBoulderBug bug = MakeSpecial<GrayboxBoulderBug>("Bug", Vector2.zero, 100000f, b =>
            {
                Set(b, "_walkSeconds", 0.05f);
                Set(b, "_rollSeconds", 30f);
            });
            GameObject plain = Make("Plain Tower", new Vector2(2, 0));
            GameObject kicker = Make("Kicker", new Vector2(-2, 0));
            kicker.AddComponent<KnockbackTower>().enabled = false;

            float deadline = Time.time + 4f;
            while (bug.Current != GrayboxBoulderBug.State.Rolling && Time.time < deadline)
            {
                yield return null;
            }

            Assert.AreEqual(GrayboxBoulderBug.State.Rolling, bug.Current);
            Assert.AreEqual(0f, Hit(bug.gameObject, 100f, plain), 0.01f);

            Assert.AreEqual(150f, Hit(bug.gameObject, 100f, kicker), 0.01f, "the hit that pops it lands, with the bonus");
            Assert.AreEqual(GrayboxBoulderBug.State.OnItsBack, bug.Current);
            Assert.AreEqual(150f, Hit(bug.gameObject, 100f, plain), 0.01f);
        }

        [UnityTest]
        public IEnumerator Hornet_HurtEnoughWhileMarking_IsShakenOff_AndTheTowerStays()
        {
            Tower tower = MakeTower(new Vector2(1f, 0f));
            GrayboxHornet hornet = MakeSpecial<GrayboxHornet>("Hornet", Vector2.zero, 1800f);
            GameObject shooter = Make("Shooter", new Vector2(0, 3));

            float deadline = Time.time + 3f;
            while (!hornet.IsMarking && Time.time < deadline)
            {
                yield return null;
            }

            Assert.IsTrue(hornet.IsMarking, "it should mark the only tower");
            Assert.AreEqual(36f, hornet.ShakeOffDamage, 0.01f);

            Hit(hornet.gameObject, hornet.ShakeOffDamage - 1f, shooter);
            yield return null;
            Assert.IsTrue(hornet.IsMarking, "just short of the mark is not enough");

            float before = hornet.GetComponent<EnemyHealth>().CurrentHealth;
            Hit(hornet.gameObject, 2f, shooter);
            yield return null;
            yield return null;
            Assert.IsFalse(hornet.IsMarking);
            Assert.AreEqual(110f, before - hornet.GetComponent<EnemyHealth>().CurrentHealth, 0.01f, "the hit (2), plus 6% of its health for being shaken off");

            yield return new WaitForSeconds(1.5f); // longer than the dive would have taken
            Assert.IsTrue(tower.gameObject.activeSelf, "a shaken-off hornet takes nothing");
            Assert.IsNull(hornet.Carried);
        }

        [UnityTest]
        public IEnumerator Hornet_ShakenOff_TriesADifferentTowerNext()
        {
            Tower defended = MakeTower(new Vector2(1f, 0f));
            Tower other = MakeTower(new Vector2(-1f, 0f));
            defended.RecordDamage(500f); // its favourite: the tower that has done the most
            GrayboxHornet hornet = MakeSpecial<GrayboxHornet>("Hornet", Vector2.zero, 1800f, h =>
            {
                Set(h, "_shakenRestSeconds", 0.1f);
                Set(h, "_markSeconds", 30f);
            });

            float deadline = Time.time + 3f;
            while (!hornet.IsMarking && Time.time < deadline)
            {
                yield return null;
            }

            Assert.AreEqual(defended, hornet.Target);
            Hit(hornet.gameObject, hornet.ShakeOffDamage + 1f, null);

            deadline = Time.time + 4f;
            yield return null;
            yield return null;
            while (!hornet.IsMarking && Time.time < deadline)
            {
                yield return null;
            }

            Assert.IsTrue(hornet.IsMarking, "it should come back for another tower");
            Assert.AreEqual(other, hornet.Target, "and not for the one that just shook it off");
        }

        [UnityTest]
        public IEnumerator Hornet_LeftAlone_CarriesTheTowerOff_AndDyingGivesItBack()
        {
            MakePath(new Vector2(-30f, 0f), new Vector2(30f, 0f)); // a burrow far enough away to still be carrying
            Tower tower = MakeTower(new Vector2(1f, 0f));
            GrayboxHornet hornet = MakeSpecial<GrayboxHornet>("Hornet", Vector2.zero, 1800f, h => Set(h, "_markSeconds", 0.5f));

            float deadline = Time.time + 5f;
            while (hornet.Carried == null && Time.time < deadline)
            {
                yield return null;
            }

            Assert.AreEqual(tower, hornet.Carried);
            Assert.IsFalse(tower.gameObject.activeSelf);

            Hit(hornet.gameObject, 99999f, null);
            Assert.IsTrue(tower.gameObject.activeSelf, "killing it while it carries puts the tower back");
        }

        [Test]
        public void RivalBurrow_SendsOutItsAnts_ThenNoMore()
        {
            MakePath(new Vector2(0, 0), new Vector2(10, 0));
            GameObject antTemplate = MakeEnemy("Ant Template", new Vector2(70, 70));
            GrayboxRivalBurrow burrow = GrayboxRivalBurrow.Begin(null, new Vector3(4, 0.5f, 0), 4f, antTemplate, null);
            _made.Add(burrow.gameObject);

            Assert.IsNull(burrow.SpawnAnt(), "nothing comes out before it is finished");
            burrow.Complete(2, 10f);
            GameObject first = burrow.SpawnAnt();
            Assert.IsNotNull(first);
            Assert.AreEqual(4f, first.transform.position.x, 0.01f, "the ant starts on the route beside the burrow");
            Assert.IsNotNull(burrow.SpawnAnt());
            Assert.IsNull(burrow.SpawnAnt());
            Assert.AreEqual(0, burrow.AntsLeft);
        }

        // ---------- regular enemies ----------

        [Test]
        public void Flyer_IgnoresTheKickerAndMines_ButNotOrdinaryShots()
        {
            GrayboxWasp wasp = MakeSpecial<GrayboxWasp>("Wasp", Vector2.zero);
            GameObject kicker = Make("Kicker", new Vector2(2, 0));
            kicker.AddComponent<KnockbackTower>().enabled = false;
            GameObject plain = Make("Plain Tower", new Vector2(2, 2));

            Assert.AreEqual(0f, Hit(wasp.gameObject, 100f, kicker), 0.01f);
            Assert.AreEqual(100f, Hit(wasp.gameObject, 100f, plain), 0.01f);
        }

        [UnityTest]
        public IEnumerator Wasp_StingsOneTowerInReach_Once()
        {
            MakePath(new Vector2(0, 0), new Vector2(10, 0));
            Tower first = MakeTower(new Vector2(0.5f, 1f));
            Tower second = MakeTower(new Vector2(0.5f, -1f));
            MakeSpecial<GrayboxWasp>("Wasp", Vector2.zero);

            yield return null;
            yield return null;
            int stung = (GrayboxTowerAffliction.Silenced(first) ? 1 : 0) + (GrayboxTowerAffliction.Silenced(second) ? 1 : 0);
            Assert.AreEqual(1, stung);
        }

        [UnityTest]
        public IEnumerator ThiefAnt_TakesFood_AndKillingItGivesTheFoodBack()
        {
            MakePath(new Vector2(0, 0), new Vector2(6, 0));
            var economy = Make("Economy", Vector2.zero).AddComponent<GrayboxEconomy>();
            int start = economy.CurrentGold;

            GrayboxThiefAnt thief = MakeSpecial<GrayboxThiefAnt>("Thief", Vector2.zero, 1000f,
                t => Set(t.GetComponent<EnemyMover>(), "_onReachEnd", EnemyMover.EndBehaviour.Stop));
            var mover = thief.GetComponent<EnemyMover>();
            mover.SnapToStart();
            mover.UpdatePositionToMatchDistance(5.9f);

            float deadline = Time.time + 3f;
            while (thief.Stolen == 0 && Time.time < deadline)
            {
                yield return null;
            }

            Assert.AreEqual(40, thief.Stolen);
            Assert.AreEqual(start - 40, economy.CurrentGold);

            Hit(thief.gameObject, 99999f, null);
            Assert.AreEqual(start, economy.CurrentGold, "the food comes back when the thief dies");
        }

        [UnityTest]
        public IEnumerator QuickZone_SpeedsUpWhoeverIsInside_AndLetsGoWhenItEnds()
        {
            GameObject inside = MakeEnemy("Inside", new Vector2(0.2f, 0f));
            GameObject outside = MakeEnemy("Outside", new Vector2(8f, 0f));
            yield return new WaitForFixedUpdate();

            GrayboxQuickZone.Spawn(null, Vector3.zero, Quaternion.identity, 0.3f, 0.9f, 1.6f);
            yield return null;
            yield return null;
            Assert.AreEqual(1.6f, inside.GetComponent<EnemyMover>().SpeedScale, 0.001f);
            Assert.AreEqual(1f, outside.GetComponent<EnemyMover>().SpeedScale, 0.001f);

            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(1f, inside.GetComponent<EnemyMover>().SpeedScale, 0.001f);
        }

        [UnityTest]
        public IEnumerator Scald_SilencesTowersInReach_OnlyWhileItLasts()
        {
            Tower near = MakeTower(new Vector2(1f, 0f));
            Tower far = MakeTower(new Vector2(9f, 0f));
            GrayboxScald scald = GrayboxScald.Spawn(null, Vector3.zero, 0.4f, 2.2f);
            _made.Add(scald.gameObject);

            yield return null;
            yield return null;
            Assert.IsTrue(GrayboxTowerAffliction.Silenced(near));
            Assert.IsFalse(GrayboxTowerAffliction.Silenced(far));

            yield return new WaitForSeconds(1.2f);
            Assert.IsFalse(GrayboxTowerAffliction.Silenced(near));
        }
    }
}
#endif
