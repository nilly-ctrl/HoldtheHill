using System.Collections.Generic;
using HoldTheHill.Features.Combat;
using HoldTheHill.Features.Towers;
using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// What enemies can do to a tower: stun it, web it, cocoon it, scald it, mark it, carry it off.
    /// Added to a tower the first time something happens to it. Towers have no health, so every
    /// one of these is a disable with a timer, shown with an FxTowerAffliction clip over the tower.
    /// </summary>
    /// <remarks>
    /// Silenced (stunned, cocooned or scalded) switches off every component on the tower that
    /// attacks, and switches back on only the ones that were on before. A web halves
    /// <see cref="Tower.FireRateScale"/>, so it only slows towers that fire on Tower's own
    /// cooldown; aura towers (Frost Ant, Kicker, Sapper) keep their own pace under a web.
    /// </remarks>
    public class GrayboxTowerAffliction : MonoBehaviour
    {
        public enum Kind
        {
            Stun,
            Web,
            Cocoon,
            Scald,
        }

        private const string FxKey = "FxTowerAffliction";
        private const int OverlayOrder = 3; // over the tower (1) and walking enemies (2)

        private static readonly List<GrayboxTowerAffliction> All = new List<GrayboxTowerAffliction>();
        private static readonly string[] LoopClip = { "Stunned", "Webbed", "Cocooned", "Scalded" };
        private static readonly string[] StartClip = { null, "WebWrap", "CocoonWrap", null };
        private static readonly string[] EndClip = { null, "WebBreak", "CocoonBreak", null };

        private readonly float[] _left = new float[4];
        private readonly GameObject[] _overlay = new GameObject[4];
        private readonly List<Behaviour> _switchedOff = new List<Behaviour>();
        private GameObject _markOverlay;
        private Tower _tower;
        private bool _silenced;

        /// <summary>True while the tower cannot attack at all.</summary>
        public bool IsSilenced => _silenced;

        /// <summary>True while a hornet has it and it is off the map.</summary>
        public bool IsCarried { get; private set; }

        public bool Has(Kind kind) => _left[(int)kind] > 0f;

        public float Remaining(Kind kind) => _left[(int)kind];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            All.Clear();
        }

        // ---------- what enemies call ----------

        public static GrayboxTowerAffliction Of(Tower tower)
        {
            if (tower == null)
            {
                return null;
            }

            var affliction = tower.GetComponent<GrayboxTowerAffliction>();
            return affliction != null ? affliction : tower.gameObject.AddComponent<GrayboxTowerAffliction>();
        }

        /// <summary>Starts or extends an affliction. A longer one already running is left alone.</summary>
        public static void Apply(Tower tower, Kind kind, float seconds)
        {
            GrayboxTowerAffliction affliction = Of(tower);
            if (affliction != null && seconds > 0f)
            {
                affliction.Begin(kind, seconds);
            }
        }

        public static void Clear(Tower tower, Kind kind)
        {
            var affliction = tower != null ? tower.GetComponent<GrayboxTowerAffliction>() : null;
            if (affliction != null && affliction.Has(kind))
            {
                affliction.End(kind);
            }
        }

        public static bool IsAfflicted(Tower tower, Kind kind)
        {
            var affliction = tower != null ? tower.GetComponent<GrayboxTowerAffliction>() : null;
            return affliction != null && affliction.Has(kind);
        }

        public static bool Silenced(Tower tower)
        {
            var affliction = tower != null ? tower.GetComponent<GrayboxTowerAffliction>() : null;
            return affliction != null && affliction.IsSilenced;
        }

        /// <summary>Shows or hides the hornet's target mark. Purely a warning; it changes nothing.</summary>
        public static void SetMarked(Tower tower, bool marked)
        {
            GrayboxTowerAffliction affliction = Of(tower);
            if (affliction != null)
            {
                affliction.Mark(marked);
            }
        }

        /// <summary>Takes the tower off the map until <see cref="Return"/> or <see cref="RestoreAll"/>.</summary>
        public static void CarryOff(Tower tower)
        {
            GrayboxTowerAffliction affliction = Of(tower);
            if (affliction == null || affliction.IsCarried)
            {
                return;
            }

            affliction.Mark(false);
            affliction.IsCarried = true;
            tower.gameObject.SetActive(false);
        }

        public static void Return(Tower tower)
        {
            var affliction = tower != null ? tower.GetComponent<GrayboxTowerAffliction>() : null;
            if (affliction == null || !affliction.IsCarried)
            {
                return;
            }

            affliction.IsCarried = false;
            tower.gameObject.SetActive(true);
            SpriteClipPlayer.SpawnOneShot(affliction.Library, "FxBuild", "Rise", tower.transform.position, Quaternion.identity, 1f, OverlayOrder);
        }

        /// <summary>Every carried tower comes home and every timer ends. Called as each wave starts.</summary>
        public static void RestoreAll()
        {
            for (int i = All.Count - 1; i >= 0; i--)
            {
                GrayboxTowerAffliction affliction = All[i];
                if (affliction == null)
                {
                    All.RemoveAt(i);
                    continue;
                }

                if (affliction.IsCarried)
                {
                    Return(affliction._tower);
                }

                affliction.Mark(false);
                for (int k = 0; k < affliction._left.Length; k++)
                {
                    if (affliction._left[k] > 0f)
                    {
                        affliction.End((Kind)k);
                    }
                }
            }
        }

        // ---------- the component ----------

        private SpriteAnimLibrary Library
        {
            get
            {
                var player = GetComponentInChildren<SpriteClipPlayer>(true);
                return player != null && player.Library != null ? player.Library : GrayboxSpecials.Library;
            }
        }

        private void Awake()
        {
            _tower = GetComponent<Tower>();
            All.Add(this);
        }

        private void OnDestroy()
        {
            All.Remove(this);
        }

        private void Update()
        {
            for (int k = 0; k < _left.Length; k++)
            {
                if (_left[k] <= 0f)
                {
                    continue;
                }

                _left[k] -= Time.deltaTime;
                if (_left[k] <= 0f)
                {
                    End((Kind)k);
                }
            }
        }

        private void Begin(Kind kind, float seconds)
        {
            int k = (int)kind;
            bool fresh = _left[k] <= 0f;
            _left[k] = Mathf.Max(_left[k], seconds);
            if (!fresh)
            {
                return;
            }

            _overlay[k] = MakeOverlay(LoopClip[k], StartClip[k], k);
            Refresh();
        }

        private void End(Kind kind)
        {
            int k = (int)kind;
            _left[k] = 0f;
            if (_overlay[k] != null)
            {
                Destroy(_overlay[k]);
                _overlay[k] = null;
            }

            if (EndClip[k] != null && gameObject.activeInHierarchy)
            {
                SpriteClipPlayer.SpawnOneShot(Library, FxKey, EndClip[k], transform.position, Quaternion.identity, 1f, OverlayOrder + 1);
            }

            Refresh();
        }

        private void Mark(bool marked)
        {
            if (marked == (_markOverlay != null))
            {
                return;
            }

            if (marked)
            {
                _markOverlay = MakeOverlay("Marked", "MarkLock", 4);
            }
            else
            {
                Destroy(_markOverlay);
                _markOverlay = null;
            }
        }

        private GameObject MakeOverlay(string loop, string intro, int slot)
        {
            SpriteAnimLibrary library = Library;
            if (library == null || library.Find(FxKey) == null)
            {
                return null;
            }

            var go = new GameObject("Affliction " + loop);
            go.transform.SetParent(transform, false);
            go.AddComponent<SpriteRenderer>().sortingOrder = OverlayOrder + slot;
            var player = go.AddComponent<SpriteClipPlayer>();
            player.Configure(library, FxKey, loop);
            if (intro != null)
            {
                player.Play(intro);
            }

            return go;
        }

        private void Refresh()
        {
            if (_tower != null)
            {
                _tower.FireRateScale = Has(Kind.Web) ? 0.5f : 1f;
            }

            bool silence = Has(Kind.Stun) || Has(Kind.Cocoon) || Has(Kind.Scald);
            if (silence == _silenced)
            {
                return;
            }

            _silenced = silence;
            if (silence)
            {
                _switchedOff.Clear();
                foreach (Behaviour weapon in Weapons())
                {
                    if (weapon.enabled)
                    {
                        weapon.enabled = false;
                        _switchedOff.Add(weapon);
                    }
                }

                var beam = GetComponent<ContinuousBeam>();
                if (beam != null)
                {
                    beam.StopFiring();
                }
            }
            else
            {
                foreach (Behaviour weapon in _switchedOff)
                {
                    if (weapon != null)
                    {
                        weapon.enabled = true;
                    }
                }

                _switchedOff.Clear();
            }
        }

        // Everything on a graybox tower that deals damage or lays something on its own clock.
        private IEnumerable<Behaviour> Weapons()
        {
            if (_tower != null) yield return _tower;
            foreach (FrostAuraTower c in GetComponents<FrostAuraTower>()) yield return c;
            foreach (KnockbackTower c in GetComponents<KnockbackTower>()) yield return c;
            foreach (MineLayerTower c in GetComponents<MineLayerTower>()) yield return c;
            foreach (OrbitingDamageField c in GetComponents<OrbitingDamageField>()) yield return c;
            foreach (GrayboxMeleeTower c in GetComponents<GrayboxMeleeTower>()) yield return c;
        }
    }
}
