using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// Plays clips from a <see cref="SpriteAnimLibrary"/> on this object's SpriteRenderer.
    /// Looping clips repeat; one-shot clips fall back to the default clip when they end,
    /// or destroy the object if it only exists to play that clip (death and blast effects).
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SpriteClipPlayer : MonoBehaviour
    {
        [SerializeField] private SpriteAnimLibrary _library;
        [Tooltip("File name of the .aseprite the clips came from, e.g. TowerLinear.")]
        [SerializeField] private string _key;
        [Tooltip("Clip that plays at start and after any one-shot clip finishes.")]
        [SerializeField] private string _defaultClip;
        [SerializeField] private bool _destroyWhenDone;

        private SpriteRenderer _renderer;
        private SpriteAnimSet _set;
        private SpriteAnimClip _clip;
        private int _frame;
        private float _timer;

        public SpriteAnimLibrary Library => _library;

        public string Key => _key;

        public string CurrentClip => _clip?.Name;

        /// <summary>Changing this swaps the running clip only if the old default is what's playing.</summary>
        public string DefaultClip
        {
            get => _defaultClip;
            set
            {
                if (_defaultClip == value)
                {
                    return;
                }

                bool wasPlayingDefault = _clip == null || _clip.Name == _defaultClip;
                _defaultClip = value;
                if (wasPlayingDefault)
                {
                    Play(_defaultClip);
                }
            }
        }

        public void Configure(SpriteAnimLibrary library, string key, string defaultClip, bool destroyWhenDone = false)
        {
            _library = library;
            _key = key;
            _defaultClip = defaultClip;
            _destroyWhenDone = destroyWhenDone;
            _set = null;
            _clip = null;

            // Show the first frame straight away, so the object looks right in the editor too.
            SpriteAnimClip first = Resolve()?.Find(defaultClip);
            if (first != null && first.Frames.Length > 0)
            {
                GetComponent<SpriteRenderer>().sprite = first.Frames[0];
            }
        }

        public bool Has(string clipName) => Resolve()?.Find(clipName) != null;

        /// <summary>Starts a clip. With restart off, a clip that is already playing carries on.</summary>
        public bool Play(string clipName, bool restart = true)
        {
            SpriteAnimClip clip = Resolve()?.Find(clipName);
            if (clip == null || clip.Frames.Length == 0)
            {
                return false;
            }

            if (!restart && _clip == clip)
            {
                return true;
            }

            _clip = clip;
            _frame = 0;
            _timer = 0f;
            Show();
            return true;
        }

        /// <summary>Spawns a throwaway object that plays one clip and then removes itself.</summary>
        public static SpriteClipPlayer SpawnOneShot(
            SpriteAnimLibrary library, string key, string clipName,
            Vector3 position, Quaternion rotation, float scale, int sortingOrder)
        {
            SpriteAnimClip clip = library != null ? library.Find(key)?.Find(clipName) : null;
            if (clip == null)
            {
                return null;
            }

            var go = new GameObject($"{key} {clipName}");
            go.transform.SetPositionAndRotation(position, rotation);
            go.transform.localScale = Vector3.one * scale;
            go.AddComponent<SpriteRenderer>().sortingOrder = sortingOrder;

            var player = go.AddComponent<SpriteClipPlayer>();
            player.Configure(library, key, clipName, destroyWhenDone: true);
            player.Play(clipName);
            return player;
        }

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            if (_clip == null)
            {
                Play(_defaultClip);
            }
        }

        private void Update()
        {
            if (_clip == null)
            {
                return;
            }

            _timer += Time.deltaTime;
            while (_timer >= _clip.Durations[_frame])
            {
                _timer -= _clip.Durations[_frame];
                _frame++;

                if (_frame < _clip.Frames.Length)
                {
                    continue;
                }

                if (_clip.Loop)
                {
                    _frame = 0;
                    continue;
                }

                if (_destroyWhenDone)
                {
                    Destroy(gameObject);
                    _clip = null;
                    return;
                }

                // A one-shot finished: go back to the default clip, or hold the last frame.
                if (_clip.Name == _defaultClip || !Play(_defaultClip))
                {
                    _frame = _clip.Frames.Length - 1;
                    _timer = 0f;
                    Show();
                    _clip = null;
                }

                return;
            }

            Show();
        }

        private void Show()
        {
            if (_renderer == null)
            {
                _renderer = GetComponent<SpriteRenderer>();
            }

            _renderer.sprite = _clip.Frames[_frame];
        }

        private SpriteAnimSet Resolve()
        {
            if (_set == null && _library != null)
            {
                _set = _library.Find(_key);
            }

            return _set;
        }
    }
}
