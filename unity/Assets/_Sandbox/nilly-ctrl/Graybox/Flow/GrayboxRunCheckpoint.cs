using System.Collections.Generic;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Enemies;
using HoldTheHill.Features.Towers;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// A saved moment of a run: wave, gold, hill health, skills, stats and every tower.
    /// <see cref="GrayboxGameFlow"/> takes one when the run starts and one at each wave start,
    /// which is what "restart" and "retry current wave" rewind to.
    /// </summary>
    /// <remarks>
    /// Towers are kept as inactive copies rather than as data. The placer builds each tower type
    /// from a different set of components, so a copy is the only description of a tower that
    /// cannot drift from how it was really built. A checkpoint can be restored any number of times.
    /// Mines already laid and shots in flight are not saved; restoring clears the mines.
    /// </remarks>
    public sealed class GrayboxRunCheckpoint
    {
        // Children that tower scripts create for themselves in Awake/Start. They are removed from
        // the stored copy because the restored tower makes them again; left in, every restore
        // would add a second range ring and a second set of orbiters.
        private static readonly string[] SelfBuiltChildPrefixes = { "RangeCircle", "TargetLine", "Orbiter" };
        private static readonly List<Tower> Towers = new List<Tower>();

        private struct StoredTower
        {
            public GameObject Copy;
            public Transform Parent;
        }

        private readonly List<StoredTower> _towers = new List<StoredTower>();
        private GameObject _store;

        /// <summary>0-based wave the spawner will run next after a restore.</summary>
        public int WaveIndex { get; private set; }
        public int Gold { get; private set; }
        public float HillHealth { get; private set; }
        public int SkillPoints { get; private set; }
        public List<SkillNodeId> UnlockedSkills { get; private set; }
        public GrayboxRunStats Stats { get; private set; }
        public int TowerCount => _towers.Count;

        /// <summary>Saves the field as it is now.</summary>
        /// <param name="waveIndex">0-based wave to come back to.</param>
        /// <param name="stats">Run stats so far; copied.</param>
        /// <param name="storeParent">Where the inactive tower copies are kept.</param>
        public static GrayboxRunCheckpoint Capture(int waveIndex, GrayboxRunStats stats, Transform storeParent)
        {
            var checkpoint = new GrayboxRunCheckpoint
            {
                WaveIndex = Mathf.Max(0, waveIndex),
                Gold = GrayboxEconomy.Instance != null ? GrayboxEconomy.Instance.CurrentGold : 0,
                HillHealth = GrayboxBaseHealth.Instance != null ? GrayboxBaseHealth.Instance.CurrentHealth : 0f,
                SkillPoints = GrayboxSkillTree.Instance != null ? GrayboxSkillTree.Instance.SkillPoints : 0,
                UnlockedSkills = GrayboxSkillTree.Instance != null ? GrayboxSkillTree.Instance.GetUnlockedIds() : new List<SkillNodeId>(),
                Stats = stats != null ? stats.Clone() : new GrayboxRunStats(),
            };

            // Inactive, so the copies never run Awake and are not counted as standing towers.
            checkpoint._store = new GameObject($"RunCheckpoint (wave {checkpoint.WaveIndex + 1})");
            checkpoint._store.SetActive(false);
            checkpoint._store.transform.SetParent(storeParent, false);

            Tower.GetActive(Towers);
            foreach (Tower tower in Towers)
            {
                GameObject copy = Object.Instantiate(tower.gameObject, checkpoint._store.transform, true);
                copy.name = tower.gameObject.name;
                StripSelfBuiltChildren(copy.transform);
                checkpoint._towers.Add(new StoredTower { Copy = copy, Parent = tower.transform.parent });
            }

            return checkpoint;
        }

        /// <summary>Clears the field and puts everything back to how it was at <see cref="Capture"/>.</summary>
        public void Restore(EnemySpawner spawner)
        {
            if (spawner != null)
            {
                // First, so nothing spawns into the field being cleared.
                spawner.RestartFromWave(WaveIndex);
                GrayboxCustomSpawner custom = spawner.GetComponent<GrayboxCustomSpawner>();
                if (custom != null) custom.StopCustomSpawning();
            }

            ClearField();

            foreach (StoredTower stored in _towers)
            {
                if (stored.Copy == null) continue;

                GameObject tower = stored.Parent != null
                    ? Object.Instantiate(stored.Copy, stored.Parent, true)
                    : Object.Instantiate(stored.Copy);
                tower.name = stored.Copy.name;
            }

            if (GrayboxEconomy.Instance != null) GrayboxEconomy.Instance.ResetGold(Gold);
            if (GrayboxSkillTree.Instance != null) GrayboxSkillTree.Instance.RestoreState(SkillPoints, UnlockedSkills);
            if (GrayboxBaseHealth.Instance != null) GrayboxBaseHealth.Instance.RestoreHealth(HillHealth);
        }

        /// <summary>Frees the stored tower copies. The checkpoint cannot be restored afterwards.</summary>
        public void Discard()
        {
            if (_store != null)
            {
                Object.Destroy(_store);
                _store = null;
            }

            _towers.Clear();
        }

        /// <summary>Removes enemies, towers, mines and fire puddles from the field.</summary>
        public static void ClearField()
        {
            foreach (EnemyHealth enemy in Object.FindObjectsByType<EnemyHealth>()) Remove(enemy.gameObject);
            Tower.GetActive(Towers);
            foreach (Tower tower in Towers) Remove(tower.gameObject);
            foreach (ProximityMine mine in Object.FindObjectsByType<ProximityMine>()) Remove(mine.gameObject);
            foreach (GroundHazard hazard in Object.FindObjectsByType<GroundHazard>()) Remove(hazard.gameObject);
        }

        // Destroy only takes effect at the end of the frame. Switching the object off first means
        // nothing later this frame (the placer's tower lookup, a victory check) still finds it.
        private static void Remove(GameObject go)
        {
            go.SetActive(false);
            Object.Destroy(go);
        }

        private static void StripSelfBuiltChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                foreach (string prefix in SelfBuiltChildPrefixes)
                {
                    if (child.name.StartsWith(prefix))
                    {
                        // Immediate: a deferred Destroy would still be in place if the checkpoint
                        // were restored later in the same frame.
                        Object.DestroyImmediate(child.gameObject);
                        break;
                    }
                }
            }
        }
    }
}
