using UnityEngine;
using UnityEngine.Pool;

namespace HoldTheHill.Features.Combat
{
    /// <summary>
    /// Recycles one kind of projectile. Spawning and destroying bullets every shot
    /// produces enough garbage to cause visible hitches during a heavy wave, so
    /// instances are reused instead.
    /// </summary>
    /// <remarks>
    /// One pool per prefab. Towers can share a pool by pointing at the same component,
    /// or call <see cref="For"/> to get a shared pool created on demand.
    /// </remarks>
    [AddComponentMenu("Hold the Hill/Combat/Projectile Pool")]
    public class ProjectilePool : MonoBehaviour
    {
        [Tooltip("Which projectile this pool recycles.")]
        [SerializeField] private Projectile _prefab;

        [Tooltip("Instances created up front, before the first shot is fired.")]
        [SerializeField, Min(0)] private int _prewarmCount = 16;

        [Tooltip("Above this many idle instances, extras are destroyed instead of kept.")]
        [SerializeField, Min(1)] private int _maxSize = 256;

        private ObjectPool<Projectile> _pool;

        /// <summary>The projectile this pool hands out.</summary>
        public Projectile Prefab => _prefab;

        /// <summary>Instances currently out in the world.</summary>
        public int ActiveCount => _pool?.CountActive ?? 0;

        private void Awake()
        {
            EnsurePool();
        }

        /// <summary>
        /// Builds the pool if it does not exist yet. Returns false when there is no prefab
        /// to pool.
        /// </summary>
        /// <remarks>
        /// Deliberately lazy rather than Awake-only. A pool created from code has its prefab
        /// assigned after <c>AddComponent</c>, which is after Awake has already run, so an
        /// Awake-only build would leave the pool permanently empty.
        /// </remarks>
        private bool EnsurePool()
        {
            if (_pool != null)
            {
                return true;
            }

            if (_prefab == null)
            {
                return false;
            }

            _pool = new ObjectPool<Projectile>(
                createFunc: CreateInstance,
                actionOnGet: OnGet,
                actionOnRelease: OnRelease,
                actionOnDestroy: OnDestroyInstance,
                collectionCheck: true,
                defaultCapacity: Mathf.Max(1, _prewarmCount),
                maxSize: Mathf.Max(1, _maxSize));

            Prewarm(_prewarmCount);
            return true;
        }

        private void OnDestroy()
        {
            _pool?.Clear();
        }

        /// <summary>
        /// Creates a pool for <paramref name="prefab"/> under its own GameObject.
        /// Use when a tower has a projectile prefab but nobody set a pool up in the scene.
        /// </summary>
        public static ProjectilePool For(Projectile prefab, int prewarm = 8)
        {
            if (prefab == null)
            {
                return null;
            }

            // Created inactive so Awake does not run before the prefab is assigned.
            var host = new GameObject($"Pool [{prefab.name}]");
            host.SetActive(false);

            var pool = host.AddComponent<ProjectilePool>();
            pool._prefab = prefab;
            pool._prewarmCount = prewarm;

            host.SetActive(true);
            return pool;
        }

        /// <summary>Takes a projectile from the pool, creating one if none are spare.</summary>
        public Projectile Get()
        {
            if (!EnsurePool())
            {
                Debug.LogError($"[ProjectilePool] '{name}' has no prefab assigned; it cannot spawn anything.", this);
                return null;
            }

            Projectile projectile = _pool.Get();
            projectile.SetPool(this);
            return projectile;
        }

        /// <summary>
        /// Returns a projectile for reuse. Safe to call on a projectile that came from
        /// somewhere else: it is destroyed rather than mixed into this pool.
        /// </summary>
        public void Release(Projectile projectile)
        {
            if (projectile == null)
            {
                return;
            }

            if (_pool == null)
            {
                Destroy(projectile.gameObject);
                return;
            }

            _pool.Release(projectile);
        }

        private void Prewarm(int count)
        {
            if (count <= 0)
            {
                return;
            }

            var warmed = new Projectile[count];
            for (int i = 0; i < count; i++)
            {
                warmed[i] = _pool.Get();
            }

            for (int i = 0; i < count; i++)
            {
                _pool.Release(warmed[i]);
            }
        }

        private Projectile CreateInstance()
        {
            Projectile projectile = Instantiate(_prefab, transform);
            projectile.SetPool(this);
            projectile.gameObject.SetActive(false);
            return projectile;
        }

        private static void OnGet(Projectile projectile)
        {
            projectile.gameObject.SetActive(true);
        }

        private static void OnRelease(Projectile projectile)
        {
            if (projectile != null)
            {
                projectile.gameObject.SetActive(false);
            }
        }

        private static void OnDestroyInstance(Projectile projectile)
        {
            if (projectile != null)
            {
                Destroy(projectile.gameObject);
            }
        }
    }
}
