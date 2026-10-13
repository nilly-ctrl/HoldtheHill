using HoldTheHill.Features.Enemies;
using UnityEngine;

namespace HoldTheHill.Features.Towers
{
    /// <summary>
    /// Keeps towers off the trail: a cell closer to the route than the clearance is blocked. Put it
    /// on the <see cref="EnemyPath"/> object; it finds the path on the same object if none is set.
    /// </summary>
    [AddComponentMenu("Hold the Hill/Towers/Path Clearance Blocker")]
    public class PathClearanceBlocker : MonoBehaviour, IBuildBlocker
    {
        [Tooltip("The route to keep clear. Empty takes the EnemyPath on this object.")]
        [SerializeField] private EnemyPath _path;

        [Tooltip("How close to the route's centre line a tower may stand, in world units.")]
        [SerializeField, Min(0f)] private float _clearance = 1.2f;

        /// <summary>How close to the route a tower may stand.</summary>
        public float Clearance => _clearance;

        private void Awake()
        {
            if (_path == null)
            {
                _path = GetComponent<EnemyPath>();
            }
        }

        private void OnEnable()
        {
            BuildBlockers.Register(this);
        }

        private void OnDisable()
        {
            BuildBlockers.Unregister(this);
        }

        public bool Blocks(Vector3 cellCentre)
        {
            return _path != null && _path.DistanceToRoute(cellCentre) < _clearance;
        }
    }
}
