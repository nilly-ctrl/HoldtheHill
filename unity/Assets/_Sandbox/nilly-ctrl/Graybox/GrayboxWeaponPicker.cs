using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// A viewer for the weapon library (the Wpn* sets): fires the chosen weapon over and over
    /// from a tower at a target, playing its Charge, Muzzle, Fly, Trail and Hit or Miss in order,
    /// so the families can be compared and the ones worth keeping ticked.
    /// </summary>
    /// <remarks>
    /// Built into its own scene by Tools > Hold the Hill > Build Weapon Picker. It only plays
    /// clips; no game systems run here. Ticked weapons are remembered between sessions and
    /// "Copy keep list" puts their names on the clipboard.
    ///
    /// Keys: Up / Down change weapon, 1 2 3 change tier, K ticks the weapon, M changes where
    /// the shot lands.
    /// </remarks>
    [AddComponentMenu("Hold the Hill/Graybox/Graybox Weapon Picker")]
    public class GrayboxWeaponPicker : MonoBehaviour
    {
        public enum Landing
        {
            Hit,
            Miss,
            Alternate,
        }

        private enum Shape
        {
            Shot,  // one sprite that flies from the tower to the target
            Strip, // a strip tiled from the tower to the target for a moment (ray, arc)
            Jet,   // a short burst that stays at the tower's mouth
        }

        private const string KeptPref = "HoldTheHill.GrayboxWeaponPicker.Kept";
        private const string Prefix = "Wpn";
        private const float TrailInterval = 0.07f;
        private const float StripSeconds = 0.45f;
        private const float JetSeconds = 0.55f;
        private const float JetReach = 1.2f;
        private const float Pause = 0.7f;
        private const float PanelWidth = 250f;

        private static readonly Dictionary<string, Shape> Shapes = new Dictionary<string, Shape>
        {
            { "WpnLaser", Shape.Strip }, { "WpnLightningBolt", Shape.Strip }, { "WpnFlameBurst", Shape.Jet },
        };

        // What each family is drawn as (see Animations/README.md).
        private static readonly Dictionary<string, string> Readings = new Dictionary<string, string>
        {
            { "WpnAcidSpit", "Formic acid glob" }, { "WpnBoulderToss", "Heaved river pebble" },
            { "WpnBubbleShot", "Dew bubble" }, { "WpnCannon", "Seed-pod launcher" },
            { "WpnCatapult", "Acorn lob" }, { "WpnCrossbow", "Thorn bolt" },
            { "WpnEmberFlick", "Glowing ember" }, { "WpnFlameBurst", "Formic fire jet" },
            { "WpnHarpoon", "Barbed stinger on silk" }, { "WpnHoneyGlob", "Honey glob" },
            { "WpnIceShard", "Frost shard" }, { "WpnLaser", "Dew-lens ray" },
            { "WpnLightningBolt", "Static arc" }, { "WpnMagicOrb", "Royal-jelly orb" },
            { "WpnMortar", "Mud bomb" }, { "WpnPebbleSling", "Slung pebble" },
            { "WpnPlasma", "Glow-spore" }, { "WpnPollenPuff", "Pollen cloud" },
            { "WpnSeedBurst", "Seed scatter" }, { "WpnSonicPulse", "Stridulation wave" },
            { "WpnStingerDart", "Wasp-stinger dart" }, { "WpnVenomBolt", "Venom bolt" },
            { "WpnWebShot", "Silk web ball" },
        };

        [SerializeField] private SpriteAnimLibrary _library;
        [Tooltip("Tower shown firing. Any Tower* set in the library.")]
        [SerializeField] private string _towerKey = "TowerLinear";
        [Tooltip("Enemy shown being hit. Any Enemy* set in the library.")]
        [SerializeField] private string _targetKey = "EnemyGrunt";
        [SerializeField] private Vector2 _muzzle = new Vector2(-2.5f, 0f);
        [SerializeField] private Vector2 _target = new Vector2(3.5f, 0f);
        [SerializeField, Min(0.5f)] private float _shotSpeed = 6f;

        private readonly List<string> _weapons = new List<string>();
        private readonly HashSet<string> _kept = new HashSet<string>();
        private readonly List<GameObject> _live = new List<GameObject>();
        private SpriteClipPlayer _targetPlayer;
        private Coroutine _loop;
        private Vector2 _scroll;
        private int _index;
        private int _tier = 1;
        private int _shots;
        private Landing _landing = Landing.Alternate;
        private float _speed = 1f;
        private string _step = "";

        /// <summary>The weapon sets found in the library, in list order.</summary>
        public IReadOnlyList<string> Weapons => _weapons;

        /// <summary>The set being fired now, e.g. "WpnAcidSpit".</summary>
        public string Current => _weapons.Count > 0 ? _weapons[_index] : null;

        /// <summary>Which part of the shot is playing: Charge, Muzzle, Fly, Hit...</summary>
        public string Step => _step;

        public int ShotsFired => _shots;

        public void Configure(SpriteAnimLibrary library) => _library = library;

        private void Start()
        {
            if (_library != null)
            {
                _weapons.AddRange(_library.Sets.Select(s => s.Key).Where(k => k.StartsWith(Prefix)).OrderBy(k => k));
            }

            foreach (string key in PlayerPrefs.GetString(KeptPref, "").Split(','))
            {
                if (_weapons.Contains(key))
                {
                    _kept.Add(key);
                }
            }

            if (_weapons.Count == 0)
            {
                Debug.LogWarning("[Graybox] Weapon picker: the library has no Wpn* sets. Rebuild the animation library.");
                return;
            }

            FitStageToScreen();

            // Tower art faces up and enemy art faces right, so turn them to face each other.
            Stand(_towerKey, "Idle", _muzzle + Vector2.left * 0.5f, -90f);
            _targetPlayer = Stand(_targetKey, "Walk", _target, 180f);
            Restart();
        }

        private void OnDisable()
        {
            Time.timeScale = 1f;
        }

        // The list panel is a fixed number of pixels wide, so on a small window it would cover
        // the tower. Put the tower just right of the panel and the target near the right edge.
        private void FitStageToScreen()
        {
            Camera view = Camera.main;
            if (view == null || !view.orthographic)
            {
                return;
            }

            float depth = -view.transform.position.z;
            float left = view.ScreenToWorldPoint(new Vector3(PanelWidth + 24f, 0f, depth)).x;
            float right = view.ScreenToWorldPoint(new Vector3(Screen.width, 0f, depth)).x;
            if (right - left < 4f)
            {
                return; // too narrow to be worth moving anything
            }

            _muzzle.x = left + 1.3f;
            _target.x = right - 1.2f;
        }

        private void Update()
        {
            Time.timeScale = _speed;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || _weapons.Count == 0)
            {
                return;
            }

            if (keyboard.downArrowKey.wasPressedThisFrame) Select(_index + 1);
            if (keyboard.upArrowKey.wasPressedThisFrame) Select(_index - 1);
            if (keyboard.digit1Key.wasPressedThisFrame) SetTier(1);
            if (keyboard.digit2Key.wasPressedThisFrame) SetTier(2);
            if (keyboard.digit3Key.wasPressedThisFrame) SetTier(3);
            if (keyboard.kKey.wasPressedThisFrame) ToggleKept(Current);
            if (keyboard.mKey.wasPressedThisFrame) SetLanding((Landing)(((int)_landing + 1) % 3));
        }

        public void Select(int index)
        {
            if (_weapons.Count == 0)
            {
                return;
            }

            _index = (index % _weapons.Count + _weapons.Count) % _weapons.Count;
            Restart();
        }

        public void SetTier(int tier)
        {
            _tier = Mathf.Clamp(tier, 1, 3);
            Restart();
        }

        public void SetLanding(Landing landing)
        {
            _landing = landing;
            Restart();
        }

        private void ToggleKept(string key)
        {
            if (!_kept.Remove(key))
            {
                _kept.Add(key);
            }

            PlayerPrefs.SetString(KeptPref, string.Join(",", _kept.OrderBy(k => k)));
        }

        private void Restart()
        {
            if (_loop != null)
            {
                StopCoroutine(_loop);
            }

            foreach (GameObject go in _live)
            {
                if (go != null)
                {
                    Destroy(go);
                }
            }

            _live.Clear();
            _shots = 0;
            _loop = StartCoroutine(FireLoop());
        }

        private IEnumerator FireLoop()
        {
            while (true)
            {
                string key = Current;
                SpriteAnimSet set = _library.Find(key);
                Shape shape = Shapes.TryGetValue(key, out Shape s) ? s : Shape.Shot;

                bool miss = _landing == Landing.Miss || (_landing == Landing.Alternate && _shots % 3 == 2);
                _shots++;

                // A miss lands on bare ground short of the target.
                Vector2 end = miss ? _target + new Vector2(-0.8f, -1.3f) : _target;
                Vector2 direction = (end - _muzzle).normalized;
                Quaternion facing = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
                if (shape == Shape.Jet)
                {
                    end = _muzzle + direction * JetReach;
                }

                SpriteAnimClip charge = set.Find("Charge");
                if (charge != null)
                {
                    _step = "Charge";
                    OneShot(key, "Charge", _muzzle, Quaternion.identity);
                    yield return new WaitForSeconds(charge.Length);
                }

                _step = "Muzzle";
                OneShot(key, "Muzzle", _muzzle + direction * 0.3f, facing);

                string fly = Tiered(set, "Fly");
                _step = fly;
                switch (shape)
                {
                    case Shape.Shot:
                        yield return FlyShot(key, fly, end, direction, facing);
                        break;
                    case Shape.Strip:
                        yield return ShowStrip(key, fly, end, direction, facing);
                        break;
                    case Shape.Jet:
                        GameObject jet = Looping(key, fly, _muzzle + direction * 0.5f, facing);
                        yield return new WaitForSeconds(JetSeconds);
                        Destroy(jet);
                        break;
                }

                string hit = miss ? "Miss" : Tiered(set, "Hit");
                _step = hit;
                OneShot(key, hit, end, Quaternion.identity);
                if (!miss && shape != Shape.Jet && _targetPlayer != null)
                {
                    _targetPlayer.Play("Hurt");
                }

                yield return new WaitForSeconds(Pause);
            }
        }

        private IEnumerator FlyShot(string key, string clip, Vector2 end, Vector2 direction, Quaternion facing)
        {
            GameObject shot = Looping(key, clip, _muzzle, facing);
            float travelled = 0f;
            float distance = Vector2.Distance(_muzzle, end);
            float trailTimer = TrailInterval;

            while (travelled < distance)
            {
                travelled = Mathf.Min(distance, travelled + _shotSpeed * Time.deltaTime);
                shot.transform.position = _muzzle + direction * travelled;

                trailTimer -= Time.deltaTime;
                if (trailTimer <= 0f)
                {
                    trailTimer = TrailInterval;
                    OneShot(key, "Trail", shot.transform.position, facing, order: 2);
                }

                yield return null;
            }

            Destroy(shot);
        }

        // One sprite is one world unit long, so lay whole ones end to end and squeeze the last to fit.
        private IEnumerator ShowStrip(string key, string clip, Vector2 end, Vector2 direction, Quaternion facing)
        {
            float distance = Vector2.Distance(_muzzle, end);
            var pieces = new List<GameObject>();
            for (float from = 0f; from < distance - 0.01f; from += 1f)
            {
                float length = Mathf.Min(1f, distance - from);
                GameObject piece = Looping(key, clip, _muzzle + direction * (from + length * 0.5f), facing);
                piece.transform.localScale = new Vector3(length, 1f, 1f);
                pieces.Add(piece);
            }

            for (float t = 0f; t < StripSeconds; t += TrailInterval * 2f)
            {
                OneShot(key, "Trail", Vector2.Lerp(_muzzle, end, Random.value), facing, order: 4);
                yield return new WaitForSeconds(TrailInterval * 2f);
            }

            foreach (GameObject piece in pieces)
            {
                Destroy(piece);
            }
        }

        /// <summary>"Fly" at tier 1; "Fly2" or "Fly3" above that when the set has them.</summary>
        private string Tiered(SpriteAnimSet set, string clip)
        {
            string tiered = clip + _tier;
            return _tier > 1 && set.Find(tiered) != null ? tiered : clip;
        }

        private void OneShot(string key, string clip, Vector2 at, Quaternion rotation, int order = 4)
        {
            SpriteClipPlayer.SpawnOneShot(_library, key, clip, at, rotation, 1f, order);
        }

        private GameObject Looping(string key, string clip, Vector2 at, Quaternion rotation)
        {
            var go = new GameObject($"{key} {clip}", typeof(SpriteRenderer));
            go.transform.SetParent(transform, false); // so it goes when the picker does
            go.transform.SetPositionAndRotation(at, rotation);
            go.GetComponent<SpriteRenderer>().sortingOrder = 3;
            go.AddComponent<SpriteClipPlayer>().Configure(_library, key, clip);
            _live.Add(go);
            return go;
        }

        private SpriteClipPlayer Stand(string key, string clip, Vector2 at, float angle)
        {
            if (_library.Find(key) == null)
            {
                return null;
            }

            var go = new GameObject(key, typeof(SpriteRenderer));
            go.transform.SetParent(transform, false);
            go.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, 0f, angle));
            go.GetComponent<SpriteRenderer>().sortingOrder = 1;
            var player = go.AddComponent<SpriteClipPlayer>();
            player.Configure(_library, key, clip);
            return player;
        }

        private void OnGUI()
        {
            GrayboxUi.Apply();

            GUILayout.BeginArea(new Rect(8f, 8f, PanelWidth, Screen.height - 16f), GUI.skin.box);
            GUILayout.Label($"WEAPON PICKER  {_kept.Count}/{_weapons.Count} KEPT");

            _scroll = GUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < _weapons.Count; i++)
            {
                string key = _weapons[i];
                GUILayout.BeginHorizontal();
                bool kept = _kept.Contains(key);
                if (GUILayout.Toggle(kept, "", GUILayout.Width(18f)) != kept)
                {
                    ToggleKept(key);
                }

                string label = (i == _index ? "> " : "") + key.Substring(Prefix.Length);
                if (GUILayout.Button(label) && i != _index)
                {
                    Select(i);
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

            if (GUILayout.Button("COPY KEEP LIST"))
            {
                string list = string.Join(", ", _kept.OrderBy(k => k));
                GUIUtility.systemCopyBuffer = list;
                Debug.Log($"[Graybox] Weapons kept: {list}");
            }

            GUILayout.EndArea();

            string current = Current;
            if (current == null)
            {
                GUI.Label(new Rect(PanelWidth + 24f, 12f, 600f, 40f), "NO WPN SETS IN THE LIBRARY. REBUILD IT.");
                return;
            }

            SpriteAnimSet set = _library.Find(current);
            GUILayout.BeginArea(new Rect(PanelWidth + 24f, 8f, Screen.width - PanelWidth - 40f, 130f), GUI.skin.box);
            Readings.TryGetValue(current, out string reading);
            GUILayout.Label($"{current.Substring(Prefix.Length)}: {reading}    NOW: {_step}");
            GUILayout.Label(string.Join("  ", set.Clips.Select(c => $"{c.Name} {c.Frames.Length}")));

            GUILayout.BeginHorizontal();
            GUILayout.Label("TIER", GUILayout.Width(50f));
            int tier = GUILayout.Toolbar(_tier - 1, new[] { "1", "2", "3" }, GUILayout.Width(150f)) + 1;
            if (tier != _tier) SetTier(tier);

            GUILayout.Space(16f);
            GUILayout.Label("LANDS", GUILayout.Width(60f));
            var landing = (Landing)GUILayout.Toolbar((int)_landing, new[] { "HIT", "MISS", "BOTH" }, GUILayout.Width(210f));
            if (landing != _landing) SetLanding(landing);

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"SPEED {_speed:0.0}", GUILayout.Width(80f));
            _speed = GUILayout.HorizontalSlider(_speed, 0.1f, 2f, GUILayout.Width(120f));
            GUILayout.Space(16f);
            GUILayout.Label("KEYS: UP/DOWN  1 2 3  K KEEP  M LANDS");
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }
}
