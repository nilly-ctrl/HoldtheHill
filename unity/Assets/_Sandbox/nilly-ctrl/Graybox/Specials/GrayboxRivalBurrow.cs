using UnityEngine;

namespace HoldTheHill.Sandbox.NillyCtrl
{
    /// <summary>
    /// The burrow the Rival Queen digs beside the path. While she digs it only shows how far
    /// along she is. Once complete it sends out a fixed number of ants, one at a time, from its
    /// place on the route, and then falls in.
    /// </summary>
    public class GrayboxRivalBurrow : MonoBehaviour
    {
        private const int GroundOrder = 1;

        private SpriteClipPlayer _player;
        private GameObject _antPrefab;
        private Transform _container;
        private float _pathDistance;
        private int _antsLeft;
        private float _interval;
        private float _cooldown;
        private bool _complete;
        private bool _collapsing;

        public bool IsComplete => _complete;

        public int AntsLeft => _antsLeft;

        public static GrayboxRivalBurrow Begin(SpriteAnimLibrary library, Vector3 position, float pathDistance, GameObject antPrefab, Transform container)
        {
            SpriteClipPlayer visual = GrayboxSpecials.LoopFx(library, "PropRivalBurrow", "Stage1", null, position, Quaternion.identity, GroundOrder);
            GameObject go = visual != null ? visual.gameObject : new GameObject("Rival Burrow");
            go.name = "Rival Burrow";
            go.transform.position = position;
            var burrow = go.AddComponent<GrayboxRivalBurrow>();
            burrow._player = visual;
            burrow._antPrefab = antPrefab;
            burrow._container = container;
            burrow._pathDistance = pathDistance;
            return burrow;
        }

        /// <summary>How far the dig has got, 0 to 1. Picks which of the three stages shows.</summary>
        public void SetProgress(float progress)
        {
            if (_player != null && !_complete && !_collapsing)
            {
                _player.DefaultClip = progress < 0.34f ? "Stage1" : progress < 0.67f ? "Stage2" : "Stage3";
            }
        }

        public void Complete(int ants, float interval)
        {
            _complete = true;
            _antsLeft = ants;
            _interval = interval;
            _cooldown = interval * 0.5f;
            if (_player != null)
            {
                _player.DefaultClip = "Complete";
            }
        }

        public void Collapse()
        {
            if (_collapsing)
            {
                return;
            }

            _collapsing = true;
            _complete = false;
            if (_player != null)
            {
                SpriteClipPlayer.SpawnOneShot(_player.Library, _player.Key, "Collapse", transform.position, Quaternion.identity, 1f, GroundOrder);
            }

            Destroy(gameObject);
        }

        private void Update()
        {
            if (!_complete || _collapsing)
            {
                return;
            }

            _cooldown -= Time.deltaTime;
            if (_cooldown > 0f)
            {
                return;
            }

            _cooldown = _interval;
            SpawnAnt();
            if (_antsLeft <= 0)
            {
                Collapse();
            }
        }

        /// <summary>Sends one ant out onto the route from here. Public so a test can skip the clock.</summary>
        public GameObject SpawnAnt()
        {
            if (_antsLeft <= 0)
            {
                return null;
            }

            _antsLeft--;
            if (_antPrefab == null)
            {
                return null;
            }

            GameObject ant = Instantiate(_antPrefab, transform.position, Quaternion.identity, _container);
            GrayboxSpecialEnemy.PlaceOnPath(ant, _pathDistance);
            return ant;
        }
    }
}
